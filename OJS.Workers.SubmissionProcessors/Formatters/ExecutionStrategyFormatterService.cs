namespace OJS.Workers.SubmissionProcessors.Formatters
{
    using System.Collections.Generic;

    using OJS.Workers.Common.Extensions;
    using OJS.Workers.Common.Models;

    public class ExecutionStrategyFormatterService
        : IExecutionStrategyFormatterService
    {
        private readonly IDictionary<ExecutionStrategyType, string> map;

        public ExecutionStrategyFormatterService()
            => this.map = new Dictionary<ExecutionStrategyType, string>()
            {
                { ExecutionStrategyType.DotNetCoreCompileExecuteAndCheck, "csharp-dot-net-core-code" },
            };

        public string Format(ExecutionStrategyType obj)
            => this.map.ContainsKey(obj)
                ? this.map[obj]
                : obj.ToString().ToHyphenSeparatedWords();
    }
}