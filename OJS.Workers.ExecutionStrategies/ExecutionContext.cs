namespace OJS.Workers.ExecutionStrategies
{
    using OJS.Workers.Common;
    using OJS.Workers.Common.Extensions;
    using OJS.Workers.Common.Models;

    public class ExecutionContext<TInput> : IExecutionContext<TInput>
    {
        public CompilerType CompilerType { get; set; }

        public string AdditionalCompilerArguments { get; set; }

        public string Code => this.FileContent.Decompress();

        public byte[] FileContent { get; set; }

        public string AllowedFileExtensions { get; set; }

        public int TimeLimit { get; set; }

        public int MemoryLimit { get; set; }

        public TInput Input { get; set; }
    }
}