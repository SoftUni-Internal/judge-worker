namespace OJS.Workers.ExecutionStrategies.Python
{
    internal static class PythonConstants
    {
        // flags
        public const string IsolatedModeArgument = "-I"; // https://docs.python.org/3/using/cmdline.html#cmdoption-I
        public const string OptimizeAndDiscardDocstringsArgument = "-OO"; // https://docs.python.org/3/using/cmdline.html#cmdoption-OO
        public const string IgnorePythonEnvVarsFlag = "-E"; // -E and -s are part of -I (isolated mode)
        public const string DontAddUserSiteDirectoryFlag = "-s";
        public const string ModuleFlag = "-m";

        // commands
        public const string DiscoverTestsCommandName = "discover";

        // modules
        public const string UnitTestModuleName = "unittest";
    }
}