using MelonLoader;
using Mono.Cecil;
using System.Collections.Generic;
using System;
using System.IO;
using System.Linq;
using Semver;
using MelonLoader.Preferences;
using MelonAutoUpdater.Search;
using MelonAutoUpdater.Helper;
using System.Reflection;
using MelonAutoUpdater.Utils;
using System.Net;
using System.Diagnostics;

namespace MelonAutoUpdater
{
    /// <summary>
    /// Class that contains most of MelonAutoUpdater's functionality
    /// </summary>
    public class MelonAutoUpdater : MelonPlugin
    {
        /// <summary>
        /// Version of MAU
        /// </summary>
        public const string Version = "0.3.1";

        /// <summary>
        /// User Agent Header for all HTTP requests
        /// </summary>
        public static string UserAgent { get; private set; }

        /// <summary>
        /// Customizable colors, why does it exist? I don't know
        /// </summary>
        internal static Theme theme = Theme.Instance;

        /// <summary>
        /// Instance of <see cref="MelonLogger"/>
        /// </summary>
        internal static MelonLogger.Instance logger;

        /// <summary>
        /// List of MAU Search Extensions
        /// </summary>
        internal IEnumerable<MAUSearch> extensions;

        /// <summary>
        /// If <see langword="true"/>, mods will only be checked the versions and not updated, even when available
        /// </summary>
        private bool dontUpdate = false;

        /// <summary>
        /// Assembly of MelonLoader
        /// </summary>
        internal static Assembly MLAssembly;

        /// <summary>
        /// If <see langword="true"/>, debug mode is enabled
        /// </summary>
#if DEBUG
        public static bool Debug { get; internal set; } = true;
#else
        public static bool Debug { get; internal set; } = false;
#endif

        /// <summary>
        /// Variable used to debug how long it took for certain processes to complete
        /// </summary>
        public static Dictionary<string, long> ElapsedTime = new Dictionary<string, long>();

        #region Melon Preferences

        /// <summary>
        /// Main Category in Preferences
        /// </summary>
        internal static MelonPreferences_Category MainCategory { get; private set; }

        /// <summary>
        /// A Melon Preferences entry of a list of mods/plugins that will not be updated
        /// </summary>
        internal static MelonPreferences_Entry Entry_ignore { get; private set; }

        /// <summary>
        /// A Melon Preferences entry of a boolean value indicating whether or not should the plugin work
        /// </summary>
        internal static MelonPreferences_Entry Entry_enabled { get; private set; }

        /// <summary>
        /// A Melon Preferences entry of a boolean value indicating whether or not it should forcefully check the API for the mod/plugins if no download link was provided with it
        /// </summary>
        internal static MelonPreferences_Entry Entry_bruteCheck { get; private set; }

        /// <summary>
        /// A Melon Preferences entry of a boolean value indicating whether or not should the melons be updated if available
        /// </summary>
        internal static MelonPreferences_Entry Entry_dontUpdate { get; private set; }

        /// <summary>
        /// Themes Category in Preferences
        /// </summary>
        internal static MelonPreferences_ReflectiveCategory ThemesCategory { get; private set; }

        /// <summary>
        /// Extensions Category in Preferences
        /// </summary>
        internal static MelonPreferences_Category ExtensionsCategory { get; private set; }

        /// <summary>
        /// Dictionary of included extensions and their Enable entry
        /// </summary>
        internal static Dictionary<MAUSearch, MelonPreferences_Entry> IncludedExtEntries { get; private set; } = new Dictionary<MAUSearch, MelonPreferences_Entry>();

