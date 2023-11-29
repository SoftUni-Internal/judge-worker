﻿namespace OJS.Workers.ExecutionStrategies.NodeJs
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Text.RegularExpressions;

    using OJS.Workers.Common;
    using OJS.Workers.Common.Extensions;
    using OJS.Workers.Common.Helpers;
    using OJS.Workers.ExecutionStrategies.Models;
    using OJS.Workers.Executors;

    public class NodeJsZipExecuteHtmlAndCssStrategy : NodeJsPreprocessExecuteAndRunUnitTestsWithMochaExecutionStrategy
    {
        protected const string EntryFileName = "*.html";
        protected const string UserBaseDirectoryPlaceholder = "#userBaseDirectoryPlaceholder#";

        public NodeJsZipExecuteHtmlAndCssStrategy(
            IProcessExecutorFactory processExecutorFactory,
            string nodeJsExecutablePath,
            string mochaModulePath,
            string chaiModulePath,
            string sinonModulePath,
            string sinonChaiModulePath,
            string jsdomModulePath,
            string jqueryModulePath,
            string underscoreModulePath,
            string bootsrapModulePath,
            string bootstrapCssPath,
            int baseTimeUsed,
            int baseMemoryUsed)
            : base(
                processExecutorFactory,
                nodeJsExecutablePath,
                mochaModulePath,
                chaiModulePath,
                sinonModulePath,
                sinonChaiModulePath,
                underscoreModulePath,
                baseTimeUsed,
                baseMemoryUsed)
        {
            if (!Directory.Exists(jsdomModulePath))
            {
                throw new ArgumentException(
                    $"jsDom not found in: {jsdomModulePath}",
                    nameof(jsdomModulePath));
            }

            if (!Directory.Exists(jqueryModulePath))
            {
                throw new ArgumentException(
                    $"jQuery not found in: {jqueryModulePath}",
                    nameof(jqueryModulePath));
            }

            if (!File.Exists(bootsrapModulePath))
            {
                throw new ArgumentException(
                    $"Bootstrap Module not found in: {bootsrapModulePath}",
                    nameof(bootsrapModulePath));
            }

            if (!File.Exists(bootstrapCssPath))
            {
                throw new ArgumentException(
                    $"Bootstrap CSS not found in: {bootstrapCssPath}",
                    nameof(bootstrapCssPath));
            }

            this.JsDomModulePath = FileHelpers.ProcessModulePath(jsdomModulePath);
            this.JQueryModulePath = FileHelpers.ProcessModulePath(jqueryModulePath);
            this.BootstrapModulePath = FileHelpers.ProcessModulePath(bootsrapModulePath);
            this.BootstrapCssPath = FileHelpers.ProcessModulePath(bootstrapCssPath);
        }

        protected string JsDomModulePath { get; }

        protected string JQueryModulePath { get; }

        protected string BootstrapModulePath { get; }

        protected string BootstrapCssPath { get; }

        protected string ProgramEntryPath { get; set; }

        protected override string JsNodeDisableCode => base.JsNodeDisableCode + @"
fs = undefined;";

        protected override string JsCodeRequiredModules => base.JsCodeRequiredModules + $@",
    fs = require('fs'),
    jsdom = require('{this.JsDomModulePath}'),
    jq = require('{this.JQueryModulePath}'),
    bootstrap = fs.readFileSync('{this.BootstrapModulePath}','utf-8'),
    bootstrapCss = fs.readFileSync('{this.BootstrapCssPath}','utf-8'),
    userCode = fs.readFileSync('{UserInputPlaceholder}','utf-8')";

        protected override string JsCodeTemplate =>
            RequiredModules + ";" +
            PreevaluationPlaceholder +
            EvaluationPlaceholder +
            PostevaluationPlaceholder;

        protected override string JsCodePreevaulationCode => $@"
describe('TestDOMScope', function() {{
    let bgCoderConsole = {{}};
before(function(done) {{
    const dom = new jsdom.JSDOM(userCode, {{
        runScripts: ""dangerously"",
        resources: ""usable""
    }});

    const {{ window }} = dom;

    global.window = window;
    global.document = window.document;
    global.$ = global.jQuery = jq(window);

    Object.getOwnPropertyNames(window)
        .filter((prop) => prop.toLowerCase().indexOf('html') >= 0)
        .forEach((prop) => {{
            global[prop] = window[prop];
        }});

    let head = $(document.head);
    let style = document.createElement('style');
    style.type = 'text/css';
    style.innerHTML = bootstrapCss;
    head.append(style);

    head.find('link').each(function() {{
        let link = $(this);
        let cssPath = link.attr('href').replace('{UserBaseDirectoryPlaceholder}/', '');
        let cssContent = fs.readFileSync(cssPath, 'utf-8');
        let inlineStyle = document.createElement('style');
        inlineStyle.type = 'text/css';
        inlineStyle.innerHTML = cssContent;
        head.append(inlineStyle);
    }}).remove();

    let bgCoderConsole = {{}};
    Object.keys(console).forEach((prop) => {{
        bgCoderConsole[prop] = console[prop];
        console[prop] = function() {{}};
    }});

    {NodeDisablePlaceholder}

    done();
}});

    after(function() {{
        Object.keys(bgCoderConsole)
            .forEach(function (prop) {{
                console[prop] = bgCoderConsole[prop];
            }});
    }});";

        protected override string JsCodeEvaluation => TestsPlaceholder;

        protected override IExecutionResult<TestResult> ExecuteAgainstTestsInput(
            IExecutionContext<TestsInputModel> executionContext,
            IExecutionResult<TestResult> result)
        {
            this.SaveZipSubmission(executionContext.FileContent, this.WorkingDirectory);
            this.ProgramEntryPath = FileHelpers.FindFileMatchingPattern(this.WorkingDirectory, EntryFileName);

            var codeToExecute = this.PreprocessJsSubmission(
                this.JsCodeTemplate,
                executionContext,
                this.ProgramEntryPath);

            var codeSavePath = FileHelpers.SaveStringToTempFile(this.WorkingDirectory, codeToExecute);
            var executor = this.CreateExecutor(ProcessExecutorType.Restricted);

            result.Results.AddRange(this.ProcessTests(
                executionContext,
                executor,
                executionContext.Input.GetChecker(),
                codeSavePath));

            File.Delete(codeSavePath);

            return result;
        }

        protected virtual string BuildTests(IEnumerable<TestContext> tests)
        {
            var testsCode = string.Empty;
            var testsCount = 1;
            foreach (var test in tests)
            {
                var code = Regex.Replace(test.Input, "([\\\\`$])", "\\$1");

                testsCode += $@"
                it('Test{testsCount++}', function(done) {{
                    this.timeout(10000);
            	    let content = `{code}`;

                    let testFunc = new Function({this.TestFuncVariables}, content);
                    testFunc.call({{}},{this.TestFuncVariables.Replace("'", string.Empty)});

                    done();
                }});";
            }

            return testsCode;
        }

        protected override List<TestResult> ProcessTests(
            IExecutionContext<TestsInputModel> executionContext,
            IExecutor executor,
            IChecker checker,
            string codeSavePath)
        {
            var testResults = new List<TestResult>();
            var arguments = new List<string>();
            arguments.Add(this.MochaModulePath);
            arguments.Add(codeSavePath);
            arguments.AddRange(this.AdditionalExecutionArguments);

            var processExecutionResult = executor.Execute(
                this.NodeJsExecutablePath,
                string.Empty,
                executionContext.TimeLimit,
                executionContext.MemoryLimit,
                arguments);

            var mochaResult = JsonExecutionResult.Parse(processExecutionResult.ReceivedOutput);
            var currentTest = 0;
            foreach (var test in executionContext.Input.Tests)
            {
                var message = "yes";
                if (!string.IsNullOrEmpty(mochaResult.Error))
                {
                    message = mochaResult.Error;
                }
                else if (mochaResult.TestErrors[currentTest] != null)
                {
                    message = $"Unexpected error: {mochaResult.TestErrors[currentTest]}";
                }

                var testResult = this.CheckAndGetTestResult(
                    test,
                    processExecutionResult,
                    checker,
                    message);
                currentTest++;
                testResults.Add(testResult);
            }

            return testResults;
        }

        protected virtual string PreprocessJsSubmission(
            string template,
            IExecutionContext<TestsInputModel> context,
            string pathToFile)
        {
            var userBaseDirectory = FileHelpers.FindFileMatchingPattern(this.WorkingDirectory, EntryFileName);
            userBaseDirectory = FileHelpers.ProcessModulePath(Path.GetDirectoryName(userBaseDirectory));

            var processedCode =
                template.Replace(RequiredModules, this.JsCodeRequiredModules)
                    .Replace(PreevaluationPlaceholder, this.JsCodePreevaulationCode)
                    .Replace(EvaluationPlaceholder, this.JsCodeEvaluation)
                    .Replace(PostevaluationPlaceholder, this.JsCodePostevaulationCode)
                    .Replace(NodeDisablePlaceholder, this.JsNodeDisableCode)
                    .Replace(UserInputPlaceholder, pathToFile)
                    .Replace(UserBaseDirectoryPlaceholder, userBaseDirectory)
                    .Replace(TestsPlaceholder, this.BuildTests(context.Input.Tests));

            return processedCode;
        }
    }
}
