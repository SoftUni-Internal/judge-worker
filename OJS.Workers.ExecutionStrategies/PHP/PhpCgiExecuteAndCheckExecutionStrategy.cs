namespace OJS.Workers.ExecutionStrategies.PHP
{
    using System;
    using System.IO;

    using OJS.Workers.Common;
    using OJS.Workers.ExecutionStrategies.Models;
    using OJS.Workers.Executors;

    public class PhpCgiExecuteAndCheckExecutionStrategy : BaseInterpretedCodeExecutionStrategy
    {
        private const string FileToExecuteOption = "--file";

        private readonly string phpCgiExecutablePath;

        public PhpCgiExecuteAndCheckExecutionStrategy(
            IProcessExecutorFactory processExecutorFactory,
            string phpCgiExecutablePath,
            int baseTimeUsed,
            int baseMemoryUsed)
            : base(processExecutorFactory, baseTimeUsed, baseMemoryUsed)
        {
            if (!File.Exists(phpCgiExecutablePath))
            {
                throw new ArgumentException($"PHP CGI not found in: {phpCgiExecutablePath}", nameof(phpCgiExecutablePath));
            }

            this.phpCgiExecutablePath = phpCgiExecutablePath;
        }

        protected override IExecutionResult<TestResult> ExecuteAgainstTestsInput(
            IExecutionContext<TestsInputModel> executionContext,
            IExecutionResult<TestResult> result)
        {
            var codeSavePath = this.SaveCodeToTempFile(executionContext);

            // Process the submission and check each test
            var executor = this.CreateExecutor(ProcessExecutorType.Restricted);

            var checker = executionContext.Input.GetChecker();

            foreach (var test in executionContext.Input.Tests)
            {
                var processExecutionResult = executor.Execute(
                    this.phpCgiExecutablePath,
                    string.Empty, // Input data is passed as the last execution argument
                    executionContext.TimeLimit,
                    executionContext.MemoryLimit,
                    new[] { FileToExecuteOption, codeSavePath, $"\"{test.Input}\"" });

                var testResult = this.CheckAndGetTestResult(
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
