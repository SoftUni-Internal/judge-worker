namespace OJS.Workers.Common.Helpers
{
    using System.Runtime.InteropServices;

    public static class OSPlatformHelpers
    {
        public static bool IsWindows() =>
            RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

        // TODO: Determine if it is running in Docker by environment variable
        public static bool IsUnix() => IsLinux() || IsMacOsX();

        private static bool IsLinux() =>
            RuntimeInformation.IsOSPlatform(OSPlatform.Linux);

        private static bool IsMacOsX() =>
            RuntimeInformation.IsOSPlatform(OSPlatform.OSX);
    }
}
