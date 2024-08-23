using MelonLoader;
using MelonLoader.Utils;
using Mono.Cecil;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Reflection.PortableExecutable;
using System.Runtime.InteropServices;
using System.Runtime.Loader;
using System.Text.Json;
using System.Text.RegularExpressions;

[assembly: MelonInfo(typeof(MelonAutoUpdater.Core), "MelonModUpdater", "1.0.0", "HAHOOS", null)]
[assembly: MelonPriority(-100)]
[assembly: MelonID("272b5cba-0b4c-4d0a-b6b6-ba2bfbb7c716")]
[assembly: AssemblyTitle("MelonModUpdater")]
[assembly: AssemblyProduct("MelonModUpdater")]
[assembly: AssemblyVersion("1.0.0")]
[assembly: AssemblyFileVersion("1.0.0")]
[assembly: AssemblyDescription("A MelonLoader Plugin that automatically updates mods & plugins!")]
[assembly: AssemblyCompany("HAHOOS")]
[assembly: Guid("272b5cba-0b4c-4d0a-b6b6-ba2bfbb7c716")]

namespace MelonAutoUpdater
{
    internal class Core : MelonPlugin
    {
        /// <summary>
        /// Path of the Temporary Files folder where downloaded files and uncompressed zip files get put temporarily
        /// </summary>
        internal string tempFilesPath = "";

        /// <summary>
        /// Path of MelonAutoUpdate folder containing all the other folders
        /// </summary>
        internal string mainFolderPath = "";

        /// <summary>
        /// Path of Backup folder where old versions of mods are saved
        /// </summary>
        internal string backupFolderPath = "";

        #region Melon Preferences

        public MelonPreferences_Category category { get; private set; }

        public MelonPreferences_Entry entry_ignore { get; private set; }
        public MelonPreferences_Entry entry_priority { get; private set; }
        public MelonPreferences_Entry entry_enabled { get; private set; }

        /// <summary>
        /// Setup Preferences
        /// </summary>
        private void SetupPreferences()
        {
            category = MelonPreferences.CreateCategory("MelonAutoUpdater", "Melon Auto Updater");
            category.SetFilePath(Path.Combine(mainFolderPath, "config.cfg"));

            entry_ignore = category.CreateEntry<List<string>>("IgnoreList", [], "Ignore List",
                description: "List of all names of Mods & Plugins that will be ignored when checking for updates");
            entry_priority = category.CreateEntry<List<string>>("PriorityList", [], "Priority List",
                description: "List of all names of Mods & Plugins that will be updated first");
            entry_enabled = category.CreateEntry<bool>("Enabled", true, "Enabled",
                description: "If true, Mods & Plugins will update on every start");

            LoggerInstance.Msg("Successfully set up Melon Preferences!");
        }

        private T GetPreferenceValue<T>(MelonPreferences_Entry entry)
        {
            if (entry != null && entry.BoxedValue != null)
            {
                try
                {
                    return (T)entry.BoxedValue;
                }
                catch (InvalidCastException)
                {
                    LoggerInstance.Error($"Preference '{entry.DisplayName}' is of incorrect type, please go to UserData/MelonAutoUpdater/config.cfg to fix");
                    return default(T);
                }
            }
            return default(T);
        }

        #endregion Melon Preferences

        // If you are wondering, this is from StackOverflow, although a bit edited, im just a bit lazy
        /// <summary>
        /// Checks for internet connection
        /// </summary>
        /// <param name="timeoutMs">Time in milliseconds after the request will be aborted if no response (Default: 10000)</param>
        /// <param name="url">URL of the website used to check for connection (Default: null, url selected in code depending on country)</param>
        /// <returns>If true, there's internet connection, otherwise not</returns>
        public static Task<bool> CheckForInternetConnection(int timeoutMs = 5000, string url = null)
        {
            try
            {
                url ??= CultureInfo.InstalledUICulture switch
                {
                    { Name: var n } when n.StartsWith("fa") => // Iran
                        "http://www.aparat.com",
                    { Name: var n } when n.StartsWith("zh") => // China
                        "http://www.baidu.com",
                    _ =>
                        "http://www.gstatic.com/generate_204",
                };

                var request = new HttpClient
                {
                    Timeout = TimeSpan.FromMilliseconds(timeoutMs)
                };
                using var response = request.GetAsync(url);
                response.Wait();
                return Task.FromResult(true);
            }
            catch
            {
                return Task.FromResult(false);
            }
        }

