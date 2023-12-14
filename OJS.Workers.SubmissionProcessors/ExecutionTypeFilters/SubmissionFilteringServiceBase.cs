namespace OJS.Workers.SubmissionProcessors.ExecutionTypeFilters
{
    using System;
    using System.Collections.Generic;
    using log4net;
    using OJS.Workers.Common;
    using OJS.Workers.Common.Models;
    using OJS.Workers.SubmissionProcessors.Workers;

    public abstract class SubmissionFilteringServiceBase
            : ISubmissionsFilteringService
    {
        private readonly ILog logger;

        protected SubmissionFilteringServiceBase()
        {
            this.logger = LogManager.GetLogger(typeof(SubmissionFilteringServiceBase));
        }

        protected abstract ISet<ExecutionStrategyType> EnabledExecutionStrategyTypes { get; }

        protected abstract ISet<ExecutionStrategyType> DisabledExecutionStrategyTypes { get; }

        protected abstract ISet<CompilerType> DisabledExecuteAndCompileCompilerTypes { get; }

        public WorkerStateForSubmission GetWorkerStateForSubmission(IOjsSubmission submission, ISubmissionWorker submissionWorker)
        {
            var isDisabledStrategy = this.IsDisabledStrategy(submission);
            var isEnabledStrategy = this.IsEnabledStrategy(submission);
            var isDisabledCompilerType = this.IsDisabledCompilerType(submission);
            var canProcessSubmissionInternal = this.CanProcessSubmissionInternal(submission, submissionWorker);

            if (isDisabledStrategy)
            {
                return WorkerStateForSubmission.DisabledStrategy;
            }

            if (!isEnabledStrategy)
            {
                return WorkerStateForSubmission.NotEnabledStrategy;
            }

            if (isDisabledCompilerType)
            {
                return WorkerStateForSubmission.DisabledCompilerType;
            }

            if (!canProcessSubmissionInternal)
            {
                return WorkerStateForSubmission.Unhealthy;
            }

            return WorkerStateForSubmission.Ready;
        }

        protected virtual bool CanProcessSubmissionInternal(IOjsSubmission submission, ISubmissionWorker submissionWorker)
            => true;

        private bool IsDisabledStrategy(IOjsSubmission submission)
            => this.DisabledExecutionStrategyTypes.Count > 0
                 && this.DisabledExecutionStrategyTypes.Contains(submission.ExecutionStrategyType);

        private bool IsEnabledStrategy(IOjsSubmission submission)
        => this.EnabledExecutionStrategyTypes.Count == 0
                || this.EnabledExecutionStrategyTypes.Contains(submission.ExecutionStrategyType);

        private bool IsDisabledCompilerType(IOjsSubmission submission)
            => submission.ExecutionStrategyType is ExecutionStrategyType.CompileExecuteAndCheck &&
               this.DisabledExecuteAndCompileCompilerTypes.Count > 0 &&
               this.DisabledExecuteAndCompileCompilerTypes.Contains(submission.CompilerType);
    }
}
