namespace OJS.Workers.ExecutionStrategies.NodeJs
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Text.RegularExpressions;
    using System.Web;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using OJS.Workers.Common;
    using OJS.Workers.Common.Helpers;
    using OJS.Workers.Common.Models;
    using OJS.Workers.ExecutionStrategies.Models;
    using OJS.Workers.ExecutionStrategies.Python;
    using OJS.Workers.Executors;

    public class NodeJsProjectRunJavaScriptProjectAndTestsWithPlaywrigth
        : PythonExecuteAndCheckExecutionStrategy
    {
        protected const string UserApplicationPathPlaceholder = "#userApplicationPath#";
        protected const string UserApplicationHttpPortPlaceholder = "#userApplicationHttpPort#";
        protected const string TestsPathPlaceholder = "#testsPath#";
        protected const string NodeModulesRequirePattern = "(require\\(\\')([\\w]*)(\\'\\))";
        protected const string MochaTestsPassingFailingResultPattern = "([\\d]*)\\s*(passing|failing)";

        public NodeJsProjectRunJavaScriptProjectAndTestsWithPlaywrigth(
            IProcessExecutorFactory processExecutorFactory,
            string pythonExecutablePath,
            string jsProjNodeModulesPath,
            int portNumber,
            int baseTimeUsed,
            int baseMemoryUsed)
            : base(
                  processExecutorFactory,
                  pythonExecutablePath,
                  baseTimeUsed,
                  baseMemoryUsed)
        {
            this.JSProjNodeModulesPath = jsProjNodeModulesPath;
            this.PortNumber = portNumber;
        }

        public string MochaModulePath => FileHelpers.BuildPath(this.JSProjNodeModulesPath, ".bin", "mocha.cmd");

        public string JSProjNodeModulesPath { get; }

        public int PortNumber { get; }

        protected string TestsPath => FileHelpers.BuildPath(this.WorkingDirectory, "test");

        protected string UserApplicationPath => FileHelpers.BuildPath(this.WorkingDirectory, "app");

        protected string NgingConfFilePath => FileHelpers.BuildPath(this.WorkingDirectory, "nginx.conf");

        protected string PythonCodeTemplate => $@"
import docker
import subprocess

mocha_path = '{this.MochaModulePath}'
tests_path = '{this.TestsPath}'
image_name = 'nginx'
path_to_project = '{this.UserApplicationPath}'
path_to_nginx_conf = '{this.NgingConfFilePath}'
path_to_node_modules = '{this.JSProjNodeModulesPath}'
port = '{this.PortNumber}'


class DockerExecutor:
    def __init__(self):
        self.client = docker.from_env()
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
                path_to_node_modules: {{
                    'bind': '/usr/share/nginx/html/node_modules',
                    'mode': 'ro'
                }}
            }},
        )

    def start(self):
        self.container.start()

    def stop(self):
        self.container.stop()
        self.container.wait()
        self.container.remove()


executor = DockerExecutor()

executor.start()

commands = [mocha_path, tests_path, '-R', 'json']

process = subprocess.run(
    commands,
    capture_output=True
)

print(process.stdout)

executor.stop()

