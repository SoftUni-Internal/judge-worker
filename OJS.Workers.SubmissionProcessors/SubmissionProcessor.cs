namespace OJS.Workers.SubmissionProcessors
{
    using System;
    using System.Collections.Concurrent;
    using System.Threading;

    using log4net;

    using OJS.Workers.Common;
    using OJS.Workers.Common.Models;
    using OJS.Workers.ExecutionStrategies.Models;
    using OJS.Workers.SubmissionProcessors.Helpers;

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
                        this.ProcessSubmission(submission);
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

        private void ProcessSubmission(ISubmission submission)
        {
            try
            {
                this.logger.Info($"Work on submission #{submission.Id} started.");

                var executionStrategy = this.CreateExecutionStrategy(submission);

                this.BeforeExecute(submission);

                dynamic executionContext;
                dynamic executionResult;
                

                if (submission.ExecutionContextType != ExecutionContextType.NonCompetitive)
                {
                    executionContext = this.CreateExecutionContext<TestsInputModel>(submission);

                    executionResult = this.ExecuteSubmission<TestsInputModel, TestResult>(
                        executionStrategy,
                        executionContext,
                        submission);
                }
                else
                {
                    executionContext = this.CreateExecutionContext<string>(submission);

                    executionResult = this.ExecuteSubmission<string, RawResult>(
                        executionStrategy,
                        executionContext,
                        submission);
                }

                this.logger.Info($"Work on submission #{submission.Id} ended.");

                this.ProcessExecutionResult(executionResult, submission);

                this.logger.Info($"Submission #{submission.Id} successfully processed.");
            }
            catch
            {
                this.submissionProcessingStrategy.OnError(submission);
            }
        }

        private IExecutionStrategy CreateExecutionStrategy(ISubmission submission)
        {
            try
            {
                return SubmissionProcessorHelper.CreateExecutionStrategy(
                    submission.ExecutionStrategyType,
                    this.portNumber);
            }
            catch (Exception ex)
            {
                this.logger.Error(
                    $"{nameof(SubmissionProcessorHelper.CreateExecutionStrategy)} has thrown an Exception: ", ex);

                submission.ProcessingComment = $"Exception in creating execution strategy: {ex.Message}";
                throw;
            }
        }

        private IExecutionContext<TInput> CreateExecutionContext<TInput>(ISubmission submission)
        {
            try
            {
                return this.submissionProcessingStrategy.CreateExecutionContext<TInput>(submission);
            }
            catch (Exception ex)
            {
                this.logger.Error(
                    $"{nameof(this.CreateExecutionContext)} has thrown an Exception: ", ex);

                submission.ProcessingComment = $"Exception in creating execution context: {ex.Message}";
                throw;
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
                this.logger.Error(
                    $"{nameof(this.submissionProcessingStrategy.BeforeExecute)} on submission #{submission.Id} has thrown an exception:",
                    ex);

                submission.ProcessingComment = $"Exception before executing the submission: {ex.Message}";
                throw;
            }
        }

        private IExecutionResult<TResult> ExecuteSubmission<TInput, TResult>(
            IExecutionStrategy executionStrategy,
            IExecutionContext<TInput> executionContext,
            ISubmission submission)
            where TResult : ISingleCodeRunResult, new()
        {
            try
            {
                return executionStrategy.SafeExecute<TInput, TResult>(executionContext);
            }
            catch (Exception ex)
            {
                this.logger.Error(
                    $"{nameof(executionStrategy.SafeExecute)} on submission #{submission.Id} has thrown an exception:",
                    ex);

                submission.ProcessingComment = $"Exception in executing the submission: {ex.Message}";
                throw;
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
                this.logger.Error(
                    $"{nameof(this.ProcessExecutionResult)} on submission #{submission.Id} has thrown an exception:",
                    ex);

                submission.ProcessingComment = $"Exception in processing submission: {ex.Message}";
                throw;
            }
        }
    }
}