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
using MelonAutoUpdater.JSONObjects;
using MelonLoader.TinyJSON;
using MelonAutoUpdater.Utils;

#if NET35_OR_GREATER
using System.Net;
#endif

#region Melon Attributes

[assembly: MelonInfo(typeof(MelonAutoUpdater.MelonAutoUpdater), "MelonAutoUpdater", "0.3.1", "HAHOOS", "https://github.com/HAHOOS/MelonAutoUpdater")]
[assembly: MelonPriority(-100000000)]
#pragma warning disable CS0618 // Type or member is obsolete
// Using ConsoleColor for backwards compatibility
[assembly: MelonColor(ConsoleColor.Green)]
#pragma warning restore CS0618 // Type or member is obsolete
[assembly: VerifyLoaderVersion("0.5.3", true)]
// They are not optional, but this is to remove the warning as NuGet will install them
[assembly: MelonOptionalDependencies("Net35.Http", "Rackspace.Threading", "System.Threading")]

#endregion Melon Attributes

#region Assembly Attributes

[assembly: AssemblyProduct("MelonAutoUpdater")]
[assembly: AssemblyVersion("0.3.1.0")]
[assembly: AssemblyFileVersion("0.3.1")]
[assembly: AssemblyTitle("MelonAutoUpdater")]
[assembly: AssemblyCompany("HAHOOS")]
[assembly: AssemblyDescription("An automatic updater for all your MelonLoader mods!")]
[assembly: AssemblyInformationalVersion("0.3.1")]

#endregion Assembly Attributes

namespace MelonAutoUpdater
{
    /// <summary>
    /// Class that contains most of MelonAutoUpdater's functionality
    /// </summary>
    public class MelonAutoUpdater : MelonPlugin
    {
        /// <summary>
        /// Version of MAU
        /// </summary>
        public static string Version { get; private set; }

        /// <summary>
        /// User Agent Header for all HTTP requests
        /// </summary>
        public static string UserAgent { get; private set; }

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

        private readonly Dictionary<string, Dictionary<string, string>> NuGetPackages = new Dictionary<string, Dictionary<string, string>> {
            {
                "net6",
                new Dictionary<string, string> {
                }
            },
            {
                "net35",
                new Dictionary<string, string> {
                    {
                        "Rackspace.Threading", "2.0.0-alpha001"
                    },
                    {
                        "TaskParallelLibrary", "1.0.2856"
                    },
                    {
                         "Net35.Http", "1.0.0"
                    },
                    {
                         "ValueTupleBridge", "0.1.5"
                    }
                }
            },
        };

        /// <summary>
        /// If <see langword="true"/>, mods will only be checked the versions and not updated, even when available
        /// </summary>
        private bool dontUpdate = false;

        /// <summary>
        /// Assembly of MelonLoader
        /// </summary>
        internal static Assembly MLAssembly;

        #region Melon Preferences

        /// <summary>
        /// Main Category in Preferences
        /// </summary>
        internal static MelonPreferences_Category MainCategory { get; private set; }

        /// <summary>
        /// A Melon Preferences entry of a list of mods/plugins that will not be updated
        /// </summary>
        internal static MelonPreferences_Entry Entry_ignore { get; private set; }

        /// <summary>
        /// A Melon Preferences entry of a boolean value indicating whether or not should the plugin work
        /// </summary>
        internal static MelonPreferences_Entry Entry_enabled { get; private set; }

        /// <summary>
        /// A Melon Preferences entry of a boolean value indicating whether or not it should forcefully check the API for the mod/plugins if no download link was provided with it
        /// </summary>
        internal static MelonPreferences_Entry Entry_bruteCheck { get; private set; }

        /// <summary>
        /// A Melon Preferences entry of a boolean value indicating whether or not should the melons be updated if available
        /// </summary>
        internal static MelonPreferences_Entry Entry_dontUpdate { get; private set; }

        /// <summary>
        /// Themes Category in Preferences
        /// </summary>
        internal static MelonPreferences_ReflectiveCategory ThemesCategory { get; private set; }

        /// <summary>
        /// Extensions Category in Preferences
        /// </summary>
        internal static MelonPreferences_Category ExtensionsCategory { get; private set; }

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
            MainCategory.SetFilePath(Path.Combine(Files.MainFolder, "config.cfg"));

            Entry_ignore = MainCategory.CreateEntry<List<string>>("IgnoreList", new List<string>(), "Ignore List",
                description: "List of all names of Mods & Plugins that will be ignored when checking for updates");

            Entry_enabled = MainCategory.CreateEntry<bool>("Enabled", true, "Enabled",
                description: "If true, Mods & Plugins will update on every start");

