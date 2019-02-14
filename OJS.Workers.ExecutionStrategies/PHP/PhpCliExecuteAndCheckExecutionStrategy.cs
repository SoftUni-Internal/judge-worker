namespace OJS.Workers.ExecutionStrategies.PHP
{
    using System;
    using System.IO;

    using OJS.Workers.Common;
    using OJS.Workers.ExecutionStrategies.Models;
    using OJS.Workers.Executors;

    public class PhpCliExecuteAndCheckExecutionStrategy : BaseInterpretedCodeExecutionStrategy
    {
        private readonly string phpCliExecutablePath;

        public PhpCliExecuteAndCheckExecutionStrategy(
            IProcessExecutorFactory processExecutorFactory,
            string phpCliExecutablePath,
            int baseTimeUsed,
            int baseMemoryUsed)
            : base(processExecutorFactory, baseTimeUsed, baseMemoryUsed)
        {
            if (!File.Exists(phpCliExecutablePath))
            {
                throw new ArgumentException($"PHP CLI not found in: {phpCliExecutablePath}", nameof(phpCliExecutablePath));
            }

            this.phpCliExecutablePath = phpCliExecutablePath;
        }

        protected override IExecutionResult<TestResult> ExecuteAgainstTestsInput(
            IExecutionContext<TestsInputModel> context,
            IExecutionResult<TestResult> result)
        {
            var codeSavePath = this.SaveCodeToTempFile(context);

            var executor = this.CreateExecutor(ProcessExecutorType.Restricted);

            var checker = context.Input.GetChecker();

            foreach (var test in context.Input.Tests)
            {
                var processExecutionResult = this.Execute(context, executor, codeSavePath, test.Input);

                var testResult = this.CheckAndGetTestResult(
                    test,
                    processExecutionResult,
                    checker,
                    processExecutionResult.ReceivedOutput);

                result.Results.Add(testResult);
            }

            return result;
        }

        protected override IExecutionResult<OutputResult> ExecuteAgainstSimpleInput(
            IExecutionContext<string> context,
            IExecutionResult<OutputResult> result)
        {
            var codeSavePath = this.SaveCodeToTempFile(context);

            var executor = this.CreateExecutor(ProcessExecutorType.Restricted);

            var processExecutionResult = this.Execute(context, executor, codeSavePath, context.Input);

            result.Results.Add(this.GetOutputResult(processExecutionResult));

            return result;
        }

        private ProcessExecutionResult Execute<TInput>(
            IExecutionContext<TInput> executionContext,
            IExecutor executor,
            string codeSavePath,
            string input)
            => executor.Execute(
                this.phpCliExecutablePath,
                input,
                executionContext.TimeLimit,
                executionContext.MemoryLimit,
                new[] { codeSavePath });
    }
}
