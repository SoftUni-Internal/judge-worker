namespace OJS.Workers.ExecutionStrategies
{
    using System;
    using System.Collections.Generic;
    using System.IO;

    using OJS.Workers.Checkers;
    using OJS.Workers.Common;
    using OJS.Workers.Common.Helpers;
    using OJS.Workers.ExecutionStrategies.Models;
    using OJS.Workers.Executors;

    public class PhpCliExecuteAndCheckExecutionStrategy : ExecutionStrategy
    {
        private readonly string phpCliExecutablePath;

        public PhpCliExecuteAndCheckExecutionStrategy(
            string phpCliExecutablePath,
            int baseTimeUsed,
            int baseMemoryUsed)
            : base(baseTimeUsed, baseMemoryUsed)
        {
            if (!File.Exists(phpCliExecutablePath))
            {
                throw new ArgumentException($"PHP CLI not found in: {phpCliExecutablePath}", nameof(phpCliExecutablePath));
            }

            this.phpCliExecutablePath = phpCliExecutablePath;
        }

        protected override IExecutionResult<TestResult> ExecuteCompetitive(
            CompetitiveExecutionContext executionContext)
        {
            var result = new ExecutionResult<TestResult>();

            // PHP code is not compiled
            result.IsCompiledSuccessfully = true;

            var codeSavePath = FileHelpers.SaveStringToTempFile(this.WorkingDirectory, executionContext.Code);

            // Process the submission and check each test
            var executor = new RestrictedProcessExecutor(this.BaseTimeUsed, this.BaseMemoryUsed);
            var checker = Checker.CreateChecker(executionContext.CheckerAssemblyName, executionContext.CheckerTypeName, executionContext.CheckerParameter);

            foreach (var test in executionContext.Tests)
            {
                var processExecutionResult = executor.Execute(
                    this.phpCliExecutablePath,
                    test.Input,
                    executionContext.TimeLimit,
                    executionContext.MemoryLimit,
                    new[] { codeSavePath });

                var testResult = this.ExecuteAndCheckTest(test, processExecutionResult, checker, processExecutionResult.ReceivedOutput);
                result.Results.Add(testResult);
            }

            // Clean up
            File.Delete(codeSavePath);

            return result;
        }
    }
}
