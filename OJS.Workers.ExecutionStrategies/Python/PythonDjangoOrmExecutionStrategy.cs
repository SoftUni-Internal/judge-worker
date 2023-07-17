namespace OJS.Workers.ExecutionStrategies.Python
{
    using OJS.Workers.Common;
    using OJS.Workers.ExecutionStrategies.Models;
    using OJS.Workers.Executors;

    public class PythonDjangoOrmExecutionStrategy : PythonProjectTestsExecutionStrategy
    {
        public PythonDjangoOrmExecutionStrategy(IProcessExecutorFactory processExecutorFactory, string pythonExecutablePath, int baseTimeUsed, int baseMemoryUsed)
            : base(processExecutorFactory, pythonExecutablePath, baseTimeUsed, baseMemoryUsed)
        {
        }

        protected override IExecutionResult<TestResult> ExecuteAgainstTestsInput(
            IExecutionContext<TestsInputModel> executionContext, IExecutionResult<TestResult> result)
        {
            this.SaveZipSubmission(executionContext.FileContent, this.WorkingDirectory);

            var executor = this.CreateExecutor();
            var checker = executionContext.Input.GetChecker();

            return this.RunTests(string.Empty, executor, checker, executionContext, result);
        }
    }
}