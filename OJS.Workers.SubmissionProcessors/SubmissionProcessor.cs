namespace OJS.Workers.SubmissionProcessors
{
    using System;
    using System.Collections.Concurrent;
    using System.Threading;

    using log4net;

    using OJS.Workers.Common;
    using OJS.Workers.Common.Models;
    using OJS.Workers.ExecutionStrategies.Models;
    using OJS.Workers.SubmissionProcessors.Models;

    public class SubmissionProcessor<TSubmission> : ISubmissionProcessor
    {
        private readonly object sharedLockObject;
        private readonly ILog logger;
        private readonly IDependencyContainer dependencyContainer;
        private readonly ConcurrentQueue<TSubmission> submissionsForProcessing;
        private readonly int portNumber;

        private ISubmissionProcessingStrategy<TSubmission> submissionProcessingStrategy;
        private bool stopping;

        public SubmissionProcessor(
            string name,
            IDependencyContainer dependencyContainer,
            ConcurrentQueue<TSubmission> submissionsForProcessing,
            int portNumber,
            object sharedLockObject)
        {
            this.Name = name;

            this.logger = LogManager.GetLogger(name);
            this.logger.Info($"{nameof(SubmissionProcessor<TSubmission>)} initializing...");

            this.stopping = false;

            this.dependencyContainer = dependencyContainer;
            this.submissionsForProcessing = submissionsForProcessing;
            this.portNumber = portNumber;
            this.sharedLockObject = sharedLockObject;


            this.logger.Info($"{nameof(SubmissionProcessor<TSubmission>)} initialized.");
        }

        public string Name { get; set; }

        public void Start()
        {
            this.logger.Info($"{nameof(SubmissionProcessor<TSubmission>)} starting...");

            while (!this.stopping)
            {
                using (this.dependencyContainer.BeginDefaultScope())
                {
                    this.submissionProcessingStrategy = this.dependencyContainer
                        .GetInstance<ISubmissionProcessingStrategy<TSubmission>>();

                    this.submissionProcessingStrategy.Initialize(
                        this.logger,
                        this.submissionsForProcessing,
                        this.sharedLockObject);

                    var submission = this.GetSubmissionForProcessing();

                    if (submission != null)
                    {
                        switch (submission.ExecutionContextType)
                        {
                            case ExecutionContextType.Competitive:
                                this.ProcessSubmission<TestsInputModel, TestResult>(submission);
                                break;
                            case ExecutionContextType.NonCompetitive:
                                this.ProcessSubmission<string, OutputResult>(submission);
                                break;
                            default: throw new ArgumentOutOfRangeException(
                                nameof(submission.ExecutionContextType),
                                "Invalid execution context type!");
                        }
                    }
                    else
                    {
                        Thread.Sleep(this.submissionProcessingStrategy.JobLoopWaitTimeInMilliseconds);
                    }
                }
            }

            this.logger.Info($"{nameof(SubmissionProcessor<TSubmission>)} stopped.");
        }

        public void Stop()
        {
            this.stopping = true;
        }

        private ISubmission GetSubmissionForProcessing()
        {
            try
            {
                return this.submissionProcessingStrategy.RetrieveSubmission();
            }
            catch (Exception exception)
            {
                this.logger.Fatal("Unable to get submission for processing.", exception);
                throw;
            }
        }

        private void ProcessSubmission<TInput, TResult>(ISubmission submission)
            where TResult : ISingleCodeRunResult, new()
        {
            try
            {
                this.logger.Info($"Work on submission #{submission.Id} started.");

                this.BeforeExecute(submission);

                var executor = new SubmissionExecutor(this.portNumber);

                var executionResult = executor.Execute<TInput, TResult>((SubmissionInputModel<TInput>)submission);

                this.logger.Info($"Work on submission #{submission.Id} ended.");

                this.ProcessExecutionResult(executionResult, submission);

                this.logger.Info($"Submission #{submission.Id} successfully processed.");
            }
            catch (Exception ex)
            {
                this.logger.Error(
                    $"{nameof(this.ProcessSubmission)} on submission #{submission.Id} has thrown an exception:",
                    ex);

                this.submissionProcessingStrategy.OnError(submission);
            }
        }

        private void BeforeExecute(ISubmission submission)
        {
            try
            {
                this.submissionProcessingStrategy.BeforeExecute();
            }
            catch (Exception ex)
            {
                submission.ProcessingComment = $"Exception before executing the submission: {ex.Message}";

                throw new Exception($"Exception in {nameof(this.submissionProcessingStrategy.BeforeExecute)}", ex);
            }
        }

        private void ProcessExecutionResult<TOutput>(IExecutionResult<TOutput> executionResult, ISubmission submission)
            where TOutput : ISingleCodeRunResult, new()
        {
            try
            {
                this.submissionProcessingStrategy.ProcessExecutionResult(executionResult);
            }
            catch (Exception ex)
            {
                submission.ProcessingComment = $"Exception in processing execution result: {ex.Message}";

                throw new Exception($"Exception in {nameof(this.ProcessExecutionResult)}", ex);
            }
        }
    }
}