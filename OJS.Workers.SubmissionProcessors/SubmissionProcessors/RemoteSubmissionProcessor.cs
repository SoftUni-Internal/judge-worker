namespace OJS.Workers.SubmissionProcessors.SubmissionProcessors
{
    using System.Collections.Concurrent;
    using System.Collections.Generic;

    using OJS.Workers.Common;
    using OJS.Workers.Common.Models;
    using OJS.Workers.ExecutionStrategies.Models;
    using OJS.Workers.SubmissionProcessors.Common;
    using OJS.Workers.SubmissionProcessors.Formatters;
    using OJS.Workers.SubmissionProcessors.Models;
    using OJS.Workers.SubmissionProcessors.Workers;

    public class RemoteSubmissionProcessor<TSubmission>
        : SubmissionProcessor<TSubmission>
    {
        private readonly RemoteWorker remoteWorker;

        private readonly ISet<ExecutionStrategyType> remoteWorkerExecutionStrategyTypes = new HashSet<ExecutionStrategyType>
        {
            ExecutionStrategyType.CompileExecuteAndCheck,
            ExecutionStrategyType.DotNetCoreCompileExecuteAndCheck,
            ExecutionStrategyType.PythonExecuteAndCheck,
            ExecutionStrategyType.JavaPreprocessCompileExecuteAndCheck,
            ExecutionStrategyType.CPlusPlusCompileExecuteAndCheckExecutionStrategy,
            ExecutionStrategyType.PhpCliExecuteAndCheck,
            ExecutionStrategyType.NodeJsPreprocessExecuteAndCheck,
        };

        private readonly HttpService http;
        private readonly string remoteWorkerEndpoint;

        public RemoteSubmissionProcessor(
            string name,
            IDependencyContainer dependencyContainer,
            ConcurrentQueue<TSubmission> submissionsForProcessing,
            string remoteWorkerEndpoint,
            object sharedLockObject)
            : base(
                name,
                dependencyContainer,
                submissionsForProcessing,
                sharedLockObject)
        {
            this.remoteWorkerEndpoint = remoteWorkerEndpoint;
            this.remoteWorker = new RemoteWorker(
                remoteWorkerEndpoint,
                new FormatterServiceFactory());

            this.http = new HttpService();
        }

        protected override IOjsSubmission GetSubmissionForProcessing()
        {
            var submission = base.GetSubmissionForProcessing();

            if (!(submission is OjsSubmission<TestsInputModel>))
            {
                return null;
            }

            if (!this.IsOnline())
            {
                return null;
            }

            return this.remoteWorkerExecutionStrategyTypes.Contains(submission.ExecutionStrategyType)
                ? submission
                : null;
        }

        protected override void ProcessSubmission<TInput, TResult>(OjsSubmission<TInput> submission)
        {
            var result = this.remoteWorker.RunSubmission<TResult>(submission as OjsSubmission<TestsInputModel>);
            this.ProcessExecutionResult(result, submission);
        }

        private bool IsOnline()
        {
            try
            {
                var result = this.http.Get($"{this.remoteWorkerEndpoint}/health?p433w0rd=h34lth-m0n1t0r1ng");
                return result.ToString() == "Healthy";
            }
            catch
            {
                return false;
            }
        }
    }
}