extern alias ml065;

using MelonAutoUpdater.JSONObjects;
using MelonAutoUpdater.Extensions;
using MelonAutoUpdater.Utils;
using ml065.MelonLoader;
using ml065.MelonLoader.TinyJSON;
using Mono.Cecil;
using ml065.Semver;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using ml065.MelonLoader.ICSharpCode.SharpZipLib.Core;
using ml065.MelonLoader.ICSharpCode.SharpZipLib.Zip;
using System.Diagnostics;
using System.Net.Http;
using static ml065::MelonLoader.MelonPlatformAttribute;
using static ml065::MelonLoader.MelonPlatformDomainAttribute;

namespace MelonAutoUpdater
{
    internal class MelonUpdater
    {
        /// <summary>
        /// User Agent Header for all HTTP requests
        /// </summary>
        public static string UserAgent { get; private set; }

        /// <summary>
        /// Customizable colors, why does it exist? I don't know
        /// </summary>
        internal static Theme theme = Theme.Instance;

        /// <summary>
        /// List of all melons that should be ignored
        /// </summary>
        internal List<string> ignoreMelons;

        /// <summary>
        /// If <see langword="true"/>, brute check will be enabled and used
        /// </summary>
        internal bool bruteCheck = false;

        internal MelonUpdater(string userAgent, Theme _theme, List<string> ignoreMelons, bool bruteCheck = false)
        {
            UserAgent = userAgent;
            theme = _theme;
            this.ignoreMelons = ignoreMelons;
            this.bruteCheck = bruteCheck;
        }

        /// <summary>
        /// Get value of an entry in Melon Preferences
        /// </summary>
        /// <typeparam name="T">A type that will be returned as value of entry</typeparam>
        /// <param name="entry">The Melon Preferences Entry to retrieve value from</param>
        /// <returns>Value of entry with inputted type</returns>

        internal static T GetEntryValue<T>(MelonPreferences_Entry entry)
        {
            if (entry != null && entry.BoxedValue != null)
            {
                try
                {
                    return (T)entry.BoxedValue;
                }
                catch (InvalidCastException)
                {
                    Logger.Error($"Preference '{entry.DisplayName}' is of incorrect type");
                    return default;
                }
            }
            return default;
        }

        /// <summary>
        /// Copied from MelonLoader v0.6.4 to make it work with older versions
        /// </summary>
        internal static bool IsCompatible(VerifyLoaderVersionAttribute attribute, SemVersion version)
           => attribute.SemVer == null || version == null || (attribute.IsMinimum ? attribute.SemVer <= version : attribute.SemVer == version);

        /// <summary>
        /// Copied from MelonLoader v0.6.4 to make it work with older versions
        /// </summary>
        internal static bool IsCompatible(VerifyLoaderVersionAttribute attribute, string version)
            => !SemVersion.TryParse(version, out SemVersion ver) || IsCompatible(attribute, ver);

        /// <summary>
        /// Unzip a file from <see cref="Stream"/><br/>
        /// </summary>
        /// <param name="zipStream"><see cref="Stream"/> of the ZIP File</param>
        /// <param name="outFolder">Path to folder which will have the content of the zip</param>
        internal static void UnzipFromStream(Stream zipStream, string outFolder)
        {
            Stopwatch sw = null;
            if (MelonAutoUpdater.Debug)
            {
                sw = Stopwatch.StartNew();
            }
            using (var zipInputStream = new ZipInputStream(zipStream))
            {
                while (zipInputStream.GetNextEntry() is ZipEntry zipEntry)
                {
                    var entryFileName = zipEntry.Name;

                    var buffer = new byte[4096];

                    var fullZipToPath = Path.Combine(outFolder, entryFileName);
                    var directoryName = Path.GetDirectoryName(fullZipToPath);

                    if (directoryName.Length > 0)
                        Directory.CreateDirectory(directoryName);
                    if (Path.GetFileName(fullZipToPath).Length == 0)
                    {
                        continue;
                    }
                    using (FileStream streamWriter = File.Create(fullZipToPath))
                    {
                        StreamUtils.Copy(zipInputStream, streamWriter, buffer);
                    }
                }
            }
            if (MelonAutoUpdater.Debug)
            {
                sw.Stop();
                MelonAutoUpdater.ElapsedTime.Add($"Unzip-{MelonUtils.RandomString(5)}", sw.ElapsedMilliseconds);
            }
        }

