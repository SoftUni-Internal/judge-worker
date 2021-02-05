namespace OJS.Workers.SubmissionProcessors
{
    using System.Collections.Generic;

    using OJS.Workers.Common;
    using OJS.Workers.Common.Models;
    using OJS.Workers.SubmissionProcessors.Workers;

    using static OJS.Workers.Common.ExecutionStrategiesConstants.NameMappings;

    public class LocalSubmissionsFilteringService
        : ISubmissionsFilteringService
    {
        private readonly ISet<ExecutionStrategyType> localWorkerDisabledExecutionStrategyTypes = LocalWorkerUnsupportedStrategies;

        public bool CanProcessSubmission(IOjsSubmission submission, ISubmissionWorker submissionWorker)
            => submission != null
               && !this.localWorkerDisabledExecutionStrategyTypes.Contains(submission.ExecutionStrategyType);
    }
}