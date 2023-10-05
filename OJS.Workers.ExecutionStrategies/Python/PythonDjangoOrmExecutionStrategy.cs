namespace OJS.Workers.ExecutionStrategies.Python
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text.RegularExpressions;
    using OJS.Workers.Common;
    using OJS.Workers.Common.Helpers;
    using OJS.Workers.ExecutionStrategies.Models;
    using OJS.Workers.Executors;

    public class PythonDjangoOrmExecutionStrategy : PythonProjectTestsExecutionStrategy
    {
        private const string VirtualEnvName = "env";
        private const string ProjectSettingsFolder = "orm_skeleton";
        private const string SettingsFileName = "settings.py";
        private const string PyenvAppFileName = "pyenv";
        private const string RequirementsFileName = "requirements.txt";

        private const string InvalidProjectStructureErrorMessage =
            "Folder project structure is invalid! Please check your zip file! It should contain requirements.txt in root of the zip and {0}/settings.py";

        private const string DatabaseConfigRegexPattern = @"(?:^|^\n\s*)DATABASES\s*=\s*\{[\s\S]*?\}\s*(?=\n{1,2}#|\n{2,}|\Z)(?!\s*\Z)";
        private const string TestResultsRegexPattern = @"(FAIL|OK)";
        private const string SuccessTestsRegexPattern = @"^\s*OK\s*$";

        private const string SqlLiteConfig =
            "DATABASES = {\n    'default': {\n        'ENGINE': 'django.db.backends.sqlite3',\n        'NAME': 'db.sqlite3',\n    }\n}\n";

        private readonly string pipExecutablePath;
        private readonly int installPackagesTimeUsed;

        public PythonDjangoOrmExecutionStrategy(
            IProcessExecutorFactory processExecutorFactory,
            string pythonExecutablePath,
            string pipExecutablePath,
            int baseTimeUsed,
            int baseMemoryUsed,
            int installPackagesTimeUsed)
            : base(
            processExecutorFactory, pythonExecutablePath, baseTimeUsed, baseMemoryUsed)
        {
            this.pipExecutablePath = pipExecutablePath ?? throw new ArgumentNullException(nameof(pipExecutablePath));
            this.installPackagesTimeUsed = installPackagesTimeUsed;
        }

        protected override Regex TestsRegex => new Regex(TestResultsRegexPattern, RegexOptions.Multiline);

        protected override Regex SuccessTestsRegex => new Regex(SuccessTestsRegexPattern, RegexOptions.Multiline);

        protected override IEnumerable<string> ExecutionArguments
            => Enumerable.Empty<string>();

        protected override IExecutionResult<TestResult> ExecuteAgainstTestsInput(
            IExecutionContext<TestsInputModel> executionContext,
            IExecutionResult<TestResult> result)
        {
            this.SaveZipSubmission(executionContext.FileContent, this.WorkingDirectory);
            var requirementsFilePath = this.WorkingDirectory + Path.DirectorySeparatorChar + RequirementsFileName;
            var pathToSettingsFile = this.WorkingDirectory + Path.DirectorySeparatorChar + ProjectSettingsFolder +
                                     Path.DirectorySeparatorChar + SettingsFileName;

            if (!File.Exists(requirementsFilePath) || !File.Exists(pathToSettingsFile))
            {
                throw new ArgumentException(string.Format(InvalidProjectStructureErrorMessage, ProjectSettingsFolder));
            }

            var executor = this.CreateExecutor();
            var checker = executionContext.Input.GetChecker();

            this.CreateVirtualEnvironment(executor, executionContext);
            this.ActivateVirtualEnvironment(executor, executionContext);
            this.ChangeDbConnection(pathToSettingsFile);
            this.RestorePackages(executor, executionContext);
            this.ExportDjangoSettingsModule(executor, executionContext);
            this.ApplyMigrations(executor, executionContext);

            this.RunTests(string.Empty, executor, checker, executionContext, result);
            this.DeleteVirtualEnvironment(executor, executionContext);

            return result;
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

                var processExecutionResult = this.ExecuteTest(executor, executionContext, testPath);

                var testResult = this.GetTestResult(processExecutionResult, test, checker);

                result.Results.Add(testResult);
            }

            return result;
        }

        private ProcessExecutionResult ExecuteTest(
            IExecutor executor,
            IExecutionContext<TestsInputModel> executionContext,
            string testPath)
        {
            var processExecutionResult = this.Execute(
                this.PythonExecutablePath,
                this.ExecutionArguments.Concat(new[]
                {
                    $"manage.py test --pattern=\"{testPath.Split(Path.DirectorySeparatorChar).Last()}\"",
                }), executor,
                executionContext);

            this.FixReceivedOutput(processExecutionResult);
            return processExecutionResult;
        }

        private void RestorePackages(IExecutor executor, IExecutionContext<TestsInputModel> executionContext)
        {
            var result = this.Execute(
                this.pipExecutablePath,
                this.ExecutionArguments.Concat(new[] { $"install -r {RequirementsFileName}" }),
                executor,
                executionContext,
                this.installPackagesTimeUsed);

            if (result.ExitCode == 0)
            {
                return;
            }

            throw new ArgumentException($"Failed to restore packages! " + this.GetErrorOutput(result));
        }

        private void CreateVirtualEnvironment(IExecutor executor, IExecutionContext<TestsInputModel> executionContext)
        {
            this.DeleteVirtualEnvironment(executor, executionContext);
            var result = this.Execute(
                PyenvAppFileName,
                this.ExecutionArguments.Concat(new[] { $"virtualenv 3.11 {VirtualEnvName}" }),
                executor,
                executionContext);

            if (result.ExitCode == 0)
            {
                return;
            }

            throw new ArgumentException($"Failed to create virtual environment! {this.GetErrorOutput(result)}");
        }

        private void ActivateVirtualEnvironment(IExecutor executor, IExecutionContext<TestsInputModel> executionContext)
        {
            var result = this.Execute(
                PyenvAppFileName,
                this.ExecutionArguments.Concat(new[] { $"local {VirtualEnvName}" }),
                executor,
                executionContext);

            if (result.ExitCode == 0)
            {
                return;
            }

            throw new ArgumentException("Failed to activate virtual environment! " + this.GetErrorOutput(result));
        }

        private void DeleteVirtualEnvironment(IExecutor executor, IExecutionContext<TestsInputModel> executionContext)
            => this.Execute(
                PyenvAppFileName,
                this.ExecutionArguments.Concat(new[] { $"virtualenv-delete {VirtualEnvName}" }),
                executor,
                executionContext,
                executionContext.TimeLimit,
                "y");

        private void ExportDjangoSettingsModule(IExecutor executor, IExecutionContext<TestsInputModel> executionContext)
        {
            var result = this.Execute(
                "/bin/bash",
                this.ExecutionArguments.Concat(new[] { $"-c export DJANGO_SETTINGS_MODULE={VirtualEnvName}.settings" }),
                executor,
                executionContext);

            if (result.ExitCode == 0)
            {
                return;
            }

            throw new ArgumentException("Failed to export DJANGO_SETTINGS_MODULE! " + this.GetErrorOutput(result));
        }

        private void ApplyMigrations(IExecutor executor, IExecutionContext<TestsInputModel> executionContext)
        {
            var result = this.Execute(
                this.PythonExecutablePath,
                this.ExecutionArguments.Concat(new[] { "manage.py migrate" }),
                executor,
                executionContext);

            if (result.ExitCode == 0)
            {
                return;
            }

            throw new ArgumentException("Failed to apply migrations! " + this.GetErrorOutput(result));
        }

        private void ChangeDbConnection(string pathToSettingsFile, string pattern = DatabaseConfigRegexPattern, string replacement = SqlLiteConfig)
        {
            var settingsContent = File.ReadAllText(pathToSettingsFile);

            var newSettingsContent = Regex.Replace(
                settingsContent,
                pattern,
                replacement,
                RegexOptions.Multiline);

            FileHelpers.WriteAllText(pathToSettingsFile, newSettingsContent);
        }

        private string GetErrorOutput(ProcessExecutionResult result)
            => $"Error output: {result.ReceivedOutput + Environment.NewLine + result.ErrorOutput} and result type: {result.Type}";

        private ProcessExecutionResult Execute(
            string fileName,
            IEnumerable<string> arguments,
            IExecutor executor,
            IExecutionContext<TestsInputModel> executionContext,
            int timeLimit = 0,
            string inputData = "")
            => executor.Execute(
                fileName,
                inputData,
                timeLimit == 0 ? executionContext.TimeLimit : timeLimit,
                executionContext.MemoryLimit,
                arguments,
                this.WorkingDirectory,
                false,
                true);
    }
}