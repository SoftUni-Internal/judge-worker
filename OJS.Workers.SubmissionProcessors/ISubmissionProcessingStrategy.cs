namespace OJS.Workers.SubmissionProcessors
{
    using System.Collections.Concurrent;

    using log4net;

    using OJS.Workers.Common;
    using OJS.Workers.SubmissionProcessors.Models;

    public interface ISubmissionProcessingStrategy<TSubmission>
    {
        int JobLoopWaitTimeInMilliseconds { get; }

        void Initialize(
            ILog logger,
            ConcurrentQueue<TSubmission> submissionsForProcessing,
            object sharedLockObject);

        SubmissionModel RetrieveSubmission();

        void BeforeExecute();

        void ProcessExecutionResult<TResult>(IExecutionResult<TResult> executionResult)
            where TResult : ISingleCodeRunResult, new();

        void OnError(SubmissionModel submission);

        IExecutionContext CreateExecutionContext(SubmissionModel submissionModel);
    }
}