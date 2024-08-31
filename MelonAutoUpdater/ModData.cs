using System;
using System.Collections.Generic;

namespace MelonAutoUpdater
{
    public class ModData
    {
        /// <summary>
        /// Latest version available online of a mod
        /// </summary>
        public ModVersion LatestVersion { get; internal set; }

        /// <summary>
        /// The URLs & to download the latest version of a mod & Content Type if provided
        /// </summary>
        public List<FileData> DownloadFiles { get; internal set; }
    }

    public class ModVersion
    {
        public int Major { get; set; }
        public int Minor { get; set; }
        public int Patch { get; set; }

        /// <summary>
        /// Get version from string
        /// </summary>
        /// <param name="version">String version</param>
        /// <returns>ModVersion object with values Major, Minor and Patch</returns>
        public static ModVersion GetFromString(string version)
        {
            if (version.StartsWith("v")) version = version.Remove(0, 1);
            string[] split = version.Split('.');
            if (split.Length >= 3)
            {
                return new ModVersion()
                {
                    Major = int.Parse(split[0]),
                    Minor = int.Parse(split[1]),
                    Patch = int.Parse(split[2])
                };
            }
            return null;
        }

        public override string ToString()
        {
            return $"{Major}.{Minor}.{Patch}";
        }

        /// <summary>
        /// Compare two versions and check which is latest
        /// </summary>
        /// <param name="version1">First version</param>
        /// <param name="version2">Second version</param>
        /// <returns>A boolean value indicating if <b>version1</b> is greater than <b>version2</b>, returns null if the versions are the same</returns>
        public static bool? CompareVersions(ModVersion version1, ModVersion version2)
        {
            if (version1 == null) throw new ArgumentNullException(nameof(version1));
            else if (version2 == null) throw new ArgumentNullException(nameof(version2));

            if (version1.Major > version2.Major) return true;
            else if (version2.Major > version1.Major) return false;

            if (version1.Minor > version2.Minor) return true;
            else if (version2.Minor > version1.Minor) return false;

            if (version1.Patch > version2.Patch) return true;
            else if (version2.Patch > version1.Patch) return false;

            return null;
        }
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