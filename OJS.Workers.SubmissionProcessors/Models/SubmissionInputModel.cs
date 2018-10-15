namespace OJS.Workers.SubmissionProcessors.Models
{
    using OJS.Workers.Common;
    using OJS.Workers.Common.Models;

    public class SubmissionInputModel<TInput> : ISubmission
    {
        public object Id { get; set; }

        public string AdditionalCompilerArguments { get; set; }

        public string ProcessingComment { get; set; }

        public int MemoryLimit { get; set; }

        public int TimeLimit { get; set; }

        public byte[] FileContent { get; set; }

        public CompilerType CompilerType { get; set; }

        public ExecutionContextType ExecutionContextType { get; set; }

        public ExecutionStrategyType ExecutionStrategyType { get; set; }

        public string AllowedFileExtensions { get; set; }

        public TInput Input { get; set; }
    }
}