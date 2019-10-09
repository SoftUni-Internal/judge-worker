namespace OJS.Workers.ExecutionStrategies.Extensions
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Runtime.CompilerServices;
    using System.Text.RegularExpressions;

    using Ionic.Zip;

    using OJS.Workers.Common;
    using OJS.Workers.ExecutionStrategies.CPlusPlus;
    using OJS.Workers.ExecutionStrategies.CSharp;

    public static class ExecutionContextExtensions
    {
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
                case nameof(DotNetCoreTestRunnerExecutionStrategy):
                    SanitizeDotNetCoreZipFile(executionContext);
                    break;
                case nameof(CPlusPlusCompileExecuteAndCheckExecutionStrategy):
                case nameof(CPlusPlusZipFileExecutionStrategy):
                    SanitizeCPlusPlusCode(executionContext);
                    break;
            }
        }

        private static void SanitizeCPlusPlusCode<TInput>(IExecutionContext<TInput> executionContext)
        {
            var processAccessRightsPattern = @"(PROCESS_[A-Z_]+)|(0x0[0-9]+)";

            if (ExecutionContextContainsZipFile(executionContext))
            {
                executionContext.FileContent = SanitizeZipFileContent(
                    executionContext.FileContent,
                    SanitizeCode);
            }

            executionContext.Code = SanitizeCode(executionContext.Code);

            string SanitizeCode(string code)
                => Regex.Replace(code, processAccessRightsPattern, string.Empty);
        }

        private static void SanitizeDotNetCoreZipFile<TInput>(IExecutionContext<TInput> executionContext)
        {
            if (!ExecutionContextContainsZipFile(executionContext))
            {
                return;
            }

            var connectionStringSearchPattern = @"(\.\s*UseSqlServer\s*\()(.*)(\))";
            var safeConnectionString = "Data Source=.;";

            executionContext.FileContent = SanitizeZipFileContent(
                executionContext.FileContent,
                SanitizeConnectionStrings);

            string SanitizeConnectionStrings(string fileContent) =>
                Regex.Replace(fileContent, connectionStringSearchPattern, $"$1\"{safeConnectionString}\"$3");
        }

        private static byte[] SanitizeZipFileContent(byte[] zipFileContent, Func<string, string> sanitizingFunc)
        {
            var sanitizedZipFile = new ZipFile();

            using (var fileContentMemoryStream = new MemoryStream(zipFileContent))
            {
                var zipFile = ZipFile.Read(fileContentMemoryStream);

                foreach (var zipEntry in zipFile.Entries.Where(e => !e.IsDirectory))
                {
                    using (var memoryInputStream = new MemoryStream())
                    {
                        zipEntry.Extract(memoryInputStream);

                        memoryInputStream.Seek(0, SeekOrigin.Begin);

                        using (var streamReader = new StreamReader(memoryInputStream))
                        {
                            var sanitizedText = sanitizingFunc(streamReader.ReadToEnd());

                            sanitizedZipFile.AddEntry(zipEntry.FileName, sanitizedText);
                        }
                    }
                }
            }

            using (var outputStream = new MemoryStream())
            {
                sanitizedZipFile.Save(outputStream);

                return outputStream.ToArray();
            }
        }

        private static bool ExecutionContextContainsZipFile<TInput>(IExecutionContext<TInput> executionContext) =>
            !string.IsNullOrWhiteSpace(executionContext.AllowedFileExtensions) &&
            executionContext.AllowedFileExtensions.Contains(Constants.ZipFileExtension.Substring(1));
    }
}
