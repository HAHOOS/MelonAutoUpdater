extern alias ml065;

using MelonAutoUpdater.JSONObjects;
using MelonAutoUpdater.Utils;
using ml065::MelonLoader.ICSharpCode.SharpZipLib.Core;
using ml065::MelonLoader.ICSharpCode.SharpZipLib.Zip;
using ml065::MelonLoader;
using ml065::Semver;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;

namespace MelonAutoUpdater.Extensions.Install
{
    internal class ZIP : InstallExtension
    {
        public override string[] FileExtensions => new string[] { ".zip" };

        public override string Name => "ZIP";

        public override SemVersion Version => new SemVersion(1, 0, 0);

        public override string Author => "HAHOOS";

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

        /// <summary>
        /// Unzip a file from <see cref="Stream"/><br/>
        /// </summary>
        /// <param name="zipStream"><see cref="Stream"/> of the ZIP File</param>
        /// <param name="outFolder">Path to folder which will have the content of the zip</param>
        internal static void UnzipFromStream(Stream zipStream, string outFolder)
        {
            Stopwatch sw = null;
            if (MelonAutoUpdater.Debug)
            {
                sw = Stopwatch.StartNew();
            }
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
            if (MelonAutoUpdater.Debug)
            {
                sw.Stop();
                MelonAutoUpdater.ElapsedTime.Add($"Unzip-{MelonUtils.RandomString(5)}", sw.ElapsedMilliseconds);
            }
        }

        /// <summary>
        /// Move all files from one directory to another
        /// </summary>
        /// <param name="path">A path to directory to copy from</param>
        /// <param name="directory">A path to directory to copy to</param>
        /// <param name="mainDirectoryName">Only used in prefix, just set <see cref="string.Empty"/></param>
        /// <param name="latestVersion">The latest version of the mod the files are from</param>
        /// <param name="config">Config of the Melon</param>
        /// <returns>Info about melon install (times when it succeeded, times when it failed, and if it threw an error)</returns>
        internal (int success, int failed) MoveAllFiles(string path, string directory, string mainDirectoryName, SemVersion latestVersion, MelonConfig config)
        {
            Stopwatch sw = null;
            if (MelonAutoUpdater.Debug)
            {
                sw = Stopwatch.StartNew();
            }
            int success = 0;
            int failed = 0;
            string prefix = (string.IsNullOrEmpty(mainDirectoryName) != true ? $"{mainDirectoryName}/{GetDirName(directory)}" : GetDirName(directory)).Pastel(Color.Cyan);
            foreach (string file in Directory.GetFiles(path))
            {
                if (config != null && !config.CanInclude(file))
                {
                    Logger.MsgPastel($"[{prefix}] {Path.GetFileName(file).Pastel(Theme.Instance.FileNameColor)} will not be loaded due to the Melon being configured this way");
                    continue;
                }
                Logger.MsgPastel($"[{prefix}] {Path.GetFileName(file).Pastel(Theme.Instance.FileNameColor)} found, moving file to folder");
                try
                {
                    string _path = Path.Combine(directory, Path.GetFileName(file));
                    if (Path.GetExtension(file) == ".dll")
                    {
                        var res = InstallPackage(file, latestVersion);
                        if (!res.threwError || res.isMelon) success += 1;
                        else if (res.threwError) failed += 1;
                        if (!res.isMelon)
                        {
                            if (!File.Exists(_path)) File.Move(file, _path);
                            else File.Replace(file, _path, Path.Combine(Files.BackupDirectory, $"{Path.GetFileName(path)}-{DateTimeOffset.Now.ToUnixTimeSeconds()}.{Path.GetExtension(file)}"));
                            Logger.MsgPastel($"[{prefix}] Successfully copied {Path.GetFileName(file).Pastel(Theme.Instance.FileNameColor)}");
                            success += 1;
                        }
                    }
                    else
                    {
                        if (!File.Exists(_path)) File.Move(file, _path);
                        else File.Replace(file, _path, Path.Combine(Files.BackupDirectory, $"{Path.GetFileName(path)}-{DateTimeOffset.Now.ToUnixTimeSeconds()}.{Path.GetExtension(file)}"));
                        Logger.MsgPastel($"[{prefix}] Successfully copied {Path.GetFileName(file).Pastel(Theme.Instance.FileNameColor)}");
                        success += 1;
                    }
                }
                catch (Exception ex)
                {
                    failed++;
                    Logger.Error($"[{prefix}] Failed to copy {Path.GetFileName(file).Pastel(Theme.Instance.FileNameColor)}, exception thrown:{ex}");
                }
            }
            foreach (string dir in Directory.GetDirectories(path))
            {
                if (config != null && !config.CanInclude(dir))
                {
                    Logger.MsgPastel($"[{prefix}] {GetDirName(dir).Pastel(Theme.Instance.FileNameColor)} will not be loaded due to the Melon being configured this way");
                    continue;
                }
                Logger.MsgPastel($"[{prefix}] Found folder {GetDirName(dir).Pastel(Theme.Instance.FileNameColor)}, going through files");
                try
                {
                    string _path = Path.Combine(directory, GetDirName(dir));
                    if (!Directory.Exists(_path)) Directory.CreateDirectory(_path);
                    var res = MoveAllFiles(dir, _path, prefix, latestVersion, config);
                    success += res.success;
                    failed += res.failed;
                }
                catch (Exception ex)
                {
                    failed++;
                    Logger.Error($"[{prefix}] Failed to copy folder {GetDirName(dir).Pastel(Theme.Instance.FileNameColor)}, exception thrown:{ex}");
                }
            }
            if (MelonAutoUpdater.Debug)
            {
                sw.Stop();
                MelonAutoUpdater.ElapsedTime.Add($"MoveFiles-{GetDirName(path)}", sw.ElapsedMilliseconds);
            }
            return (success, failed);
        }

