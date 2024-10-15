extern alias ml065;

using MelonAutoUpdater.Helper;
using MelonAutoUpdater.Utils;
using ml065::Semver;
using System.Collections.Generic;
using System;
using System.Drawing;
using System.Reflection;
using System.Linq;
using ml065::MelonLoader;
using System.IO;

namespace MelonAutoUpdater.Extensions
{
    /// <summary>
    /// Base class for extensions
    /// </summary>
    public abstract class ExtensionBase
    {
        /// <summary>
        /// Type of the extension
        /// </summary>
        internal abstract Type Type { get; }

        /// <summary>
        /// Name of the extension that will be displayed in console
        /// </summary>
        public abstract string Name { get; }

        /// <summary>
        /// Version of the extension that will be displayed in the console
        /// </summary>
        public abstract SemVersion Version { get; }

        /// <summary>
        /// Author of the extension that will be displayed in the console
        /// </summary>
        public abstract string Author { get; }

        /// <summary>
        /// ID of the Extension, this is optional and only to be able to load extensions with the same names and authors if in some case you would need, i don't know why you would need it tho.
        /// </summary>
        public virtual string ID { get; }

        /// <summary>
        /// <see cref="Color"/> that should be displayed with the Author of the extension
        /// </summary>
        public virtual Color AuthorColor
        { get { return Color.LightBlue; } }

        /// <summary>
        /// <see cref="Color"/> that should be displayed with the Name of the extension
        /// </summary>
        public virtual Color NameColor
        { get { return Color.LightBlue; } }

        /// <summary>
        /// The required version needed of MAU for the extension to work
        /// </summary>
        public virtual (SemVersion version, bool isMinimum) RequiredMAUVersion
        { get { return (null, false); } }

        /// <summary>
        /// The required version needed of ML for the extension to work
        /// </summary>
        public virtual (SemVersion version, bool isMinimum) RequiredMLVersion
        { get { return (null, false); } }

        /// <summary>
        /// Called when extension is loaded into the plugin
        /// </summary>
        public virtual void OnInitialization()
        { }

        /// <summary>
        /// Configure necessary things in extension
        /// </summary>
        internal virtual void Setup()
        {
            MelonAutoUpdater.logger.DebugMsg($"Setting up logger for {Name}");
            Logger = new MAULogger(Name, ID);
        }

        #region Unload

        /// <summary>
        /// Unloads the specified <see cref="ExtensionBase"/>, which will cause it to not be used in checking
        /// <para>Note that this <b>does not</b> unload the <see cref="Assembly"/></para>
        /// </summary>
        public void Unload(bool printmsg = true)
        {
            if (string.IsNullOrEmpty(ID)) LoadedExtensions.RemoveAll(x => x.Name == Name && x.Author == x.Author);
            else LoadedExtensions.RemoveAll(x => x.Name == Name && x.Author == x.Author && x.ID == ID);
            RottenExtensions.Add(new RottenExtension(this, "The extension has been unloaded by another extension or a melon"));
            if (printmsg) MelonAutoUpdater.logger._MsgPastel($"Extension {Name.Pastel(NameColor)} has been unloaded by another extension or a melon");
        }

        /// <summary>
        /// Unloads the specified <see cref="ExtensionBase"/>, which will cause it to not be used in checking
        /// <para>Note that this <b>does not</b> unload the <see cref="Assembly"/></para>
        /// </summary>
        public void Unload(string message, bool printmsg = true)
        {
            if (string.IsNullOrEmpty(ID)) LoadedExtensions.RemoveAll(x => x.Name == Name && x.Author == x.Author);
            else LoadedExtensions.RemoveAll(x => x.Name == Name && x.Author == x.Author && x.ID == ID);
            RottenExtensions.Add(new RottenExtension(this, $"The extension has been unloaded by another extension or a melon, reason provided: {message}"));
            if (printmsg) MelonAutoUpdater.logger._MsgPastel($"Extension {Name.Pastel(NameColor)} has been unloaded by another extension or a melon with the following message: {message}");
        }

