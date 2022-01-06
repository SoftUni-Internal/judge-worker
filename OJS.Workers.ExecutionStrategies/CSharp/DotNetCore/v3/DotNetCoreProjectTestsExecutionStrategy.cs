namespace OJS.Workers.ExecutionStrategies.CSharp.DotNetCore.v3
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using OJS.Workers.Common;
    using OJS.Workers.Common.Helpers;
    using OJS.Workers.Common.Models;
    using OJS.Workers.ExecutionStrategies.Extensions;
    using OJS.Workers.ExecutionStrategies.Models;
    using OJS.Workers.Executors;

    public class DotNetCoreProjectTestsExecutionStrategy : CSharpProjectTestsExecutionStrategy
    {
        protected new const string AdditionalExecutionArguments = "--noresult";
        protected const string CsProjFileExtension = ".csproj";

        private const string ProjectPathPlaceholder = "##projectPath##";
        private const string ProjectReferencesPlaceholder = "##ProjectReferences##";
        private const string NUnitLiteConsoleAppFolderName = "NUnitLiteConsoleApp";
        private const string UserSubmissionFolderName = "UserProject";
        private const string NUnitLiteConsoleAppProgramName = "Program";

        private const string NUnitLiteConsoleAppProgramTemplate = @"
            using System;
            using System.Reflection;
            using NUnit.Common;
            using NUnitLite;

            public class Program
            {
                public static void Main(string[] args)
                {
                    var writter = new ExtendedTextWrapper(Console.Out);
                    new AutoRun(typeof(Program).GetTypeInfo().Assembly).Execute(args, writter, Console.In);
                }
            }";

        private readonly string projectReferenceTemplate =
            $@"<ProjectReference Include=""{ProjectPathPlaceholder}"" />";

        public DotNetCoreProjectTestsExecutionStrategy(
            Func<CompilerType, string> getCompilerPathFunc,
            IProcessExecutorFactory processExecutorFactory,
            int baseTimeUsed,
            int baseMemoryUsed)
            : base(getCompilerPathFunc, processExecutorFactory, baseTimeUsed, baseMemoryUsed)
        {
        }

        protected virtual string TargetFrameworkName => "netcoreapp3.1";

        protected virtual string MicrosoftEntityFrameworkCoreInMemoryVersion => "3.1.4";

        protected virtual string MicrosoftEntityFrameworkCoreProxiesVersion => "3.1.4";

        protected string NUnitLiteConsoleAppDirectory =>
            Path.Combine(this.WorkingDirectory, NUnitLiteConsoleAppFolderName);

        protected string UserProjectDirectory =>
            Path.Combine(this.WorkingDirectory, UserSubmissionFolderName);

        private string NUnitLiteConsoleAppCsProjTemplate => $@"
            <Project Sdk=""Microsoft.NET.Sdk"">
                <PropertyGroup>
                    <OutputType>Exe</OutputType>
                    <TargetFramework>{TargetFrameworkName}</TargetFramework>
                </PropertyGroup>
                <ItemGroup>
                    <PackageReference Include=""NUnitLite"" Version=""3.12.0"" />
                    <PackageReference Include=""Microsoft.EntityFrameworkCore.InMemory"" Version=""{MicrosoftEntityFrameworkCoreInMemoryVersion}"" />
                    <PackageReference Include=""Microsoft.EntityFrameworkCore.Proxies"" Version=""{MicrosoftEntityFrameworkCoreProxiesVersion}"" />
                </ItemGroup>
                <ItemGroup>
                    {ProjectReferencesPlaceholder}
                </ItemGroup>
            </Project>";

        protected override IExecutionResult<TestResult> ExecuteAgainstTestsInput(
            IExecutionContext<TestsInputModel> executionContext,
            IExecutionResult<TestResult> result)
        {
            executionContext.SanitizeContent();

            Directory.CreateDirectory(this.NUnitLiteConsoleAppDirectory);
            Directory.CreateDirectory(this.UserProjectDirectory);

            this.SaveZipSubmission(executionContext.FileContent, this.UserProjectDirectory);
            this.ExtractTestNames(executionContext.Input.Tests);

            this.SaveTestFiles(executionContext.Input.Tests, this.NUnitLiteConsoleAppDirectory);
            this.SaveSetupFixture(this.NUnitLiteConsoleAppDirectory);

            var userCsProjPaths = FileHelpers.FindAllFilesMatchingPattern(
                this.UserProjectDirectory, CsProjFileSearchPattern);

            var nUnitLiteConsoleApp = this.CreateNUnitLiteConsoleApp(userCsProjPaths);

            var compilerPath = this.GetCompilerPathFunc(executionContext.CompilerType);

            var compilerResult = this.Compile(
                executionContext.CompilerType,
                compilerPath,
                executionContext.AdditionalCompilerArguments,
                nUnitLiteConsoleApp.csProjPath);

            result.IsCompiledSuccessfully = compilerResult.IsCompiledSuccessfully;

            if (!result.IsCompiledSuccessfully)
            {
                result.CompilerComment = compilerResult.CompilerComment;
                return result;
            }

            // Delete tests before execution so the user can't access them
            FileHelpers.DeleteFiles(this.TestPaths.ToArray());

            var executor = this.CreateExecutor(ProcessExecutorType.Restricted);

            return this.RunUnitTests(
                compilerPath,
                executionContext,
                executor,
                executionContext.Input.GetChecker(),
                result,
                compilerResult.OutputFile,
                AdditionalExecutionArguments);
        }

        protected (string csProjTemplate, string csProjPath) CreateNUnitLiteConsoleApp(
            IEnumerable<string> projectsToTestCsProjPaths)
        {
            var consoleAppEntryPointPath = FileHelpers.BuildPath(
                this.NUnitLiteConsoleAppDirectory,
                $"{NUnitLiteConsoleAppProgramName}{Constants.CSharpFileExtension}");

            File.WriteAllText(consoleAppEntryPointPath, NUnitLiteConsoleAppProgramTemplate);

            var references = projectsToTestCsProjPaths
                .Select(path => this.projectReferenceTemplate.Replace(ProjectPathPlaceholder, path));

            var csProjTemplate = this.NUnitLiteConsoleAppCsProjTemplate
                .Replace(ProjectReferencesPlaceholder, string.Join(Environment.NewLine, references));

            var csProjPath = this.CreateNUnitLiteConsoleAppCsProjFile(csProjTemplate);

            return (csProjTemplate, csProjPath);
        }

        protected string CreateNUnitLiteConsoleAppCsProjFile(string csProjTemplate)
        {
            var consoleAppCsProjPath = FileHelpers.BuildPath(
                this.NUnitLiteConsoleAppDirectory,
                $"{NUnitLiteConsoleAppFolderName}{CsProjFileExtension}");

            File.WriteAllText(consoleAppCsProjPath, csProjTemplate);

            return consoleAppCsProjPath;
        }
    }
}