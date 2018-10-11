namespace OJS.Workers.SubmissionProcessors
{
    using System.Collections.Concurrent;

    using log4net;

    using OJS.Workers.Common;
    using OJS.Workers.Common.Models;
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

        public abstract void BeforeExecute();

        public abstract SubmissionModel RetrieveSubmission();

        public abstract void ProcessExecutionResult<TResult>(IExecutionResult<TResult> executionResult)
            where TResult : ISingleCodeRunResult, new();

        public abstract void OnError(SubmissionModel submission);

        public virtual IExecutionContext CreateExecutionContext(SubmissionModel submission)
        {
            if (submission.ExecutionContextType == ExecutionContextType.NonCompetitive)
            {
                return new NonCompetitiveExecutionContext
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

            return new CompetitiveExecutionContext
            {
                SubmissionId = submission.Id,
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
        }
    }
}