using MelonLoader;
using MelonLoader.Utils;
using System.Globalization;
using System.IO.Compression;
using System.Net;
using System.Reflection;
using System.Runtime.Loader;
using System.Text.Json;

[assembly: MelonInfo(typeof(MelonAutoUpdater.Core), "MelonModUpdater", "1.0.0", "HAHOOS", null)]
[assembly: MelonPriority(-100)]

namespace MelonAutoUpdater
{
    internal class Core : MelonPlugin
    {
        /// <summary>
        /// Path of the Temporary Files folder in UserData
        /// </summary>
        internal string tempFilesPath = "";

        internal string mainFolderPath = "";

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
        public static async Task<bool> CheckForInternetConnection(int timeoutMs = 5000, string url = null)
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
                using var response = await request.GetAsync(url);
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Get data about the mod from a downloadLink
        /// </summary>
        /// <param name="downloadLink">Download Link, possibly included in the MelonInfoAttribute</param>
        /// <returns>If found, returns a ModData object which includes the latest version of the mod online, the download link and if the file is a ZIP file or not</returns>
        internal async Task<ModData> GetModData(string downloadLink)
        {
            if (downloadLink == null)
            {
                LoggerInstance.Msg("No download link provided with mod, unable to fetch necessary information");
                return null;
            }

            #region Thunderstore

            if (downloadLink.StartsWith("https://thunderstore.io"))
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

                HttpClient request = new();
                HttpResponseMessage response = await request.GetAsync($"https://thunderstore.io/api/experimental/package/{namespaceName}/{packageName}/");
                if (response.IsSuccessStatusCode)
                {
                    string body = await response.Content.ReadAsStringAsync();
                    if (body != null)
                    {
                        Dictionary<string, JsonElement> data;
                        try
                        {
                            data = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(body);
                        }
                        catch (Exception ex)
                        {
                            LoggerInstance.Error($"An unexpected error occurred while deserializing response\n{ex.Message}\n{ex.StackTrace}");
                            return null;
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
                        return new ModData()
                        {
                            LatestVersion = ModVersion.GetFromString(latest["version_number"].GetString()),
                            DownloadFileURI = new Uri(latest["download_url"].GetString()),
                            IsZIP = true
                        };
                    }
                    else
                    {
                        LoggerInstance.Error("Thunderstore API returned no body, unable to fetch package information");
                        return null;
                    }
                }
                else
                {
                    LoggerInstance.Error
                        ($"Failed to fetch package information from Thunderstore, returned {response.StatusCode} with following message:\n{response.ReasonPhrase}");
                    return null;
                }

                #endregion Thunderstore
            }
            return null;
        }

        /// <summary>
        /// Check if Assembly is a MelonMod, a MelonPlugin or something else
        /// </summary>
        /// <param name="assembly">Assembly of the file to check</param>
        /// <returns>A FileType, either MelonMod, MelonPlugin or Other</returns>
        internal static FileType GetFileTypeFromAssembly(Assembly assembly)
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

#nullable enable

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