        /// <summary>
        /// Setup Preferences
        /// </summary>
        private void SetupPreferences()
        {
            Stopwatch sw = null;
            if (MelonAutoUpdater.Debug)
            {
                sw = Stopwatch.StartNew();
            }
            // Main Category

            LoggerInstance.DebugMsg("Setting up config.cfg");

            MainCategory = MelonPreferences.CreateCategory("MelonAutoUpdater", "Melon Auto Updater");
            MainCategory.SetFilePath(Path.Combine(Files.MainFolder, "config.cfg"));

            Entry_enabled = MainCategory.CreateEntry<bool>("Enabled", true, "Enabled",
                description: "If true, Mods & Plugins will update on every start");

            LoggerInstance.DebugMsg($"Added Enabled to config.cfg");

            Entry_ignore = MainCategory.CreateEntry<List<string>>("IgnoreList", new List<string>(), "Ignore List",
                description: "List of all names of Mods & Plugins that will be ignored when checking for updates");

            LoggerInstance.DebugMsg($"Added IgnoreList to config.cfg");

            Entry_bruteCheck = MainCategory.CreateEntry<bool>("BruteCheck", false, "Brute Check",
                description: "If true, when there's no download link provided with mod/plugin, it will check every supported platform providing the Name & Author\nWARNING: You may get rate-limited with large amounts of mods/plugins, use with caution");

            LoggerInstance.DebugMsg($"Added BruteCheck to config.cfg");

            Entry_dontUpdate = MainCategory.CreateEntry<bool>("DontUpdate", false, "Dont Update",
                description: "If true, Melons will only be checked if they are outdated or not, they will not be updated automatically");

            LoggerInstance.DebugMsg($"Added DontUpdate to config.cfg");

            if (dontUpdate == false)
            {
                LoggerInstance.DebugMsg($"Don't Update mode enabled via Preferences");
                dontUpdate = (bool)Entry_dontUpdate.BoxedValue;
            }

            MainCategory.SaveToFile(false);
            LoggerInstance.DebugMsg("Set up config.cfg");

            // Themes Category

            LoggerInstance.DebugMsg("Setting up theme.cfg");

            ThemesCategory = MelonPreferences.CreateCategory<Theme>("Theme", "Theme");
            ThemesCategory.SetFilePath(Path.Combine(Files.MainFolder, "theme.cfg"));
            ThemesCategory.SaveToFile(false);

            LoggerInstance.DebugMsg("Set up theme.cfg");

            // Extensions Category

            LoggerInstance.DebugMsg("Setting up extensions.cfg");

            ExtensionsCategory = MelonPreferences.CreateCategory("Extensions", "Extensions");
            ExtensionsCategory.SetFilePath(Path.Combine(Files.MainFolder, "extensions.cfg"));

            foreach (Type type in
                    Assembly.GetExecutingAssembly().GetTypes()
                    .Where(myType => myType.IsClass && !myType.IsAbstract && myType.IsSubclassOf(typeof(MAUSearch))))
            {
                var obj = (MAUSearch)Activator.CreateInstance(type);
                MelonPreferences_Entry entry = ExtensionsCategory.CreateEntry<bool>($"{obj.Name}_Enabled", true, $"{obj.Name} Enabled",
                    description: $"If true, {obj.Name} will be used in searches");
                IncludedExtEntries.Add(obj, entry);
                LoggerInstance.DebugMsg($"Added {obj.Name}_Enabled to extensions.cfg");
            }

            ExtensionsCategory.SaveToFile(false);

            LoggerInstance.DebugMsg("Set up extensions.cfg");

            if (MelonAutoUpdater.Debug)
            {
                sw.Stop();
                MelonAutoUpdater.ElapsedTime.Add($"SetupPreferences", sw.ElapsedMilliseconds);
            }

            LoggerInstance.Msg("Successfully set up Melon Preferences!");
        }

        /// <summary>
        /// Get value of an entry in Melon Preferences
        /// </summary>
        /// <typeparam name="T">A type that will be returned as value of entry</typeparam>
        /// <param name="entry">The Melon Preferences Entry to retrieve value from</param>
        /// <returns>Value of entry with inputted type</returns>

        internal static T GetEntryValue<T>(MelonPreferences_Entry entry)
        {
            if (entry != null && entry.BoxedValue != null)
            {
                try
                {
                    return (T)entry.BoxedValue;
                }
                catch (InvalidCastException)
                {
                    logger.Error($"Preference '{entry.DisplayName}' is of incorrect type");
                    return default;
                }
            }
            return default;
        }

        #endregion Melon Preferences

        #region Command Line Arguments

        /// <summary>
        /// If <see langword="true"/>, plugin will not continue execution
        /// </summary>
        private bool stopPlugin = false;

