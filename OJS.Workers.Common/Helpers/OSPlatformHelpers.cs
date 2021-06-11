namespace OJS.Workers.Common.Helpers
{
    using System.Runtime.InteropServices;

    public static class OSPlatformHelpers
    {
        public static bool IsWindows() =>
            RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

        public static bool IsLinux() =>
            RuntimeInformation.IsOSPlatform(OSPlatform.Linux);

        public static bool IsMacOs() =>
            RuntimeInformation.IsOSPlatform(OSPlatform.OSX);

        // TODO: Determine if it is running in Docker by environment variable
        public static bool IsUnixOs() => IsLinux() || IsMacOs();
    }
}
