namespace OJS.Workers.SubmissionProcessors
{
    using System.Collections.Concurrent;

    using log4net;

    using OJS.Workers.Common;

    public interface ISubmissionProcessingStrategy<TSubmission>
    {
        int JobLoopWaitTimeInMilliseconds { get; }

        void Initialize(
            ILog logger,
            ConcurrentQueue<TSubmission> submissionsForProcessing,
            object sharedLockObject);

        ISubmission RetrieveSubmission();

        void BeforeExecute();

        void ProcessExecutionResult<TResult>(IExecutionResult<TResult> executionResult)
            where TResult : ISingleCodeRunResult, new();

        void OnError(ISubmission submission);
    }
}