        /// <summary>
        /// Get data about the mod from a downloadLink<br/>
        /// Currently Supported: Thunderstore
        /// </summary>
        /// <param name="downloadLink">Download Link, possibly included in the MelonInfoAttribute</param>
        /// <returns>If found, returns a ModData object which includes the latest version of the mod online, the download link and if the file is a ZIP file or not</returns>
        internal Task<ModData> GetModData(string downloadLink)
        {
            if (downloadLink == null)
            {
                LoggerInstance.Msg("No download link provided with mod, unable to fetch necessary information");
                return Task.FromResult<ModData>(null);
            }

            #region Thunderstore

            Regex ThunderstoreFind_Regex = new(@"https?:\/\/(?:.+\.)?thunderstore\.io|http?:\/\/(?:.+\.)?thunderstore\.io");

            if (ThunderstoreFind_Regex.Match(downloadLink).Success)
            {
                LoggerInstance.Msg("Thunderstore detected");
                string[] split = downloadLink.Split('/');
                string packageName;
                string namespaceName;
                if (downloadLink.EndsWith("/"))
                {
                    packageName = split[^2];
                    namespaceName = split[^3];
                }
                else
                {
                    packageName = split[^1];
                    namespaceName = split[^2];
                }

                LoggerInstance.Msg($"Found {namespaceName}-{packageName}");

                HttpClient request = new();
                Task<HttpResponseMessage> response = request.GetAsync($"https://thunderstore.io/api/experimental/package/{namespaceName}/{packageName}/");
                response.Wait();
                if (response.Result.IsSuccessStatusCode)
                {
                    Task<string> body = response.Result.Content.ReadAsStringAsync();
                    body.Wait();
                    if (body.Result != null)
                    {
                        Dictionary<string, JsonElement> data;
                        try
                        {
                            data = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(body.Result);
                        }
                        catch (Exception ex)
                        {
                            LoggerInstance.Error($"An unexpected error occurred while deserializing response\n{ex.Message}\n{ex.StackTrace}");
                            return Task.FromResult<ModData>(null);
                        }
                        Dictionary<string, JsonElement> latest = null;
                        try
                        {
                            JsonElement d = data["latest"];
                            var deserialize = d.Deserialize<Dictionary<string, JsonElement>>();
                            latest = deserialize;
                        }
                        catch (Exception ex)
                        {
                            LoggerInstance.Error($"Latest not found\n{ex.Message}\n{ex.StackTrace}");
                        }
                        LoggerInstance.Msg(new Uri(latest["download_url"].GetString()));
                        return Task.FromResult(new ModData()
                        {
                            LatestVersion = ModVersion.GetFromString(latest["version_number"].GetString()),
                            DownloadFileURI = new Uri(latest["download_url"].GetString()),
                        });
                    }
                    else
                    {
                        LoggerInstance.Error("Thunderstore API returned no body, unable to fetch package information");
                        return Task.FromResult<ModData>(null);
                    }
                }
                else
                {
                    LoggerInstance.Error
                        ($"Failed to fetch package information from Thunderstore, returned {response.Result.StatusCode} with following message:\n{response.Result.ReasonPhrase}");
                    return Task.FromResult<ModData>(null);
                }

                #endregion Thunderstore
            }
            return Task.FromResult<ModData>(null);
        }