        /// <summary>
        /// Unloads the specified <see cref="ExtensionBase"/>, which will cause it to not be used in checking
        /// <para>Note that this <b>does not</b> unload the <see cref="Assembly"/></para>
        /// </summary>
        public static void Unload(SearchExtension extension, bool printmsg = true)
        {
            if (string.IsNullOrEmpty(extension.ID)) LoadedExtensions.RemoveAll(x => x.Name == extension.Name && x.Author == x.Author);
            else LoadedExtensions.RemoveAll(x => x.Name == extension.Name && x.Author == x.Author && x.ID == extension.ID);
            RottenExtensions.Add(new RottenExtension(extension, "The extension has been unloaded by another extension or a melon"));
            if (printmsg) MelonAutoUpdater.logger._MsgPastel($"Extension {extension.Name.Pastel(extension.NameColor)} has been unloaded by another extension or a melon");
        }

        /// <summary>
        /// Unloads the specified <see cref="ExtensionBase"/>, which will cause it to not be used in checking
        /// <para>Note that this <b>does not</b> unload the <see cref="Assembly"/></para>
        /// </summary>
        public static void Unload(SearchExtension extension, string message, bool printmsg = true)
        {
            if (string.IsNullOrEmpty(extension.ID)) LoadedExtensions.RemoveAll(x => x.Name == extension.Name && x.Author == x.Author);
            else LoadedExtensions.RemoveAll(x => x.Name == extension.Name && x.Author == x.Author && x.ID == extension.ID);
            RottenExtensions.Add(new RottenExtension(extension, "The extension has been unloaded by another extension or a melon"));
            if (printmsg) MelonAutoUpdater.logger._MsgPastel($"Extension {extension.Name.Pastel(extension.NameColor)} has been unloaded by another extension or a melon with the following message: {message}");
        }

        /// <summary>
        /// Unloads the specified <see cref="ExtensionBase"/>, which will cause it to not be used in checking
        /// <para>Note that this <b>does not</b> unload the <see cref="Assembly"/></para>
        /// </summary>
        internal void InternalUnload(Exception exception, bool printmsg = true)
        {
            if (string.IsNullOrEmpty(ID)) LoadedExtensions.RemoveAll(x => x.Name == Name && x.Author == x.Author);
            else LoadedExtensions.RemoveAll(x => x.Name == Name && x.Author == x.Author && x.ID == ID);
            RottenExtensions.Add(new RottenExtension(this, exception));
            if (printmsg) MelonAutoUpdater.logger._MsgPastel($"Extension {Name.Pastel(NameColor)} has been unloaded");
        }

        /// <summary>
        /// Unloads the specified <see cref="ExtensionBase"/>, which will cause it to not be used in checking
        /// <para>Note that this <b>does not</b> unload the <see cref="Assembly"/></para>
        /// </summary>
        internal void InternalUnload(Exception exception, string message, bool printmsg = true)
        {
            if (string.IsNullOrEmpty(ID)) LoadedExtensions.RemoveAll(x => x.Name == Name && x.Author == x.Author);
            else LoadedExtensions.RemoveAll(x => x.Name == Name && x.Author == x.Author && x.ID == ID);
            RottenExtensions.Add(new RottenExtension(this, exception, message));
            if (printmsg) MelonAutoUpdater.logger._MsgPastel($"Extension {Name.Pastel(NameColor)} has been unloaded");
        }

        /// <summary>
        /// Unloads the specified <see cref="ExtensionBase"/>, which will cause it to not be used in checking
        /// <para>Note that this <b>does not</b> unload the <see cref="Assembly"/></para>
        /// </summary>
        internal void InternalUnload(string message, bool printmsg = true)
        {
            if (string.IsNullOrEmpty(ID)) LoadedExtensions.RemoveAll(x => x.Name == Name && x.Author == x.Author);
            else LoadedExtensions.RemoveAll(x => x.Name == Name && x.Author == x.Author && x.ID == ID);
            RottenExtensions.Add(new RottenExtension(this, message));
            if (printmsg) MelonAutoUpdater.logger._MsgPastel($"Extension {Name.Pastel(NameColor)} has been unloaded");
        }

        #endregion Unload

        #region Helper

        /// <summary>
        /// Create preferences category for saving data
        /// </summary>
        /// <param name="category">Optional parameter, name for your category</param>
        /// <returns>Melon Preferences Category</returns>
        public MelonPreferences_Category CreateCategory(string category = "")
        {
            MelonPreferences_Category _Category = MelonPreferences.CreateCategory(!string.IsNullOrEmpty(category) ? category : Name);
            string path = Path.Combine(Files.ExtConfigDirectory, $"{Name}.cfg");
            _Category.SetFilePath(path);
            return _Category;
        }

