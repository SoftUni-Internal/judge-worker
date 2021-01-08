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
                // .NET Core
                { ExecutionStrategyType.DotNetCoreCompileExecuteAndCheck, "csharp-dot-net-core-code" },
                { ExecutionStrategyType.DotNetCoreProjectTestsExecutionStrategy, "dot-net-core-project-tests" },

                // .NET
                { ExecutionStrategyType.CompileExecuteAndCheck, "csharp-code" },

                // Python
                { ExecutionStrategyType.PythonExecuteAndCheck, "python-code" },

                // PHP
                { ExecutionStrategyType.PhpCliExecuteAndCheck, "php-code" },

                // C++
                { ExecutionStrategyType.CPlusPlusCompileExecuteAndCheckExecutionStrategy, "cpp-code" },

                // JavaScript
                { ExecutionStrategyType.NodeJsPreprocessExecuteAndCheck, "javascript-code" },
                { ExecutionStrategyType.NodeJsPreprocessExecuteAndRunUnitTestsWithMocha, "javascript-unit-tests-with-mocha" },
                { ExecutionStrategyType.NodeJsPreprocessExecuteAndRunJsDomUnitTests, "javascript-js-dom-unit-tests" },
                { ExecutionStrategyType.NodeJsExecuteAndRunAsyncJsDomTestsWithReactExecutionStrategy, "javascript-async-js-dom-tests-with-react" },
                { ExecutionStrategyType.NodeJsPreprocessExecuteAndRunCodeAgainstUnitTestsWithMochaExecutionStrategy, "javascript-code-against-unit-tests-with-mocha" },

                // SQL Server
                { ExecutionStrategyType.SqlServerLocalDbPrepareDatabaseAndRunQueries, "sql-server-prepare-db-and-run-queries" },
                { ExecutionStrategyType.SqlServerLocalDbRunQueriesAndCheckDatabase, "sql-server-run-queries-and-check-database" },

                // Java
                { ExecutionStrategyType.JavaPreprocessCompileExecuteAndCheck, "java-code" },
                { ExecutionStrategyType.JavaProjectTestsExecutionStrategy, "java-project-tests" },
                { ExecutionStrategyType.JavaUnitTestsExecutionStrategy, "java-unit-tests" },
                { ExecutionStrategyType.JavaZipFileCompileExecuteAndCheck, "java-zip-file-code" },

                // Plain text
                { ExecutionStrategyType.CheckOnly, "plaintext" },
            };

        public string Format(ExecutionStrategyType obj)
            => this.map.ContainsKey(obj)
                ? this.map[obj]
                : obj.ToString().ToHyphenSeparatedWords();
    }
}