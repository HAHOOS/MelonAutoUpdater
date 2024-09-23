using System;

namespace MelonAutoUpdater.Utils
{
    /// <summary>
    /// Class with utilities helping determine the Platform
    /// <para>Used for ML backwards compatibility</para>
    /// </summary>
    public static class Platform
    {
        /// <summary>
        /// Get the current <see cref="PlatformID"/>
        /// </summary>
        public static PlatformID GetPlatform => Environment.OSVersion.Platform;

        /// <summary>
        /// If <see langword="true"/>, platform is Unix
        /// </summary>
        public static bool IsUnix => GetPlatform is PlatformID.Unix;

        /// <summary>
        /// If <see langword="true"/>, platform is Windows
        /// </summary>
        public static bool IsWindows => GetPlatform == PlatformID.Win32NT || (GetPlatform == PlatformID.Win32S || (GetPlatform == PlatformID.Win32Windows || (GetPlatform == PlatformID.WinCE)));

        /// <summary>
        /// If <see langword="true"/>, platform is Mac
        /// </summary>
        public static bool IsMac => GetPlatform is PlatformID.MacOSX;
    }
}