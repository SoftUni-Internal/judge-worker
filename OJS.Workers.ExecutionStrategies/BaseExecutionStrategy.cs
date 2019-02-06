namespace OJS.Workers.ExecutionStrategies
{
    using System;
    using System.Threading.Tasks;

    using log4net;

    using OJS.Workers.Common;
    using OJS.Workers.Common.Exceptions;
    using OJS.Workers.Common.Helpers;
    using OJS.Workers.ExecutionStrategies.Models;

    public abstract class BaseExecutionStrategy : IExecutionStrategy
    {
        private readonly ILog logger;

        public BaseExecutionStrategy() => this.logger = LogManager.GetLogger(typeof(BaseExecutionStrategy));

        protected string WorkingDirectory { get; set; }

        public IExecutionResult<TResult> Execute<TInput, TResult>(IExecutionContext<TInput> executionContext)
            where TResult : ISingleCodeRunResult, new()
        {
            var result = new ExecutionResult<TResult>();

            if (executionContext is IExecutionContext<string> stringInputExecutionContext &&
                result is IExecutionResult<OutputResult> outputResult)
            {
                this.ExecuteAgainstSimpleInput(stringInputExecutionContext, outputResult);
            }
            else if (executionContext is IExecutionContext<TestsInputModel> testsExecutionContext &&
                result is IExecutionResult<TestResult> testsResult)
            {
                this.ExecuteAgainstTestsInput(testsExecutionContext, testsResult);
            }
            else
            {
                throw new InvalidExecutionContextException<TInput, TResult>(executionContext, result);
            }

            return result;
        }

        public IExecutionResult<TResult> SafeExecute<TInput, TResult>(IExecutionContext<TInput> executionContext)
            where TResult : ISingleCodeRunResult, new()
        {
            this.WorkingDirectory = DirectoryHelpers.CreateTempDirectoryForExecutionStrategy();

            try
            {
                return this.Execute<TInput, TResult>(executionContext);
            }
            finally
            {
                Task.Run(() =>
                {
                    try
                    {
                        DirectoryHelpers.SafeDeleteDirectory(this.WorkingDirectory, true);
                    }
                    catch (Exception ex)
                    {
                        this.logger.Error("executionStrategy.SafeDeleteDirectory has thrown an exception:", ex);
                    }
                });
            }
        }

        protected virtual void ExecuteAgainstSimpleInput(
            IExecutionContext<string> executionContext,
            IExecutionResult<OutputResult> result)
            => throw new DerivedImplementationNotFoundException();

        protected virtual void ExecuteAgainstTestsInput(
            IExecutionContext<TestsInputModel> executionContext,
            IExecutionResult<TestResult> result)
            => throw new DerivedImplementationNotFoundException();
    }
}
