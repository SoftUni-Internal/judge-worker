﻿namespace OJS.Workers.SubmissionProcessors
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

        IOjsSubmission RetrieveSubmission();

        void BeforeExecute();

        void ProcessExecutionResult<TResult>(IExecutionResult<TResult> executionResult)
            where TResult : ISingleCodeRunResult, new();

        void OnError(IOjsSubmission submission);

        void SetSubmissionWorker(string worker);

        void SetSubmissionToProcessing();

        void ReleaseSubmission();
    }
}
