namespace OJS.Workers.ExecutionStrategies.Extensions
{
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.Text.RegularExpressions;

    using Ionic.Zip;

    using OJS.Workers.Common;

    public static class ExecutionContextExtensions
    {
        public static void SanitizeContent(this ExecutionContext executionContext)
        {
            var frame = new StackFrame(1);

            var callingClassName = frame.GetMethod()?.DeclaringType?.Name;

            switch (callingClassName)
            {
                case nameof(DotNetCoreProjectTestsExecutionStrategy):
                case nameof(DotNetCoreProjectExecutionStrategy):
                case nameof(DotNetCoreUnitTestsExecutionStrategy):
                case nameof(DotNetCoreTestRunnerExecutionStrategy):
                    SanitizeDotNetCoreZipFile(executionContext);
                    break;
            }
        }

        private static void SanitizeAllFilesInZip(
            ExecutionContext executionContext,
            Func<string, string> sanitizingFunc)
        {
            var zipExtension = Constants.ZipFileExtension.Substring(1);

            if (string.IsNullOrWhiteSpace(executionContext.AllowedFileExtensions) ||
                !executionContext.AllowedFileExtensions.Contains(zipExtension))
            {
                return;
            }

            var sanitizedZipFile = new ZipFile();

            using (var fileContentMemoryStream = new MemoryStream(executionContext.FileContent))
            {
                var zipFile = ZipFile.Read(fileContentMemoryStream);

                foreach (var entry in zipFile.Entries)
                {
                    using (var zipFileMemoryStream = new MemoryStream())
                    {
                        entry.Extract(zipFileMemoryStream);

                        zipFileMemoryStream.Seek(0, SeekOrigin.Begin);

                        using (var streamReader = new StreamReader(zipFileMemoryStream))
                        {
                            var sanitizedText = sanitizingFunc(streamReader.ReadToEnd());

                            sanitizedZipFile.AddEntry(entry.FileName, sanitizedText);
                        }
                    }
                }
            }

            using (var outputStream = new MemoryStream())
            {
                sanitizedZipFile.Save(outputStream);

                executionContext.FileContent = outputStream.ToArray();
            }
        }

        private static void SanitizeDotNetCoreZipFile(ExecutionContext executionContext)
        {
            const string connectionStringSearchPattern = @"(\.\s*UseSqlServer\s*\()(.*)(\))";
            const string safeConnectionString = "Data Source=.;";

            SanitizeAllFilesInZip(executionContext, SanitizeConnectionString);

            string SanitizeConnectionString(string fileContent) =>
                Regex.Replace(fileContent, connectionStringSearchPattern, $"$1\"{safeConnectionString}\"$3");
        }
    }
}