namespace OJS.Workers.ExecutionStrategies.CSharp.DotNetCore.v6
{
    using OJS.Workers.Common.Models;
    using OJS.Workers.ExecutionStrategies.CSharp.DotNetCore.v3;
    using OJS.Workers.Executors;
    using System;

    public class DotNetCore6ProjectTestsExecutionStrategy : DotNetCoreProjectTestsExecutionStrategy
    {
        public DotNetCore6ProjectTestsExecutionStrategy(
            Func<CompilerType, string> getCompilerPathFunc,
            IProcessExecutorFactory processExecutorFactory,
            int baseTimeUsed,
            int baseMemoryUsed)
            : base(getCompilerPathFunc, processExecutorFactory, baseTimeUsed, baseMemoryUsed)
        {
        }

        protected override string TargetFrameworkName => "net6.0";

        protected override string MicrosoftEntityFrameworkCoreInMemoryVersion => "6.0.1";
        protected override string MicrosoftEntityFrameworkCoreProxiesVersion => "6.0.1";
    }
}