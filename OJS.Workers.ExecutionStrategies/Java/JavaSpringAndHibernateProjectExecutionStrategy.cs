﻿namespace OJS.Workers.ExecutionStrategies.Java
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text.RegularExpressions;
    using System.Xml;
    using System.Text;

    using OJS.Workers.Common;
    using OJS.Workers.Common.Helpers;
    using OJS.Workers.Common.Models;
    using OJS.Workers.ExecutionStrategies.Models;
    using OJS.Workers.Executors;

    using static OJS.Workers.Common.Constants;
    using static OJS.Workers.ExecutionStrategies.Helpers.JavaStrategiesHelper;

    public class JavaSpringAndHibernateProjectExecutionStrategy : JavaProjectTestsExecutionStrategy
    {
        private const string PomXmlFileNameAndExtension = "pom.xml";
        private const string ApplicationPropertiesFileName = "application.properties";
        private const string IntelliJProjectTemplatePattern = "src/main/java";
        private const string IntelliJTestProjectTemplatePattern = "src/test/java";
        private const string PropertySourcePattern = @"(@PropertySources?\((?:.*?)\))";
        private const string PomXmlNamespace = @"http://maven.apache.org/POM/4.0.0";
        private const string StartClassNodeXPath = @"//pomns:properties/pomns:start-class";
        private const string DependencyNodeXPathTemplate = @"//pomns:dependencies/pomns:dependency[pomns:groupId='##' and pomns:artifactId='!!']";
        private const string DependenciesNodeXPath = @"//pomns:dependencies";
        private const string MavenTestCommand = "test -f {0} -Dtest=\"{1}\"";
        private const string MavenBuild = "compile";
        private const string PomXmlBuildSettingsPattern = @"<build>(?s:.)*<\/build>";
        private const string TestsFolderPattern = @"src/test/java/*";
        private const string MainCodeFolderPattern = @"src/main/java/";

        private static readonly string MavenErrorFailurePattern =
            $@"\[ERROR\]";

        public JavaSpringAndHibernateProjectExecutionStrategy(
            Func<CompilerType, string> getCompilerPathFunc,
            IProcessExecutorFactory processExecutorFactory,
            string javaExecutablePath,
            string javaLibrariesPath,
            string mavenPath,
            int baseTimeUsed,
            int baseMemoryUsed)
            : base(
                getCompilerPathFunc,
                processExecutorFactory,
                javaExecutablePath,
                javaLibrariesPath,
                baseTimeUsed,
                baseMemoryUsed)
            => this.MavenPath = mavenPath;

        // Property contains Dictionary<GroupId, Tuple<ArtifactId, Version>>
        public Dictionary<string, Tuple<string, string>> Dependencies =>
            new Dictionary<string, Tuple<string, string>>()
            {
                { "org.junit.jupiter", new Tuple<string, string>("junit-jupiter-api", "5.9.2") },
                { "org.hibernate", new Tuple<string, string>("hibernate-core", "5.6.3.Final") },
                { "mysql", new Tuple<string, string>("mysql-connector-java", "8.0.27") },
                { "org.assertj", new Tuple<string, string>("assertj-core", "3.22.0") },
                { "org.springframework.boot", new Tuple<string, string>("spring-boot-starter-test", "2.6.2") },
            };

        protected string MavenPath { get; set; }

        protected string PackageName { get; set; }

        protected string MainClassFileName { get; set; }

        protected string ProjectRootDirectoryInSubmissionZip { get; set; }

        protected string ProjectTestDirectoryInSubmissionZip { get; set; }

        protected string PomXmlBuildSettings => @"
        <build>
            <plugins>
                <!-- Maven Compiler Plugin -->
                <plugin>
                    <groupId>org.apache.maven.plugins</groupId>
                    <artifactId>maven-compiler-plugin</artifactId>
                    <version>3.8.1</version>
                </plugin>
            </plugins>
        </build>";

        protected override string ClassPathArgument
            => $"-cp {this.JavaLibrariesPath}*{ClassPathArgumentSeparator}{this.WorkingDirectory}{Path.DirectorySeparatorChar}target{Path.DirectorySeparatorChar}* ";

        protected override IExecutionResult<TestResult> ExecuteAgainstTestsInput(
            IExecutionContext<TestsInputModel> executionContext,
            IExecutionResult<TestResult> result)
        {
            // Create a temp file with the submission code
            string submissionFilePath;
            try
            {
                submissionFilePath = this.CreateSubmissionFile(executionContext);
                var isValid = this.ValidateFolderStructure(submissionFilePath);

                if (!isValid)
                {
                    throw new ArgumentException("Folder structure is invalid!");
                }
            }
            catch (ArgumentException exception)
            {
                result.IsCompiledSuccessfully = false;
                result.CompilerComment = exception.Message;

                return result;
            }

            FileHelpers.UnzipFile(submissionFilePath, this.WorkingDirectory);

            var pomXmlPath = FileHelpers.FindFileMatchingPattern(this.WorkingDirectory, PomXmlFileNameAndExtension);

            var mavenArgs = new[] { string.Format(MavenBuild, pomXmlPath) };

            var mavenExecutor = this.CreateExecutor(ProcessExecutorType.Standard);

            var packageExecutionResult = mavenExecutor.Execute(
              this.MavenPath,
              string.Empty,
              executionContext.TimeLimit,
              executionContext.MemoryLimit,
              mavenArgs,
              this.WorkingDirectory);

            var mavenBuildFailureRegex = new Regex(MavenErrorFailurePattern);

            result.IsCompiledSuccessfully = !mavenBuildFailureRegex.IsMatch(packageExecutionResult.ReceivedOutput);

            if (!result.IsCompiledSuccessfully)
            {
                result.CompilerComment = this.GetMavenErrorsComment(packageExecutionResult.ReceivedOutput);
                return result;
            }

            var executor = this.CreateExecutor(ProcessExecutorType.Restricted);

            var checker = executionContext.Input.GetChecker();
            var testIndex = 0;

            foreach (var test in executionContext.Input.Tests)
            {
                var testFile = this.TestNames[testIndex++];
                mavenArgs = new[] { string.Format(MavenTestCommand, pomXmlPath, testFile) };

                var processExecutionResult = executor.Execute(
                this.MavenPath,
                string.Empty,
                executionContext.TimeLimit,
                executionContext.MemoryLimit,
                mavenArgs,
                this.WorkingDirectory);

                ValidateJvmInitialization(processExecutionResult.ReceivedOutput);

                if (processExecutionResult.ReceivedOutput.Contains($"Could not find class: {testFile}"))
                {
                    throw new FileLoadException("Tests could not be loaded, project structure is incorrect");
                }

                var message = this.EvaluateMavenTestOutput(processExecutionResult.ReceivedOutput, mavenBuildFailureRegex);

                var testResult = this.CheckAndGetTestResult(
                    test,
                    processExecutionResult,
                    checker,
                    message);

                result.Results.Add(testResult);
            }

            return result;
        }

        protected override string PrepareSubmissionFile(IExecutionContext<TestsInputModel> context)
        {
            var submissionFilePath = $"{this.WorkingDirectory}{Path.DirectorySeparatorChar}{SubmissionFileName}";
            File.WriteAllBytes(submissionFilePath, context.FileContent);
            FileHelpers.RemoveFilesFromZip(submissionFilePath, RemoveMacFolderPattern);

            this.ExtractPackageAndDirectoryNames(submissionFilePath);
            this.OverwriteApplicationProperties(submissionFilePath);
            this.RemovePropertySourceAnnotationsFromMainClass(submissionFilePath);
            this.AddTestsToUserSubmission(context, submissionFilePath);
            this.PreparePomXml(submissionFilePath);

            return submissionFilePath;
        }

        protected void ExtractPackageAndDirectoryNames(string submissionFilePath)
        {
            this.MainClassFileName = this.ExtractEntryPointFromPomXml(submissionFilePath);

            this.PackageName = this.MainClassFileName
                .Substring(0, this.MainClassFileName.LastIndexOf(".", StringComparison.Ordinal));

            var normalizedPath = this.PackageName.Replace(".", "/");

            this.ProjectRootDirectoryInSubmissionZip = $"{IntelliJProjectTemplatePattern}/{normalizedPath}/";
            this.ProjectTestDirectoryInSubmissionZip = $"{IntelliJTestProjectTemplatePattern}/{normalizedPath}/";

            var fileNameWithoutExtension = this.MainClassFileName.Substring(
                this.MainClassFileName.LastIndexOf(".", StringComparison.Ordinal) + 1);

            this.MainClassFileName = fileNameWithoutExtension + JavaSourceFileExtension;
        }

        protected void OverwriteApplicationProperties(string submissionZipFilePath)
        {
            var fakeApplicationPropertiesText = @"
                spring.jpa.properties.hibernate.show_sql= false
                spring.jpa.properties.hibernate.use_sql_comments=false
                spring.jpa.properties.hibernate.format_sql=false
                logging.level.root=off
                spring.datasource.url=jdbc:h2:mem:testdb
                spring.datasource.driverClassName=org.h2.Driver
                spring.datasource.username=sa
                spring.datasource.password=
                spring.jpa.database-platform=org.hibernate.dialect.H2Dialect
                spring.jpa.hibernate.ddl-auto=create-drop";

            var fakeApplicationPropertiesPath = $"{this.WorkingDirectory}{Path.DirectorySeparatorChar}{ApplicationPropertiesFileName}";
            File.WriteAllText(fakeApplicationPropertiesPath, fakeApplicationPropertiesText);

            var pathsInZip = FileHelpers.GetFilePathsFromZip(submissionZipFilePath);

            var resourceDirectory = Path.GetDirectoryName(pathsInZip.FirstOrDefault(f => f.EndsWith(ApplicationPropertiesFileName)));

            if (string.IsNullOrEmpty(resourceDirectory))
            {
                throw new FileNotFoundException(
                    $"Resource directory not found in the project!");
            }

            FileHelpers.AddFilesToZipArchive(submissionZipFilePath, resourceDirectory, fakeApplicationPropertiesPath);
            File.Delete(fakeApplicationPropertiesPath);
        }

        protected void RemovePropertySourceAnnotationsFromMainClass(string submissionFilePath)
        {
            var extractionDirectory = DirectoryHelpers.CreateTempDirectoryForExecutionStrategy();

            var mainClassFilePath = FileHelpers.ExtractFileFromZip(
                submissionFilePath,
                this.MainClassFileName,
            extractionDirectory);

            var mainClassContent = File.ReadAllText(mainClassFilePath);

            var propertySourceMatcher = new Regex(PropertySourcePattern);
            while (propertySourceMatcher.IsMatch(mainClassContent))
            {
                mainClassContent = Regex.Replace(mainClassContent, PropertySourcePattern, string.Empty);
            }

            File.WriteAllText(mainClassFilePath, mainClassContent);
            var pomXmlFolderPathInZip = Path.GetDirectoryName(FileHelpers
                .GetFilePathsFromZip(submissionFilePath)
                .FirstOrDefault(f => f.EndsWith(this.MainClassFileName)));

            FileHelpers.AddFilesToZipArchive(submissionFilePath, pomXmlFolderPathInZip, mainClassFilePath);
            DirectoryHelpers.SafeDeleteDirectory(extractionDirectory, true);
        }

        protected override void AddTestsToUserSubmission(
            IExecutionContext<TestsInputModel> context,
            string submissionZipFilePath)
        {
            var testNumber = 0;
            var filePaths = new string[context.Input.Tests.Count()];

            FileHelpers.RemoveFilesFromZip(
                submissionZipFilePath,
                TestsFolderPattern);

            foreach (var test in context.Input.Tests)
            {
                var className = JavaCodePreprocessorHelper.GetPublicClassName(test.Input);
                var testFileName =
                        $"{this.WorkingDirectory}{Path.DirectorySeparatorChar}{className}{JavaSourceFileExtension}";
                File.WriteAllText(testFileName, $"package {this.PackageName};{Environment.NewLine}{test.Input}");
                filePaths[testNumber] = testFileName;
                this.TestNames.Add($"{this.PackageName}.{className}");
                testNumber++;
            }

            FileHelpers.AddFilesToZipArchive(
                submissionZipFilePath,
                this.ProjectTestDirectoryInSubmissionZip,
                filePaths);
            FileHelpers.DeleteFiles(filePaths);
        }

        protected override void ExtractUserClassNames(string submissionFilePath)
        {
            this.UserClassNames.AddRange(FileHelpers
                .GetFilePathsFromZip(submissionFilePath)
                .Where(x => !x.EndsWith("/") && x.EndsWith(JavaSourceFileExtension))
                    .Select(x => x.Contains(IntelliJProjectTemplatePattern)
                    ? x.Substring(x.LastIndexOf(
                        IntelliJProjectTemplatePattern,
                        StringComparison.Ordinal)
                        + IntelliJProjectTemplatePattern.Length
                        + 1)
                    : x)
                .Select(x => x.Contains(".") ? x.Substring(0, x.LastIndexOf(".", StringComparison.Ordinal)) : x)
                .Select(x => x.Replace("/", ".")));
        }

        protected void PreparePomXml(string submissionFilePath)
        {
            var extractionDirectory = DirectoryHelpers.CreateTempDirectoryForExecutionStrategy();

            var pomXmlFilePath = FileHelpers.ExtractFileFromZip(
                submissionFilePath,
                PomXmlFileNameAndExtension,
            extractionDirectory);

            if (string.IsNullOrEmpty(pomXmlFilePath))
            {
                throw new FileNotFoundException("Pom.xml not found in submission!");
            }

            this.AddBuildSettings(pomXmlFilePath);
            this.AddDependencies(pomXmlFilePath);
            var mainClassFolderPathInZip = Path.GetDirectoryName(FileHelpers
                .GetFilePathsFromZip(submissionFilePath)
                .FirstOrDefault(f => f.EndsWith(PomXmlFileNameAndExtension)));

            FileHelpers.AddFilesToZipArchive(submissionFilePath, mainClassFolderPathInZip, pomXmlFilePath);
            DirectoryHelpers.SafeDeleteDirectory(extractionDirectory, true);
        }

        private void AddBuildSettings(string pomXmlFilePath)
        {
            var pomXmlContent = File.ReadAllText(pomXmlFilePath);
            var buildSettingsRegex = new Regex(PomXmlBuildSettingsPattern);
            if (buildSettingsRegex.IsMatch(pomXmlContent))
            {
                pomXmlContent = Regex.Replace(pomXmlContent, PomXmlBuildSettingsPattern, this.PomXmlBuildSettings);
            }

            File.WriteAllText(pomXmlFilePath, pomXmlContent);
        }

        private void AddDependencies(string pomXmlFilePath)
        {
            var doc = new XmlDocument();
            doc.Load(pomXmlFilePath);

            var namespaceManager = new XmlNamespaceManager(doc.NameTable);
            namespaceManager.AddNamespace("pomns", PomXmlNamespace);

            XmlNode rootNode = doc.DocumentElement;
            if (rootNode == null)
            {
                throw new XmlException("Root element not specified in pom.xml");
            }

            var dependenciesNode = rootNode.SelectSingleNode(DependenciesNodeXPath, namespaceManager);
            if (dependenciesNode == null)
            {
                throw new XmlException("No dependencies specified in pom.xml");
            }

            foreach (var groupIdArtifactId in this.Dependencies)
            {
                var dependencyNode = rootNode
                    .SelectSingleNode(
                     DependencyNodeXPathTemplate
                    .Replace("##", groupIdArtifactId.Key).Replace("!!", groupIdArtifactId.Value.Item1), namespaceManager);

                if (dependencyNode == null)
                {
                    dependencyNode = doc.CreateNode(XmlNodeType.Element, "dependency", PomXmlNamespace);

                    var groupId = doc.CreateNode(XmlNodeType.Element, "groupId", PomXmlNamespace);
                    groupId.InnerText = groupIdArtifactId.Key;
                    var artifactId = doc.CreateNode(XmlNodeType.Element, "artifactId", PomXmlNamespace);
                    artifactId.InnerText = groupIdArtifactId.Value.Item1;

                    if (groupIdArtifactId.Value.Item2 != null)
                    {
                        var versionNumber = doc.CreateNode(XmlNodeType.Element, "version", PomXmlNamespace);
                        versionNumber.InnerText = groupIdArtifactId.Value.Item2;
                        dependencyNode.AppendChild(versionNumber);
                    }

                    dependencyNode.AppendChild(groupId);
                    dependencyNode.AppendChild(artifactId);
                    dependenciesNode.AppendChild(dependencyNode);
                }
            }

            doc.Save(pomXmlFilePath);
        }

        private string ExtractEntryPointFromPomXml(string submissionFilePath)
        {
            var pomXmlPath = FileHelpers.ExtractFileFromZip(submissionFilePath, "pom.xml", this.WorkingDirectory);

            if (string.IsNullOrEmpty(pomXmlPath))
            {
                throw new ArgumentException($"{nameof(pomXmlPath)} was not found in submission!");
            }

            var pomXml = new XmlDocument();
            pomXml.Load(pomXmlPath);

            var namespaceManager = new XmlNamespaceManager(pomXml.NameTable);
            namespaceManager.AddNamespace("pomns", PomXmlNamespace);

            XmlNode rootNode = pomXml.DocumentElement;

            var packageName = rootNode?.SelectSingleNode(StartClassNodeXPath, namespaceManager);

            if (packageName == null)
            {
                throw new ArgumentException($"Starter path not defined in pom.xml!");
            }

            FileHelpers.DeleteFiles(pomXmlPath);
            return packageName.InnerText.Trim();
        }

        private string GetMavenErrorsComment(string testOutput)
        {
            var sb = new StringBuilder();

            foreach (var line in testOutput.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries)
                         .Where(x => x
                             .StartsWith("[ERROR]") || x.StartsWith("[FAILURE]")))
            {
                sb.Append("\t" + line);
            }

            return sb.ToString();
        }

        private string EvaluateMavenTestOutput(string testOutput, Regex testErrorMatcher)
        {
            var message = TestPassedMessage;
            var errorMatch = testErrorMatcher.Match(testOutput);

            if (!errorMatch.Success)
            {
                return message;
            }

            return this.GetMavenErrorsComment(testOutput);
        }

        private bool ValidateFolderStructure(string submissionFilePath)
        {
            var paths = FileHelpers.GetFilePathsFromZip(submissionFilePath).ToList();

            return paths.Any(x => x.StartsWith(MainCodeFolderPattern)) && paths.Any(x => x.StartsWith(PomXmlFileNameAndExtension));
        }
    }
}