        public override async void OnPreInitialization()
        {
            LoggerInstance.Msg("Creating folders in UserData");
            DirectoryInfo mainDir = Directory.CreateDirectory(Path.Combine(MelonEnvironment.UserDataDirectory, "MelonAutoUpdater"));
            DirectoryInfo tempDir = mainDir.CreateSubdirectory("TemporaryFiles");

            tempFilesPath = tempDir.FullName;
            mainFolderPath = mainDir.FullName;

            LoggerInstance.Msg("Setup Melon Preferences");

            SetupPreferences();

            bool internetCheck = await CheckForInternetConnection();
            if (!internetCheck)
            {
                LoggerInstance.Msg("It seems like there is no internet, aborting..");
                return;
            }

            Dictionary<string, int> priority = [];

            string modDirectory = MelonEnvironment.ModsDirectory;

            List<string> files = [.. Directory.GetFiles(modDirectory, "*.dll")];

            List<string> ignore = GetPreferenceValue<List<string>>(entry_ignore);
            List<string> topPriority = GetPreferenceValue<List<string>>(entry_priority);
            bool enabled = GetPreferenceValue<bool>(entry_enabled);

            if (!enabled)
            {
                LoggerInstance.Msg("Plugin disabled in preferences, aborting..");
                return;
            }

            string[] fileNameIgnore = [];
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
                Assembly assembly = System.Reflection.Assembly.Load(File.ReadAllBytes(x));
                if (assembly != null)
                {
                    MelonPriorityAttribute priorityAttribute = assembly.GetCustomAttribute<MelonPriorityAttribute>();

                    priority.Add(x, priorityAttribute != null ? priorityAttribute.Priority : 0);
                    priorityAttribute = null;
                    LoggerInstance.Msg("Adding priority in " + Path.GetFileName(x));
                    await Task.Delay(1000);
                }
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
                Assembly file = null;
                try
                {
                    file = System.Reflection.Assembly.Load(File.ReadAllBytes(path));
                }
                catch (Exception ex)
                {
                    LoggerInstance.Error($"Unable to load assembly\n{ex.Message}\n{ex.StackTrace}");
                    continue;
                }
                if (file != null)
                {
                    FileType fileType = GetFileTypeFromAssembly(file);
                    if (fileType != FileType.Other)
                    {
                        LoggerInstance.Msg("File Type");
                        var melonAssemblyInfo = GetMelonInfo(file);
                        if (melonAssemblyInfo != null)
                        {
                            var data = await GetModData(melonAssemblyInfo.DownloadLink);
                            if (data != null)
                            {
                                ModVersion currentVersion = ModVersion.GetFromString(melonAssemblyInfo.Version);
                                if (currentVersion != null && data.LatestVersion != null)
                                {
                                    bool? needsUpdate = ModVersion.CompareVersions(data.LatestVersion, currentVersion);
                                    if (needsUpdate != null && needsUpdate == true)
                                    {
                                        LoggerInstance.Msg($"A new version \x1b[32mv{data.LatestVersion}\x1b[0m is available, meanwhile the current version is \u001b[32m{currentVersion}\u001b[0m, updating");
                                        LoggerInstance.Msg("Downloading file");
                                        using var httpClient = new HttpClient();
                                        using var response = await httpClient.GetAsync(data.DownloadFileURI, HttpCompletionOption.ResponseHeadersRead);

                                        FileStream downloadedFile = null;
                                        var filePath = Path.Combine(tempFilesPath, data.IsZIP ? $"{fileName}.zip" : $"{fileName}.dll");

                                        try
                                        {
                                            response.EnsureSuccessStatusCode();
                                            using var ms = await response.Content.ReadAsStreamAsync();
                                            using var fs = File.Create(filePath);
                                            await ms.CopyToAsync(fs);
                                            fs.Flush();
                                            downloadedFile = fs;
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
                                            if (data.IsZIP)
                                            {
                                                LoggerInstance.Msg("File is a ZIP file, extracting files...");
                                                try
                                                {
                                                    string extractPath = Path.Combine(tempFilesPath, melonAssemblyInfo.Name.Replace(" ", "-"));
                                                    ZipFile.ExtractToDirectory(filePath, extractPath);
                                                    LoggerInstance.Msg("Successfully extracted files!");
                                                    LoggerInstance.Msg("Installing content to MelonLoader");
                                                    await downloadedFile.DisposeAsync();
                                                    var extractedFiles = Directory.GetFiles(extractPath).ToList();
                                                    Directory.GetDirectories(extractPath).ToList().ForEach((x) => extractedFiles.Add(x));
                                                    foreach (string extPath in extractedFiles)
                                                    {
                                                        if (!Path.HasExtension(extPath))
                                                        {
                                                            if (Path.GetFileName(extPath) == "Mods")
                                                            {
                                                                foreach (var fPath in Directory.GetFiles(extPath))
                                                                {
                                                                    var fFile = System.Reflection.Assembly.Load(File.ReadAllBytes(fPath));

                                                                    FileType _fileType = GetFileTypeFromAssembly(fFile);
                                                                    if (_fileType == FileType.MelonMod)
                                                                    {
                                                                        try
                                                                        {
                                                                            File.Delete(path);
                                                                            File.Move(fPath, path);
                                                                        }
                                                                        catch (Exception ex)
                                                                        {
                                                                            LoggerInstance.Error($"An unexpected error occurred while replacing old version with new version\n{ex.Message}\n{ex.StackTrace}");
                                                                            Directory.Delete(extractPath, true);
                                                                            File.Delete(filePath);
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
                                                            Assembly extFile = System.Reflection.Assembly.LoadFile(extPath);
                                                            FileType _fileType = GetFileTypeFromAssembly(extFile);
                                                            if (_fileType != FileType.Other)
                                                            {
                                                                LoggerInstance.Msg("Moving file " + Path.GetFileName(extPath));
                                                                // Right now it only predicts its a mod
                                                                File.Move(extPath, Path.Combine(MelonEnvironment.ModsDirectory, fileName));
                                                            }
                                                            else
                                                            {
                                                                LoggerInstance.Msg($"Not extracting {Path.GetFileName(extPath)}, because it does not have the Melon Info Attribute");
                                                            }
                                                        }
                                                    }
                                                    Directory.Delete(extractPath, true);
                                                    File.Delete(filePath);
                                                }
                                                catch (Exception ex)
                                                {
                                                    LoggerInstance.Error($"An exception occurred while extracting files from a ZIP file\n{ex.Message}\n{ex.StackTrace}");
                                                    File.Delete(filePath);
                                                    foreach (var path_ in Directory.GetFiles(tempFilesPath))
                                                    {
                                                        File.Delete(path_);
                                                    }
                                                }
                                            }
                                            LoggerInstance.Msg($"Successfully updated {melonAssemblyInfo.Name} from \x1b[32;1mv{currentVersion} --> v{data.LatestVersion}\x1b[0m");
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
                        else
                        {
                            LoggerInstance.Warning($"{fileName} does not seem to be a MelonLoader Mod");
                        }
                    }
                    await Task.Delay(250);
                }
                LoggerInstance.Msg("\x1b[34;1m-----------\x1b[0m");
            }
        }
    }
}