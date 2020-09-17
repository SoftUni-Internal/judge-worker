namespace OJS.Workers.SubmissionProcessors.SubmissionProcessors
{
    using System;
    using System.Collections.Concurrent;

    using OJS.Workers.Common;
    using OJS.Workers.SubmissionProcessors.Models;

    public class LocalSubmissionProcessor<TSubmission>
        : SubmissionProcessor<TSubmission>
    {
        private readonly int portNumber;

        public LocalSubmissionProcessor(
            string name,
            IDependencyContainer dependencyContainer,
            ConcurrentQueue<TSubmission> submissionsForProcessing,
            int portNumber,
            object sharedLockObject)
            : base(name, dependencyContainer, submissionsForProcessing, sharedLockObject)
        {
            this.portNumber = portNumber;
        }

        protected override void ProcessSubmission<TInput, TResult>(OjsSubmission<TInput> submission)
        {
            this.Logger.Info($"Work on submission #{submission.Id} started.");

            this.BeforeExecute(submission);

            var executor = new SubmissionExecutor(this.portNumber);

            var executionResult = executor.Execute<TInput, TResult>(submission);

            this.Logger.Info($"Work on submission #{submission.Id} ended.");

            this.ProcessExecutionResult(executionResult, submission);

            this.Logger.Info($"Submission #{submission.Id} successfully processed.");
        }
    }
}