namespace OJS.Workers.SubmissionProcessors.Workers
{
    using System.Collections.Generic;

    using OJS.Workers.Common;
    using OJS.Workers.Common.Models;
    using OJS.Workers.ExecutionStrategies.Models;
    using OJS.Workers.SubmissionProcessors.Common;
    using OJS.Workers.SubmissionProcessors.Formatters;
    using OJS.Workers.SubmissionProcessors.Models;

    public class RemoteWorker
    {
        private readonly IFormatterServiceFactory formatterServicesFactory;
        private readonly string endpoint;
        private readonly HttpService http;

        public RemoteWorker(string endpointRoot, IFormatterServiceFactory formatterServicesFactory)
        {
            this.formatterServicesFactory = formatterServicesFactory;
            this.endpoint = $"{endpointRoot}/executeSubmission";
            this.http = new HttpService();
        }

        public IExecutionResult<TResult> RunSubmission<TResult>(OjsSubmission<TestsInputModel> submission) where TResult
            : ISingleCodeRunResult, new()
        {
            var submissionRequestBody = new
            {
                ExecutionType = this.formatterServicesFactory.Get<ExecutionType>()
                    .Format(submission.ExecutionType),
                ExecutionStrategy = this.formatterServicesFactory.Get<ExecutionStrategyType>()
                    .Format(submission.ExecutionStrategyType),
                FileContents = submission.FileContent,
                Code = submission.Code,
                TimeLimit = submission.TimeLimit,
                MemoryLimit = submission.MemoryLimit,
                ExecutionDetails = new
                {
                    MaxPoints = 100,
                    CheckerType = this.formatterServicesFactory.Get<string>()
                        .Format(submission.Input.CheckerTypeName),
                    Tests = submission.Input.Tests,
                },
            };

            var result = this.http.PostJson<object, RemoteSubmissionResult>(this.endpoint, submissionRequestBody);
            return new ExecutionResult<TResult>
            {
                CompilerComment = result.ExecutionResult.CompilerComment,
                IsCompiledSuccessfully = result.ExecutionResult.IsCompiledSuccessfully,
                Results = new List<TResult>(),
            };
        }

        public override bool Equals(object obj)
        {
            var other = obj as RemoteWorker;
            return obj != null && this.Equals(other);
        }

        public bool Equals(RemoteWorker other)
            => this.endpoint.Equals(other.endpoint);

        public override int GetHashCode()
            => this.endpoint.GetHashCode();
    }
}
