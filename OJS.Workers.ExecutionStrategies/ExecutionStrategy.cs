namespace OJS.Workers.ExecutionStrategies
{
    using System;
    using System.IO;
    using System.Threading.Tasks;

    using log4net;

    using OJS.Workers.Common;
    using OJS.Workers.Common.Exceptions;
    using OJS.Workers.Common.Extensions;
    using OJS.Workers.Common.Helpers;
    using OJS.Workers.Common.Models;
    using OJS.Workers.Compilers;
    using OJS.Workers.ExecutionStrategies.Models;
    using OJS.Workers.Executors;

    public abstract class ExecutionStrategy : IExecutionStrategy
    {
        protected const string RemoveMacFolderPattern = "__MACOSX/*";

        protected readonly IProcessExecutorFactory ProcessExecutorFactory;

        private readonly ILog logger;

        protected ExecutionStrategy(
            IProcessExecutorFactory processExecutorFactory,
            int baseTimeUsed,
            int baseMemoryUsed)
        {
            this.BaseTimeUsed = baseTimeUsed;
            this.BaseMemoryUsed = baseMemoryUsed;
            this.ProcessExecutorFactory = processExecutorFactory;
            this.logger = LogManager.GetLogger(typeof(ExecutionStrategy));
        }

        protected int BaseTimeUsed { get; }

        protected int BaseMemoryUsed { get; }

        protected string WorkingDirectory { get; set; }

        public IExecutionResult<TResult> Execute<TInput, TResult>(IExecutionContext<TInput> executionContext)
            where TResult : ISingleCodeRunResult, new()
        {
            switch (executionContext)
            {
                case IExecutionContext<TestsInputModel> testsExecutionContext:
                    return (IExecutionResult<TResult>)this.ExecuteAgainstTestsInput(testsExecutionContext);
                case IExecutionContext<string> stringInputExecutionContext:
                    return (IExecutionResult<TResult>)this.ExecuteAgainstSimpleInput(stringInputExecutionContext);
                default:
                    return new ExecutionResult<TResult>
                    {
                        IsCompiledSuccessfully = false,
                        CompilerComment = "Execution context not found"
                    };
            }
        }

        public IExecutionResult<TResult> SafeExecute<TInput, TResult>(IExecutionContext<TInput> executionContext)
            where TResult : ISingleCodeRunResult, new()
        {
            this.WorkingDirectory = DirectoryHelpers.CreateTempDirectoryForExecutionStrategy();

            try
            {
                return this.Execute<TInput, TResult>(executionContext);
            }
            finally
            {
                Task.Run(() =>
                {
                    try
                    {
                        DirectoryHelpers.SafeDeleteDirectory(this.WorkingDirectory, true);
                    }
                    catch (Exception ex)
                    {
                        this.logger.Error("executionStrategy.SafeDeleteDirectory has thrown an exception:", ex);
                    }
                });
            }
        }

        protected IExecutor CreateExecutor(ProcessExecutorType processExecutorType)
            => this.ProcessExecutorFactory
                .CreateProcessExecutor(this.BaseTimeUsed, this.BaseMemoryUsed, processExecutorType);

        protected virtual IExecutionResult<OutputResult> ExecuteAgainstSimpleInput(
            IExecutionContext<string> executionContext) =>
                throw new DerivedImplementationNotFoundException();

        protected virtual IExecutionResult<TestResult> ExecuteAgainstTestsInput(
            IExecutionContext<TestsInputModel> executionContext) =>
                throw new DerivedImplementationNotFoundException();

        protected IExecutionResult<TestResult> CompileExecuteAndCheck(
            IExecutionContext<TestsInputModel> executionContext,
            Func<CompilerType, string> getCompilerPathFunc,
            IExecutor executor,
            bool useSystemEncoding = true,
            bool dependOnExitCodeForRunTimeError = false)
        {
            var result = new ExecutionResult<TestResult>();

            // Compile the file
            var compileResult = this.ExecuteCompiling(
                executionContext,
                getCompilerPathFunc,
                result);

            if (!compileResult.IsCompiledSuccessfully)
            {
                return result;
            }

            var outputFile = compileResult.OutputFile;

            // Execute and check each test
            var checker = executionContext.Input.GetChecker();

            foreach (var test in executionContext.Input.Tests)
            {
                var processExecutionResult = executor.Execute(
                    outputFile,
                    test.Input,
                    executionContext.TimeLimit,
                    executionContext.MemoryLimit,
                    null,
                    null,
                    false,
                    useSystemEncoding,
                    dependOnExitCodeForRunTimeError);

                var testResult = this.ExecuteAndCheckTest(
                    test,
                    processExecutionResult,
                    checker,
                    processExecutionResult.ReceivedOutput);

                result.Results.Add(testResult);
            }

            return result;
        }

        protected TestResult ExecuteAndCheckTest(
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
                testResult.ResultType = checkerResult.IsCorrect ? TestRunResultType.CorrectAnswer : TestRunResultType.WrongAnswer;

                // TODO: Do something with checkerResult.ResultType
                testResult.CheckerDetails = checkerResult.CheckerDetails;
            }
            else
            {
                throw new ArgumentOutOfRangeException(nameof(processExecutionResult), "Invalid ProcessExecutionResultType value.");
            }

            return testResult;
        }

        protected OutputResult GetOutputResult(ProcessExecutionResult processExecutionResult) =>
            new OutputResult
            {
                TimeUsed = (int)processExecutionResult.TimeWorked.TotalMilliseconds,
                MemoryUsed = (int)processExecutionResult.MemoryUsed,
                ResultType = processExecutionResult.Type,
                Output = string.IsNullOrWhiteSpace(processExecutionResult.ErrorOutput)
                    ? processExecutionResult.ReceivedOutput
                    : processExecutionResult.ErrorOutput
            };

        protected CompileResult ExecuteCompiling<TInput, TResult>(
            IExecutionContext<TInput> executionContext,
            Func<CompilerType, string> getCompilerPathFunc,
            IExecutionResult<TResult> result)
            where TResult : ISingleCodeRunResult, new()
        {
            var submissionFilePath = string.IsNullOrEmpty(executionContext.AllowedFileExtensions)
                ? FileHelpers.SaveStringToTempFile(this.WorkingDirectory, executionContext.Code)
                : FileHelpers.SaveByteArrayToTempFile(this.WorkingDirectory, executionContext.FileContent);

            var compilerPath = getCompilerPathFunc(executionContext.CompilerType);

            var compileResult = this.Compile(
                executionContext.CompilerType,
                compilerPath,
                executionContext.AdditionalCompilerArguments,
                submissionFilePath);

            result.IsCompiledSuccessfully = compileResult.IsCompiledSuccessfully;
            result.CompilerComment = compileResult.CompilerComment;

            return compileResult;
        }

        protected virtual CompileResult Compile(
            CompilerType compilerType,
            string compilerPath,
            string compilerArguments,
            string submissionFilePath)
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
    }
}
