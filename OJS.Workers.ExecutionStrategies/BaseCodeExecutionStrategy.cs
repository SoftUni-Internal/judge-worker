﻿namespace OJS.Workers.ExecutionStrategies
{
    using System;

    using OJS.Workers.Common;
    using OJS.Workers.Common.Extensions;
    using OJS.Workers.Common.Helpers;
    using OJS.Workers.Common.Models;
    using OJS.Workers.ExecutionStrategies.Models;
    using OJS.Workers.Executors;

    public class BaseCodeExecutionStrategy : BaseExecutionStrategy
    {
        protected const string RemoveMacFolderPattern = "__MACOSX/*";

        protected const string ZippedSubmissionName = "Submission.zip";

        protected readonly IProcessExecutorFactory ProcessExecutorFactory;

        protected BaseCodeExecutionStrategy(
            IProcessExecutorFactory processExecutorFactory,
            int baseTimeUsed,
            int baseMemoryUsed)
        {
            this.ProcessExecutorFactory = processExecutorFactory;
            this.BaseTimeUsed = baseTimeUsed;
            this.BaseMemoryUsed = baseMemoryUsed;
        }

        protected int BaseTimeUsed { get; }

        protected int BaseMemoryUsed { get; }

        protected IExecutor CreateExecutor(ProcessExecutorType processExecutorType)
            => this.ProcessExecutorFactory
                .CreateProcessExecutor(this.BaseTimeUsed, this.BaseMemoryUsed, processExecutorType);

        protected virtual string SaveCodeToTempFile<TINput>(IExecutionContext<TINput> executionContext)
            => string.IsNullOrEmpty(executionContext.AllowedFileExtensions)
                ? FileHelpers.SaveStringToTempFile(this.WorkingDirectory, executionContext.Code)
                : FileHelpers.SaveByteArrayToTempFile(this.WorkingDirectory, executionContext.FileContent);

        protected void SaveZipSubmission(byte[] submissionContent, string directory)
        {
            var submissionFilePath = FileHelpers.BuildPath(directory, ZippedSubmissionName);
            FileHelpers.WriteAllBytes(submissionFilePath, submissionContent);
            FileHelpers.RemoveFilesFromZip(submissionFilePath, RemoveMacFolderPattern);
            FileHelpers.UnzipFile(submissionFilePath, directory);
            FileHelpers.DeleteFile(submissionFilePath);
        }

        protected TestResult CheckAndGetTestResult(
            TestContext test,
            ProcessExecutionResult processExecutionResult,
            IChecker checker,
            string receivedOutput)
        {
            var testResult = new TestResult
            {
                Id = test.Id,
                TimeUsed = (int)processExecutionResult.TimeWorked.TotalMilliseconds,
                MemoryUsed = (int)processExecutionResult.MemoryUsed,
                IsTrialTest = test.IsTrialTest,
            };

            if (processExecutionResult.Type == ProcessExecutionResultType.RunTimeError)
            {
                testResult.ResultType = TestRunResultType.RunTimeError;
                testResult.ExecutionComment = processExecutionResult.ErrorOutput.MaxLength(2048); // Trimming long error texts
            }
            else if (processExecutionResult.Type == ProcessExecutionResultType.TimeLimit)
            {
                testResult.ResultType = TestRunResultType.TimeLimit;
            }
            else if (processExecutionResult.Type == ProcessExecutionResultType.MemoryLimit)
            {
                testResult.ResultType = TestRunResultType.MemoryLimit;
            }
            else if (processExecutionResult.Type == ProcessExecutionResultType.Success)
            {
                var checkerResult = checker.Check(test.Input, receivedOutput, test.Output, test.IsTrialTest);

                testResult.ResultType = checkerResult.IsCorrect
                    ? TestRunResultType.CorrectAnswer
                    : TestRunResultType.WrongAnswer;

                // TODO: Do something with checkerResult.ResultType
                testResult.CheckerDetails = checkerResult.CheckerDetails;
            }
            else
            {
                throw new ArgumentOutOfRangeException(
                    nameof(processExecutionResult),
                    "Invalid ProcessExecutionResultType value.");
            }

            testResult.Input = test.Input;

            return testResult;
        }

        protected OutputResult GetOutputResult(ProcessExecutionResult processExecutionResult)
            => new OutputResult
            {
                TimeUsed = (int)processExecutionResult.TimeWorked.TotalMilliseconds,
                MemoryUsed = (int)processExecutionResult.MemoryUsed,
                ResultType = processExecutionResult.Type,
                Output = string.IsNullOrWhiteSpace(processExecutionResult.ErrorOutput)
                    ? processExecutionResult.ReceivedOutput
                    : processExecutionResult.ErrorOutput
            };
    }
}
