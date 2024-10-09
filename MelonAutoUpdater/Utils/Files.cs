extern alias ml065;

using MelonAutoUpdater.Helper;
using ml065.MelonLoader;
using System;
using System.IO;

namespace MelonAutoUpdater.Utils
{
    /// <summary>
    /// Class responsible for handling temporary files
    /// </summary>
    public static class Files
    {
        /// <summary>
        /// Path to the main folder for Temporary files and/or folders
        /// </summary>
        public static string TemporaryMainFolder { get; internal set; }

        /// <summary>
        /// Path to folder where Melons have files (for example downloads) stored temporarily
        /// </summary>
        public static string TemporaryMelonsFolder { get; internal set; }

        /// <summary>
        /// Path to folder for packages that are cached
        /// </summary>
        public static string CachePackagesFolder { get; internal set; }

        /// <summary>
        /// Path to folder where folders and/or files should be saved when in another folder the path is too long
        /// </summary>
        public static string RedirectFolder { get; internal set; }

        /// <summary>
        /// Path to folder where Melons have files (for example downloads) stored temporarily
        /// </summary>
        public static string Redirect_TemporaryMelonsFolder { get; internal set; }

        /// <summary>
        /// Path to folder for packages that are cached
        /// </summary>
        public static string Redirect_CachePackagesFolder { get; internal set; }

        /// <summary>
        /// Path of MelonAutoUpdate folder containing all the other folders
        /// </summary>
        public static string MainFolder { get; internal set; }

        /// <summary>
        /// Path of Backup folder where old versions of mods are saved
        /// </summary>
        public static string BackupFolder { get; internal set; }

        /// <summary>
        /// Path of Config folder for all extension config's
        /// </summary>
        public static string ExtConfigFolder { get; internal set; }

        /// <summary>
        /// Setup
        /// </summary>
        internal static void Setup()
        {
            string path;

            if (Platform.IsWindows)
            {
                path = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            }
            else
            {
                path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Documents");
            }
            var mau_documents = Directory.CreateDirectory(Path.Combine(path, "MelonAutoUpdater"));
            RedirectFolder = mau_documents.FullName;

            Redirect_TemporaryMelonsFolder = mau_documents.CreateSubdirectory("Melons").FullName;
            Redirect_CachePackagesFolder = mau_documents.CreateSubdirectory("Packages").FullName;

#pragma warning disable CS0618 // Type or member is obsolete
            DirectoryInfo mainDir = Directory.CreateDirectory(Path.Combine(MelonUtils.UserDataDirectory, "MelonAutoUpdater"));
#pragma warning restore CS0618 // Type or member is obsolete
            DirectoryInfo tempDir = mainDir.CreateSubdirectory("Temporary");
            DirectoryInfo cacheDir = mainDir.CreateSubdirectory("Cache");

            DirectoryInfo packagesDir = cacheDir.CreateSubdirectory("Packages");

            DirectoryInfo melonsDir = tempDir.CreateSubdirectory("Melons");

            DirectoryInfo backupDir = mainDir.CreateSubdirectory("Backups");
            DirectoryInfo extConfigDir = mainDir.CreateSubdirectory("ExtensionsConfig");

            ExtConfigFolder = extConfigDir.FullName;

            MainFolder = mainDir.FullName;
            BackupFolder = backupDir.FullName;

            TemporaryMainFolder = tempDir.FullName;
            CachePackagesFolder = packagesDir.FullName;
            TemporaryMelonsFolder = melonsDir.FullName;
        }

        /// <summary>
        /// Remove all files and folders from a directory/folder
        /// </summary>
        /// <param name="path">Path to the directory</param>
        /// <exception cref="DirectoryNotFoundException">Directory was not found</exception>
        public static void RemoveAll(string path)
        {
            if (string.IsNullOrEmpty(path)) throw new ArgumentNullException(nameof(path));
            var dir = new DirectoryInfo(path);
            if (dir.Exists)
            {
                foreach (FileInfo file in dir.GetFiles()) file.Delete();
                foreach (DirectoryInfo subDirectory in dir.GetDirectories()) subDirectory.Delete(true);
            }
            else
            {
                throw new DirectoryNotFoundException(path);
            }
        }

        /// <summary>
        /// Remove all files and folders from a directory/folder
        /// </summary>
        /// <param name="dir">The directory</param>
        /// <exception cref="DirectoryNotFoundException">Directory was not found</exception>
        public static void RemoveAll(this DirectoryInfo dir)
        {
            if (dir == null) throw new ArgumentNullException(nameof(dir));
            if (dir.Exists)
            {
                foreach (FileInfo file in dir.GetFiles()) file.Delete();
                foreach (DirectoryInfo subDirectory in dir.GetDirectories()) subDirectory.Delete(true);
            }
            else
            {
                throw new DirectoryNotFoundException(dir.FullName);
            }
        }