            Entry_dontUpdate = MainCategory.CreateEntry<bool>("DontUpdate", false, "Dont Update",
                description: "If true, Melons will only be checked if they are outdated or not, they will not be updated automatically");
            if (dontUpdate == false) dontUpdate = (bool)Entry_dontUpdate.BoxedValue;

            Entry_bruteCheck = MainCategory.CreateEntry<bool>("BruteCheck", false, "Brute Check",
                description: "If true, when there's no download link provided with mod/plugin, it will check every supported platform providing the Name & Author\nWARNING: You may get rate-limited with large amounts of mods/plugins, use with caution");

            MainCategory.SaveToFile(false);

            // Themes Category

            ThemesCategory = MelonPreferences.CreateCategory<Theme>("Theme", "Theme");
            ThemesCategory.SetFilePath(Path.Combine(Files.MainFolder, "theme.cfg"));
            ThemesCategory.SaveToFile(false);

            // Extensions Category

            ExtensionsCategory = MelonPreferences.CreateCategory("Extensions", "Extensions");
            ExtensionsCategory.SetFilePath(Path.Combine(Files.MainFolder, "extensions.cfg"));

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

        #region Command Line Arguments

        /// <summary>
        /// If <see langword="true"/>, plugin will not continue execution
        /// </summary>
        private bool stopPlugin = false;

