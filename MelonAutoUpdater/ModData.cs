using Semver;
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

        /// <summary>
        /// Creates new instance of <see cref="ModData">
        /// </summary>
        /// <param name="latestVersion">Latest version available in the API</param>
        /// <param name="downloadFiles">List of <see cref="FileData"></param>
        public ModData(SemVersion latestVersion, List<FileData> downloadFiles)
        {
            this.LatestVersion = latestVersion;
            this.DownloadFiles = downloadFiles;
        }

        /// <summary>
        /// Creates new instance of <see cref="ModData">
        /// </summary>
        /// <param name="latestVersion">Latest version available in the API</param>
        /// <param name="downloadFiles">List of <see cref="FileData"></param>
        public ModData(SemVersion latestVersion)
        {
            this.LatestVersion = latestVersion;
        }

        /// <summary>
        /// Creates new instance of <see cref="ModData">
        /// </summary>
        public ModData()
        {
        }
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