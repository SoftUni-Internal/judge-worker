namespace OJS.Workers.ExecutionStrategies.Python
{
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;

    using OJS.Workers.Common;
    using OJS.Workers.ExecutionStrategies.Models;
    using OJS.Workers.Executors;

    using static OJS.Workers.Common.Constants;

    public class PythonProjectTestsExecutionStrategy : PythonCodeExecuteAgainstUnitTestsExecutionStrategy
    {
        private const string TestsFolderName = "tests";
        private const string InitFileName = "__init__";
        private const string IgnorePythonEnvVarsFlag = "-E"; // -E and -s are part of -I (isolated mode)
        private const string DontAddUserSiteDirectoryFlag = "-s";
        private const string UnitTestFlag = "-m unittest";

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
            => new[] { IgnorePythonEnvVarsFlag, DontAddUserSiteDirectoryFlag, UnitTestFlag };

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

        protected virtual IExecutionResult<TestResult> RunTests(
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

        protected virtual void SaveTests(IList<TestContext> tests)
        {
            Directory.CreateDirectory(this.TestsDirectoryName);
            var initFilePath = Path.Combine(this.TestsDirectoryName, $"{InitFileName}{PythonFileExtension}");
            File.WriteAllText(initFilePath, string.Empty);

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