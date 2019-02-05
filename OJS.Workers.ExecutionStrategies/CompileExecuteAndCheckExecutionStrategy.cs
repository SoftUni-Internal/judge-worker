namespace OJS.Workers.ExecutionStrategies
{
    using System;

    using OJS.Workers.Common;
    using OJS.Workers.Common.Models;
    using OJS.Workers.ExecutionStrategies.Models;
    using OJS.Workers.Executors;

    public class CompileExecuteAndCheckExecutionStrategy : ExecutionStrategy
    {
        public CompileExecuteAndCheckExecutionStrategy(
            Func<CompilerType, string> getCompilerPathFunc,
            IProcessExecutorFactory processExecutorFactory,
            int baseTimeUsed,
            int baseMemoryUsed)
            : base(processExecutorFactory, baseTimeUsed, baseMemoryUsed)
            => this.GetCompilerPathFunc = getCompilerPathFunc;

        protected Func<CompilerType, string> GetCompilerPathFunc { get; }

        protected override IExecutionResult<TestResult> ExecuteAgainstTestsInput(
            IExecutionContext<TestsInputModel> executionContext) =>
                this.CompileExecuteAndCheck(
                    executionContext,
                    this.GetCompilerPathFunc,
                    this.CreateExecutor(ProcessExecutorType.Restricted));

        protected override IExecutionResult<OutputResult> ExecuteAgainstSimpleInput(
            IExecutionContext<string> executionContext)
        {
            var result = new ExecutionResult<OutputResult>();

            var compileResult = this.ExecuteCompiling(
                executionContext,
                this.GetCompilerPathFunc,
                result);

            if (!compileResult.IsCompiledSuccessfully)
            {
                return result;
            }

            var executor = this.CreateExecutor(ProcessExecutorType.Restricted);

            var processExecutionResult = executor.Execute(
                compileResult.OutputFile,
                executionContext.Input ?? string.Empty,
                executionContext.TimeLimit,
                executionContext.MemoryLimit,
                null,
                this.WorkingDirectory);

            result.Results.Add(this.GetOutputResult(processExecutionResult));

            return result;
        }
    }
}
