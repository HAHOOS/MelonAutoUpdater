using System;
using System.Collections.Generic;

namespace MelonAutoUpdater.Attributes
{
    /// <summary>
    /// Attribute that forces MAU to only download files with names (example <c>MAU-net6.zip</c>) that are listed when updating<br/>
    /// This does not affect file downloads that are not provided a name
    /// </summary>
    [AttributeUsage(AttributeTargets.Assembly)]
    public class MAUDownloadFilesAllowedAttribute : Attribute
    {
        /// <summary>
        /// List of file names (example <c>MAU-net6.zip</c>) that are allowed to be downloaded, if not on list, the download will be disregarded
        /// </summary>
        public List<string> AllowedFiles;

        /// <summary>
        /// Creates instance of MAUDownloadFilesAllowed Attribute
        /// </summary>
        /// <param name="AllowedFiles">List of file names (example <c>MAU-net6.zip</c>) that are allowed to be downloaded, if not on list, the download will be disregarded</param>
        public MAUDownloadFilesAllowedAttribute(List<string> AllowedFiles) => this.AllowedFiles = AllowedFiles;
    }
}