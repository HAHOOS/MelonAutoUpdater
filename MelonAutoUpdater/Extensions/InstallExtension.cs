extern alias ml065;

using ml065::MelonLoader;
using ml065::Semver;
using MelonAutoUpdater.Utils;
using Mono.Cecil;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using static MelonAutoUpdater.MelonUpdater;
using System.Collections.Generic;
using MelonAutoUpdater.JSONObjects;

namespace MelonAutoUpdater.Extensions
{
    /// <summary>
    /// Extension for installing downloaded files if update was found
    /// </summary>
    public abstract class InstallExtension : ExtensionBase
    {
        internal override Type Type => typeof(InstallExtension);

        /// <summary>
        /// Priority at which the extension will be used for file installing
        /// </summary>
        public virtual int Priority { get => 0; }

        /// <summary>
        /// List of all file extensions this extension handles (include the dot in the file extension)
        /// <para>Use <c>*</c> (anywhere in the array) to indicate that this extension handles all files</para>
        /// </summary>
        public abstract string[] FileExtensions { get; }

        /// <summary>
        /// List of files that currently need install due to a melon update
        /// </summary>
        public static string[] InstallList { get; internal set; }

        /// <summary>
        /// If <see langword="true"/>, this indicates that if not updated and RemoveIncompatible in config is enabled, the Melon will be deleted (if its a mod)
        /// <para>This is used when there is at least one <see cref="Incompatibility"/>. If your extension installed/updated something that should fix the issue, you can set this to <see langword="false"/></para>
        /// </summary>
        public static bool NeedUpdate { get => MelonUpdater.needUpdate; set => MelonUpdater.needUpdate = value; }

        /// <summary>
        /// File name of the currently checked Melon
        /// </summary>
        public static string MelonFileName { get; internal set; }

        /// <summary>
        /// Information returned by Search Extensions regarding latest version etc regarding the currently checked Melon
        /// </summary>
        public static MelonData MelonData { get; internal set; }

        /// <summary>
        /// Config regarding how Melon should be handled in MAU if found, otherwise <see langword="null"/>
        /// </summary>
        public static MelonConfig MelonConfig { get; internal set; }

        /// <summary>
        /// Current version of the currently checked Melon
        /// </summary>
        public static SemVersion MelonCurrentVersion { get; internal set; }

        /// <summary>
        /// Name of the downloaded file
        /// </summary>
        public static string FileName { get; internal set; }

        /// <summary>
        /// Called when a file needs to be installed
        /// </summary>
        /// <param name="path">Path to the file that should be installed</param>
        /// <returns>Whether or not the extension was able to handle the file</returns>
        public abstract (bool handled, int success, int failed) Install(string path);

        /// <inheritdoc cref="MelonUpdater.CheckCompatibility(AssemblyDefinition, bool)"/>
        public static Incompatibility[] CheckCompatibility(AssemblyDefinition assembly)
        {
            return MelonUpdater.CheckCompatibility(assembly);
        }

        /// <summary>
        /// Use extensions to handle a file
        /// </summary>
        /// <param name="path">Path to the file</param>
        /// <returns>Whether the file was handled successfully by any extension</returns>
        public static (bool handled, int success, int failed) HandleFile(string path)
        {
            var file = new FileInfo(path);
            List<InstallExtension> installExtensions = new List<InstallExtension>();
            if (file.Exists)
            {
                var extension = file.Extension;
                foreach (var ext in LoadedExtensions)
                {
                    if (ext.Type == typeof(InstallExtension))
                    {
                        var inst = (InstallExtension)ext;
                        if (inst.FileExtensions.Contains(extension) || inst.FileExtensions.Contains("*"))
                        {
                            installExtensions.Add(inst);
                        }
                    }
                }
                if (installExtensions.Count > 0)
                {
                    // HACK: I'm too lazy to do it another way
                    installExtensions.OrderBy(x => x.Priority * (-1));
                    foreach (var ext in installExtensions)
                    {
                        MelonAutoUpdater.logger.Msg($"Handling with {ext.Name.Pastel(ext.NameColor)}");
                        (bool handled, int success, int failed) func() => ext.Install(path);
                        var ret = Safe.SafeFunction(func);
                        if (ret.handled == true) return ret;
                        else MelonAutoUpdater.logger.Msg($"{ext.Name.Pastel(ext.NameColor)} could not handle the file");
                    }
                }
                else
                {
                    MelonAutoUpdater.logger.Msg("No Install Extension is able to handle the file!");
                    return (false, -1, -1);
                }
            }
            else
            {
                MelonAutoUpdater.logger.Msg($"{file.Name.Pastel(Theme.Instance.FileNameColor)} does not exist");
                return (false, -1, -1);
            }
            return (false, -1, -1);
        }

