namespace OJS.Workers.SubmissionProcessors.Formatters
{
    using System.Collections.Generic;

    using OJS.Workers.Common.Extensions;
    using OJS.Workers.Common.Models;

    public class ExecutionStrategyFormatterService
        : IExecutionStrategyFormatterService
    {
        private readonly IDictionary<ExecutionStrategyType, string> map;

        public ExecutionStrategyFormatterService()
            => this.map = new Dictionary<ExecutionStrategyType, string>()
            {
                { ExecutionStrategyType.DotNetCoreCompileExecuteAndCheck, "csharp-dot-net-core-code" },
                { ExecutionStrategyType.CompileExecuteAndCheck, "csharp-code" },
                { ExecutionStrategyType.JavaPreprocessCompileExecuteAndCheck, "java-code" },
                { ExecutionStrategyType.PythonExecuteAndCheck, "python-code" },
                { ExecutionStrategyType.PhpCliExecuteAndCheck, "php-code" },
                { ExecutionStrategyType.CPlusPlusCompileExecuteAndCheckExecutionStrategy, "cpp-code" },
                { ExecutionStrategyType.NodeJsPreprocessExecuteAndCheck, "javascript-code" },
                { ExecutionStrategyType.NodeJsPreprocessExecuteAndRunJsDomUnitTests, "javascript-dom-tests-code" },
                { ExecutionStrategyType.NodeJsPreprocessExecuteAndRunUnitTestsWithMocha, "javascript-tests-code" },
                { ExecutionStrategyType.NodeJsExecuteAndRunAsyncJsDomTestsWithReactExecutionStrategy, "javascript-dom-with-react-tests-code" },
                { ExecutionStrategyType.NodeJsPreprocessExecuteAndRunCodeAgainstUnitTestsWithMochaExecutionStrategy, "javascript-against-tests-code" },
            };

        public string Format(ExecutionStrategyType obj)
            => this.map.ContainsKey(obj)
                ? this.map[obj]
                : obj.ToString().ToHyphenSeparatedWords();
    }
}