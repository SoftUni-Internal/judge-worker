namespace OJS.Workers.SubmissionProcessors.Models
{
    using System;

    public class RemoteSubmissionResult
    {
        public ExceptionModel Exception { get; set; }

        public ExecutionResultResponseModel ExecutionResult { get; set; }

        public DateTime? StartedExecutionOn { get; set; }

        public DateTime? CompletedExecutionOn { get; set; }
    }
}