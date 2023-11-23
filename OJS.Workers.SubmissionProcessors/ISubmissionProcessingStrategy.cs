namespace OJS.Workers.SubmissionProcessors
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using log4net;
    using OJS.Workers.Common;
    using OJS.Workers.Common.Models;

    public interface ISubmissionProcessingStrategy<TSubmission>
    {
        int JobLoopWaitTimeInMilliseconds { get; }

        void Initialize(
            ILog logger,
            ConcurrentQueue<TSubmission> submissionsForProcessing,
            object sharedLockObject);

        IOjsSubmission RetrieveSubmission(List<WorkerType> workerTypes);

        void BeforeExecute();

        void ProcessExecutionResult<TResult>(IExecutionResult<TResult> executionResult)
            where TResult : ISingleCodeRunResult, new();

        void OnError(IOjsSubmission submission, Exception ex);

        void SetSubmissionToProcessing();

        void ReleaseSubmission();
    }
}
