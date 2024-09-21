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
using System.Reflection;

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

        internal string TempFolder;

        /// <summary>
        /// Creates new instance of <see cref="NuGet"/>
        /// </summary>
        public NuGet()
        {
            TempFolder = Core.packagesPath;
        }

        internal void DownloadFile(string url, string path)
        {
#if NET35_OR_GREATER
            WebClient webClient = new WebClient();
            webClient.DownloadFile(url, path);
#elif NET6_0_OR_GREATER
            HttpClient httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Add("User-Agent", Core.UserAgent);
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

        internal static void FileMoveTo(string originPath, string desPath, bool overwrite = false)
        {
#if NET6_0_OR_GREATER
            var originFile = new FileInfo(originPath);
            if (originFile.Exists)
            {
                originFile.MoveTo(desPath, overwrite);
            }
            else
            {
                throw new FileNotFoundException("Could not find the origin file!", originPath);
            }
#elif NET35_OR_GREATER
            var originFile = new FileInfo(originPath);
            if (originFile.Exists)
            {
                var originStream = originFile.OpenRead();
                var desFile = new FileInfo(desPath);
                if (desFile.Exists)
                {
                    if (overwrite)
                    {
                        var desStream = desFile.OpenWrite();
                        originStream.CopyTo(desStream);

                        originStream.Dispose();
                        desStream.Dispose();

                        originFile.Delete();
                    }
                    else
                    {
                        throw new Exception("Cannot overwrite file " + desPath);
                    }
                }
                else
                {
                    var desStream = File.Create(desPath);
                    originStream.CopyTo(desStream);

                    originStream.Dispose();
                    desStream.Dispose();

                    originFile.Delete();
                }
            }
            else
            {
                throw new FileNotFoundException("Could not find the origin file!", originPath);
            }
#endif
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
        /// Unzip a file from <see cref="Stream"/><br/>
        /// </summary>
        /// <param name="zipStream"><see cref="Stream"/> of the ZIP File</param>
        /// <param name="outFolder"><see cref="Path"/> to folder which will have the content of the zip</param>
        internal static bool UnzipFromStream(Stream zipStream, string outFolder)
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
            return true;
        }

        internal SemVersion _ProcessLatestVerBody(string body, bool includePreRelease)
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
                        var x_ver = SemVersion.Parse(x["catalogEntry"]["version"].ToString());
                        var y_ver = SemVersion.Parse(y["catalogEntry"]["version"].ToString());
                        if (x_ver == null && y_ver == null) return 0;
                        else if (x_ver == null) return -1;
                        else if (y_ver == null) return 1;
                        else if (!string.IsNullOrEmpty(x_ver.Prerelease) && !includePreRelease) return -1;
                        else if (!string.IsNullOrEmpty(y_ver.Prerelease) && !includePreRelease) return 1;
                        else return y_ver.CompareTo(x_ver);
                    });
                    var latest = list[0];
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
            string apiUrl = $"https://api.nuget.org/v3/registration5-semver1/{name.ToLower()}/index.json";
