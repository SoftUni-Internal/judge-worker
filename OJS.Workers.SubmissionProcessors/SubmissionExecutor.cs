namespace OJS.Workers.SubmissionProcessors
{
    using OJS.Workers.Common;
    using OJS.Workers.SubmissionProcessors.Helpers;
    using OJS.Workers.SubmissionProcessors.Models;

    public class SubmissionExecutor : ISubmissionExecutor
    {
        private readonly string submissionProcessorIdentifier;

        public SubmissionExecutor(string submissionProcessorIdentifier) => this.submissionProcessorIdentifier = submissionProcessorIdentifier;

        public IExecutionResult<TResult> Execute<TInput, TResult>(
            IOjsSubmission submission)
            where TResult : ISingleCodeRunResult, new()
        {
            var executionStrategy = SubmissionProcessorHelper.CreateExecutionStrategy(
                submission.ExecutionStrategyType,
                this.submissionProcessorIdentifier);

            var executionContext = SubmissionProcessorHelper.CreateExecutionContext(submission as OjsSubmission<TInput>);

            return executionStrategy.SafeExecute<TInput, TResult>(executionContext);
        }
    }
}