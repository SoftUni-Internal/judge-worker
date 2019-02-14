namespace OJS.Workers.ExecutionStrategies.Python
{
    using System;
    using System.IO;

    using OJS.Workers.Common;
    using OJS.Workers.Common.Helpers;
    using OJS.Workers.ExecutionStrategies.Models;
    using OJS.Workers.Executors;

    public class PythonExecuteAndCheckExecutionStrategy : BaseInterpretedCodeExecutionStrategy
    {
        private const string PythonIsolatedModeArgument = "-I"; // https://docs.python.org/3/using/cmdline.html#cmdoption-I
        private const string PythonOptimizeAndDiscardDocstringsArgument = "-OO"; // https://docs.python.org/3/using/cmdline.html#cmdoption-OO

        private readonly string pythonExecutablePath;

        public PythonExecuteAndCheckExecutionStrategy(
            IProcessExecutorFactory processExecutorFactory,
            string pythonExecutablePath,
            int baseTimeUsed,
            int baseMemoryUsed)
            : base(processExecutorFactory, baseTimeUsed, baseMemoryUsed)
        {
            if (!File.Exists(pythonExecutablePath))
            {
                throw new ArgumentException($"Python not found in: {pythonExecutablePath}", nameof(pythonExecutablePath));
            }

            this.pythonExecutablePath = pythonExecutablePath;
        }

        protected override IExecutionResult<TestResult> ExecuteAgainstTestsInput(
            IExecutionContext<TestsInputModel> context,
            IExecutionResult<TestResult> result)
        {
            var codeSavePath = this.SaveCodeToTempFile(context);

            var executor = this.CreateExecutor();

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

            var executor = this.CreateExecutor();

            var processExecutionResult = this.Execute(context, executor, codeSavePath, context.Input);

            result.Results.Add(this.GetOutputResult(processExecutionResult));

            return result;
        }

        private IExecutor CreateExecutor()
            => this.CreateExecutor(ProcessExecutorType.Restricted);

        private ProcessExecutionResult Execute<TInput>(
            IExecutionContext<TInput> executionContext,
            IExecutor executor,
            string codeSavePath,
            string input)
            => executor.Execute(
                this.pythonExecutablePath,
                input,
                executionContext.TimeLimit,
                executionContext.MemoryLimit,
                new[] { PythonIsolatedModeArgument, PythonOptimizeAndDiscardDocstringsArgument, codeSavePath },
                null,
                false,
                true);
    }
}
