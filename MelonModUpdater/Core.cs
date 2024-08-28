using MelonLoader;
using Mono.Cecil;
using System.Collections.Generic;
using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using MelonAutoUpdater.Helper;
using MelonLoader.ICSharpCode.SharpZipLib.Core;
using MelonLoader.ICSharpCode.SharpZipLib.Zip;
using MelonLoader.TinyJSON;
using System.Threading.Tasks;
using Semver;

[assembly: MelonInfo(typeof(MelonAutoUpdater.Core), "MelonAutoUpdater", "1.0.0", "HAHOOS", null)]
[assembly: MelonPriority(-10000)]

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
        /// The time (in Unix time seconds) when the rate limit will disappear
        /// </summary>
        private long githubResetDate;

        #region Melon Preferences

        /// <summary>
        /// Main Category in Preferences
        /// </summary>
        internal MelonPreferences_Category Category { get; private set; }

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
        /// Setup Preferences
        /// </summary>
        private void SetupPreferences()
        {
            Category = MelonPreferences.CreateCategory("MelonAutoUpdater", "Melon Auto Updater");
            Category.SetFilePath(Path.Combine(mainFolderPath, "config.cfg"));

            Entry_ignore = Category.CreateEntry<List<string>>("IgnoreList", new List<string>(), "Ignore List",
                description: "List of all names of Mods & Plugins that will be ignored when checking for updates");
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

        /// <summary>
        /// Copied from MelonLoader v0.6.4 to make it work with older versions
        /// </summary>
        internal bool IsCompatible(VerifyLoaderVersionAttribute attribute, SemVersion version)
           => attribute.SemVer == null || version == null || (attribute.IsMinimum ? attribute.SemVer <= version : attribute.SemVer == version);

        /// <summary>
        /// Copied from MelonLoader v0.6.4 to make it work with older versions
        /// </summary>
        internal bool IsCompatible(VerifyLoaderVersionAttribute attribute, string version)
            => !SemVersion.TryParse(version, out SemVersion ver) || IsCompatible(attribute, ver);

        /// <summary>
        /// Copy a stream to a new one<br/>
        /// Made to work with net35
        /// </summary>
        /// <param name="input">The stream u want to copy from</param>
        /// <param name="output">The stream u want to copy to</param>
        internal static void CopyTo(Stream input, Stream output)
        {
            byte[] buffer = new byte[16 * 1024];
            int bytesRead;

            while ((bytesRead = input.Read(buffer, 0, buffer.Length)) > 0)
            {
                output.Write(buffer, 0, bytesRead);
            }
        }

        /// <summary>
        /// Transform function (or action) to a task<br/>
        /// Made to work with net35
        /// </summary>
        /// <param name="action">The action that u want to convert into a task</param>
        /// <returns>A task of the provided action</returns>
        internal static Task<string> ToTask(Action action)
        {
            TaskCompletionSource<string> taskCompletionSource = new
                   TaskCompletionSource<string>();
            string result = null;
            try
            {
                Task.Factory.StartNew(() => action());
            }
            catch (Exception ex)
            {
                result = ex.Message;
            }
            taskCompletionSource.SetResult(result);
            return taskCompletionSource.Task;
        }

        /// <summary>
        /// Create's an empty task, so you can return null with tasks<br/>
        /// Made to work with net35
        /// </summary>
        /// <typeparam name="T">Type that will be returned in Task</typeparam>
        /// <returns>An empty Task with provided type</returns>
        internal static Task<T> CreateEmptyTask<T>()
        {
            var tcs = new TaskCompletionSource<T>();
            tcs.SetResult(default);
            return tcs.Task;
        }

        /// <summary>
        /// Unzip a file from stream<br/>
        /// Made to work with net35
        /// </summary>
        /// <param name="zipStream">Stream of the ZIP File</param>
        /// <param name="outFolder">Path to folder which will have the content of the zip/param>
        /// <returns>A task</returns>
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
        /// <param name="timeoutMs">Time in milliseconds after the request will be aborted if no response (Default: 10000)</param>
        /// <param name="url">URL of the website used to check for connection (Default: null, url selected in code depending on country)</param>
        /// <returns>If true, there's internet connection, otherwise not</returns>
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
        /// <param name="downloadLink">Download Link, possibly included in the MelonInfoAttribute</param>
        /// <returns>If found, returns a ModData object which includes the latest version of the mod online and the download link(s)</returns>
        internal Task<ModData> GetModData(string downloadLink)
        {
            if (string.IsNullOrEmpty(downloadLink))
            {
                LoggerInstance.Msg("No download link provided with mod, unable to fetch necessary information");
                return CreateEmptyTask<ModData>();
            }

            Regex ThunderstoreFind_Regex = new Regex(@"https?:\/\/(?:.+\.)?thunderstore\.io|http?:\/\/(?:.+\.)?thunderstore\.io");
            Regex githubRegex = new Regex(@"(?=http:\/\/|https:\/\/)?github.com\/");

            #region Thunderstore

            if (ThunderstoreFind_Regex.Match(downloadLink).Success)
            {
                LoggerInstance.Msg("Thunderstore detected");
                string[] split = downloadLink.Split('/');
                string packageName;
                string namespaceName;
                if (downloadLink.EndsWith("/"))
                {
                    packageName = split[split.Length - 2];
                    namespaceName = split[split.Length - 3];
                }
                else
                {
                    packageName = split[split.Length - 1];
                    namespaceName = split[split.Length - 2];
                }

                HttpClient request = new HttpClient();
                Task<HttpResponseMessage> response = request.GetAsync($"https://thunderstore.io/api/experimental/package/{namespaceName}/{packageName}/");
                response.Wait();
                if (response.Result.IsSuccessStatusCode)
                {
                    Task<string> body = response.Result.Content.ReadAsStringAsync();
                    body.Wait();
                    if (body.Result != null)
                    {
                        var _data = JSON.Load(body.Result);

                        request.Dispose();
                        response.Dispose();
                        body.Dispose();

                        return Task.Factory.StartNew<ModData>(() => new ModData()
                        {
                            LatestVersion = ModVersion.GetFromString((string)_data["latest"]["version_number"]),
                            DownloadFileURL = new List<string>() { (string)_data["latest"]["download_url"] },
                        });
                    }
                    else
                    {
                        LoggerInstance.Error("Thunderstore API returned no body, unable to fetch package information");

                        request.Dispose();
                        response.Dispose();
                        body.Dispose();

                        return CreateEmptyTask<ModData>();
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

                    return CreateEmptyTask<ModData>();
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
                    packageName = split[split.Length - 2];
                    namespaceName = split[split.Length - 3];
                }
                else
                {
                    packageName = split[split.Length - 1];
                    namespaceName = split[split.Length - 2];
                }
                HttpClient client = new HttpClient();
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
                            var data = JSON.Load(body.Result);
                            string version = (string)data["tag_name"];
                            List<string> downloadURLs = new List<string>();

                            foreach (var file in data["assets"] as ProxyArray)
                            {
                                downloadURLs.Add((string)file["browser_download_url"]);
                            }

                            client.Dispose();
                            response.Dispose();
                            body.Dispose();
                            return Task.Factory.StartNew<ModData>(() => new ModData()
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

                            return CreateEmptyTask<ModData>();
                        }
                    }
                    else
                    {
                        int remaining = int.Parse(response.Result.Headers.GetValues("x-ratelimit-remaining").First());
                        int limit = int.Parse(response.Result.Headers.GetValues("x-ratelimit-limit").First());
                        long reset = long.Parse(response.Result.Headers.GetValues("x-ratelimit-reset").First());
                        if (remaining <= 0)
                        {
                            LoggerInstance.Error($"You've reached the rate limit of Github API ({limit}) and you will be able to use the Github API again at {DateTimeOffsetHelper.FromUnixTimeSeconds(reset).ToLocalTime():t}");
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

                        return CreateEmptyTask<ModData>();
                    }
                }
                else
                {
                    MelonLogger.Warning(
                        "Github API access is currently disabled and this check will be aborted, you should be good to use the API at " + DateTimeOffsetHelper.FromUnixTimeSeconds(githubResetDate).ToLocalTime().ToString("t"));
                }
            }

            #endregion Github;

            return CreateEmptyTask<ModData>();
        }

        /// <summary>
        /// Get data about the mod from a name and author<br/>
        /// Github is not supported in brute checking due to extremely strict rate limits
        /// Currently Supported: Thunderstore
        /// </summary>
        /// <returns>If found, returns a ModData object which includes the latest version of the mod online and the download link(s)</returns>
        internal Task<ModData> GetModDataFromInfo(string name, string author)
        {
            if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(author))
            {
                LoggerInstance.Msg("Either author or name is empty, unable to fetch necessary information");
                return CreateEmptyTask<ModData>();
            }

            #region Thunderstore

            LoggerInstance.Msg("Checking Thunderstore");

            HttpClient request = new HttpClient();
            Task<HttpResponseMessage> response = request.GetAsync($"https://thunderstore.io/api/experimental/package/{author}/{name}/");
            response.Wait();
            if (response.Result.IsSuccessStatusCode)
            {
                Task<string> body = response.Result.Content.ReadAsStringAsync();
                body.Wait();
                if (body.Result != null)
                {
                    var _data = JSON.Load(body.Result);

                    request.Dispose();
                    response.Dispose();
                    body.Dispose();

                    return Task.Factory.StartNew<ModData>(() => new ModData()
                    {
                        LatestVersion = ModVersion.GetFromString((string)_data["latest"]["version_number"]),
                        DownloadFileURL = new List<string>() { (string)_data["latest"]["download_url"] },
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

            return CreateEmptyTask<ModData>();
        }

        /// <summary>
        /// Check if a file on specified path is a MelonMod, a MelonPlugin or something else
        /// </summary>
        /// <param name="path">Path of the file to check</param>
        /// <returns>A FileType, either MelonMod, MelonPlugin or Other</returns>
        internal static FileType GetFileType(string path)
        {
            MelonInfoAttribute infoAttribute = GetMelonInfo(path);

            if (infoAttribute != null)
            {
                return infoAttribute.SystemType == typeof(MelonMod) ? FileType.MelonMod : infoAttribute.SystemType == typeof(MelonPlugin) ? FileType.MelonPlugin : FileType.Other;
            }

            return FileType.Other;
        }

        /// <summary>
        /// Get value from a custom attribute
        /// </summary>
        /// <typeparam name="T">Type that will be returned as value</typeparam>
        /// <param name="customAttribute">The custom attribute you want to get value from</param>
        /// <param name="index">Index of the value</param>
        /// <returns>A value from the Custom Attribute with provided Type</returns>
        internal static T Get<T>(CustomAttribute customAttribute, int index)
        {
            if (customAttribute.ConstructorArguments.Count <= 0) return default;
            return (T)customAttribute.ConstructorArguments[index].Value;
        }

        /// <summary>
        /// Retrieve information from the MelonInfoAttribute in an file using Mono.Cecil
        /// </summary>
        /// <param name="path">Path to the file</param>
        /// <returns>If present, returns a MelonInfoAttribute</returns>

        internal static MelonInfoAttribute GetMelonInfo(string path)
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

        /// <summary>
        /// Retrieve information from the MelonPriorityAttribute in an file using Mono.Cecil
        /// </summary>
        /// <param name="path">Path to the file</param>
        /// <returns>If present, returns a MelonPriorityAttribute</returns>
        internal static MelonPriorityAttribute GetMelonPriority(string path)
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

        /// <summary>
        /// Retrieve information from the VerifyLoaderVersionAttribute in an file using Mono.Cecil
        /// </summary>
        /// <param name="path">Path to the file</param>
        /// <returns>If present, returns a VerifyLoaderVersionAttribute</returns>
        internal static VerifyLoaderVersionAttribute GetLoaderVersionRequired(string path)
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

        /// <summary>
        /// Check directory for mods & plugins that can be updated
        /// </summary>
        /// <param name="directory">Path to the directory</param>
        /// <param name="automatic">If true, the mods/plugins will be updated automatically, otherwise there will be only a message displayed about a new version</param>
        internal void CheckDirectory(string directory, bool automatic = true)
        {
            List<string> files = Directory.GetFiles(directory, "*.dll").ToList();

            List<string> ignore = GetPreferenceValue<List<string>>(Entry_ignore);
            bool enabled = GetPreferenceValue<bool>(Entry_enabled);

            if (!enabled)
            {
                LoggerInstance.Msg("Plugin disabled in preferences, aborting..");
                return;
            }

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
                        return;
                    }
                }
            });
            files.RemoveAll(x => fileNameIgnore.Contains(x));

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
                            if (!IsCompatible(loaderVer, BuildInfo.Version))
                            {
                                string installString = loaderVer.IsMinimum ? $"{loaderVer.SemVer} or later" : $"{loaderVer.SemVer} specifically";
                                LoggerInstance.Warning($"{assemblyName} is not compatible with the current version of MelonLoader ({BuildInfo.Version}), for it to work you need to install {installString}");
                                continue;
                            }
                        }
                        var data = GetModData(melonAssemblyInfo.DownloadLink);
                        data.Wait();
                        if (data.Result == null && string.IsNullOrEmpty(melonAssemblyInfo.DownloadLink))
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
                                            var httpClient = new HttpClient();
                                            var response = httpClient.GetAsync(filePath, HttpCompletionOption.ResponseHeadersRead);
                                            response.Wait();
                                            FileStream downloadedFile = null;
                                            string pathToSave = "";
                                            try
                                            {
                                                response.Result.EnsureSuccessStatusCode();
                                                string _contentType = response.Result.Content.Headers.ContentType.MediaType;
                                                if (_contentType != null)
                                                {
                                                    if (_contentType == "application/zip")
                                                    {
                                                        pathToSave = Path.Combine(tempFilesPath, $"{melonAssemblyInfo.Name.Replace(" ", "")}.zip");
                                                    }
                                                    else if (_contentType == "application/x-msdownload")
                                                    {
                                                        pathToSave = Path.Combine(tempFilesPath, $"{melonAssemblyInfo.Name.Replace(" ", "")}.dll");
                                                    }
                                                    else
                                                    {
                                                        pathToSave = Path.Combine(tempFilesPath, $"{melonAssemblyInfo.Name.Replace(" ", "")}.temp");
                                                    }
                                                }

                                                var ms = response.Result.Content.ReadAsStreamAsync();
                                                ms.Wait();
                                                var fs = File.Create(pathToSave);
                                                var copyTask = ToTask(() => CopyTo(ms.Result, fs));
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
                                                downloadedFile.Dispose();
                                                bool threwError = false;
                                                int failed = 0;
                                                int success = 0;
                                                if (Path.GetExtension(pathToSave) == ".zip")
                                                {
                                                    LoggerInstance.Msg("File is a ZIP file, extracting files...");
                                                    string extractPath = Path.Combine(tempFilesPath, melonAssemblyInfo.Name.Replace(" ", "-"));
                                                    try
                                                    {
                                                        Task<bool> unzipTask = UnzipFromStream(File.OpenRead(pathToSave), extractPath);
                                                        unzipTask.Wait();
                                                        LoggerInstance.Msg("Successfully extracted files!");
                                                        LoggerInstance.Msg("Installing content to MelonLoader");
                                                        downloadedFile.Dispose();
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
                                                            foreach (var fPath in Directory.GetFiles(extPath, "*.dll"))
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
                                                                            if (!IsCompatible(_loaderVer, BuildInfo.Version))
                                                                            {
                                                                                string installString = _loaderVer.IsMinimum ? $"{_loaderVer.SemVer} or later" : $"{_loaderVer.SemVer} specifically";
                                                                                LoggerInstance.Warning($"{Path.GetFileName(fPath)} ({GetMelonInfo(fPath).Version}), a newly downloaded mod, is not compatible with the current version of MelonLoader ({BuildInfo.Version}), for it to work you need to install {installString}.");
                                                                                LoggerInstance.Warning($"Not installing {Path.GetFileName(fPath)}");
                                                                                continue;
                                                                            }
                                                                        }
                                                                        string _path = Path.Combine(Path.Combine(MelonUtils.BaseDirectory, "Mods"), Path.GetFileName(fPath));
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
                                                                            if (!IsCompatible(_loaderVer, BuildInfo.Version))
                                                                            {
                                                                                string installString = _loaderVer.IsMinimum ? $"{_loaderVer.SemVer} or later" : $"{_loaderVer.SemVer} specifically";
                                                                                LoggerInstance.Warning($"{Path.GetFileName(fPath)} ({GetMelonInfo(fPath).Version}), a newly downloaded plugin, is not compatible with the current version of MelonLoader ({BuildInfo.Version}), for it to work you need to install {installString}.");
                                                                                LoggerInstance.Warning($"Not installing {Path.GetFileName(fPath)}");
                                                                                continue;
                                                                            }
                                                                        }
                                                                        string pluginPath = Path.Combine(Path.Combine(MelonUtils.BaseDirectory, "Plugins"), fileName);
                                                                        string _path = Path.Combine(Path.Combine(MelonUtils.BaseDirectory, "Plugins"), Path.GetFileName(fPath));
                                                                        if (!File.Exists(_path)) File.Move(fPath, _path);
                                                                        else File.Replace(fPath, _path, Path.Combine(backupFolderPath, $"{Path.GetFileName(_path)}-{DateTimeOffset.Now.ToUnixTimeSeconds()}.dll"));
                                                                        //var melonAssembly = MelonAssembly.LoadMelonAssembly(pluginPath);
                                                                        LoggerInstance.Warning("WARNING: The plugin will only work after game restart");
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
                                                                            string _path = Path.Combine(Path.Combine(MelonUtils.BaseDirectory, "UserLibs"), Path.GetFileName(fPath));
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
                                                                        if (!IsCompatible(_loaderVer, BuildInfo.Version))
                                                                        {
                                                                            string installString = _loaderVer.IsMinimum ? $"{_loaderVer.SemVer} or later" : $"{_loaderVer.SemVer} specifically";
                                                                            LoggerInstance.Warning($"{Path.GetFileName(extPath)} ({GetMelonInfo(extPath).Version}), a newly downloaded mod, is not compatible with the current version of MelonLoader ({BuildInfo.Version}), for it to work you need to install {installString}.");
                                                                            LoggerInstance.Warning($"Not installing {Path.GetFileName(extPath)}");
                                                                            continue;
                                                                        }
                                                                    }
                                                                    string _path = Path.Combine(Path.Combine(MelonUtils.BaseDirectory, "Mods"), Path.GetFileName(extPath));
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
                                                                        if (!IsCompatible(_loaderVer, BuildInfo.Version))
                                                                        {
                                                                            string installString = _loaderVer.IsMinimum ? $"{_loaderVer.SemVer} or later" : $"{_loaderVer.SemVer} specifically";
                                                                            LoggerInstance.Warning($"{Path.GetFileName(extPath)} ({GetMelonInfo(extPath).Version}), a newly downloaded plugin, is not compatible with the current version of MelonLoader ({BuildInfo.Version}), for it to work you need to install {installString}.");
                                                                            LoggerInstance.Warning($"Not installing {Path.GetFileName(extPath)}");
                                                                            continue;
                                                                        }
                                                                    }
                                                                    string pluginPath = Path.Combine(Path.Combine(MelonUtils.BaseDirectory, "Plugins"), fileName);
                                                                    string _path = Path.Combine(Path.Combine(MelonUtils.BaseDirectory, "Plugins"), Path.GetFileName(extPath));
                                                                    if (!File.Exists(_path)) File.Move(extPath, _path);
                                                                    else File.Replace(extPath, _path, Path.Combine(backupFolderPath, $"{Path.GetFileName(path)}-{DateTimeOffset.Now.ToUnixTimeSeconds()}.dll"));
                                                                    //var melonAssembly = MelonAssembly.LoadMelonAssembly(pluginPath);
                                                                    LoggerInstance.Warning("WARNING: The plugin will only work after game restart");
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
            DirectoryInfo mainDir = Directory.CreateDirectory(Path.Combine(Path.Combine(MelonUtils.BaseDirectory, "UserData"), "MelonAutoUpdater"));
            DirectoryInfo tempDir = mainDir.CreateSubdirectory("TemporaryFiles");
            DirectoryInfo backupDir = mainDir.CreateSubdirectory("Backups");

            tempFilesPath = tempDir.FullName;
            mainFolderPath = mainDir.FullName;
            backupFolderPath = backupDir.FullName;

            LoggerInstance.Msg("Clearing possibly left temporary files");

            List<string> tempPaths = Directory.GetFiles(tempFilesPath).ToList();
            tempPaths.AddRange(Directory.GetDirectories(tempFilesPath));

            foreach (FileInfo file in tempDir.GetFiles()) file.Delete();
            foreach (DirectoryInfo subDirectory in tempDir.GetDirectories()) subDirectory.Delete(true);

            LoggerInstance.Msg("Setup Melon Preferences");

            SetupPreferences();

            LoggerInstance.Msg("Checking plugins...");
            CheckDirectory(Path.Combine(MelonUtils.BaseDirectory, "Plugins"), false);
            LoggerInstance.Msg("Done checking plugins");

            LoggerInstance.Msg("Checking mods...");
            CheckDirectory(Path.Combine(MelonUtils.BaseDirectory, "Mods"));
            LoggerInstance.Msg("Done checking mods");
        }
    }
}