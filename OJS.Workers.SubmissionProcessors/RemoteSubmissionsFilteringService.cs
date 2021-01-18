namespace OJS.Workers.SubmissionProcessors
{
    using System.Collections.Generic;

    using OJS.Workers.Common;
    using OJS.Workers.Common.Models;
    using OJS.Workers.ExecutionStrategies.Models;
    using OJS.Workers.SubmissionProcessors.Common;
    using OJS.Workers.SubmissionProcessors.Models;
    using OJS.Workers.SubmissionProcessors.Workers;

    using static OJS.Workers.Common.ExecutionStrategiesConstants.NameMappings;

    public class RemoteSubmissionsFilteringService
        : ISubmissionsFilteringService
    {
        private readonly ISet<ExecutionStrategyType> remoteWorkerExecutionStrategyTypes = RemoteWorkerSupportedStrategies;

        private readonly HttpService http;

        public RemoteSubmissionsFilteringService()
            => this.http = new HttpService();

        public bool CanProcessSubmission(IOjsSubmission submission, ISubmissionWorker submissionWorker)
            => submission is OjsSubmission<TestsInputModel>
               && this.remoteWorkerExecutionStrategyTypes.Contains(submission.ExecutionStrategyType)
               && this.IsOnline(submissionWorker);

        private bool IsOnline(ISubmissionWorker submissionWorker)
        {
            try
            {
                var result = this.http.Get($"{submissionWorker.Location}/health?p433w0rd=h34lth-m0n1t0r1ng");
                return result == "Healthy";
            }
            catch
            {
                return false;
            }
        }
    }
}