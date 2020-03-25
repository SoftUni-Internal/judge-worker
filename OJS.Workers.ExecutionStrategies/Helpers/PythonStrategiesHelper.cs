namespace OJS.Workers.ExecutionStrategies.Helpers
{
    using System.IO;

    using static OJS.Workers.Common.Constants;

    public static class PythonStrategiesHelper
    {
        private const string InitFileName = "__init__";

        public static void CreateInitFile(string directory)
        {
            var initFilePath = Path.Combine(directory, $"{InitFileName}{PythonFileExtension}");
            File.WriteAllText(initFilePath, string.Empty);
        }
    }
}