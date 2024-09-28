using MelonAutoUpdater.Helper;
using MelonLoader;
using MelonLoader.ICSharpCode.SharpZipLib.Core;
using MelonLoader.ICSharpCode.SharpZipLib.Zip;
using MelonLoader.TinyJSON;
using Mono.Cecil;
using Semver;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;

#if NET6_0_OR_GREATER

using System.Net.Http;

#endif

using static MelonAutoUpdater.Utils.NuGet;

namespace MelonAutoUpdater.Utils
{
    /// <summary>
    /// Class responsible for handling NuGet Packages
    /// </summary>
    public class NuGet
    {
        /// <summary>
        /// Triggers when NuGet tries to make a log
        /// </summary>
        public event EventHandler<LogEventArgs> Log;

        /// <summary>
        /// Creates new instance of <see cref="NuGet"/>
        /// </summary>
        public NuGet()
        {
        }

        internal static void DownloadFile(string url, string path)
        {
#if NET35_OR_GREATER
            WebClient webClient = new WebClient();
            webClient.DownloadFile(url, path);
#elif NET6_0_OR_GREATER
            HttpClient httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Add("User-Agent", MelonAutoUpdater.UserAgent);
            var get = httpClient.GetAsync(url);
            get.Wait();
            var response = get.Result;
            response.EnsureSuccessStatusCode();
            var fileStream = File.Open(path, FileMode.OpenOrCreate);
            fileStream.Seek(0, SeekOrigin.Begin);
            var resStream = response.Content.ReadAsStream();
            resStream.Seek(0, SeekOrigin.Begin);

            resStream.CopyTo(fileStream);

            fileStream.Dispose();
            resStream.Dispose();

#endif
        }

        /// <summary>
        /// Get name of a directory
        /// </summary>
        /// <param name="path">Path to the directory</param>
        /// <returns>Name of directory</returns>
        internal static string GetDirName(string path)
        {
            return Path.GetFileName(path);
        }

