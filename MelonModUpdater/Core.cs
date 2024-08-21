using MelonLoader;
using MelonLoader.Utils;
using System.IO.Compression;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Loader;
using System.Text.Json;

[assembly: MelonInfo(typeof(MelonModUpdater.Core), "MelonModUpdater", "1.0.0", "HAHOOS", null)]
[assembly: MelonPriority(-100)]

namespace MelonModUpdater
{
    public class Core : MelonPlugin
    {
        /// <summary>
        /// Path of the Temporary Files folder in UserData
        /// </summary>
        public string tempFilesPath = "";

        public override void OnInitializeMelon()
        {
        }

        public async Task<ModData> GetModData(string downloadLink)
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

        public static FileType GetFileTypeFromAssembly(Assembly assembly)
        {
            FileType fType = FileType.Other;
            foreach (Type type in assembly.GetTypes().Where(x => x.IsClass && !x.IsAbstract))
            {
                if (type.IsSubclassOf(typeof(MelonMod)))
                {
                    fType = FileType.MelonMod;
                    return fType;
                }
                else if (type.IsSubclassOf(typeof(MelonPlugin)))
                {
                    fType = FileType.MelonPlugin;
                    return fType;
                }
            }
            return fType;
        }

#nullable enable

        public MelonInfoAttribute? GetMelonInfo(Assembly assembly)
#nullable disable
        {
            var melonInfo = assembly.GetCustomAttribute<MelonInfoAttribute>();
            if (melonInfo != null)
                return melonInfo;
            else
            {
                foreach (Type type in assembly.GetTypes().Where(x => x.IsClass && !x.IsAbstract))
                {
                    LoggerInstance.Msg(type.FullName);
                    if (type.Name == "BuildInfo" || type.Name == "AssemblyInfo")
                    {
                        try
                        {
                            string Name = (string)type.GetProperty("Name").GetValue(type, null);
                            MelonLogger.Msg($"Name: {Name}");
                            string Author = (string)type.GetProperty("Author").GetValue(type, null);
                            MelonLogger.Msg($"Author: {Name}");
                            string Version = (string)type.GetProperty("Version").GetValue(type, null);
                            MelonLogger.Msg($"Version: {Name}");
                            string Link = (string)type.GetProperty("URL").GetValue(type, null);
                            MelonLogger.Msg($"URL: {Name}");
                            if (string.IsNullOrEmpty(Link))
                            {
                                Link = (string)type.GetProperty("DownloadLink").GetValue(type, null);
                                MelonLogger.Msg($"DownloadLink: {Name}");
                            }

                            return new MelonInfoAttribute(typeof(Core), Name, Version, Author, Link);
                        }
                        catch (Exception ex)
                        {
                            LoggerInstance.Error($"Could not get necessary properties when creating MelonInfoAttribute\n{ex.Message}\n{ex.StackTrace}");
                            return null;
                        }
                    }
                }
            }
            return null;
        }

        private Assembly? LoadAssembly(string path)
        {
            // The load context needs access to the .Net "core" assemblies...
            string[] runtimeAssemblies = Directory.GetFiles(RuntimeEnvironment.GetRuntimeDirectory(), "*.dll");
            var paths = new List<string>(runtimeAssemblies);
            paths.Add(Path.Combine(MelonEnvironment.UserLibsDirectory, "System.Reflection.Metadata.dll"));
            var resolver = new PathAssemblyResolver(paths);
            using (var mlc = new MetadataLoadContext(resolver))
            {
                return mlc.LoadFromByteArray(File.ReadAllBytes(path));
            }
        }

        public override async void OnPreInitialization()
        {
            LoggerInstance.Msg("Melon Initialized.");
            LoggerInstance.Msg("Creating folders in UserData");
            DirectoryInfo mainDir = Directory.CreateDirectory(Path.Combine(MelonEnvironment.UserDataDirectory, "MelonUpdater"));
            DirectoryInfo tempDir = mainDir.CreateSubdirectory("TemporaryFiles");

            tempFilesPath = tempDir.FullName;

            string modDirectory = MelonEnvironment.ModsDirectory;
            string[] files = Directory.GetFiles(modDirectory);

            LoggerInstance.Msg("\x1b[34;1m-----------\x1b[0m");
            foreach (string path in files)
            {
                await Task.Delay(50);
                if (path.EndsWith(".dll"))
                {
                    string fileName = Path.GetFileName(path);
                    LoggerInstance.Msg($"Checking \x1b[31m{fileName}\x1b[0m");
                    AssemblyLoadContext assemblyLoadContext = null;
                    Assembly file = null;
                    MetadataLoadContext context = null;
                    try
                    {
                        //assemblyLoadContext = new AssemblyLoadContext("MelonModUpdater_AssemblyLoader", true);
                        //file = assemblyLoadContext.LoadFromAssemblyPath(path);
                        file = LoadAssembly(path);
                    }
                    catch (Exception ex)
                    {
                        LoggerInstance.Error($"Unable to load assembly\n{ex.Message}\n{ex.StackTrace}");
                        continue;
                    }
                    if (file != null)
                    {
                        FileType fileType = GetFileTypeFromAssembly(file);
                        if (fileType == FileType.MelonMod)
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
                                                context.Dispose();
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
                                                                        var fFile = assemblyLoadContext.LoadFromAssemblyPath(fPath);

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
                        context.Dispose();
                    }
                    LoggerInstance.Msg("\x1b[34;1m-----------\x1b[0m");
                }
            }
        }

        private Assembly Resolving(AssemblyLoadContext context, AssemblyName name)
        {
            string assemblyPath = $"{MelonEnvironment.ModsDirectory}\\{name.Name}.dll";
            if (assemblyPath != null)
                return context.LoadFromAssemblyPath(assemblyPath);
            return null;
        }
    }
}