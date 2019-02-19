namespace OJS.Workers.ExecutionStrategies.Models
{
    using System.Collections.Generic;

    using OJS.Workers.Common;

    public class ExecutionResult<TResult> : IExecutionResult<TResult>
        where TResult : ISingleCodeRunResult, new()
    {
        public bool IsCompiledSuccessfully { get; set; }

        public string CompilerComment { get; set; }

        public ICollection<TResult> Results { get; set; } = new List<TResult>();
    }
}
