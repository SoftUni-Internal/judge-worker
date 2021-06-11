using System.Linq;
using OJS.Workers.SubmissionProcessors.Formatters;
using OJS.Workers.SubmissionProcessors.Workers;

namespace OJS.Workers
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.IO;
    using System.Reflection;
    using System.ServiceProcess;
    using System.Threading;

    using log4net;

    using OJS.Workers.Common;
    using OJS.Workers.SubmissionProcessors;
    using OJS.Workers.SubmissionProcessors.ExecutionTypeFilters;

    public class LocalWorkerServiceBase<TSubmission> : ServiceBase
    {
        private readonly ICollection<Thread> threads;
        private readonly ICollection<ISubmissionProcessor> submissionProcessors;

        protected LocalWorkerServiceBase()
        {
            var loggerAssembly = Assembly.GetEntryAssembly() ?? Assembly.GetCallingAssembly();

            this.Logger = LogManager.GetLogger(loggerAssembly, Constants.LocalWorkerServiceLogName);

            this.threads = new List<Thread>();
            this.submissionProcessors = new List<ISubmissionProcessor>();
        }

        protected ILog Logger { get; }

        protected IDependencyContainer DependencyContainer { get; private set; }

        protected virtual int TimeBeforeAbortingThreadsInMilliseconds =>
            Constants.DefaultTimeBeforeAbortingThreadsInMilliseconds;

        protected override void OnStart(string[] args)
        {
            this.Logger.Info($"{Constants.LocalWorkerServiceName} starting...");

            this.DependencyContainer = this.GetDependencyContainer();

            this.BeforeStartingThreads();

            this.StartThreads();

            this.Logger.Info($"{Constants.LocalWorkerServiceName} started.");
        }

        protected override void OnStop()
        {
            this.Logger.Info($"{Constants.LocalWorkerServiceName} stopping...");

            this.BeforeAbortingThreads();

            this.AbortThreads();

            this.Logger.Info($"{Constants.LocalWorkerServiceName} stopped.");
        }

        protected virtual void BeforeStartingThreads()
        {
            this.SpawnSubmissionProcessorsAndThreads();

            this.CreateExecutionStrategiesWorkingDirectory();
        }

        protected virtual void BeforeAbortingThreads()
        {
            this.StopSubmissionProcessors();

            Thread.Sleep(this.TimeBeforeAbortingThreadsInMilliseconds);
        }

        protected virtual IDependencyContainer GetDependencyContainer() =>
            throw new InvalidOperationException(
                $"{nameof(this.GetDependencyContainer)} method required but not implemented in derived service");

        private void SpawnSubmissionProcessorsAndThreads()
        {
            var submissionsForProcessing = new ConcurrentQueue<TSubmission>();
            var sharedLockObject = new object();

            var workerThreads = new List<(SubmissionProcessor<TSubmission> submissionProcessor, Thread thread)>();

            var formatterServiceFactory = new FormatterServiceFactory();
            var remoteSubmissionsFilteringService = new RemoteSubmissionsFilteringService();
            var localSubmissionsFilteringService = new LocalSubmissionsFilteringService();
            workerThreads.AddRange(this.GetLocalWorkers(Settings.ThreadsCount, submissionsForProcessing, sharedLockObject, localSubmissionsFilteringService));
            workerThreads.AddRange(this.GetRemoteWorkers(Settings.RemoteWorkerEndpoints, submissionsForProcessing, sharedLockObject, formatterServiceFactory, remoteSubmissionsFilteringService));

            workerThreads
                .ToList()
                .ForEach(workerThread =>
                {
                    var (submissionProcessor, thread) = workerThread;
                    this.submissionProcessors.Add(submissionProcessor);
                    this.threads.Add(thread);
                });
        }

        private IEnumerable<(SubmissionProcessor<TSubmission> submissionProcessor, Thread thread)> GetRemoteWorkers(
            IEnumerable<string> remoteWorkerEndpoints,
            ConcurrentQueue<TSubmission> submissionsForProcessing,
            object sharedLockObject,
            IFormatterServiceFactory formatterServiceFactory,
            ISubmissionsFilteringService submissionsFilteringService)
        {
            var remoteWorkerEndpointsList = remoteWorkerEndpoints.ToList();
            return Enumerable.Range(0, remoteWorkerEndpointsList.Count)
                .Select(index => this.CreateRemoteWorker(
                    index + 1,
                    remoteWorkerEndpointsList[index],
                    submissionsForProcessing,
                    sharedLockObject,
                    formatterServiceFactory,
                    submissionsFilteringService));
        }

        private IEnumerable<(SubmissionProcessor<TSubmission> submissionProcessor, Thread thread)> GetLocalWorkers(
            int count,
            ConcurrentQueue<TSubmission> submissionsForProcessing,
            object sharedLockObject,
            ISubmissionsFilteringService submissionsFilteringService)
            => Enumerable.Range(0, count)
                .Select(index => this.CreateLocalWorker(index + 1, submissionsForProcessing, sharedLockObject, submissionsFilteringService));

        private (SubmissionProcessor<TSubmission> submissionProcessor, Thread thread) CreateLocalWorker(int index,
            ConcurrentQueue<TSubmission> submissionsForProcessing,
            object sharedLockObject,
            ISubmissionsFilteringService submissionsFilteringService)
        {
            var worker = new LocalSubmissionWorker(Settings.GanacheCliDefaultPortNumber + index);
            var submissionProcessor = new SubmissionProcessor<TSubmission>(
                name: $"LSP #{index}",
                dependencyContainer: this.DependencyContainer,
                submissionsForProcessing: submissionsForProcessing,
                sharedLockObject: sharedLockObject,
                submissionsFilteringService: submissionsFilteringService,
                submissionWorker: worker);

            var thread = new Thread(submissionProcessor.Start)
            {
                Name = $"{nameof(Thread)} #{index}"
            };

            return (submissionProcessor, thread);
        }

        private (SubmissionProcessor<TSubmission> submissionProcessor, Thread thread) CreateRemoteWorker(int index,
            string endpoint,
            ConcurrentQueue<TSubmission> submissionsForProcessing,
            object sharedLockObject,
            IFormatterServiceFactory formatterServiceFactory,
            ISubmissionsFilteringService submissionsFilteringService)
        {
            var worker = new RemoteSubmissionsWorker(
                endpoint,
                formatterServiceFactory);

            var submissionProcessor = new SubmissionProcessor<TSubmission>(
                name: $"RSP #{index}",
                dependencyContainer: this.DependencyContainer,
                submissionsForProcessing: submissionsForProcessing,
                sharedLockObject: sharedLockObject,
                submissionsFilteringService: submissionsFilteringService,
                submissionWorker: worker);

            var thread = new Thread(submissionProcessor.Start)
            {
                Name = $"{nameof(Thread)} #{index}"
            };

            return (submissionProcessor, thread);
        }

        private void StartThreads()
        {
            foreach (var thread in this.threads)
            {
                this.Logger.InfoFormat($"Starting {thread.Name}...");
                thread.Start();
                this.Logger.InfoFormat($"{thread.Name} started.");
                Thread.Sleep(234);
            }
        }

        private void StopSubmissionProcessors()
        {
            foreach (var submissionProcessor in this.submissionProcessors)
            {
                submissionProcessor.Stop();
                this.Logger.InfoFormat($"{submissionProcessor.Name} stopped.");
            }
        }

        private void AbortThreads()
        {
            foreach (var thread in this.threads)
            {
                thread.Abort();
                this.Logger.InfoFormat($"{thread.Name} aborted.");
            }
        }

        /// <summary>
        /// Creates folder in the Temp directory if not already created,
        /// in which all strategies create their own working directories
        /// making easier the deletion of left-over files by the background job
        /// </summary>
        private void CreateExecutionStrategiesWorkingDirectory()
        {
            var path = Constants.ExecutionStrategiesWorkingDirectoryPath;
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
        }
    }
}