using Semver;
using System.Collections.Generic;
using System.Reflection;
using System;
using System.Linq;
using System.Drawing;
using MelonLoader;
using System.IO;
using MelonAutoUpdater.Utils;
using MelonAutoUpdater.Helper;

namespace MelonAutoUpdater.Search
{
    /// <summary>
    /// Class to derive from to create search extensions<br/>
    /// Search Extensions are provided a URL and should get necessary information (Latest Version, File Data) if possible
    /// </summary>
    public abstract class MAUExtension
    {
        #region Extension Info

        /// <summary>
        /// Name of the MAU Search Extension that will be displayed in console
        /// </summary>
        public abstract string Name { get; }

        /// <summary>
        /// Version of the MAU Search Extension that will be displayed in the console
        /// </summary>
        public abstract SemVersion Version { get; }

        /// <summary>
        /// Author of the MAU Search Extension that will be displayed in the console
        /// </summary>
        public abstract string Author { get; }

        /// <summary>
        /// Link to the platform that the MAU Search Extension supports
        /// </summary>
        public abstract string Link { get; }

        /// <summary>
        /// <see cref="Color"/> that should be displayed with the Author of the MAU Search Extension
        /// </summary>
        public virtual Color AuthorColor
        { get { return Color.LightBlue; } }

        /// <summary>
        /// <see cref="Color"/> that should be displayed with the Name of the MAU Search Extension
        /// </summary>
        public virtual Color NameColor
        { get { return Color.LightBlue; } }

        /// <summary>
        /// If true, the brute check event will be called
        /// </summary>
        public virtual bool BruteCheckEnabled
        { get { return false; } }

        #endregion Extension Info

        #region Internal MelonPreferences

        internal MelonPreferences_Category Internal_Category;

        internal MelonPreferences_Entry Entry_Enabled;
        internal MelonPreferences_Entry Entry_BruteCheckEnabled;

        #endregion Internal MelonPreferences

        #region Extension Methods

        /// <summary>
        /// Called when the extension needs to perform a search with provided URL
        /// </summary>
        /// <param name="url">URL retrieved from mod/plugin that needs to be checked</param>
        /// <param name="currentVersion">Current version of the mod/plugin</param>
        /// <returns><see cref="MelonData"/> if able to retrieve information from link, otherwise <see langword="null"/></returns>
        public abstract MelonData Search(string url, SemVersion currentVersion);

        /// <summary>
        /// Called when the extension needs to perform a search with provided Author and Name
        /// </summary>
        /// <param name="name">Name provided with mod/plugin being checked</param>
        /// <param name="author">Author provided with mod/plugin being checked</param>
        /// <param name="currentVersion">Current version of mod/plugin</param>
        /// <returns><see cref="MelonData"/> if able to retrieve information from name and author, otherwise <see langword="null"/></returns>
        public virtual MelonData BruteCheck(string name, string author, SemVersion currentVersion)
        {
            return null;
        }

        /// <summary>
        /// Configure necessary things in extension
        /// </summary>
        internal void Setup()
        {
            MelonAutoUpdater.logger.DebugMsg($"Setting up logger for {Name}");
            Logger = new MAULogger(Name);

            Logger.DebugMsg(Internal_Category != null ? $"Category exists, {Internal_Category.Identifier}" : "Category does not exist");

            Logger.DebugMsg("Creating category");
            Internal_Category = CreateCategory($"{Name}_Internal_Settings");
            Logger.DebugMsg($"Entries: {Internal_Category.Entries.Count}");
            Logger.DebugMsg("Created category");

            Logger.DebugMsg("Creating entry 'Enabled'");
            Entry_Enabled = Internal_Category.CreateEntry<bool>("Enabled", true, "Enabled",
                description: "If true, the extension will be enabled, by default its true");
            Logger.DebugMsg("Created entry 'Enabled'");

            Logger.DebugMsg("Creating entry 'BruteCheckEnabled'");
            Entry_BruteCheckEnabled = Internal_Category.CreateEntry<bool>("BruteCheckEnabled", BruteCheckEnabled, "Brute Check Enabled",
                description: "If true, the extension will be used in brute checks if set up");
            Logger.DebugMsg("Created entry 'BruteCheckEnabled'");

            Internal_Category.SaveToFile(false);
        }

