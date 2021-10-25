﻿namespace OJS.Workers.ExecutionStrategies.CSharp
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text.RegularExpressions;

    using Microsoft.Build.Evaluation;

    using OJS.Workers.Common;
    using OJS.Workers.Common.Exceptions;
    using OJS.Workers.Common.Extensions;
    using OJS.Workers.Common.Helpers;
    using OJS.Workers.Common.Models;
    using OJS.Workers.ExecutionStrategies.Extensions;
    using OJS.Workers.ExecutionStrategies.Models;
    using OJS.Workers.Executors;

    using static OJS.Workers.Common.Constants;

    public class CSharpProjectTestsExecutionStrategy : BaseCompiledCodeExecutionStrategy
    {
        protected const string SetupFixtureTemplate = @"
        using System;
        using System.IO;
        using NUnit.Framework;

        [SetUpFixture]
        public class SetUpClass
        {
            [OneTimeSetUp]
            public void RedirectConsoleOutputBeforeEveryTest()
            {
                TextWriter writer = new StringWriter();
                Console.SetOut(writer);
            }
        }
";

        protected const string SetupFixtureFileName = "_$SetupFixture";
        protected const string CompeteTest = "Test";
        protected const string TrialTest = "Test.000";
        protected const string CsProjFileSearchPattern = "*.csproj";
        protected const string NUnitReference =
            "nunit.framework, Version=3.8.0.0, Culture=neutral, PublicKeyToken=2638cd05610744eb, processorArchitecture=MSIL";

        protected const string EntityFrameworkCoreInMemoryReference =
                "Microsoft.EntityFrameworkCore.InMemory, Version=1.1.3.0, Culture=neutral, PublicKeyToken=adb9793829ddae60, processorArchitecture=MSIL";

        protected const string SystemDataCommonReference =
            "System.Data.Common, Version=4.1.1.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a, processorArchitecture=MSIL";

        protected const string AdditionalExecutionArguments = "--noresult --inprocess";
        protected const string VsttPackageName = "Microsoft.VisualStudio.QualityTools.UnitTestFramework";

        // Extracts the number of total and passed tests
        protected const string TestResultsRegex =
            @"Test Count: (\d+), Passed: (\d+), Failed: (\d+), Warnings: \d+, Inconclusive: \d+, Skipped: \d+";

        // Extracts error/failure messages and the class which threw it
        protected static readonly string ErrorMessageRegex =
            @"((?:\d+|\d+-\d+)\) (?:Failed|Error)\s:\s(.*)\.(.*))\r?\n((?:.*)\r?\n(?:.*))";

        public CSharpProjectTestsExecutionStrategy(
            Func<CompilerType, string> getCompilerPathFunc,
            IProcessExecutorFactory processExecutorFactory,
            int baseTimeUsed,
            int baseMemoryUsed)
            : base(processExecutorFactory, baseTimeUsed, baseMemoryUsed)
        {
            this.GetCompilerPathFunc = getCompilerPathFunc;
            this.TestNames = new List<string>();
            this.TestPaths = new List<string>();
        }

        public CSharpProjectTestsExecutionStrategy(
            Func<CompilerType, string> getCompilerPathFunc,
            IProcessExecutorFactory processExecutorFactory,
            string nUnitConsoleRunnerPath,
            int baseTimeUsed,
            int baseMemoryUsed)
            : base(processExecutorFactory, baseTimeUsed, baseMemoryUsed)
        {
            if (!File.Exists(nUnitConsoleRunnerPath))
            {
                throw new ArgumentException(
                    $"NUnitConsole not found in: {nUnitConsoleRunnerPath}",
                    nameof(nUnitConsoleRunnerPath));
            }

            this.NUnitConsoleRunnerPath = nUnitConsoleRunnerPath;
            this.GetCompilerPathFunc = getCompilerPathFunc;
            this.TestNames = new List<string>();
            this.TestPaths = new List<string>();
        }

        protected string NUnitConsoleRunnerPath { get; }

        protected Func<CompilerType, string> GetCompilerPathFunc { get; }

        protected string SetupFixturePath { get; set; }

        protected List<string> TestNames { get; }

        protected List<string> TestPaths { get; }

        protected override IExecutionResult<TestResult> ExecuteAgainstTestsInput(
            IExecutionContext<TestsInputModel> executionContext,
            IExecutionResult<TestResult> result)
        {
            this.SaveZipSubmission(executionContext.FileContent, this.WorkingDirectory);

            var csProjFilePath = this.GetCsProjFilePath();

            this.ExtractTestNames(executionContext.Input.Tests);

            var project = new Project(csProjFilePath);
            var compileDirectory = project.DirectoryPath;

            this.SaveTestFiles(executionContext.Input.Tests, compileDirectory);
            this.SaveSetupFixture(compileDirectory);

            this.CorrectProjectReferences(project);

            var compilerPath = this.GetCompilerPathFunc(executionContext.CompilerType);
            var compilerResult = this.Compile(
                executionContext.CompilerType,
                compilerPath,
                executionContext.AdditionalCompilerArguments,
                csProjFilePath);

            result.IsCompiledSuccessfully = compilerResult.IsCompiledSuccessfully;
            result.CompilerComment = compilerResult.CompilerComment;

            if (!compilerResult.IsCompiledSuccessfully)
            {
                return result;
            }

            // Delete tests before execution so the user can't access them
            FileHelpers.DeleteFiles(this.TestPaths.ToArray());

            var executor = this.CreateExecutor(ProcessExecutorType.Restricted);

            return this.RunUnitTests(
                this.NUnitConsoleRunnerPath,
                executionContext,
                executor,
                executionContext.Input.GetChecker(),
                result,
                compilerResult.OutputFile,
                AdditionalExecutionArguments);
        }

        protected void SaveSetupFixture(string directory)
        {
            this.SetupFixturePath = $"{directory}\\{SetupFixtureFileName}{CSharpFileExtension}";
            File.WriteAllText(this.SetupFixturePath, SetupFixtureTemplate);
            this.TestPaths.Add(this.SetupFixturePath);
        }

        protected void SaveTestFiles(IEnumerable<TestContext> tests, string compileDirectory)
        {
            var index = 0;
            foreach (var test in tests)
            {
                var testName = this.TestNames[index++];
                var testedCodePath = FileHelpers.BuildPath(compileDirectory, $"{testName}{CSharpFileExtension}");
                this.TestPaths.Add(testedCodePath);
                File.WriteAllText(testedCodePath, test.Input);
            }
        }

        protected virtual IExecutionResult<TestResult> RunUnitTests(
            string consoleRunnerPath,
            IExecutionContext<TestsInputModel> executionContext,
            IExecutor executor,
            IChecker checker,
            IExecutionResult<TestResult> result,
            string compiledFile,
            string additionalExecutionArguments)
        {
            var arguments = new List<string> { $"\"{compiledFile}\"" };
            arguments.AddRange(additionalExecutionArguments.Split(' '));

            var processExecutionResult = executor.Execute(
                consoleRunnerPath,
                string.Empty,
                executionContext.TimeLimit,
                executionContext.MemoryLimit,
                arguments,
                null,
                false,
                true);

            if (!string.IsNullOrWhiteSpace(processExecutionResult.ErrorOutput))
            {
                throw new InvalidProcessExecutionOutputException(processExecutionResult.ErrorOutput);
            }

            var (totalTestsCount, failedTestsCount) =
                this.ExtractTotalFailedTestsCount(processExecutionResult.ReceivedOutput);

            var errorsByFiles = this.GetTestErrors(processExecutionResult.ReceivedOutput);

            if (failedTestsCount != errorsByFiles.Count || totalTestsCount != executionContext.Input.Tests.Count())
            {
                throw new ArgumentException("Failing tests not captured properly, please contact an administrator");
            }

            var testIndex = 0;

            foreach (var test in executionContext.Input.Tests)
            {
                var message = TestPassedMessage;
                var testFile = this.TestNames[testIndex++];
                if (errorsByFiles.ContainsKey(testFile))
                {
                    message = errorsByFiles[testFile];
                }

                var testResult = this.CheckAndGetTestResult(test, processExecutionResult, checker, message);
                result.Results.Add(testResult);
            }

            return result;
        }

        protected virtual Dictionary<string, string> GetTestErrors(string receivedOutput)
        {
            var errorsByFiles = new Dictionary<string, string>();
            var errorRegex = new Regex(ErrorMessageRegex);
            var errors = errorRegex.Matches(receivedOutput);

            foreach (Match error in errors)
            {
                var failedAssert = error.Groups[1].Value;
                var cause = error.Groups[4].Value;
                var fileName = error.Groups[2].Value;
                var output = $"{failedAssert} : {cause}".ToSingleLine();

                if (errorsByFiles.ContainsKey(fileName))
                {
                    errorsByFiles[fileName] += ". " + output;
                }
                else
                {
                    errorsByFiles.Add(fileName, output);
                }
            }

            return errorsByFiles;
        }

        protected virtual void CorrectProjectReferences(Project project)
        {
            var additionalCompileItems = new List<string>(this.TestNames) { SetupFixtureFileName };

            project.AddCompileItems(additionalCompileItems);

            project.EnsureAssemblyNameIsCorrect();

            project.SetProperty("OutputType", "Library");

            project.AddReferences(
                NUnitReference,
                EntityFrameworkCoreInMemoryReference,
                SystemDataCommonReference);

            // Check for VSTT just in case, we don't want Assert conflicts
            project.RemoveItemByName(VsttPackageName);

            project.Save(project.FullPath);
            project.ProjectCollection.UnloadAllProjects();

            project.RemoveNuGetPackageImportsTarget();
        }

        protected virtual void ExtractTestNames(IEnumerable<TestContext> tests)
        {
            foreach (var test in tests)
            {
                var testName = CSharpPreprocessorHelper.GetClassName(test.Input);
                this.TestNames.Add(testName);
            }
        }

        protected (int totalTestsCount, int failedTestsCount) ExtractTotalFailedTestsCount(string testsOutput)
        {
            var testsSummaryMatcher = new Regex(TestResultsRegex);
            var testsSummaryMatches = testsSummaryMatcher.Matches(testsOutput);
            if (testsSummaryMatches.Count == 0)
            {
                throw new InvalidProcessExecutionOutputException();
            }

            var failedTestsCount = int.Parse(testsSummaryMatches[testsSummaryMatches.Count - 1].Groups[3].Value);
            var totalTestsCount = int.Parse(testsSummaryMatches[testsSummaryMatches.Count - 1].Groups[1].Value);
            return (totalTestsCount, failedTestsCount);
        }

        protected (int totalTests, int passedTests) ExtractTotalAndPassedTestsCount(MatchCollection matches)
        {
            // Grabs the last match from a match collection,
            // since the NUnit output is always the last one,
            // thus ensuring that the tests output is the genuine one,
            // preventing the user from tampering with it
            var lastMatch = matches[matches.Count - 1];

            var totalTests = int.Parse(lastMatch.Groups[1].Value);
            var passedTests = int.Parse(lastMatch.Groups[2].Value);

            return (totalTests, passedTests);
        }

        protected virtual string GetCsProjFilePath() => FileHelpers.FindFileMatchingPattern(
            this.WorkingDirectory,
            CsProjFileSearchPattern,
            f => new FileInfo(f).Length);
    }
}