";

        protected string NginxFileContent => $@"
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
            // Unzip submission file
            // Create a temp file with the submission code
            string submissionFilePath;
            try
            {
                var trimmedAllowedFileExtensions = executionContext.AllowedFileExtensions?.Trim();
                var allowedFileExtensions = (!trimmedAllowedFileExtensions?.StartsWith(".") ?? false)
                    ? $".{trimmedAllowedFileExtensions}"
                    : trimmedAllowedFileExtensions;

                if (allowedFileExtensions != ".zip")
                {
                    throw new ArgumentException("Submission file is not a zip file!");
                }

                submissionFilePath = FileHelpers.BuildPath(this.WorkingDirectory, "temp");
                File.WriteAllBytes(submissionFilePath, executionContext.FileContent);
                FileHelpers.RemoveFilesFromZip(submissionFilePath, RemoveMacFolderPattern);
            }
            catch (ArgumentException exception)
            {
                result.IsCompiledSuccessfully = false;
                result.CompilerComment = exception.Message;

                return result;
            }

            FileHelpers.UnzipFile(submissionFilePath, this.UserApplicationPath);

            System.IO.Directory.CreateDirectory(this.UserApplicationPath);
            System.IO.Directory.CreateDirectory(this.TestsPath);
            // Save test to file
            this.SaveTestsToFiles(executionContext.Input.Tests);
            this.SaveNginxFile();

            var codeSavePath = this.SavePythonCodeTemplateToTempFile();

            var executor = this.CreateExecutor();

            var checker = executionContext.Input.GetChecker();

            result = this.RunTests(codeSavePath, executor, checker, executionContext, result);
            return result;
        }

        protected override IExecutionResult<TestResult> RunTests(
            string codeSavePath,
            IExecutor executor,
            IChecker checker,
            IExecutionContext<TestsInputModel> executionContext,
            IExecutionResult<TestResult> result)
        {
            var requirePattern = new Regex(MochaTestsPassingFailingResultPattern);
            foreach (var test in executionContext.Input.Tests)
            {
                var processExecutionResult = this.Execute(executionContext, executor, codeSavePath, test.Input);

                string receivedOutput = processExecutionResult.ReceivedOutput.Replace("\\\"", "\"");
                receivedOutput = receivedOutput.Replace("\\n", "");
                receivedOutput = receivedOutput.Replace("b'", "");

                receivedOutput = Regex.Unescape(receivedOutput);
                receivedOutput = receivedOutput.Replace("}'", "}");

                dynamic deserializedOutput = JsonConvert.DeserializeObject(receivedOutput);
                var testResults = deserializedOutput.tests;

               
                foreach (var testResult in testResults)
                {
                    int errorCount = ((JObject)testResult.err).Count;
                    var testResultDTO = new TestResult
                    {
                        Id = test.Id,
                        IsTrialTest = false,
                        ResultType = TestRunResultType.CorrectAnswer
                    };

                    // test did not pass
                    if (errorCount != 0)
                    {
                        testResultDTO.CheckerDetails = new CheckerDetails { UserOutputFragment = $"{testResult.fullTitle} {testResult.err}" };
                        testResultDTO.ResultType = TestRunResultType.WrongAnswer;
                    }

                    result.Results.Add(testResultDTO);
                }
            }
            
            return result;
        }

        private string SavePythonCodeTemplateToTempFile()
        {
            string pythonCodeTemplate = this.PythonCodeTemplate.Replace("\\", "\\\\");
            return FileHelpers.SaveStringToTempFile(this.WorkingDirectory, pythonCodeTemplate);
        }

        private void SaveNginxFile() => FileHelpers.SaveStringToFile(this.NginxFileContent, this.NgingConfFilePath);
        

        private void SaveTestsToFiles(IEnumerable<TestContext> tests)
        {
            foreach (var test in tests)
            {
                var testInputContent = test.Input.Replace(UserApplicationHttpPortPlaceholder, this.PortNumber.ToString());
                testInputContent = this.ReplaceNodeModulesRequireStatementsInTests(testInputContent);
                FileHelpers.SaveStringToFile(testInputContent, FileHelpers.BuildPath(this.TestsPath, $"{test.Id}.js"));
            }
        }

        private string ReplaceNodeModulesRequireStatementsInTests(string testInputContent)
        {
            var requirePattern = new Regex(NodeModulesRequirePattern);
            var results = requirePattern.Matches(testInputContent);
            foreach (Match match in results)
            {
                string fullRequireStatement = match.Groups[0].ToString();
                string nodeModuleName = match.Groups[2].ToString();
                string nodeModulePath = FileHelpers.BuildPath(this.JSProjNodeModulesPath, nodeModuleName);
                string statementToReplaceWith = $"{fullRequireStatement.Replace(nodeModuleName, nodeModulePath)}";
                testInputContent = testInputContent.Replace(fullRequireStatement, statementToReplaceWith.Replace("\\","\\\\"));
            }

            return testInputContent;
        }

        protected override IExecutor CreateExecutor() => this.CreateExecutor(ProcessExecutorType.Standard);
    }
}
