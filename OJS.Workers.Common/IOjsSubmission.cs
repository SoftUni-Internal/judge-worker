﻿namespace OJS.Workers.Common
{
    using System;
    using OJS.Workers.Common.Models;

    public interface IOjsSubmission
    {
        object Id { get; }

        string AdditionalCompilerArguments { get; }

        int MemoryLimit { get; }

        int TimeLimit { get; }

        string Code { get; }

        byte[] FileContent { get; }

        string AllowedFileExtensions { get; }

        CompilerType CompilerType { get; }

        ExecutionType ExecutionType { get; }

        ExecutionStrategyType ExecutionStrategyType { get; }

        ExceptionType ExceptionType { get; set; }

        DateTime? StartedExecutionOn { get; set; }

        DateTime? CompletedExecutionOn { get; set; }

        string WorkerEndpoint { get; set; }
    }
}