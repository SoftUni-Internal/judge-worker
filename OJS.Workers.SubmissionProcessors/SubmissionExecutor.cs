namespace OJS.Workers.SubmissionProcessors
{
    using System;

    using OJS.Workers.Common;
    using OJS.Workers.SubmissionProcessors.Helpers;
    using OJS.Workers.SubmissionProcessors.Models;

    public class SubmissionExecutor : ISubmissionExecutor
    {
        private readonly int portNumber;

        public SubmissionExecutor(int portNumber) => this.portNumber = portNumber;

        public IExecutionResult<TResult> Execute<TInput, TResult>(
            IOjsSubmission submission)
            where TResult : ISingleCodeRunResult, new()
        {
            var executionStrategy = this.CreateExecutionStrategy(submission);

            var executionContext = this.CreateExecutionContext<TInput>(submission);

            return this.ExecuteSubmission<TInput, TResult>(executionStrategy, executionContext, submission);
        }

        private IExecutionStrategy CreateExecutionStrategy(IOjsSubmission submission)
        {
            try
            {
                return SubmissionProcessorHelper.CreateExecutionStrategy(
                    submission.ExecutionStrategyType,
                    this.portNumber);
            }
            catch (Exception ex)
            {
                submission.ProcessingComment = $"Exception in creating execution strategy: {ex.Message}";

                throw new Exception($"Exception in {nameof(this.CreateExecutionStrategy)}", ex);
            }
        }

        private IExecutionContext<TInput> CreateExecutionContext<TInput>(IOjsSubmission submission)
        {
            try
            {
                return SubmissionProcessorHelper.CreateExecutionContext(
                    submission as OjsSubmission<TInput>);
            }
            catch (Exception ex)
            {
                submission.ProcessingComment = $"Exception in creating execution context: {ex.Message}";

                throw new Exception($"Exception in {nameof(this.CreateExecutionContext)}", ex);
            }
        }

        private IExecutionResult<TResult> ExecuteSubmission<TInput, TResult>(
            IExecutionStrategy executionStrategy,
            IExecutionContext<TInput> executionContext,
            IOjsSubmission submission)
            where TResult : ISingleCodeRunResult, new()
        {
            try
            {
                return executionStrategy.SafeExecute<TInput, TResult>(executionContext);
            }
            catch (Exception ex)
            {
                submission.ProcessingComment = $"Exception in executing the submission: {ex.Message}";

                throw new Exception($"Exception in {nameof(this.ExecuteSubmission)}", ex);
            }
        }
    }
}