namespace OJS.Workers.SubmissionProcessors.Workers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

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

        public IExecutionResult<TResult> RunSubmission<TResult>(OjsSubmission<TestsInputModel> submission)
            where TResult : class, ISingleCodeRunResult, new()
        {
            var submissionRequestBody = this.BuildRequestBody(submission);

            var result = this.http.PostJson<object, RemoteSubmissionResult>(this.endpoint, submissionRequestBody);
            if (result.Exception != null)
            {
                throw new Exception(result.Exception.Message);
            }

            var executionResult = new ExecutionResult<TResult>
            {
                CompilerComment = result.ExecutionResult.CompilerComment,
                IsCompiledSuccessfully = result.ExecutionResult.IsCompiledSuccessfully,
                Results = result.ExecutionResult.TaskResult.TestResults
                    .Select(testResult =>
                    {
                        var test = submission.Input.Tests.FirstOrDefault(t => t.Id == testResult.Id);
                        return this.BuildTestResult<TResult>(test, testResult);
                    })
                    .ToList(),
            };

            return executionResult;
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

        private object BuildRequestBody(OjsSubmission<TestsInputModel> submission)
            => new
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

        private TResult BuildTestResult<TResult>(TestContext test, TestResultResponseModel testResult)
            where TResult : class
        {
            Enum.TryParse(testResult.ResultType, out TestRunResultType resultType);

            var result = new TestResult
            {
                Input = test.Input,
                IsTrialTest = test.IsTrialTest,
                ExecutionComment = testResult.ExecutionComment,
                MemoryUsed = 0,
                TimeUsed = 0,
                ResultType = resultType,
                Id = test.Id,
                CheckerDetails = new CheckerDetails
                {
                    Comment = testResult.ExecutionComment,
                    UserOutputFragment = testResult.Output,
                    ExpectedOutputFragment = test.Output,
                },
            };

            return result as TResult;
        }
    }
}
