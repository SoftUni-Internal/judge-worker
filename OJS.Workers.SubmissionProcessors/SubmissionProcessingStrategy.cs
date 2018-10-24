namespace OJS.Workers.SubmissionProcessors
{
    using System;
    using System.Collections.Concurrent;

    using log4net;

    using OJS.Workers.Common;
    using OJS.Workers.ExecutionStrategies.Models;

    public abstract class SubmissionProcessingStrategy<TSubmission> : ISubmissionProcessingStrategy<TSubmission>
    {
        protected ILog Logger { get; private set; }

        protected ConcurrentQueue<TSubmission> SubmissionsForProcessing { get; private set; }

        protected object SharedLockObject { get; private set; }

        public int JobLoopWaitTimeInMilliseconds { get; protected set; } =
            Constants.DefaultJobLoopWaitTimeInMilliseconds;

        public virtual void Initialize(
            ILog logger,
            ConcurrentQueue<TSubmission> submissionsForProcessing,
            object sharedLockObject)
        {
            this.Logger = logger;
            this.SubmissionsForProcessing = submissionsForProcessing;
            this.SharedLockObject = sharedLockObject;
        }

        public void ProcessExecutionResult<TResult>(IExecutionResult<TResult> executionResult)
            where TResult : ISingleCodeRunResult, new()
        {
            switch (executionResult)
            {
                case IExecutionResult<TestResult> testsExecutionResult:
                    this.ProcessTestsExecutionResult(testsExecutionResult);
                    break;
                case IExecutionResult<OutputResult> outputExecutionResult:
                    this.ProcessOutputExecutionResult(outputExecutionResult);
                    break;
                default:
                    throw new ArgumentException("Invalid execution result", nameof(executionResult));
            }
        }

        public abstract void BeforeExecute();

        public abstract ISubmission RetrieveSubmission();

        public abstract void OnError(ISubmission submission);

        protected abstract void ProcessTestsExecutionResult(IExecutionResult<TestResult> testsExecutionResult);

        protected abstract void ProcessOutputExecutionResult(IExecutionResult<OutputResult> outputExecutionResult);
    }
}