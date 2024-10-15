extern alias ml065;

using ml065.Semver;
using System;
using System.Collections.Generic;

namespace MelonAutoUpdater
{
    /// <summary>
    /// Class that contains data about a Melon<br/>
    /// Like: Latest Version and Download Files
    /// </summary>
    public class MelonData
    {
        /// <summary>
        /// Latest version available online of the Melon
        /// </summary>
        public SemVersion LatestVersion { get; internal set; }

        /// <summary>
        /// The URLs and to download the latest version of a mod and Content Type if provided
        /// </summary>
        public List<FileData> DownloadFiles { get; internal set; }

        /// <summary>
        /// The URI to a website where you can download the melon from
        /// </summary>
        public Uri DownloadLink { get; internal set; }

        /// <inheritdoc cref="MelonInstallSettings"/>
        public MelonInstallSettings InstallSettings { get; internal set; }

        /// <summary>
        /// Creates new instance of <see cref="MelonData" />
        /// </summary>
        /// <param name="latestVersion"><inheritdoc cref="LatestVersion"/></param>
        /// <param name="downloadFiles"><inheritdoc cref="DownloadFiles"/></param>
        /// <param name="downloadLink"><inheritdoc cref="DownloadLink"/></param>
        /// <param name="installSettings"><inheritdoc cref="InstallSettings"/></param>
        public MelonData(SemVersion latestVersion, List<FileData> downloadFiles, Uri downloadLink, MelonInstallSettings installSettings)
        {
            this.LatestVersion = latestVersion;
            this.DownloadFiles = downloadFiles;
            this.DownloadLink = downloadLink;
            this.InstallSettings = installSettings;
        }

        /// <summary>
        /// Creates new instance of <see cref="MelonData" />
        /// </summary>
        /// <param name="latestVersion"><inheritdoc cref="LatestVersion"/></param>
        /// <param name="downloadFiles"><inheritdoc cref="DownloadFiles"/></param>
        /// <param name="downloadLink"><inheritdoc cref="DownloadLink"/></param>
        public MelonData(SemVersion latestVersion, List<FileData> downloadFiles, Uri downloadLink)
        {
            this.LatestVersion = latestVersion;
            this.DownloadFiles = downloadFiles;
            this.DownloadLink = downloadLink;
        }

        /// <summary>
        /// Creates new instance of <see cref="MelonData" />
        /// </summary>
        /// <param name="latestVersion"><inheritdoc cref="LatestVersion"/></param>
        /// <param name="downloadFiles"><inheritdoc cref="DownloadFiles"/></param>
        public MelonData(SemVersion latestVersion, List<FileData> downloadFiles)
        {
            this.LatestVersion = latestVersion;
            this.DownloadFiles = downloadFiles;
        }

        /// <summary>
        /// Creates new instance of <see cref="MelonData" />
        /// </summary>
        /// <param name="latestVersion"><inheritdoc cref="LatestVersion"/></param>
        public MelonData(SemVersion latestVersion)
        {
            this.LatestVersion = latestVersion;
        }

        /// <summary>
        /// Creates new instance of <see cref="MelonData" />
        /// </summary>
        public MelonData()
        {
        }
    }

    /// <summary>
    /// Settings for installing files from updated melons
    /// </summary>
    public class MelonInstallSettings
    {
        /// <summary>
        /// List of all file names to be ignored
        /// </summary>
        public string[] IgnoreFiles { get; set; }
    }

    /// <summary>
    /// Type of file, either <see cref="ml065.MelonLoader.MelonMod"/>, <see cref="ml065.MelonLoader.MelonPlugin"/> or Other
    /// </summary>
    public enum FileType
    {
        /// <summary>
        /// Has attribute MelonInfo with type <see cref="ml065.MelonLoader.MelonMod"/> or is attribute <see cref="ml065.MelonLoader.MelonModInfoAttribute"/>
        /// </summary>
        MelonMod = 1,

        /// <summary>
        /// Has attribute MelonInfo with type <see cref="ml065.MelonLoader.MelonPlugin"/> or is attribute <see cref="ml065.MelonLoader.MelonPluginInfoAttribute"/>
        /// </summary>
        MelonPlugin = 2,

        /// <summary>
        /// Has no attribute identifying Melon's
        /// </summary>
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

        /// <summary>
        /// Creates new instance of <see cref="FileData"/>
        /// </summary>
        public FileData()
        { }

        /// <summary>
        /// Creates new instance of <see cref="FileData"/>
        /// </summary>
        /// <param name="url">URL to the file</param>
        /// <param name="fileName">File Name, if provided with response</param>
        /// <param name="contentType">Content Type returned by API</param>
        public FileData(string url, string fileName, string contentType)
        {
            this.URL = url;
            this.ContentType = contentType;
            this.FileName = fileName;
        }

        /// <summary>
        /// Creates new instance of <see cref="FileData"/>
        /// </summary>
        /// <param name="url">URL to the file</param>
        public FileData(string url)
        {
            this.URL = url;
        }

        /// <summary>
        /// Creates new instance of <see cref="FileData"/>
        /// </summary>
        /// <param name="url">URL to the file</param>
        /// <param name="contentType">Content Type returned by API</param>
        public FileData(string url, string contentType)
        {
            this.URL = url;
            this.ContentType = contentType;
        }
    }
}