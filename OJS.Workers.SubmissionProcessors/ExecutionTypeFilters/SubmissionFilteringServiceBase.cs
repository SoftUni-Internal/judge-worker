namespace OJS.Workers.SubmissionProcessors.ExecutionTypeFilters
{
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

        public bool CanProcessSubmission(IOjsSubmission submission, ISubmissionWorker submissionWorker)
        {
            if (submission == null)
            {
                return false;
            }

            var isDisabledStrategy = this.IsDisabledStrategy(submission);
            var isEnabledStrategy = this.IsDisabledStrategy(submission);
            var canProcessSubmissionInternal = this.CanProcessSubmissionInternal(submission, submissionWorker);
            var isDisabledCompilerType = this.IsDisabledCompilerType(submission);

            var canProcessSubmission = !isDisabledStrategy
                   && isEnabledStrategy
                   && canProcessSubmissionInternal
                   && !isDisabledCompilerType;

            if (canProcessSubmission)
            {
                return true;
            }

            var reason = string.Empty;

            if (isDisabledStrategy)
            {
                reason = "Strategy is disabled.";
            }

            if (!isEnabledStrategy)
            {
                reason = "Strategy is not enabled.";
            }

            if (!canProcessSubmissionInternal)
            {
                reason = "Cannot be processed by the worker.";
            }

            if (isDisabledCompilerType)
            {
                reason = "Compiler type is disabled.";
            }

            this.logger.Error($"Submission with Id: {submission.Id}, cannot be processed. Reason: {reason} ");

            return false;
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
