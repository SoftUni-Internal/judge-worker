namespace OJS.Workers.Common.Extensions
{
    public static class ExecutionResultExtensions
    {
        public static IExecutionResult<TResult> CompilationFail<TResult>(
            this IExecutionResult<TResult> result,
            string message)
            where TResult : ISingleCodeRunResult, new()
        {
            result.IsCompiledSuccessfully = false;
            result.CompilerComment = message;
            result.Results.Clear();

            return result;
        }
    }
}
