namespace OJS.Workers.ExecutionStrategies
{
    using System;
    using System.IO;

    using OJS.Workers.Common;
    using OJS.Workers.Common.Helpers;
    using OJS.Workers.ExecutionStrategies.Models;
    using OJS.Workers.Executors;

    public class PythonExecuteAndCheckExecutionStrategy : ExecutionStrategy
    {
        private const string PythonIsolatedModeArgument = "-I"; // https://docs.python.org/3/using/cmdline.html#cmdoption-I
        private const string PythonOptimizeAndDiscardDocstringsArgument = "-OO"; // https://docs.python.org/3/using/cmdline.html#cmdoption-OO

        private readonly string pythonExecutablePath;

        public PythonExecuteAndCheckExecutionStrategy(
            string pythonExecutablePath,
            int baseTimeUsed,
            int baseMemoryUsed)
            : base(baseTimeUsed, baseMemoryUsed)
        {
            if (!File.Exists(pythonExecutablePath))
            {
                throw new ArgumentException($"Python not found in: {pythonExecutablePath}", nameof(pythonExecutablePath));
            }

            this.pythonExecutablePath = pythonExecutablePath;
        }

        protected override IExecutionResult<TestResult> ExecuteCompetitive(
            IExecutionContext<TestsInputModel> executionContext)
        {
            var result = new ExecutionResult<TestResult>();

            // Python code is not compiled
            result.IsCompiledSuccessfully = true;

            var codeSavePath = FileHelpers.SaveStringToTempFile(this.WorkingDirectory, executionContext.Code);

            // Process the submission and check each test
            var executor = new RestrictedProcessExecutor(this.BaseTimeUsed, this.BaseMemoryUsed);

            var checker = executionContext.Input.GetChecker();

            foreach (var test in executionContext.Input.Tests)
            {
                var processExecutionResult = executor.Execute(
                    this.pythonExecutablePath,
                    test.Input,
                    executionContext.TimeLimit,
                    executionContext.MemoryLimit,
                    new[] { PythonIsolatedModeArgument, PythonOptimizeAndDiscardDocstringsArgument, codeSavePath },
                    null,
                    false,
                    true);

                var testResult = this.ExecuteAndCheckTest(
                    test,
                    processExecutionResult,
                    checker,
                    processExecutionResult.ReceivedOutput);

                result.Results.Add(testResult);
            }

            // Clean up
            File.Delete(codeSavePath);

            return result;
        }
    }
}
