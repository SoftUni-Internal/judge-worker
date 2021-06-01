namespace OJS.Workers.SubmissionProcessors.ExecutionTypeFilters
{
    using System;
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
                && !this.IsDisabledStrategy(submission)
                && this.IsEnabledStrategy(submission)
                && this.CanProcessSubmissionInternal(submission, submissionWorker);

        private bool IsDisabledStrategy(IOjsSubmission submission)
            => this.DisabledExecutionStrategyTypes.Count > 0
                 && this.DisabledExecutionStrategyTypes.Contains(submission.ExecutionStrategyType);

        private bool IsEnabledStrategy(IOjsSubmission submission)
        => this.EnabledExecutionStrategyTypes.Count == 0
                || this.EnabledExecutionStrategyTypes.Contains(submission.ExecutionStrategyType);

        protected virtual bool CanProcessSubmissionInternal(IOjsSubmission submission, ISubmissionWorker submissionWorker)
            => true;
    }
}
