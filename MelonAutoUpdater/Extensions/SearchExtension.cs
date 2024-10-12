extern alias ml065;

using ml065.Semver;
using ml065.MelonLoader;
using MelonAutoUpdater.Helper;
using System;

namespace MelonAutoUpdater.Extensions
{
    /// <summary>
    /// Class to derive from to create search extensions<br/>
    /// Search Extensions are provided a URL and should get necessary information (Latest Version, File Data) if possible
    /// </summary>
    public abstract class SearchExtension : ExtensionBase
    {
        internal override Type Type => typeof(SearchExtension);

        #region Extension Info

        /// <summary>
        /// If true, the brute check event will be called
        /// </summary>
        public virtual bool BruteCheckEnabled
        { get { return false; } }

        /// <summary>
        /// Link to the platform that the Search Extension supports
        /// </summary>
        public abstract string Link { get; }

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
        internal new void Setup()
        {
            MelonAutoUpdater.logger.DebugMsg($"Setting up logger for {Name}");
            Logger = new MAULogger(Name, ID);

            Logger.DebugMsg("Creating category");
            Internal_Category = CreateCategory($"{Name}_Internal_Settings");
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

        #endregion Extension Methods

        #region Helper

        /// <summary>
        /// User Agent Header for all HTTP requests
        /// </summary>
        public static string UserAgent { get; internal set; }

        #endregion Helper
    }
}