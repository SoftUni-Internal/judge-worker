namespace OJS.Workers.Common
{
    public interface IExecutionStrategy
    {
        IExecutionResult<TResult> SafeExecute<TResult>(IExecutionContext executionContext)
            where TResult : ISingleCodeRunResult, new();

        IExecutionResult<TResult> Execute<TResult>(IExecutionContext executionContext)
            where TResult : ISingleCodeRunResult, new();
    }
}