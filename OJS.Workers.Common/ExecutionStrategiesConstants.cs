namespace OJS.Workers.Common
{
    using System.Collections.Generic;
    using System.Linq;

    using OJS.Workers.Common.Models;

    public static class ExecutionStrategiesConstants
    {
        public static class ExecutionStrategyNames
        {
            // .NET
            public const string CsharpCode = "csharp-code";

            // .NET Core
            public const string CsharpDotNetCoreCode = "csharp-dot-net-core-code";
            public const string CSharpDotNetCoreProjectTests = "dot-net-core-project-tests";
            public const string CSharpDotNetCore6ProjectTests = "dot-net-core-6-project-tests";
            public const string CSharpDotNetCoreProject = "dot-net-core-project";
            public const string CSharpDotNetCoreUnitTests = "dot-net-core-unit-tests";

            // Java
            public const string JavaCode = "java-code";
            public const string JavaProjectTests = "java-project-tests";
            public const string JavaUnitTests = "java-unit-tests";
            public const string JavaZipFileCode = "java-zip-file-code";

            // JavaScript
            public const string JavaScriptCode = "javascript-code";
            public const string JavaScriptJsDomUnitTests = "javascript-js-dom-unit-tests";
            public const string JavaScriptUnitTestsWithMocha = "javascript-unit-tests-with-mocha";
            public const string JavaScriptAsyncJsDomTestsWithReact = "javascript-async-js-dom-tests-with-react";

            public const string JavaScriptCodeAgainstUnitTestsWithMocha =
                "javascript-code-against-unit-tests-with-mocha";

            public const string JavaScriptCodeAgainstUnitTestsWithDomAndMocha =
                "javascript-code-against-unit-tests-with-dom-and-mocha";

            // Python
            public const string PythonCode = "python-code";
            public const string PythonCodeUnitTests = "python-code-unit-tests";
            public const string PythonProjectTests = "python-project-tests";
            public const string PythonProjectUnitTests = "python-project-unit-tests";

            // Php
            public const string PhpCode = "php-code";
            public const string PhpCodeCgi = "php-code-cgi";

            // HTML and CSS
            public const string HtmlAndCssZipFile = "html-and-css-zip-file";

            // C++
            public const string CppCode = "cpp-code";
            public const string CppZipFile = "cpp-zip-file";

            // Plain text
            public const string PlainText = "plaintext";

            // SqlServer
            public const string SqlServerPrepareDatabaseAndRunQueries = "sql-server-prepare-db-and-run-queries";
            public const string SqlServerRunQueriesAndCheckDatabase = "sql-server-run-queries-and-check-database";
            public const string SqlServerRunSkeletonRunQueriesAndCheckDatabase = "sql-server-run-skeleton-run-queries-and-check-database";

            // MySQL/MariaDb
            public const string MySqlPrepareDbAndRunQueries = "mysql-prepare-db-and-run-queries";
            public const string MySqlRunQueriesAndCheckDatabase = "mysql-run-queries-and-check-database";
            public const string MySqlRunSkeletonRunQueriesAndCheckDatabase = "mysql-run-skeleton-run-queries-and-check-database";

            // Run SPA and Execute mocha tests
            public const string RunSpaAndExecuteMochaTestsExecutionStrategy = "run-spa-and-execute-mocha-tests";
        }

        public static class NameMappings
        {
            public static readonly IDictionary<string, ExecutionStrategyType> NameToExecutionStrategyMappings =
                new Dictionary<string, ExecutionStrategyType>
                {
                    // .Net Core
                    { ExecutionStrategyNames.CsharpDotNetCoreCode, ExecutionStrategyType.DotNetCoreCompileExecuteAndCheck },
                    { ExecutionStrategyNames.CSharpDotNetCoreProject, ExecutionStrategyType.DotNetCoreProjectExecutionStrategy },
                    { ExecutionStrategyNames.CSharpDotNetCoreProjectTests, ExecutionStrategyType.DotNetCoreProjectTestsExecutionStrategy },
                    { ExecutionStrategyNames.CSharpDotNetCore6ProjectTests, ExecutionStrategyType.DotNetCore6ProjectTestsExecutionStrategy},
                    { ExecutionStrategyNames.CSharpDotNetCoreUnitTests, ExecutionStrategyType.DotNetCoreUnitTestsExecutionStrategy },

                    // Python
                    { ExecutionStrategyNames.PythonCode, ExecutionStrategyType.PythonExecuteAndCheck },
                    { ExecutionStrategyNames.PythonCodeUnitTests, ExecutionStrategyType.PythonCodeExecuteAgainstUnitTests },
                    { ExecutionStrategyNames.PythonProjectTests, ExecutionStrategyType.PythonProjectTests },
                    { ExecutionStrategyNames.PythonProjectUnitTests, ExecutionStrategyType.PythonProjectUnitTests },

                    // PHP
                    { ExecutionStrategyNames.PhpCode, ExecutionStrategyType.PhpCliExecuteAndCheck },
                    { ExecutionStrategyNames.PhpCodeCgi, ExecutionStrategyType.PhpCgiExecuteAndCheck },

                    // HTML
                    { ExecutionStrategyNames.HtmlAndCssZipFile, ExecutionStrategyType.NodeJsZipExecuteHtmlAndCssStrategy },

                    // C++
                    { ExecutionStrategyNames.CppCode, ExecutionStrategyType.CPlusPlusCompileExecuteAndCheckExecutionStrategy },
                    { ExecutionStrategyNames.CppZipFile, ExecutionStrategyType.CPlusPlusZipFileExecutionStrategy },

                    // JavaScript
                    { ExecutionStrategyNames.JavaScriptCode, ExecutionStrategyType.NodeJsPreprocessExecuteAndCheck },
                    { ExecutionStrategyNames.JavaScriptUnitTestsWithMocha, ExecutionStrategyType.NodeJsPreprocessExecuteAndRunUnitTestsWithMocha },
                    { ExecutionStrategyNames.JavaScriptJsDomUnitTests, ExecutionStrategyType.NodeJsPreprocessExecuteAndRunJsDomUnitTests },
                    { ExecutionStrategyNames.JavaScriptAsyncJsDomTestsWithReact, ExecutionStrategyType.NodeJsExecuteAndRunAsyncJsDomTestsWithReactExecutionStrategy },
                    { ExecutionStrategyNames.JavaScriptCodeAgainstUnitTestsWithMocha, ExecutionStrategyType.NodeJsPreprocessExecuteAndRunCodeAgainstUnitTestsWithMochaExecutionStrategy },
                    { ExecutionStrategyNames.JavaScriptCodeAgainstUnitTestsWithDomAndMocha, ExecutionStrategyType.NodeJsZipPreprocessExecuteAndRunUnitTestsWithDomAndMocha },

                    // Java
                    { ExecutionStrategyNames.JavaCode, ExecutionStrategyType.JavaPreprocessCompileExecuteAndCheck },
                    { ExecutionStrategyNames.JavaProjectTests, ExecutionStrategyType.JavaProjectTestsExecutionStrategy },
                    { ExecutionStrategyNames.JavaZipFileCode, ExecutionStrategyType.JavaZipFileCompileExecuteAndCheck },
                    { ExecutionStrategyNames.JavaUnitTests, ExecutionStrategyType.JavaUnitTestsExecutionStrategy },

                    // Plain text
                    { ExecutionStrategyNames.PlainText, ExecutionStrategyType.CheckOnly },

                    // Sql Server
                    { ExecutionStrategyNames.SqlServerPrepareDatabaseAndRunQueries, ExecutionStrategyType.SqlServerSingleDatabasePrepareDatabaseAndRunQueries },
                    { ExecutionStrategyNames.SqlServerRunQueriesAndCheckDatabase,  ExecutionStrategyType.SqlServerSingleDatabaseRunQueriesAndCheckDatabase },
                    { ExecutionStrategyNames.SqlServerRunSkeletonRunQueriesAndCheckDatabase, ExecutionStrategyType.SqlServerSingleDatabaseRunSkeletonRunQueriesAndCheckDatabase },

                    // MySQL/MariaDb
                    { ExecutionStrategyNames.MySqlPrepareDbAndRunQueries, ExecutionStrategyType.MySqlPrepareDatabaseAndRunQueries },
                    { ExecutionStrategyNames.MySqlRunQueriesAndCheckDatabase, ExecutionStrategyType.MySqlRunQueriesAndCheckDatabase },
                    { ExecutionStrategyNames.MySqlRunSkeletonRunQueriesAndCheckDatabase, ExecutionStrategyType.MySqlRunSkeletonRunQueriesAndCheckDatabase },

                    // Php
                    // { ExecutionStrategyNames.PhpCode, ExecutionStrategyType.PhpCliExecuteAndCheck },

                    // Run SPA and Execute mocha tests
                    { ExecutionStrategyNames.RunSpaAndExecuteMochaTestsExecutionStrategy, ExecutionStrategyType.RunSpaAndExecuteMochaTestsExecutionStrategy },
                };

            public static readonly IDictionary<ExecutionStrategyType, string> ExecutionStrategyToNameMappings =
                NameToExecutionStrategyMappings.ToDictionary(x => x.Value, y => y.Key);

            public static readonly ISet<ExecutionStrategyType> EnabledRemoteWorkerStrategies =
                new HashSet<ExecutionStrategyType>(NameToExecutionStrategyMappings.Values);

            public static readonly ISet<ExecutionStrategyType> DisabledLocalWorkerStrategies = new HashSet<ExecutionStrategyType>
            {
                // JS Project strategy
                ExecutionStrategyType.RunSpaAndExecuteMochaTestsExecutionStrategy,
                ExecutionStrategyType.CPlusPlusCompileExecuteAndCheckExecutionStrategy,

                // MySQL strategies
                ExecutionStrategyType.MySqlPrepareDatabaseAndRunQueries,
                ExecutionStrategyType.MySqlRunQueriesAndCheckDatabase,
                ExecutionStrategyType.MySqlRunSkeletonRunQueriesAndCheckDatabase,
            };
        }
    }
}