        /// <summary>
        /// Check if Assembly is a MelonMod, a MelonPlugin or something else using Mono.Cecil
        /// </summary>
        /// <param name="assembly">Path of the file to check</param>
        /// <returns>A FileType, either MelonMod, MelonPlugin or Other</returns>
        internal static FileType GetFileType(Assembly assembly)
        {
#nullable enable
            MelonInfoAttribute? infoAttribute = assembly.GetCustomAttribute<MelonInfoAttribute>();

            if (infoAttribute != null)
            {
                return infoAttribute.SystemType == typeof(MelonMod) ? FileType.MelonMod : FileType.MelonPlugin;
            }
#nullable disable

            return FileType.Other;
        }

        /// <summary>
        /// Check if Assembly is a MelonMod, a MelonPlugin or something else
        /// </summary>
        /// <param name="assembly">Assembly of the file to check</param>
        /// <returns>A FileType, either MelonMod, MelonPlugin or Other</returns>
        internal static FileType GetFileType(string path)
        {
#nullable enable
            MelonInfoAttribute? infoAttribute = GetMelonInfo(path);

            if (infoAttribute != null)
            {
                return infoAttribute.SystemType == typeof(MelonMod) ? FileType.MelonMod : infoAttribute.SystemType == typeof(MelonPlugin) ? FileType.MelonPlugin : FileType.Other;
            }
#nullable disable

            return FileType.Other;
        }

#nullable enable

        /// <summary>
        ///
        /// </summary>
        public enum MelonInfoValue
        {
            Type = 0,
            Name = 1,
            Version = 2,
            Author = 3,
            DownloadLink = 4
        }

        internal static T? Get<T>(CustomAttribute customAttribute, int index)
        {
            if (customAttribute.ConstructorArguments.Count <= 0) return default;
            return (T)customAttribute.ConstructorArguments[index].Value;
        }

        /// <summary>
        /// Retrieve information from the MelonInfoAttribute in an Assembly
        /// </summary>
        /// <param name="assembly">Assembly to get the attribute from</param>
        /// <returns>If present, returns a MelonInfoAttribute</returns>
        internal static MelonInfoAttribute? GetMelonInfo(Assembly assembly)
#nullable disable
        {
            var melonInfo = assembly.GetCustomAttribute<MelonInfoAttribute>();
            if (melonInfo != null)
                return melonInfo;
            return null;
        }

        /// <summary>
        /// Retrieve information from the MelonInfoAttribute in an Assembly using Mono.Cecil
        /// </summary>
        /// <param name="path">Path to the assembly</param>
        /// <returns>If present, returns a MelonInfoAttribute</returns>
        internal static MelonInfoAttribute? GetMelonInfo(string path)
        {
            AssemblyDefinition assembly = AssemblyDefinition.ReadAssembly(path);
            foreach (var attr in assembly.CustomAttributes)
            {
                if (attr.AttributeType.Name == nameof(MelonInfoAttribute))
                {
                    var _type = Get<TypeDefinition>(attr, (int)MelonInfoValue.Type);
                    Type type = _type.BaseType.Name == "MelonMod" ? typeof(MelonMod) : _type.BaseType.Name == "MelonPlugin" ? typeof(MelonPlugin) : null;
                    string Name = Get<string>(attr, (int)MelonInfoValue.Name);
                    string Author = Get<string>(attr, (int)MelonInfoValue.Author);
                    string Version = Get<string>(attr, (int)MelonInfoValue.Version);
                    string DownloadLink = Get<string>(attr, (int)MelonInfoValue.DownloadLink);

                    assembly.Dispose();

                    return new MelonInfoAttribute(type: type, name: Name, version: Version, author: Author, downloadLink: DownloadLink);
                }
            }
            return null;
        }