        /// <summary>
        /// Unzip a file from <see cref="Stream"/><br/>
        /// </summary>
        /// <param name="zipStream"><see cref="Stream"/> of the ZIP File</param>
        /// <param name="outFolder">Path to folder which will have the content of the zip</param>
        /// <param name="dirName">Name of directory, will be removed later</param>
        /// <param name="isRedirect">If <see langword="true"/>, this will be treated as a redirect to the Documents folder and if the path is exceeded, an error will be thrown</param>
        /// <exception cref="PathTooLongException">A path was too long, redirect was already done</exception>
        internal string UnzipFromStream(Stream zipStream, string outFolder, string dirName, bool isRedirect = false)
        {
            OnLog("Unzipping content", LogSeverity.MESSAGE);
            try
            {
                zipStream.Seek(0, SeekOrigin.Begin);
                using (var zipInputStream = new ZipInputStream(zipStream))
                {
                    while (zipInputStream.GetNextEntry() is ZipEntry zipEntry)
                    {
                        var entryFileName = zipEntry.Name;

                        var buffer = new byte[4096];

                        var fullZipToPath = Path.Combine(outFolder, entryFileName);
                        if (fullZipToPath.Length >= 256 && !isRedirect)
                        {
                            var _outFolder = new DirectoryInfo(Files.Redirect_CachePackagesFolder).CreateSubdirectory(dirName);
                            return UnzipFromStream(zipStream, _outFolder.FullName, dirName, true);
                        }
                        else if (fullZipToPath.Length > 256 && isRedirect)
                        {
                            throw new PathTooLongException(fullZipToPath);
                        }
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
                OnLog("Successfully unzipped content", LogSeverity.MESSAGE);
                return outFolder;
            }
            catch (Exception ex)
            {
                OnLog($"An unexpected error occurred while unzipping content: \n {ex}", LogSeverity.ERROR);
                return null;
            }
        }

        private SemVersion ProcessLatestVerBody(string body, bool includePreRelease)
        {
            var res = JSON.Load(body);
            int count = (int)res["count"];
            if (count > 0)
            {
                int packages_count = (int)res["items"][0]["count"];
                if (packages_count > 0)
                {
                    var versions = (ProxyArray)res["items"][0]["items"];
                    var list = versions.ToList();
                    list.Sort(delegate (Variant x, Variant y)
                    {
                        var x_parse = SemVersion.TryParse(x["catalogEntry"]["version"].ToString(), out SemVersion x_ver);
                        var y_parse = SemVersion.TryParse(y["catalogEntry"]["version"].ToString(), out SemVersion y_ver);
                        if (!x_parse && !y_parse) return 0;
                        else if (!x_parse) return 1;
                        else if (!y_parse) return -1;
                        else if (x_ver == null && y_ver == null) return 0;
                        else if (x_ver == null) return 1;
                        else if (y_ver == null) return -1;
                        else if (!string.IsNullOrEmpty(x_ver.Prerelease) && !includePreRelease) return 1;
                        else if (!string.IsNullOrEmpty(y_ver.Prerelease) && !includePreRelease) return -1;
                        else return y_ver.CompareTo(x_ver);
                    });
                    var latest = list.First();
                    var latest_ver = SemVersion.Parse(latest["catalogEntry"]["version"].ToString());
                    return latest_ver.ToString();
                }
                else
                {
                    OnLog("No version found associated with package", LogSeverity.ERROR);
                    return null;
                }
            }
            else
            {
                OnLog("No package found", LogSeverity.ERROR);
                return null;
            }
        }

        internal SemVersion GetLatestNuGetVersion(string name, bool includePreRelease)
        {
            OnLog($"Checking for latest version of {name.Pastel(Theme.Instance.FileNameColor)}", LogSeverity.MESSAGE);
            string apiUrl = $"https://api.nuget.org/v3/registration5-gz-semver2/{name.ToLower()}/index.json";
#if NET35_OR_GREATER
            try
            {
                var client = new GZIPWebClient();
                client.Headers.Add("User-Agent", MelonAutoUpdater.UserAgent);
                client.Headers.Add("Accept", "application/json");
                string response = client.DownloadString(apiUrl);
                if (!string.IsNullOrEmpty(response))
                {
                    return ProcessLatestVerBody(response, includePreRelease);
                }
                else
                {
                    OnLog($"Failed to retrieve latest version: \nBody is empty", LogSeverity.ERROR);
                    return null;
                }
            }
            catch (WebException ex)
            {
                OnLog($"Failed to retrieve latest version: \n{ex}", LogSeverity.ERROR);
                return null;
            }

#elif NET6_0_OR_GREATER
            HttpClientHandler handler = new HttpClientHandler()
            {
                AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
            };

            var client = new HttpClient(handler);
            client.DefaultRequestHeaders.Add("User-Agent", MelonAutoUpdater.UserAgent);
            client.DefaultRequestHeaders.Add("Accept", "application/json");
            var res = client.GetAsync(apiUrl);
            res.Wait();
            if (res.Result.IsSuccessStatusCode)
            {
                var body = res.Result.Content.ReadAsStringAsync();
                body.Wait();
                if (!string.IsNullOrEmpty(body.Result))
                {
                    body.Dispose();
                    res.Dispose();
                    return ProcessLatestVerBody(body.Result, includePreRelease);
                }
                else
                {
                    OnLog($"Failed to retrieve latest version: \nBody is empty", LogSeverity.ERROR);
                    body.Dispose();
                    res.Dispose();
                    return null;
                }
            }
            else
            {
                res.Dispose();
                OnLog($"Failed to retrieve latest version: \n{res.Result.StatusCode}: {res.Result.ReasonPhrase}", LogSeverity.ERROR);
                return null;
            }
#endif
        }

        /// <summary>
        /// Install a package from NuGet<br/>
        /// Does not check if it already exists, will overwrite
        /// </summary>
        /// <param name="name">Name of the NuGet package</param>
        /// <param name="version">Version of the NuGet package (Optional)</param>
        /// <param name="includePreRelease">If <see langword="true"/>, when checking for latest version it will include prereleases</param>
        public void InstallPackage(string name, string version = "", bool includePreRelease = false)
        {
            OnLog($"Installing {name.Pastel(Theme.Instance.FileNameColor)}", LogSeverity.MESSAGE);
            var (DLLFile, AllFiles) = DownloadPackage(name, version, includePreRelease);
#pragma warning disable CS0618 // Type or member is obsolete
            var userLibs = Path.Combine(MelonUtils.BaseDirectory, "UserLibs");
#pragma warning restore CS0618 // Type or member is obsolete
            if (DLLFile != null && AllFiles != null && AllFiles.Count > 0)
            {
                var files = new List<FileInfo>();
                foreach (var file in AllFiles)
                {
                    if (File.Exists(file))
                    {
                        var fileName = Path.GetFileName(file);
                        OnLog($"Moving {fileName.Pastel(Theme.Instance.FileNameColor)} to UserLibs", LogSeverity.MESSAGE);
                        var fileInfo = new FileInfo(file);
                        files.Add(fileInfo);
                        fileInfo.CopyTo(Path.Combine(userLibs, fileName), true);
                    }
                }
                foreach (var file in files)
                {
                    if (file.Exists)
                    {
                        if (file.Extension == ".dll")
                        {
                            OnLog($"Loading {file.Name.Pastel(Theme.Instance.FileNameColor)}", LogSeverity.MESSAGE);
                            System.Reflection.Assembly.LoadFile(file.FullName);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Download a package from NuGet
        /// </summary>
        /// <param name="name">Name of the NuGet package</param>
        /// <param name="version">Version of the NuGet package (Optional)</param>
        /// <param name="includePreRelease">If <see langword="true"/>, when checking for latest version it will include prerelease's</param>
        /// <returns>A <see cref="ValueTuple"/> with values DLLFile - path of main DLL file and AllFiles - path of all downloaded files</returns>
        public (string DLLFile, List<string> AllFiles) DownloadPackage(string name, string version = "", bool includePreRelease = false)
        {
            if (string.IsNullOrEmpty(version))
            {
                var latest = GetLatestNuGetVersion(name, includePreRelease);
                if (latest != null)
                {
                    version = latest.ToString();
                }
                else
                {
                    return (null, null);
                }
            }

            var tempDir = Directory.CreateDirectory(Path.Combine(Files.CachePackagesFolder, $"{name}-{version}"));
            if (tempDir.Exists && tempDir.GetFiles("*.dll").Length > 0)
            {
                OnLog($"Found {name.Pastel(Theme.Instance.FileNameColor)} in cache", LogSeverity.DEBUG);
                var dllFile = tempDir.GetFiles("*.dll")[0].FullName;
                List<string> allFiles = new List<string>();
                tempDir.GetFiles().ToList().ForEach(x => allFiles.Add(x.FullName));
                return (dllFile, allFiles);
            }

            var tempDir2 = Directory.CreateDirectory(Path.Combine(Files.Redirect_CachePackagesFolder, $"{name}-{version}"));
            if (tempDir2.Exists && tempDir2.GetFiles("*.dll").Length > 0)
            {
                OnLog($"Found {name.Pastel(Theme.Instance.FileNameColor)} in cache", LogSeverity.DEBUG);
                var dllFile = tempDir2.GetFiles("*.dll")[0].FullName;
                List<string> allFiles = new List<string>();
                tempDir2.GetFiles().ToList().ForEach(x => allFiles.Add(x.FullName));
                return (dllFile, allFiles);
            }
            else
            {
                tempDir2.Delete();
            }

            OnLog($"Downloading {name.Pastel(Theme.Instance.FileNameColor)}", LogSeverity.DEBUG);

            (string DLLFile, List<string> AllFiles) result;
            result.DLLFile = string.Empty;
            result.AllFiles = new List<string>();
            string path = Path.Combine(tempDir.FullName, $"{name}.{version}.nupkg");
            OnLog("Downloading file", LogSeverity.DEBUG);
            DownloadFile($"https://api.nuget.org/v3-flatcontainer/{name.ToLower()}/{version}/{name.ToLower()}.{version}.nupkg", path);
            if (File.Exists(path))
            {
                OnLog("Downloaded successfully, extracting files", LogSeverity.DEBUG);
                FileInfo fileInfo = new FileInfo(path);
                string zip_Path = Path.ChangeExtension(path, "zip");
                fileInfo.MoveTo(zip_Path, true);
                string dirPath = Path.Combine(tempDir.FullName, $"{name}.{version}");
                string unzip = UnzipFromStream(File.Open(zip_Path, FileMode.Open), dirPath, $"{name}.{version}");
                if (unzip == null) return (null, null);
                if (unzip != dirPath)
                {
                    tempDir2 = new DirectoryInfo(unzip).Root;
                }
                dirPath = unzip;
                if (Directory.Exists(dirPath))
                {
                    OnLog("Extracted successfully", LogSeverity.DEBUG);
                    var libDir = new DirectoryInfo(Path.Combine(dirPath, "lib"));
                    if (libDir.Exists)
                    {
                        List<string> dependencyFiles = new List<string>();
                        OnLog("Found lib directory", LogSeverity.DEBUG);
                        var dllFiles = libDir.GetFiles();
                        if (dllFiles.Length > 0)
                        {
                            foreach (var dllFile in dllFiles)
                            {
                                var dllFile2 = Path.Combine(tempDir.FullName, dllFile.Name);
                                dllFile.MoveTo(dllFile2, true);
                                result.AllFiles.Add(dllFile2);
                                if (dllFile.Extension == ".dll")
                                {
                                    result.DLLFile = dllFile2;
                                }
                            }
                        }
                        else
                        {
                            OnLog("Looking for corresponding net version in libraries", LogSeverity.DEBUG);
#if NET6_0_OR_GREATER
                            string netVer = "net6";
#elif NET35_OR_GREATER
                            string netVer = "net35";
#endif
                            var _netDir = Directory.GetDirectories(libDir.FullName).Where(x => GetDirName(x).ToLower().StartsWith(netVer));
                            if (_netDir.Any())
                            {
                                var netDir = new DirectoryInfo(_netDir.First());
                                OnLog("Found corresponding net version in libraries", LogSeverity.DEBUG);
                                var dllFiles2 = netDir.GetFiles();
                                if (dllFiles2.Length > 0)
                                {
                                    foreach (var dllFile in dllFiles2)
                                    {
                                        var dllFile2 = Path.Combine(tempDir.FullName, dllFile.Name);
                                        dllFile.MoveTo(dllFile2, true);
                                        result.AllFiles.Add(dllFile2);
                                        if (dllFile.Extension == ".dll")
                                        {
                                            result.DLLFile = dllFile2;
                                        }
                                    }
                                }
                            }
                            else
                            {
                                OnLog("Could not find corresponding NET version in dependency, unable to install", LogSeverity.ERROR);
                            }
                        }
                    }
                    else
                    {
                        OnLog("Could not find 'lib' directory in downloaded NuGet package", LogSeverity.DEBUG_ERROR);
                    }
                    Directory.Delete(dirPath, true);
                }
                else
                {
                    OnLog("Download failed", LogSeverity.DEBUG_ERROR);
                }
                File.Delete(zip_Path);
            };

            return result;
        }

        /// <summary>
        /// Checks if a NuGet Package is loaded in the assembly
        /// </summary>
        /// <param name="name">Name of the NuGet package</param>
        /// <param name="advancedCheck">If true, it will download and cache the requested package, and check the name used</param>
        /// <param name="version">
        /// Version of the NuGet package (Optional)<br/>
        /// If not set, the version will not be checked if its equal to the version of an installed dependency if found
        /// </param>
        /// <param name="includePreRelease">If <see langword="true"/>, when checking for latest version it will include prereleases</param>
        /// <returns>
        /// isLoaded - <see langword="true"/> if is loaded, otherwise <see langword="false"/><br/>
        /// dllFile - path to DLL file found when using <b>advancedCheck</b>
        /// </returns>
        public (bool isLoaded, string dllFile) IsLoaded(string name, bool advancedCheck, string version = "", bool includePreRelease = false)
        {
            OnLog($"Checking for {name.Pastel(Theme.Instance.FileNameColor)}{(!string.IsNullOrEmpty(version) ? $" v{version}".Pastel(Theme.Instance.NewVersionColor) : "".Pastel(Theme.Instance.NewVersionColor))}", LogSeverity.MESSAGE);
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();

            string assName = name; // This is meant as assembly name, not ass name, didn't have any other ideas, although the name could be horrible
            string dllFile = string.Empty;
            if (advancedCheck)
            {
                var (DLLFile, AllFiles) = DownloadPackage(name, includePreRelease: includePreRelease);
                if (DLLFile != null) dllFile = DLLFile;
                if (DLLFile != null && AllFiles != null)
                {
                    AssemblyDefinition assembly = AssemblyDefinition.ReadAssembly(DLLFile);
                    var assemblyName = Path.GetFileNameWithoutExtension(assembly.MainModule.Name);
                    assName = assemblyName; // Once again, this is not meant to be read as ass, but as a short of assembly.
                }
            }

            if (assemblies != null)
            {
                foreach (var assembly in assemblies)
                {
                    var assemblyName = assembly.GetName();
                    if (assemblyName.Name == assName)
                    {
                        if (!string.IsNullOrEmpty(version))
                        {
                            if (assemblyName.Version == new Version(version))
                            {
                                return (true, dllFile);
                            }
                        }
                        else
                        {
                            return (true, dllFile);
                        }
                    }
                }
            }
            return (false, dllFile);
        }

        /// <summary>
        /// Checks if a NuGet Package is loaded, by checking if its loaded in the assembly and if the file is present in UserLibs
        /// </summary>
        /// <param name="name">Name of the NuGet package</param>
        /// <param name="advancedCheck">If true, it will download and cache the requested package, and check the name used</param>
        /// <param name="version">
        /// Version of the NuGet package (Optional)<br/>
        /// If not set, the version will not be checked if its equal to the version of an installed dependency if found
        /// </param>
        /// <param name="includePreRelease">If <see langword="true"/>, when checking for latest version it will include prereleases</param>
        /// <returns>
        /// <see langword="true"/> if is loaded, otherwise <see langword="false"/>
        /// </returns>
        public bool Internal_IsLoaded(string name, bool advancedCheck, string version = "", bool includePreRelease = false)
        {
            var (isLoaded, dllFile) = IsLoaded(name, advancedCheck, version, includePreRelease);
            if (!isLoaded)
            {
                // Check if file exists

#pragma warning disable CS0618 // Type or member is obsolete
                var userLibs = Path.Combine(MelonUtils.BaseDirectory, "UserLibs");
#pragma warning restore CS0618 // Type or member is obsolete
                var libs = Directory.GetFiles(userLibs, "*.dll");
                var assemblyName = GetAssemblyNameOfNuGetPackage(dllFile);
                if (!string.IsNullOrEmpty(dllFile))
                {
                    var found = libs.Where(x => x.ToLower().EndsWith($"{Path.GetFileName(dllFile).ToLower()}"));
                    if (found.Any())
                    {
                        return true;
                    }
                }
                return false;
            }
            return true;
        }

        /// <summary>
        /// Gets name of assembly of a NuGet package
        /// </summary>
        /// <param name="name">Name of the NuGet package</param>
        /// <param name="version">Version of the NuGet package (Optional)</param>
        /// <param name="includePreRelease">If <see langword="true"/>, when checking for latest version it will include prereleases</param>
        /// <returns></returns>
        public string GetAssemblyNameOfNuGetPackage(string name, string version = "", bool includePreRelease = false)
        {
            if (string.IsNullOrEmpty(version))
            {
                var latest = GetLatestNuGetVersion(name, includePreRelease);
                if (latest != null)
                {
                    version = latest.ToString();
                }
                else
                {
                    return null;
                }
            }

            var (DLLFile, AllFiles) = DownloadPackage(name, version, includePreRelease: includePreRelease);
            if (DLLFile != null && AllFiles != null)
            {
                AssemblyDefinition assembly = AssemblyDefinition.ReadAssembly(DLLFile);
                var assemblyName = Path.GetFileNameWithoutExtension(assembly.MainModule.Name);
                return assemblyName;
            }
            return null;
        }

        /// <summary>
        /// Gets name of assembly of a NuGet package
        /// </summary>
        /// <param name="DLLFile">Path to the DLL file</param>
        /// <returns></returns>
        public static string GetAssemblyNameOfNuGetPackage(string DLLFile)
        {
            if (DLLFile != null && Path.GetExtension(DLLFile) == ".dll")
            {
                AssemblyDefinition assembly = AssemblyDefinition.ReadAssembly(DLLFile);
                var assemblyName = Path.GetFileNameWithoutExtension(assembly.MainModule.Name);
                return assemblyName;
            }
            return null;
        }

        /// <summary>
        /// Triggers the <see cref="Log"/> event
        /// </summary>
        /// <param name="e">Arguments for the event</param>
        protected virtual void OnLog(LogEventArgs e)
        {
            Log?.Invoke(this, e);
        }

        /// <summary>
        /// Triggers the <see cref="Log"/> event
        /// </summary>
        /// <param name="message"><see cref="LogEventArgs.Message"/></param>
        /// <param name="severity"><see cref="LogEventArgs.Severity"/></param>
        protected virtual void OnLog(string message, LogSeverity severity)
        {
            LogEventArgs e = new LogEventArgs(message, severity);
            Log?.Invoke(this, e);
        }

        /// <summary>
        /// <see langword="enum"/> used to describe the severity of the log
        /// </summary>
        public enum LogSeverity
        {
            /// <summary>
            /// The log will be sent as a message
            /// </summary>
            MESSAGE,

            /// <summary>
            /// The log will be sent as a warning
            /// </summary>
            WARNING,

            /// <summary>
            /// The log will be sent as a error
            /// </summary>
            ERROR,

            /// <summary>
            /// The log will be sent as a debug message, which means only when the plugin is in DEBUG mode it will be displayed
            /// </summary>
            DEBUG,

            /// <summary>
            /// The log will be sent as a debug warning, which means only when the plugin is in DEBUG mode it will be displayed
            /// </summary>
            DEBUG_WARNING,

            /// <summary>
            /// The log will be sent as a debug error, which means only when the plugin is in DEBUG mode it will be displayed
            /// </summary>
            DEBUG_ERROR,
        }
    }

    /// <summary>
    /// Event arguments for the event Log in <see cref="NuGet"/>
    /// </summary>
    public class LogEventArgs : EventArgs
    {
        /// <summary>
        /// Message in the log
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// Severity of the log
        /// </summary>
        public LogSeverity Severity { get; set; }

        /// <summary>
        /// Creates new instance of <see cref="LogEventArgs"/>
        /// </summary>
        /// <param name="message"><inheritdoc cref="Message"/></param>
        /// <param name="severity"><inheritdoc cref="Severity"/></param>
        public LogEventArgs(string message, LogSeverity severity)
        {
            Message = message;
            Severity = severity;
        }
    }
}