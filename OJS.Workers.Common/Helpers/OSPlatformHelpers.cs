namespace OJS.Workers.Common.Helpers
{
    using System.Runtime.InteropServices;

    public static class OSPlatformHelpers
    {
        public static bool IsWindows() =>
            RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

        public static bool IsLinux() =>
            RuntimeInformation.IsOSPlatform(OSPlatform.Linux);

        public static bool IsDockerContainer() => true;
    }
}
