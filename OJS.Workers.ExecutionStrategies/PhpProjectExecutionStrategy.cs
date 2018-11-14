namespace OJS.Workers.ExecutionStrategies
{
    using System;
    using System.IO;

    using OJS.Workers.Common;
    using OJS.Workers.Common.Helpers;
    using OJS.Workers.ExecutionStrategies.Models;
    using OJS.Workers.Executors;

    public class PhpProjectExecutionStrategy : ExecutionStrategy
    {
        protected const string ZippedSubmissionName = "_$Submission";
        protected const string ApplicationEntryPoint = "index.php";
        private const string SuperGlobalsTemplateName = "_Superglobals.php";
        private const string SuperGlobalsRequireStatementTemplate = "<?php require_once '##templateName##'; ?>";

        public PhpProjectExecutionStrategy(
            string phpCliExecutablePath,
            int baseTimeUsed,
            int baseMemoryUsed)
            : base(baseTimeUsed, baseMemoryUsed)
        {
            if (!File.Exists(phpCliExecutablePath))
            {
                throw new ArgumentException(
                    $"PHP CLI not found in: {phpCliExecutablePath}",
                    nameof(phpCliExecutablePath));
            }

            this.PhpCliExecutablePath = phpCliExecutablePath;
        }

        public string SuperGlobalsTemplatePath => $"{this.WorkingDirectory}\\{SuperGlobalsTemplateName}";

        protected string PhpCliExecutablePath { get; }

        protected override IExecutionResult<TestResult> ExecuteAgainstTestsInput(
            IExecutionContext<TestsInputModel> executionContext)
        {
            var result = new ExecutionResult<TestResult>();

            // PHP code is not compiled
            result.IsCompiledSuccessfully = true;

            string submissionPath =
                $@"{this.WorkingDirectory}\\{ZippedSubmissionName}{Constants.ZipFileExtension}";
            File.WriteAllBytes(submissionPath, executionContext.FileContent);
            FileHelpers.UnzipFile(submissionPath, this.WorkingDirectory);
            File.Delete(submissionPath);

            string applicationEntryPointPath =
                FileHelpers.FindFileMatchingPattern(this.WorkingDirectory, ApplicationEntryPoint);

            if (string.IsNullOrEmpty(applicationEntryPointPath))
            {
                throw new ArgumentException($"{ApplicationEntryPoint} not found in submission folder!");
            }

            this.RequireSuperGlobalsTemplateInUserCode(applicationEntryPointPath);

            var executor = new RestrictedProcessExecutor(this.BaseTimeUsed, this.BaseMemoryUsed);

            var checker = executionContext.Input.GetChecker();

            foreach (var test in executionContext.Input.Tests)
            {
                File.WriteAllText(this.SuperGlobalsTemplatePath, test.Input);

                var processExecutionResult = executor.Execute(
                    this.PhpCliExecutablePath,
                    string.Empty,
                    executionContext.TimeLimit,
                    executionContext.MemoryLimit,
                    new[] { applicationEntryPointPath });

                var testResult = this.ExecuteAndCheckTest(
                    test,
                    processExecutionResult,
                    checker,
                    processExecutionResult.ReceivedOutput);

                result.Results.Add(testResult);
            }

            return result;
        }

        protected void RequireSuperGlobalsTemplateInUserCode(string pathToSubmissionEntryPoint)
        {
            string entryPointContents = File.ReadAllText(pathToSubmissionEntryPoint);

            string requireSuperGlobalsStatement =
                SuperGlobalsRequireStatementTemplate.Replace("##templateName##", SuperGlobalsTemplateName);
            entryPointContents = $"{requireSuperGlobalsStatement}{Environment.NewLine}{entryPointContents}";

            File.WriteAllText(pathToSubmissionEntryPoint, entryPointContents);
        }
    }
}
