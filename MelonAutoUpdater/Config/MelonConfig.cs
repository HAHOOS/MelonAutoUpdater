extern alias ml070;

using MelonAutoUpdater.Extensions;
using MelonAutoUpdater.Utils;

using ml070.MelonLoader.TinyJSON;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace MelonAutoUpdater.Config
{
    /// <summary>
    /// Class used to deserialize mau.json files
    /// </summary>
    public class MelonConfig
    {
        /// <summary>
        /// If true, melon will be ignored in checking and updating
        /// </summary>
        [JsonProperty]
        public bool Disable { get; set; }

        /// <summary>
        /// List of file names that are allowed to be downloaded and installed through e.g. Github
        /// <para>Set to null or don't include the variable in the JSON for all files to be able to be used</para>
        /// </summary>
        [JsonProperty]
        public string[] AllowedFileDownloads { get; set; }

        /// <summary>
        /// List of files/directories that should not be installed/copied over. Below are examples for format
        /// <para>Files with name: <c>test.dll</c></para>
        /// <para>Files on path: <c>TestDirectory/test.dll</c></para>
        /// <para>Directory with name: <c>TestDirectory</c></para>
        /// <para>Directory on path: <c>Test/TestDirectory</c></para>
        /// </summary>
        [JsonProperty]
        public string[] DontInclude { get; set; }

        /// <inheritdoc cref="ConfigPlatform" />
        [JsonProperty]
        public ConfigPlatform Platform { get; set; }

        /// <summary>
        /// Data for extensions, allows for custom configs for extensions
        /// <para>Key is the extension name, Value is <see cref="JToken"/> that contains all the provided data</para>
        /// </summary>
        [JsonProperty]
        public Dictionary<string, JToken> ExtensionMetaData { get; set; }

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
                    if (directory.Name == format)
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        /// <summary>
        /// Config regarding allowed/disallowed platforms (search extensions) for class <see cref="MelonConfig"/>
        /// </summary>
        public class ConfigPlatform
        {
            /// <summary>
            /// If true, list will be treated as whitelist, otherwise list will be treated as blacklist
            /// </summary>
            [JsonProperty]
            public bool Whitelist { get; set; }

            /// <summary>
            /// List of all platforms that are whitelisted/blacklisted, depending on the <see cref="Whitelist"/> property
            /// </summary>
            [JsonProperty]
            public string[] List { get; set; }

            /// <summary>
            /// Checks if the <see cref="ConfigPlatform"/> allows the platform used in <see cref="SearchExtension"/>
            /// </summary>
            /// <param name="extension">The extension to check</param>
            /// <param name="melonConfig">The <see cref="MelonConfig"/> to check the <see cref="ConfigPlatform"/> from, which will be used to check the whitelist/blacklist from</param>
            /// <returns>If <see langword="true"/>, the search extension can be used</returns>
            /// <exception cref="ArgumentNullException">Extension is null</exception>
            public static bool IsPlatformAllowed(SearchExtension extension, MelonConfig melonConfig)
            {
                if (melonConfig == null) return true;
                if (extension == null) throw new ArgumentNullException(nameof(extension));
                if (melonConfig.Platform != null)
                {
                    if (melonConfig.Platform.List == null) return true;
                    if (melonConfig.Platform.Whitelist)
                    {
                        if (melonConfig.Platform.List.Any(x => x == extension.Name)) return true;
                    }
                    else
                    {
                        if (!melonConfig.Platform.List.Any(x => x == extension.Name)) return true;
                    }
                }
                return false;
            }

            /// <summary>
            /// Checks if the <see cref="ConfigPlatform"/> allows the platform used in <see cref="SearchExtension"/>
            /// </summary>
            /// <param name="extension">The extension to check</param>
            /// <param name="platform">The <see cref="ConfigPlatform"/> to check the whitelist/blacklist from</param>
            /// <returns>If <see langword="true"/>, the search extension can be used</returns>
            /// <exception cref="ArgumentNullException">Extension is null</exception>
            public static bool IsPlatformAllowed(SearchExtension extension, ConfigPlatform platform)
            {
                if (platform == null) return true;
                if (extension == null) throw new ArgumentNullException(nameof(extension));
                if (platform.List == null) return true;
                if (platform.Whitelist)
                {
                    if (platform.List.Any(x => x == extension.Name)) return true;
                }
                else
                {
                    if (!platform.List.Any(x => x == extension.Name)) return true;
                }

                return false;
            }

            /// <summary>
            /// Checks if the current <see cref="ConfigPlatform"/> allows the platform used in <see cref="SearchExtension"/>
            /// </summary>
            /// <param name="extension">The extension to check</param>
            /// <returns>If <see langword="true"/>, the search extension can be used</returns>
            /// <exception cref="ArgumentNullException">Extension is null</exception>
            public bool IsPlatformAllowed(SearchExtension extension)
            {
                if (extension == null) throw new ArgumentNullException(nameof(extension));
                if (List == null) return true;
                if (Whitelist)
                {
                    if (List.Any(x => x == extension.Name)) return true;
                }
                else
                {
                    if (!List.Any(x => x == extension.Name)) return true;
                }
                return false;
            }
        }
    }
}