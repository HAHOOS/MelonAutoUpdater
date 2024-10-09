extern alias ml065;

using ml065.MelonLoader.TinyJSON;
using System.IO;
using System.Linq;

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
        [DecodeAlias("disabled", "disable", "Disabled")]
        public bool Disable { get; set; }

        /// <summary>
        /// List of file names that are allowed to be downloaded and installed through e.g. Github
        /// </summary>
        [Include]
        [DecodeAlias("allowedFileDownloads")]
        public string[] AllowedFileDownloads { get; set; }

        /// <summary>
        /// List of files/directories that should not be installed/copied over. Below are examples for format
        /// <para>Files with name: <c>test.dll</c></para>
        /// <para>Files on path: <c>TestDirectory/test.dll</c></para>
        /// <para>Directory with name: <c>TestDirectory</c></para>
        /// <para>Directory on path: <c>Test/TestDirectory</c></para>
        /// </summary>
        [Include]
        [DecodeAlias("dontInclude", "doNotInclude", "DoNotInclude")]
        public string[] DontInclude { get; set; }

        /// <inheritdoc cref="JSONObjects.MelonConfig.Platform" />
        [Include]
        [DecodeAlias("platform", "extension", "Extension")]
        public ConfigPlatform Platform { get; set; }

        /// <summary>
        /// Checks if file or directory can be included
        /// </summary>
        /// <param name="path">Path to the file directory</param>
        /// <returns>If <see langword="true"/>, file/directory can be included</returns>
        // REVIEW: Actually check if this works
        public bool CanInclude(string path)
        {
            foreach (string format in DontInclude)
            {
                var file = new FileInfo(path);
                var directory = new DirectoryInfo(path);
                if (Path.HasExtension(path) && file.Exists)
                {
                    string fileName = file.Name;
                    string[] args = format.Split('/');
                    if (args.Length > 1)
                    {
                        args[args.Length] = null;
                        DirectoryInfo _rootPath = file.Directory;
                        bool _break = false;
                        foreach (var parent in args.Reverse())
                        {
                            if (_rootPath.Name != parent)
                            {
                                _break = true;
                                break;
                            }
                            else
                            {
                                _rootPath = _rootPath.Parent;
                            }
                        }
                        if (!_break) return false;
                    }
                    else
                    {
                        if (fileName == format) return false;
                    }
                }
                else if (directory.Exists)
                {
                    string[] args = format.Split('/');
                    if (args.Length > 1)
                    {
                        args[args.Length] = null;
                        DirectoryInfo _rootPath = directory.Parent;
                        bool _break = false;
                        foreach (var parent in args.Reverse())
                        {
                            if (_rootPath.Name != parent)
                            {
                                _break = true;
                                break;
                            }
                            else
                            {
                                _rootPath = _rootPath.Parent;
                            }
                        }
                        if (!_break) return false;
                    }
                    else
                    if (directory.Name == format) return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Config regarding allowed/disallowed platform for class <see cref="MelonConfig"/>
        /// </summary>
        public class ConfigPlatform
        {
            /// <summary>
            /// If true, list will be treated as whitelist, otherwise list will be treated as blacklist
            /// </summary>
            [Include]
            [DecodeAlias("whitelist")]
            public bool Whitelist { get; set; }

            /// <summary>
            /// List of all platforms that are whitelisted/blacklisted, depending on the <see cref="Whitelist"/> property
            /// </summary>
            [Include]
            [DecodeAlias("list", "Platforms", "platforms")]
            public string[] List { get; set; }
        }
    }
}