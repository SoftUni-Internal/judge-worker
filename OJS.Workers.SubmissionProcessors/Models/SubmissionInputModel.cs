﻿namespace OJS.Workers.SubmissionProcessors.Models
{
    using OJS.Workers.Common;
    using OJS.Workers.Common.Extensions;
    using OJS.Workers.Common.Models;

    public class SubmissionInputModel<TInput> : ISubmission
    {
        private string code;

        public object Id { get; set; }

        public string AdditionalCompilerArguments { get; set; }

        public string ProcessingComment { get; set; }

        public int MemoryLimit { get; set; }

        public int TimeLimit { get; set; }

        public string Code
        {
            get => this.code
                ?? (string.IsNullOrWhiteSpace(this.AllowedFileExtensions)
                    ? this.FileContent.Decompress()
                    : null);
            set => this.code = value;
        }

        public byte[] FileContent { get; set; }

        public CompilerType CompilerType { get; set; }

        public ExecutionContextType ExecutionContextType { get; set; }

        public ExecutionStrategyType ExecutionStrategyType { get; set; }

        public string AllowedFileExtensions { get; set; }

        public TInput Input { get; set; }
    }
}