namespace OJS.Workers.ExecutionStrategies.Python
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text.RegularExpressions;

    using OJS.Workers.Common;
    using OJS.Workers.ExecutionStrategies.Models;
    using OJS.Workers.Executors;

    using static OJS.Workers.Common.Constants;

    public class PythonProjectUnitTestsExecutionStrategy : PythonUnitTestsExecutionStrategy
    {
        private const string ProjectFolderName = "project";
        private const string ProjectFilesCountPlaceholder = "# project_files_count:";
        private const string ClassNameRegexPattern = @"^class\s+([a-zA-z0-9]+)";
        private const string UpperCaseSplitRegexPattern = @"(?<!^)(?=[A-Z])";

        private const string ProjectFilesNotCapturedCorrectlyErrorMessageTemplate =
            "There should be {0} classes in test #{1}, but found {2}. Ensure the test is correct";

        private readonly string projectFilesCountRegexPattern = $@"^{ProjectFilesCountPlaceholder}\s+([0-9])\s*$";
        private readonly string projectFilesRegexPattern =
            $@"(?:^from\s+[\s\S]+?)?{ClassNameRegexPattern}[\s\S]+?(?=^from|^class)";

        private readonly string projectFilesCountNotSpecifiedInSolutionSkeletonErrorMessage =
            $"Expecting \"{ProjectFilesCountPlaceholder}\" in solution skeleton followed by the number of files that the project has";

        public PythonProjectUnitTestsExecutionStrategy(
            IProcessExecutorFactory processExecutorFactory,
            string pythonExecutablePath,
            int baseTimeUsed,
            int baseMemoryUsed)
            : base(processExecutorFactory, pythonExecutablePath, baseTimeUsed, baseMemoryUsed)
        {
        }

        private string ProjectDirectoryPath => Path.Combine(this.WorkingDirectory, ProjectFolderName);

        protected override IExecutionResult<TestResult> ExecuteAgainstTestsInput(
            IExecutionContext<TestsInputModel> executionContext,
            IExecutionResult<TestResult> result)
        {
            this.SaveZipSubmission(executionContext.FileContent, this.WorkingDirectory);

            var executor = this.CreateExecutor();
            var checker = executionContext.Input.GetChecker();

            return this.RunTests(executor, checker, executionContext, result);
        }

        private static string GetFileNameFromClassName(string className)
            => string.Join(
                "_",
                Regex
                    .Split(className, UpperCaseSplitRegexPattern)
                    .Select(x => x.ToLower()));

        private IExecutionResult<TestResult> RunTests(
            IExecutor executor,
            IChecker checker,
            IExecutionContext<TestsInputModel> executionContext,
            IExecutionResult<TestResult> result)
        {
            Directory.CreateDirectory(this.ProjectDirectoryPath);

            var expectedProjectFilesCount = this.GetProjectFilesCount(executionContext.Input.TaskSkeletonAsString);

            foreach (var test in executionContext.Input.Tests)
            {
                this.SaveTestProjectFiles(expectedProjectFilesCount, test);
            }

            return result;
        }

        private void SaveTestProjectFiles(int expectedFilesCount, TestContext test)
        {
            var projectFilesToBeCreated = this.GetProjectFilesToBeCreated(test);

            if (projectFilesToBeCreated.Count != expectedFilesCount)
            {
                throw new ArgumentException(string.Format(
                    ProjectFilesNotCapturedCorrectlyErrorMessageTemplate,
                    expectedFilesCount,
                    test.Id,
                    projectFilesToBeCreated.Count));
            }

            foreach (var projectFile in projectFilesToBeCreated)
            {
                var fileName = $"{projectFile.Key}{PythonFileExtension}";
                var filePath = Path.Combine(this.ProjectDirectoryPath, fileName);

                File.WriteAllText(filePath, projectFile.Value);
            }
        }

        /// <summary>
        /// Gets the count of the files that need to be generated and put in the projects directory
        /// </summary>
        /// <param name="solutionSkeleton">The skeleton in which this count is written upon task creation</param>
        /// <returns>Number of files that need to be extracted from every test input and saved in the working directory</returns>
        /// <exception cref="ArgumentException">Exception thrown if the count is not given as expected</exception>
        private int GetProjectFilesCount(string solutionSkeleton)
        {
            var regex = new Regex(this.projectFilesCountRegexPattern);
            var projectFilesCountAsString = regex.Match(solutionSkeleton ?? string.Empty).Groups[1].Value;

            if (int.TryParse(projectFilesCountAsString, out var projectFilesCount))
            {
                return projectFilesCount;
            }

            throw new ArgumentException(this.projectFilesCountNotSpecifiedInSolutionSkeletonErrorMessage);
        }

        private Dictionary<string, string> GetProjectFilesToBeCreated(TestContext test)
        {
            var testInput = test.Input;

            var filesRegex = new Regex(this.projectFilesRegexPattern, RegexOptions.Multiline);
            var classNameRegex = new Regex(ClassNameRegexPattern, RegexOptions.Multiline);

            var projectFilesToBeCreated = filesRegex.Matches(testInput)
                .Cast<Match>()
                .ToDictionary(
                    m => GetFileNameFromClassName(m.Groups[1].Value),
                    m => m.Value);

            // removing all matches and leaving the last one
            var lastFileContent = filesRegex.Replace(testInput, string.Empty).Trim();
            var lastClassName = classNameRegex.Match(lastFileContent).Groups[1].Value;
            var lastFileName = GetFileNameFromClassName(lastClassName);

            projectFilesToBeCreated.Add(lastFileName, lastFileContent);

            return projectFilesToBeCreated;
        }
    }
}