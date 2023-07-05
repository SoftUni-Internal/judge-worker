﻿namespace OJS.Workers.ExecutionStrategies.CSharp.DotNetCore
{
    using OJS.Workers.Common;
    using OJS.Workers.Common.Models;
    using OJS.Workers.ExecutionStrategies.Extensions;
    using OJS.Workers.ExecutionStrategies.Models;
    using OJS.Workers.Executors;

    public class DotNetCoreProjectExecutionStrategy : CSharpProjectTestsExecutionStrategy
    {
        protected new const string AdditionalExecutionArguments = "--no-build --no-restore";

        public DotNetCoreProjectExecutionStrategy(
            Func<CompilerType, string> getCompilerPathFunc,
            IProcessExecutorFactory processExecutorFactory,
            int baseTimeUsed,
            int baseMemoryUsed)
            : base(getCompilerPathFunc, processExecutorFactory, baseTimeUsed, baseMemoryUsed)
        {
        }

        protected override IExecutionResult<TestResult> ExecuteAgainstTestsInput(
            IExecutionContext<TestsInputModel> executionContext,
            IExecutionResult<TestResult> result)
        {
            executionContext.SanitizeContent();

            this.SaveZipSubmission(executionContext.FileContent, this.WorkingDirectory);

            var csProjFilePath = this.GetCsProjFilePath();

            var compilerPath = this.GetCompilerPathFunc(executionContext.CompilerType);

            var compilerResult = this.Compile(
                executionContext.CompilerType,
                compilerPath,
                executionContext.AdditionalCompilerArguments,
                csProjFilePath);

            result.IsCompiledSuccessfully = compilerResult.IsCompiledSuccessfully;

            if (!result.IsCompiledSuccessfully)
            {
                result.CompilerComment = compilerResult.CompilerComment;
                return result;
            }

            var executor = this.CreateExecutor(ProcessExecutorType.Restricted);

            var checker = executionContext.Input.GetChecker();

            var arguments = new string[]
            {
                compilerResult.OutputFile,
                AdditionalExecutionArguments
            };

            foreach (var test in executionContext.Input.Tests)
            {
                var processExecutionResult = executor.Execute(
                    compilerPath,
                    test.Input,
                    executionContext.TimeLimit,
                    executionContext.MemoryLimit,
                    arguments,
                    this.WorkingDirectory);

                var testResult = this.CheckAndGetTestResult(
                    test,
                    processExecutionResult,
                    checker,
                    processExecutionResult.ReceivedOutput);

                result.Results.Add(testResult);
            }

            return result;
        }
    }
}
