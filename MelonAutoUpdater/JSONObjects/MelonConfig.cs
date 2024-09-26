using MelonLoader.TinyJSON;
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
        [DecodeAlias("disabled", "Disable", "Disabled")]
        public bool disable { get; set; }

        /// <summary>
        /// List of file names that are allowed to be downloaded and installed through e.g. Github
        /// </summary>
        [Include]
        [DecodeAlias("AllowedFileDownloads")]
        public string[] allowedFileDownloads { get; set; }

        /// <summary>
        /// List of files/directories that should not be installed/copied over. Below are examples for format
        /// <para>Files with name: <c>test.dll</c></para>
        /// <para>Files on path: <c>TestDirectory/test.dll</c></para>
        /// <para>Directory with name: <c>TestDirectory</c></para>
        /// <para>Directory on path: <c>Test/TestDirectory</c></para>
        /// </summary>
        [Include]
        [DecodeAlias("DontInclude", "doNotInclude", "DoNotInclude")]
        public string[] dontInclude { get; set; }

        /// <inheritdoc cref="JSONObjects.MelonConfig.Platform" />
        [Include]
        [DecodeAlias("Platform", "extension", "Extension")]
        public Platform platform { get; set; }

        /// <summary>
        /// Checks if file or directory can be included
        /// </summary>
        /// <param name="path">Path to the file directory</param>
        /// <returns>If <see langword="true"/>, file/directory can be included</returns>
        public bool CanInclude(string path)
        {
            foreach (string format in dontInclude)
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
        public class Platform
        {
            /// <summary>
            /// If true, list will be treated as whitelist, otherwise list will be treated as blacklist
            /// </summary>
            [Include]
            [DecodeAlias("Whitelist")]
            public bool whitelist { get; set; }

            /// <summary>
            /// List of all platforms that are whitelisted/blacklisted, depending on the <see cref="whitelist"/> property
            /// </summary>
            [Include]
            [DecodeAlias("List", "Platforms")]
            public string[] list { get; set; }
        }
    }
}