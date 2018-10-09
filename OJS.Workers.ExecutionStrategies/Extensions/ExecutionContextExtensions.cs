namespace OJS.Workers.ExecutionStrategies.Extensions
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Runtime.CompilerServices;
    using System.Text.RegularExpressions;

    using Ionic.Zip;

    using OJS.Workers.Common;

    public static class ExecutionContextExtensions
    {
        public static void SanitizeContent(
            this ExecutionContext executionContext,
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
            }
        }

        private static void SanitizeDotNetCoreZipFile(ExecutionContext executionContext)
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

        private static bool ExecutionContextContainsZipFile(ExecutionContext executionContext) =>
            !string.IsNullOrWhiteSpace(executionContext.AllowedFileExtensions) &&
            executionContext.AllowedFileExtensions.Contains(Constants.ZipFileExtension.Substring(1));
    }
}