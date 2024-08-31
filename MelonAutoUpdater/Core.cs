using MelonLoader;
using Mono.Cecil;
using System.Collections.Generic;
using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using MelonLoader.ICSharpCode.SharpZipLib.Core;
using MelonLoader.ICSharpCode.SharpZipLib.Zip;
using MelonLoader.TinyJSON;
using Semver;
using System.Threading.Tasks;
using MelonAutoUpdater.Pastel;
using System.Drawing;
using MelonLoader.Preferences;
using MelonAutoUpdater.Helper;

[assembly: MelonInfo(typeof(MelonAutoUpdater.Core), "MelonAutoUpdater", "0.2.0", "HAHOOS", "https://github.com/HAHOOS/MelonAutoUpdater")]
[assembly: MelonPriority(-100000000)]
#pragma warning disable CS0618 // Type or member is obsolete
[assembly: MelonColor(ConsoleColor.Green)]
[assembly: MelonAuthorColor(ConsoleColor.Yellow)]
#pragma warning restore CS0618 // Type or member is obsolete

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

        /// <summary>
        /// User Agent Header for all HTTP requests
        /// </summary>
        private string UserAgent;

        /// <summary>
        /// Customized colors, why does it exist? idk
        /// </summary>
        internal Theme theme = new Theme();

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

        internal MelonPreferences_ReflectiveCategory ThemesCategory { get; private set; }

        /// <summary>
        /// Setup Preferences
        /// </summary>
        private Task<bool> SetupPreferences()
        {
            MainCategory = MelonPreferences.CreateCategory("MelonAutoUpdater", "Melon Auto Updater");
            MainCategory.SetFilePath(Path.Combine(mainFolderPath, "config.cfg"));
            Entry_ignore = MainCategory.CreateEntry<List<string>>("IgnoreList", new List<string>(), "Ignore List",
                description: "List of all names of Mods & Plugins that will be ignored when checking for updates");
            Entry_enabled = MainCategory.CreateEntry<bool>("Enabled", true, "Enabled",
                description: "If true, Mods & Plugins will update on every start");
            Entry_bruteCheck = MainCategory.CreateEntry<bool>("BruteCheck", false, "Brute Check",
                description: "If true, when there's no download link provided with mod/plugin, it will check every possible platform providing the Name & Author\nThis is not recommended as it will very easily result in this plugin being rate-limited");

            MainCategory.SaveToFile(false);

            ThemesCategory = MelonPreferences.CreateCategory<Theme>("Theme", "Theme");
            ThemesCategory.SetFilePath(Path.Combine(mainFolderPath, "theme.cfg"));
            ThemesCategory.SaveToFile(false);

            LoggerInstance.Msg("Successfully set up Melon Preferences!");
            return Task.Factory.StartNew(() => true);
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
        internal static bool IsCompatible(VerifyLoaderVersionAttribute attribute, SemVersion version)
           => attribute.SemVer == null || version == null || (attribute.IsMinimum ? attribute.SemVer <= version : attribute.SemVer == version);

        /// <summary>
        /// Copied from MelonLoader v0.6.4 to make it work with older versions
        /// </summary>
        internal static bool IsCompatible(VerifyLoaderVersionAttribute attribute, string version)
            => !SemVersion.TryParse(version, out SemVersion ver) || IsCompatible(attribute, ver);

        /// <summary>
        /// Copy a stream to a new one<br/>
        /// Made to work with net35
        /// </summary>
        /// <param name="input">The stream u want to copy from</param>
        /// <param name="output">The stream u want to copy to</param>
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
                request.DefaultRequestHeaders.Add("User-Agent", UserAgent);
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

                        List<FileData> files = new List<FileData>();

                        FileData fileData = new FileData
                        {
                            FileName = packageName,
                            URL = (string)_data["latest"]["download_url"]
                        };

                        files.Add(fileData);

                        return Task.Factory.StartNew<ModData>(() => new ModData()
                        {
                            LatestVersion = ModVersion.GetFromString((string)_data["latest"]["version_number"]),
                            DownloadFiles = files,
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

            #region Github

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
                client.DefaultRequestHeaders.Add("User-Agent", UserAgent);
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
                            List<FileData> downloadURLs = new List<FileData>();

                            foreach (var file in data["assets"] as ProxyArray)
                            {
                                downloadURLs.Add(new FileData() { URL = (string)file["browser_download_url"], ContentType = (string)file["content_type"], FileName = Path.GetFileNameWithoutExtension((string)file["browser_download_url"]) });
                            }

                            client.Dispose();
                            response.Dispose();
                            body.Dispose();
                            return Task.Factory.StartNew<ModData>(() => new ModData()
                            {
                                LatestVersion = ModVersion.GetFromString(version),
                                DownloadFiles = downloadURLs,
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

            #endregion Github

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
            request.DefaultRequestHeaders.Add("User-Agent", UserAgent);
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

                    List<FileData> files = new List<FileData>();

                    FileData fileData = new FileData
                    {
                        FileName = name,
                        URL = (string)_data["latest"]["download_url"]
                    };

                    return Task.Factory.StartNew<ModData>(() => new ModData()
                    {
                        LatestVersion = ModVersion.GetFromString((string)_data["latest"]["version_number"]),
                        DownloadFiles = files,
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
        /// Get path to save a file from contentType & name provided
        /// </summary>
        /// <param name="contentType">Content Type (Example: application/zip)</param>
        /// <param name="name">Name of the file, without extension</param>
        /// <returns>A path to temporary directory with file name and extension according to contentType</returns>

        internal string GetPathFromContentType(string contentType, string name)
        {
            if (contentType == "application/zip")
            {
                return Path.Combine(tempFilesPath, $"{name.Replace(" ", "")}.zip");
            }
            else if (contentType == "application/x-msdownload")
            {
                return Path.Combine(tempFilesPath, $"{name.Replace(" ", "")}.dll");
            }
            else
            {
                return Path.Combine(tempFilesPath, $"{name.Replace(" ", "")}.temp");
            }
        }

        /// <summary>
        /// Check if an assembly is a MelonMod, a MelonPlugin or something else
        /// </summary>
        /// <param name="assembly">Assembly of the file</param>
        /// <returns>A FileType, either MelonMod, MelonPlugin or Other</returns>
        internal static FileType GetFileType(AssemblyDefinition assembly)
        {
            MelonInfoAttribute infoAttribute = GetMelonInfo(assembly);

            if (infoAttribute != null)
            {
                return infoAttribute.SystemType == typeof(MelonMod) ? FileType.MelonMod : infoAttribute.SystemType == typeof(MelonPlugin) ? FileType.MelonPlugin : FileType.Other;
            }

            return FileType.Other;
        }

        internal void ReplaceAllFiles(string path, string directory)
        {
            foreach (string file in Directory.GetFiles(path))
            {
                LoggerInstance.Msg($"{Path.GetFileName(file)} found in {Path.GetDirectoryName(directory)}, copying file to folder");
                string _path = Path.Combine(directory, Path.GetFileName(file));
                if (!File.Exists(_path)) File.Move(file, _path);
                else File.Replace(file, _path, Path.Combine(backupFolderPath, $"{Path.GetFileName(path)}-{DateTimeOffset.Now.ToUnixTimeSeconds()}.{Path.GetExtension(file)}"));
            }
            foreach (string dir in Directory.GetDirectories(path))
            {
                LoggerInstance.Msg($"Found {Path.GetDirectoryName(dir)}, going through files");
                string _path = Path.Combine(directory, Path.GetDirectoryName(dir));
                if (!Directory.Exists(_path)) Directory.CreateDirectory(_path);
                ReplaceAllFiles(dir, _path);
            }
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
        /// Retrieve information from the MelonInfoAttribute in a file using Mono.Cecil
        /// </summary>
        /// <param name="assembly">Assembly of the file</param>
        /// <returns>If present, returns a MelonInfoAttribute</returns>

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
        /// Retrieve information from the MelonPriorityAttribute in a file using Mono.Cecil
        /// </summary>
        /// <param name="assembly">Assembly of the file</param>
        /// <returns>If present, returns a MelonPriorityAttribute</returns>
        internal static MelonPriorityAttribute GetMelonPriority(AssemblyDefinition assembly)
        {
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
        /// Retrieve information from the VerifyLoaderVersionAttribute in a file using Mono.Cecil
        /// </summary>
        /// <param name="assembly">Assembly of the file</param>
        /// <returns>If present, returns a VerifyLoaderVersionAttribute</returns>
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
        /// Check directory for mods & plugins that can be updated
        /// </summary>
        /// <param name="directory">Path to the directory</param>
        /// <param name="automatic">If true, the mods/plugins will be updated automatically, otherwise there will be only a message displayed about a new version</param>
        internal void CheckDirectory(string directory, bool automatic = true)
        {
            Color newLineColor = (Color)new ColorConverter().ConvertFromString(theme.LineColor);

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

            LoggerInstance.Msg("------------------------------".Pastel(newLineColor));
            foreach (string path in files)
            {
                AssemblyDefinition mainAssembly = AssemblyDefinition.ReadAssembly(path);
                string fileName = Path.GetFileName(path);
                LoggerInstance.Msg($"Checking {fileName.Pastel(theme.FileNameColor)}");
                FileType fileType = GetFileType(mainAssembly);
                if (fileType != FileType.Other)
                {
                    var melonAssemblyInfo = GetMelonInfo(mainAssembly);
                    string assemblyName = (string)melonAssemblyInfo.Name.Clone();
                    if (melonAssemblyInfo != null)
                    {
                        if (!CheckCompability(mainAssembly)) { mainAssembly.Dispose(); continue; }
                        var data = GetModData(melonAssemblyInfo.DownloadLink);
                        data.Wait();
                        if (data.Result == null && string.IsNullOrEmpty(melonAssemblyInfo.DownloadLink))
                        {
                            if (GetPreferenceValue<bool>(Entry_bruteCheck))
                            {
                                LoggerInstance.Msg("Running " + "brute check..".Pastel(Color.Red));
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
                                        LoggerInstance.Msg($"A new version " + $"v{data.Result.LatestVersion}".Pastel(theme.NewVersionColor) + $" is available, meanwhile the current version is " + $"v{currentVersion}".Pastel(theme.OldVersionColor) + ", updating");
                                        LoggerInstance.Msg("Downloading file(s)");
                                        foreach (var retFile in data.Result.DownloadFiles)
                                        {
                                            var httpClient = new HttpClient();
                                            var response = httpClient.GetAsync(retFile.URL, HttpCompletionOption.ResponseHeadersRead);
                                            response.Wait();
                                            FileStream downloadedFile = null;
                                            string pathToSave = "";
                                            string name = !string.IsNullOrEmpty(retFile.FileName) ? retFile.FileName : melonAssemblyInfo.Name;
                                            try
                                            {
                                                response.Result.EnsureSuccessStatusCode();
                                                string _contentType = response.Result.Content.Headers.ContentType.MediaType;
                                                if (!string.IsNullOrEmpty(retFile.ContentType))
                                                {
                                                    pathToSave = GetPathFromContentType(retFile.ContentType, name);
                                                }
                                                else if (_contentType != null)
                                                {
                                                    pathToSave = GetPathFromContentType(_contentType, name);
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
                                                    LoggerInstance.Msg("Downloaded file is a ZIP file, extracting files...");
                                                    string extractPath = Path.Combine(tempFilesPath, name.Replace(" ", "-"));
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
                                                            string dirName = Path.GetDirectoryName(extPath);
                                                            if (dirName != "MelonLoader" && dirName != "UserData")
                                                            {
                                                                foreach (var fPath in Directory.GetFiles(extPath, "*.dll"))
                                                                {
                                                                    AssemblyDefinition _assembly = AssemblyDefinition.ReadAssembly(fPath);
                                                                    FileType _fileType = GetFileType(_assembly);
                                                                    if (_fileType == FileType.MelonMod)
                                                                    {
                                                                        try
                                                                        {
                                                                            LoggerInstance.Msg("Installing mod file " + Path.GetFileName(fPath).Pastel(theme.FileNameColor));
                                                                            if (!CheckCompability(_assembly)) continue;
#pragma warning disable CS0618 // Type or member is obsolete
                                                                            string _path = Path.Combine(Path.Combine(MelonUtils.BaseDirectory, "Mods"), Path.GetFileName(fPath));
#pragma warning restore CS0618 // Type or member is obsolete
                                                                            if (!File.Exists(_path)) File.Move(fPath, _path);
                                                                            else File.Replace(fPath, _path, Path.Combine(backupFolderPath, $"{Path.GetFileName(_path)}-{DateTimeOffset.Now.ToUnixTimeSeconds()}.dll"));
                                                                            success += 1;
                                                                            LoggerInstance.Msg("Successfully installed mod file " + Path.GetFileName(fPath).Pastel(theme.FileNameColor));
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
                                                                            LoggerInstance.Msg("Installing plugin file " + Path.GetFileName(fPath).Pastel(theme.FileNameColor));
                                                                            if (!CheckCompability(_assembly)) { _assembly.Dispose(); continue; }
#pragma warning disable CS0618 // Type or member is obsolete
                                                                            string pluginPath = Path.Combine(Path.Combine(MelonUtils.BaseDirectory, "Plugins"), fileName);
                                                                            string _path = Path.Combine(Path.Combine(MelonUtils.BaseDirectory, "Plugins"), Path.GetFileName(fPath));
#pragma warning restore CS0618 // Type or member is obsolete
                                                                            if (!File.Exists(_path)) File.Move(fPath, _path);
                                                                            else File.Replace(fPath, _path, Path.Combine(backupFolderPath, $"{Path.GetFileName(_path)}-{DateTimeOffset.Now.ToUnixTimeSeconds()}.dll"));
                                                                            //var melonAssembly = MelonAssembly.LoadMelonAssembly(pluginPath);
                                                                            LoggerInstance.Warning("WARNING: The plugin will only work after game restart");
                                                                            LoggerInstance.Msg("Successfully installed plugin file " + Path.GetFileName(fPath).Pastel(theme.FileNameColor));
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
#pragma warning disable CS0618 // Type or member is obsolete
                                                                                string _path = Path.Combine(Path.Combine(MelonUtils.BaseDirectory, "UserLibs"), Path.GetFileName(fPath));
#pragma warning restore CS0618 // Type or member is obsolete
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
                                                            else
                                                            {
                                                                LoggerInstance.Msg($"Found {dirName}, installing all content from it...");
#pragma warning disable CS0618 // Type or member is obsolete
                                                                ReplaceAllFiles(extPath, dirName == "MelonLoader" ? Path.Combine(MelonUtils.BaseDirectory, "MelonLoader") : dirName == "UserData" ? MelonUtils.UserDataDirectory : string.Empty);
#pragma warning restore CS0618 // Type or member is obsolete
                                                            }
                                                        }
                                                        else if (Path.GetExtension(extPath) == ".dll")
                                                        {
                                                            AssemblyDefinition _assembly = AssemblyDefinition.ReadAssembly(extPath);
                                                            FileType _fileType = GetFileType(_assembly);
                                                            if (_fileType == FileType.MelonMod)
                                                            {
                                                                try
                                                                {
                                                                    LoggerInstance.Msg("Installing mod file " + Path.GetFileName(extPath).Pastel(theme.FileNameColor));
                                                                    if (!CheckCompability(_assembly)) { _assembly.Dispose(); continue; }
#pragma warning disable CS0618 // Type or member is obsolete
                                                                    string _path = Path.Combine(Path.Combine(MelonUtils.BaseDirectory, "Mods"), Path.GetFileName(extPath));
#pragma warning restore CS0618 // Type or member is obsolete
                                                                    if (!File.Exists(_path)) File.Move(extPath, _path);
                                                                    else File.Replace(extPath, _path, Path.Combine(backupFolderPath, $"{Path.GetFileName(path)}-{DateTimeOffset.Now.ToUnixTimeSeconds()}.dll"));
                                                                    success += 1;
                                                                    LoggerInstance.Msg("Successfully installed mod file " + Path.GetFileName(extPath).Pastel(theme.FileNameColor));
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
                                                                    LoggerInstance.Msg("Installing plugin file " + Path.GetFileName(extPath).Pastel(theme.FileNameColor));
                                                                    if (!CheckCompability(_assembly)) { _assembly.Dispose(); continue; }
#pragma warning disable CS0618 // Type or member is obsolete
                                                                    string pluginPath = Path.Combine(Path.Combine(MelonUtils.BaseDirectory, "Plugins"), fileName);
                                                                    string _path = Path.Combine(Path.Combine(MelonUtils.BaseDirectory, "Plugins"), Path.GetFileName(extPath));
#pragma warning restore CS0618 // Type or member is obsolete
                                                                    if (!File.Exists(_path)) File.Move(extPath, _path);
                                                                    else File.Replace(extPath, _path, Path.Combine(backupFolderPath, $"{Path.GetFileName(path)}-{DateTimeOffset.Now.ToUnixTimeSeconds()}.dll"));
                                                                    //var melonAssembly = MelonAssembly.LoadMelonAssembly(pluginPath);
                                                                    LoggerInstance.Warning("WARNING: The plugin will only work after game restart");
                                                                    LoggerInstance.Msg("Successfully installed plugin file " + Path.GetFileName(extPath).Pastel(theme.FileNameColor));
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
                                                else if (Path.GetExtension(pathToSave) == ".dll")
                                                {
                                                    LoggerInstance.Msg("Downloaded file is a DLL file, installing content...");
                                                    AssemblyDefinition _assembly = AssemblyDefinition.ReadAssembly(pathToSave);
                                                    FileType _fileType = GetFileType(_assembly);
                                                    if (_fileType == FileType.MelonMod)
                                                    {
                                                        try
                                                        {
                                                            LoggerInstance.Msg("Installing mod file " + Path.GetFileName(pathToSave).Pastel(theme.FileNameColor));
                                                            if (!CheckCompability(_assembly)) { _assembly.Dispose(); continue; }
#pragma warning disable CS0618 // Type or member is obsolete
                                                            string _path = Path.Combine(Path.Combine(MelonUtils.BaseDirectory, "Mods"), Path.GetFileName(pathToSave));
#pragma warning restore CS0618 // Type or member is obsolete
                                                            if (!File.Exists(_path)) File.Move(pathToSave, _path);
                                                            else File.Replace(pathToSave, _path, Path.Combine(backupFolderPath, $"{Path.GetFileName(path)}-{DateTimeOffset.Now.ToUnixTimeSeconds()}.dll"));
                                                            success += 1;
                                                            LoggerInstance.Msg("Successfully installed mod file " + Path.GetFileName(pathToSave).Pastel(theme.FileNameColor));
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
                                                            LoggerInstance.Msg("Installing plugin file " + Path.GetFileName(pathToSave).Pastel(theme.FileNameColor));
                                                            if (!CheckCompability(_assembly)) { _assembly.Dispose(); continue; }
#pragma warning disable CS0618 // Type or member is obsolete
                                                            string pluginPath = Path.Combine(Path.Combine(MelonUtils.BaseDirectory, "Plugins"), fileName);
                                                            string _path = Path.Combine(Path.Combine(MelonUtils.BaseDirectory, "Plugins"), Path.GetFileName(pathToSave));
#pragma warning restore CS0618 // Type or member is obsolete
                                                            if (!File.Exists(_path)) File.Move(pathToSave, _path);
                                                            else File.Replace(pathToSave, _path, Path.Combine(backupFolderPath, $"{Path.GetFileName(path)}-{DateTimeOffset.Now.ToUnixTimeSeconds()}.dll"));
                                                            //var melonAssembly = MelonAssembly.LoadMelonAssembly(pluginPath);
                                                            LoggerInstance.Warning("WARNING: The plugin will only work after game restart");
                                                            LoggerInstance.Msg("Successfully installed plugin file " + Path.GetFileName(pathToSave).Pastel(theme.FileNameColor));
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
                                                        LoggerInstance.Msg($"Not extracting {Path.GetFileName(pathToSave)}, because it does not have the Melon Info Attribute");
                                                    }
                                                }
                                                else
                                                {
                                                    LoggerInstance.Warning($"An unknown file extension {Path.GetExtension(pathToSave)} was found that was not coded to be installed");
                                                }
                                                LoggerInstance.Msg(
                                                    threwError
                                                    ? $"Failed to update {assemblyName}".Pastel(Color.Red)
                                                    : success + failed > 0
                                                    ? $"Updated {assemblyName} from {currentVersion.ToString().Pastel(theme.OldVersionColor)} --> " + $"v{data.Result.LatestVersion}".Pastel(theme.NewVersionColor) + ", " + $"({success}/{success + failed})".Pastel(theme.DownloadCountColor) + " installed successfully"
                                                    : "No files were downloaded, update unsuccessful".Pastel(Color.Yellow)
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
                                    LoggerInstance.Msg("Version is up-to-date!".Pastel(theme.UpToDateVersionColor));
                                }
                            }
                        }
                    }
                }
                else
                {
                    LoggerInstance.Warning($"{fileName} does not seem to be a MelonLoader Mod");
                }
                LoggerInstance.Msg("------------------------------".Pastel(newLineColor));
            }
        }

        // Note to self: Don't use async
        public override void OnPreInitialization()
        {
            UserAgent = $"{this.Info.Name}/{this.Info.Version} Auto-Updater for ML mods";

            LoggerInstance.Msg("Creating folders in UserData");
#pragma warning disable CS0618 // Type or member is obsolete
            DirectoryInfo mainDir = Directory.CreateDirectory(Path.Combine(MelonUtils.UserDataDirectory, "MelonAutoUpdater"));
#pragma warning restore CS0618 // Type or member is obsolete
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

            SetupPreferences().Wait();

            theme = ThemesCategory.GetValue<Theme>();
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