﻿namespace OJS.Workers.ExecutionStrategies.CSharp.DotNetCore.V3
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text.RegularExpressions;
    using OJS.Workers.Common;
    using OJS.Workers.Common.Exceptions;
    using OJS.Workers.Common.Helpers;
    using OJS.Workers.Common.Models;
    using OJS.Workers.ExecutionStrategies.Extensions;
    using OJS.Workers.ExecutionStrategies.Helpers;
    using OJS.Workers.ExecutionStrategies.Models;
    using OJS.Workers.Executors;

    public class DotNetCoreUnitTestsExecutionStrategy : DotNetCoreProjectTestsExecutionStrategy
    {
        private readonly IEnumerable<string> packageNamesToRemoveFromUserCsProjFile = new[]
        {
            "NUnit",
            "NUnitLite",
            "Microsoft.EntityFrameworkCore.InMemory"
        };

        private readonly string csFileSearchPattern = $"*{Constants.CSharpFileExtension}";

        private string nUnitLiteConsoleAppCsProjTemplate;

        public DotNetCoreUnitTestsExecutionStrategy(
            Func<CompilerType, string> getCompilerPathFunc,
            IProcessExecutorFactory processExecutorFactory,
            int baseTimeUsed,
            int baseMemoryUsed,
            string targetFrameworkName,
            string microsoftEntityFrameworkCoreInMemoryVersion,
            string microsoftEntityFrameworkCoreProxiesVersion)
            : base(
                getCompilerPathFunc,
                processExecutorFactory,
                baseTimeUsed,
                baseMemoryUsed,
                targetFrameworkName,
                microsoftEntityFrameworkCoreInMemoryVersion,
                microsoftEntityFrameworkCoreProxiesVersion)
        {
        }

        protected override IExecutionResult<TestResult> ExecuteAgainstTestsInput(
            IExecutionContext<TestsInputModel> executionContext,
            IExecutionResult<TestResult> result)
        {
            executionContext.SanitizeContent();

            Directory.CreateDirectory(this.NUnitLiteConsoleAppDirectory);
            Directory.CreateDirectory(this.UserProjectDirectory);

            this.SaveZipSubmission(executionContext.FileContent, this.UserProjectDirectory);

            this.MoveUserCsFilesToNunitLiteConsoleAppFolder();

            var userCsProjPath = this.RemoveUnwantedReferencesFromUserCsProjFile();

            var nunitLiteConsoleApp = this.CreateNUnitLiteConsoleApp(new List<string> { userCsProjPath });

            this.nUnitLiteConsoleAppCsProjTemplate = nunitLiteConsoleApp.csProjTemplate;

            var executor = this.CreateExecutor(ProcessExecutorType.Restricted);

            return this.RunUnitTests(
                nunitLiteConsoleApp.csProjPath,
                executionContext,
                executor,
                executionContext.Input.GetChecker(),
                result,
                string.Empty,
                AdditionalExecutionArguments);
        }

        protected override IExecutionResult<TestResult> RunUnitTests(
            string consoleRunnerPath,
            IExecutionContext<TestsInputModel> executionContext,
            IExecutor executor,
            IChecker checker,
            IExecutionResult<TestResult> result,
            string csProjFilePath,
            string additionalExecutionArguments)
        {
            var additionalExecutionArgumentsArray = additionalExecutionArguments
                .Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

            var compilerPath = this.GetCompilerPathFunc(executionContext.CompilerType);
            var testedCodePath = FileHelpers.BuildPath(
                this.NUnitLiteConsoleAppDirectory,
                UnitTestStrategiesHelper.TestedCodeFileNameWithExtension);
            var originalTestsPassed = -1;

            var tests = executionContext.Input.Tests.OrderBy(x => x.IsTrialTest).ThenBy(x => x.OrderBy).ToList();

            for (var i = 0;  i < tests.Count; i++)
            {
                var test = tests[i];

                this.SaveSetupFixture(this.NUnitLiteConsoleAppDirectory);

                File.WriteAllText(testedCodePath, test.Input);

                // Compiling
                var compilerResult = this.Compile(
                    executionContext.CompilerType,
                    compilerPath,
                    executionContext.AdditionalCompilerArguments,
                    consoleRunnerPath);

                result.IsCompiledSuccessfully = compilerResult.IsCompiledSuccessfully;
                result.CompilerComment = compilerResult.CompilerComment;

                if (!compilerResult.IsCompiledSuccessfully)
                {
                    return result;
                }

                // Delete tests before execution so the user can't acces them
                FileHelpers.DeleteFiles(testedCodePath, this.SetupFixturePath);

                var arguments = new List<string> { compilerResult.OutputFile };
                arguments.AddRange(additionalExecutionArgumentsArray);

                var processExecutionResult = executor.Execute(
                    compilerPath,
                    string.Empty,
                    executionContext.TimeLimit,
                    executionContext.MemoryLimit,
                    arguments,
                    workingDirectory: null,
                    useProcessTime: false,
                    useSystemEncoding: true);

                if (!string.IsNullOrWhiteSpace(processExecutionResult.ErrorOutput))
                {
                    throw new InvalidProcessExecutionOutputException(processExecutionResult.ErrorOutput);
                }

                var testResultsRegex = new Regex(TestResultsRegex);

                var processExecutionTestResult = UnitTestStrategiesHelper.GetTestResult(
                    processExecutionResult.ReceivedOutput,
                    testResultsRegex,
                    originalTestsPassed,
                    i == 0,
                    this.ExtractTotalAndPassedTestsCount);

                var message = processExecutionTestResult.message;
                originalTestsPassed = processExecutionTestResult.originalTestsPassed;

                var testResult = this.CheckAndGetTestResult(test, processExecutionResult, checker, message);
                result.Results.Add(testResult);

                if (i < tests.Count - 1)
                {
                    // Recreate NUnitLite Console App .csproj file, deleted after compilation, to compile again
                    this.CreateNUnitLiteConsoleAppCsProjFile(this.nUnitLiteConsoleAppCsProjTemplate);
                }
            }

            return result;
        }

        private void MoveUserCsFilesToNunitLiteConsoleAppFolder()
        {
            var userCsFiles = FileHelpers
                .FindAllFilesMatchingPattern(this.UserProjectDirectory, this.csFileSearchPattern)
                .Select(f => new FileInfo(f));

            foreach (var userFile in userCsFiles)
            {
                var destination = userFile.FullName
                    .Replace(this.UserProjectDirectory, this.NUnitLiteConsoleAppDirectory);

                new FileInfo(destination).Directory?.Create();
                File.Move(userFile.FullName, destination);
            }
        }

        private string RemoveUnwantedReferencesFromUserCsProjFile()
        {
            var userCsProjFiles = FileHelpers
                .FindAllFilesMatchingPattern(this.UserProjectDirectory, CsProjFileSearchPattern)
                .ToList();

            if (userCsProjFiles.Count != 1)
            {
                throw new ArgumentException("The submission should have exactly one .csproj file.");
            }

            var csProjPath = userCsProjFiles.First();

            DotNetCoreStrategiesHelper.RemoveAllProjectReferencesFromCsProj(csProjPath);

            DotNetCoreStrategiesHelper.RemovePackageReferencesFromCsProj(
                csProjPath,
                this.packageNamesToRemoveFromUserCsProjFile);

            return csProjPath;
        }
    }
}
