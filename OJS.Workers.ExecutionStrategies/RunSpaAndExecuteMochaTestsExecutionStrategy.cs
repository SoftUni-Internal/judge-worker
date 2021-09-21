namespace OJS.Workers.ExecutionStrategies
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text.RegularExpressions;
    using Ionic.Zip;
    using OJS.Workers.Common;
    using OJS.Workers.Common.Extensions;
    using OJS.Workers.Common.Helpers;
    using OJS.Workers.Common.Models;
    using OJS.Workers.ExecutionStrategies.Models;
    using OJS.Workers.ExecutionStrategies.Python;
    using OJS.Workers.Executors;
    using static OJS.Workers.Common.Constants;

    public class RunSpaAndExecuteMochaTestsExecutionStrategy : PythonExecuteAndCheckExecutionStrategy
    {
        private const string UserApplicationHttpPortPlaceholder = "#userApplicationHttpPort#";
        private const string NodeModulesRequirePattern = "(require\\(\\')([\\w]*)(\\'\\))";
        private const string TestsDirectoryName = "test";
        private const string UserApplicationDirectoryName = "app";
        private const string NginxConfFileName = "nginx.conf";

        public RunSpaAndExecuteMochaTestsExecutionStrategy(
            IProcessExecutorFactory processExecutorFactory,
            string pythonExecutablePath,
            string jsProjNodeModulesPath,
            string mochaNodeModulePath,
            string chaiNodeModulePath,
            string playwrightModulePath,
            int portNumber,
            int baseTimeUsed,
            int baseMemoryUsed)
            : base(
                  processExecutorFactory,
                  pythonExecutablePath,
                  baseTimeUsed,
                  baseMemoryUsed)
        {
            this.JsProjNodeModulesPath = jsProjNodeModulesPath;
            this.MochaModulePath = mochaNodeModulePath;
            this.ChaiModulePath = chaiNodeModulePath;
            this.PlaywrightModulePath = playwrightModulePath;
            this.PortNumber = portNumber;
        }

        private int PortNumber { get; }

        private string MochaModulePath { get; }

        private string ChaiModulePath { get; }

        private string PlaywrightModulePath { get; }

        private string JsProjNodeModulesPath { get; }

        private string TestsPath => FileHelpers.BuildPath(this.WorkingDirectory, TestsDirectoryName);

        private string UserApplicationPath => FileHelpers.BuildPath(this.WorkingDirectory, UserApplicationDirectoryName);

        private string NginxConfFileDirectory => FileHelpers.BuildPath(this.WorkingDirectory, "nginx");

        private string NginxConfFileFullPath => FileHelpers.BuildPath(this.NginxConfFileDirectory, NginxConfFileName);

        private string PythonCodeTemplate => $@"
import docker
import subprocess

import shutil
import tarfile

from os import chdir, remove
from os.path import basename, join, dirname

mocha_path = '{this.MochaModulePath}'
tests_path = '{this.TestsPath}'
image_name = 'nginx'
path_to_project = '{this.UserApplicationPath}'
path_to_nginx_conf = '{this.NginxConfFileDirectory}/nginx.conf'
path_to_node_modules = '{this.JsProjNodeModulesPath}'
port = '{this.PortNumber}'


class DockerExecutor:
    def __init__(self):
        self.client = docker.from_env()
        self.__ensure_image_is_present()
        self.container = self.client.containers.create(
            image=image_name,
            ports={{'80/tcp': port}},
            volumes={{
                path_to_nginx_conf: {{
                    'bind': '/etc/nginx/nginx.conf',
                    'mode': 'ro',
                }},
                path_to_project: {{
                    'bind': '/usr/share/nginx/html',
                    'mode': 'rw',
                }},
            }},
        )

    def start(self):
        self.container.start()
        self.copy_to_container(path_to_node_modules, '/usr/share/nginx/html/node_modules');

    def stop(self):
        self.container.stop()
        self.container.wait()
        self.container.remove()

    def copy_to_container(self, source, destination):
        chdir(dirname(source))
        local_dest_name = join(dirname(source), basename(destination))
        if local_dest_name != source:
            shutil.copy2(source, local_dest_name)
        dst_name = basename(destination)
        tar_path = local_dest_name + '.tar'

        tar = tarfile.open(tar_path, mode='w')
        try:
            tar.add(dst_name)
        finally:
            tar.close()

        data = open(tar_path, 'rb').read()
        self.container.put_archive(dirname(destination), data)

        remove(tar_path)
        # remove(local_dest_name)

    def __ensure_image_is_present(self):
        def is_latest_image_present(name):
            image_tag = name + ':latest'
            all_tags = [tag for img in self.client.images.list() for tag in img.tags]
            return any(tag for tag in all_tags if tag == image_tag)

        if not is_latest_image_present(image_name):
            self.client.images.pull(image_name)


executor = DockerExecutor()

try:
    executor.start()
    commands = [mocha_path, tests_path, '-R', 'json']

    process = subprocess.run(
        commands,
    )

    print(process.stdout)
except Exception as e:
    print(e)
finally:
    executor.stop()
";

        private string NginxFileContent => $@"
worker_processes  1;

events {{
    worker_connections  1024;
}}

http {{
    include mime.types;
    types
    {{
        application/javascript mjs;
    }}

    default_type  application/octet-stream;
    sendfile        on;
    keepalive_timeout  65;

    server {{
      root /usr/share/nginx/html;

      listen 80;
      server_name localhost;

      location / {{
        try_files $uri $uri/ /index.html;
        autoindex on;
      }}
    }}
}}";

        protected override IExecutionResult<TestResult> ExecuteAgainstTestsInput(
            IExecutionContext<TestsInputModel> executionContext,
            IExecutionResult<TestResult> result)
        {
            try
            {
                this.ExtractSubmissionFiles(executionContext);
            }
            catch (ArgumentException exception)
            {
                result.IsCompiledSuccessfully = false;
                result.CompilerComment = exception.Message;

                return result;
            }

            this.SaveTestsToFiles(executionContext.Input.Tests);
            this.SaveNginxFile();

            var codeSavePath = this.SavePythonCodeTemplateToTempFile();
            var executor = this.CreateExecutor();
            var checker = executionContext.Input.GetChecker();
            return this.RunTests(codeSavePath, executor, checker, executionContext, result);
        }

        protected override IExecutionResult<TestResult> RunTests(
            string codeSavePath,
            IExecutor executor,
            IChecker checker,
            IExecutionContext<TestsInputModel> executionContext,
            IExecutionResult<TestResult> result)
        {
            result.Results.AddRange(
                executionContext.Input.Tests
                    .Select(
                        test => this.RunIndividualTest(
                            codeSavePath,
                            executor,
                            executionContext,
                            test))
                    .SelectMany(resultList => resultList));
            return result;
        }

        protected override IExecutor CreateExecutor()
            => this.CreateExecutor(ProcessExecutorType.Standard);

        private ICollection<TestResult> RunIndividualTest(
            string codeSavePath,
            IExecutor executor,
            IExecutionContext<TestsInputModel> executionContext,
            TestContext test)
        {
            var processExecutionResult = this.Execute(executionContext, executor, codeSavePath, test.Input);
            return this.ExtractTestResultsFromReceivedOutput(processExecutionResult.ReceivedOutput, test.Id);
        }

        private void ExtractSubmissionFiles<TInput>(IExecutionContext<TInput> executionContext)
        {
            this.ValidateAllowedFileExtension(executionContext);

            var submissionFilePath = FileHelpers.BuildPath(this.WorkingDirectory, "temp");
            File.WriteAllBytes(submissionFilePath, executionContext.FileContent);
            using (ZipFile zip = ZipFile.Read(submissionFilePath))
            {
                zip.RemoveSelectedEntries("node_modules/*");
                zip.Save();
            }

            FileHelpers.RemoveFilesFromZip(submissionFilePath, RemoveMacFolderPattern);
            FileHelpers.UnzipFile(submissionFilePath, this.UserApplicationPath);

            Directory.CreateDirectory(this.TestsPath);
            Directory.CreateDirectory(this.NginxConfFileDirectory);
        }

        private void ValidateAllowedFileExtension<TInput>(IExecutionContext<TInput> executionContext)
        {
            var trimmedAllowedFileExtensions = executionContext.AllowedFileExtensions?.Trim();
            var allowedFileExtensions = (!trimmedAllowedFileExtensions?.StartsWith(".") ?? false)
                ? $".{trimmedAllowedFileExtensions}"
                : trimmedAllowedFileExtensions;

            if (allowedFileExtensions != ZipFileExtension)
            {
                throw new ArgumentException(
                    "This file extension is not allowed for the execution strategy. Please contact an administrator.");
            }
        }

        private ICollection<TestResult> ExtractTestResultsFromReceivedOutput(string receivedOutput, int parentTestId)
        {
            JsonExecutionResult mochaResult = JsonExecutionResult.Parse(this.PreproccessReceivedExecutionOutput(receivedOutput));
            if (mochaResult.TotalTests == 0)
            {
                return new List<TestResult>
                {
                    new TestResult
                    {
                        Id = parentTestId,
                        IsTrialTest = false,
                        ResultType = TestRunResultType.WrongAnswer,
                        CheckerDetails = new CheckerDetails
                        {
                            UserOutputFragment = receivedOutput,
                        },
                    },
                };
            }

            return mochaResult.TestErrors
                .Select(test => this.ParseTestResult(test, parentTestId))
                .ToList();
        }

        private TestResult ParseTestResult(string testResult, int parentTestId)
            => new TestResult
            {
                Id = parentTestId,
                IsTrialTest = false,
                ResultType = testResult == null
                    ? TestRunResultType.CorrectAnswer
                    : TestRunResultType.WrongAnswer,
                CheckerDetails = testResult == null
                    ? default(CheckerDetails)
                    : new CheckerDetails { UserOutputFragment = testResult },
            };

        private string PreproccessReceivedExecutionOutput(string receivedOutput)
            => receivedOutput
                .Trim()
                .Replace("b'", string.Empty)
                .Replace("}'", "}")
                .Replace("}None", "}");

        private string SavePythonCodeTemplateToTempFile()
        {
            var pythonCodeTemplate = this.PythonCodeTemplate.Replace("\\", "\\\\");
            return FileHelpers.SaveStringToTempFile(this.WorkingDirectory, pythonCodeTemplate);
        }

        private void SaveNginxFile()
            => FileHelpers.SaveStringToFile(this.NginxFileContent, this.NginxConfFileFullPath);

        private void SaveTestsToFiles(IEnumerable<TestContext> tests)
        {
            foreach (var test in tests)
            {
                var testInputContent = this.PreprocessTestInput(test.Input);

                FileHelpers.SaveStringToFile(
                    testInputContent,
                    FileHelpers.BuildPath(this.TestsPath, $"{test.Id}{JavaScriptFileExtension}"));
            }
        }

        private string ReplaceNodeModulesRequireStatementsInTests(string testInputContent)
        {
            this.GetNodeModules(testInputContent)
                .ToList()
                .ForEach(nodeModule =>
                {
                    var (name, requireStatement) = nodeModule;
                    testInputContent = this.FixPathsForNodeModule(testInputContent, name, requireStatement);
                });

            return testInputContent;
        }

        private string FixPathsForNodeModule(string testInputContent, string name, string requireStatement)
        {
            var path = this.GetNodeModulePathByName(name);

            var fixedRequireStatement = requireStatement.Replace(name, path)
                .Replace("\\", "\\\\");

            return testInputContent.Replace(requireStatement, fixedRequireStatement);
        }

        private string GetNodeModulePathByName(string name)
        {
            switch (name)
            {
                case "mocha":
                    return this.MochaModulePath;
                case "chai":
                    return this.ChaiModulePath;
                case "playwright":
                    return this.PlaywrightModulePath;
                default:
                    return null;
            }
        }

        private IEnumerable<(string, string)> GetNodeModules(string testInputContent)
        {
            var requirePattern = new Regex(NodeModulesRequirePattern);
            var results = requirePattern.Matches(testInputContent);
            var nodeModules = new List<(string, string)>();

            foreach (Match match in results)
            {
                var fullRequireStatement = match.Groups[0].ToString();
                var nodeModuleName = match.Groups[2].ToString();
                nodeModules.Add((nodeModuleName, fullRequireStatement));
            }

            return nodeModules;
        }

        private string PreprocessTestInput(string testInput)
        {
            testInput = this.ReplaceNodeModulesRequireStatementsInTests(testInput)
                .Replace(UserApplicationHttpPortPlaceholder, this.PortNumber.ToString());

            return OSPlatformHelpers.IsDocker()
                ? testInput.Replace("localhost", "host.docker.internal")
                : testInput;
        }
    }
}