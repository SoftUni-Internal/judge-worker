﻿namespace OJS.Workers.SubmissionProcessors.Models
{
    using System;
    using OJS.Workers.Common;
    using OJS.Workers.Common.Extensions;
    using OJS.Workers.Common.Models;

    public class OjsSubmission<TInput> : IOjsSubmission
    {
        private string code;

        public object Id { get; set; }

        public string AdditionalCompilerArguments { get; set; }

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

        public byte[] AdditionalFiles { get; set; }

        public CompilerType CompilerType { get; set; }

        public ExecutionType ExecutionType { get; set; }

        public ExecutionStrategyType ExecutionStrategyType { get; set; }

        public string AllowedFileExtensions { get; set; }

        public TInput Input { get; set; }

        public int MaxPoints { get; set; }

        public ExceptionType ExceptionType { get; set; }

        public DateTime? StartedExecutionOn { get; set; }

        public DateTime? CompletedExecutionOn { get; set; }

        public string WorkerEndpoint { get; set; }
    }
}