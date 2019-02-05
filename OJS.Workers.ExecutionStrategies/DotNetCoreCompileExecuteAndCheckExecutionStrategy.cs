namespace OJS.Workers.ExecutionStrategies
{
    using System;
    using System.IO;
    using System.Linq;

    using OJS.Workers.Common;
    using OJS.Workers.Common.Models;
    using OJS.Workers.ExecutionStrategies.Models;
    using OJS.Workers.Executors;

    using static OJS.Workers.Common.Constants;

    public class DotNetCoreCompileExecuteAndCheckExecutionStrategy : BaseCodeExecutionStrategy
    {
        private readonly string dotNetCoreRuntimeVersion;

        public DotNetCoreCompileExecuteAndCheckExecutionStrategy(
            Func<CompilerType, string> getCompilerPathFunc,
            IProcessExecutorFactory processExecutorFactory,
            string dotNetCoreRuntimeVersion,
            int baseTimeUsed,
            int baseMemoryUsed)
            : base(processExecutorFactory, baseTimeUsed, baseMemoryUsed)
        {
            this.GetCompilerPathFunc = getCompilerPathFunc;
            this.dotNetCoreRuntimeVersion = dotNetCoreRuntimeVersion;
        }

        protected Func<CompilerType, string> GetCompilerPathFunc { get; }

        private string RuntimeConfigJsonTemplate => $@"
            {{
	            ""runtimeOptions"": {{
                    ""framework"": {{
                        ""name"": ""Microsoft.NETCore.App"",
                        ""version"": ""{this.dotNetCoreRuntimeVersion}""
                    }}
                }}
            }}";

        protected override void ExecuteAgainstTestsInput(
            IExecutionContext<TestsInputModel> executionContext,
            IExecutionResult<TestResult> result)
        {
            var compileResult = this.ExecuteCompiling(
                executionContext,
                this.GetCompilerPathFunc,
                result);

            if (!compileResult.IsCompiledSuccessfully)
            {
                return;
            }

            var executor = this.PrepareExecutor(
                compileResult,
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
        }

        protected override void ExecuteAgainstSimpleInput(
            IExecutionContext<string> executionContext,
            IExecutionResult<OutputResult> result)
        {
            var compileResult = this.ExecuteCompiling(
                executionContext,
                this.GetCompilerPathFunc,
                result);

            if (!compileResult.IsCompiledSuccessfully)
            {
                return;
            }

            var executor = this.PrepareExecutor(
                compileResult,
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
        }

        private IExecutor PrepareExecutor<TInput>(
            CompileResult compileResult,
            IExecutionContext<TInput> executionContext,
            out string[] arguments,
            out string compilerPath)
        {
            var executor = this.CreateExecutor(ProcessExecutorType.Restricted);

            arguments = new[]
            {
                compileResult.OutputFile
            };

            compilerPath = this.GetCompilerPathFunc(executionContext.CompilerType);

            this.CreateRuntimeConfigJsonFile(this.WorkingDirectory, this.RuntimeConfigJsonTemplate);

            return executor;
        }

        private void CreateRuntimeConfigJsonFile(string directory, string text)
        {
            var compiledFileName = Directory
                .GetFiles(directory)
                .Select(Path.GetFileNameWithoutExtension)
                .First();

            var jsonFileName = $"{compiledFileName}.runtimeconfig{JsonFileExtension}";

            var jsonFilePath = Path.Combine(directory, jsonFileName);

            File.WriteAllText(jsonFilePath, text);
        }
    }
}
