namespace OJS.Workers.SubmissionProcessors
{
    using OJS.Workers.Common;
    using OJS.Workers.SubmissionProcessors.Workers;

    public class LocalSubmissionsFilteringService
        : ISubmissionsFilteringService
    {
        public bool CanProcessSubmission(IOjsSubmission submission, ISubmissionWorker submissionWorker)
            => submission != null;
    }
}