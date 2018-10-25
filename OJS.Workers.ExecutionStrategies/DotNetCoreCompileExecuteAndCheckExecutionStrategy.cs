namespace OJS.Workers.ExecutionStrategies
{
    using System;
    using System.IO;
    using System.Linq;

    using OJS.Workers.Common;
    using OJS.Workers.Common.Models;
    using OJS.Workers.ExecutionStrategies.Models;
    using OJS.Workers.Executors;

    public class DotNetCoreCompileExecuteAndCheckExecutionStrategy : ExecutionStrategy
    {
        private const string RuntimeConfigJsonTemplate = @"
            {
	            ""runtimeOptions"": {
                    ""framework"": {
                        ""name"": ""Microsoft.NETCore.App"",
                        ""version"": ""2.0.5""
                    }
                }
            }";

        public DotNetCoreCompileExecuteAndCheckExecutionStrategy(
            Func<CompilerType, string> getCompilerPathFunc,
            int baseTimeUsed,
            int baseMemoryUsed)
            : base(baseTimeUsed, baseMemoryUsed) =>
                this.GetCompilerPathFunc = getCompilerPathFunc;

        protected Func<CompilerType, string> GetCompilerPathFunc { get; }

        protected override IExecutionResult<TestResult> ExecuteAgainstTestsInput(
            IExecutionContext<TestsInputModel> executionContext)
        {
            var result = new ExecutionResult<TestResult>();

            var isCompiledSuccessfully = this.ExecuteCompiling(
                executionContext,
                this.GetCompilerPathFunc,
                result,
                out var compilerResult);

            if (!isCompiledSuccessfully)
            {
                return result;
            }

            var executor = this.PrepareExecutor(
                compilerResult,
                executionContext,
                out var arguments,
                out var compilerPath);

            var checker = executionContext.Input.GetChecker();

            foreach (var test in executionContext.Input.Tests)
            {
                var processExecutionResult = executor.Execute(
                    compilerPath,
                    test.Input,
                    executionContext.TimeLimit,
                    executionContext.MemoryLimit,
                    arguments,
                    this.WorkingDirectory);

                var testResult = this.ExecuteAndCheckTest(
                    test,
                    processExecutionResult,
                    checker,
                    processExecutionResult.ReceivedOutput);

                result.Results.Add(testResult);
            }

            return result;
        }

        protected override IExecutionResult<OutputResult> ExecuteAgainstSimpleInput(
            IExecutionContext<string> executionContext)
        {
            var result = new ExecutionResult<OutputResult>();

            var isCompiledSuccessfully = this.ExecuteCompiling(
                executionContext,
                this.GetCompilerPathFunc,
                result,
                out var compilerResult);

            if (!isCompiledSuccessfully)
            {
                return result;
            }

            var executor = this.PrepareExecutor(
                compilerResult,
                executionContext,
                out var arguments,
                out var compilerPath);

            var processExecutionResult = executor.Execute(
                compilerPath,
                executionContext.Input ?? string.Empty,
                executionContext.TimeLimit,
                executionContext.MemoryLimit,
                arguments,
                this.WorkingDirectory);

            var outputResult = this.GetOutputResult(processExecutionResult);

            result.Results.Add(outputResult);

            return result;
        }

        private IExecutor PrepareExecutor<TInput>(
            CompileResult compileResult,
            IExecutionContext<TInput> executionContext,
            out string[] arguments,
            out string compilerPath)
        {
            var executor = new RestrictedProcessExecutor(this.BaseTimeUsed, this.BaseMemoryUsed);

            arguments = new[]
            {
                compileResult.OutputFile
            };

            compilerPath = this.GetCompilerPathFunc(executionContext.CompilerType);

            this.CreateRuntimeConfigJsonFile(this.WorkingDirectory, RuntimeConfigJsonTemplate);

            return executor;
        }

        private void CreateRuntimeConfigJsonFile(string directory, string text)
        {
            var compiledFileName = Directory
                .GetFiles(directory)
                .Select(Path.GetFileNameWithoutExtension)
                .First();

            var jsonFileName = $"{compiledFileName}.runtimeconfig.json";

            var jsonFilePath = Path.Combine(directory, jsonFileName);

            File.WriteAllText(jsonFilePath, text);
        }
    }
}