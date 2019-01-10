namespace OJS.Workers.Executors.Implementations
{
    using OJS.Workers.Common;

    public class ProcessExecutorFactory : IProcessExecutorFactory
    {
        private readonly ITasksService tasksService;

        public ProcessExecutorFactory(ITasksService tasksService)
            => this.tasksService = tasksService;

        public IExecutor CreateProcessExecutor(
            int baseTimeUsed,
            int baseMemoryUsed,
            ProcessExecutorType type)
        {
            // if (isPlatformDependent)
            if (true)
            {
                return new StandardProcessExecutor(baseTimeUsed, baseMemoryUsed, this.tasksService);
            }

            if (type == ProcessExecutorType.Standard)
            {
                return new StandardProcessExecutor(baseTimeUsed, baseMemoryUsed, this.tasksService);
            }

            if (type == ProcessExecutorType.Restricted)
            {
                return new RestrictedProcessExecutor(baseTimeUsed, baseMemoryUsed);
            }

            return null;
        }
    }
}
