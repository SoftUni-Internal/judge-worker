﻿namespace OJS.Workers.ExecutionStrategies.Java
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text.RegularExpressions;

    using OJS.Workers.Common;
    using OJS.Workers.Common.Helpers;
    using OJS.Workers.Common.Models;
    using OJS.Workers.Compilers;
    using OJS.Workers.ExecutionStrategies.Helpers;
    using OJS.Workers.ExecutionStrategies.Models;
    using OJS.Workers.Executors;

    using static OJS.Workers.Common.Constants;
    using static OJS.Workers.ExecutionStrategies.Helpers.JavaStrategiesHelper;

    public class JavaUnitTestsExecutionStrategy : JavaZipFileCompileExecuteAndCheckExecutionStrategy
    {
        protected const string IncorrectTestFormat =
            "The problem's tests were not uploaded as an archive of zips. Reupload the tests in the correct format.";

        protected const string FilenameRegex = @"^//((?:\w+/)*[a-zA-Z_][a-zA-Z_0-9]*\.java)";

        protected const string JUnitRunnerClassName = "_$TestRunner";

        protected const string AdditionalExecutionArguments = "-Dfile.encoding=UTF-8 -Xms16m -Xmx256m";

        protected const string TestResultsRegex = @"Total Tests: (\d+) Successful: (\d+) Failed: (\d+)";

        public JavaUnitTestsExecutionStrategy(
            Func<CompilerType, string> getCompilerPathFunc,
            IProcessExecutorFactory processExecutorFactory,
            string javaExecutablePath,
            string javaLibrariesPath,
            int baseTimeUsed,
            int baseMemoryUsed)
            : base(
                getCompilerPathFunc,
                processExecutorFactory,
                javaExecutablePath,
                javaLibrariesPath,
                baseTimeUsed,
                baseMemoryUsed)
            => this.TestNames = new List<string>();

        protected string JUnitTestRunnerSourceFilePath =>
            FileHelpers.BuildPath(this.WorkingDirectory, $"{JUnitRunnerClassName}{JavaSourceFileExtension}");

        protected List<string> TestNames { get; }

        protected override string ClassPathArgument => $@" -classpath ""{this.JavaLibrariesPath}*""";

        protected virtual string JUnitTestRunnerCode
        {
            get => $@"
import org.junit.runner.JUnitCore;
import org.junit.runner.Result;

import java.io.ByteArrayInputStream;
import java.io.ByteArrayOutputStream;
import java.io.InputStream;
import java.io.PrintStream;

import java.util.ArrayList;
import java.util.List;

public class _$TestRunner {{
    public static void main(String[] args) {{
        Class[] testClasses = new Class[]{{{string.Join(", ", this.TestNames.Select(x => x.Replace(".java", ".class").Replace("/", ".")))}}};

        InputStream originalIn = System.in;
        PrintStream originalOut = System.out;

        InputStream in = new ByteArrayInputStream(new byte[0]);
        System.setIn(in);

        ByteArrayOutputStream out = new ByteArrayOutputStream();
        System.setOut(new PrintStream(out));

        List<Result> results = new ArrayList<>();
        for (Class testClass: testClasses) {{
            results.add(JUnitCore.runClasses(testClass));
        }}

        System.setIn(originalIn);
        System.setOut(originalOut);

        int total = 0;
        int successful = 0;
        int failed = 0;
        for (Result result : results){{
            if(result.wasSuccessful()){{
                successful += result.getRunCount();
            }}
            else{{
                successful += result.getRunCount() - result.getFailureCount();
                failed += result.getFailureCount();
                System.out.println(result.getFailures().toString());
            }}

            total += result.getRunCount();
        }}

        System.out.printf(""Total Tests: %d Successful: %d Failed: %d"", total, successful, failed);
    }}
}}";
        }

        protected override IExecutionResult<TestResult> ExecuteAgainstTestsInput(
            IExecutionContext<TestsInputModel> executionContext,
            IExecutionResult<TestResult> result)
        {
            string submissionFilePath;

            try
            {
                submissionFilePath = this.CreateSubmissionFile(executionContext);
            }
            catch (ArgumentException exception)
            {
                result.IsCompiledSuccessfully = false;
                result.CompilerComment = exception.Message;

                return result;
            }

            FileHelpers.UnzipFile(submissionFilePath, this.WorkingDirectory);
            File.Delete(submissionFilePath);

            var executor = this.CreateExecutor(ProcessExecutorType.Restricted);

            var checker = executionContext.Input.GetChecker();

            var originalTestsPassed = int.MaxValue;
            var count = 0;

            var tests = executionContext.Input.Tests.OrderBy(x => x.IsTrialTest).ThenBy(x => x.OrderBy);

            foreach (var test in tests)
            {
                var testFileNames = this.ExtractFileNames(test.Input)
                    .ToList();
                if (testFileNames.Any(string.IsNullOrEmpty))
                {
                    result.IsCompiledSuccessfully = false;
                    result.CompilerComment = IncorrectTestFormat;
                    return result;
                }

                var compilerResult = this.CompileProject(executionContext);
                result.IsCompiledSuccessfully = compilerResult.IsCompiledSuccessfully;
                result.CompilerComment = compilerResult.CompilerComment;
                if (!result.IsCompiledSuccessfully)
                {
                    return result;
                }

                var classPathWithCompiledFile = $@" -classpath ""{this.JavaLibrariesPath}*{ClassPathArgumentSeparator}{compilerResult.OutputFile}""";

                var arguments = new List<string>
                {
                    classPathWithCompiledFile,
                    AdditionalExecutionArguments,
                    JUnitRunnerClassName,
                };

                // Process the submission and check each test
                var processExecutionResult = executor.Execute(
                    this.JavaExecutablePath,
                    string.Empty,
                    executionContext.TimeLimit,
                    executionContext.MemoryLimit,
                    arguments,
                    this.WorkingDirectory,
                    true);

                JavaStrategiesHelper.ValidateJvmInitialization(processExecutionResult.ReceivedOutput);

                // Construct and figure out what the Test results are
                this.ExtractTestResult(processExecutionResult.ReceivedOutput, out var passedTests, out var totalTests);

                var message = TestPassedMessage;

                if (totalTests == 0)
                {
                    message = "No tests found";
                }
                else if (passedTests >= originalTestsPassed)
                {
                    message = "No functionality covering this test!";
                }

                if (count == 0)
                {
                    originalTestsPassed = passedTests;
                    if (totalTests != passedTests)
                    {
                        message = "Not all tests passed on the correct solution.";
                    }
                }

                var testResult = this.CheckAndGetTestResult(
                    test,
                    processExecutionResult,
                    checker,
                    message);

                result.Results.Add(testResult);
                count++;
            }

            return result;
        }

        protected override CompileResult Compile(
            CompilerType compilerType,
            string compilerPath,
            string compilerArguments,
            string submissionFilePath,
            bool useWorkingDirectoryForProcess = false)
        {
            if (compilerType == CompilerType.None)
            {
                return new CompileResult(true, null) { OutputFile = submissionFilePath };
            }

            if (!File.Exists(compilerPath))
            {
                throw new ArgumentException($"Compiler not found in: {compilerPath}", nameof(compilerPath));
            }

            var compiler = Compiler.CreateCompiler(compilerType);
            var compilerResult = compiler.Compile(compilerPath, submissionFilePath, compilerArguments);
            return compilerResult;
        }

        protected override string CreateSubmissionFile<TInput>(IExecutionContext<TInput> executionContext)
        {
            var trimmedAllowedFileExtensions = executionContext.AllowedFileExtensions?.Trim();
            var allowedFileExtensions = (!trimmedAllowedFileExtensions?.StartsWith(".") ?? false)
                ? $".{trimmedAllowedFileExtensions}"
                : trimmedAllowedFileExtensions;

            if (allowedFileExtensions != ZipFileExtension)
            {
                throw new ArgumentException("Submission file is not a zip file!");
            }

            return this.PrepareSubmissionFile((IExecutionContext<TestsInputModel>)executionContext);
        }

        protected virtual string PrepareSubmissionFile(IExecutionContext<TestsInputModel> context)
        {
            var submissionFilePath = FileHelpers.BuildPath(this.WorkingDirectory, SubmissionFileName);
            File.WriteAllBytes(submissionFilePath, context.FileContent);
            FileHelpers.RemoveFilesFromZip(submissionFilePath, RemoveMacFolderPattern);
            this.ExtractUserTestFiles(submissionFilePath);
            this.AddTestRunnerTemplate(submissionFilePath);
            return submissionFilePath;
        }

        private void AddTestRunnerTemplate(string submissionFilePath)
        {
            // It is important to call the JUintTestRunnerCodeTemplate after the TestClasses have been filled
            // otherwise no tests will be queued in the JUnitTestRunner, which would result in no tests failing.
            File.WriteAllText(this.JUnitTestRunnerSourceFilePath, this.JUnitTestRunnerCode);
            FileHelpers.AddFilesToZipArchive(submissionFilePath, string.Empty, this.JUnitTestRunnerSourceFilePath);
            FileHelpers.DeleteFiles(this.JUnitTestRunnerSourceFilePath);
        }

        private void ExtractUserTestFiles(string submissionFilePath)
        {
            var fileNames = FileHelpers.GetFilePathsFromZip(submissionFilePath)
                .Where(x => x.EndsWith(JavaSourceFileExtension));
            this.TestNames.AddRange(fileNames);
        }

        private void ExtractTestResult(string receivedOutput, out int passedTests, out int totalTests)
        {
            var testResultsRegex = new Regex(TestResultsRegex);
            var res = testResultsRegex.Match(receivedOutput);
            if (!res.Success)
            {
                throw new ArgumentException("Process output was incorrect or empty.");
            }

            totalTests = int.Parse(res.Groups[1].Value);
            passedTests = int.Parse(res.Groups[2].Value);
        }

        private IEnumerable<string> ExtractFileNames(string testInput)
            => testInput.Split(new[] { ClassDelimiterUnix, ClassDelimiterWin }, StringSplitOptions.RemoveEmptyEntries)
                .Select(this.PrepareFileFromClassname);

        private string PrepareFileFromClassname(string projectClass)
        {
            var name = Regex.Match(projectClass, FilenameRegex, RegexOptions.Multiline);
            if (!name.Success)
            {
                return null;
            }

            var filename = FileHelpers.BuildPath(name.Groups[1].Value.Split('/', '\\'));
            var filepath = FileHelpers.BuildPath(this.WorkingDirectory, filename);
            var className = filename.Replace('/', '.')
                .Replace(".java", string.Empty);

            var directory = Path.GetDirectoryName(filepath);
            if (!Directory.Exists(directory) && !string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.WriteAllText(filepath, projectClass);
            return filename;
        }

        private CompileResult CompileProject(IExecutionContext<TestsInputModel> executionContext)
        {
            var compilerPath = this.GetCompilerPathFunc(executionContext.CompilerType);
            var combinedArguments = executionContext.AdditionalCompilerArguments + this.ClassPathArgument;

            return this.Compile(
                executionContext.CompilerType,
                compilerPath,
                combinedArguments,
                this.WorkingDirectory);
        }
    }
}
