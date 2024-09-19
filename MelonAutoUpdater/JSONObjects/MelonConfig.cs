using MelonLoader.TinyJSON;

namespace MelonAutoUpdater.JSONObjects
{
    /// <summary>
    /// Class used to deserialize mau.json files
    /// </summary>
    public class MelonConfig
    {
        /// <summary>
        /// If true, melon will be ignored in checking and updating
        /// </summary>
        [Include]
        [DecodeAlias("disabled", "Disable", "Disabled")]
        public bool disable { get; internal set; }

        /// <summary>
        /// List of file names that are allowed to be downloaded and installed through e.g. Github
        /// </summary>
        [Include]
        [DecodeAlias("AllowedFileDownloads")]
        public string[] allowedFileDownloads { get; internal set; }

        /// <summary>
        /// List of files/directories that should not be installed/copied over. Below are examples for format
        /// <para>Files with name: <c>test.dll</c></para>
        /// <para>Files on path: <c>TestDirectory/test.dll</c></para>
        /// <para>Directory with name: <c>TestDirectory</c></para>
        /// <para>Directory on path: <c>Test/TestDirectory</c></para>
        /// </summary>
        [Include]
        [DecodeAlias("DontInclude", "doNotInclude", "DoNotInclude")]
        public string[] dontInclude { get; internal set; }

        /// <inheritdoc cref="MelonAutoUpdater.JSONObjects.Platform" />
        [Include]
        [DecodeAlias("Platform", "extension", "Extension")]
        public Platform platform { get; internal set; }
    }

    /// <summary>
    /// Config regarding allowed/disallowed platform for class <see cref="MelonConfig"/>
    /// </summary>
    public class Platform
    {
        /// <summary>
        /// If true, list will be treated as whitelist, otherwise list will be treated as blacklist
        /// </summary>
        [Include]
        [DecodeAlias("Whitelist")]
        public bool whitelist { get; internal set; }

        /// <summary>
        /// List of all platforms that are whitelisted/blacklisted, depending on the <see cref="whitelist"/> property
        /// </summary>
        [Include]
        [DecodeAlias("List", "Platforms")]
        public string[] list { get; internal set; }
    }
}