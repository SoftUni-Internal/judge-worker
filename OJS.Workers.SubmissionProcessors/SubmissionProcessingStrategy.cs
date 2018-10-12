namespace OJS.Workers.SubmissionProcessors
{
    using System;
    using System.Collections.Concurrent;

    using log4net;

    using OJS.Workers.Common;
    using OJS.Workers.ExecutionStrategies.Models;
    using OJS.Workers.SubmissionProcessors.Models;

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

        public virtual IExecutionContext CreateExecutionContext(ISubmission submission)
        {
            switch (submission)
            {
                case SubmissionWithTests submissionWithTests:
                    return CreateCompetitiveExecutionContext(submissionWithTests);
                case SubmissionWithInputs rawSubmission:
                    return CreateNonCompetitiveExecutionContext(rawSubmission);
                default:
                    throw new ArgumentException("Invalid submission", nameof(submission));
            }
        }

        public void ProcessExecutionResult<TResult>(IExecutionResult<TResult> executionResult)
            where TResult : ISingleCodeRunResult, new()
        {
            switch (executionResult)
            {
                case IExecutionResult<TestResult> testsExecutionResult:
                    this.ProcessTestsExecutionResult(testsExecutionResult);
                    break;
                case IExecutionResult<RawResult> rawExecutionResult:
                    this.ProcessRawExecutionResult(rawExecutionResult);
                    break;
                default:
                    throw new ArgumentException("Invalid execution result", nameof(executionResult));
            }
        }

        public abstract void BeforeExecute();

        public abstract ISubmission RetrieveSubmission();

        public abstract void OnError(ISubmission submission);

        protected abstract void ProcessTestsExecutionResult(IExecutionResult<TestResult> testsExecutionResult);

        protected abstract void ProcessRawExecutionResult(IExecutionResult<RawResult> rawExecutionResult);

        private static IExecutionContext CreateCompetitiveExecutionContext(SubmissionWithTests submission) =>
            new CompetitiveExecutionContext
            {
                AdditionalCompilerArguments = submission.AdditionalCompilerArguments,
                CheckerAssemblyName = submission.CheckerAssemblyName,
                CheckerParameter = submission.CheckerParameter,
                CheckerTypeName = submission.CheckerTypeName,
                FileContent = submission.FileContent,
                AllowedFileExtensions = submission.AllowedFileExtensions,
                CompilerType = submission.CompilerType,
                MemoryLimit = submission.MemoryLimit,
                TimeLimit = submission.TimeLimit,
                TaskSkeleton = submission.TaskSkeleton,
                Tests = submission.Tests
            };


        private static IExecutionContext CreateNonCompetitiveExecutionContext(SubmissionWithInputs submission) =>
            new NonCompetitiveExecutionContext
            {
                AdditionalCompilerArguments = submission.AdditionalCompilerArguments,
                FileContent = submission.FileContent,
                AllowedFileExtensions = submission.AllowedFileExtensions,
                CompilerType = submission.CompilerType,
                MemoryLimit = submission.MemoryLimit,
                TimeLimit = submission.TimeLimit,
                Tests = submission.Inputs
            };
    }
}