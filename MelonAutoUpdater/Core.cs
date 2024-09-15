using MelonLoader;
using Mono.Cecil;
using System.Collections.Generic;
using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using MelonLoader.ICSharpCode.SharpZipLib.Core;
using MelonLoader.ICSharpCode.SharpZipLib.Zip;
using Semver;
using System.Threading.Tasks;
using System.Drawing;
using MelonLoader.Preferences;
using MelonAutoUpdater.Search;
using MelonAutoUpdater.Helper;
using System.Reflection;
using MelonAutoUpdater.Attributes;

[assembly: MelonInfo(typeof(MelonAutoUpdater.Core), "MelonAutoUpdater", "0.3.0", "HAHOOS", "https://github.com/HAHOOS/MelonAutoUpdater")]
[assembly: MelonPriority(-100000000)]
#pragma warning disable CS0618 // Type or member is obsolete
[assembly: MelonColor(ConsoleColor.Green)]
#pragma warning restore CS0618 // Type or member is obsolete

[assembly: AssemblyProduct("MelonAutoUpdater")]
[assembly: AssemblyVersion("0.3.0.0")]
[assembly: AssemblyFileVersion("0.3.0")]
[assembly: AssemblyTitle("MelonAutoUpdater")]
[assembly: AssemblyCompany("HAHOOS")]
[assembly: AssemblyDescription("An automatic updater for all your MelonLoader mods!")]
[assembly: VerifyLoaderVersion("0.5.3", true)]

namespace MelonAutoUpdater
{
    public class Core : MelonPlugin
    {
        /// <summary>
        /// Path of the Temporary Files folder where downloaded files and uncompressed zip files get put temporarily
        /// </summary>
        internal static string tempFilesPath = "";

        /// <summary>
        /// Path of MelonAutoUpdate folder containing all the other folders
        /// </summary>
        internal static string mainFolderPath = "";

        /// <summary>
        /// Path of Backup folder where old versions of mods are saved
        /// </summary>
        internal static string backupFolderPath = "";

        /// <summary>
        /// Path of Config folder for all extension config's
        /// </summary>
        internal static string extConfigFolderPath = "";

        /// <summary>
        /// Version of MAU
        /// </summary>
        public static string Version { get; private set; }

        /// <summary>
        /// User Agent Header for all HTTP requests
        /// </summary>
        public string UserAgent { get; private set; }

        /// <summary>
        /// Customizable colors, why does it exist? I don't know
        /// </summary>
        internal static Theme theme = Theme.Instance;

        /// <summary>
        /// Instance of <see cref="MelonLogger"/>
        /// </summary>
        internal static MelonLogger.Instance logger;

        /// <summary>
        /// List of MAU Search Extensions
        /// </summary>
        internal IEnumerable<MAUSearch> extensions;

        #region Melon Preferences

        /// <summary>
        /// Main Category in Preferences
        /// </summary>
        internal MelonPreferences_Category MainCategory { get; private set; }

        /// <summary>
        /// A Melon Preferences entry of a list of mods/plugins that will not be updated
        /// </summary>
        internal MelonPreferences_Entry Entry_ignore { get; private set; }

        /// <summary>
        /// A Melon Preferences entry of a boolean value indicating whether or not should the plugin work
        /// </summary>
        internal MelonPreferences_Entry Entry_enabled { get; private set; }

        /// <summary>
        /// A Melon Preferences entry of a boolean value indicating whether or not it should forcefully check the API for the mod/plugins if no download link was provided with it
        /// </summary>
        internal MelonPreferences_Entry Entry_bruteCheck { get; private set; }

        /// <summary>
        /// Themes Category in Preferences
        /// </summary>
        internal MelonPreferences_ReflectiveCategory ThemesCategory { get; private set; }

        /// <summary>
        /// Extensions Category in Preferences
        /// </summary>
        internal MelonPreferences_Category ExtensionsCategory { get; private set; }

        /// <summary>
        /// Dictionary of included extensions and their Enable entry
        /// </summary>
        internal static Dictionary<MAUSearch, MelonPreferences_Entry> IncludedExtEntries { get; private set; } = new Dictionary<MAUSearch, MelonPreferences_Entry>();

