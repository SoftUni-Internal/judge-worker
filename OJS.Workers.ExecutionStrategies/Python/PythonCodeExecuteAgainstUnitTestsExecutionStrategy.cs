namespace OJS.Workers.ExecutionStrategies.Python
{
    using System;
    using System.Text.RegularExpressions;

    using OJS.Workers.Common;
    using OJS.Workers.Common.Helpers;
    using OJS.Workers.ExecutionStrategies.Models;
    using OJS.Workers.Executors;

    public class PythonCodeExecuteAgainstUnitTestsExecutionStrategy : PythonExecuteAndCheckExecutionStrategy
    {
        private const string PythonIsolatedModeArgument = "-I"; // https://docs.python.org/3/using/cmdline.html#cmdoption-I
        private const string PythonOptimizeAndDiscardDocstringsArgument = "-OO"; // https://docs.python.org/3/using/cmdline.html#cmdoption-OO
        private const string ErrorsTestRegex = @"ERROR:(?:.|\r\n|\r|\n)*?(^[^\s]*Error.*)";
        private const string FailedTestRegex = @"FAIL:(?:.|\r\n|\r|\n)*?(^[^\s]*Error.*)";
        private const string SuccessTestRegex = @"[.]+(?!F|E)([\n]|[\r]|[\r\n])*[-]*(?!\d)[\s\S]*?OK";
        private const string SavedSyntaxErrorRegex = @"line[\s\S]*?SyntaxError: invalid syntax";

        public PythonCodeExecuteAgainstUnitTestsExecutionStrategy(
            IProcessExecutorFactory processExecutorFactory,
            string pythonExecutablePath,
            int baseTimeUsed,
            int baseMemoryUsed)
            : base(processExecutorFactory, pythonExecutablePath, baseTimeUsed, baseMemoryUsed)
        {
        }

        protected override IExecutionResult<TestResult> ExecuteAgainstTestsInput(
            IExecutionContext<TestsInputModel> executionContext,
            IExecutionResult<TestResult> result)
        {
            var executor = this.CreateExecutor();

            var checker = executionContext.Input.GetChecker();

            foreach (var test in executionContext.Input.Tests)
            {
                var codeAndTestFile = this.SaveCodeAndTest(executionContext, test);

                var testSaveFullPath = this.SaveTestToTempFile(test);

                var processExecutionResult = this.Execute(executionContext, executor, codeAndTestFile, string.Empty);

                var message = "Test Passed!";

                if (!string.IsNullOrWhiteSpace(processExecutionResult.ErrorOutput))
                {
                    var errors = Regex.Match(processExecutionResult.ErrorOutput, ErrorsTestRegex, RegexOptions.Multiline).Groups[1].ToString();

                    if (!string.IsNullOrWhiteSpace(errors))
                    {
                        processExecutionResult.ErrorOutput = errors;
                    }

                    var syntaxError = Regex.Match(processExecutionResult.ErrorOutput, SavedSyntaxErrorRegex);

                    if (!string.IsNullOrWhiteSpace(syntaxError.Value))
                    {
                        processExecutionResult.Type = ProcessExecutionResultType.RunTimeError;
                        processExecutionResult.ErrorOutput = syntaxError.Value;
                    }

                    var failedTestError = Regex.Match(processExecutionResult.ErrorOutput, FailedTestRegex, RegexOptions.Multiline).Groups[1].ToString();

                    if (!string.IsNullOrWhiteSpace(failedTestError))
                    {
                        processExecutionResult.Type = ProcessExecutionResultType.Success;
                        message = failedTestError;
                    }

                    var successTest = Regex.IsMatch(processExecutionResult.ErrorOutput, SuccessTestRegex);

                    if (successTest)
                    {
                        processExecutionResult.ReceivedOutput = message;
                        processExecutionResult.Type = ProcessExecutionResultType.Success;
                        processExecutionResult.ErrorOutput = string.Empty;
                    }
                }

                var testResult = this.CheckAndGetTestResult(
                    test,
                    processExecutionResult,
                    checker,
                    message);

                result.Results.Add(testResult);
            }

            return result;
        }

        protected string SaveCodeAndTest<TInput>(IExecutionContext<TInput> execution, TestContext test)
        {
            var codeAndTestText = execution.Code + Environment.NewLine + test.Input;

            return FileHelpers.SaveStringToTempFile(this.WorkingDirectory, codeAndTestText);
        }

        protected override ProcessExecutionResult Execute<TInput>(
            IExecutionContext<TInput> executionContext,
            IExecutor executor,
            string testsSavePath,
            string input)
            => executor.Execute(
                this.PythonExecutablePath,
                input,
                executionContext.TimeLimit,
                executionContext.MemoryLimit,
                new[] { PythonIsolatedModeArgument, PythonOptimizeAndDiscardDocstringsArgument, testsSavePath },
                null,
                false,
                true);

        protected string SaveTestToTempFile(TestContext test)
            => FileHelpers.SaveStringToTempFile(this.WorkingDirectory, test.Input);
    }
}