        /// <summary>
        /// Get value of entry in preferences
        /// </summary>
        /// <typeparam name="T">Type to return</typeparam>
        /// <param name="entry">The entry to get value from</param>
        /// <returns>Value with provided type</returns>
        public static T GetEntryValue<T>(MelonPreferences_Entry entry)
        {
            if (entry != null)
            {
                var value = entry.BoxedValue;
                if (value != null)
                {
                    return (T)value;
                }
                else
                {
                    return default;
                }
            }
            return default;
        }

        /// <summary>
        /// Copied from MelonLoader v0.6.4 to make it work with older versions
        /// </summary>
        internal static bool IsCompatible(SemVersion ver, bool isMinimum, SemVersion version)
       => ver == null || version == null || (isMinimum ? ver <= version : ver == version);

        /// <summary>
        /// Copied from MelonLoader v0.6.4 to make it work with older versions
        /// </summary>
        internal static bool IsCompatible(string ver, bool isMinimum, string version)
            => !SemVersion.TryParse(version, out SemVersion _version) || !SemVersion.TryParse(ver, out SemVersion _ver) || IsCompatible(_ver, isMinimum, _version);

        /// <summary>
        /// Logger to use to display information in console
        /// </summary>
        public MAULogger Logger { get; internal set; }

        /// <summary>
        /// Get current version of MAU
        /// </summary>
        /// <returns><see cref="SemVersion"/> of current MAU version</returns>
        public static SemVersion GetMAUVersion() => SemVersion.Parse(MelonAutoUpdater.Version);

        /// <summary>
        /// Get current version of ML
        /// </summary>
        /// <returns><see cref="SemVersion"/> of current ML version</returns>
        public static SemVersion GetMLVersion() => MelonAutoUpdater.MLVersion;

        #endregion Helper

        #region Static Methods

        /// <summary>
        /// All loaded extensions
        /// </summary>
        public static List<ExtensionBase> LoadedExtensions { get; internal set; } = new List<ExtensionBase>();

        /// <summary>
        /// All extensions that were unloaded due to an exception
        /// </summary>
        public static List<RottenExtension> RottenExtensions { get; internal set; } = new List<RottenExtension>();

        internal static MelonInfoAttribute GetInfoFromAssembly(Assembly assembly)
        {
            var info = assembly.GetCustomAttribute<MelonInfoAttribute>();
            if (info == null)
            {
#pragma warning disable CS0618 // Type or member is obsolete
                var pluginInfo = assembly.GetCustomAttribute<MelonPluginInfoAttribute>();
#pragma warning restore CS0618 // Type or member is obsolete
                if (pluginInfo == null)
                {
                    return null;
                }
                else
                {
#pragma warning disable CS0618 // Type or member is obsolete
                    return new MelonInfoAttribute(pluginInfo.SystemType, pluginInfo.Name, pluginInfo.Version, pluginInfo.Author, pluginInfo.DownloadLink);
#pragma warning restore CS0618 // Type or member is obsolete
                }
            }
            else
            {
                return info;
            }
        }

        internal static MelonIDAttribute GetIDFromAssembly(Assembly assembly)
        {
            var info = assembly.GetCustomAttribute<MelonIDAttribute>();
            return info;
        }

