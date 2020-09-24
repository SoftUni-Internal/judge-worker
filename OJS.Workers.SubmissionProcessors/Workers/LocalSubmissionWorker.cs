namespace OJS.Workers.SubmissionProcessors.Workers
{
    using OJS.Workers.Common;
    using OJS.Workers.SubmissionProcessors.Models;

    public class LocalSubmissionWorker
        : ISubmissionWorker
    {
        private readonly int portNumber;

        public LocalSubmissionWorker(int portNumber)
            => this.portNumber = portNumber;

        public string Location
            => this.portNumber.ToString();

        public IExecutionResult<TResult> RunSubmission<TInput, TResult>(OjsSubmission<TInput> submission)
            where TResult : class, ISingleCodeRunResult, new()
            => new SubmissionExecutor(this.portNumber)
                .Execute<TInput, TResult>(submission);
    }
}
