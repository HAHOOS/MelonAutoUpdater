using MelonLoader;
using MelonLoader.Utils;
using Mono.Cecil;
using System.Globalization;
using System.IO.Compression;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Loader;
using System.Text.Json;
using System.Text.RegularExpressions;

[assembly: MelonInfo(typeof(MelonAutoUpdater.Core), "MelonModUpdater", "1.0.0", "HAHOOS", null)]
[assembly: MelonPriority(-10000)]
[assembly: MelonID("272b5cba-0b4c-4d0a-b6b6-ba2bfbb7c716")]
[assembly: VerifyLoaderVersion(0, 6, 0, true)]
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

        /// <summary>
        /// This is used to prevent from rate-limiting the API
        /// </summary>
        private bool disableGithubAPI = false;

        /// <summary>
        /// The time (in Unix time seconds) when the rate limit will dissapear
        /// </summary>
        private long githubResetDate;

        #region Melon Preferences

        /// <summary>
        /// Main Category in Preferences
        /// </summary>
        public MelonPreferences_Category Category { get; private set; }

        /// <summary>
        /// An entry
        /// </summary>
        public MelonPreferences_Entry Entry_ignore { get; private set; }

        public MelonPreferences_Entry Entry_priority { get; private set; }
        public MelonPreferences_Entry Entry_enabled { get; private set; }
        public MelonPreferences_Entry Entry_bruteCheck { get; private set; }

        /// <summary>
        /// Setup Preferences
        /// </summary>
        private void SetupPreferences()
        {
            Category = MelonPreferences.CreateCategory("MelonAutoUpdater", "Melon Auto Updater");
            Category.SetFilePath(Path.Combine(mainFolderPath, "config.cfg"));

            Entry_ignore = Category.CreateEntry<List<string>>("IgnoreList", [], "Ignore List",
                description: "List of all names of Mods & Plugins that will be ignored when checking for updates");
            Entry_priority = Category.CreateEntry<List<string>>("PriorityList", [], "Priority List",
                description: "List of all names of Mods & Plugins that will be updated first");
            Entry_enabled = Category.CreateEntry<bool>("Enabled", true, "Enabled",
                description: "If true, Mods & Plugins will update on every start");
            Entry_bruteCheck = Category.CreateEntry<bool>("BruteCheck", false, "Brute Check",
                description: "If true, when there's no download link provided with mod/plugin, it will check every possible platform providing the Name & Author\nThis is not recommended as it will very easily result in this plugin being rate-limited");

            LoggerInstance.Msg("Successfully set up Melon Preferences!");
        }

        /// <summary>
        /// Get value of an entry in Melon Preferences
        /// </summary>
        /// <typeparam name="T">A type that will be returned as value of entry</typeparam>
        /// <param name="entry">The Melon Preferences Entry to retrieve value from</param>
        /// <returns>Value of entry with inputted type</returns>

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
                    return default;
                }
            }
            return default;
        }

        #endregion Melon Preferences

        // If you are wondering, this is from StackOverflow, although a bit edited, I'm just a bit lazy
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
        /// Currently Supported: Thunderstore, Github
        /// </summary>
        /// <param name="downloadLink">Download Link, possibly included in the MelonInfoAttribute</param>
        /// <returns>If found, returns a ModData object which includes the latest version of the mod online and the download link(s)</returns>
        internal Task<ModData> GetModData(string downloadLink)
        {
            if (string.IsNullOrWhiteSpace(downloadLink))
            {
                LoggerInstance.Msg("No download link provided with mod, unable to fetch necessary information");
                return Task.FromResult<ModData>(null);
            }

            Regex ThunderstoreFind_Regex = new(@"https?:\/\/(?:.+\.)?thunderstore\.io|http?:\/\/(?:.+\.)?thunderstore\.io");
            Regex githubRegex = new(@"(?=http:\/\/|https:\/\/)?github.com\/");

            #region Thunderstore

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
                            LoggerInstance.Error($"Latest could not be retrieved\n{ex.Message}\n{ex.StackTrace}");
                        }

                        request.Dispose();
                        response.Dispose();
                        body.Dispose();

                        return Task.FromResult(new ModData()
                        {
                            LatestVersion = ModVersion.GetFromString(latest["version_number"].GetString()),
                            DownloadFileURL = [latest["download_url"].GetString()],
                        });
                    }
                    else
                    {
                        LoggerInstance.Error("Thunderstore API returned no body, unable to fetch package information");

                        request.Dispose();
                        response.Dispose();
                        body.Dispose();

                        return Task.FromResult<ModData>(null);
                    }
                }
                else
                {
                    if (response.Result.StatusCode == System.Net.HttpStatusCode.NotFound)
                    {
                        LoggerInstance.Msg("Thunderstore API could not locate the mod/plugin");
                    }
                    else
                    {
                        LoggerInstance.Error
                            ($"Failed to fetch package information from Thunderstore, returned {response.Result.StatusCode} with following message:\n{response.Result.ReasonPhrase}");
                    }
                    request.Dispose();
                    response.Dispose();

                    return Task.FromResult<ModData>(null);
                }
            }

            #endregion Thunderstore

            #region Github;

            else if (githubRegex.Match(downloadLink).Success)
            {
                LoggerInstance.Msg("Github detected");

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

                var client = new HttpClient();
                client.DefaultRequestHeaders.Add("Accept", "application/vnd.github+json");
                if (disableGithubAPI && DateTimeOffset.UtcNow.ToUnixTimeSeconds() > githubResetDate) disableGithubAPI = false;
                if (!disableGithubAPI)
                {
                    var response = client.GetAsync($"https://api.github.com/repos/{namespaceName}/{packageName}/releases/latest", HttpCompletionOption.ResponseContentRead);
                    response.Wait();
                    if (response.Result.IsSuccessStatusCode)
                    {
                        int remaining = int.Parse(response.Result.Headers.GetValues("x-ratelimit-remaining").First());
                        long reset = long.Parse(response.Result.Headers.GetValues("x-ratelimit-reset").First());
                        if (remaining <= 1)
                        {
                            LoggerInstance.Warning("Due to rate limits nearly reached, any attempt to send an API call to Github during this session will be aborted");
                            githubResetDate = reset;
                            disableGithubAPI = true;
                        }
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
                            string version = data["tag_name"].GetString();
                            Dictionary<string, JsonElement> assets = null;
                            try
                            {
                                JsonElement d = data["assets"];
                                var deserialize = d.Deserialize<Dictionary<string, JsonElement>>();
                                assets = deserialize;
                            }
                            catch (Exception ex)
                            {
                                LoggerInstance.Error($"Assets could not be retrieved\n{ex.Message}\n{ex.StackTrace}");
                            }

                            List<string> downloadURLs = [];

                            foreach (var file in assets)
                            {
                                try
                                {
                                    JsonElement d = file.Value;
                                    var deserialize = d.Deserialize<Dictionary<string, JsonElement>>();
                                    downloadURLs.Add(deserialize["browser_download_url"].GetString());
                                }
                                catch (Exception ex)
                                {
                                    LoggerInstance.Error($"File could not be retrieved\n{ex.Message}\n{ex.StackTrace}");
                                }
                            }

                            client.Dispose();
                            response.Dispose();
                            body.Dispose();
                            return Task.FromResult(new ModData()
                            {
                                LatestVersion = ModVersion.GetFromString(version),
                                DownloadFileURL = downloadURLs,
                            });
                        }
                        else
                        {
                            LoggerInstance.Error("Github API returned no body, unable to fetch package information");

                            client.Dispose();
                            response.Dispose();
                            body.Dispose();

                            return Task.FromResult<ModData>(null);
                        }
                    }
                    else
                    {
                        int remaining = int.Parse(response.Result.Headers.GetValues("x-ratelimit-remaining").First());
                        int limit = int.Parse(response.Result.Headers.GetValues("x-ratelimit-limit").First());
                        long reset = long.Parse(response.Result.Headers.GetValues("x-ratelimit-reset").First());
                        if (remaining <= 0)
                        {
                            LoggerInstance.Error($"You've reached the rate limit of Github API ({limit}) and you will be able to use the Github API again at {DateTimeOffset.FromUnixTimeSeconds(reset).ToLocalTime():t}");
                            githubResetDate = reset;
                            disableGithubAPI = true;
                        }
                        else
                        {
                            if (response.Result.StatusCode == System.Net.HttpStatusCode.NotFound)
                            {
                                LoggerInstance.Warning("Github API could not find the mod/plugin");
                            }
                            else
                            {
                                LoggerInstance.Error
                                    ($"Failed to fetch package information from Github, returned {response.Result.StatusCode} with following message:\n{response.Result.ReasonPhrase}");
                            }
                        }
                        client.Dispose();
                        response.Dispose();

                        return Task.FromResult<ModData>(null);
                    }
                }
                else
                {
                    MelonLogger.Warning(
                        "Github API access is currently disabled and this check will be aborted, you should be good to use the API at " + DateTimeOffset.FromUnixTimeSeconds(githubResetDate).ToLocalTime().ToString("t"));
                }
            }

            #endregion Github;

            return Task.FromResult<ModData>(null);
        }

        /// <summary>
        /// Get data about the mod from a name and author<br/>
        /// <b>Might not comply with platform's ToS!</b><br/>
        /// Currently Supported: Thunderstore, Github
        /// </summary>
        /// <returns>If found, returns a ModData object which includes the latest version of the mod online and the download link(s)</returns>
        internal Task<ModData> GetModDataFromInfo(string name, string author)
        {
            if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(author))
            {
                LoggerInstance.Msg("Either author or name is empty, unable to fetch necessary information");
                return Task.FromResult<ModData>(null);
            }

            #region Thunderstore

            LoggerInstance.Msg("Checking Thunderstore");

            HttpClient request = new();
            Task<HttpResponseMessage> response = request.GetAsync($"https://thunderstore.io/api/experimental/package/{author}/{name}/");
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
                        LoggerInstance.Error($"Latest could not be retrieved\n{ex.Message}\n{ex.StackTrace}");
                    }

                    LoggerInstance.Msg("Thunderstore return");

                    request.Dispose();
                    response.Dispose();
                    body.Dispose();

                    return Task.FromResult(new ModData()
                    {
                        LatestVersion = ModVersion.GetFromString(latest["version_number"].GetString()),
                        DownloadFileURL = [latest["download_url"].GetString()],
                    });
                }
                else
                {
                    LoggerInstance.Error("Thunderstore API returned no body, unable to fetch package information");

                    request.Dispose();
                    response.Dispose();
                    body.Dispose();
                }
            }
            else
            {
                if (response.Result.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    LoggerInstance.Warning("Thunderstore API could not locate the mod/plugin");
                }
                else
                {
                    LoggerInstance.Error
                        ($"Failed to fetch package information from Thunderstore, returned {response.Result.StatusCode} with following message:\n{response.Result.ReasonPhrase}");
                }
                request.Dispose();
                response.Dispose();
            }

            #endregion Thunderstore

            #region Github;

            LoggerInstance.Msg("Checking Github");

            var _client = new HttpClient();
            _client.DefaultRequestHeaders.Add("Accept", "application/vnd.github+json");
            _client.DefaultRequestHeaders.Add("User-Agent", "MelonAutoUpdater");
            if (disableGithubAPI && DateTimeOffset.UtcNow.ToUnixTimeSeconds() > githubResetDate) disableGithubAPI = false;
            if (!disableGithubAPI)
            {
                var _response = _client.GetAsync($"https://api.github.com/repos/{author}/{name}/releases/latest", HttpCompletionOption.ResponseContentRead);
                _response.Wait();
                if (_response.Result.IsSuccessStatusCode)
                {
                    int remaining = int.Parse(_response.Result.Headers.GetValues("x-ratelimit-remaining").FirstOrDefault());
                    long reset = long.Parse(_response.Result.Headers.GetValues("x-ratelimit-reset").FirstOrDefault());
                    if (remaining <= 1)
                    {
                        LoggerInstance.Warning("Due to rate limits nearly reached, any attempt to send an API call to Github during this session will be aborted");
                        githubResetDate = reset;
                        disableGithubAPI = true;
                    }
                    Task<string> body = _response.Result.Content.ReadAsStringAsync();
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
                        string version = data["tag_name"].GetString();
                        Dictionary<string, JsonElement> assets = null;
                        try
                        {
                            JsonElement d = data["assets"];
                            var deserialize = d.Deserialize<Dictionary<string, JsonElement>>();
                            assets = deserialize;
                        }
                        catch (Exception ex)
                        {
                            LoggerInstance.Error($"Assets could not be retrieved\n{ex.Message}\n{ex.StackTrace}");
                        }

                        List<string> downloadURLs = [];

                        foreach (var file in assets)
                        {
                            try
                            {
                                JsonElement d = file.Value;
                                var deserialize = d.Deserialize<Dictionary<string, JsonElement>>();
                                downloadURLs.Add(deserialize["browser_download_url"].GetString());
                            }
                            catch (Exception ex)
                            {
                                LoggerInstance.Error($"File could not be retrieved\n{ex.Message}\n{ex.StackTrace}");
                            }
                        }

                        _client.Dispose();
                        _response.Dispose();
                        body.Dispose();
                        return Task.FromResult(new ModData()
                        {
                            LatestVersion = ModVersion.GetFromString(version),
                            DownloadFileURL = downloadURLs,
                        });
                    }
                    else
                    {
                        LoggerInstance.Error("Github API returned no body, unable to fetch package information");

                        _client.Dispose();
                        _response.Dispose();
                        body.Dispose();
                    }
                }
                else
                {
                    int remaining = int.Parse(_response.Result.Headers.GetValues("x-ratelimit-remaining").First());
                    int limit = int.Parse(_response.Result.Headers.GetValues("x-ratelimit-limit").First());
                    long reset = long.Parse(_response.Result.Headers.GetValues("x-ratelimit-reset").First());
                    if (remaining <= 0)
                    {
                        LoggerInstance.Error($"You've reached the rate limit of Github API ({limit}) and you will be able to use the Github API again at {DateTimeOffset.FromUnixTimeSeconds(reset).ToLocalTime():t}");
                        disableGithubAPI = true;
                        githubResetDate = reset;
                    }
                    else
                    {
                        if (_response.Result.StatusCode == System.Net.HttpStatusCode.NotFound)
                        {
                            LoggerInstance.Warning("Github API could not find the mod/plugin");
                        }
                        else
                        {
                            LoggerInstance.Error
        ($"Failed to fetch package information from Github, returned {_response.Result.StatusCode} with following message:\n{_response.Result.ReasonPhrase}");
                        }
                    }

                    _client.Dispose();
                    _response.Dispose();
                }
            }
            else
            {
                MelonLogger.Warning(
                    "Github API access is currently disabled and this check will be aborted, you should be good to use the API at " + DateTimeOffset.FromUnixTimeSeconds(githubResetDate).ToLocalTime().ToString("t"));
            }

            #endregion Github;

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
        /// Check if a file on specified path is a MelonMod, a MelonPlugin or something else
        /// </summary>
        /// <param name="path">Path of the file to check</param>
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
        /// Retrieve information from the MelonInfoAttribute in an file using Mono.Cecil
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
                    var _type = Get<TypeDefinition>(attr, 0);
                    Type type = _type.BaseType.Name == "MelonMod" ? typeof(MelonMod) : _type.BaseType.Name == "MelonPlugin" ? typeof(MelonPlugin) : null;
                    string Name = Get<string>(attr, 1);
                    string Version = Get<string>(attr, 2);
                    string Author = Get<string>(attr, 3);
                    string DownloadLink = Get<string>(attr, 4);

                    assembly.Dispose();

                    return new MelonInfoAttribute(type: type, name: Name, version: Version, author: Author, downloadLink: DownloadLink);
                }
            }
            assembly.Dispose();
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

                    assembly.Dispose();

                    return new MelonPriorityAttribute(priority);
                }
            }
            assembly.Dispose();
            return null;
        }

        internal static VerifyLoaderVersionAttribute? GetLoaderVersionRequired(string path)
        {
            AssemblyDefinition assembly = AssemblyDefinition.ReadAssembly(path);
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

        internal void CheckDirectory(string directory, bool automatic = true)
        {
            Dictionary<string, int> priority = [];

            List<string> files = [.. Directory.GetFiles(directory, "*.dll")];

            List<string> ignore = GetPreferenceValue<List<string>>(Entry_ignore);
            List<string> topPriority = GetPreferenceValue<List<string>>(Entry_priority);
            bool enabled = GetPreferenceValue<bool>(Entry_enabled);

            if (!enabled)
            {
                LoggerInstance.Msg("Plugin disabled in preferences, aborting..");
                return;
            }

            List<string> fileNameIgnore = [];
            AssemblyLoadContext priorityContext = new("MelonAutoUpdater_PriorityCheck", true);
            files.ForEach(x =>
            {
                if (ignore != null && ignore.Count > 0)
                {
                    string fileName = Path.GetFileNameWithoutExtension(x);
                    if (ignore.Contains(fileName))
                    {
                        LoggerInstance.Msg($"{fileName} is in ignore list, removing from update list");
                        fileNameIgnore.Add(fileName);
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
                        var loaderVer = GetLoaderVersionRequired(path);
                        if (loaderVer != null)
                        {
                            if (!loaderVer.IsCompatible(BuildInfo.Version))
                            {
                                string installString = loaderVer.IsMinimum ? $"{loaderVer.SemVer} or later" : $"{loaderVer.SemVer} specifically";
                                LoggerInstance.Warning($"{assemblyName} is not compatible with the current version of MelonLoader ({BuildInfo.Version}), for it to work you need to install {installString}");
                                continue;
                            }
                        }
                        var data = GetModData(melonAssemblyInfo.DownloadLink);
                        data.Wait();
                        if (data.Result == null && string.IsNullOrWhiteSpace(melonAssemblyInfo.DownloadLink))
                        {
                            if (GetPreferenceValue<bool>(Entry_bruteCheck))
                            {
                                LoggerInstance.Msg("Running brute check..");
                                data = GetModDataFromInfo(melonAssemblyInfo.Name.Replace(" ", ""), melonAssemblyInfo.Author);
                                data.Wait();
                            }
                        }
                        if (data.Result != null)
                        {
                            ModVersion currentVersion = ModVersion.GetFromString(melonAssemblyInfo.Version);
                            if (currentVersion != null && data.Result.LatestVersion != null)
                            {
                                bool? needsUpdate = ModVersion.CompareVersions(data.Result.LatestVersion, currentVersion);
                                if (needsUpdate != null && needsUpdate == true)
                                {
                                    if (automatic)
                                    {
                                        LoggerInstance.Msg($"A new version \x1b[32mv{data.Result.LatestVersion}\x1b[0m is available, meanwhile the current version is \u001b[32mv{currentVersion}\u001b[0m, updating");
                                        LoggerInstance.Msg("Downloading file(s)");
                                        foreach (string filePath in data.Result.DownloadFileURL)
                                        {
                                            using var httpClient = new HttpClient();
                                            using var response = httpClient.GetAsync(filePath, HttpCompletionOption.ResponseHeadersRead);
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
                                                using var ms = response.Result.Content.ReadAsStreamAsync();
                                                ms.Wait();
                                                using var fs = File.Create(pathToSave);
                                                var copyTask = ms.Result.CopyToAsync(fs);
                                                copyTask.Wait();
                                                fs.Flush();
                                                downloadedFile = fs;
                                                ms.Dispose();
                                                LoggerInstance.Msg($"Download successful");
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
                                                        DirectoryInfo tempDir = new(tempFilesPath);
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
                                                                        var _loaderVer = GetLoaderVersionRequired(fPath);
                                                                        if (_loaderVer != null)
                                                                        {
                                                                            if (!_loaderVer.IsCompatible(BuildInfo.Version))
                                                                            {
                                                                                string installString = _loaderVer.IsMinimum ? $"{_loaderVer.SemVer} or later" : $"{_loaderVer.SemVer} specifically";
                                                                                LoggerInstance.Warning($"{Path.GetFileName(fPath)} ({GetMelonInfo(fPath).Version}), a newly downloaded mod, is not compatible with the current version of MelonLoader ({BuildInfo.Version}), for it to work you need to install {installString}.");
                                                                                LoggerInstance.Warning($"Not installing {Path.GetFileName(fPath)}");
                                                                                continue;
                                                                            }
                                                                        }
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
                                                                        var _loaderVer = GetLoaderVersionRequired(fPath);
                                                                        if (_loaderVer != null)
                                                                        {
                                                                            if (!_loaderVer.IsCompatible(BuildInfo.Version))
                                                                            {
                                                                                string installString = _loaderVer.IsMinimum ? $"{_loaderVer.SemVer} or later" : $"{_loaderVer.SemVer} specifically";
                                                                                LoggerInstance.Warning($"{Path.GetFileName(fPath)} ({GetMelonInfo(fPath).Version}), a newly downloaded plugin, is not compatible with the current version of MelonLoader ({BuildInfo.Version}), for it to work you need to install {installString}.");
                                                                                LoggerInstance.Warning($"Not installing {Path.GetFileName(fPath)}");
                                                                                continue;
                                                                            }
                                                                        }
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
                                                                    var _loaderVer = GetLoaderVersionRequired(extPath);
                                                                    if (_loaderVer != null)
                                                                    {
                                                                        if (!_loaderVer.IsCompatible(BuildInfo.Version))
                                                                        {
                                                                            string installString = _loaderVer.IsMinimum ? $"{_loaderVer.SemVer} or later" : $"{_loaderVer.SemVer} specifically";
                                                                            LoggerInstance.Warning($"{Path.GetFileName(extPath)} ({GetMelonInfo(extPath).Version}), a newly downloaded mod, is not compatible with the current version of MelonLoader ({BuildInfo.Version}), for it to work you need to install {installString}.");
                                                                            LoggerInstance.Warning($"Not installing {Path.GetFileName(extPath)}");
                                                                            continue;
                                                                        }
                                                                    }
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
                                                                    var _loaderVer = GetLoaderVersionRequired(extPath);
                                                                    if (_loaderVer != null)
                                                                    {
                                                                        if (!_loaderVer.IsCompatible(BuildInfo.Version))
                                                                        {
                                                                            string installString = _loaderVer.IsMinimum ? $"{_loaderVer.SemVer} or later" : $"{_loaderVer.SemVer} specifically";
                                                                            LoggerInstance.Warning($"{Path.GetFileName(extPath)} ({GetMelonInfo(extPath).Version}), a newly downloaded plugin, is not compatible with the current version of MelonLoader ({BuildInfo.Version}), for it to work you need to install {installString}.");
                                                                            LoggerInstance.Warning($"Not installing {Path.GetFileName(extPath)}");
                                                                            continue;
                                                                        }
                                                                    }
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
                                                    ? $"Updated {assemblyName} from \x1b[32;1mv{currentVersion} --> v{data.Result.LatestVersion}\x1b[0m, ({success}/{success + failed}) installed successfully"
                                                    : $"Failed to update {assemblyName}"
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
                                        LoggerInstance.Msg($"A new version \x1b[32mv{data.Result.LatestVersion}\x1b[0m is available, meanwhile the current version is \u001b[32mv{currentVersion}\u001b[0m. We recommend that you update, go to this site to download: " + melonAssemblyInfo.DownloadLink);
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

            LoggerInstance.Msg("Checking plugins...");
            CheckDirectory(MelonEnvironment.PluginsDirectory, false);
            LoggerInstance.Msg("Done checking plugins");
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
    }
}