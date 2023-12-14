namespace OJS.Workers.SubmissionProcessors.ExecutionTypeFilters
{
    using OJS.Workers.Common;
    using OJS.Workers.Common.Models;
    using OJS.Workers.SubmissionProcessors.Workers;

    public interface ISubmissionsFilteringService
    {
        WorkerStateForSubmission GetWorkerStateForSubmission(IOjsSubmission submission, ISubmissionWorker submissionWorker);
    }
}
