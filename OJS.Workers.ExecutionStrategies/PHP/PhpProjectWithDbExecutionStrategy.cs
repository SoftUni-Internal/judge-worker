namespace OJS.Workers.ExecutionStrategies.PHP
{
    using System.IO;

    using OJS.Workers.Common;
    using OJS.Workers.Common.Helpers;
    using OJS.Workers.ExecutionStrategies.Models;
    using OJS.Workers.ExecutionStrategies.Sql.MySql;
    using OJS.Workers.Executors;

    public class PhpProjectWithDbExecutionStrategy : PhpProjectExecutionStrategy
    {
        protected const string DatabaseConfigurationFileName = "db.ini";
        protected const string TestRunnerClassName = "JudgeTestRunner";
        private readonly string restrictedUserId;
        private readonly string restrictedUserPassword;

        public PhpProjectWithDbExecutionStrategy(
            IProcessExecutorFactory processExecutorFactory,
            string phpCliExecutablePath,
            string sysDbConnectionString,
            string restrictedUserId,
            string restrictedUserPassword,
            int baseTimeUsed,
            int baseMemoryUsed)
            : base(processExecutorFactory, phpCliExecutablePath, baseTimeUsed, baseMemoryUsed)
        {
            this.MySqlHelperStrategy = new MySqlPrepareDatabaseAndRunQueriesExecutionStrategy(
                sysDbConnectionString,
                restrictedUserId,
                restrictedUserPassword);

            this.restrictedUserId = restrictedUserId;
            this.restrictedUserPassword = restrictedUserPassword;
        }

        protected string ConnectionStringTemplate => @"dsn=""mysql:host=localhost;port=3306;dbname=##dbName##""
            user=""##username##""
            pass=""##password##""";

        protected string TestRunnerCodeTemplate => @"if(class_exists(""##testRunnerClassName##""))
    \##testRunnerClassName##::test();";

        protected BaseMySqlExecutionStrategy MySqlHelperStrategy { get; set; }

        protected override void ExecuteAgainstTestsInput(
            IExecutionContext<TestsInputModel> executionContext,
            IExecutionResult<TestResult> result)
        {
            var databaseName = this.MySqlHelperStrategy.GetDatabaseName();

            // PHP code is not compiled
            result.IsCompiledSuccessfully = true;

            var submissionPath = this.GetZipFilePath(ZippedSubmissionName);

            File.WriteAllBytes(submissionPath, executionContext.FileContent);
            FileHelpers.UnzipFile(submissionPath, this.WorkingDirectory);
            File.Delete(submissionPath);

            this.ReplaceDatabaseConfigurationFile(databaseName);

            var applicationEntryPointPath = this.AddTestRunnerTemplateToApplicationEntryPoint();

            this.RequireSuperGlobalsTemplateInUserCode(applicationEntryPointPath);

            var executor = this.CreateExecutor(ProcessExecutorType.Restricted);

            var checker = executionContext.Input.GetChecker();

            foreach (var test in executionContext.Input.Tests)
            {
                var dbConnection = this.MySqlHelperStrategy.GetOpenConnection(databaseName);
                dbConnection.Close();

                File.WriteAllText(this.SuperGlobalsTemplatePath, test.Input);

                var processExecutionResult = executor.Execute(
                    this.PhpCliExecutablePath,
                    string.Empty,
                    executionContext.TimeLimit,
                    executionContext.MemoryLimit,
                    new[] { applicationEntryPointPath });

                var testResult = this.CheckAndGetTestResult(
                    test,
                    processExecutionResult,
                    checker,
                    processExecutionResult.ReceivedOutput);

                result.Results.Add(testResult);
                this.MySqlHelperStrategy.DropDatabase(databaseName);
            }
        }

        private string GetZipFilePath(string zipFileName) =>
            $@"{this.WorkingDirectory}\\{zipFileName}{Constants.ZipFileExtension}";

        private string AddTestRunnerTemplateToApplicationEntryPoint()
        {
            var applicationEntryPointPath = FileHelpers.FindFileMatchingPattern(
                this.WorkingDirectory,
                ApplicationEntryPoint);

            var entryPointContent = File.ReadAllText(applicationEntryPointPath);

            var testRunnerCode = this.TestRunnerCodeTemplate.Replace("##testRunnerClassName##", TestRunnerClassName);
            entryPointContent += testRunnerCode;
            File.WriteAllText(applicationEntryPointPath, entryPointContent);

            return applicationEntryPointPath;
        }

        private void ReplaceDatabaseConfigurationFile(string databaseName)
        {
            var databaseConfiguration = this.ConnectionStringTemplate
                .Replace("##dbName##", databaseName)
                .Replace("##username##", this.restrictedUserId)
                .Replace("##password##", this.restrictedUserPassword)
                .Replace(" ", string.Empty);

            var databaseConfigurationFilePath = FileHelpers.FindFileMatchingPattern(
                this.WorkingDirectory,
                DatabaseConfigurationFileName);

            File.WriteAllText(databaseConfigurationFilePath, databaseConfiguration);
        }
    }
}
