namespace OJS.Workers.ExecutionStrategies.Helpers
{
    using OJS.Workers.Common.Helpers;

    using static OJS.Workers.Common.Constants;

    public static class PythonStrategiesHelper
    {
        private const string InitFileName = "__init__" + PythonFileExtension;

        public static void CreateInitFile(string directory)
            => FileHelpers.SaveStringToFileInDirectory(directory, InitFileName, string.Empty);
    }
}