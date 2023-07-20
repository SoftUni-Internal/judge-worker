﻿namespace OJS.Workers.ExecutionStrategies.Python
{
    using System;
    using System.Text.RegularExpressions;

    using OJS.Workers.Common;
    using OJS.Workers.Common.Helpers;
    using OJS.Workers.ExecutionStrategies.Models;
    using OJS.Workers.Executors;

    using static OJS.Workers.Common.Constants;

    public class PythonCodeExecuteAgainstUnitTestsExecutionStrategy : PythonExecuteAndCheckExecutionStrategy
    {
        private const string ErrorInTestRegexPattern = @"^ERROR:[\s\S]+(^\w*Error:[\s\S]+)(?=^-{2,})";
        private const string FailedTestRegexPattern = @"^FAIL:[\s\S]+(^\w*Error:[\s\S]+)(?=^-{2,})";
        private const string SuccessTestsRegexPattern = @"^[.]+\s*[-]{2,}.+(?<=\r\n|\r|\n)OK(?=\s*?$)";
        private const string TestResultsRegexPattern = @"^([.FE]+)\s*.+(?<=\r\n|\r|\n)(OK|FAILED\s\(.+\))(?=\s*?$)";

        public PythonCodeExecuteAgainstUnitTestsExecutionStrategy(
            IProcessExecutorFactory processExecutorFactory,
            string pythonExecutablePath,
            int baseTimeUsed,
            int baseMemoryUsed)
            : base(processExecutorFactory, pythonExecutablePath, baseTimeUsed, baseMemoryUsed)
        {
        }

        protected virtual Regex TestsRegex => new Regex(TestResultsRegexPattern, RegexOptions.Singleline);

        protected virtual Regex SuccessTestsRegex => new Regex(SuccessTestsRegexPattern, RegexOptions.Singleline);

        protected virtual Regex ErrorsInTestsRegex => new Regex(ErrorInTestRegexPattern, RegexOptions.Multiline);

        protected virtual Regex FailedTestsRegex => new Regex(FailedTestRegexPattern, RegexOptions.Multiline);

        protected override TestResult RunIndividualTest(
            string codeSavePath,
            IExecutor executor,
            IChecker checker,
            IExecutionContext<TestsInputModel> executionContext,
            TestContext test)
        {
            this.WriteTestInCodeFile(executionContext.Code, codeSavePath, test.Input);

            var processExecutionResult = this.Execute(executionContext, executor, codeSavePath, string.Empty);

            var testResult = this.GetTestResult(processExecutionResult, test, checker);

            return testResult;
        }

        protected override ProcessExecutionResult Execute<TInput>(
            IExecutionContext<TInput> executionContext,
            IExecutor executor,
            string codeSavePath,
            string input,
            string directory = null)
        {
            var processExecutionResult = base.Execute(executionContext, executor, codeSavePath, input, directory);
            this.FixReceivedOutput(processExecutionResult);
            return processExecutionResult;
        }

        protected TestResult GetTestResult(
            ProcessExecutionResult processExecutionResult,
            TestContext test,
            IChecker checker)
        {
            var message = "Failing tests are not captured correctly. Please contact an Administrator.";

            var errorMatch = ErrorsInTestsRegex.Match(processExecutionResult.ReceivedOutput);
            var failedTestMatch = FailedTestsRegex.Match(processExecutionResult.ReceivedOutput);

            if (errorMatch.Success)
            {
                processExecutionResult.ErrorOutput = errorMatch.Groups[1].Value;
                processExecutionResult.Type = ProcessExecutionResultType.RunTimeError;
            }
            else if (failedTestMatch.Success)
            {
                message = failedTestMatch.Groups[1].Value;
            }
            else if (SuccessTestsRegex.IsMatch(processExecutionResult.ReceivedOutput))
            {
                message = TestPassedMessage;
            }

            var testResult = this.CheckAndGetTestResult(
                test,
                processExecutionResult,
                checker,
                message);

            return testResult;
        }

        protected void WriteTestInCodeFile(string code, string codeSavePath, string testContent)
        {
            var codeAndTestText = code + Environment.NewLine + testContent;

            FileHelpers.WriteAllText(codeSavePath, codeAndTestText);
        }

        protected void FixReceivedOutput(ProcessExecutionResult processExecutionResult)
        {
            var output = processExecutionResult.ErrorOutput ?? string.Empty;

            if (TestsRegex.IsMatch(output))
            {
                processExecutionResult.ReceivedOutput = output;
                processExecutionResult.ErrorOutput = string.Empty;
                processExecutionResult.Type = ProcessExecutionResultType.Success;
            }
        }
    }
}
