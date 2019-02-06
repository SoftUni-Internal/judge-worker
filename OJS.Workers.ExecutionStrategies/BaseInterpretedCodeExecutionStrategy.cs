namespace OJS.Workers.ExecutionStrategies
{
    using OJS.Workers.Common;
    using OJS.Workers.Executors;

    public class BaseInterpretedCodeExecutionStrategy : BaseCodeExecutionStrategy
    {
        protected BaseInterpretedCodeExecutionStrategy(
            IProcessExecutorFactory processExecutorFactory,
            int baseTimeUsed,
            int baseMemoryUsed)
            : base(processExecutorFactory, baseTimeUsed, baseMemoryUsed)
        {
        }

        protected override IExecutionResult<TResult> InternalExecute<TInput, TResult>(
            IExecutionContext<TInput> executionContext,
            IExecutionResult<TResult> result)
        {
            result.IsCompiledSuccessfully = true;

            result = base.InternalExecute(executionContext, result);

            return result;
        }
    }
}
