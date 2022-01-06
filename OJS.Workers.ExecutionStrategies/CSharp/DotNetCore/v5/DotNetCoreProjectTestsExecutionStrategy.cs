namespace OJS.Workers.ExecutionStrategies.CSharp.DotNetCore.v5
{
    using System;
    using OJS.Workers.Common.Models;
    using OJS.Workers.ExecutionStrategies.CSharp.DotNetCore.v3;
    using OJS.Workers.Executors;

    public class DotNetCore5ProjectTestsExecutionStrategy : DotNetCoreProjectTestsExecutionStrategy
    {
        public DotNetCore5ProjectTestsExecutionStrategy(
            Func<CompilerType, string> getCompilerPathFunc,
            IProcessExecutorFactory processExecutorFactory,
            int baseTimeUsed,
            int baseMemoryUsed)
            : base(getCompilerPathFunc, processExecutorFactory, baseTimeUsed, baseMemoryUsed)
        {
        }

        protected override string TargetFrameworkName => "net5.0";

        protected override string MicrosoftEntityFrameworkCoreInMemoryVersion => "5.0.13";

        protected override string MicrosoftEntityFrameworkCoreProxiesVersion => "5.0.13";
    }
}