namespace OJS.Workers.ExecutionStrategies.NodeJs
{
    using System;
    using System.IO;
    using OJS.Workers.Common;
    using OJS.Workers.Common.Helpers;
    using OJS.Workers.Executors;

    public class NodeJsProjectRunJavaScriptProjectAndTestsWithPlaywrigth
        : NodeJsPreprocessExecuteAndCheckExecutionStrategy
    {
        protected const string UserApplicationPathPlaceholder = "#userApplicationPath#";
        protected const string TestsPathPlaceholder = "#testsPath#";

        public NodeJsProjectRunJavaScriptProjectAndTestsWithPlaywrigth(
            IProcessExecutorFactory processExecutorFactory,
            string nodeJsExecutablePath,
            string mochaModulePath,
            string chaiModulePath,
            string httpServerModulePath,
            string underscoreModulePath,
            int portNumber,
            int baseTimeUsed,
            int baseMemoryUsed)
            : base(
                  processExecutorFactory,
                  nodeJsExecutablePath,
                  underscoreModulePath,
                  baseTimeUsed,
                  baseMemoryUsed)
        {
            // TODO: Check if this can be automated
            if (!File.Exists(mochaModulePath))
            {
                throw new ArgumentException(
                    $"Mocha not found in: {mochaModulePath}",
                    nameof(mochaModulePath));
            }

            if (!Directory.Exists(chaiModulePath))
            {
                throw new ArgumentException(
                    $"Chai not found in: {chaiModulePath}",
                    nameof(chaiModulePath));
            }

            if (!File.Exists(httpServerModulePath))
            {
                throw new ArgumentException(
                    $"HttpServer not found in: {httpServerModulePath}",
                    nameof(httpServerModulePath));
            }

            this.MochaModulePath = mochaModulePath;
            this.ChaiModulePath = FileHelpers.ProcessModulePath(chaiModulePath);
            this.HttpServerModulePath = FileHelpers.ProcessModulePath(httpServerModulePath);
            this.PortNumber = portNumber;
        }

        public string MochaModulePath { get; }

        public string ChaiModulePath { get; }

        public string HttpServerModulePath { get; }

        public int PortNumber { get; }

        public string TestsPath => "C:\\repos\\judge-strategies\\js-exam-poc\\test";

        protected string UserApplicationPath => "C:\\repos\\judge-strategies\\js-exam-poc";

        protected override string JsCodeTemplate => $@"
const {{ spawn }} = require('child_process');

const httpServerPort = {this.PortNumber};
const httpServerPath = '{this.HttpServerModulePath}';
const userApplicationPath = '{UserApplicationPathPlaceholder}';
const mochaPath = '{this.MochaModulePath}';
const testsPath = '{TestsPathPlaceholder}';

const httpServerProcess = spawn(
    httpServerPath,
    ['-P', `http://localhost:${{httpServerPort}}?`, '-s', '--cors', userApplicationPath]
);

const testsProcess = spawn(
    mochaPath,
    [testsPath]
);

testsProcess.stdout.on('data', (data) => {{
    console.log(`(PR2) stdout: ${{data}}`);
}});

testsProcess.stderr.on('data', (data) => {{
    console.error(`(PR2) stderr: ${{data}}`);
}});

testsProcess.on('close', (code) => {{
    console.log(`child process exited with code ${{code}}`);
    httpServerProcess.kill('SIGINT');
}});
";

        protected override string PreprocessJsSubmission<TInput>(string template, IExecutionContext<TInput> context)
            => template.Replace(UserApplicationPathPlaceholder, this.UserApplicationPath)
                .Replace(TestsPathPlaceholder, this.TestsPath);
    }
}
