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
using System.Diagnostics;
using System.Net.Http;
using static ml065::MelonLoader.MelonPlatformAttribute;
using static ml065::MelonLoader.MelonPlatformDomainAttribute;

namespace MelonAutoUpdater
{
    /// <summary>
    /// Class that handles all of the checking and updating
    /// </summary>
    public class MelonUpdater
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
        /// Get data about the melon from a downloadLink<br/>
        /// Currently Supported: Thunderstore, Github
        /// </summary>
        /// <param name="downloadLink">Download Link, possibly included in the <see cref="MelonInfoAttribute"/></param>
        /// <param name="currentVersion">Current version of the Melon in question</param>
        /// <param name="melonConfig">Config, if found, of the Melon</param>
        /// <returns>If found, returns a <see cref="MelonData"/> object which includes the latest version of the melon online and the download link(s)</returns>
        internal MelonData GetMelonData(string downloadLink, SemVersion currentVersion, MelonConfig melonConfig)
        {
            if (string.IsNullOrEmpty(downloadLink) || downloadLink == "UNKNOWN")
            {
                Logger.Msg("No download link was provided with the melon");
                return null;
            }
            List<SearchExtension> extensions = new List<SearchExtension>();
            foreach (var _ext in ExtensionBase.LoadedExtensions)
            {
                if (_ext.Type == typeof(SearchExtension))
                {
                    var ext = _ext as SearchExtension;
                    extensions.Add(ext);
                }
            }
            extensions.OrderBy(x => x.Priority * (-1));
            foreach (var ext in extensions)
            {
                if (CanSearch(ext, melonConfig))
                {
                    Logger.MsgPastel($"Checking with {ext.Name.Pastel(ext.NameColor)}");
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
            return null;
        }

        /// <summary>
        /// Get data about the melon from name and author<br/>
        /// Github is not supported in brute checking due to extremely strict rate limits
        /// Currently Supported: Thunderstore
        /// </summary>
        /// <returns>If found, returns a <see cref="MelonData"/> object which includes the latest version of the melon online and the download link(s)</returns>
        internal MelonData GetMelonDataFromInfo(string name, string author, SemVersion currentVersion, MelonConfig melonConfig)
        {
            if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(author) || author == "UNKNOWN")
            {
                Logger.Msg("Name/Author was not provided with the melon");
                return null;
            }
            List<SearchExtension> extensions = new List<SearchExtension>();
            foreach (var _ext in ExtensionBase.LoadedExtensions)
            {
                if (_ext.Type == typeof(SearchExtension))
                {
                    var ext = _ext as SearchExtension;
                    extensions.Add(ext);
                }
            }
            extensions.OrderBy(x => x.Priority * (-1));
            foreach (var ext in extensions)
            {
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

            return null;
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

        /// <summary>
        /// Checks if the <see cref="Assembly"/> is compatible with the current ML Instance
        /// </summary>
        /// <param name="assembly"><see cref="AssemblyDefinition"/> to check</param>
        /// <param name="printmsg">If <see langword="true"/>, will log the incompatibilities</param>
        /// <returns>Array of all incompatibilities</returns>
        public static Incompatibility[] CheckCompatibility(AssemblyDefinition assembly, bool printmsg = true)
        {
            if (!GetEntryValue<bool>(MelonAutoUpdater.Entry_checkCompatibility)) return new Incompatibility[] { };
            var result = new List<Incompatibility>();
            var modInfo = MelonAttribute.GetMelonInfo(assembly);
            if (modInfo == null)
            {
                // Assembly is not a mod, only checking .NET version
                bool net6 = Environment.Version.Major >= 6;
                if (!net6)
                {
                    bool isFramework = assembly.MainModule.AssemblyReferences.Where(x => x.Name == "mscorlib") != null;
                    if (!isFramework)
                    {
                        result.Add(Incompatibility.NETVersion);
                    }
                }
            }
            else
            {
                CompatiblePlatforms CurrentPlatform = MelonUtils.IsGame32Bit() ? CompatiblePlatforms.WINDOWS_X86 : CompatiblePlatforms.WINDOWS_X64; // Temporarily
                CompatibleDomains CurrentDomain = MelonUtils.IsGameIl2Cpp() ? CompatibleDomains.IL2CPP : CompatibleDomains.MONO;

                var name = AssemblyNameReference.Parse(MelonAutoUpdater.MLAssembly.FullName);
                assembly.MainModule.AssemblyReferences.Add(name);

                var loaderVer = MelonAttribute.GetLoaderVersionRequired(assembly);
                var game = MelonAttribute.GetMelonGameAttribute(assembly);
                var gameVers = MelonAttribute.GetMelonGameVersionAttribute(assembly);
                var process = MelonAttribute.GetMelonProcessAttribute(assembly);
                var platform = MelonAttribute.GetMelonPlatformAttribute(assembly);
                var domain = MelonAttribute.GetMelonPlatformDomainAttribute(assembly);
                var build = MelonAttribute.GetVerifyLoaderBuildAttribute(assembly);
                if (!(loaderVer == null || MelonAttribute.IsCompatible(loaderVer, MelonAutoUpdater.MLVersion)))
                {
                    if (printmsg) Logger.Warning($"{modInfo.Name} {modInfo.Version} is not compatible with the current version of MelonLoader : v{MelonAutoUpdater.MLVersion}");
                    if (printmsg) Logger.Warning($"Compatible Versions:");
                    if (printmsg) Logger.Warning($"    - v{loaderVer.SemVer} {(loaderVer.IsMinimum ? "or higher" : "")}");
                    result.Add(Incompatibility.MLVersion);
                }
                else if (!(build == null || MelonAttribute.IsCompatible(build, MelonUtils.HashCode)))
                {
                    if (printmsg) Logger.Warning($"{modInfo.Name} {modInfo.Version} is not compatible with the current build hash code of MelonLoader : {MelonUtils.HashCode}");
                    if (printmsg) Logger.Warning($"Compatible Build Hash Codes:");
                    if (printmsg) Logger.Warning($"    - v{build.HashCode}");
                    result.Add(Incompatibility.MLBuild);
                }
                bool net6 = Environment.Version.Major >= 6;
                if (!net6)
                {
                    bool isFramework = assembly.MainModule.AssemblyReferences.Where(x => x.Name == "mscorlib") != null;
                    if (!isFramework)
                    {
                        if (printmsg) Logger.Error($"{modInfo.Name} {modInfo.Version} is not compatible with .NET Framework");
                        result.Add(Incompatibility.NETVersion);
                    }
                }
#pragma warning disable CS0618 // Type or member is obsolete
                var gameName = ml065.MelonLoader.MelonUtils.GameName;
                var gameDev = ml065.MelonLoader.MelonUtils.GameDeveloper;
                var gameVer = ml065.MelonLoader.MelonUtils.GameVersion;
#pragma warning restore CS0618 // Type or member is obsolete
                if (!(game.Length == 0 || game.Any(x => x.IsCompatible(gameDev, gameName))))
                {
                    if (printmsg) Logger.Warning($"{modInfo.Name} {modInfo.Version} is not compatible with the running game: {gameName} (by {gameDev})");
                    if (printmsg) Logger.Warning($"Compatible Games:");
                    foreach (var g in game)
                    {
                        if (printmsg) Logger.Warning($"=  - {g.Name} by {g.Developer}");
                    }
                    result.Add(Incompatibility.Game);
                }
                else
                {
                    if (!(gameVers.Length == 0 || gameVers.Any(x => x.Version == gameVer)))
                    {
                        if (printmsg) Logger.Warning($"{modInfo.Name} {modInfo.Version} is not compatible with the version of the running game: {gameVer}");
                        if (printmsg) Logger.Warning($"Compatible Game Versions:");
                        foreach (var g in gameVers)
                        {
                            if (printmsg) Logger.Warning($"   - {g.Version}");
                        }
                        result.Add(Incompatibility.GameVersion);
                    }
                    var processName = Process.GetCurrentProcess().ProcessName;
                    if (!(process.Length == 0 || process.Any(x => MelonAttribute.IsCompatible(x, processName))))
                    {
                        if (printmsg) Logger.Warning($"{modInfo.Name} {modInfo.Version} is not compatible with the running process: {processName}");
                        if (printmsg) Logger.Warning($"Compatible Processes:");
                        foreach (var g in process)
                        {
                            if (printmsg) Logger.Warning($"   - {g.EXE_Name}");
                        }
                        result.Add(Incompatibility.ProcessName);
                    }

                    if (!(platform == null || MelonAttribute.IsCompatible(platform, CurrentPlatform)))
                    {
                        if (printmsg) Logger.Warning($"{modInfo.Name} {modInfo.Version} is not compatible with the current platform: {CurrentPlatform}");
                        if (printmsg) Logger.Warning($"Compatible Platforms:");
                        foreach (var p in platform.Platforms)
                        {
                            if (printmsg) Logger.Warning($"   - {p}");
                        }
                        result.Add(Incompatibility.Platform);
                    }
                    if (!(domain == null || MelonAttribute.IsCompatible(domain, CurrentDomain)))
                    {
                        if (printmsg) Logger.Warning($"{modInfo.Name} {modInfo.Version} is not compatible with the current platform: {CurrentDomain}");
                        if (printmsg) Logger.Warning($"Compatible Domain:");
                        if (printmsg) Logger.Warning($"   - {domain.Domain}");
                        result.Add(Incompatibility.Domain);
                    }
                }
            }
            return result.ToArray();
        }

        /// <summary>
        /// Enum indicating what incompatibilities a <see cref="AssemblyDefinition"/> has
        /// </summary>
        public enum Incompatibility
        {
            /// <summary>
            /// Incompatible MelonLoader Version (indicated by <see cref="VerifyLoaderVersionAttribute"/>)
            /// </summary>
            MLVersion,

            /// <summary>
            /// Incompatible MelonLoader Build (indicated by <see cref="VerifyLoaderBuildAttribute"/>)
            /// </summary>
            MLBuild,

            /// <summary>
            /// Incompatible Game (indicated by <see cref="MelonGameAttribute"/>)
            /// </summary>
            Game,

            /// <summary>
            /// Incompatible Game Version (indicated by <see cref="MelonGameVersionAttribute"/>)
            /// </summary>
            GameVersion,

            /// <summary>
            /// Incompatible Process Name (indicated by <see cref="MelonProcessAttribute"/>)
            /// </summary>
            ProcessName,

            /// <summary>
            /// Incompatible Domain (indicated by <see cref="MelonPlatformDomainAttribute"/>)
            /// </summary>
            Domain,

            /// <summary>
            /// Incompatible Platform (indicated by <see cref="MelonPlatformAttribute"/>)
            /// </summary>
            Platform,

            /// <summary>
            /// Incompatible Version of .NET (<see cref="AssemblyDefinition"/> is running on .NET 6, meanwhile the game is .NET Framework [some version here])
            /// </summary>
            NETVersion
        }

        /// <summary>
        /// Variable if melon being checked needs to be updated due to being incompatible
        /// </summary>
        internal static bool needUpdate = false;

        /// <summary>
        /// Variable if melon being checked needs to be updated due to being incompatible
        /// </summary>
        internal static string melonFileName = string.Empty;

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
                AssemblyDefinition mainAssembly = AssemblyDefinition.ReadAssembly(path, new ReaderParameters() { AssemblyResolver = new CustomCecilResolver() });
                var config = GetMelonConfig(mainAssembly);
                if (config != null)
                {
                    Logger.Msg("Found MAU config associated with Melon");
                }
                bool _ignore = config != null && config.Disable;
                var melonAssemblyInfo = mainAssembly.GetMelonInfo();
                if (_ignore)
                {
                    Logger.MsgPastel($"Ignoring {fileName.Pastel(theme.FileNameColor)}, because it is configured to be ignored");
                    Logger.MsgPastel("------------------------------".Pastel(theme.LineColor));
                    mainAssembly.Dispose();
                    continue;
                }
                if (melonAssemblyInfo != null)
                {
                    Logger.Msg($"{melonAssemblyInfo.Name.Pastel(Theme.Instance.FileNameColor)} " + $"v{melonAssemblyInfo.Version}".Pastel(Theme.Instance.OldVersionColor));
                    string assemblyName = (string)melonAssemblyInfo.Name.Clone();
                    if (melonAssemblyInfo != null)
                    {
                        if (CheckCompatibility(mainAssembly).Length > 0) { InstallExtension.NeedUpdate = true; } else { InstallExtension.NeedUpdate = false; }
                        SemVersion currentVersion = melonAssemblyInfo.SemanticVersion;
                        var data = GetMelonData(melonAssemblyInfo.DownloadLink, currentVersion, config);
                        if (data == null || string.IsNullOrEmpty(melonAssemblyInfo.DownloadLink))
                        {
                            if (bruteCheck)
                            {
                                Logger.MsgPastel("Running " + "brute check..".Pastel(Color.Red));
                                _bruteCheck = true;
                                data = GetMelonDataFromInfo(melonAssemblyInfo.Name, melonAssemblyInfo.Author, currentVersion, config);
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
                                        List<string> downloadedFiles = new List<string>();
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
                                                downloadedFile.Dispose();
                                                downloadedFiles.Add(pathToSave);
                                            }
                                            catch (Exception ex)
                                            {
                                                Logger.Error($"Failed to download file through link, exception:\n{ex}");
                                                downloadedFile.Dispose();
                                                downloadedFile = null;
                                            }

                                            if (MelonAutoUpdater.Debug)
                                            {
                                                sw3.Stop();
                                                MelonAutoUpdater.ElapsedTime.Add($"DownloadFile-{name}", sw.ElapsedMilliseconds);
                                            }
                                        }
                                        InstallExtension.InstallList = downloadedFiles.ToArray();
                                        InstallExtension.MelonCurrentVersion = currentVersion;
                                        InstallExtension.MelonConfig = config;
                                        InstallExtension.MelonFileName = fileName;
                                        InstallExtension.MelonData = data;
                                        foreach (var downloadPath in downloadedFiles)
                                        {
                                            InstallExtension.FileName = Path.GetFileNameWithoutExtension(downloadPath);

                                            var downloadedFile = new FileInfo(downloadPath).OpenRead();
                                            if (downloadedFile != null && downloadedFile.Length > 0)
                                            {
                                                downloadedFile.Dispose();
                                                var res = InstallExtension.HandleFile(downloadPath);
                                                if (res.handled)
                                                {
                                                    success += res.success;
                                                    failed += res.failed;
                                                }
                                                else
                                                {
                                                    File.Delete(downloadPath);
                                                }
                                            }
                                            else
                                            {
                                                Logger.Error("Downloaded file is empty, unable to update melon");
                                            }
                                        }
                                        Logger.MsgPastel(
                                            failed > 0
                                                ? $"Failed to update {assemblyName}".Pastel(Color.Red)
                                                : success + failed > 0
                                                ? $"Updated {assemblyName.Pastel(theme.FileNameColor)} from " + $"v{currentVersion}".Pastel(theme.OldVersionColor) + " --> " + $"v{data.LatestVersion}".Pastel(theme.NewVersionColor) + ", " + $"({success}/{success + failed})".Pastel(theme.DownloadCountColor) + " melons installed successfully"
                                                : "No melons were installed".Pastel(Color.Yellow)
                                        );

                                        if (failed > 0) result.error++;
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
                            if (MelonAttribute.GetFileType(melonAssemblyInfo) == FileType.MelonMod)
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
                    if (failed <= 0)
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