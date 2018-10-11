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
            int baseTimeUsed,
            int baseMemoryUsed)
            : base(getCompilerPathFunc, baseTimeUsed, baseMemoryUsed)
        {
        }

        protected override IExecutionResult<TestResult> ExecuteCompetitive(
            CompetitiveExecutionContext executionContext)
        {
            IExecutor executor = new RestrictedProcessExecutor(this.BaseTimeUsed, this.BaseMemoryUsed);

            var result = this.CompileExecuteAndCheck(
                executionContext,
                this.GetCompilerPathFunc,
                executor,
                useSystemEncoding: false,
                dependOnExitCodeForRunTimeError: true);

            return result;
        }
    }
}
