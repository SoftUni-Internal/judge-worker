namespace OJS.Workers.ExecutionStrategies
{
    using OJS.Workers.Common;
    using OJS.Workers.ExecutionStrategies.Models;

    public class CheckOnlyExecutionStrategy : ExecutionStrategy
    {
        public CheckOnlyExecutionStrategy(int baseTimeUsed, int baseMemoryUsed)
            : base(baseTimeUsed, baseMemoryUsed)
        {
        }

        protected override IExecutionResult<TestResult> ExecuteCompetitive(
            IExecutionContext<TestsInputModel> executionContext)
        {
            var result = new ExecutionResult<TestResult>
            {
                IsCompiledSuccessfully = true
            };

            var processExecutionResult = new ProcessExecutionResult
            {
                Type = ProcessExecutionResultType.Success,
                ReceivedOutput = executionContext.Code
            };

            foreach (var test in executionContext.Input.Tests)
            {
                var testResult = this.ExecuteAndCheckTest(
                    test,
                    processExecutionResult,
                    executionContext.Input.Checker,
                    processExecutionResult.ReceivedOutput);

                result.Results.Add(testResult);
            }

            return result;
        }
    }
}