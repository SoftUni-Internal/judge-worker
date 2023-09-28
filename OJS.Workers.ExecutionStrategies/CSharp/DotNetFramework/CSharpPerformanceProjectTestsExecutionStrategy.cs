﻿namespace OJS.Workers.ExecutionStrategies.CSharp.DotNetFramework
{
    using OJS.Workers.Common;
    using OJS.Workers.Common.Helpers;
    using OJS.Workers.Common.Models;
    using OJS.Workers.ExecutionStrategies.Models;
    using OJS.Workers.Executors;

    using static Common.Constants;

    public class CSharpPerformanceProjectTestsExecutionStrategy : CSharpProjectTestsExecutionStrategy
    {
        public CSharpPerformanceProjectTestsExecutionStrategy(
            Func<CompilerType, string> getCompilerPathFunc,
            IProcessExecutorFactory processExecutorFactory,
            string nUnitConsoleRunnerPath,
            int baseTimeUsed,
            int baseMemoryUsed)
            : base(getCompilerPathFunc, processExecutorFactory, nUnitConsoleRunnerPath, baseTimeUsed, baseMemoryUsed)
            => this.TestClassNames = new List<string>();

        protected List<string> TestClassNames { get; }

        protected override IExecutionResult<TestResult> RunUnitTests(
            string consoleRunnerPath,
            IExecutionContext<TestsInputModel> executionContext,
            IExecutor executor,
            IChecker checker,
            IExecutionResult<TestResult> result,
            string compiledFile,
            string additionalExecutionArguments)
        {
            var testIndex = 0;
            foreach (var test in executionContext.Input.Tests)
            {
                var arguments = new List<string> { $"--where \"class == {this.TestClassNames[testIndex]}\" \"{compiledFile}\"" };
                arguments.AddRange(additionalExecutionArguments.Split(' '));

                var processExecutionResult = executor.Execute(
                    consoleRunnerPath,
                    string.Empty,
                    executionContext.TimeLimit,
                    executionContext.MemoryLimit,
                    arguments);

                var errorsByFiles = this.GetTestErrors(processExecutionResult.ReceivedOutput);

                var message = TestPassedMessage;
                var testFile = this.TestNames[testIndex];
                if (errorsByFiles.ContainsKey(testFile))
                {
                    message = errorsByFiles[testFile];
                }

                var testResult = this.CheckAndGetTestResult(test, processExecutionResult, checker, message);
                result.Results.Add(testResult);
                testIndex++;
            }

            return result;
        }

        protected override void ExtractTestNames(IEnumerable<TestContext> tests)
        {
            var trialTests = 1;
            var competeTests = 1;

            foreach (var test in tests)
            {
                var namespacePrefix = CSharpPreprocessorHelper.GetNamespaceName(test.Input);
                namespacePrefix = namespacePrefix == null ? string.Empty : namespacePrefix + ".";
                this.TestClassNames.Add($"{namespacePrefix}{CSharpPreprocessorHelper.GetClassName(test.Input)}");
                if (test.IsTrialTest)
                {
                    var testNumber = trialTests < 10 ? $"00{trialTests}" : $"0{trialTests}";
                    this.TestNames.Add($"{TrialTest}.{testNumber}");
                    trialTests++;
                }
                else
                {
                    var testNumber = competeTests < 10 ? $"00{competeTests}" : $"0{competeTests}";
                    this.TestNames.Add($"{CompeteTest}.{testNumber}");
                    competeTests++;
                }
            }
        }
    }
}
