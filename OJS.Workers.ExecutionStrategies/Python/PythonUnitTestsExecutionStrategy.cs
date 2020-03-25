namespace OJS.Workers.ExecutionStrategies.Python
{
    using System;
    using System.Linq;
    using System.Text.RegularExpressions;

    using OJS.Workers.Common;
    using OJS.Workers.ExecutionStrategies.Helpers;
    using OJS.Workers.ExecutionStrategies.Models;
    using OJS.Workers.Executors;

    public class PythonUnitTestsExecutionStrategy : PythonCodeExecuteAgainstUnitTestsExecutionStrategy
    {
        private const string ClassNamePlaceholder = "# class_name ";
        private const string ImportTargetClassRegexPattern = @"^(from\s+{0}\s+import\s.*)|^(import\s+{0}(?=\s|$).*)";

        private readonly string classNameInSkeletonRegexPattern = $@"{ClassNamePlaceholder}\s*([^\s]+)\s*$";
        private readonly string classNameNotFoundErrorMessage =
            $"Class name not found in Solution Skeleton. Expecting \"{ClassNamePlaceholder}\" followed by the test class's name.";

        public PythonUnitTestsExecutionStrategy(
            IProcessExecutorFactory processExecutorFactory,
            string pythonExecutablePath,
            int baseTimeUsed,
            int baseMemoryUsed)
            : base(processExecutorFactory, pythonExecutablePath, baseTimeUsed, baseMemoryUsed)
        {
        }

        protected override IExecutionResult<TestResult> RunTests(
            string codeSavePath,
            IExecutor executor,
            IChecker checker,
            IExecutionContext<TestsInputModel> executionContext,
            IExecutionResult<TestResult> result)
        {
            var originalTestsPassed = -1;

            var tests = executionContext.Input.Tests
                .OrderByDescending(x => x.IsTrialTest)
                .ThenBy(x => x.OrderBy)
                .ToList();

            for (var i = 0; i < tests.Count; i++)
            {
                var test = tests[i];

                this.WriteTestInCodeFile(test.Input, codeSavePath, executionContext.Code);

                var processExecutionResult = this.Execute(executionContext, executor, codeSavePath, string.Empty);

                var endMessage = string.Empty;

                if (processExecutionResult.Type == ProcessExecutionResultType.Success)
                {
                    var (message, testsPassed) = UnitTestStrategiesHelper.GetTestResult(
                        processExecutionResult.ReceivedOutput,
                        TestsRegex,
                        originalTestsPassed,
                        i == 0,
                        this.ExtractTestsCountFromMatchCollection);

                    originalTestsPassed = testsPassed;
                    endMessage = message;
                }

                var testResult = this.CheckAndGetTestResult(test, processExecutionResult, checker, endMessage);
                result.Results.Add(testResult);
            }

            return result;
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

        (int totalTests, int passedTests) ExtractTestsCountFromMatchCollection(MatchCollection matches)
        {
            var testRunsPattern = matches[0].Groups[1].Value.Trim();

            var testRuns = testRunsPattern.ToCharArray();

            var totalTests = testRuns.Length;

            // '.' indicates passed test in the unittest console output e.g. "...F..F"
            var passedTests = testRuns.Count(c => c == '.');

            return (totalTests, passedTests);
        }

        private string GetTestCodeClassName(TestsInputModel testsInput)
        {
            var taskSkeleton = testsInput.TaskSkeletonAsString ?? string.Empty;

            var className = Regex.Match(taskSkeleton, this.classNameInSkeletonRegexPattern)
                .Groups[1]
                .Value;

            if (string.IsNullOrWhiteSpace(className))
            {
                throw new ArgumentException(this.classNameNotFoundErrorMessage);
            }

            return className;
        }
    }
}
