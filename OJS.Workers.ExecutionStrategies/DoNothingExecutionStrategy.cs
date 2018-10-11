namespace OJS.Workers.ExecutionStrategies
{
    using System.Collections.Generic;

    using OJS.Workers.Common;
    using OJS.Workers.Common.Helpers;
    using OJS.Workers.ExecutionStrategies.Models;

    public class DoNothingExecutionStrategy : IExecutionStrategy
    {
        protected string WorkingDirectory { get; set; }

        public ExecutionResult SafeExecute(IExecutionContext executionContext)
        {
            this.WorkingDirectory = DirectoryHelpers.CreateTempDirectoryForExecutionStrategy();
            try
            {
                return this.Execute(executionContext);
            }
            finally
            {
                DirectoryHelpers.SafeDeleteDirectory(this.WorkingDirectory, true);
            }
        }

        public ExecutionResult Execute(IExecutionContext executionContext) =>
            new ExecutionResult
            {
                CompilerComment = null,
                IsCompiledSuccessfully = true,
                TestResults = new List<TestResult>()
            };
    }
}