        #region Unload

        /// <summary>
        /// Unloads the specified <see cref="MAUExtension"/>, which will cause it to not be used in checking
        /// <para>Note that this <b>does not</b> unload the <see cref="Assembly"/></para>
        /// </summary>
        public void Unload(bool printmsg = true)
        {
            LoadedExtensions.Remove(this);
            RottenExtensions.Add(new RottenExtension(this, "The extension has been unloaded by another extension or a melon"));
            if (printmsg) MelonAutoUpdater.logger._MsgPastel($"Extension {Name.Pastel(NameColor)} has been unloaded by another extension or a melon");
        }

        /// <summary>
        /// Unloads the specified <see cref="MAUExtension"/>, which will cause it to not be used in checking
        /// <para>Note that this <b>does not</b> unload the <see cref="Assembly"/></para>
        /// </summary>
        public void Unload(string message, bool printmsg = true)
        {
            LoadedExtensions.Remove(this);
            RottenExtensions.Add(new RottenExtension(this, $"The extension has been unloaded by another extension or a melon, reason provided: {message}"));
            if (printmsg) MelonAutoUpdater.logger._MsgPastel($"Extension {Name.Pastel(NameColor)} has been unloaded by another extension or a melon with the following message: {message}");
        }

        /// <summary>
        /// Unloads the specified <see cref="MAUExtension"/>, which will cause it to not be used in checking
        /// <para>Note that this <b>does not</b> unload the <see cref="Assembly"/></para>
        /// </summary>
        public static void Unload(MAUExtension extension, bool printmsg = true)
        {
            LoadedExtensions.Remove(extension);
            RottenExtensions.Add(new RottenExtension(extension, "The extension has been unloaded by another extension or a melon"));
            if (printmsg) MelonAutoUpdater.logger._MsgPastel($"Extension {extension.Name.Pastel(extension.NameColor)} has been unloaded by another extension or a melon");
        }

        /// <summary>
        /// Unloads the specified <see cref="MAUExtension"/>, which will cause it to not be used in checking
        /// <para>Note that this <b>does not</b> unload the <see cref="Assembly"/></para>
        /// </summary>
        public static void Unload(MAUExtension extension, string message, bool printmsg = true)
        {
            LoadedExtensions.Remove(extension);
            RottenExtensions.Add(new RottenExtension(extension, "The extension has been unloaded by another extension or a melon"));
            if (printmsg) MelonAutoUpdater.logger._MsgPastel($"Extension {extension.Name.Pastel(extension.NameColor)} has been unloaded by another extension or a melon with the following message: {message}");
        }

        /// <summary>
        /// Unloads the specified <see cref="MAUExtension"/>, which will cause it to not be used in checking
        /// <para>Note that this <b>does not</b> unload the <see cref="Assembly"/></para>
        /// </summary>
        internal void InternalUnload(Exception exception, bool printmsg = true)
        {
            LoadedExtensions.Remove(this);
            RottenExtensions.Add(new RottenExtension(this, exception));
            if (printmsg) MelonAutoUpdater.logger._MsgPastel($"Extension {Name.Pastel(NameColor)} has been unloaded");
        }

        /// <summary>
        /// Unloads the specified <see cref="MAUExtension"/>, which will cause it to not be used in checking
        /// <para>Note that this <b>does not</b> unload the <see cref="Assembly"/></para>
        /// </summary>
        internal void InternalUnload(Exception exception, string message, bool printmsg = true)
        {
            LoadedExtensions.Remove(this);
            RottenExtensions.Add(new RottenExtension(this, exception, message));
            if (printmsg) MelonAutoUpdater.logger._MsgPastel($"Extension {Name.Pastel(NameColor)} has been unloaded");
        }