        [System.Runtime.CompilerServices.MethodImpl(
    System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private void HandleArguments()
        {
            var args = MelonLaunchOptions.ExternalArguments;

            // Disable argument - melonautoupdater.disable
            if (args.ContainsKey("melonautoupdater.disable"))
            {
                LoggerInstance.Msg("Disable argument found, disabling plugin..");
                stopPlugin = true;
            }

            // Debug argument - melonautoupdater.debug
            if (args.ContainsKey("melonautoupdater.debug"))
            {
                LoggerInstance.Msg("Debug argument found, turning on DEBUG mode");
                Debug = true;
            }

            // Don't Update argument - melonautoupdater.dontupdate

            if (args.ContainsKey("melonautoupdater.dontupdate"))
            {
                LoggerInstance.Msg("DontUpdate argument found, will only check versions");
                dontUpdate = true;
            }
        }

        #endregion Command Line Arguments

        // If you are wondering, this is from StackOverflow, although a bit edited, I'm just a bit lazy
        /// <summary>
        /// Checks for internet connection
        /// </summary>
        /// <param name="url">URL of the website used to check for connection (Default: <c>http://www.gstatic.com/generate_204</c>)</param>
        /// <returns>If <see langword="true"/>, there's internet connection, otherwise <see langword="false"/></returns>
        public static bool CheckForInternetConnection(string url = "http://www.gstatic.com/generate_204")
        {
            try
            {
                var request = new WebClient();
                request.DownloadData(url);
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Get value from a custom attribute
        /// </summary>
        /// <typeparam name="T"><see cref="Type"/> that will be returned as value</typeparam>
        /// <param name="customAttribute">The custom attribute you want to get value from</param>
        /// <param name="index">Index of the value</param>
        /// <returns>A value from the Custom Attribute with provided <see cref="Type"/></returns>
        internal static T Get<T>(CustomAttribute customAttribute, int index)
        {
            if (customAttribute.ConstructorArguments.Count <= 0) return default;
            return (T)customAttribute.ConstructorArguments[index].Value;
        }

        // Note to self: Don't use async
        /// <summary>
        /// Runs before MelonLoader fully initializes
        /// </summary>
        public override void OnPreInitialization()
        {
            MLAssembly = AppDomain.CurrentDomain.GetAssemblies().Where(x => x.GetName().Name == "MelonLoader").FirstOrDefault();
            Version MelonLoaderVersion = MLAssembly.GetName().Version;

            if (new SemVersion(MelonLoaderVersion.Major, MelonLoaderVersion.Minor, MelonLoaderVersion.Build) >= new SemVersion(0, 6, 5))
            {
                LoggerInstance.Msg("Checking command line arguments");
                HandleArguments();
            }
            else
            {
                LoggerInstance.Msg($"Could not check command line arguments due to outdated MelonLoader version (Current is v{MelonLoaderVersion.Major}.{MelonLoaderVersion.Minor}.{MelonLoaderVersion.Build}, required is minimum v0.6.5)");
            }

            if (stopPlugin) return;

            if (MelonLaunchOptions.Core.IsDebug) Debug = true;

            Stopwatch sw = null;

            if (Debug)
            {
                sw = Stopwatch.StartNew();
            }

            LoggerInstance.Msg("Creating folders in UserData");

            Files.Setup();

            LoggerInstance.Msg("Clearing possibly left temporary files");

            Files.Clear(TempDirectory.Melons);

            LoggerInstance.Msg("Checking if internet is connected");
            var internetConnected = CheckForInternetConnection();
            if (internetConnected)
            {
                LoggerInstance.Msg("Internet is connected!");
            }
            else
            {
                LoggerInstance.Msg("Internet is not connected, aborting");
                return;
            }

            logger = LoggerInstance;
            UserAgent = $"{this.Info.Name}/{Version} Auto-Updater for ML mods";
            MAUSearch.UserAgent = UserAgent;

            LoggerInstance.Msg("Setup Melon Preferences");

            SetupPreferences();

            theme = ThemesCategory.GetValue<Theme>();

            bool enabled = GetEntryValue<bool>(Entry_enabled);

            if (!enabled)
            {
                LoggerInstance.Msg("Plugin disabled in preferences, aborting..");
                return;
            }

            ContentType.Load();

            LoggerInstance.Msg("Setting up search extensions");
            extensions = MAUSearch.GetExtensions(AppDomain.CurrentDomain.GetAssemblies());

#pragma warning disable CS0618 // Type or member is obsolete
            string pluginsDir = Path.Combine(MelonUtils.BaseDirectory, "Plugins");
            string modsDir = Path.Combine(MelonUtils.BaseDirectory, "Mods");
#pragma warning restore CS0618 // Type or member is obsolete

            var updater = new MelonUpdater(extensions, UserAgent, theme, GetEntryValue<List<string>>(Entry_ignore), GetEntryValue<bool>(Entry_bruteCheck));

            LoggerInstance.Msg("Checking plugins...");
            updater.CheckDirectory(pluginsDir, false);
            LoggerInstance.Msg("Done checking plugins");

            LoggerInstance.Msg("Checking mods...");
            updater.CheckDirectory(modsDir);
            LoggerInstance.Msg("Done checking mods");

            if (Debug)
            {
                sw.Stop();
                LoggerInstance.DebugMsg($"The plugin took {sw.ElapsedMilliseconds} ms to complete");
                LoggerInstance.DebugWarning("Please note that processes such as command line arguments are not taken into consideration, due to the required Debug mode enabled");
                LoggerInstance.DebugWarning("Pausing the console (selecting text on the console) will affect the time and will not be accurate to how it would actually perform");
                LoggerInstance.DebugMsg($"List of all processes and how long it took for them to complete:");
                foreach (var diagnostics in ElapsedTime)
                {
                    LoggerInstance.DebugMsg($"{diagnostics.Key}: {diagnostics.Value} ms");
                }
            }
        }
    }
}