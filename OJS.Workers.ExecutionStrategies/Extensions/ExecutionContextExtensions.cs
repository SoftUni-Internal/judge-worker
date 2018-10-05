namespace OJS.Workers.ExecutionStrategies.Extensions
{
    using System.Diagnostics;
    using System.IO;
    using System.Text.RegularExpressions;

    using Ionic.Zip;

    using OJS.Workers.Common;

    public static class ExecutionContextExtensions
    {
        public static void SanitizeContent(
            this ExecutionContext context)
        {
            var frame = new StackFrame(1);

            var callingClassName = frame.GetMethod()?.DeclaringType?.Name;

            switch (callingClassName)
            {
                case nameof(CompileExecuteAndCheckExecutionStrategy):
                case nameof(CSharpPerformanceProjectTestsExecutionStrategy):
                case nameof(CSharpAspProjectTestsExecutionStrategy):
                case nameof(CSharpProjectTestsExecutionStrategy):
                case nameof(CSharpUnitTestsExecutionStrategy):
                case nameof(DotNetCoreProjectTestsExecutionStrategy):
                case nameof(DotNetCoreProjectExecutionStrategy):
                case nameof(DotNetCoreUnitTestsExecutionStrategy):
                case nameof(DotNetCoreTestRunnerExecutionStrategy):
                    DisableIntegratedSecurityInZipFile(context);
                    break;
            }
        }

        private static void DisableIntegratedSecurityInZipFile(ExecutionContext executionContext)
        {
            var zipExtension = Constants.ZipFileExtension.Substring(1);

            if (string.IsNullOrWhiteSpace(executionContext.AllowedFileExtensions) ||
                !executionContext.AllowedFileExtensions.Contains(zipExtension))
            {
                return;
            }

            var integratedSecuritySearchRegex = new Regex(
                "Integrated Security=true|Trusted_Connection=yes",
                RegexOptions.IgnoreCase);

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
                            var text = streamReader.ReadToEnd();
                            var sanitizedText = integratedSecuritySearchRegex.Replace(text, string.Empty);

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
    }
}