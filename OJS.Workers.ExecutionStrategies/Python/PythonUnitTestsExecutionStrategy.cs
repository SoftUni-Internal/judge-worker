namespace OJS.Workers.ExecutionStrategies.Python
{
    using System;
    using System.IO;
    using System.Text.RegularExpressions;

    using OJS.Workers.Common;
    using OJS.Workers.Common.Helpers;
    using OJS.Workers.ExecutionStrategies.Models;
    using OJS.Workers.Executors;

    using static OJS.Workers.Common.Constants;

    public class PythonUnitTestsExecutionStrategy : BaseInterpretedCodeExecutionStrategy
    {
        private const string UnitTestArgument = "-m unittest";
        private const string BufferArgument = "--buffer";
        private const string ClassNameInSkeletonRegexPattern = @"#\s+class_name\s+([^\s]+)\s*$";
        private const string ClassNameNotFoundErrorMessage =
            "class_name is required in Solution Skeleton. Please contact an Administrator.";

        private readonly string pythonExecutablePath;

        public PythonUnitTestsExecutionStrategy(
            IProcessExecutorFactory processExecutorFactory,
            string pythonExecutablePath,
            int baseTimeUsed,
            int baseMemoryUsed)
            : base(processExecutorFactory, baseTimeUsed, baseMemoryUsed)
        {
            if (!File.Exists(pythonExecutablePath))
            {
                throw new ArgumentException($"Python not found in: {pythonExecutablePath}", nameof(pythonExecutablePath));
            }

            this.pythonExecutablePath = pythonExecutablePath;
        }

        protected override IExecutionResult<TestResult> ExecuteAgainstTestsInput(
            IExecutionContext<TestsInputModel> executionContext,
            IExecutionResult<TestResult> result)
        {
            var codeSavePath = FileHelpers.SaveStringToTempFile(
                this.WorkingDirectory,
                executionContext.Code,
                PythonFileExtension);

            var executor = this.CreateExecutor(ProcessExecutorType.Restricted);

            var checker = executionContext.Input.GetChecker();

            var className = this.GetTestCodeClassName(executionContext.Input);

            foreach (var test in executionContext.Input.Tests)
            {
                FileHelpers.SaveStringToFile(
                    this.WorkingDirectory,
                    test.Input,
                    className);

                var processExecutionResult = executor.Execute(
                    this.pythonExecutablePath,
                    string.Empty,
                    executionContext.TimeLimit,
                    executionContext.MemoryLimit,
                    new[] { UnitTestArgument, BufferArgument, codeSavePath },
                    this.WorkingDirectory,
                    false,
                    true);

                var testResult = this.CheckAndGetTestResult(
                    test,
                    processExecutionResult,
                    checker,
                    processExecutionResult.ReceivedOutput);

                result.Results.Add(testResult);
            }

            return result;
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

            return className + PythonFileExtension;
        }
    }
}
