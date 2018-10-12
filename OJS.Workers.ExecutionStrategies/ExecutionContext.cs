namespace OJS.Workers.ExecutionStrategies
{
    using System.Collections.Generic;

    using OJS.Workers.Common;
    using OJS.Workers.Common.Extensions;
    using OJS.Workers.Common.Models;

    public abstract class ExecutionContext<TInput> : IExecutionContext
    {
        public CompilerType CompilerType { get; set; }

        public string AdditionalCompilerArguments { get; set; }

        public string Code => this.FileContent.Decompress();

        public byte[] FileContent { get; set; }

        public string AllowedFileExtensions { get; set; }

        public abstract IEnumerable<TInput> Tests { get; set; }

        public int TimeLimit { get; set; }

        public int MemoryLimit { get; set; }
    }
}