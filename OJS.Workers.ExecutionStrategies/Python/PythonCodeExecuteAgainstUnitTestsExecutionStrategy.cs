namespace OJS.Workers.ExecutionStrategies.Python
{
    using System;

    using OJS.Workers.Common;
    using OJS.Workers.Common.Helpers;
    using OJS.Workers.ExecutionStrategies.Models;
    using OJS.Workers.Executors;

    public class PythonCodeExecuteAgainstUnitTestsExecutionStrategy : PythonExecuteAndCheckExecutionStrategy
    {
        private const string PythonIsolatedModeArgument = "-I"; // https://docs.python.org/3/using/cmdline.html#cmdoption-I
        private const string PythonOptimizeAndDiscardDocstringsArgument = "-OO"; // https://docs.python.org/3/using/cmdline.html#cmdoption-OO
        private const string NameForModule = "activities.py";

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

                var testResult = this.CheckAndGetTestResult(
                    test,
                    processExecutionResult,
                    checker,
                    processExecutionResult.ReceivedOutput);

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
