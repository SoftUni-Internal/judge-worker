namespace OJS.Workers.SubmissionProcessors.ExecutionTypeFilters
{
    using System;
    using System.Collections.Generic;
    using log4net;
    using OJS.Workers.Common;
    using OJS.Workers.Common.Extensions;
    using OJS.Workers.Common.Helpers;
    using OJS.Workers.Common.Models;
    using OJS.Workers.SubmissionProcessors.Common;
    using OJS.Workers.SubmissionProcessors.Workers;
    using static OJS.Workers.Common.ExecutionStrategiesConstants.NameMappings;

    public class RemoteSubmissionsFilteringService
        : SubmissionFilteringServiceBase
    {
        private readonly ILog logger;
        private readonly HttpService http;

        public RemoteSubmissionsFilteringService()
        {
            this.http = new HttpService();
            this.logger = LogManager.GetLogger(typeof(RemoteSubmissionsFilteringService));
        }

        protected override ISet<ExecutionStrategyType> EnabledExecutionStrategyTypes
            => EnabledRemoteWorkerStrategies;

        protected override ISet<ExecutionStrategyType> DisabledExecutionStrategyTypes
            => new HashSet<ExecutionStrategyType>();

        protected override ISet<CompilerType> DisabledExecuteAndCompileCompilerTypes
            => new HashSet<CompilerType>();

        protected override bool CanProcessSubmissionInternal(IOjsSubmission submission, ISubmissionWorker submissionWorker)
            => this.IsOnline(submissionWorker);

        private bool IsOnline(ISubmissionWorker submissionWorker)
        {
            var healthConfigKey = SettingsHelper.GetSetting("HealthConfigKey");
            var healthConfigPassword = SettingsHelper.GetSetting("HealthConfigPassword");
            var url = $"{submissionWorker.Location}/health?{healthConfigKey}={healthConfigPassword}";

            try
            {
                var result = this.http.Get(url);

                if (result == "Healthy")
                {
                    return true;
                }

                this.logger.Info($"Response from '{url}' is: {result}");
                return false;
            }
            catch (Exception ex)
            {
                this.logger.Error($"Exception in getting remote worker health response from '{url}'. Reason: {ex.GetAllMessages()}.");
                return false;
            }
        }
    }
}