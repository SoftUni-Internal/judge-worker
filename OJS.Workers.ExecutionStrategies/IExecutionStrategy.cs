namespace OJS.Workers.ExecutionStrategies
{
    using OJS.Workers.Common;

    public interface IExecutionStrategy
    {
        ExecutionResult SafeExecute(IExecutionContext executionContext);

        ExecutionResult Execute(IExecutionContext executionContext);
    }
}