﻿namespace OJS.Workers.ExecutionStrategies.Extensions
{
    using System.IO;
    using System.Runtime.CompilerServices;

    using OJS.Workers.Common;
    using OJS.Workers.ExecutionStrategies.CodeSanitizers;
    using OJS.Workers.ExecutionStrategies.CPlusPlus;
    using OJS.Workers.ExecutionStrategies.CSharp;
    using OJS.Workers.ExecutionStrategies.CSharp.DotNetCore;
    using OJS.Workers.ExecutionStrategies.CSharp.DotNetCore.V3;

    /// <summary>
    /// Extensions that operate on the execution context.
    /// </summary>
    public static class ExecutionContextExtensions
    {
        /// <summary>
        /// Processes the text of a submission and removes potentially harmful code from the execution context,
        /// using the corresponding sanitizer, depending on the caller strategy.
        /// </summary>
        /// <param name="executionContext">Execution context of the submission.</param>
        /// <param name="callerFilePath">The path of the class that has called this extension method.</param>
        /// <typeparam name="TInput">Type of the input.</typeparam>
        public static void SanitizeContent<TInput>(
            this IExecutionContext<TInput> executionContext,
            [CallerFilePath]string callerFilePath = null)
        {
            var callerClassName = Path.GetFileNameWithoutExtension(callerFilePath);

            switch (callerClassName)
            {
                case nameof(DotNetCoreProjectTestsExecutionStrategy):
                case nameof(DotNetCoreProjectExecutionStrategy):
                case nameof(DotNetCoreUnitTestsExecutionStrategy):
                    new DotNetCoreSanitizer().Sanitize(executionContext);
                    break;
                case nameof(CPlusPlusCompileExecuteAndCheckExecutionStrategy):
                case nameof(CPlusPlusZipFileExecutionStrategy):
                    new CPlusPlusSanitizer().Sanitize(executionContext);
                    break;
            }
        }
    }
}