        /// <summary>
        /// Setup Preferences
        /// </summary>
        private bool SetupPreferences()
        {
            // Main Category
            MainCategory = MelonPreferences.CreateCategory("MelonAutoUpdater", "Melon Auto Updater");
            MainCategory.SetFilePath(Path.Combine(mainFolderPath, "config.cfg"));
            Entry_ignore = MainCategory.CreateEntry<List<string>>("IgnoreList", new List<string>(), "Ignore List",
                description: "List of all names of Mods & Plugins that will be ignored when checking for updates");
            Entry_enabled = MainCategory.CreateEntry<bool>("Enabled", true, "Enabled",
                description: "If true, Mods & Plugins will update on every start");
            Entry_bruteCheck = MainCategory.CreateEntry<bool>("BruteCheck", false, "Brute Check",
                description: "If true, when there's no download link provided with mod/plugin, it will check every supported platform providing the Name & Author\nWARNING: You may get rate-limited with large amounts of mods/plugins, use with caution");

            MainCategory.SaveToFile(false);

            // Themes Category

            ThemesCategory = MelonPreferences.CreateCategory<Theme>("Theme", "Theme");
            ThemesCategory.SetFilePath(Path.Combine(mainFolderPath, "theme.cfg"));
            ThemesCategory.SaveToFile(false);

            // Extensions Category

            ExtensionsCategory = MelonPreferences.CreateCategory("Extensions", "Extensions");
            ExtensionsCategory.SetFilePath(Path.Combine(mainFolderPath, "extensions.cfg"));

            foreach (Type type in
                    Assembly.GetExecutingAssembly().GetTypes()
                    .Where(myType => myType.IsClass && !myType.IsAbstract && myType.IsSubclassOf(typeof(MAUSearch))))
            {
                var obj = (MAUSearch)Activator.CreateInstance(type);
                MelonPreferences_Entry entry = ExtensionsCategory.CreateEntry<bool>($"{obj.Name}_Enabled", true, $"{obj.Name} Enabled",
                    description: $"If true, {obj.Name} will be used in searches");
                IncludedExtEntries.Add(obj, entry);
            }

            ExtensionsCategory.SaveToFile(false);

            LoggerInstance.Msg("Successfully set up Melon Preferences!");
            return true;
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
                    logger.Error($"Preference '{entry.DisplayName}' is of incorrect type");
                    return default;
                }
            }
            return default;
        }

        #endregion Melon Preferences

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
        /// Copy a <see cref="Stream"/> to a new one<br/>
        /// Made to work with net35
        /// </summary>
        /// <param name="input">The <see cref="Stream"/> u want to copy from</param>
        /// <param name="output">The <see cref="Stream"/> u want to copy to</param>
        [System.Runtime.CompilerServices.MethodImpl(
    System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        internal static Task<bool> CopyTo(Stream input, Stream output)
        {
            byte[] buffer = new byte[16 * 1024];
            int bytesRead;

            while ((bytesRead = input.Read(buffer, 0, buffer.Length)) > 0)
            {
                output.Write(buffer, 0, bytesRead);
            }
            return Task.Factory.StartNew(() => true);
        }

        /// <summary>
        /// Copy a <see cref="Stream"/> to a new one<br/>
        /// Made to work with net35
        /// </summary>
        /// <param name="input">The <see cref="Stream"/> u want to copy from</param>
        /// <param name="output">The <see cref="Stream"/> u want to copy to</param>
        internal static bool CopyToNotTask(Stream input, Stream output)
        {
            byte[] buffer = new byte[16 * 1024];
            int bytesRead;

            while ((bytesRead = input.Read(buffer, 0, buffer.Length)) > 0)
            {
                output.Write(buffer, 0, bytesRead);
            }
            return true;
        }

        /// <summary>
        /// Unzip a file from <see cref="Stream"/><br/>
        /// Made to work with net35
        /// </summary>
        /// <param name="zipStream"><see cref="Stream"/> of the ZIP File</param>
        /// <param name="outFolder"><see cref="Path"/> to folder which will have the content of the zip/param>
        /// <returns>A <see cref="Task"/> that returns true if completed successfully</returns>
        internal static Task<bool> UnzipFromStream(Stream zipStream, string outFolder)
        {
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
            return Task.Factory.StartNew(() => true);
        }

        // If you are wondering, this is from StackOverflow, although a bit edited, I'm just a bit lazy
        /// <summary>
        /// Checks for internet connection
        /// </summary>
        /// <param name="timeoutMs">Time in milliseconds after the request will be aborted if no response (Default: 5000)</param>
        /// <param name="url">URL of the website used to check for connection (Default: <c>http://www.gstatic.com/generate_204</c>)</param>
        /// <returns>If <see cref="true"/>, there's internet connection, otherwise not</returns>
        internal static Task<bool> CheckForInternetConnection(int timeoutMs = 5000, string url = "http://www.gstatic.com/generate_204")
        {
            try
            {
                var request = new HttpClient
                {
                    Timeout = TimeSpan.FromMilliseconds(timeoutMs)
                };
                var response = request.GetAsync(url);
                response.Wait();
                return Task.Factory.StartNew<bool>(() => response.Result.IsSuccessStatusCode);
            }
            catch
            {
                return Task.Factory.StartNew<bool>(() => false);
            }
        }

        /// <summary>
        /// Get data about the mod from a downloadLink<br/>
        /// Currently Supported: Thunderstore, Github
        /// </summary>
        /// <param name="downloadLink">Download Link, possibly included in the <see cref="MelonInfoAttribute"/></param>
        /// <returns>If found, returns a <see cref="ModData"/> object which includes the latest version of the mod online and the download link(s)</returns>
        internal Task<ModData> GetModData(string downloadLink, SemVersion currentVersion)
        {
            if (string.IsNullOrEmpty(downloadLink))
            {
                LoggerInstance.Msg("No download link was provided with the mod");
                TaskCompletionSource<ModData> _res = new TaskCompletionSource<ModData>();
                _res.SetResult(null);
                return _res.Task;
            }
            foreach (var ext in extensions)
            {
                LoggerInstance.Msg($"Checking {ext.Name.Pastel(ext.NameColor)}");
                ext.Setup();
                var result = ext.Search(downloadLink, currentVersion);
                result.Wait();
                if (result == null || result.Result == null)
                {
                    LoggerInstance.Msg($"Nothing found with {ext.Name.Pastel(ext.NameColor)}");
                }
                else
                {
                    LoggerInstance.Msg($"Found data with {ext.Name.Pastel(ext.NameColor)}");
                    return result;
                }
            }
            TaskCompletionSource<ModData> res = new TaskCompletionSource<ModData>();
            res.SetResult(null);
            return res.Task;
        }

        /// <summary>
        /// Get data about the mod from a name and author<br/>
        /// Github is not supported in brute checking due to extremely strict rate limits
        /// Currently Supported: Thunderstore
        /// </summary>
        /// <returns>If found, returns a <see cref="ModData"/> object which includes the latest version of the mod online and the download link(s)</returns>
        internal Task<ModData> GetModDataFromInfo(string name, string author, SemVersion currentVersion)
        {
            if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(author))
            {
                LoggerInstance.Msg("Name/Author was not provided with the mod");
                TaskCompletionSource<ModData> _res = new TaskCompletionSource<ModData>();
                _res.SetResult(null);
                return _res.Task;
            }
            foreach (var ext in extensions)
            {
                ModData result = null;
                if (ext.BruteCheckEnabled)
                {
                    LoggerInstance.Msg($"Brute checking with {ext.Name.Pastel(ext.NameColor)}");
                    var task = ext.BruteCheck(name, author, currentVersion);
                    task.Wait();
                    result = task.Result;
                    if (result == null || result.LatestVersion == null)
                    {
                        LoggerInstance.Msg($"Nothing found with {ext.Name.Pastel(ext.NameColor)}");
                    }
                    else
                    {
                        LoggerInstance.Msg($"Found data with {ext.Name.Pastel(ext.NameColor)}");
                        return Task.Factory.StartNew(() => result);
                    }
                }
                else
                {
                    LoggerInstance.Msg($"Brute checking disabled in {ext.Name.Pastel(ext.NameColor)}");
                }
            }

            TaskCompletionSource<ModData> res = new TaskCompletionSource<ModData>();
            res.SetResult(null);
            return res.Task;
        }

        /// <summary>
        /// Check if an assembly is a <see cref="MelonMod"/>, a <see cref="MelonPlugin"/> or something else
        /// </summary>
        /// <param name="assembly"><see cref="Assembly"/> of the file</param>
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
            if (Directory.Exists(path))
            {
                var info = new DirectoryInfo(path);
                if (info != null)
                {
                    return info.Name;
                }
            }
            return path;
        }

        /// <summary>
        /// Move all files from one directory to another
        /// </summary>
        /// <param name="path">A <see cref="Path"/> to directory to copy from</param>
        /// <param name="directory">A <see cref="Path"/> to directory to copy to</param>
        /// <param name="mainDirectoryName">Only used in prefix, just set <see cref="string.Empty"/></param>
        /// <param name="latestVersion">The latest version of the mod the files are from</param>
        /// <returns>Info about mod/plugin install (times when it succeeded, times when it failed, and if it threw an error)</returns>
        internal (int success, int failed, bool threwError) MoveAllFiles(string path, string directory, string mainDirectoryName, SemVersion latestVersion)
        {
            int success = 0;
            int failed = 0;
            bool threwError = false;
            string prefix = (string.IsNullOrEmpty(mainDirectoryName) != true ? $"{mainDirectoryName}/{GetDirName(directory)}" : GetDirName(directory)).Pastel(Color.Cyan);
            foreach (string file in Directory.GetFiles(path))
            {
                LoggerInstance.Msg($"[{prefix}] {Path.GetFileName(file)} found, copying file to folder");
                try
                {
                    string _path = Path.Combine(directory, Path.GetFileName(file));
                    if (Path.GetExtension(file) == ".dll")
                    {
                        var res = InstallPackage(file, latestVersion);
                        if (res.threwError) threwError = true;
                        if (res.success) success++;
                        else failed++;
                    }
                    else
                    {
                        if (!File.Exists(_path)) File.Move(file, _path);
                        else File.Replace(file, _path, Path.Combine(backupFolderPath, $"{Path.GetFileName(path)}-{DateTimeOffset.Now.ToUnixTimeSeconds()}.{Path.GetExtension(file)}"));
                        LoggerInstance.Msg($"[{prefix}] Successfully copied {Path.GetFileName(file)}");
                    }
                }
                catch (Exception ex)
                {
                    LoggerInstance.Error($"[{prefix}] Failed to copy {Path.GetFileName(file)}, exception thrown:{ex}");
                }
            }
            foreach (string dir in Directory.GetDirectories(path))
            {
                LoggerInstance.Msg($"[{prefix}] Found folder {GetDirName(dir)}, going through files");
                try
                {
                    string _path = Path.Combine(directory, GetDirName(dir));
                    if (!Directory.Exists(_path)) Directory.CreateDirectory(_path);
                    var res = MoveAllFiles(dir, _path, prefix, latestVersion);
                    if (res.threwError) threwError = true;
                    success += res.success;
                    failed += res.failed;
                }
                catch (Exception ex)
                {
                    LoggerInstance.Error($"[{prefix}] Failed to copy folder {GetDirName(dir)}, exception thrown:{ex}");
                }
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
            if (customAttribute.ConstructorArguments.Count <= 0) return default;
            return (T)customAttribute.ConstructorArguments[index].Value;
        }

        /// <summary>
        /// Retrieve information from the <see cref="MelonInfoAttribute"/> in a file using Mono.Cecil
        /// </summary>
        /// <param name="assembly"><see cref="Assembly"/> of the file</param>
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
                        assembly.Dispose();
                        return new VerifyLoaderVersionAttribute(major, minor, patch, isMinimum);
                    }
                    catch (Exception)
                    {
                        string version = Get<string>(attr, 0);
                        assembly.Dispose();
                        return new VerifyLoaderVersionAttribute(version);
                    }
                }
            }
            assembly.Dispose();
            return null;
        }

        /// <summary>
        /// Retrieve information from the <see cref="MAUIgnoreAttribute"/> in a file using Mono.Cecil
        /// </summary>
        /// <param name="assembly"><see cref="Assembly"/> of the file</param>
        /// <returns>If present, returns a <see cref="MAUIgnoreAttribute"/></returns>
        internal static MAUIgnoreAttribute GetMAUIgnoreAttribute(AssemblyDefinition assembly)
        {
            foreach (var attr in assembly.CustomAttributes)
            {
                if (attr.AttributeType.Name == nameof(MAUIgnoreAttribute))
                {
                    bool isTrue = Get<bool>(attr, 0);
                    assembly.Dispose();
                    return new MAUIgnoreAttribute(isTrue);
                }
            }
            assembly.Dispose();
            return null;
        }

        /// <summary>
        /// Retrieve information from the <see cref="MAUIgnoreAttribute"/> in a file using Mono.Cecil
        /// </summary>
        /// <param name="assembly"><see cref="Assembly"/> of the file</param>
        /// <returns>If present, returns a <see cref="MAUIgnoreAttribute"/></returns>
        internal static MAUDownloadFilesAllowedAttribute GetMAUDownloadFilesAllowedAttribute(AssemblyDefinition assembly)
        {
            foreach (var attr in assembly.CustomAttributes)
            {
                if (attr.AttributeType.Name == nameof(MAUDownloadFilesAllowedAttribute))
                {
                    List<string> list = Get<List<string>>(attr, 0);
                    assembly.Dispose();
                    return new MAUDownloadFilesAllowedAttribute(list);
                }
            }
            assembly.Dispose();
            return null;
        }

        /// <summary>
        /// Checks if the <see cref="Assembly"/> is compatible with the current ML Instance
        /// </summary>
        /// <param name="assembly"><see cref="AssemblyDefinition"/> to check</param>
        /// <returns><see cref="true"/>, if compatible, otherwise <see cref="false"/></returns>
        internal bool CheckCompability(AssemblyDefinition assembly)
        {
            var modInfo = GetMelonInfo(assembly);
            var loaderVer = GetLoaderVersionRequired(assembly);
            if (loaderVer != null)
            {
                if (!IsCompatible(loaderVer, BuildInfo.Version))
                {
                    string installString = loaderVer.IsMinimum ? $"{loaderVer.SemVer} or later" : $"{loaderVer.SemVer} specifically";
                    LoggerInstance.Warning($"{modInfo.Name} {modInfo.Version} is not compatible with the current version of MelonLoader ({BuildInfo.Version}), its only compatible with {installString}");
                    LoggerInstance.Warning("Still checking for updates");
                }
            }
            bool net6 = Environment.Version.Major >= 6;
            if (!net6)
            {
                bool isFramework = assembly.MainModule.AssemblyReferences.Where(x => x.Name == "mscorlib") != null;
                if (!isFramework)
                {
                    LoggerInstance.Error($"{modInfo.Name} {modInfo.Version} is not compatible with .NET Framework");
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Installs mod/plugin from path
        /// </summary>
        /// <param name="path"><see cref="Path"/> of mod/plugin</param>
        /// <param name="latestVersion">Latest version of mod/plugin, used to modify <see cref="MelonInfoAttribute"/> in case the version is not correct</param>
        /// <returns>A <see cref="Tuple"/>, success and threwError, self explanatory</returns>
        internal (bool success, bool threwError) InstallPackage(string path, SemVersion latestVersion)
        {
            bool success = false;
            bool threwError = false;
            string fileName = Path.GetFileName(path);
            AssemblyDefinition _assembly = AssemblyDefinition.ReadAssembly(path);
            FileType _fileType = GetFileType(_assembly);
            if (_fileType == FileType.MelonMod)
            {
                try
                {
                    LoggerInstance.Msg("Installing mod file " + Path.GetFileName(path).Pastel(theme.FileNameColor));
                    if (!CheckCompability(_assembly)) { _assembly.Dispose(); threwError = true; success = false; return (success, threwError); }
#pragma warning disable CS0618 // Type or member is obsolete
                    string _path = Path.Combine(Path.Combine(MelonUtils.BaseDirectory, "Mods"), Path.GetFileName(path));
#pragma warning restore CS0618 // Type or member is obsolete
                    if (!File.Exists(_path)) File.Move(path, _path);
                    else File.Replace(path, _path, Path.Combine(backupFolderPath, $"{Path.GetFileName(path)}-{DateTimeOffset.Now.ToUnixTimeSeconds()}.dll"));
                    success = true;

                    LoggerInstance.Msg("Checking if mod version is valid");
                    var fileStream = File.Open(_path, FileMode.Open, FileAccess.ReadWrite);
                    _assembly = AssemblyDefinition.ReadAssembly(fileStream);
                    var melonInfo = GetMelonInfo(_assembly);
                    if (melonInfo.Version < latestVersion)
                    {
                        LoggerInstance.Warning("Mod has incorrect version which can lead to repeated unnecessary updates, fixing");
                        var module = _assembly.MainModule;
#pragma warning disable CS0618 // Type or member is obsolete
                        var attr = _assembly.CustomAttributes.Where(x => x.AttributeType.Name == nameof(MelonInfoAttribute) || x.AttributeType.Name == nameof(MelonModInfoAttribute));
#pragma warning restore CS0618 // Type or member is obsolete
                        if (attr.Any())
                        {
                            LoggerInstance.Msg("Found attribute");
                            var a = attr.First();
                            var versionType = module.ImportReference(typeof(string));
                            a.ConstructorArguments[2] = new CustomAttributeArgument(versionType, latestVersion.ToString());
                            _assembly.Write();
                            LoggerInstance.Msg("Fixed incorrect version of mod");
                        }
                        else
                        {
                            LoggerInstance.Error("Could not find attribute, cannot fix incorrect version");
                        }
                    }
                    else
                    {
                        LoggerInstance.Msg("Correct mod version, not changing anything");
                    }
                    fileStream.Flush();
                    fileStream.Dispose();
                    _assembly.Dispose();
                    LoggerInstance.Msg("Successfully installed mod file " + Path.GetFileName(path).Pastel(theme.FileNameColor));
                }
                catch (Exception ex)
                {
                    LoggerInstance.Error($"An unexpected error occurred while installing content{ex}");
                    threwError = true;
                    success = false;
                }
            }
            else if (_fileType == FileType.MelonPlugin)
            {
                try
                {
                    LoggerInstance.Msg("Installing plugin file " + Path.GetFileName(path).Pastel(theme.FileNameColor));
                    if (!CheckCompability(_assembly)) { _assembly.Dispose(); threwError = true; success = false; return (success, threwError); }
#pragma warning disable CS0618 // Type or member is obsolete
                    string pluginPath = Path.Combine(Path.Combine(MelonUtils.BaseDirectory, "Plugins"), fileName);
                    string _path = Path.Combine(Path.Combine(MelonUtils.BaseDirectory, "Plugins"), Path.GetFileName(path));
#pragma warning restore CS0618 // Type or member is obsolete
                    if (!File.Exists(_path)) File.Move(path, _path);
                    else File.Replace(path, _path, Path.Combine(backupFolderPath, $"{Path.GetFileName(path)}-{DateTimeOffset.Now.ToUnixTimeSeconds()}.dll"));

                    LoggerInstance.Msg("Checking if plugin version is valid");
                    var fileStream = File.Open(_path, FileMode.Open, FileAccess.ReadWrite);
                    _assembly = AssemblyDefinition.ReadAssembly(fileStream);
                    var melonInfo = GetMelonInfo(_assembly);
                    if (melonInfo.Version < latestVersion)
                    {
                        LoggerInstance.Warning("Plugin has incorrect version which can lead to repeated unnecessary updates, fixing");
                        var module = _assembly.MainModule;
#pragma warning disable CS0618 // Type or member is obsolete
                        var attr = _assembly.CustomAttributes.Where(x => x.AttributeType.Name == nameof(MelonInfoAttribute) || x.AttributeType.Name == nameof(MelonPluginInfoAttribute));
#pragma warning restore CS0618 // Type or member is obsolete
                        if (attr.Any())
                        {
                            LoggerInstance.Msg("Found attribute");
                            var a = attr.First();
                            var semVersionType = module.ImportReference(typeof(SemVersion));
                            a.ConstructorArguments[2] = new CustomAttributeArgument(semVersionType, latestVersion);
                            _assembly.Write();
                            LoggerInstance.Msg("Fixed incorrect version of plugin");
                        }
                        else
                        {
                            LoggerInstance.Error("Could not find attribute, cannot fix incorrect version");
                        }
                    }
                    else
                    {
                        LoggerInstance.Msg("Correct plugin version, not changing anything");
                    }
                    _assembly.Dispose();
                    fileStream.Flush();
                    fileStream.Dispose();

                    //var melonAssembly = MelonAssembly.LoadMelonAssembly(pluginPath);
                    LoggerInstance.Warning("WARNING: The plugin will only work after game restart");
                    LoggerInstance.Msg("Successfully installed plugin file " + Path.GetFileName(path).Pastel(theme.FileNameColor));
                    success = true;
                }
                catch (Exception ex)
                {
                    LoggerInstance.Error($"An unexpected error occurred while installing content{ex}");
                    threwError = true;
                    success = false;
                }
            }
            else
            {
                LoggerInstance.Msg($"Not extracting {Path.GetFileName(path)}, because it does not have the Melon Info Attribute");
            }
            return (success, threwError);
        }

        /// <summary>
        /// Check directory for mods & plugins that can be updated
        /// </summary>
        /// <param name="directory"><see cref="Path"/> to the directory</param>
        /// <param name="automatic">If <see cref="true"/>, the mods/plugins will be updated automatically, otherwise there will be only a message displayed about a new version</param>
        internal void CheckDirectory(string directory, bool automatic = true)
        {
            List<string> files = Directory.GetFiles(directory, "*.dll").ToList();

            List<string> ignore = GetEntryValue<List<string>>(Entry_ignore);

            List<string> fileNameIgnore = new List<string>();
            files.ForEach(x =>
            {
                if (ignore != null && ignore.Count > 0)
                {
                    string fileName = Path.GetFileNameWithoutExtension(x);
                    if (ignore.Contains(fileName))
                    {
                        LoggerInstance.Msg($"{fileName} is in ignore list, removing from update list");
                        fileNameIgnore.Add(fileName);
                    }
                }
            });
            files.RemoveAll(x => fileNameIgnore.Contains(x));
            LoggerInstance.Msg("------------------------------".Pastel(theme.LineColor));
            foreach (string path in files)
            {
                AssemblyDefinition mainAssembly = AssemblyDefinition.ReadAssembly(path);
                bool _ignore = GetMAUIgnoreAttribute(mainAssembly) != null;
                var melonAssemblyInfo = GetMelonInfo(mainAssembly);
                string fileName = Path.GetFileName(path);
                if (_ignore)
                {
                    LoggerInstance.Msg($"Ignoring {fileName.Pastel(theme.FileNameColor)}, because it is configured to be ignored");
                    LoggerInstance.Msg("------------------------------".Pastel(theme.LineColor));
                    continue;
                }
                LoggerInstance.Msg($"File: {fileName.Pastel(theme.FileNameColor)}");
                FileType fileType = GetFileType(melonAssemblyInfo);
                if (fileType != FileType.Other)
                {
                    string assemblyName = (string)melonAssemblyInfo.Name.Clone();
                    if (melonAssemblyInfo != null)
                    {
                        if (!CheckCompability(mainAssembly)) { mainAssembly.Dispose(); continue; }
                        SemVersion currentVersion = SemVersion.Parse(melonAssemblyInfo.Version);
                        var data = GetModData(melonAssemblyInfo.DownloadLink, currentVersion);
                        data.Wait();
                        if (data.Result == null && string.IsNullOrEmpty(melonAssemblyInfo.DownloadLink))
                        {
                            if (GetEntryValue<bool>(Entry_bruteCheck))
                            {
                                LoggerInstance.Msg("Running " + "brute check..".Pastel(Color.Red));
                                data = GetModDataFromInfo(melonAssemblyInfo.Name, melonAssemblyInfo.Author, currentVersion);
                                data.Wait();
                            }
                        }
                        if (data.Result != null)
                        {
                            if (currentVersion != null && data.Result.LatestVersion != null)
                            {
                                if (data.Result.LatestVersion > currentVersion)
                                {
                                    if (automatic)
                                    {
                                        LoggerInstance.Msg($"A new version " + $"v{data.Result.LatestVersion}".Pastel(theme.NewVersionColor) + $" is available, meanwhile the current version is " + $"v{currentVersion}".Pastel(theme.OldVersionColor) + ", updating");
                                        LoggerInstance.Msg("Downloading file(s)");
                                        MAUDownloadFilesAllowedAttribute downloadFilesAllowed = GetMAUDownloadFilesAllowedAttribute(mainAssembly);
                                        foreach (var retFile in data.Result.DownloadFiles)
                                        {
                                            var httpClient = new HttpClient();
                                            var response = httpClient.GetAsync(retFile.URL, HttpCompletionOption.ResponseHeadersRead);
                                            response.Wait();
                                            FileStream downloadedFile = null;
                                            string pathToSave = "";
                                            string name = !string.IsNullOrEmpty(retFile.FileName) ? retFile.FileName : $"{melonAssemblyInfo.Name}-{MelonUtils.RandomString(7)}";
                                            try
                                            {
                                                response.Result.EnsureSuccessStatusCode();
                                                string resContentType = response.Result.Content.Headers.ContentType.MediaType;
                                                ContentType contentType;
                                                if (!string.IsNullOrEmpty(retFile.ContentType))
                                                {
                                                    bool parseSuccess = ContentType.TryParse(ContentType_Parse.MimeType, retFile.ContentType, out ContentType _contentType);
                                                    if (parseSuccess)
                                                    {
                                                        contentType = _contentType;
                                                        if (!string.IsNullOrEmpty(_contentType.Extension))
                                                        {
                                                            pathToSave = Path.Combine(tempFilesPath, $"{name.Replace(" ", "")}.{_contentType.Extension}");
                                                        }
                                                        else
                                                        {
                                                            LoggerInstance.Error("Content-Type is not associated with any file type, continuing without downloading & installing file");
                                                            response.Dispose();
                                                            httpClient.Dispose();
                                                            continue;
                                                        }
                                                    }
                                                    else
                                                    {
                                                        LoggerInstance.Error("Could not determine Content-Type, continuing without downloading & installing file");
                                                        response.Dispose();
                                                        httpClient.Dispose();
                                                        continue;
                                                    }
                                                }
                                                else if (resContentType != null)
                                                {
                                                    bool parseSuccess = ContentType.TryParse(ContentType_Parse.MimeType, resContentType, out ContentType _contentType);
                                                    if (parseSuccess)
                                                    {
                                                        contentType = _contentType;
                                                        if (!string.IsNullOrEmpty(_contentType.Extension))
                                                        {
                                                            pathToSave = Path.Combine(tempFilesPath, $"{name.Replace(" ", "")}.{_contentType.Extension}");
                                                        }
                                                        else
                                                        {
                                                            LoggerInstance.Error("Content-Type is not associated with any file type, continuing without downloading file");
                                                            response.Dispose();
                                                            httpClient.Dispose();
                                                            continue;
                                                        }
                                                    }
                                                    else
                                                    {
                                                        LoggerInstance.Error("Could not determine Content-Type, continuing without downloading file");
                                                        response.Dispose();
                                                        httpClient.Dispose();
                                                        continue;
                                                    }
                                                }
                                                else
                                                {
                                                    LoggerInstance.Error("No Content Type was provided, continuing without downloading file");
                                                    response.Dispose();
                                                    httpClient.Dispose();
                                                    continue;
                                                }
                                                if (downloadFilesAllowed != null && contentType != null && !string.IsNullOrEmpty(retFile.FileName))
                                                {
                                                    string _fileName = Path.GetFileName(pathToSave);
                                                    if (!string.IsNullOrEmpty(_fileName) && downloadFilesAllowed.AllowedFiles != null && downloadFilesAllowed.AllowedFiles.Any())
                                                    {
                                                        if (!downloadFilesAllowed.AllowedFiles.Contains(_fileName))
                                                        {
                                                            LoggerInstance.Msg($"{_fileName} was configured to not be downloaded & installed, aborting download");
                                                        }
                                                    }
                                                }
                                                var ms = response.Result.Content.ReadAsStreamAsync();
                                                ms.Wait();
                                                var fs = File.Create(pathToSave);
                                                var copyTask = CopyTo(ms.Result, fs);
                                                copyTask.Wait();
                                                fs.Flush();
                                                downloadedFile = fs;
                                                ms.Dispose();
                                                LoggerInstance.Msg($"Download successful");
                                            }
                                            catch (Exception ex)
                                            {
                                                LoggerInstance.Error($"Failed to download file through link{ex}");
                                                downloadedFile.Dispose();
                                                downloadedFile = null;
                                            }

                                            if (downloadedFile != null)
                                            {
                                                downloadedFile.Dispose();
                                                bool threwError = false;
                                                int failed = 0;
                                                int success = 0;
                                                if (Path.GetExtension(pathToSave) == ".zip")
                                                {
                                                    LoggerInstance.Msg("File is a ZIP, extracting files...");
                                                    string extractPath = Path.Combine(tempFilesPath, name.Replace(" ", "-"));
                                                    try
                                                    {
                                                        Task<bool> unzipTask = UnzipFromStream(File.OpenRead(pathToSave), extractPath);
                                                        unzipTask.Wait();
                                                        LoggerInstance.Msg("Successfully extracted files! Installing content..");
                                                        downloadedFile.Dispose();
                                                    }
                                                    catch (Exception ex)
                                                    {
                                                        threwError = true;
                                                        LoggerInstance.Error($"An exception occurred while extracting files from a ZIP file{ex}");
                                                        File.Delete(pathToSave);
                                                        DirectoryInfo tempDir = new DirectoryInfo(tempFilesPath);
                                                        foreach (FileInfo file in tempDir.GetFiles()) file.Delete();
                                                        foreach (DirectoryInfo subDirectory in tempDir.GetDirectories()) subDirectory.Delete(true);
                                                    }
                                                    var extractedFiles = Directory.GetFiles(extractPath).ToList();
                                                    Directory.GetDirectories(extractPath).ToList().ForEach((x) => extractedFiles.Add(x));
                                                    foreach (string extPath in extractedFiles)
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
#pragma warning disable CS0618 // Type or member is obsolete
                                                                    var res1 = MoveAllFiles(Path.Combine(extPath, subdir), Path.Combine(MelonUtils.BaseDirectory, subdir), string.Empty, data.Result.LatestVersion);
#pragma warning restore CS0618 // Type or member is obsolete
                                                                    checkedDirs++;
                                                                    success += res1.success;
                                                                    failed += res1.failed;
                                                                    if (res1.threwError) threwError = true;
                                                                }
                                                            }
                                                            if (checkedDirs < Directory.GetDirectories(extPath).Length)
                                                            {
                                                                LoggerInstance.Msg($"Found {dirName}, installing all content from it...");
#pragma warning disable CS0618 // Type or member is obsolete
                                                                var res1 = MoveAllFiles(extPath, Path.Combine(MelonUtils.BaseDirectory, dirName), string.Empty, data.Result.LatestVersion);
                                                                success += res1.success;
                                                                failed += res1.failed;
                                                                if (res1.threwError) threwError = true;
#pragma warning restore CS0618 // Type or member is obsolete
                                                            }
                                                        }
                                                        else if (Path.GetExtension(extPath) == ".dll")
                                                        {
                                                            var res = InstallPackage(extPath, data.Result.LatestVersion);
                                                            if (res.threwError) threwError = true;
                                                            if (res.success) success += 1;
                                                            else failed += 1;
                                                        }
                                                    }
                                                    Directory.Delete(extractPath, true);
                                                    File.Delete(pathToSave);
                                                }
                                                else if (Path.GetExtension(pathToSave) == ".dll")
                                                {
                                                    LoggerInstance.Msg("Downloaded file is a DLL file, installing content...");
                                                    var res = InstallPackage(pathToSave, data.Result.LatestVersion);
                                                    if (res.threwError) threwError = true;
                                                    if (res.success) success += 1;
                                                    else failed += 1;
                                                }
                                                else
                                                {
                                                    LoggerInstance.Warning($"An unknown file extension {Path.GetExtension(pathToSave)} was found that was not coded to be installed");
                                                }
                                                LoggerInstance.Msg(
                                                    threwError
                                                    ? $"Failed to update {assemblyName}".Pastel(Color.Red)
                                                    : success + failed > 0
                                                    ? $"Updated {assemblyName.Pastel(theme.FileNameColor)} from " + $"v{currentVersion}".Pastel(theme.OldVersionColor) + " --> " + $"v{data.Result.LatestVersion}".Pastel(theme.NewVersionColor) + ", " + $"({success}/{success + failed})".Pastel(theme.DownloadCountColor) + " installed successfully"
                                                    : "No mods and/or plugins were installed".Pastel(Color.Yellow)
                                                    );
                                            }
                                            else
                                            {
                                                LoggerInstance.Error("Downloaded file is empty, unable to update mod");
                                            }
                                        }
                                    }
                                    else
                                    {
                                        LoggerInstance.Msg($"A new version " + $"v{data.Result.LatestVersion}".Pastel(theme.NewVersionColor) + $" is available, meanwhile the current version is " + $"v{currentVersion}".Pastel(theme.OldVersionColor) + ". We recommend that you update, go to this site to download: " + melonAssemblyInfo.DownloadLink);
                                    }
                                }
                                else
                                {
                                    if (data.Result.LatestVersion == currentVersion)
                                    {
                                        LoggerInstance.Msg("Version is up-to-date!".Pastel(theme.UpToDateVersionColor));
                                    }
                                    else if (data.Result.LatestVersion < currentVersion)
                                    {
                                        LoggerInstance.Msg("Current version is newer than in the API".Pastel(theme.UpToDateVersionColor));
                                    }
                                }
                            }
                        }
                    }
                }
                else
                {
                    LoggerInstance.Warning($"{fileName} does not seem to be a MelonLoader Mod");
                }
                LoggerInstance.Msg("------------------------------".Pastel(theme.LineColor));
            }
        }

        // Note to self: Don't use async
        [System.Runtime.CompilerServices.MethodImpl(
    System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        public override void OnPreInitialization()
        {
            LoggerInstance.Msg("Checking if internet is connected");
            var internetConnected = CheckForInternetConnection();
            internetConnected.Wait();
            if (internetConnected.Result)
            {
                LoggerInstance.Msg("Internet is connected!");
            }
            else
            {
                LoggerInstance.Msg("Internet is not connected, aborting");
                return;
            }

            logger = LoggerInstance;
            UserAgent = $"{this.Info.Name}/{this.Info.Version} Auto-Updater for ML mods";
            Version = this.Info.Version;
            MAUSearch.UserAgent = UserAgent;
#if NET35
            LoggerInstance.Msg("Checking for dependencies");
#pragma warning disable CS0618 // Type or member is obsolete
            string path = Path.Combine(Path.Combine(MelonUtils.BaseDirectory, "MelonLoader"), "Managed");
#pragma warning restore CS0618 // Type or member is obsolete

            var assemblyInfo = System.Reflection.Assembly.GetExecutingAssembly().GetName();
            var resources = System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceNames();
            foreach (string resourceName in resources)
            {
                string remove = $"{assemblyInfo.Name}.Embedded.Dependencies.";
                if (!resourceName.StartsWith(remove)) continue;
                if (!string.IsNullOrEmpty(resourceName))
                {
                    string fileName = resourceName.Replace(remove, "");
                    string pathToFile = Path.Combine(path, fileName);
                    if (File.Exists(pathToFile))
                    {
                        LoggerInstance.Msg($"Found {fileName}");
                    }
                    else
                    {
                        LoggerInstance.Msg($"Did not find {fileName}, installing...");
                        try
                        {
                            var stream = Assembly
                                .GetExecutingAssembly()
                                .GetManifestResourceStream(resourceName);
                            Stream fileStream = File.Create(pathToFile);
                            fileStream.Flush();
                            fileStream.Seek(0, SeekOrigin.Begin);
                            lock (this)
                            {
                                CopyToNotTask(stream, fileStream);
                            };
                            fileStream.Dispose();
                            stream.Dispose();

                            System.Reflection.Assembly.LoadFile(pathToFile);

                            LoggerInstance.Msg($"Installed {fileName} successfully!");
                        }
                        catch (Exception ex)
                        {
                            LoggerInstance.Error($"Failed to install {fileName}\n{ex}");
                        }
                    }
                }
            }
#endif

            LoggerInstance.Msg("Creating folders in UserData");
#pragma warning disable CS0618 // Type or member is obsolete
            DirectoryInfo mainDir = Directory.CreateDirectory(Path.Combine(MelonUtils.UserDataDirectory, "MelonAutoUpdater"));
#pragma warning restore CS0618 // Type or member is obsolete
            DirectoryInfo tempDir = mainDir.CreateSubdirectory("TemporaryFiles");
            DirectoryInfo backupDir = mainDir.CreateSubdirectory("Backups");
            DirectoryInfo extensionsDir = mainDir.CreateSubdirectory("SearchExtensions");

            DirectoryInfo net35ExtDir = extensionsDir.CreateSubdirectory("net35");
            DirectoryInfo net6ExtDir = extensionsDir.CreateSubdirectory("net6");

            DirectoryInfo extConfigDir = extensionsDir.CreateSubdirectory("Config");

            extConfigFolderPath = extConfigDir.FullName;

            tempFilesPath = tempDir.FullName;
            mainFolderPath = mainDir.FullName;
            backupFolderPath = backupDir.FullName;

            LoggerInstance.Msg("Clearing possibly left temporary files");

            List<string> tempPaths = Directory.GetFiles(tempFilesPath).ToList();
            tempPaths.AddRange(Directory.GetDirectories(tempFilesPath));

            foreach (FileInfo file in tempDir.GetFiles()) file.Delete();
            foreach (DirectoryInfo subDirectory in tempDir.GetDirectories()) subDirectory.Delete(true);

            LoggerInstance.Msg("Setup Melon Preferences");

            lock (this)
            {
                SetupPreferences();
            }

            theme = ThemesCategory.GetValue<Theme>();

            bool enabled = GetEntryValue<bool>(Entry_enabled);

            if (!enabled)
            {
                LoggerInstance.Msg("Plugin disabled in preferences, aborting..");
                return;
            }

            ContentType.Load();

            LoggerInstance.Msg("Load search extensions");
            FileInfo[] extFiles = Environment.Version.Major >= 6 ? net6ExtDir.GetFiles("*.dll") : net35ExtDir.GetFiles("*.dll");
            List<Assembly> assemblies = new List<Assembly> { System.Reflection.Assembly.GetExecutingAssembly() };
            foreach (FileInfo file in extFiles)
            {
                LoggerInstance.Msg($"Checking {file.Name.Pastel(theme.FileNameColor)}");
                AssemblyDefinition assembly = Mono.Cecil.AssemblyDefinition.ReadAssembly(file.FullName);
                bool isExtension = false;
                foreach (var attr in assembly.CustomAttributes)
                {
                    if (attr.AttributeType.Name == nameof(IsMAUSearchExtensionAttribute))
                    {
                        bool value = Get<bool>(attr, 0);
                        assembly.Dispose();
                        isExtension = value;
                        break;
                    }
                }
                assembly.Dispose();

                if (isExtension)
                {
                    LoggerInstance.Msg($"{file.Name.Pastel(theme.FileNameColor)} is a MAU Search Extension");
                    System.Reflection.Assembly assembly1 = System.Reflection.Assembly.LoadFile(file.FullName);
                    assemblies.Add(assembly1);
                    LoggerInstance.Msg("Loading dependencies");
                    foreach (var a in assembly1.GetReferencedAssemblies())
                    {
                        System.Reflection.Assembly.Load(a);
                    }
                }
                else
                {
                    LoggerInstance.Msg($"{file.Name.Pastel(theme.FileNameColor)} is not a MAU Search Extension, continuing without loading");
                }
            }

            LoggerInstance.Msg("Setting up search extensions");
            extensions = MAUSearch.GetExtensions(assemblies.ToArray());

#pragma warning disable CS0618 // Type or member is obsolete
            string pluginsDir = Path.Combine(MelonUtils.BaseDirectory, "Plugins");
            string modsDir = Path.Combine(MelonUtils.BaseDirectory, "Mods");
#pragma warning restore CS0618 // Type or member is obsolete

            LoggerInstance.Msg("Checking plugins...");
            CheckDirectory(pluginsDir, false);
            LoggerInstance.Msg("Done checking plugins");

            LoggerInstance.Msg("Checking mods...");
            CheckDirectory(modsDir);
            LoggerInstance.Msg("Done checking mods");
        }
    }
}