        [System.Runtime.CompilerServices.MethodImpl(
System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        internal static bool IsLoaded(string Name, string Author, string ID = "")
        {
            if (!string.IsNullOrEmpty(ID))
            {
                return MelonBase.RegisteredMelons.Where(x => x.Info.Name == Name && x.Info.Author == Author && x.ID == ID).Any();
            }
            else
            {
                return MelonBase.RegisteredMelons.Where(x => x.Info.Name == Name && x.Info.Author == Author).Any();
            }
        }

        /// <summary>
        /// Get all loaded extensions
        /// </summary>
        /// <returns>A list of <see cref="ExtensionBase"/> objects</returns>
        internal static void LoadExtensions(Assembly[] loadedAssemblies)
        {
            LoadedExtensions = new List<ExtensionBase>();
            foreach (Assembly assembly in loadedAssemblies)
            {
                foreach (Type type in
                    assembly.GetTypes()
                    .Where(myType => myType.IsClass && !myType.IsAbstract))
                {
                    if (type.IsSubclassOf(typeof(SearchExtension)) || type.IsSubclassOf(typeof(InstallExtension)))
                    {
                        var info = GetInfoFromAssembly(assembly);
                        if (info == null) { MelonAutoUpdater.logger.Msg("Attempted to load an extension that is not a Melon, skipping"); return; }
                        else
                        {
                            if (MelonAutoUpdater.MLVersion > new SemVersion(0, 5, 4))
                            {
                                var id = GetIDFromAssembly(assembly);
                                if (id != null)
                                {
                                    if (!IsLoaded(info.Name, info.Author, id.ID))
                                    {
                                        continue;
                                    }
                                }
                                else
                                {
                                    if (!IsLoaded(info.Name, info.Author))
                                    {
                                        continue;
                                    }
                                }
                            }
                        }
                        var obj = (ExtensionBase)Activator.CreateInstance(type);
                        if (obj.RequiredMAUVersion.version != null)
                        {
                            if (!IsCompatible(GetMAUVersion(), obj.RequiredMAUVersion.isMinimum, obj.RequiredMAUVersion.version))
                            {
                                MelonAutoUpdater.logger.Msg(
                                    $"Current MAU version is not compatible with the one required by {obj.Name.Pastel(obj.NameColor)} " + $"v{obj.Version}".Pastel(MelonAutoUpdater.theme.NewVersionColor) + $" (Current is v{GetMAUVersion()}, v{obj.RequiredMAUVersion.version} {(obj.RequiredMAUVersion.isMinimum ? "minimally" : "specifically")})");
                            }
                        }

                        if (obj.RequiredMLVersion.version != null)
                        {
                            if (!IsCompatible(GetMLVersion(), obj.RequiredMLVersion.isMinimum, obj.RequiredMLVersion.version))
                            {
                                MelonAutoUpdater.logger.Msg(
                                    $"Current MAU version is not compatible with the one required by {obj.Name.Pastel(obj.NameColor)} " + $"v{obj.Version}".Pastel(MelonAutoUpdater.theme.NewVersionColor) + $" (Current is v{GetMAUVersion()}, v{obj.RequiredMLVersion.version} {(obj.RequiredMLVersion.isMinimum ? "minimally" : "specifically")})");
                            }
                        }
                        if (string.IsNullOrEmpty(obj.ID))
                        {
                            if (LoadedExtensions.Find(x => x.Name == obj.Name && x.Author == obj.Author) != null)
                            {
                                MelonAutoUpdater.logger.Warning("Found an extension with identical Names & Author to another extension, not loading");
                                continue;
                            }
                        }
                        else
                        {
                            if (LoadedExtensions.Find(x => x.Name == obj.Name && x.Author == obj.Author && x.ID == obj.ID) != null)
                            {
                                MelonAutoUpdater.logger.Warning("Found an extension with identical Names & Author and also ID to another extension, not loading");
                                continue;
                            }
                        }
                        if (type.IsSubclassOf(typeof(SearchExtension)))
                        {
                            var search = (SearchExtension)obj;
                            MelonAutoUpdater.logger._MsgPastel($"Loaded Search Extension: {obj.Name.Pastel(obj.NameColor)} " + $"v{obj.Version}".Pastel(MelonAutoUpdater.theme.NewVersionColor) + $" by {obj.Author.Pastel(obj.AuthorColor)}");
                            LoadedExtensions.Add(search);
                            search.SafeAction(search.Setup);
                            search.SafeAction(search.OnInitialization);
                        }
                        else if (type.IsSubclassOf(typeof(InstallExtension)))
                        {
                            var install = (InstallExtension)obj;
                            MelonAutoUpdater.logger._MsgPastel($"Loaded Install Extension: {obj.Name.Pastel(obj.NameColor)} " + $"v{obj.Version}".Pastel(MelonAutoUpdater.theme.NewVersionColor) + $" by {obj.Author.Pastel(obj.AuthorColor)}");
                            LoadedExtensions.Add(install);
                            install.SafeAction(install.Setup);
                            install.SafeAction(install.OnInitialization);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Checks if assembly is an extension
        /// </summary>
        /// <param name="assembly"><see cref="Assembly"/> to check if is an extension</param>
        /// <returns>If <see langword="true"/>, it is an extension, otherwise, <see langword="false"/></returns>
        public static bool IsExtension(Assembly assembly)
        {
            return assembly.GetTypes()
                .Where(myType => myType.IsClass && !myType.IsAbstract && myType.IsSubclassOf(typeof(SearchExtension))).Any();
        }

        #endregion Static Methods
    }
}