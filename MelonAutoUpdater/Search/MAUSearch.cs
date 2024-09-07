using Semver;
using System.Collections.Generic;
using System.Reflection;
using System;
using System.Threading.Tasks;
using System.Linq;
using System.Drawing;
using MelonLoader;
using System.IO;

namespace MelonAutoUpdater.Search
{
    /// <summary>
    /// Class to derive from to create search extensions<br/>
    /// Search Extensions are provided a URL and should get necessary information (Latest Version, File Data) if possible
    /// </summary>
    public abstract class MAUSearch
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
        /// Color that should be displayed with the Author of the MAU Search Extension
        /// </summary>
        public virtual Color AuthorColor
        { get { return Color.LightBlue; } }

        /// <summary>
        /// Color that should be displayed with the Name of the MAU Search Extension
        /// </summary>
        public virtual Color NameColor
        { get { return Color.LightBlue; } }

        #endregion Extension Info

        #region Extension Methods

        /// <summary>
        /// Called when the extension needs to perform a search with provided URL
        /// </summary>
        /// <param name="url">URL retrieved from mod/plugin that needs to be checked</param>
        /// <returns>ModData if able to retrieve information from link, otherwise <c>null</c></returns>
        public abstract Task<ModData> Search(string url);

        /// <summary>
        /// Configure necessary things in extension
        /// </summary>
        internal void Setup()
        {
            Logger = new MAULogger(Name);
        }

        /// <summary>
        /// Called when extension is loaded into the plugin
        /// </summary>
        public virtual void OnInitialization()
        { }

        #endregion Extension Methods

        #region Helper

        /// <summary>
        /// Returns a task will a null ModData result
        /// </summary>
        /// <returns>Just like I said</returns>
        public static Task<ModData> ReturnEmpty()
        {
            TaskCompletionSource<ModData> source = new TaskCompletionSource<ModData>();
            source.SetResult(null);
            return source.Task;
        }

        public MelonPreferences_Category CreateCategory(string category = "")
        {
            MelonPreferences_Category _Category = MelonPreferences.CreateCategory(!string.IsNullOrEmpty(category) ? category : Name);
            _Category.SetFilePath(Path.Combine(Core.extConfigFolderPath, $"{Name}.cfg"));
            return _Category;
        }

        /// <summary>
        /// User Agent Header for all HTTP requests
        /// </summary>
        public static string UserAgent { get; internal set; }

        /// <summary>
        /// Logger to use to display information in console
        /// </summary>
        public MAULogger Logger { get; internal set; }

        #endregion Helper

        #region Static Methods

        /// <summary>
        /// Get all loaded extensions
        /// </summary>
        /// <returns>A list of MAUSearch objects</returns>
        public static IEnumerable<MAUSearch> GetExtensions(Assembly[] loadedAssemblies)
        {
            List<MAUSearch> objects = new List<MAUSearch>();
            foreach (Assembly assembly in loadedAssemblies)
            {
                foreach (Type type in
                    assembly.GetTypes()
                    .Where(myType => myType.IsClass && !myType.IsAbstract && myType.IsSubclassOf(typeof(MAUSearch))))
                {
                    var obj = (MAUSearch)Activator.CreateInstance(type);
                    objects.Add(obj);
                    Core.logger.Msg($"Loaded MAU Search Extension: {obj.Name.Pastel(obj.NameColor)} " + $"v{obj.Version}".Pastel(Core.theme.NewVersionColor) + $" by {obj.Author.Pastel(obj.AuthorColor)}");
                    obj.OnInitialization();
                }
            }
            return objects;
        }

        /// <summary>
        /// Checks if assembly is an extension
        /// </summary>
        /// <param name="assembly"></param>
        /// <returns></returns>
        public static bool IsExtension(Assembly assembly)
        {
            return assembly.GetTypes()
                .Where(myType => myType.IsClass && !myType.IsAbstract && myType.IsSubclassOf(typeof(MAUSearch))).Any();
        }

        #endregion Static Methods
    }
}