namespace OJS.Workers.SubmissionProcessors.ExecutionTypeFilters
{
    using System.Collections.Generic;
    using OJS.Workers.Common;
    using OJS.Workers.Common.Models;
    using OJS.Workers.SubmissionProcessors.Workers;

    public abstract class SubmissionFilteringServiceBase
            : ISubmissionsFilteringService
    {
        protected abstract ISet<ExecutionStrategyType> EnabledExecutionStrategyTypes { get; }

        protected abstract ISet<ExecutionStrategyType> DisabledExecutionStrategyTypes { get; }

        public bool CanProcessSubmission(IOjsSubmission submission, ISubmissionWorker submissionWorker)
            => submission != null
                && !this.DisabledExecutionStrategyTypes.Contains(submission.ExecutionStrategyType)
                && this.EnabledExecutionStrategyTypes.Contains(submission.ExecutionStrategyType)
                && this.CanProcessSubmissionInternal(submission, submissionWorker);

        protected virtual bool CanProcessSubmissionInternal(IOjsSubmission submission, ISubmissionWorker submissionWorker)
            => true;
    }
}