        internal static MelonPriorityAttribute? GetMelonPriority(string path)
        {
            AssemblyDefinition assembly = AssemblyDefinition.ReadAssembly(path);
            foreach (var attr in assembly.CustomAttributes)
            {
                if (attr.AttributeType.Name == nameof(MelonPriorityAttribute))
                {
                    int priority = Get<int>(attr, 0);
                    return new MelonPriorityAttribute(priority);
                }
            }
            return null;
        }

        internal void CheckDirectory(string directory)
        {
            Dictionary<string, int> priority = [];

            List<string> files = [.. Directory.GetFiles(directory, "*.dll")];

            List<string> ignore = GetPreferenceValue<List<string>>(entry_ignore);
            List<string> topPriority = GetPreferenceValue<List<string>>(entry_priority);
            bool enabled = GetPreferenceValue<bool>(entry_enabled);

            if (!enabled)
            {
                LoggerInstance.Msg("Plugin disabled in preferences, aborting..");
                return;
            }

            string[] fileNameIgnore = [];
            AssemblyLoadContext priorityContext = new("MelonAutoUpdater_PriorityCheck", true);
            files.ForEach(async x =>
            {
                if (ignore != null && ignore.Count > 0)
                {
                    string fileName = Path.GetFileNameWithoutExtension(x);
                    if (ignore.Contains(fileName))
                    {
                        LoggerInstance.Msg($"{fileName} is in ignore list, removing from update list");
                        fileNameIgnore.Append(fileName);
                        return;
                    }
                }
                MelonPriorityAttribute priorityAttribute = GetMelonPriority(x);
                priority.Add(x, priorityAttribute != null ? priorityAttribute.Priority : 0);
                LoggerInstance.Msg("Adding priority in " + Path.GetFileName(x));
            });
            files.RemoveAll(x => fileNameIgnore.Contains(x));

            files.Sort(delegate (string x, string y)
            {
                if (x == null && y == null) return 0;
                else if (x == null) return -1;
                else if (y == null) return 1;
                else
                {
                    var xFileName = Path.GetFileNameWithoutExtension(x);
                    var yFileName = Path.GetFileNameWithoutExtension(y);
                    if (topPriority.Contains(xFileName)) return -1;
                    if (topPriority.Contains(yFileName)) return 1;

                    if (!priority.ContainsKey(x)) return -1;
                    if (!priority.ContainsKey(y)) return 1;
                    var xPriority = priority[x];
                    var yPriority = priority[y];
                    return xPriority.CompareTo(yPriority);
                }
            });

            LoggerInstance.Msg("\x1b[34;1m-----------\x1b[0m");
            foreach (string path in files)
            {
                string fileName = Path.GetFileName(path);
                LoggerInstance.Msg($"Checking \x1b[31m{fileName}\x1b[0m");
                FileType fileType = GetFileType(path);
                if (fileType != FileType.Other)
                {
                    LoggerInstance.Msg("File Type");
                    var melonAssemblyInfo = GetMelonInfo(path);
                    string assemblyName = (string)melonAssemblyInfo.Name.Clone();
                    if (melonAssemblyInfo != null)
                    {
                        var data = GetModData(melonAssemblyInfo.DownloadLink);
                        data.Wait();
                        if (data.Result != null)
                        {
                            ModVersion currentVersion = ModVersion.GetFromString(melonAssemblyInfo.Version);
                            if (currentVersion != null && data.Result.LatestVersion != null)
                            {
                                bool? needsUpdate = ModVersion.CompareVersions(data.Result.LatestVersion, currentVersion);
                                if (needsUpdate != null && needsUpdate == true)
                                {
                                    LoggerInstance.Msg($"A new version \x1b[32mv{data.Result.LatestVersion}\x1b[0m is available, meanwhile the current version is \u001b[32mv{currentVersion}\u001b[0m, updating");
                                    LoggerInstance.Msg("Downloading file");
                                    using var httpClient = new HttpClient();
                                    using var response = httpClient.GetAsync(data.Result.DownloadFileURI, HttpCompletionOption.ResponseHeadersRead);
                                    response.Wait();
                                    FileStream downloadedFile = null;
                                    string pathToSave = "";
                                    try
                                    {
                                        response.Result.EnsureSuccessStatusCode();
                                        string _contentType = response.Result.Content.Headers.ContentType.MediaType;
                                        if (_contentType != null)
                                        {
                                            pathToSave = _contentType switch
                                            {
                                                "application/zip" => Path.Combine(tempFilesPath, $"{melonAssemblyInfo.Name.Replace(" ", "")}.zip"),
                                                "application/x-msdownload" => Path.Combine(tempFilesPath, $"{melonAssemblyInfo.Name.Replace(" ", "")}.dll"),
                                                _ => Path.Combine(tempFilesPath, $"{melonAssemblyInfo.Name.Replace(" ", "")}.temp"),
                                            };
                                        }
                                        LoggerInstance.Msg(_contentType);
                                        LoggerInstance.Msg(pathToSave != null ? pathToSave : "No Path");
                                        using var ms = response.Result.Content.ReadAsStreamAsync();
                                        ms.Wait();
                                        using var fs = File.Create(pathToSave);
                                        var copyTask = ms.Result.CopyToAsync(fs);
                                        copyTask.Wait();
                                        fs.Flush();
                                        downloadedFile = fs;
                                        ms.Result.Close();
                                        LoggerInstance.Msg($"Successfully downloaded the latest version of \x1b[32m{melonAssemblyInfo.Name}\x1b[0m");
                                    }
                                    catch (Exception ex)
                                    {
                                        LoggerInstance.Error($"Failed to download file through link\n{ex.Message}\n{ex.StackTrace}");
                                        downloadedFile.Dispose();
                                        downloadedFile = null;
                                    }

                                    if (downloadedFile != null)
                                    {
                                        bool threwError = false;
                                        int failed = 0;
                                        int success = 0;
                                        if (Path.GetExtension(pathToSave) == ".zip")
                                        {
                                            LoggerInstance.Msg("File is a ZIP file, extracting files...");
                                            string extractPath = Path.Combine(tempFilesPath, melonAssemblyInfo.Name.Replace(" ", "-"));
                                            try
                                            {
                                                ZipFile.ExtractToDirectory(pathToSave, extractPath);
                                                LoggerInstance.Msg("Successfully extracted files!");
                                                LoggerInstance.Msg("Installing content to MelonLoader");
                                                var disposeTask = downloadedFile.DisposeAsync();
                                                disposeTask.AsTask().Wait();
                                            }
                                            catch (Exception ex)
                                            {
                                                threwError = true;
                                                LoggerInstance.Error($"An exception occurred while extracting files from a ZIP file\n{ex.Message}\n{ex.StackTrace}");
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
                                                    foreach (var fPath in Directory.GetFiles(extPath))
                                                    {
                                                        FileType _fileType = GetFileType(fPath);
                                                        if (_fileType == FileType.MelonMod)
                                                        {
                                                            try
                                                            {
                                                                LoggerInstance.Msg("Installing mod file " + Path.GetFileName(fPath));
                                                                string _path = Path.Combine(MelonEnvironment.ModsDirectory, Path.GetFileName(fPath));
                                                                if (!File.Exists(_path)) File.Move(fPath, _path);
                                                                else File.Replace(fPath, _path, Path.Combine(backupFolderPath, $"{Path.GetFileName(_path)}-{DateTimeOffset.Now.ToUnixTimeSeconds()}.dll"));
                                                                success += 1;
                                                                LoggerInstance.Msg("Successfully installed mod file " + Path.GetFileName(fPath));
                                                            }
                                                            catch (Exception ex)
                                                            {
                                                                LoggerInstance.Error($"An unexpected error occurred while installing content\n{ex.Message}\n{ex.StackTrace}");
                                                                failed += 1;
                                                            }
                                                        }
                                                        else if (_fileType == FileType.MelonPlugin)
                                                        {
                                                            try
                                                            {
                                                                LoggerInstance.Msg("Installing plugin file " + Path.GetFileName(fPath));
                                                                string pluginPath = Path.Combine(MelonEnvironment.PluginsDirectory, fileName);
                                                                string _path = Path.Combine(MelonEnvironment.ModsDirectory, Path.GetFileName(fPath));
                                                                if (!File.Exists(_path)) File.Move(fPath, _path);
                                                                else File.Replace(fPath, _path, Path.Combine(backupFolderPath, $"{Path.GetFileName(_path)}-{DateTimeOffset.Now.ToUnixTimeSeconds()}.dll"));
                                                                var melonAssembly = MelonAssembly.LoadMelonAssembly(pluginPath);
                                                                LoggerInstance.Warning("WARNING: The plugin might not work properly or crash due to the fact it was loaded at a later time");
                                                                LoggerInstance.Msg("Successfully installed plugin file " + Path.GetFileName(fPath));
                                                                success += 1;
                                                            }
                                                            catch (Exception ex)
                                                            {
                                                                LoggerInstance.Error($"An unexpected error occurred while installing content\n{ex.Message}\n{ex.StackTrace}");
                                                                failed += 1;
                                                            }
                                                        }
                                                        else
                                                        {
                                                            if (Path.GetDirectoryName(extPath) == "UserLibs")
                                                            {
                                                                try
                                                                {
                                                                    LoggerInstance.Msg("Installing new library " + Path.GetFileName(fPath));
                                                                    string _path = Path.Combine(MelonEnvironment.UserLibsDirectory, Path.GetFileName(fPath));
                                                                    if (!File.Exists(_path)) File.Move(fPath, _path);
                                                                    else File.Replace(fPath, _path, Path.Combine(backupFolderPath, $"{Path.GetFileName(_path)}-{DateTimeOffset.Now.ToUnixTimeSeconds()}.dll"));
                                                                }
                                                                catch (Exception ex)
                                                                {
                                                                    LoggerInstance.Msg($"An unexpected error occurred while installing library\n{ex.Message}\n{ex.StackTrace}");
                                                                }
                                                            }
                                                            else
                                                            {
                                                                LoggerInstance.Msg($"Not extracting {Path.GetFileName(extPath)}, because it does not have the Melon Info Attribute");
                                                            }
                                                        }
                                                    }
                                                }
                                                else if (Path.GetExtension(extPath) == ".dll")
                                                {
                                                    FileType _fileType = GetFileType(extPath);
                                                    if (_fileType == FileType.MelonMod)
                                                    {
                                                        try
                                                        {
                                                            LoggerInstance.Msg("Installing mod file " + Path.GetFileName(extPath));
                                                            string _path = Path.Combine(MelonEnvironment.ModsDirectory, Path.GetFileName(extPath));
                                                            if (!File.Exists(_path)) File.Move(extPath, _path);
                                                            else File.Replace(extPath, _path, Path.Combine(backupFolderPath, $"{Path.GetFileName(path)}-{DateTimeOffset.Now.ToUnixTimeSeconds()}.dll"));
                                                            success += 1;
                                                            LoggerInstance.Msg("Successfully installed mod file " + Path.GetFileName(extPath));
                                                        }
                                                        catch (Exception ex)
                                                        {
                                                            LoggerInstance.Error($"An unexpected error occurred while installing content\n{ex.Message}\n{ex.StackTrace}");
                                                            threwError = true;
                                                            failed += 1;
                                                        }
                                                    }
                                                    else if (_fileType == FileType.MelonPlugin)
                                                    {
                                                        try
                                                        {
                                                            LoggerInstance.Msg("Installing plugin file " + Path.GetFileName(extPath));
                                                            string pluginPath = Path.Combine(MelonEnvironment.PluginsDirectory, fileName);
                                                            string _path = Path.Combine(MelonEnvironment.ModsDirectory, Path.GetFileName(extPath));
                                                            if (!File.Exists(_path)) File.Move(extPath, _path);
                                                            else File.Replace(extPath, _path, Path.Combine(backupFolderPath, $"{Path.GetFileName(path)}-{DateTimeOffset.Now.ToUnixTimeSeconds()}.dll"));
                                                            var melonAssembly = MelonAssembly.LoadMelonAssembly(pluginPath);
                                                            LoggerInstance.Warning("WARNING: The plugin might not work properly or crash due to the fact it was loaded at a later time");
                                                            LoggerInstance.Msg("Successfully installed plugin file " + Path.GetFileName(extPath));
                                                            success += 1;
                                                        }
                                                        catch (Exception ex)
                                                        {
                                                            LoggerInstance.Error($"An unexpected error occurred while installing content\n{ex.Message}\n{ex.StackTrace}");
                                                            threwError = true;
                                                            failed += 1;
                                                        }
                                                    }
                                                    else
                                                    {
                                                        LoggerInstance.Msg($"Not extracting {Path.GetFileName(extPath)}, because it does not have the Melon Info Attribute");
                                                    }
                                                }
                                            }
                                            Directory.Delete(extractPath, true);
                                            File.Delete(pathToSave);
                                        }
                                        LoggerInstance.Msg(
                                            !threwError
                                            ? $"Updated {assemblyName} from \x1b[32;1mv{currentVersion} --> v{data.Result.LatestVersion}\x1b[0m, {success}/{success + failed} installed successfully"
                                            : $"Failed to update {assemblyName}"
                                            );
                                    }
                                    else
                                    {
                                        LoggerInstance.Error("Downloaded file is empty, unable to update mod");
                                    }
                                }
                                else
                                {
                                    LoggerInstance.Msg("\x1b[32mVersion is up-to-date!\x1b[0m");
                                }
                            }
                        }
                    }
                }
                else
                {
                    LoggerInstance.Warning($"{fileName} does not seem to be a MelonLoader Mod");
                }
                LoggerInstance.Msg("\x1b[34;1m-----------\x1b[0m");
            }
        }

