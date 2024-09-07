using Semver;
using System.Collections.Generic;

namespace MelonAutoUpdater
{
    public class ModData
    {
        /// <summary>
        /// Latest version available online of a mod
        /// </summary>
        public SemVersion LatestVersion { get; internal set; }

        /// <summary>
        /// The URLs & to download the latest version of a mod & Content Type if provided
        /// </summary>
        public List<FileData> DownloadFiles { get; internal set; }
    }

    /// <summary>
    /// Type of file, either MelonMod, MelonPlugin or Other
    /// </summary>
    public enum FileType
    {
        MelonMod = 1,
        MelonPlugin = 2,
        Other = 3
    }

    public class FileData
    {
        /// <summary>
        /// URL to the file
        /// </summary>
        public string URL { get; internal set; }

        /// <summary>
        /// Content Type returned by API
        /// </summary>
        public string ContentType { get; internal set; }

        /// <summary>
        /// File Name, if provided with response
        /// </summary>
        public string FileName { get; internal set; }
    }
}