        [System.Runtime.CompilerServices.MethodImpl(
    System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private void HandleArguments()
        {
            var args = MelonLaunchOptions.ExternalArguments;

            // Disable argument - melonautoupdater.disable
            if (args.ContainsKey("melonautoupdater.disable"))
            {
                LoggerInstance.Msg("Disable argument found, disabling plugin..");
                stopPlugin = true;
            }

            // Dont Update argument - melonautoupdater.dontupdate

            if (args.ContainsKey("melonautoupdater.dontupdate"))
            {
                LoggerInstance.Msg("DontUpdate argument found, will only check versions");
                dontUpdate = true;
            }
        }

        #endregion Command Line Arguments

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

        // If you are wondering, this is from StackOverflow, although a bit edited, I'm just a bit lazy
        /// <summary>
        /// Checks for internet connection
        /// </summary>
        /// <param name="timeoutMs">Time in milliseconds after the request will be aborted if no response (Default: 5000)</param>
        /// <param name="url">URL of the website used to check for connection (Default: <c>http://www.gstatic.com/generate_204</c>)</param>
        /// <returns>If <see langword="true"/>, there's internet connection, otherwise <see langword="false"/></returns>
        public static bool CheckForInternetConnection(int timeoutMs = 5000, string url = "http://www.gstatic.com/generate_204")
        {
            try
            {
#if NET35_OR_GREATER
                var request = new WebClient();
                request.DownloadData(url);
#elif NET6_0_OR_GREATER
                var request = new HttpClient();
                var res = request.GetAsync(url);
                res.Wait();
                res.Result.EnsureSuccessStatusCode();
#endif
                return true;
            }
            catch
            {
                return false;
            }
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
        /// <param name="path">A path to directory to copy from</param>
        /// <param name="directory">A path to directory to copy to</param>
        /// <param name="mainDirectoryName">Only used in prefix, just set <see cref="string.Empty"/></param>
        /// <param name="latestVersion">The latest version of the mod the files are from</param>
        /// <param name="config">Config of the Melon</param>
        /// <returns>Info about mod/plugin install (times when it succeeded, times when it failed, and if it threw an error)</returns>
        internal (int success, int failed, bool threwError) MoveAllFiles(string path, string directory, string mainDirectoryName, SemVersion latestVersion, MelonConfig config)
        {
            int success = 0;
            int failed = 0;
            bool threwError = false;
            string prefix = (string.IsNullOrEmpty(mainDirectoryName) != true ? $"{mainDirectoryName}/{GetDirName(directory)}" : GetDirName(directory)).Pastel(Color.Cyan);
            foreach (string file in Directory.GetFiles(path))
            {
                if (!config.CanInclude(file))
                {
                    LoggerInstance._MsgPastel($"[{prefix}] {Path.GetFileName(file)} will not be loaded due to the Melon being configured this way");
                    continue;
                }
                LoggerInstance._MsgPastel($"[{prefix}] {Path.GetFileName(file)} found, copying file to folder");
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
                        else File.Replace(file, _path, Path.Combine(Files.BackupFolder, $"{Path.GetFileName(path)}-{DateTimeOffset.Now.ToUnixTimeSeconds()}.{Path.GetExtension(file)}"));
                        LoggerInstance._MsgPastel($"[{prefix}] Successfully copied {Path.GetFileName(file)}");
                    }
                }
                catch (Exception ex)
                {
                    LoggerInstance.Error($"[{prefix}] Failed to copy {Path.GetFileName(file)}, exception thrown:{ex}");
                }
            }
            foreach (string dir in Directory.GetDirectories(path))
            {
                if (!config.CanInclude(dir))
                {
                    LoggerInstance._MsgPastel($"[{prefix}] {GetDirName(dir)} will not be loaded due to the Melon being configured this way");
                    continue;
                }
                LoggerInstance._MsgPastel($"[{prefix}] Found folder {GetDirName(dir)}, going through files");
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
        /// Checks if the <see cref="Assembly"/> is compatible with the current ML Instance
        /// </summary>
        /// <param name="assembly"><see cref="AssemblyDefinition"/> to check</param>
        /// <returns><see langword="true"/>, if compatible, otherwise <see langword="false"/></returns>
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
        /// Installs melon from path
        /// </summary>
        /// <param name="path">Path of melon</param>
        /// <param name="latestVersion">Latest version of melon, used to modify <see cref="MelonInfoAttribute"/> in case the version is not correct</param>
        /// <returns>A <see langword="Tuple"/>, success and threwError, self explanatory</returns>
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
                    LoggerInstance._MsgPastel("Installing mod file " + Path.GetFileName(path).Pastel(theme.FileNameColor));
                    if (!CheckCompability(_assembly)) { _assembly.Dispose(); threwError = true; success = false; return (success, threwError); }
#pragma warning disable CS0618 // Type or member is obsolete
                    string _path = Path.Combine(Path.Combine(MelonUtils.BaseDirectory, "Mods"), Path.GetFileName(path));
#pragma warning restore CS0618 // Type or member is obsolete
                    if (!File.Exists(_path)) File.Move(path, _path);
                    else File.Replace(path, _path, Path.Combine(Files.BackupFolder, $"{Path.GetFileName(path)}-{DateTimeOffset.Now.ToUnixTimeSeconds()}.dll"));
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
                    LoggerInstance._MsgPastel("Successfully installed mod file " + Path.GetFileName(path).Pastel(theme.FileNameColor));
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
                    LoggerInstance._MsgPastel("Installing plugin file " + Path.GetFileName(path).Pastel(theme.FileNameColor));
                    if (!CheckCompability(_assembly)) { _assembly.Dispose(); threwError = true; success = false; return (success, threwError); }
#pragma warning disable CS0618 // Type or member is obsolete
                    string pluginPath = Path.Combine(Path.Combine(MelonUtils.BaseDirectory, "Plugins"), fileName);
                    string _path = Path.Combine(Path.Combine(MelonUtils.BaseDirectory, "Plugins"), Path.GetFileName(path));
#pragma warning restore CS0618 // Type or member is obsolete
                    if (!File.Exists(_path)) File.Move(path, _path);
                    else File.Replace(path, _path, Path.Combine(Files.BackupFolder, $"{Path.GetFileName(path)}-{DateTimeOffset.Now.ToUnixTimeSeconds()}.dll"));

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
                    LoggerInstance._MsgPastel("Successfully installed plugin file " + Path.GetFileName(path).Pastel(theme.FileNameColor));
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

        // Note to self: Don't use async
        /// <summary>
        /// Runs before MelonLoader fully initializes
        /// </summary>
        public override void OnPreInitialization()
        {
            MLAssembly = AppDomain.CurrentDomain.GetAssemblies().Where(x => x.GetName().Name == "MelonLoader").FirstOrDefault();
            Version MelonLoaderVersion = MLAssembly.GetName().Version;

            if (new SemVersion(MelonLoaderVersion.Major, MelonLoaderVersion.Minor, MelonLoaderVersion.Build) >= new SemVersion(0, 6, 5))
            {
                LoggerInstance.Msg("Checking command line arguments");
                HandleArguments();
            }
            else
            {
                LoggerInstance.Msg($"Could not check command line arguments due to outdated MelonLoader version (Current is v{MelonLoaderVersion.Major}.{MelonLoaderVersion.Minor}.{MelonLoaderVersion.Build}, required is minimum v0.6.5)");
            }

            if (stopPlugin) return;

            LoggerInstance.Msg("Creating folders in UserData");

            Files.Setup();

            LoggerInstance.Msg("Clearing possibly left temporary files");

            Files.Clear(TempDirectory.Melons);

            LoggerInstance.Msg("Loading necessary dependencies");
#if NET35_OR_GREATER
            var dependencies = NuGetPackages["net35"];
#elif NET6_0_OR_GREATER
            var dependencies = NuGetPackages["net6"];
#endif

            var nuget = new NuGet();
            nuget.Log += (sender, args) =>
            {
                string msg = $"[" + "NuGet".Pastel(Color.Cyan) + $"] {args.Message}";
                string msg_nopastel = $"[NuGet] {args.Message}";
                if (args.Severity == NuGet.LogSeverity.MESSAGE)
                {
                    LoggerInstance._MsgPastel(msg);
                }
                else if (args.Severity == NuGet.LogSeverity.WARNING)
                {
                    LoggerInstance.Warning(msg_nopastel);
                }
                else if (args.Severity == NuGet.LogSeverity.ERROR)
                {
                    LoggerInstance.Error(msg_nopastel);
                }
            };

            int needDownload = dependencies.Count;
            if (dependencies != null && needDownload > 0)
            {
                foreach (var dependency in dependencies)
                {
                    bool isLoaded = nuget.Internal_IsLoaded(dependency.Key, true, dependency.Value, true);
                    if (!isLoaded)
                    {
                        LoggerInstance._MsgPastel($"{dependency.Key.Pastel(theme.FileNameColor)} is not loaded!");
                        // Install package

                        nuget.InstallPackage(dependency.Key, dependency.Value);
                    }
                    else
                    {
                        LoggerInstance._MsgPastel($"{dependency.Key.Pastel(theme.FileNameColor)} is loaded!");
                    }
                }
            }
            LoggerInstance.Msg("Checking if internet is connected");
            var internetConnected = CheckForInternetConnection();
            if (internetConnected)
            {
                LoggerInstance.Msg("Internet is connected!");
            }
            else
            {
                LoggerInstance.Msg("Internet is not connected, aborting");
                return;
            }

            logger = LoggerInstance;
            UserAgent = $"{this.Info.Name}/{this.Info.Version} Auto-Updater for ML mods and plugins";
            Version = this.Info.Version;
            MAUSearch.UserAgent = UserAgent;

            LoggerInstance.Msg("Setup Melon Preferences");

            SetupPreferences();

            theme = ThemesCategory.GetValue<Theme>();

            bool enabled = GetEntryValue<bool>(Entry_enabled);

            if (!enabled)
            {
                LoggerInstance.Msg("Plugin disabled in preferences, aborting..");
                return;
            }

            ContentType.Load();

            LoggerInstance.Msg("Load search extensions");
            FileInfo[] extFiles = Environment.Version.Major >= 6 ? new DirectoryInfo(Files.Net6ExtFolder).GetFiles("*.dll") : new DirectoryInfo(Files.Net35ExtFolder).GetFiles("*.dll");
            List<Assembly> assemblies = new List<Assembly> { System.Reflection.Assembly.GetExecutingAssembly() };
            foreach (FileInfo file in extFiles)
            {
                LoggerInstance._MsgPastel($"Checking {file.Name.Pastel(theme.FileNameColor)}");
                AssemblyDefinition assembly = AssemblyDefinition.ReadAssembly(file.FullName);
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
                    LoggerInstance._MsgPastel($"{file.Name.Pastel(theme.FileNameColor)} is a MAU Search Extension");
                    System.Reflection.Assembly assembly1 = System.Reflection.Assembly.LoadFile(file.FullName);
                    assemblies.Add(assembly1);
                    LoggerInstance.Msg("Loading dependencies");
                }
                else
                {
                    LoggerInstance._MsgPastel($"{file.Name.Pastel(theme.FileNameColor)} is not a MAU Search Extension, continuing without loading");
                }
            }

            LoggerInstance.Msg("Setting up search extensions");
            extensions = MAUSearch.GetExtensions(assemblies.ToArray());

#pragma warning disable CS0618 // Type or member is obsolete
            string pluginsDir = Path.Combine(MelonUtils.BaseDirectory, "Plugins");
            string modsDir = Path.Combine(MelonUtils.BaseDirectory, "Mods");
#pragma warning restore CS0618 // Type or member is obsolete

            var updater = new MelonUpdater(extensions, UserAgent, theme, GetEntryValue<List<string>>(Entry_ignore), GetEntryValue<bool>(Entry_bruteCheck));

            LoggerInstance.Msg("Checking plugins...");
            updater.CheckDirectory(pluginsDir, false);
            LoggerInstance.Msg("Done checking plugins");

            LoggerInstance.Msg("Checking mods...");
            updater.CheckDirectory(modsDir);
            LoggerInstance.Msg("Done checking mods");
        }
    }
}