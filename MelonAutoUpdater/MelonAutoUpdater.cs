extern alias ml070;
extern alias ml057;

using ml070::MelonLoader;
using Mono.Cecil;
using System.Collections.Generic;
using System;
using System.IO;
using System.Linq;
using ml070::Semver;
using ml070::MelonLoader.Preferences;
using MelonAutoUpdater.Extensions;
using MelonAutoUpdater.Helper;
using System.Reflection;
using MelonAutoUpdater.Utils;
using System.Net;
using System.Diagnostics;
using MelonAutoUpdater.Config;
using static ml070::MelonLoader.MelonPlatformAttribute;
using static ml070::MelonLoader.MelonPlatformDomainAttribute;

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
        public const string Version = "0.4.0";

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
        internal static Logger logger;

        /// <summary>
        /// If <see langword="true"/>, mods will only be checked the versions and not updated, even when available
        /// </summary>
        private bool dontUpdate = false;

        /// <summary>
        /// Assembly of MelonLoader
        /// </summary>
        internal static Assembly MLAssembly;

        /// <summary>
        /// Version of MelonLoader
        /// </summary>
        public static SemVersion MLVersion;

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
        /// A Melon Preferences entry of a boolean value indicating whether or not should incompatible melons be removed if not updated
        /// </summary>
        internal static MelonPreferences_Entry Entry_removeIncompatible { get; private set; }

        /// <summary>
        /// A Melon Preferences entry of a boolean value indicating whether or not should melons be checked for compatibility
        /// </summary>
        internal static MelonPreferences_Entry Entry_checkCompatibility { get; private set; }

        /// <summary>
        /// A Melon Preferences entry of a boolean value indicating whether or not should the plugin use Pastel (ANSI colors)
        /// </summary>
        internal static MelonPreferences_Entry Entry_usePastel { get; private set; }

        /// <summary>
        /// A Melon Preferences entry of a boolean value indicating whether or not should the plugin be in Debug mode
        /// </summary>
        internal static MelonPreferences_Entry Entry_debug { get; private set; }

        /// <summary>
        /// Themes Category in Preferences
        /// </summary>
        internal static MelonPreferences_ReflectiveCategory ThemesCategory { get; private set; }

        /// <summary>
        /// Setup Preferences
        /// </summary>
        private void SetupPreferences()
        {
            Stopwatch sw = Stopwatch.StartNew();
            // Main Category

            LoggerInstance.DebugMsg("Setting up config.cfg");

            MainCategory = MelonPreferences.CreateCategory("MelonAutoUpdater", "Melon Auto Updater");
            MainCategory.SetFilePath(Path.Combine(Files.MainDirectory, "config.cfg"));

            Entry_enabled = MainCategory.CreateEntry<bool>("Enabled", true, "Enabled",
                description: "If true, Mods & Plugins will update on every start");

            LoggerInstance.DebugMsg("Added Enabled to config.cfg");

            Entry_ignore = MainCategory.CreateEntry<List<string>>("IgnoreList", new List<string>(), "Ignore List",
                description: "List of all file names (without extension) of Mods & Plugins that will be ignored when checking for updates");

            LoggerInstance.DebugMsg("Added IgnoreList to config.cfg");

            Entry_bruteCheck = MainCategory.CreateEntry<bool>("BruteCheck", false, "Brute Check",
                description: "If true, when there's no download link provided with mod/plugin, it will check every supported platform providing the Name & Author\nWARNING: You may get rate-limited with large amounts of mods/plugins, use with caution");

            LoggerInstance.DebugMsg("Added BruteCheck to config.cfg");

            Entry_debug = MainCategory.CreateEntry<bool>("Debug", false, "Debug",
               description: "If true, the plugin will be enabled in Debug mode, providing some possibly useful information");

            LoggerInstance.DebugMsg("Added Debug to config.cfg");

            if (!Debug)
            {
                Debug = (bool)Entry_debug.BoxedValue;
                LoggerInstance.DebugMsg("Debug mode enabled via Preferences");
            }

            Entry_dontUpdate = MainCategory.CreateEntry<bool>("DontUpdate", false, "Don't Update",
                description: "If true, Melons will only be checked if they are outdated or not, they will not be updated automatically");

            LoggerInstance.DebugMsg("Added DontUpdate to config.cfg");

            if (!dontUpdate)
            {
                LoggerInstance.DebugMsg("Don't Update mode enabled via Preferences");
                dontUpdate = (bool)Entry_dontUpdate.BoxedValue;
            }

            Entry_removeIncompatible = MainCategory.CreateEntry<bool>("RemoveIncompatible", false, "Remove Incompatible",
                description: "If true, if incompatible melons are not updated, they will be removed to avoid possible crashes (for example due to a \"Too new\" version of .NET)");

            LoggerInstance.DebugMsg("Added RemoveIncompatible to config.cfg");

            Entry_checkCompatibility = MainCategory.CreateEntry<bool>("CheckCompatibility", true, "Check Compatibility",
                description: "If true, melons will be checked to determine if they are compatible with the install, if not, they will not be installed (if those were the download update melons) or removed if the melon was checked, not updated, incompatible and RemoveIncompatible is true\nWARNING: This may cause some melons to stop working or the game to crash, due to faulty/incompatible versions");

            LoggerInstance.DebugMsg("Added CheckCompatibility to config.cfg");

            Entry_usePastel = MainCategory.CreateEntry<bool>("UsePastel", true, "Use Pastel",
                description: "If true, the plugin will use ANSI colors, but might and most likely will make logs hardly readable");

            LoggerInstance.DebugMsg("Added UsePastel to config.cfg");

            MainCategory.SaveToFile(false);
            LoggerInstance.DebugMsg("Set up config.cfg");

            // Themes Category

            LoggerInstance.DebugMsg("Setting up theme.cfg");

            ThemesCategory = MelonPreferences.CreateCategory<Theme>("Theme", "Theme");
            ThemesCategory.SetFilePath(Path.Combine(Files.MainDirectory, "theme.cfg"));

            theme = ThemesCategory.GetValue<Theme>();
            theme.Setup();

            ThemesCategory.SaveToFile(false);

            LoggerInstance.DebugMsg("Set up theme.cfg");

            if (Debug)
            {
                sw.Stop();
                ElapsedTime.Add("SetupPreferences", sw.ElapsedMilliseconds);
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
            if (entry?.BoxedValue != null)
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
            var args = ml070.MelonLoader.MelonLaunchOptions.ExternalArguments;

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
            if (customAttribute.ConstructorArguments.Count == 0) return default;
            return (T)customAttribute.ConstructorArguments[index].Value;
        }

        [System.Runtime.CompilerServices.MethodImpl(
    System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private bool IsMLDebug060() => LoaderConfig.Current.Loader.DebugMode;

        [System.Runtime.CompilerServices.MethodImpl(
   System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private bool IsMLDebug057() => ml057.MelonLoader.MelonLaunchOptions.Debug.Enabled;

        private static NuGet NuGet = new();

        // Note to self: Don't use async
        /// <summary>
        /// Runs before MelonLoader fully initializes
        /// </summary>
        public override void OnPreInitialization()
        {
            logger = new Logger();
            logger.Log += Log;

            MLAssembly = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(x => x.GetName().Name == "MelonLoader");
            Version MelonLoaderVersion = MLAssembly.GetName().Version;
            MLVersion = new SemVersion(MelonLoaderVersion.Major, MelonLoaderVersion.Minor, MelonLoaderVersion.Build);

            if (MLVersion >= new SemVersion(0, 6, 5))
            {
                LoggerInstance.Msg("Checking command line arguments");
                HandleArguments();
            }
            else
            {
                LoggerInstance.Msg($"Could not check command line arguments due to outdated MelonLoader version (Current is v{MelonLoaderVersion.Major}.{MelonLoaderVersion.Minor}.{MelonLoaderVersion.Build}, required is minimum v0.6.5)");
            }

            if (stopPlugin) return;

            if (MLVersion >= new SemVersion(0, 6, 0))
            {
                if (IsMLDebug060()) Debug = true;
            }
            else if (MLVersion == new SemVersion(0, 5, 7))
            {
                if (IsMLDebug057()) Debug = true;
            }

            Stopwatch sw = Stopwatch.StartNew();

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
                LoggerInstance.Warning("Internet is not connected, aborting");
                return;
            }

            UserAgent = $"{this.Info.Name}/{Version} Auto-Updater for ML mods";
            SearchExtension.UserAgent = UserAgent;

            LoggerInstance.Msg("Setting up Melon Preferences");

            SetupPreferences();

            if (!GetEntryValue<bool>(Entry_usePastel)) ConsoleExtensions.Disable();

            bool enabled = GetEntryValue<bool>(Entry_enabled);

            if (!enabled)
            {
                LoggerInstance.Msg("Plugin disabled in preferences, aborting..");
                return;
            }

            LoggerInstance.Msg("Adding placeholders for config");

#pragma warning disable CS0618 // Type or member is obsolete
            var gameName = ml070.MelonLoader.InternalUtils.UnityInformationHandler.GameName;
            var gameDev = ml070.MelonLoader.InternalUtils.UnityInformationHandler.GameDeveloper;
            var gameVer = ml070.MelonLoader.InternalUtils.UnityInformationHandler.GameVersion;
#pragma warning restore CS0618 // Type or member is obsolete

            CompatiblePlatforms CurrentPlatform = MelonUtils.IsGame32Bit() ? CompatiblePlatforms.WINDOWS_X86 : CompatiblePlatforms.WINDOWS_X64; // Temporarily
            CompatibleDomains CurrentDomain = MelonUtils.IsGameIl2Cpp() ? CompatibleDomains.IL2CPP : CompatibleDomains.MONO;

            ContentType.Load();

            LoggerInstance.Msg("Installing SharpZipLib if necessary");
            var assembly = Assembly.GetExecutingAssembly();
            if (!AppDomain.CurrentDomain.GetAssemblies().Any(x => x.GetName().Name.Equals("ICSharpCode.SharpZipLib", StringComparison.CurrentCultureIgnoreCase)))
            {
                LoggerInstance.Msg("Installing...");
                const string path = "{0}.Dependencies.ICSharpCode.SharpZipLib.dll";
                string filePath = Path.Combine(ml070.MelonLoader.Utils.MelonEnvironment.UserLibsDirectory, "ICSharpCode.SharpZipLib.dll");
                using Stream stream = assembly.GetManifestResourceStream(string.Format(path, assembly.GetName().Name));
                using FileStream fileStream = File.Create(filePath);
                stream.CopyTo(fileStream);
                fileStream.Close();
                Assembly.LoadFile(filePath);
            }

            LoggerInstance.Msg("Setting up search extensions");
            SearchExtension.LoadExtensions(AppDomain.CurrentDomain.GetAssemblies());

            var updater = new MelonUpdater(UserAgent, theme, GetEntryValue<List<string>>(Entry_ignore), logger, GetEntryValue<bool>(Entry_bruteCheck));

            LoggerInstance.Msg("Checking plugins...");
            updater.CheckDirectory(Files.PluginsDirectory);
            LoggerInstance.Msg("Done checking plugins");

            LoggerInstance.Msg("Checking mods...");
            updater.CheckDirectory(Files.ModsDirectory);
            LoggerInstance.Msg("Done checking mods");

            if (Debug)
            {
                sw.Stop();
                LoggerInstance.DebugMsg($"The plugin took {sw.ElapsedMilliseconds} ms to complete");
                LoggerInstance.DebugWarning("Please note that processes such as command line arguments are not taken into consideration, due to the required Debug mode enabled");
                LoggerInstance.DebugWarning("Pausing the console (selecting text on the console) will affect the time and will not be accurate to how it would actually perform");
                LoggerInstance.DebugMsg("List of all processes and how long it took for them to complete:");
                foreach (var diagnostics in ElapsedTime)
                {
                    LoggerInstance.DebugMsg($"{diagnostics.Key}: {diagnostics.Value} ms");
                }
            }
        }

        private void Log(object sender, LogEventArgs e)
        {
            switch (e.Severity)
            {
                case Logger.LogSeverity.MESSAGE:
                    LoggerInstance._MsgPastel(e.Message);
                    break;

                case Logger.LogSeverity.WARNING:
                    LoggerInstance.Warning(e.Message);
                    break;

                case Logger.LogSeverity.ERROR:
                    LoggerInstance.Error(e.Message);
                    break;

                case Logger.LogSeverity.DEBUG:
                    LoggerInstance.DebugMsgPastel(e.Message);
                    break;

                case Logger.LogSeverity.DEBUG_WARNING:
                    LoggerInstance.DebugWarning(e.Message);
                    break;

                case Logger.LogSeverity.DEBUG_ERROR:
                    LoggerInstance.DebugError(e.Message);
                    break;
            }
        }
    }
}