        public override (bool handled, int success, int failed) Install(string path)
        {
            int success = 0;
            int failed = 0;
            Logger.Msg("File is a ZIP, extracting files...");
            string extractPath = Path.Combine(Files.TemporaryMelonsDirectory, FileName.Replace(" ", "-"));
            try
            {
                UnzipFromStream(File.OpenRead(path), extractPath);
                Logger.Msg("Successfully extracted files! Installing content..");
            }
            catch (Exception ex)
            {
                failed += 1;
                Logger.Error($"An exception occurred while extracting files from a ZIP file{ex}");
                Files.Clear(TempDirectory.Melons);
                return (false, 0, 1);
            }
            var allContent = new List<string>();
            var extracedDirectories = Directory.GetDirectories(extractPath).ToList();
            var extractedFiles = Directory.GetFiles(extractPath).ToList();
            extractedFiles.ForEach(x => allContent.Add(x));
            extracedDirectories.ForEach((x) => allContent.Add(x));
            Logger.Msg($"Found {extractedFiles.Count} files and {extracedDirectories.Count} directories");
            foreach (string extPath in allContent)
            {
                if (Directory.Exists(extPath))
                {
                    string dirName = GetDirName(extPath);
                    List<string> SubDirCheck = new List<string>
                                                                {
                                                                    "Mods",
                                                                    "Plugins",
                                                                    "MelonLoader",
                                                                    "UserData",
                                                                    "UserLibs"
                                                                };
                    int checkedDirs = 0;
                    foreach (var subdir in SubDirCheck)
                    {
                        if (Directory.GetDirectories(extPath).Contains(Path.Combine(extPath, subdir)))
                        {
                            var res1 = MoveAllFiles(Path.Combine(extPath, subdir), Files.GetDirectoryInBaseDir(subdir).FullName, string.Empty, MelonData.LatestVersion, MelonConfig);
                            checkedDirs++;
                            success += res1.success;
                            failed += res1.failed;
                        }
                    }
                    if (checkedDirs <= Directory.GetDirectories(extPath).Length)
                    {
                        Logger.Msg($"Found {dirName}, installing all content from it...");
                        var res1 = MoveAllFiles(extPath, Files.GetDirectoryInBaseDir(dirName).FullName, string.Empty, MelonData.LatestVersion, MelonConfig);
                        success += res1.success;
                        failed += res1.failed;
                    }
                }
                else if (Path.GetExtension(extPath) == ".dll")
                {
                    var res = InstallPackage(extPath, MelonData.LatestVersion);
                    if (res.threwError) failed += 1;
                    else success += 1;
                }
                else
                {
                    Logger.Warning($"Not moving {Path.GetFileName(extPath)}, as it seems useless, sorry in advance");
                }
            }
            Directory.Delete(extractPath, true);
            return (true, success, failed);
        }
    }
}