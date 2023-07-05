namespace OJS.Workers.Common.Helpers
{
    using System;
    using System.Runtime.InteropServices;
    using static OJS.Workers.Common.Constants;

    public static class OsPlatformHelpers
    {
        public static bool IsWindows() =>
            RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

        public static bool IsUnix() => IsLinux() || IsMacOsX();

        public static bool IsDocker() =>
            Environment.GetEnvironmentVariable(AspNetCoreEnvironmentVariable) == "Docker";

        private static bool IsLinux() =>
            RuntimeInformation.IsOSPlatform(OSPlatform.Linux);

        private static bool IsMacOsX() =>
            RuntimeInformation.IsOSPlatform(OSPlatform.OSX);
    }
}
