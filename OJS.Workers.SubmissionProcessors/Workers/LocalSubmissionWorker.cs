namespace OJS.Workers.SubmissionProcessors.Workers
{
    using OJS.Workers.Common;
    using OJS.Workers.SubmissionProcessors.Models;

    public class LocalSubmissionWorker
        : ISubmissionWorker
    {
        public string Location => string.Empty;

        public IExecutionResult<TResult> RunSubmission<TInput, TResult>(OjsSubmission<TInput> submission)
            where TResult : class, ISingleCodeRunResult, new()
            => new SubmissionExecutor().Execute<TInput, TResult>(submission);
    }
}