        internal static bool CanSearch(SearchExtension extension, MelonConfig melonConfig)
        {
            if (melonConfig == null) return true;
            if (extension == null) throw new ArgumentNullException(nameof(extension));
            if (melonConfig.Platform != null)
            {
                if (melonConfig.Platform.List == null) return true;
                if (melonConfig.Platform.Whitelist)
                {
                    if (melonConfig.Platform.List.Where(x => x == extension.Name).Any()) return true;
                }
                else
                {
                    if (!melonConfig.Platform.List.Where(x => x == extension.Name).Any()) return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Get data about the mod from a downloadLink<br/>
        /// Currently Supported: Thunderstore, Github
        /// </summary>
        /// <param name="downloadLink">Download Link, possibly included in the <see cref="MelonInfoAttribute"/></param>
        /// <param name="currentVersion">Current version of the Melon in question</param>
        /// <param name="melonConfig">Config, if found, of the Melon</param>
        /// <returns>If found, returns a <see cref="MelonData"/> object which includes the latest version of the mod online and the download link(s)</returns>
        internal MelonData GetModData(string downloadLink, SemVersion currentVersion, MelonConfig melonConfig)
        {
            if (string.IsNullOrEmpty(downloadLink) || downloadLink == "UNKNOWN")
            {
                Logger.Msg("No download link was provided with the mod");
                return null;
            }
            foreach (var _ext in ExtensionBase.LoadedExtensions)
            {
                if (_ext.Type == typeof(SearchExtension))
                {
                    var ext = _ext as SearchExtension;
                    if (CanSearch(ext, melonConfig))
                    {
                        Logger.MsgPastel($"Checking {ext.Name.Pastel(ext.NameColor)}");
                        MelonData func() => ext.Search(downloadLink, currentVersion);
                        var result = Safe.SafeFunction<MelonData>(func);
                        if (result == null)
                        {
                            Logger.MsgPastel($"Nothing found with {ext.Name.Pastel(ext.NameColor)}");
                        }
                        else
                        {
                            Logger.MsgPastel($"Found data with {ext.Name.Pastel(ext.NameColor)}");
                            return result;
                        }
                    }
                    else
                    {
                        Logger.MsgPastel($"Unable to search with {ext.Name.Pastel(ext.NameColor)} as it has been configured to not be used");
                    }
                }
            }
            return null;
        }

        /// <summary>
        /// Get data about the mod from a name and author<br/>
        /// Github is not supported in brute checking due to extremely strict rate limits
        /// Currently Supported: Thunderstore
        /// </summary>
        /// <returns>If found, returns a <see cref="MelonData"/> object which includes the latest version of the mod online and the download link(s)</returns>
        internal MelonData GetModDataFromInfo(string name, string author, SemVersion currentVersion, MelonConfig melonConfig)
        {
            if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(author) || author == "UNKNOWN")
            {
                Logger.Msg("Name/Author was not provided with the mod");
                return null;
            }
            foreach (var _ext in ExtensionBase.LoadedExtensions)
            {
                if (_ext.Type == typeof(SearchExtension))
                {
                    var ext = _ext as SearchExtension;
                    if (ext.BruteCheckEnabled && (bool)ext.Entry_BruteCheckEnabled.BoxedValue)
                    {
                        if (CanSearch(ext, melonConfig))
                        {
                            Logger.MsgPastel($"Brute checking with {ext.Name.Pastel(ext.NameColor)}");
                            MelonData func() => ext.BruteCheck(name, author, currentVersion);
                            var result = Safe.SafeFunction<MelonData>(func);
                            if (result == null)
                            {
                                Logger.MsgPastel($"Nothing found with {ext.Name.Pastel(ext.NameColor)}");
                            }
                            else
                            {
                                Logger.MsgPastel($"Found data with {ext.Name.Pastel(ext.NameColor)}");
                                return result;
                            }
                        }
                        else
                        {
                            Logger.MsgPastel($"Unable to brute check with {ext.Name.Pastel(ext.NameColor)} as it has been configured to not be used");
                        }
                    }
                    else
                    {
                        Logger.MsgPastel($"Brute checking disabled in {ext.Name.Pastel(ext.NameColor)}");
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Check if an assembly is a <see cref="MelonMod"/>, a <see cref="MelonPlugin"/> or something else
        /// </summary>
        /// <param name="assembly"><see cref="AssemblyDefinition"/> of the file</param>
        /// <returns>A FileType, either <see cref="MelonMod"/>, <see cref="MelonPlugin"/> or Other</returns>
        internal static FileType GetFileType(AssemblyDefinition assembly)
        {
            MelonInfoAttribute infoAttribute = GetMelonInfo(assembly);

            if (infoAttribute != null)
            {
                return infoAttribute.SystemType == typeof(MelonMod) ? FileType.MelonMod : infoAttribute.SystemType == typeof(MelonPlugin) ? FileType.MelonPlugin : FileType.Other;
            }

            return FileType.Other;
        }

        /// <summary>
        /// Check if an assembly is a <see cref="MelonMod"/>, a <see cref="MelonPlugin"/> or something else
        /// </summary>
        /// <param name="infoAttribute"><see cref="MelonInfoAttribute"/> of the assembly</param>
        /// <returns>A FileType, either <see cref="MelonMod"/>, <see cref="MelonPlugin"/> or Other</returns>
        internal static FileType GetFileType(MelonInfoAttribute infoAttribute)
        {
            if (infoAttribute != null)
            {
                return infoAttribute.SystemType == typeof(MelonMod) ? FileType.MelonMod : infoAttribute.SystemType == typeof(MelonPlugin) ? FileType.MelonPlugin : FileType.Other;
            }

            return FileType.Other;
        }

        /// <summary>
        /// Get name of a directory
        /// </summary>
        /// <param name="path">Path to the directory</param>
        /// <returns>Name of directory</returns>
        internal static string GetDirName(string path)
        {
            path.Replace(" ", "_");
            var info = new DirectoryInfo(path);
            if (info != null)
            {
                return info.Name;
            }
            return path;
        }

        /// <summary>
        /// Move all files from one directory to another
        /// </summary>
        /// <param name="path">A path to directory to copy from</param>
        /// <param name="directory">A path to directory to copy to</param>
        /// <param name="mainDirectoryName">Only used in prefix, just set <see cref="string.Empty"/></param>
        /// <param name="latestVersion">The latest version of the mod the files are from</param>
        /// <param name="config">Config of the Melon</param>
        /// <returns>Info about melon install (times when it succeeded, times when it failed, and if it threw an error)</returns>
        internal (int success, int failed, bool threwError) MoveAllFiles(string path, string directory, string mainDirectoryName, SemVersion latestVersion, MelonConfig config)
        {
            Stopwatch sw = null;
            if (MelonAutoUpdater.Debug)
            {
                sw = Stopwatch.StartNew();
            }
            int success = 0;
            int failed = 0;
            bool threwError = false;
            string prefix = (string.IsNullOrEmpty(mainDirectoryName) != true ? $"{mainDirectoryName}/{GetDirName(directory)}" : GetDirName(directory)).Pastel(Color.Cyan);
            foreach (string file in Directory.GetFiles(path))
            {
                if (config != null && !config.CanInclude(file))
                {
                    Logger.MsgPastel($"[{prefix}] {Path.GetFileName(file)} will not be loaded due to the Melon being configured this way");
                    continue;
                }
                Logger.MsgPastel($"[{prefix}] {Path.GetFileName(file)} found, copying file to folder");
                try
                {
                    string _path = Path.Combine(directory, Path.GetFileName(file));
                    if (Path.GetExtension(file) == ".dll")
                    {
                        var res = InstallPackage(file, latestVersion);
                        if (res.threwError || res.success)
                        {
                            if (res.threwError) threwError = true;
                            if (res.success) success++;
                            else failed++;
                        }
                    }
                    else
                    {
                        if (!File.Exists(_path)) File.Move(file, _path);
                        else File.Replace(file, _path, Path.Combine(Files.BackupDirectory, $"{Path.GetFileName(path)}-{DateTimeOffset.Now.ToUnixTimeSeconds()}.{Path.GetExtension(file)}"));
                        Logger.MsgPastel($"[{prefix}] Successfully copied {Path.GetFileName(file)}");
                    }
                }
                catch (Exception ex)
                {
                    Logger.Error($"[{prefix}] Failed to copy {Path.GetFileName(file)}, exception thrown:{ex}");
                }
            }
            foreach (string dir in Directory.GetDirectories(path))
            {
                if (config != null && !config.CanInclude(dir))
                {
                    Logger.MsgPastel($"[{prefix}] {GetDirName(dir)} will not be loaded due to the Melon being configured this way");
                    continue;
                }
                Logger.MsgPastel($"[{prefix}] Found folder {GetDirName(dir)}, going through files");
                try
                {
                    string _path = Path.Combine(directory, GetDirName(dir));
                    if (!Directory.Exists(_path)) Directory.CreateDirectory(_path);
                    var res = MoveAllFiles(dir, _path, prefix, latestVersion, config);
                    if (res.threwError) threwError = true;
                    success += res.success;
                    failed += res.failed;
                }
                catch (Exception ex)
                {
                    Logger.Error($"[{prefix}] Failed to copy folder {GetDirName(dir)}, exception thrown:{ex}");
                }
            }
            if (MelonAutoUpdater.Debug)
            {
                sw.Stop();
                MelonAutoUpdater.ElapsedTime.Add($"MoveFiles-{GetDirName(path)}", sw.ElapsedMilliseconds);
            }
            return (success, failed, threwError);
        }

        /// <summary>
        /// Get value from a custom attribute
        /// </summary>
        /// <typeparam name="T"><see cref="Type"/> that will be returned as value</typeparam>
        /// <param name="customAttribute">The custom attribute you want to get value from</param>
        /// <param name="index">Index of the value</param>
        /// <returns>A value from the Custom Attribute with provided <see cref="Type"/></returns>
        internal static T Get<T>(CustomAttribute customAttribute, int index)
        {
            if (customAttribute == null || !customAttribute.HasConstructorArguments || customAttribute.ConstructorArguments.Count <= 0) return default;
            return (T)customAttribute.ConstructorArguments[index].Value;
        }

        /// <summary>
        /// Retrieve information from the <see cref="MelonInfoAttribute"/> in a file using Mono.Cecil
        /// </summary>
        /// <param name="assembly"><see cref="AssemblyDefinition"/> of the file</param>
        /// <returns>If present, returns a <see cref="MelonInfoAttribute"/></returns>

        internal static MelonInfoAttribute GetMelonInfo(AssemblyDefinition assembly)
        {
            foreach (var attr in assembly.CustomAttributes)
            {
#pragma warning disable CS0618 // Type or member is obsolete
                if (attr.AttributeType.Name == nameof(MelonInfoAttribute)
                    || attr.AttributeType.Name == nameof(MelonModInfoAttribute)
                    || attr.AttributeType.Name == nameof(MelonPluginInfoAttribute))
                {
                    var _type = Get<TypeDefinition>(attr, 0);
                    Type type = _type.BaseType.Name == "MelonMod" ? typeof(MelonMod) : _type.BaseType.Name == "MelonPlugin" ? typeof(MelonPlugin) : null;
                    string Name = Get<string>(attr, 1);
                    string Version = Get<string>(attr, 2);
                    string Author = Get<string>(attr, 3);
                    string DownloadLink = Get<string>(attr, 4);

                    assembly.Dispose();

                    return new MelonInfoAttribute(type: type, name: Name, version: Version, author: Author, downloadLink: DownloadLink);
                }
#pragma warning restore CS0618 // Type or member is obsolete
            }
            assembly.Dispose();
            return null;
        }

        /// <summary>
        /// Retrieve information from the <see cref="VerifyLoaderVersionAttribute"/> in a file using Mono.Cecil
        /// </summary>
        /// <param name="assembly"><see cref="Assembly"/> of the file</param>
        /// <returns>If present, returns a <see cref="VerifyLoaderVersionAttribute"/></returns>
        internal static VerifyLoaderVersionAttribute GetLoaderVersionRequired(AssemblyDefinition assembly)
        {
            foreach (var attr in assembly.CustomAttributes)
            {
                if (attr.AttributeType.Name == nameof(VerifyLoaderVersionAttribute))
                {
                    try
                    {
                        int major = Get<int>(attr, 0);
                        int minor = Get<int>(attr, 1);
                        int patch = Get<int>(attr, 2);
                        bool isMinimum = Get<bool>(attr, 3);
                        return new VerifyLoaderVersionAttribute(major, minor, patch, isMinimum);
                    }
                    catch (Exception)
                    {
                        string version = Get<string>(attr, 0);
                        bool isMinimum = Get<bool>(attr, 1);
                        assembly.Dispose();
                        return new VerifyLoaderVersionAttribute(version, isMinimum);
                    }
                }
            }
            assembly.Dispose();
            return null;
        }

        /// <summary>
        /// Retrieve information from the <see cref="MelonGameAttribute"/> in a file using Mono.Cecil
        /// </summary>
        /// <param name="assembly"><see cref="AssemblyDefinition"/> of the file</param>
        /// <returns>If present, returns a <see cref="MelonGameAttribute"/></returns>
        internal static MelonGameAttribute[] GetMelonGameAttribute(AssemblyDefinition assembly)
        {
            List<MelonGameAttribute> games = new List<MelonGameAttribute>();
            foreach (var attr in assembly.CustomAttributes)
            {
#pragma warning disable CS0618 // Type or member is obsolete
                if (attr.AttributeType.Name == nameof(MelonGameAttribute)
                    || attr.AttributeType.Name == nameof(MelonModGameAttribute)
                    || attr.AttributeType.Name == nameof(MelonPluginGameAttribute))
                {
                    string developer = Get<string>(attr, 0);
                    string name = Get<string>(attr, 1);
                    games.Add(new MelonGameAttribute(developer, name));
                }
#pragma warning restore CS0618 // Type or member is obsolete
            }
            assembly.Dispose();
            return games.ToArray();
        }

        /// <summary>
        /// Retrieve information from the <see cref="MelonProcessAttribute"/> in a file using Mono.Cecil
        /// </summary>
        /// <param name="assembly"><see cref="AssemblyDefinition"/> of the file</param>
        /// <returns>If present, returns a <see cref="MelonProcessAttribute"/></returns>
        internal static MelonProcessAttribute[] GetMelonProcessAttribute(AssemblyDefinition assembly)
        {
            List<MelonProcessAttribute> games = new List<MelonProcessAttribute>();
            foreach (var attr in assembly.CustomAttributes)
            {
                if (attr.AttributeType.Name == nameof(MelonProcessAttribute))
                {
                    string exe = Get<string>(attr, 0);
                    games.Add(new MelonProcessAttribute(exe));
                }
            }
            assembly.Dispose();
            return games.ToArray();
        }

        /// <summary>
        /// Retrieve information from the <see cref="MelonPlatformAttribute"/> in a file using Mono.Cecil
        /// </summary>
        /// <param name="assembly"><see cref="AssemblyDefinition"/> of the file</param>
        /// <returns>If present, returns a <see cref="MelonPlatformAttribute"/></returns>
        internal static MelonPlatformAttribute GetMelonPlatformAttribute(AssemblyDefinition assembly)
        {
            foreach (var attr in assembly.CustomAttributes)
            {
                if (attr.AttributeType.Name == nameof(MelonPlatformAttribute))
                {
                    CompatiblePlatforms platforms = Get<CompatiblePlatforms>(attr, 0);
                    assembly.Dispose();
                    return new MelonPlatformAttribute(platforms);
                }
            }
            assembly.Dispose();
            return null;
        }

        /// <summary>
        /// Retrieve information from the <see cref="VerifyLoaderBuildAttribute"/> in a file using Mono.Cecil
        /// </summary>
        /// <param name="assembly"><see cref="AssemblyDefinition"/> of the file</param>
        /// <returns>If present, returns a <see cref="VerifyLoaderBuildAttribute"/></returns>
        internal static VerifyLoaderBuildAttribute GetVerifyLoaderBuildAttribute(AssemblyDefinition assembly)
        {
            foreach (var attr in assembly.CustomAttributes)
            {
                if (attr.AttributeType.Name == nameof(VerifyLoaderBuildAttribute))
                {
                    string build = Get<string>(attr, 0);
                    assembly.Dispose();
                    return new VerifyLoaderBuildAttribute(build);
                }
            }
            assembly.Dispose();
            return null;
        }

        /// <summary>
        /// Retrieve information from the <see cref="MelonPlatformDomainAttribute"/> in a file using Mono.Cecil
        /// </summary>
        /// <param name="assembly"><see cref="AssemblyDefinition"/> of the file</param>
        /// <returns>If present, returns a <see cref="MelonPlatformDomainAttribute"/></returns>
        internal static MelonPlatformDomainAttribute GetMelonPlatformDomainAttribute(AssemblyDefinition assembly)
        {
            foreach (var attr in assembly.CustomAttributes)
            {
                if (attr.AttributeType.Name == nameof(MelonPlatformDomainAttribute))
                {
                    CompatibleDomains domains = Get<CompatibleDomains>(attr, 0);
                    assembly.Dispose();
                    return new MelonPlatformDomainAttribute(domains);
                }
            }
            assembly.Dispose();
            return null;
        }

        /// <summary>
        /// Retrieve information from the <see cref="MelonGameVersionAttribute"/> in a file using Mono.Cecil
        /// </summary>
        /// <param name="assembly"><see cref="AssemblyDefinition"/> of the file</param>
        /// <returns>If present, returns a <see cref="MelonGameAttribute"/></returns>
        internal static MelonGameVersionAttribute[] GetMelonGameVersionAttribute(AssemblyDefinition assembly)
        {
            List<MelonGameVersionAttribute> versions = new List<MelonGameVersionAttribute>();
            foreach (var attr in assembly.CustomAttributes)
            {
#pragma warning disable CS0618 // Type or member is obsolete
                if (attr.AttributeType.Name == nameof(MelonGameVersionAttribute))
                {
                    string version = Get<string>(attr, 0);
                    versions.Add(new MelonGameVersionAttribute(version));
                }
#pragma warning restore CS0618 // Type or member is obsolete
            }
            assembly.Dispose();
            return versions.ToArray();
        }

        internal MelonConfig GetMelonConfig(AssemblyDefinition assembly)
        {
            var resources = assembly.MainModule.Resources;
            var assemblyName = Path.GetFileNameWithoutExtension(assembly.MainModule.Name);
            foreach (EmbeddedResource resource in resources.Cast<EmbeddedResource>())
            {
                if (resource.Name == $"{assemblyName}.mau.json")
                {
                    try
                    {
                        var stream = resource.GetResourceStream();
                        var streamReader = new StreamReader(stream);
                        string jsonString = streamReader.ReadToEnd();
                        var json = JSON.Load(jsonString).Make<MelonConfig>();
                        return json;
                    }
                    catch (Exception e)
                    {
                        Logger.Error($"An unexpected error was thrown while getting mau.json\n{e}");
                    }
                }
            }
            return null;
        }

        public bool IsBuildCompatible(VerifyLoaderBuildAttribute attr, string hashCode)
             => attr == null || string.IsNullOrEmpty(attr.HashCode) || string.IsNullOrEmpty(hashCode) || attr.HashCode == hashCode;

        public bool IsDomainCompatible(MelonPlatformDomainAttribute attr, CompatibleDomains domain)
           => attr.Domain == CompatibleDomains.UNIVERSAL || domain == CompatibleDomains.UNIVERSAL || attr.Domain == domain;

        public bool IsPlatformCompatible(MelonPlatformAttribute attr, CompatiblePlatforms platform)
            => attr.Platforms == null || attr.Platforms.Length == 0 || attr.Platforms.Contains(platform);

        public bool IsProcessCompatible(MelonProcessAttribute attr, string processName)
            => attr.Universal || string.IsNullOrEmpty(processName) || (RemoveExtension(processName) == attr.EXE_Name);

        private string RemoveExtension(string name)
            => name == null ? null : (name.EndsWith(".exe") ? name.Remove(name.Length - 4) : name);

        /// <summary>
        /// Checks if the <see cref="Assembly"/> is compatible with the current ML Instance
        /// </summary>
        /// <param name="assembly"><see cref="AssemblyDefinition"/> to check</param>
        /// <returns><see langword="true"/>, if compatible, otherwise <see langword="false"/></returns>
        internal bool CheckCompability(AssemblyDefinition assembly)
        {
            CompatiblePlatforms CurrentPlatform = MelonUtils.IsGame32Bit() ? MelonPlatformAttribute.CompatiblePlatforms.WINDOWS_X86 : MelonPlatformAttribute.CompatiblePlatforms.WINDOWS_X64; // Temporarily
            CompatibleDomains CurrentDomain = MelonUtils.IsGameIl2Cpp() ? MelonPlatformDomainAttribute.CompatibleDomains.IL2CPP : MelonPlatformDomainAttribute.CompatibleDomains.MONO;

            bool _return = true;

            var name = AssemblyNameReference.Parse(MelonAutoUpdater.MLAssembly.FullName);
            assembly.MainModule.AssemblyReferences.Add(name);

            var modInfo = GetMelonInfo(assembly);
            var loaderVer = GetLoaderVersionRequired(assembly);
            var game = GetMelonGameAttribute(assembly);
            var gameVers = GetMelonGameVersionAttribute(assembly);
            var process = GetMelonProcessAttribute(assembly);
            var platform = GetMelonPlatformAttribute(assembly);
            var domain = GetMelonPlatformDomainAttribute(assembly);
            var build = GetVerifyLoaderBuildAttribute(assembly);

            if (!(loaderVer == null || IsCompatible(loaderVer, MelonAutoUpdater.MLVersion)))
            {
                Logger.Warning($"{modInfo.Name} {modInfo.Version} is not compatible with the current version of MelonLoader : v{MelonAutoUpdater.MLVersion}");
                Logger.Warning($"Compatible Versions:");
                Logger.Warning($"    - v{loaderVer.SemVer} {(loaderVer.IsMinimum ? "or higher" : "")}");
                _return = false;
            }
            else if (!(build == null || IsBuildCompatible(build, MelonUtils.HashCode)))
            {
                Logger.Warning($"{modInfo.Name} {modInfo.Version} is not compatible with the current build hash code of MelonLoader : {MelonUtils.HashCode}");
                Logger.Warning($"Compatible Build Hash Codes:");
                Logger.Warning($"    - v{build.HashCode}");
                _return = false;
            }
            bool net6 = Environment.Version.Major >= 6;
            if (!net6)
            {
                bool isFramework = assembly.MainModule.AssemblyReferences.Where(x => x.Name == "mscorlib") != null;
                if (!isFramework)
                {
                    Logger.Error($"{modInfo.Name} {modInfo.Version} is not compatible with .NET Framework");
                    _return = false;
                }
            }
#pragma warning disable CS0618 // Type or member is obsolete
            var gameName = ml065.MelonLoader.MelonUtils.GameName;
            var gameDev = ml065.MelonLoader.MelonUtils.GameDeveloper;
            var gameVer = ml065.MelonLoader.MelonUtils.GameVersion;
#pragma warning restore CS0618 // Type or member is obsolete
            if (!(game.Length == 0 || game.Any(x => x.IsCompatible(gameDev, gameName))))
            {
                Logger.Warning($"{modInfo.Name} {modInfo.Version} is not compatible with the running game: {gameName} (by {gameDev})");
                Logger.Warning($"Compatible Games:");
                foreach (var g in game)
                {
                    Logger.Warning($"=  - {g.Name} by {g.Developer}");
                }
                _return = false;
            }
            else
            {
                if (!(gameVers.Length == 0 || gameVers.Any(x => x.Version == gameVer)))
                {
                    Logger.Warning($"{modInfo.Name} {modInfo.Version} is not compatible with the version of the running game: {gameVer}");
                    Logger.Warning($"Compatible Game Versions:");
                    foreach (var g in gameVers)
                    {
                        Logger.Warning($"   - {g.Version}");
                    }
                    _return = false;
                }
                var processName = Process.GetCurrentProcess().ProcessName;
                if (!(process.Length == 0 || process.Any(x => IsProcessCompatible(x, processName))))
                {
                    Logger.Warning($"{modInfo.Name} {modInfo.Version} is not compatible with the running process: {processName}");
                    Logger.Warning($"Compatible Processes:");
                    foreach (var g in process)
                    {
                        Logger.Warning($"   - {g.EXE_Name}");
                    }
                    _return = false;
                }

                if (!(platform == null || IsPlatformCompatible(platform, CurrentPlatform)))
                {
                    Logger.Warning($"{modInfo.Name} {modInfo.Version} is not compatible with the current platform: {CurrentPlatform}");
                    Logger.Warning($"Compatible Platforms:");
                    foreach (var p in platform.Platforms)
                    {
                        Logger.Warning($"   - {p}");
                    }
                    _return = false;
                }
                if (!(domain == null || IsDomainCompatible(domain, CurrentDomain)))
                {
                    Logger.Warning($"{modInfo.Name} {modInfo.Version} is not compatible with the current platform: {CurrentDomain}");
                    Logger.Warning($"Compatible Domain:");
                    Logger.Warning($"   - {domain.Domain}");
                    _return = false;
                }
            }

            return _return;
        }

        /// <summary>
        /// Installs melon from path
        /// </summary>
        /// <param name="path">Path of melon</param>
        /// <param name="latestVersion">Latest version of melon, used to modify <see cref="MelonInfoAttribute"/> in case the version is not correct</param>
        /// <returns>A <see langword="Tuple"/>, success and threwError, self explanatory</returns>
        internal (bool success, bool threwError) InstallPackage(string path, SemVersion latestVersion)
        {
            Stopwatch sw = null;
            if (MelonAutoUpdater.Debug)
            {
                sw = Stopwatch.StartNew();
            }
            bool success = false;
            bool threwError = false;
            string fileName = Path.GetFileName(path);
            AssemblyDefinition _assembly = AssemblyDefinition.ReadAssembly(path, new ReaderParameters() { AssemblyResolver = new CustomCecilResolver() });
            FileType _fileType = GetFileType(_assembly);
            if (_fileType == FileType.MelonMod)
            {
                try
                {
                    Logger.MsgPastel("Installing mod file " + Path.GetFileName(path).Pastel(theme.FileNameColor));
                    if (!CheckCompability(_assembly)) { _assembly.Dispose(); threwError = true; success = false; return (success, threwError); }
                    string _path = Path.Combine(Files.ModsDirectory, Path.GetFileName(path));
                    if (!File.Exists(_path)) File.Move(path, _path);
                    else File.Replace(path, _path, Path.Combine(Files.BackupDirectory, $"{Path.GetFileName(path)}-{DateTimeOffset.Now.ToUnixTimeSeconds()}.dll"));
                    success = true;

                    Logger.Msg("Checking if mod version is valid");
                    var fileStream = File.Open(_path, FileMode.Open, FileAccess.ReadWrite);
                    _assembly = AssemblyDefinition.ReadAssembly(fileStream, new ReaderParameters() { AssemblyResolver = new CustomCecilResolver() });
                    var melonInfo = GetMelonInfo(_assembly);
                    if (melonInfo.Version < latestVersion)
                    {
                        Logger.Warning("Mod has incorrect version which can lead to repeated unnecessary updates, fixing");
                        var module = _assembly.MainModule;
#pragma warning disable CS0618 // Type or member is obsolete
                        var attr = _assembly.CustomAttributes.Where(x => x.AttributeType.Name == nameof(MelonInfoAttribute) || x.AttributeType.Name == nameof(MelonModInfoAttribute));
#pragma warning restore CS0618 // Type or member is obsolete
                        if (attr.Any())
                        {
                            Logger.Msg("Found attribute");
                            var a = attr.First();
                            var versionType = module.ImportReference(typeof(string));
                            a.ConstructorArguments[2] = new CustomAttributeArgument(versionType, latestVersion.ToString());
                            _assembly.Write();
                            Logger.Msg("Fixed incorrect version of mod");
                        }
                        else
                        {
                            Logger.Error("Could not find attribute, cannot fix incorrect version");
                        }
                    }
                    else
                    {
                        Logger.Msg("Correct mod version, not changing anything");
                    }
                    fileStream.Flush();
                    fileStream.Dispose();
                    _assembly.Dispose();
                    Logger.MsgPastel("Successfully installed mod file " + Path.GetFileName(path).Pastel(theme.FileNameColor));
                }
                catch (Exception ex)
                {
                    Logger.Error($"An unexpected error occurred while installing content{ex}");
                    threwError = true;
                    success = false;
                }
            }
            else if (_fileType == FileType.MelonPlugin)
            {
                try
                {
                    Logger.MsgPastel("Installing plugin file " + Path.GetFileName(path).Pastel(theme.FileNameColor));
                    if (!CheckCompability(_assembly)) { _assembly.Dispose(); threwError = true; success = false; return (success, threwError); }

                    string pluginPath = Path.Combine(Files.PluginsDirectory, fileName);
                    string _path = Path.Combine(Files.PluginsDirectory, Path.GetFileName(path));
                    if (!File.Exists(_path)) File.Move(path, _path);
                    else File.Replace(path, _path, Path.Combine(Files.BackupDirectory, $"{Path.GetFileName(path)}-{DateTimeOffset.Now.ToUnixTimeSeconds()}.dll"));

                    Logger.Msg("Checking if plugin version is valid");
                    var fileStream = File.Open(_path, FileMode.Open, FileAccess.ReadWrite);
                    _assembly = AssemblyDefinition.ReadAssembly(fileStream, new ReaderParameters() { AssemblyResolver = new CustomCecilResolver() });
                    var melonInfo = GetMelonInfo(_assembly);
                    if (melonInfo.Version < latestVersion)
                    {
                        Logger.Warning("Plugin has incorrect version which can lead to repeated unnecessary updates, fixing");
                        var module = _assembly.MainModule;
#pragma warning disable CS0618 // Type or member is obsolete
                        var attr = _assembly.CustomAttributes.Where(x => x.AttributeType.Name == nameof(MelonInfoAttribute) || x.AttributeType.Name == nameof(MelonPluginInfoAttribute));
#pragma warning restore CS0618 // Type or member is obsolete
                        if (attr.Any())
                        {
                            Logger.Msg("Found attribute");
                            var a = attr.First();
                            var semVersionType = module.ImportReference(typeof(string));
                            a.ConstructorArguments[2] = new CustomAttributeArgument(semVersionType, latestVersion.ToString());
                            _assembly.Write();
                            Logger.Msg("Fixed incorrect version of plugin");
                        }
                        else
                        {
                            Logger.Error("Could not find attribute, cannot fix incorrect version");
                        }
                    }
                    else
                    {
                        Logger.Msg("Correct plugin version, not changing anything");
                    }
                    _assembly.Dispose();
                    fileStream.Flush();
                    fileStream.Dispose();

                    //var melonAssembly = MelonAssembly.LoadMelonAssembly(pluginPath);
                    Logger.Warning("WARNING: The plugin will only work after game restart");
                    Logger.MsgPastel("Successfully installed plugin file " + Path.GetFileName(path).Pastel(theme.FileNameColor));
                    success = true;
                }
                catch (Exception ex)
                {
                    Logger.Error($"An unexpected error occurred while installing content{ex}");
                    threwError = true;
                    success = false;
                }
            }
            else
            {
                Logger.Msg($"Not extracting {Path.GetFileName(path)}, because it does not have the Melon Info Attribute");
            }
            if (MelonAutoUpdater.Debug)
            {
                sw.Stop();
                MelonAutoUpdater.ElapsedTime.Add($"InstallPackage-{Path.GetFileName(path)}", sw.ElapsedMilliseconds);
            }
            if (success && melonFileName == fileName) needUpdate = false;
            return (success, threwError);
        }

        /// <summary>
        /// Variable if melon being checked needs to be updated due to being incompatible
        /// </summary>
        private bool needUpdate = false;

        /// <summary>
        /// Variable if melon being checked needs to be updated due to being incompatible
        /// </summary>
        private string melonFileName = string.Empty;

        /// <summary>
        /// Check directory for mods and plugins that can be updated
        /// </summary>
        /// <param name="directory">Path to the directory</param>
        /// <param name="automatic">If <see langword="true"/>, the mods/plugins will be updated automatically, otherwise there will be only a message displayed about a new version</param>
        internal void CheckDirectory(string directory, bool automatic = true)
        {
            Stopwatch sw = null;
            if (MelonAutoUpdater.Debug)
            {
                sw = Stopwatch.StartNew();
            }

            List<string> files = Directory.GetFiles(directory, "*.dll").ToList();

            List<string> ignore = ignoreMelons;

            List<string> fileNameIgnore = new List<string>();

            (int success, int warn, int error, List<(string name, SemVersion oldVersion, SemVersion newVersion, bool threwError, int success, int failed)> updates) result = (0, 0, 0, new List<(string name, SemVersion oldVersion, SemVersion newVersion, bool threwError, int success, int failed)>());

            List<(string name, SemVersion oldVer, SemVersion newVer, Uri downloadLink)> manualUpdate = new List<(string name, SemVersion oldVer, SemVersion newVer, Uri downloadLink)>();

            files.ForEach(x =>
            {
                if (ignore != null && ignore.Count > 0)
                {
                    string fileName = Path.GetFileNameWithoutExtension(x);
                    if (ignore.Contains(fileName))
                    {
                        Logger.Msg($"{fileName} is in ignore list, removing from update list");
                        fileNameIgnore.Add(x);
                    }
                }
            });
            files.RemoveAll(x => fileNameIgnore.Contains(x));
            Logger.MsgPastel("------------------------------".Pastel(theme.LineColor));
            Stopwatch sw2 = null;
            string previousFileName = string.Empty;
            bool _bruteCheck = false;
            foreach (string path in files)
            {
                string fileName = Path.GetFileName(path);
                needUpdate = false;
                melonFileName = fileName;
                if (MelonAutoUpdater.Debug)
                {
                    if (sw2 != null)
                    {
                        sw2.Stop();
                        MelonAutoUpdater.ElapsedTime.Add($"CheckFile-{previousFileName}{(_bruteCheck ? " (with Brute Check)" : "")}", sw2.ElapsedMilliseconds);
                    }
                    _bruteCheck = false;
                    sw2 = Stopwatch.StartNew();
                    previousFileName = fileName;
                }
                Logger.MsgPastel($"File: {fileName.Pastel(theme.FileNameColor)}");
                AssemblyDefinition mainAssembly = AssemblyDefinition.ReadAssembly(path, new ReaderParameters() { AssemblyResolver = new CustomCecilResolver() });
                var config = GetMelonConfig(mainAssembly);
                if (config != null)
                {
                    Logger.Msg("Found MAU config associated with Melon");
                }
                bool _ignore = config != null && config.Disable;
                var melonAssemblyInfo = GetMelonInfo(mainAssembly);
                if (_ignore)
                {
                    Logger.MsgPastel($"Ignoring {fileName.Pastel(theme.FileNameColor)}, because it is configured to be ignored");
                    Logger.MsgPastel("------------------------------".Pastel(theme.LineColor));
                    continue;
                }
                if (melonAssemblyInfo != null)
                {
                    string assemblyName = (string)melonAssemblyInfo.Name.Clone();
                    Type assemblyType = (Type)melonAssemblyInfo.SystemType;
                    if (melonAssemblyInfo != null)
                    {
                        if (!CheckCompability(mainAssembly)) needUpdate = true;
                        SemVersion currentVersion = SemVersion.Parse(melonAssemblyInfo.Version);
                        var data = GetModData(melonAssemblyInfo.DownloadLink, currentVersion, config);
                        if (data == null || string.IsNullOrEmpty(melonAssemblyInfo.DownloadLink))
                        {
                            if (bruteCheck)
                            {
                                Logger.MsgPastel("Running " + "brute check..".Pastel(Color.Red));
                                _bruteCheck = true;
                                data = GetModDataFromInfo(melonAssemblyInfo.Name, melonAssemblyInfo.Author, currentVersion, config);
                            }
                        }
                        if (data != null)
                        {
                            if (currentVersion != null && data.LatestVersion != null)
                            {
                                if (data.LatestVersion > currentVersion)
                                {
                                    if (automatic)
                                    {
                                        Logger.MsgPastel($"A new version " + $"v{data.LatestVersion}".Pastel(theme.NewVersionColor) + $" is available, meanwhile the current version is " + $"v{currentVersion}".Pastel(theme.OldVersionColor) + ", updating");
                                        Logger.Msg("Downloading file(s)");
                                        int success = 0;
                                        int failed = 0;
                                        bool threwError = false;
                                        foreach (var retFile in data.DownloadFiles)
                                        {
                                            string pathToSave = "";
                                            string name = !string.IsNullOrEmpty(retFile.FileName) ? retFile.FileName : $"{melonAssemblyInfo.Name}-{MelonUtils.RandomString(7)}";
                                            Stopwatch sw3 = null;
                                            if (MelonAutoUpdater.Debug)
                                            {
                                                sw3 = Stopwatch.StartNew();
                                            }
                                            FileStream downloadedFile = null;
                                            var httpClient = new HttpClient();
                                            var response = httpClient.GetAsync(retFile.URL, HttpCompletionOption.ResponseHeadersRead);
                                            response.Wait();
                                            try
                                            {
                                                response.Result.EnsureSuccessStatusCode();
                                                string resContentType = response.Result.Content.Headers.ContentType.MediaType;
                                                ContentType contentType;
                                                if (!string.IsNullOrEmpty(retFile.ContentType))
                                                {
                                                    bool parseSuccess = ContentType.TryParse(ParseType.MimeType, retFile.ContentType, out ContentType _contentType);
                                                    if (parseSuccess)
                                                    {
                                                        contentType = _contentType;
                                                        if (!string.IsNullOrEmpty(_contentType.Extension))
                                                        {
                                                            pathToSave = Path.Combine(Files.TemporaryMelonsDirectory, $"{name.Replace(" ", "")}.{_contentType.Extension}");
                                                        }
                                                        else
                                                        {
                                                            Logger.Warning("Content-Type is not associated with any file type, continuing without downloading & installing file");
                                                            httpClient.Dispose();
                                                            continue;
                                                        }
                                                    }
                                                    else
                                                    {
                                                        Logger.Warning("Could not determine Content-Type, continuing without downloading & installing file");
                                                        httpClient.Dispose();
                                                        continue;
                                                    }
                                                }
                                                else if (resContentType != null)
                                                {
                                                    bool parseSuccess = ContentType.TryParse(ParseType.MimeType, resContentType, out ContentType _contentType);
                                                    if (parseSuccess)
                                                    {
                                                        contentType = _contentType;
                                                        if (!string.IsNullOrEmpty(_contentType.Extension))
                                                        {
                                                            pathToSave = Path.Combine(Files.TemporaryMelonsDirectory, $"{name.Replace(" ", "")}.{_contentType.Extension}");
                                                        }
                                                        else
                                                        {
                                                            Logger.Warning("Content-Type is not associated with any file type, continuing without downloading file");
                                                            httpClient.Dispose();
                                                            continue;
                                                        }
                                                    }
                                                    else
                                                    {
                                                        Logger.Warning("Could not determine Content-Type, continuing without downloading file");
                                                        httpClient.Dispose();
                                                        continue;
                                                    }
                                                }
                                                else
                                                {
                                                    Logger.Warning("No Content Type was provided, continuing without downloading file");
                                                    httpClient.Dispose();
                                                    continue;
                                                }
                                                if (config != null && config.AllowedFileDownloads != null && contentType != null && !string.IsNullOrEmpty(retFile.FileName))
                                                {
                                                    string _fileName = Path.GetFileName(pathToSave);
                                                    if (!string.IsNullOrEmpty(_fileName) && config.AllowedFileDownloads != null && config.AllowedFileDownloads.Any())
                                                    {
                                                        if (!config.AllowedFileDownloads.Contains(_fileName))
                                                        {
                                                            Logger.Msg($"{_fileName} was configured to not be downloaded & installed, aborting download");
                                                            continue;
                                                        }
                                                    }
                                                }
                                                var ms = response.Result.Content.ReadAsStreamAsync();
                                                ms.Wait();
                                                var fs = File.Create(pathToSave);
                                                ms.Result.CopyTo(fs);
                                                fs.Flush();
                                                downloadedFile = fs;
                                                ms.Dispose();
                                                Logger.Msg($"Download successful");
                                            }
                                            catch (Exception ex)
                                            {
                                                Logger.Error($"Failed to download file through link, exception:\n{ex}");
                                                downloadedFile.Dispose();
                                                downloadedFile = null;
                                            }

                                            if (downloadedFile != null)
                                            {
                                                downloadedFile.Dispose();
                                                if (Path.GetExtension(pathToSave) == ".zip")
                                                {
                                                    Logger.Msg("File is a ZIP, extracting files...");
                                                    string extractPath = Path.Combine(Files.TemporaryMelonsDirectory, name.Replace(" ", "-"));
                                                    try
                                                    {
                                                        UnzipFromStream(File.OpenRead(pathToSave), extractPath);
                                                        Logger.Msg("Successfully extracted files! Installing content..");
                                                        downloadedFile.Dispose();
                                                    }
                                                    catch (Exception ex)
                                                    {
                                                        threwError = true;
                                                        Logger.Error($"An exception occurred while extracting files from a ZIP file{ex}");
                                                        File.Delete(pathToSave);
                                                        DirectoryInfo tempDir = new DirectoryInfo(Files.TemporaryMelonsDirectory);
                                                        foreach (FileInfo file in tempDir.GetFiles()) file.Delete();
                                                        foreach (DirectoryInfo subDirectory in tempDir.GetDirectories()) subDirectory.Delete(true);
                                                    }
                                                    var allContent = new List<string>();
                                                    var extracedDirectories = Directory.GetDirectories(extractPath).ToList();
                                                    var extractedFiles = Directory.GetFiles(extractPath).ToList();
                                                    extractedFiles.ForEach(x => allContent.Add(x));
                                                    extracedDirectories.ForEach((x) => allContent.Add(x));
                                                    Logger.Msg($"Found {extractedFiles.Count} files and {extracedDirectories.Count} directories");
                                                    foreach (string extPath in allContent)
                                                    {
                                                        if (Directory.Exists(extPath))
                                                        {
                                                            string dirName = GetDirName(extPath);
                                                            List<string> SubDirCheck = new List<string>
                                                                {
                                                                    "Mods",
                                                                    "Plugins",
                                                                    "MelonLoader",
                                                                    "UserData",
                                                                    "UserLibs"
                                                                };
                                                            int checkedDirs = 0;
                                                            foreach (var subdir in SubDirCheck)
                                                            {
                                                                if (Directory.GetDirectories(extPath).Contains(Path.Combine(extPath, subdir)))
                                                                {
                                                                    var res1 = MoveAllFiles(Path.Combine(extPath, subdir), Files.GetDirectoryInBaseDir(subdir).FullName, string.Empty, data.LatestVersion, config);
                                                                    checkedDirs++;
                                                                    success += res1.success;
                                                                    failed += res1.failed;
                                                                    if (res1.threwError) threwError = true;
                                                                }
                                                            }
                                                            if (checkedDirs <= Directory.GetDirectories(extPath).Length)
                                                            {
                                                                Logger.Msg($"Found {dirName}, installing all content from it...");
                                                                var res1 = MoveAllFiles(extPath, Files.GetDirectoryInBaseDir(dirName).FullName, string.Empty, data.LatestVersion, config);
                                                                success += res1.success;
                                                                failed += res1.failed;
                                                                if (res1.threwError) threwError = true;
                                                            }
                                                        }
                                                        else if (Path.GetExtension(extPath) == ".dll")
                                                        {
                                                            var res = InstallPackage(extPath, data.LatestVersion);
                                                            if (res.threwError) threwError = true;
                                                            if (res.success) success += 1;
                                                            else failed += 1;
                                                        }
                                                        else
                                                        {
                                                            Logger.Warning($"Not moving {Path.GetFileName(extPath)}, as it seems useless, sorry in advance");
                                                        }
                                                    }
                                                    Directory.Delete(extractPath, true);
                                                    File.Delete(pathToSave);
                                                }
                                                else if (Path.GetExtension(pathToSave) == ".dll")
                                                {
                                                    Logger.Msg("Downloaded file is a DLL file, installing content...");
                                                    var res = InstallPackage(pathToSave, data.LatestVersion);
                                                    if (res.threwError) threwError = true;
                                                    if (res.success) success += 1;
                                                    else failed += 1;
                                                }
                                                else
                                                {
                                                    Logger.Warning($"Not moving {Path.GetFileName(pathToSave)}, as it seems useless, sorry in advance");
                                                }
                                            }
                                            else
                                            {
                                                Logger.Error("Downloaded file is empty, unable to update melon");
                                            }
                                            if (MelonAutoUpdater.Debug)
                                            {
                                                sw3.Stop();
                                                MelonAutoUpdater.ElapsedTime.Add($"DownloadFile-{name}", sw.ElapsedMilliseconds);
                                            }
                                        }
                                        Logger.MsgPastel(
                                            threwError
                                                ? $"Failed to update {assemblyName}".Pastel(Color.Red)
                                                : success + failed > 0
                                                ? $"Updated {assemblyName.Pastel(theme.FileNameColor)} from " + $"v{currentVersion}".Pastel(theme.OldVersionColor) + " --> " + $"v{data.LatestVersion}".Pastel(theme.NewVersionColor) + ", " + $"({success}/{success + failed})".Pastel(theme.DownloadCountColor) + " melons installed successfully"
                                                : "No melons were installed".Pastel(Color.Yellow)
                                        );

                                        if (threwError) result.error++;
                                        else if (success + failed > 0) result.success++;
                                        else result.warn++;

                                        result.updates.Add((assemblyName, currentVersion, data.LatestVersion, threwError, success, failed));
                                    }
                                    else
                                    {
                                        Logger.MsgPastel($"A new version " + $"v{data.LatestVersion}".Pastel(theme.NewVersionColor) + $" is available, meanwhile the current version is " + $"v{currentVersion}".Pastel(theme.OldVersionColor) + ". We recommend that you update, go to this site to download: " + data.DownloadLink.ToString().Pastel(theme.LinkColor).Underline().Blink());
                                        manualUpdate.Add((assemblyName, currentVersion, data.LatestVersion, data.DownloadLink));
                                    }
                                }
                                else
                                {
                                    if (data.LatestVersion == currentVersion)
                                    {
                                        Logger.MsgPastel("Version is up-to-date!".Pastel(theme.UpToDateVersionColor));
                                    }
                                    else if (data.LatestVersion < currentVersion)
                                    {
                                        Logger.MsgPastel("Current version is newer than in the API".Pastel(theme.UpToDateVersionColor));
                                    }
                                }
                            }
                        }
                        if (needUpdate && GetEntryValue<bool>(MelonAutoUpdater.Entry_removeIncompatible))
                        {
                            Logger.Msg($"Removing {fileName.Pastel(theme.FileNameColor)}, due to it being incompatible and not being updated");
                            if (GetFileType(melonAssemblyInfo) == FileType.MelonMod)
                            {
                                File.Delete(path);
                            }
                            else
                            {
                                Logger.Warning("Cannot remove due to it being a plugin, meaning its already loaded by MelonLoader");
                            }
                        }
                    }
                }
                else
                {
                    Logger.Warning($"{fileName} does not seem to be a Melon");
                }
                mainAssembly.Dispose();
                Logger.MsgPastel("------------------------------".Pastel(theme.LineColor));
            }
            Logger.Msg($"Results " + (automatic ? $"({result.updates.Count} updates)" : $"({manualUpdate.Count} need to be updated)") + ":");
            if (result.updates.Count > 0 || manualUpdate.Count > 0)
            {
                foreach (var (name, oldVersion, newVersion, threwError, success, failed) in result.updates)
                {
                    if (!threwError)
                    {
                        if (success + failed > 0)
                        {
                            Logger.MsgPastel($"{"[V]".Pastel(Color.LawnGreen)} {name.Pastel(theme.FileNameColor)} {$"v{oldVersion}".Pastel(theme.OldVersionColor)} ---> {$"v{newVersion}".Pastel(theme.NewVersionColor)} ({$"{success}/{success + failed}".Pastel(theme.DownloadCountColor)} melons installed successfully)");
                        }
                        else
                        {
                            Logger.MsgPastel($"{"[?]".Pastel(Color.Yellow)} {name.Pastel(theme.FileNameColor)} {$"v{oldVersion}".Pastel(theme.OldVersionColor)} ---> {$"v{newVersion}".Pastel(theme.NewVersionColor)} ({$"{success}/{success + failed}".Pastel(theme.DownloadCountColor)} melons installed successfully)");
                        }
                    }
                    else
                    {
                        Logger.MsgPastel($"{"[X]".Pastel(Color.Red)} {name.Pastel(theme.FileNameColor)} {$"v{oldVersion}".Pastel(theme.OldVersionColor)} ---> {$"v{newVersion}".Pastel(theme.NewVersionColor)} ({$"{success}/{success + failed}".Pastel(theme.DownloadCountColor)} melons installed successfully)");
                    }
                }
                foreach (var (name, oldVer, newVer, downloadLink) in manualUpdate)
                {
                    Logger.MsgPastel($"{"[!]".Pastel(Color.Red)} New version available for {name.Pastel(theme.FileNameColor)} {$"v{oldVer}".Pastel(theme.OldVersionColor)} ---> {$"v{newVer}".Pastel(theme.NewVersionColor)}. Go to {downloadLink.ToString().Pastel(Color.Aqua).Underline().Blink()} to download the new version");
                }
            }
            else
            {
                Logger.MsgPastel("All melons are up to date!".Pastel(theme.UpToDateVersionColor));
            }
            Logger.MsgPastel("------------------------------".Pastel(theme.LineColor));
            if (MelonAutoUpdater.Debug)
            {
                sw.Stop();
                MelonAutoUpdater.ElapsedTime.Add($"CheckDirectory-{GetDirName(directory)}", sw.ElapsedMilliseconds);
            }
        }
    }
}