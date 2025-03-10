﻿namespace OJS.Workers.SubmissionProcessors.Workers
{
    using System;
    using System.Linq;

    using log4net;

    using OJS.Workers.Common;
    using OJS.Workers.Common.Models;
    using OJS.Workers.ExecutionStrategies.Models;
    using OJS.Workers.SubmissionProcessors.Common;
    using OJS.Workers.SubmissionProcessors.Formatters;
    using OJS.Workers.SubmissionProcessors.Models;

    public class RemoteSubmissionsWorker
    : ISubmissionWorker
    {
        private readonly IFormatterServiceFactory formatterServicesFactory;
        private readonly ILog logger;
        private readonly HttpService http;
        private readonly string endpoint;

        public RemoteSubmissionsWorker(
            string endpointRoot,
            IFormatterServiceFactory formatterServicesFactory,
            ILog logger)
        {
            this.formatterServicesFactory = formatterServicesFactory;
            this.logger = logger;
            this.Location = endpointRoot;
            this.endpoint = $"{endpointRoot}/executeSubmission";
            this.http = new HttpService(Settings.HttpClientTimeoutForRemoteWorkersInSeconds);
        }

        public string Location { get; }

        public IExecutionResult<TResult> RunSubmission<TInput, TResult>(OjsSubmission<TInput> submission)
            where TResult : class, ISingleCodeRunResult, new()
        {
            this.logger.Info($"Preparing submission #{submission.Id} for remote worker {this.Location}");

            var testInputSubmission = submission as OjsSubmission<TestsInputModel>;
            var submissionRequestBody = this.BuildRequestBody(testInputSubmission);

            this.logger.Info($"Request body for submission {submission.Id} is ready.");

            var result = this.ExecuteSubmissionRemotely(testInputSubmission, submissionRequestBody);

            var executionResult = new ExecutionResult<TResult>();
            executionResult.StartedExecutionOn = result.StartedExecutionOn;
            executionResult.CompletedExecutionOn = result.CompletedExecutionOn;
            executionResult.WorkerEndpoint = result.WorkerEndpoint;

            if (result == null)
            {
                this.logger.Error($"RSP {this.Location} returned null result for submission {submission.Id}.");
                return executionResult;
            }

            if (result.Exception != null)
            {
                this.logger.Error(
                    $"RSP {this.Location} returned {nameof(result.Exception)} for #{submission.Id} with message: " +
                    $"{result.Exception.Message} and stack trace: {result.Exception.StackTrace}");
                executionResult.CompilerComment = result.Exception.Message;

                return executionResult;
            }

            if (result.ExecutionResult == null)
            {
                this.logger.Error(
                    $"RSP {this.Location} returned null {nameof(result.ExecutionResult)} for #{submission.Id}.");
                return executionResult;
            }

            this.logger.Info(
                $"RSP {this.Location} successfully returned {nameof(result.ExecutionResult)} for #{submission.Id}.");

            executionResult.CompilerComment = result.ExecutionResult.CompilerComment;
            executionResult.IsCompiledSuccessfully = result.ExecutionResult.IsCompiledSuccessfully;
            executionResult.Results = result.ExecutionResult.TaskResult.TestResults
                .Select(testResult =>
                {
                    var test = testInputSubmission.Input.Tests.FirstOrDefault(t => t.Id == testResult.Id);
                    return this.BuildTestResult<TResult>(test, testResult);
                })
                .ToList();

            return executionResult;
        }

        private object BuildRequestBody(OjsSubmission<TestsInputModel> submission) =>
            new
            {
                ExecutionType = this.formatterServicesFactory.Get<ExecutionType>()
                        .Format(submission.ExecutionType),
                ExecutionStrategy = this.formatterServicesFactory.Get<ExecutionStrategyType>()
                        .Format(submission.ExecutionStrategyType),
                CompilerType = submission.CompilerType.ToString(),
                FileContent = string.IsNullOrEmpty(submission.Code)
                        ? submission.FileContent
                        : null,
                submission.AdditionalFiles,
                Code = submission.Code ?? string.Empty,
                submission.Id,
                submission.TimeLimit,
                submission.MemoryLimit,
                ExecutionDetails = new
                {
                    submission.MaxPoints,
                    CheckerType = this.formatterServicesFactory.Get<string>()
                            .Format(submission.Input.CheckerTypeName),
                    submission.Input.CheckerParameter,
                    submission.Input.Tests,
                    submission.Input.TaskSkeleton,
                    submission.Input.TaskSkeletonAsString,
                },
                ExecutionOptions = new
                {
                    KeepDetails = true,
                    EscapeTests = false,
                    EscapeLineEndings = true,
                },
                WithExceptionStackTrace = true,
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
                MemoryUsed = testResult.MemoryUsed,
                TimeUsed = testResult.TimeUsed,
                ResultType = resultType,
                Id = test.Id,
                CheckerDetails = new CheckerDetails
                {
                    Comment = testResult.CheckerDetails.Comment,
                    UserOutputFragment = testResult.CheckerDetails.UserOutputFragment,
                    ExpectedOutputFragment = testResult.CheckerDetails.ExpectedOutputFragment,
                },
            };

            return result as TResult;
        }

        private RemoteSubmissionResult ExecuteSubmissionRemotely(
            IOjsSubmission submission,
            object submissionRequestBody)
        {
            RemoteSubmissionResult result;

            try
            {
                result = this.http.PostJson<object, RemoteSubmissionResult>(this.endpoint, submissionRequestBody);
            }
            catch (Exception ex)
            {
                submission.ExceptionType = ExceptionType.Remote;
                submission.WorkerEndpoint = this.Location;
                throw new Exception($"Exception in {nameof(this.ExecuteSubmissionRemotely)}", ex);
            }

            if (result.Exception == null)
            {
                result.WorkerEndpoint = this.Location;
                return result;
            }

            submission.StartedExecutionOn = result.StartedExecutionOn;
            submission.CompletedExecutionOn = result.CompletedExecutionOn;
            submission.ExceptionType = result.Exception.ExceptionType ?? ExceptionType.None;
            submission.WorkerEndpoint = this.Location;
            throw new Exception(
                result.Exception.Message,
                new Exception($"Remote worker caught an unexpected error:" +
                $"{Environment.NewLine}{result.Exception.StackTrace}"));
        }
    }
}
