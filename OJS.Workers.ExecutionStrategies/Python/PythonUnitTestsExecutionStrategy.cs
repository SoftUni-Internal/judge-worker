namespace OJS.Workers.ExecutionStrategies.Python
{
    using System;
    using System.Linq;
    using System.Text.RegularExpressions;

    using OJS.Workers.Common;
    using OJS.Workers.Common.Helpers;
    using OJS.Workers.ExecutionStrategies.Helpers;
    using OJS.Workers.ExecutionStrategies.Models;
    using OJS.Workers.Executors;

    public class PythonUnitTestsExecutionStrategy : PythonExecuteAndCheckExecutionStrategy
    {
        private const string ClassNameInSkeletonRegexPattern = @"#\s+class_name\s+([^\s]+)\s*$";
        private const string ImportTargetClassRegexPattern = @"^(from\s+{0}\s+import\s.*)|^(import\s+{0}(?=\s|$).*)";
        private const char PassedTestMarker = '.';
        private const char FailedTestMarker = 'F';
        private const char ErrorInTestMarker = 'E';
        private const string ClassNameNotFoundErrorMessage =
            "class_name is required in Solution Skeleton. Please contact an Administrator.";

        private readonly string testResultsRegexPattern =
            $@"^([{PassedTestMarker}{FailedTestMarker}{ErrorInTestMarker}]+)\s*$";

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

            var tests = executionContext.Input.Tests.OrderBy(x => x.IsTrialTest).ThenBy(x => x.OrderBy).ToList();

            for (var i = 0; i < tests.Count; i++)
            {
                var test = tests[i];

                FileHelpers.WriteAllText(codeSavePath, test.Input + Environment.NewLine + executionContext.Code);

                var processExecutionResult = this.Execute(executionContext, executor, codeSavePath, string.Empty);

                var testResultsRegex = new Regex(this.testResultsRegexPattern, RegexOptions.Multiline);

                var (message, testsPassed) = UnitTestStrategiesHelper.GetTestResult(
                    processExecutionResult.ReceivedOutput,
                    testResultsRegex,
                    originalTestsPassed,
                    i == 0,
                    this.ExtractTestsCountFromMatchCollection);

                originalTestsPassed = testsPassed;

                var testResult = this.CheckAndGetTestResult(test, processExecutionResult, checker, message);
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

        protected override ProcessExecutionResult Execute<TInput>(
            IExecutionContext<TInput> executionContext,
            IExecutor executor,
            string codeSavePath,
            string input)
        {
            var processExecutionResult = base.Execute(executionContext, executor, codeSavePath, input);
            this.FixReceivedOutput(processExecutionResult);
            return processExecutionResult;
        }

        (int totalTests, int passedTests) ExtractTestsCountFromMatchCollection(MatchCollection matches)
        {
            var testRunsPattern = matches[0].Groups[1].Value;

            var testRuns = testRunsPattern.ToCharArray();

            var totalTests = testRuns.Length;
            var passedTests = testRuns.Count(c => c == PassedTestMarker);

            return (totalTests, passedTests);
        }

        private void FixReceivedOutput(ProcessExecutionResult processExecutionResult)
        {
            var output = processExecutionResult.ErrorOutput ?? string.Empty;

            if (processExecutionResult.Type == ProcessExecutionResultType.RunTimeError &&
                Regex.IsMatch(output, this.testResultsRegexPattern, RegexOptions.Multiline))
            {
                processExecutionResult.ReceivedOutput = output;
                processExecutionResult.ErrorOutput = string.Empty;
                processExecutionResult.Type = ProcessExecutionResultType.Success;
            }
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
