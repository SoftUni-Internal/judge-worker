namespace OJS.Workers.SubmissionProcessors.Models
{
    using OJS.Workers.Common;
    using OJS.Workers.Common.Extensions;
    using OJS.Workers.Common.Models;

    public class SubmissionInputModel<TInput> : ISubmission
    {
        private byte[] fileContent;
        private string code;

        public object Id { get; set; }

        public string AdditionalCompilerArguments { get; set; }

        public string ProcessingComment { get; set; }

        public int MemoryLimit { get; set; }

        public int TimeLimit { get; set; }

        public string Code
        {
            get => this.code ?? this.FileContent.Decompress();
            set => this.code = value;
        }

        public byte[] FileContent
        {
            get => this.fileContent ?? this.Code.Compress();
            set => this.fileContent = value;
        }

        public CompilerType CompilerType { get; set; }

        public ExecutionContextType ExecutionContextType { get; set; }

        public ExecutionStrategyType ExecutionStrategyType { get; set; }

        public string AllowedFileExtensions { get; set; }

        public TInput Input { get; set; }
    }
}