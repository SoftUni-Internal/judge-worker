namespace OJS.Workers.ExecutionStrategies.CSharp
{
    using System;

    using Microsoft.Build.Evaluation;

    using OJS.Workers.Common.Models;
    using OJS.Workers.ExecutionStrategies.Extensions;
    using OJS.Workers.Executors;

    public class CSharpAspProjectTestsExecutionStrategy : CSharpProjectTestsExecutionStrategy
    {
        protected const string MoqAssemblyReference =
            "Moq, Version=4.7.8.0, Culture=neutral, PublicKeyToken=69f491c39445e920, processorArchitecture=MSIL";

        protected const string CastleCoreAssemblyReference =
            @"Castle.Core, Version=4.0.0.0, Culture=neutral, PublicKeyToken=407dd0808d44fbdc";

        public CSharpAspProjectTestsExecutionStrategy(
            Func<CompilerType, string> getCompilerPathFunc,
            IProcessExecutorFactory processExecutorFactory,
            string nUnitConsoleRunnerPath,
            int baseTimeUsed,
            int baseMemoryUsed)
            : base(getCompilerPathFunc, processExecutorFactory, nUnitConsoleRunnerPath, baseTimeUsed, baseMemoryUsed)
        {
        }

        protected override void CorrectProjectReferences(Project project)
        {
            project.AddReferences(MoqAssemblyReference, CastleCoreAssemblyReference);

            base.CorrectProjectReferences(project);

            project.RemoveVsToolsImport();
        }
    }
}
