﻿using Semver;
using System.Collections.Generic;

namespace MelonAutoUpdater
{
    /// <summary>
    /// Class that contains data about mod<br/>
    /// Like: Latest Version and Download Files
    /// </summary>
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
    /// Type of file, either <see cref="MelonLoader.MelonMod"/>, <see cref="MelonLoader.MelonPlugin"/> or Other
    /// </summary>
    public enum FileType
    {
        MelonMod = 1,
        MelonPlugin = 2,
        Other = 3
    }

    /// <summary>
    /// Data regarding file, including URL to download, the content-type if provided and name if provided
    /// </summary>
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