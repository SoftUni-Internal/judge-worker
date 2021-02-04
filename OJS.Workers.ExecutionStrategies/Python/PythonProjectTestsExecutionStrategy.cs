namespace OJS.Workers.ExecutionStrategies.Python
{
    using System.Collections.Generic;
    using System.Linq;

    using OJS.Workers.Common;
    using OJS.Workers.Common.Helpers;
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
            => new[]
            {
                IgnorePythonEnvVarsArgument,
                DontAddUserSiteDirectoryArgument,
                ModuleNameArgument,
                UnitTestModuleName,
            };

        protected override IExecutionResult<TestResult> ExecuteAgainstTestsInput(
            IExecutionContext<TestsInputModel> executionContext,
            IExecutionResult<TestResult> result)
        {
            this.SaveZipSubmission(executionContext.FileContent, this.WorkingDirectory);

            var executor = this.CreateExecutor();
            var checker = executionContext.Input.GetChecker();

            return this.RunTests(string.Empty, executor, checker, executionContext, result);
        }

        protected override IExecutionResult<TestResult> RunTests(
            string codeSavePath,
            IExecutor executor,
            IChecker checker,
            IExecutionContext<TestsInputModel> executionContext,
            IExecutionResult<TestResult> result)
        {
            var tests = executionContext.Input.Tests.ToList();

            this.SaveTests(tests);

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

        /// <summary>
        /// Saves all tests from the execution context as separate files in tests directory.
        /// Full paths to the files are preserved in a private field.
        /// </summary>
        /// <param name="tests">All tests from the execution context</param>
        private void SaveTests(IList<TestContext> tests)
        {
            var testsDirectoryName = FileHelpers.BuildPath(this.WorkingDirectory, TestsFolderName);

            this.testPaths = new string[tests.Count];

            for (var i = 0; i < tests.Count; i++)
            {
                var test = tests[i];
                var testFileName = $"test_{i}{PythonFileExtension}";
                var testSavePath = FileHelpers.BuildPath(testsDirectoryName, testFileName);

                PythonStrategiesHelper.CreateFileInPackage(testSavePath, test.Input);

                this.testPaths[i] = testSavePath;
            }
        }
    }
}