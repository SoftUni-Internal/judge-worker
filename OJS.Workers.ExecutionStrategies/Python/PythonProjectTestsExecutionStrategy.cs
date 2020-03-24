namespace OJS.Workers.ExecutionStrategies.Python
{
    using OJS.Workers.Executors;

    public class PythonProjectTestsExecutionStrategy : PythonCodeExecuteAgainstUnitTestsExecutionStrategy
    {
        public PythonProjectTestsExecutionStrategy(
            IProcessExecutorFactory processExecutorFactory,
            string pythonExecutablePath,
            int baseTimeUsed,
            int baseMemoryUsed)
            : base(processExecutorFactory, pythonExecutablePath, baseTimeUsed, baseMemoryUsed)
        {
        }
    }
}