        /// <summary>
        /// Move all content of a file to a new one and removes the old one
        /// </summary>
        /// <param name="originFile">The file to copy from</param>
        /// <param name="desPath">The destination of the file</param>
        /// <param name="overwrite">Overwrite the content</param>
        /// <exception cref="IOException">File cannot be overwritten</exception>
        /// <exception cref="FileNotFoundException">The origin file was not found</exception>
        public static void MoveTo(this FileInfo originFile, string desPath, bool overwrite = false)
        {
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
                        throw new IOException("Cannot overwrite file " + desPath);
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
                throw new FileNotFoundException("Could not find the origin file!", originFile.FullName);
            }
        }

        /// <summary>
        /// Move all content of a file to a new one and removes the old one
        /// </summary>
        /// <param name="originFile">The file to copy from</param>
        /// <param name="desFile">The file to copy to</param>
        /// <param name="overwrite">Overwrite the content</param>
        /// <exception cref="IOException">File cannot be overwritten</exception>
        /// <exception cref="FileNotFoundException">The origin file was not found</exception>
        public static void MoveTo(this FileInfo originFile, FileInfo desFile, bool overwrite = false)
        {
            if (originFile.Exists)
            {
                var originStream = originFile.OpenRead();
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
                        throw new IOException("Cannot overwrite file " + desFile.FullName);
                    }
                }
                else
                {
                    var desStream = File.Create(desFile.FullName);
                    originStream.CopyTo(desStream);

                    originStream.Dispose();
                    desStream.Dispose();

                    originFile.Delete();
                }
            }
            else
            {
                throw new FileNotFoundException("Could not find the origin file!", originFile.FullName);
            }
        }

        /// <summary>
        /// Move all content of a file to a new one and removes the old one
        /// </summary>
        /// <param name="originPath">The path of file to copy from</param>
        /// <param name="desPath">The destination of the file</param>
        /// <param name="overwrite">Overwrite the content</param>
        /// <exception cref="IOException">File cannot be overwritten</exception>
        /// <exception cref="FileNotFoundException">The origin file was not found</exception>
        public static void MoveTo(string originPath, string desPath, bool overwrite = false)
        {
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
                        throw new IOException("Cannot overwrite file " + desPath);
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
                throw new FileNotFoundException("Could not find the origin file!", originFile.FullName);
            }
        }

        /// <summary>
        /// Move all content of a file to a new one and removes the old one
        /// </summary>
        /// <param name="originPath">The path of file to copy from</param>
        /// <param name="desFile">The file to copy to</param>
        /// <param name="overwrite">Overwrite the content</param>
        /// <exception cref="IOException">File cannot be overwritten</exception>
        /// <exception cref="FileNotFoundException">The origin file was not found</exception>
        public static void MoveTo(string originPath, FileInfo desFile, bool overwrite = false)
        {
            var originFile = new FileInfo(originPath);
            if (originFile.Exists)
            {
                var originStream = originFile.OpenRead();
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
                        throw new IOException("Cannot overwrite file " + desFile.FullName);
                    }
                }
                else
                {
                    var desStream = File.Create(desFile.FullName);
                    originStream.CopyTo(desStream);

                    originStream.Dispose();
                    desStream.Dispose();

                    originFile.Delete();
                }
            }
            else
            {
                throw new FileNotFoundException("Could not find the origin file!", originFile.FullName);
            }
        }

        /// <summary>
        /// Clear all files from a temporary directory
        /// </summary>
        /// <param name="directory">The directory</param>
        /// <exception cref="ArgumentOutOfRangeException">Could not find a correct <see cref="TempDirectory"/> value</exception>
        /// <exception cref="DirectoryNotFoundException">Directory was not found, it is likely you did not run <see cref="Setup()"/></exception>
        public static void Clear(TempDirectory directory)
        {
            if (directory == TempDirectory.Melons)
            {
                RemoveAll(TemporaryMelonsFolder);
                RemoveAll(Redirect_TemporaryMelonsFolder);
            }
            else if (directory == TempDirectory.Packages)
            {
                RemoveAll(CachePackagesFolder);
                RemoveAll(Redirect_CachePackagesFolder);
            }
            else if (directory == TempDirectory.Redirect)
            {
                RemoveAll(Redirect_TemporaryMelonsFolder);
                RemoveAll(Redirect_CachePackagesFolder);
            }
            else if (directory == TempDirectory.All)
            {
                Clear(TempDirectory.Melons);
                Clear(TempDirectory.Packages);
            }
            else
            {
                throw new ArgumentOutOfRangeException(nameof(directory));
            }
        }
    }

    /// <summary>
    /// <see cref="Enum"/> used to indicate what folder to clear off of temporary files
    /// </summary>
    public enum TempDirectory
    {
        /// <summary>
        /// Temporary path for Melon downloads
        /// </summary>
        Melons,

        /// <summary>
        /// Temporary path for Package downloads
        /// </summary>
        Packages,

        /// <summary>
        /// Temporary path for downloads that were redirected
        /// </summary>
        Redirect,

        /// <summary>
        /// All temporary files
        /// </summary>
        All
    }
}