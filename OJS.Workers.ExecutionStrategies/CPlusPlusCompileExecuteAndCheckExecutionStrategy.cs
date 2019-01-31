namespace OJS.Workers.ExecutionStrategies
{
    using System;

    using OJS.Workers.Common;
    using OJS.Workers.Common.Models;
    using OJS.Workers.ExecutionStrategies.Models;
    using OJS.Workers.Executors;

    public class CPlusPlusCompileExecuteAndCheckExecutionStrategy : CompileExecuteAndCheckExecutionStrategy
    {
        public CPlusPlusCompileExecuteAndCheckExecutionStrategy(
            Func<CompilerType, string> getCompilerPathFunc,
            IProcessExecutorFactory processExecutorFactory,
            int baseTimeUsed,
            int baseMemoryUsed)
            : base(getCompilerPathFunc, processExecutorFactory, baseTimeUsed, baseMemoryUsed)
        {
        }

        protected override IExecutionResult<TestResult> ExecuteAgainstTestsInput(
            IExecutionContext<TestsInputModel> executionContext)
        {
            var result = this.CompileExecuteAndCheck(
                executionContext,
                this.GetCompilerPathFunc,
                this.CreateExecutor(ProcessExecutorType.Restricted),
                useSystemEncoding: false,
                dependOnExitCodeForRunTimeError: true);

            return result;
        }
    }
}
