namespace OJS.Workers.ExecutionStrategies.Python
{
    using System;
    using System.Text.RegularExpressions;

    using OJS.Workers.Common;
    using OJS.Workers.Common.Helpers;
    using OJS.Workers.ExecutionStrategies.Models;
    using OJS.Workers.Executors;

    public class PythonUnitTestsExecutionStrategy : PythonExecuteAndCheckExecutionStrategy
    {
        private const string ClassNameInSkeletonRegexPattern = @"#\s+class_name\s+([^\s]+)\s*$";
        private const string ImportTargetClassRegexPattern = @"^(from\s+{0}\s+import\s.*)|^(import\s+{0}(?=\s|$).*)";
        private const string ClassNameNotFoundErrorMessage =
            "class_name is required in Solution Skeleton. Please contact an Administrator.";

        public PythonUnitTestsExecutionStrategy(
            IProcessExecutorFactory processExecutorFactory,
            string pythonExecutablePath,
            int baseTimeUsed,
            int baseMemoryUsed)
            : base(processExecutorFactory, pythonExecutablePath, baseTimeUsed, baseMemoryUsed)
        {
        }

        protected override ProcessExecutionResult Execute(
            IExecutionContext<TestsInputModel> executionContext,
            IExecutor executor,
            string codeSavePath,
            TestContext test)
        {
            FileHelpers.WriteAllText(codeSavePath, test.Input + Environment.NewLine + executionContext.Code);

            var processExecutionResult = this.Execute(executionContext, executor, codeSavePath, string.Empty);

            return processExecutionResult;
        }

        protected override string SaveCodeToTempFile<TInput>(IExecutionContext<TInput> executionContext)
        {
            var className = this.GetTestCodeClassName(executionContext.Input as TestsInputModel);
            var classImportPattern = string.Format(ImportTargetClassRegexPattern, className);

            executionContext.Code = Regex.Replace(
                executionContext.Code,
                classImportPattern,
                string.Empty,
                RegexOptions.Multiline);

            return base.SaveCodeToTempFile(executionContext);
        }

        private string GetTestCodeClassName(TestsInputModel testsInput)
        {
            var className = Regex.Match(testsInput.TaskSkeletonAsString, ClassNameInSkeletonRegexPattern)
                .Groups[1]
                .Value;

            if (string.IsNullOrWhiteSpace(className))
            {
                throw new ArgumentException(ClassNameNotFoundErrorMessage);
            }

            return className;
        }
    }
}