        // Note to self: Don't use async
        public override void OnPreInitialization()
        {
            LoggerInstance.Msg("Creating folders in UserData");
            DirectoryInfo mainDir = Directory.CreateDirectory(Path.Combine(MelonEnvironment.UserDataDirectory, "MelonAutoUpdater"));
            DirectoryInfo tempDir = mainDir.CreateSubdirectory("TemporaryFiles");
            DirectoryInfo backupDir = mainDir.CreateSubdirectory("Backups");

            tempFilesPath = tempDir.FullName;
            mainFolderPath = mainDir.FullName;
            backupFolderPath = backupDir.FullName;

            LoggerInstance.Msg("Clearing possibly left temporary files");

            List<string> tempPaths = [.. Directory.GetFiles(tempFilesPath)];
            tempPaths.AddRange(Directory.GetDirectories(tempFilesPath));

            foreach (FileInfo file in tempDir.GetFiles()) file.Delete();
            foreach (DirectoryInfo subDirectory in tempDir.GetDirectories()) subDirectory.Delete(true);

            LoggerInstance.Msg("Setup Melon Preferences");

            SetupPreferences();
        }

        public override void OnPreModsLoaded()
        {
            Task<bool> internetCheck = CheckForInternetConnection();
            internetCheck.Wait();
            if (!internetCheck.Result)
            {
                LoggerInstance.Msg("It seems like there is no internet, aborting..");
                return;
            }
            LoggerInstance.Msg("Checking mods...");
            CheckDirectory(MelonEnvironment.ModsDirectory);
            LoggerInstance.Msg("Done checking mods");
        }

        public override void OnApplicationQuit()
        {
        }
    }
}