#if NET35_OR_GREATER
            try
            {
                var client = new WebClient();
                string response = client.DownloadString(apiUrl);
                if (response != null)
                {
                    return _ProcessLatestVerBody(response, includePreRelease);
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
            var client = new HttpClient();
            var res = client.GetAsync(apiUrl);
            res.Wait();
            if (res.Result.IsSuccessStatusCode)
            {
                var body = res.Result.Content.ReadAsStringAsync();
                body.Wait();
                if (!string.IsNullOrEmpty(body.Result))
                {
                    return _ProcessLatestVerBody(body.Result, includePreRelease);
                }
                else
                {
                    OnLog($"Failed to retrieve latest version: \nBody is empty", LogSeverity.ERROR);
                    return null;
                }
            }
            else
            {
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
            var package = DownloadPackage(name, version, includePreRelease);
#pragma warning disable CS0618 // Type or member is obsolete
            var userLibs = Path.Combine(MelonUtils.BaseDirectory, "UserLibs");
#pragma warning restore CS0618 // Type or member is obsolete
            if (package.DLLFile != null && package.AllFiles != null)
            {
                foreach (var file in package.AllFiles)
                {
                    if (File.Exists(file))
                    {
                        var fileName = Path.GetFileName(file);
                        OnLog($"Moving {fileName.Pastel(Theme.Instance.FileNameColor)} to UserLibs", LogSeverity.MESSAGE);
                        FileMoveTo(file, Path.Combine(userLibs, fileName), true);
                    }
                }
                foreach (var file in package.AllFiles)
                {
                    if (File.Exists(file))
                    {
                        string extension = Path.GetExtension(file);
                        if (extension == ".dll")
                        {
                            System.Reflection.Assembly.LoadFile(file);
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
        /// <param name="includePreRelease">If <see langword="true"/>, when checking for latest version it will include prereleases</param>
        /// <returns>A <see cref="ValueTuple"/> with values DLLFile - path of main DLL file and AllFiles - path of all downloaded files</returns>
        public (string DLLFile, List<string> AllFiles) DownloadPackage(string name, string version = "", bool includePreRelease = false)
        {
            OnLog($"Downloading {name.Pastel(Theme.Instance.FileNameColor)}", LogSeverity.MESSAGE);

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

            var tempDir = Directory.CreateDirectory(Path.Combine(TempFolder, $"{name}-{version}"));
            if (tempDir.GetFiles("*.dll").Length > 0)
            {
                OnLog($"Found {name.Pastel(Theme.Instance.FileNameColor)} in cache", LogSeverity.MESSAGE);
                var dllFile = tempDir.GetFiles("*.dll")[0].FullName;
                List<string> allFiles = new List<string>();
                tempDir.GetFiles().ToList().ForEach(x => allFiles.Add(x.FullName));
                return (dllFile, allFiles);
            }

            (string DLLFile, List<string> AllFiles) result;
            result.DLLFile = string.Empty;
            result.AllFiles = new List<string>();

#pragma warning disable CS0618 // Type or member is obsolete
            string path = Path.Combine(tempDir.FullName, $"{name}.{version}.nupkg");
#pragma warning restore CS0618 // Type or member is obsolete
            OnLog("Downloading file", LogSeverity.MESSAGE);
            DownloadFile($"https://api.nuget.org/v3-flatcontainer/{name.ToLower()}/{version}/{name.ToLower()}.{version}.nupkg", path);
            if (File.Exists(path))
            {
                OnLog("Downloaded successfully, extracting files", LogSeverity.MESSAGE);
                FileInfo fileInfo = new FileInfo(path);
                string zip_Path = Path.ChangeExtension(path, "zip");
                FileMoveTo(fileInfo.FullName, zip_Path, true);
#pragma warning disable CS0618 // Type or member is obsolete
                string dirPath = Path.Combine(tempDir.FullName, $"{name}.{version}-dependency");
#pragma warning restore CS0618 // Type or member is obsolete
                UnzipFromStream(File.Open(zip_Path, FileMode.Open), dirPath);
                if (Directory.Exists(dirPath))
                {
                    OnLog("Extracted successfully", LogSeverity.MESSAGE);
                    var libDir = new DirectoryInfo(Path.Combine(dirPath, "lib"));
                    if (libDir.Exists)
                    {
                        List<string> dependencyFiles = new List<string>();
                        OnLog("Found lib directory, finding dependency", LogSeverity.MESSAGE);
                        var dllFiles = libDir.GetFiles();
                        if (dllFiles.Length > 0)
                        {
                            OnLog("Found dependency in lib directory", LogSeverity.MESSAGE);
                        }
                        else
                        {
                            OnLog("Looking for corresponding net version in libraries", LogSeverity.MESSAGE);
#if NET6_0_OR_GREATER
                            string netVer = "net6";
#elif NET35_OR_GREATER
                            string netVer = "net35";
#endif
                            var _netDir = Directory.GetDirectories(libDir.FullName).Where(x => GetDirName(x).ToLower().StartsWith(netVer));
                            if (_netDir.Any())
                            {
                                var netDir = new DirectoryInfo(_netDir.First());
                                OnLog("Found corresponding net version in libraries, installing", LogSeverity.MESSAGE);
                                var dllFiles2 = netDir.GetFiles();
                                if (dllFiles2.Length > 0)
                                {
                                    foreach (var dllFile in dllFiles2)
                                    {
                                        FileMoveTo(dllFile.FullName, Path.Combine(tempDir.FullName, dllFile.Name), true);
                                        result.AllFiles.Add(Path.Combine(tempDir.FullName, dllFile.Name));
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
                        OnLog("Could not find 'lib' directory in downloaded NuGet package", LogSeverity.ERROR);
                    }
                    Directory.Delete(dirPath, true);
                }
                else
                {
                    OnLog("Download failed", LogSeverity.ERROR);
                }
                File.Delete(zip_Path);
            };

            return result;
        }

        /// <summary>
        /// Checks if a NuGet Package is loaded
        /// </summary>
        /// <param name="name">Name of the NuGet package</param>
        /// <param name="advancedCheck">If true, it will download and cache the requested package, and check the name used</param>
        /// <param name="version">Version of the NuGet package (Optional)</param>
        /// <param name="includePreRelease">If <see langword="true"/>, when checking for latest version it will include prereleases</param>
        /// <returns><see langword="true"/> if is loaded, otherwise <see langword="false"/></returns>
        public bool IsLoaded(string name, bool advancedCheck, string version = "", bool includePreRelease = false)
        {
            OnLog($"Checking for {name} {(!string.IsNullOrEmpty(version) ? $"v{version}" : "")}", LogSeverity.MESSAGE);
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();

            string assName = name; // This is meant as assembly name, not ass name, didn't have any other ideas, although the name could be horrible
            if (advancedCheck)
            {
                var package = DownloadPackage(name, includePreRelease: includePreRelease);
                if (package.DLLFile != null && package.AllFiles != null)
                {
                    AssemblyDefinition assembly = AssemblyDefinition.ReadAssembly(package.DLLFile);
                    var assemblyName = Path.GetFileNameWithoutExtension(assembly.MainModule.Name);
                    assName = assemblyName; // Once again, this is not meant to be read as ass, but as a short of assembly.
                }
            }

            if (assemblies != null)
            {
                foreach (var assembly in assemblies)
                {
                    var assemblyName = assembly.GetName();
                    if (assemblyName.Name == name)
                    {
                        if (!string.IsNullOrEmpty(version))
                        {
                            if (assemblyName.Version == new Version(version))
                            {
                                return true;
                            }
                        }
                        else
                        {
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// Gets name of assembly of a NuGet package
        /// </summary>
        /// <param name="name">Name of the NuGet package</param>
        /// <param name="version">Version of the NuGet package (Optional)</param>
        /// <param name="includePreRelease">If <see langword="true"/>, when checking for latest version it will include prereleases</param>
        /// <returns></returns>
        public string GetAssemblyNameOfNuGetPackage(string name, string version, bool includePreRelease = false)
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

            var package = DownloadPackage(name, version, includePreRelease: includePreRelease);
            if (package.DLLFile != null && package.AllFiles != null)
            {
                AssemblyDefinition assembly = AssemblyDefinition.ReadAssembly(package.DLLFile);
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
            ERROR
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