        /// <summary>
        /// Installs Melon from path
        /// </summary>
        /// <param name="path">Path of melon</param>
        /// <param name="latestVersion">Latest version of melon, used to modify <see cref="MelonInfoAttribute"/> in case the version is not correct</param>
        /// <returns>A <see langword="Tuple"/>, success and threwError, self explanatory</returns>
        public (bool isMelon, bool threwError) InstallPackage(string path, SemVersion latestVersion)
        {
            Stopwatch sw = null;
            if (MelonAutoUpdater.Debug)
            {
                sw = Stopwatch.StartNew();
            }
            bool isMelon = false;
            bool threwError = false;
            string fileName = Path.GetFileName(path);
            AssemblyDefinition _assembly = AssemblyDefinition.ReadAssembly(path, new ReaderParameters() { AssemblyResolver = new CustomCecilResolver() });
            FileType _fileType = _assembly.GetFileType();
            if (_fileType == FileType.MelonMod)
            {
                isMelon = true;
                try
                {
                    Logger.MsgPastel("Installing mod file " + Path.GetFileName(path).Pastel(Theme.Instance.FileNameColor));
                    if (CheckCompatibility(_assembly).Length > 0) { _assembly.Dispose(); threwError = true; isMelon = true; return (isMelon, threwError); }
                    string _path = Path.Combine(Files.ModsDirectory, Path.GetFileName(path));
                    if (!File.Exists(_path)) File.Move(path, _path);
                    else File.Replace(path, _path, Path.Combine(Files.BackupDirectory, $"{Path.GetFileName(path)}-{DateTimeOffset.Now.ToUnixTimeSeconds()}.dll"));
                    isMelon = true;

                    Logger.Msg("Checking if mod version is valid");
                    var fileStream = File.Open(_path, FileMode.Open, FileAccess.ReadWrite);
                    _assembly = AssemblyDefinition.ReadAssembly(fileStream, new ReaderParameters() { AssemblyResolver = new CustomCecilResolver() });
                    var melonInfo = MelonAttribute.GetMelonInfo(_assembly);
                    if (melonInfo.Version < latestVersion)
                    {
                        Logger.Warning("Mod has incorrect version which can lead to repeated unnecessary updates, fixing");
                        var module = _assembly.MainModule;
#pragma warning disable CS0618 // Type or member is obsolete
                        var attr = _assembly.CustomAttributes.Where(x => x.AttributeType.Name == nameof(MelonInfoAttribute) || x.AttributeType.Name == nameof(MelonModInfoAttribute));
#pragma warning restore CS0618 // Type or member is obsolete
                        if (attr.Any())
                        {
                            Logger.Msg("Found attribute");
                            var a = attr.First();
                            var versionType = module.ImportReference(typeof(string));
                            a.ConstructorArguments[2] = new CustomAttributeArgument(versionType, latestVersion.ToString());
                            Logger.Msg("Fixed incorrect version of mod");
                        }
                        else
                        {
                            Logger.Error("Could not find attribute, cannot fix incorrect version");
                        }
                    }
                    else
                    {
                        Logger.Msg("Correct mod version, not changing anything");
                    }

                    Logger.Msg("Checking if mod contains download link");
                    if (string.IsNullOrEmpty(melonInfo.DownloadLink))
                    {
                        Logger.Warning("Mod has no download link provided, to improve future checking, one will be added");
                        var module = _assembly.MainModule;
#pragma warning disable CS0618 // Type or member is obsolete
                        var attr = _assembly.CustomAttributes.Where(x => x.AttributeType.Name == nameof(MelonInfoAttribute) || x.AttributeType.Name == nameof(MelonModInfoAttribute));
#pragma warning restore CS0618 // Type or member is obsolete
                        if (attr.Any())
                        {
                            Logger.Msg("Found attribute");
                            var a = attr.First();
                            var versionType = module.ImportReference(typeof(string));
                            a.ConstructorArguments[4] = new CustomAttributeArgument(versionType, MelonData.DownloadLink.ToString());
                            Logger.Msg("Added download link");
                        }
                        else
                        {
                            Logger.Error("Could not find attribute, cannot add download link");
                        }
                    }
                    _assembly.Write();
                    fileStream.Flush();
                    fileStream.Dispose();
                    _assembly.Dispose();
                    Logger.MsgPastel("Successfully installed mod file " + Path.GetFileName(path).Pastel(Theme.Instance.FileNameColor));
                }
                catch (Exception ex)
                {
                    Logger.Error($"An unexpected error occurred while installing content{ex}");
                    threwError = true;
                    isMelon = true;
                }
            }
            else if (_fileType == FileType.MelonPlugin)
            {
                isMelon = true;
                try
                {
                    Logger.MsgPastel("Installing plugin file " + Path.GetFileName(path).Pastel(Theme.Instance.FileNameColor));
                    if (CheckCompatibility(_assembly).Length > 0) { _assembly.Dispose(); threwError = true; isMelon = true; return (isMelon, threwError); }

                    string pluginPath = Path.Combine(Files.PluginsDirectory, fileName);
                    string _path = Path.Combine(Files.PluginsDirectory, Path.GetFileName(path));
                    if (!File.Exists(_path)) File.Move(path, _path);
                    else File.Replace(path, _path, Path.Combine(Files.BackupDirectory, $"{Path.GetFileName(path)}-{DateTimeOffset.Now.ToUnixTimeSeconds()}.dll"));

                    Logger.Msg("Checking if plugin version is valid");
                    var fileStream = File.Open(_path, FileMode.Open, FileAccess.ReadWrite);
                    _assembly = AssemblyDefinition.ReadAssembly(fileStream, new ReaderParameters() { AssemblyResolver = new CustomCecilResolver() });
                    var melonInfo = _assembly.GetMelonInfo();
                    if (melonInfo.Version < latestVersion)
                    {
                        Logger.Warning("Plugin has incorrect version which can lead to repeated unnecessary updates, fixing");
                        var module = _assembly.MainModule;
#pragma warning disable CS0618 // Type or member is obsolete
                        var attr = _assembly.CustomAttributes.Where(x => x.AttributeType.Name == nameof(MelonInfoAttribute) || x.AttributeType.Name == nameof(MelonPluginInfoAttribute));
#pragma warning restore CS0618 // Type or member is obsolete
                        if (attr.Any())
                        {
                            Logger.Msg("Found attribute");
                            var a = attr.First();
                            var semVersionType = module.ImportReference(typeof(string));
                            a.ConstructorArguments[2] = new CustomAttributeArgument(semVersionType, latestVersion.ToString());
                            Logger.Msg("Fixed incorrect version of plugin");
                        }
                        else
                        {
                            Logger.Error("Could not find attribute, cannot fix incorrect version");
                        }
                    }
                    else
                    {
                        Logger.Msg("Correct plugin version, not changing anything");
                    }

                    Logger.Msg("Checking if plugin contains download link");
                    if (string.IsNullOrEmpty(melonInfo.DownloadLink))
                    {
                        Logger.Warning("Plugin has no download link provided, to improve future checking, one will be added");
                        var module = _assembly.MainModule;
#pragma warning disable CS0618 // Type or member is obsolete
                        var attr = _assembly.CustomAttributes.Where(x => x.AttributeType.Name == nameof(MelonInfoAttribute) || x.AttributeType.Name == nameof(MelonPluginInfoAttribute));
#pragma warning restore CS0618 // Type or member is obsolete
                        if (attr.Any())
                        {
                            Logger.Msg("Found attribute");
                            var a = attr.First();
                            var versionType = module.ImportReference(typeof(string));
                            a.ConstructorArguments[4] = new CustomAttributeArgument(versionType, MelonData.DownloadLink.ToString());
                            Logger.Msg("Added download link");
                        }
                        else
                        {
                            Logger.Error("Could not find attribute, cannot add download link");
                        }
                    }
                    _assembly.Write();
                    _assembly.Dispose();
                    fileStream.Flush();
                    fileStream.Dispose();

                    //var melonAssembly = MelonAssembly.LoadMelonAssembly(pluginPath);
                    Logger.Warning("WARNING: The plugin will only work after game restart");
                    Logger.MsgPastel("Successfully installed plugin file " + Path.GetFileName(path).Pastel(theme.FileNameColor));
                }
                catch (Exception ex)
                {
                    Logger.Error($"An unexpected error occurred while installing content{ex}");
                    threwError = true;
                    isMelon = false;
                }
            }
            else
            {
                isMelon = false;
                Logger.Msg($"Not installing (as a Melon) {Path.GetFileName(path)}, because it does not have the Melon Info Attribute");
            }
            if (MelonAutoUpdater.Debug)
            {
                sw.Stop();
                MelonAutoUpdater.ElapsedTime.Add($"InstallPackage-{Path.GetFileName(path)}", sw.ElapsedMilliseconds);
            }
            if (!threwError && MelonFileName == fileName) NeedUpdate = false;
            return (isMelon, threwError);
        }
    }
}