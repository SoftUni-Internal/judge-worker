namespace OJS.Workers.SubmissionProcessors
{
    using System;
    using System.Collections.Concurrent;

    using log4net;

    using OJS.Workers.Common;
    using OJS.Workers.ExecutionStrategies;
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

        public virtual IExecutionContext<TInput> CreateExecutionContext<TInput>(ISubmission submission)
        {
            switch (submission)
            {
                case SubmissionWithTests submissionWithTests:
                    return (IExecutionContext<TInput>)CreateCompetitiveExecutionContext(submissionWithTests);
                case SubmissionWithInput rawSubmission:
                    return (IExecutionContext<TInput>)CreateNonCompetitiveExecutionContext(rawSubmission);
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

        private static IExecutionContext<TestsInputModel> CreateCompetitiveExecutionContext(SubmissionWithTests submission) =>
            new ExecutionContext<TestsInputModel>
            {
                AdditionalCompilerArguments = submission.AdditionalCompilerArguments,
                
                FileContent = submission.FileContent,
                AllowedFileExtensions = submission.AllowedFileExtensions,
                CompilerType = submission.CompilerType,
                MemoryLimit = submission.MemoryLimit,
                TimeLimit = submission.TimeLimit,              
                Input = new TestsInputModel
                {
                    CheckerAssemblyName = submission.CheckerAssemblyName,
                    CheckerParameter = submission.CheckerParameter,
                    CheckerTypeName = submission.CheckerTypeName,
                    TaskSkeleton = submission.TaskSkeleton,
                    Tests = submission.Tests
                }
            };


        private static IExecutionContext<string> CreateNonCompetitiveExecutionContext(SubmissionWithInput submission) =>
            new ExecutionContext<string>
            {
                AdditionalCompilerArguments = submission.AdditionalCompilerArguments,
                FileContent = submission.FileContent,
                AllowedFileExtensions = submission.AllowedFileExtensions,
                CompilerType = submission.CompilerType,
                MemoryLimit = submission.MemoryLimit,
                TimeLimit = submission.TimeLimit,
                Input = submission.Input
            };
    }
}