        /// <summary>
        /// Unloads the specified <see cref="MAUExtension"/>, which will cause it to not be used in checking
        /// <para>Note that this <b>does not</b> unload the <see cref="Assembly"/></para>
        /// </summary>
        internal void InternalUnload(string message, bool printmsg = true)
        {
            LoadedExtensions.Remove(this);
            RottenExtensions.Add(new RottenExtension(this, message));
            if (printmsg) MelonAutoUpdater.logger._MsgPastel($"Extension {Name.Pastel(NameColor)} has been unloaded");
        }

        #endregion Unload

        /// <summary>
        /// Called when extension is loaded into the plugin
        /// </summary>
        public virtual void OnInitialization()
        { }

        #endregion Extension Methods

        #region Helper

        /// <summary>
        /// Create preferences category for saving data
        /// </summary>
        /// <param name="category">Optional parameter, name for your category</param>
        /// <returns>Melon Preferences Category</returns>
        public MelonPreferences_Category CreateCategory(string category = "")
        {
            MelonPreferences_Category _Category = MelonPreferences.CreateCategory(!string.IsNullOrEmpty(category) ? category : Name);
            string path = Path.Combine(Files.ExtConfigFolder, $"{Name}.cfg");
            _Category.SetFilePath(path);
            Logger.DebugMsg($"Path: {path}");
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
                return MelonPreferences.GetEntryValue<T>(entry.Category.Identifier, entry.Identifier);
            }
            return default;
        }

        /// <summary>
        /// User Agent Header for all HTTP requests
        /// </summary>
        public static string UserAgent { get; internal set; }

        /// <summary>
        /// Logger to use to display information in console
        /// </summary>
        public MAULogger Logger { get; internal set; }

        /// <summary>
        /// Get current version of MAU
        /// </summary>
        /// <returns><see cref="SemVersion"/> of current MAU version</returns>
        public static string GetMAUVersion() => MelonAutoUpdater.Version;

        #endregion Helper

        #region Static Methods

        /// <summary>
        /// All loaded extensions
        /// </summary>
        public static List<MAUExtension> LoadedExtensions { get; internal set; } = new List<MAUExtension>();

        /// <summary>
        /// All extensions that were unloaded due to an exception
        /// </summary>
        public static List<RottenExtension> RottenExtensions { get; internal set; } = new List<RottenExtension>();

        /// <summary>
        /// Get all loaded extensions
        /// </summary>
        /// <returns>A list of <see cref="MAUExtension"/> objects</returns>
        internal static void LoadExtensions(Assembly[] loadedAssemblies)
        {
            LoadedExtensions = new List<MAUExtension>();
            foreach (Assembly assembly in loadedAssemblies)
            {
                foreach (Type type in
                    assembly.GetTypes()
                    .Where(myType => myType.IsClass && !myType.IsAbstract && myType.IsSubclassOf(typeof(MAUExtension))))
                {
                    var obj = (MAUExtension)Activator.CreateInstance(type);
                    if (LoadedExtensions.Find(x => x.Name == obj.Name && x.Author == obj.Author) != null)
                    {
                        MelonAutoUpdater.logger.Warning("Found an extension with identical Names & Author to another extension, not loading");
                        continue;
                    }
                    MelonAutoUpdater.logger._MsgPastel($"Loaded Search Extension: {obj.Name.Pastel(obj.NameColor)} " + $"v{obj.Version}".Pastel(MelonAutoUpdater.theme.NewVersionColor) + $" by {obj.Author.Pastel(obj.AuthorColor)}");
                    obj.SafeAction(obj.Setup);
                    obj.SafeAction(obj.OnInitialization);
                    LoadedExtensions.Add(obj);
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
                .Where(myType => myType.IsClass && !myType.IsAbstract && myType.IsSubclassOf(typeof(MAUExtension))).Any();
        }

        #endregion Static Methods
    }
}