namespace OJS.Workers.ExecutionStrategies.CodeSanitizers
{
    using System.Linq;
    using System.Text.RegularExpressions;

    /// <inheritdoc />
    public class CPlusPlusSanitizer : BaseCodeSanitizer
    {
        private const string ProcessAccessRightsPattern = @"(PROCESS_[A-Z_]+)|(0x0[0-9]+)";

        /// <inheritdoc/>
        protected override string DoSanitize(string content)
        {
            var functionsToDisable = new[]
            {
                "OpenProcess",
                "OpenThread",
                "GetProcessId",
                "GetThreadId",
                "GetCurrentProcess",
                "GetCurrentThread",
                "GetCurrentProcessId",
                "GetCurrentThreadId",
                "TerminateProcess",
                "TerminateThread",
                "SwitchToThread",
                "SuspendThread",
            }
            .Select(f => f + "\\s*\\(")
            .ToList();

            var functionsToDisableRegexPattern = string.Join("|", functionsToDisable);

            content = Regex.Replace(content, ProcessAccessRightsPattern, string.Empty);
            return Regex.Replace(content, functionsToDisableRegexPattern, string.Empty);
        }
    }
}