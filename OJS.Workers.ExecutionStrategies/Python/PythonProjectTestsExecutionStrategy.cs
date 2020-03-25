namespace OJS.Workers.ExecutionStrategies.Python
{
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;

    using OJS.Workers.Common;
    using OJS.Workers.ExecutionStrategies.Helpers;
    using OJS.Workers.ExecutionStrategies.Models;
    using OJS.Workers.Executors;

    using static OJS.Workers.Common.Constants;
    using static OJS.Workers.ExecutionStrategies.Python.PythonConstants;

    public class PythonProjectTestsExecutionStrategy : PythonCodeExecuteAgainstUnitTestsExecutionStrategy
    {
        private const string TestsFolderName = "tests";

        private string[] testPaths;

        public PythonProjectTestsExecutionStrategy(
            IProcessExecutorFactory processExecutorFactory,
            string pythonExecutablePath,
            int baseTimeUsed,
            int baseMemoryUsed)
            : base(processExecutorFactory, pythonExecutablePath, baseTimeUsed, baseMemoryUsed)
        {
        }

        protected override IEnumerable<string> ExecutionArguments
            => new[] { IgnorePythonEnvVarsFlag, DontAddUserSiteDirectoryFlag, ModuleFlag, UnitTestModuleName };

        private string TestsDirectoryName => Path.Combine(this.WorkingDirectory, TestsFolderName);

        protected override IExecutionResult<TestResult> ExecuteAgainstTestsInput(
            IExecutionContext<TestsInputModel> executionContext,
            IExecutionResult<TestResult> result)
        {
            this.SaveZipSubmission(executionContext.FileContent, this.WorkingDirectory);
            this.SaveTests(executionContext.Input.Tests.ToList());

            var executor = this.CreateExecutor();
            var checker = executionContext.Input.GetChecker();

            return this.RunTests(executor, checker, executionContext, result);
        }

        private IExecutionResult<TestResult> RunTests(
            IExecutor executor,
            IChecker checker,
            IExecutionContext<TestsInputModel> executionContext,
            IExecutionResult<TestResult> result)
        {
            var tests = executionContext.Input.Tests.ToList();

            for (var i = 0; i < tests.Count; i++)
            {
                var test = tests[i];
                var testPath = this.testPaths[i];

                var processExecutionResult = this.Execute(
                    executionContext,
                    executor,
                    testPath,
                    string.Empty,
                    this.WorkingDirectory);

                var testResult = this.GetTestResult(processExecutionResult, test, checker);

                result.Results.Add(testResult);
            }

            return result;
        }

        private void SaveTests(IList<TestContext> tests)
        {
            Directory.CreateDirectory(this.TestsDirectoryName);
            PythonStrategiesHelper.CreateInitFile(this.TestsDirectoryName);

            this.testPaths = new string[tests.Count];

            for (var i = 0; i < tests.Count; i++)
            {
                var test = tests[i];
                var testName = $"Test_{i}{PythonFileExtension}";
                var testSavePath = Path.Combine(this.TestsDirectoryName, testName);

                this.testPaths[i] = testSavePath;

                File.WriteAllText(testSavePath, test.Input);
            }
        }
    }
}