using MelonAutoUpdater.Helper;
using System.Collections.Generic;
using System.Reflection;
using Tomlet.Attributes;

namespace MelonAutoUpdater
{
    /// <summary>
    /// Contains all data regarding current theme
    /// </summary>
    public class Theme
    {
        /// <summary>
        /// Instance of Theme that can be used by any code
        /// </summary>
        public static Theme Instance { get; private set; } = new Theme();

        /// <summary>
        /// Default values of properties
        /// </summary>
        public static readonly Dictionary<string, string> Defaults = new Dictionary<string, string>()
        {
            { "LinkColor",  "#00FFFF"},
            { "ExtensionNameDefaultColor", "#FF00FFFF" },
            { "DownloadCountColor","#FF008000" },
            { "UpToDateVersionColor","#FF90EE90" },
            { "CurrentVersionColor" ,"#0DC681" },
            {"NewVersionColor", "#FF00FA9A" },
            {"OldVersionColor","#FFFF0000" },
            {"FileNameColor","#FFB22222" },
            {"LineColor","#FF1E90FF" }
        };

        /// <summary>
        /// The color of the line dividing the messages
        /// </summary>
        [TomlInlineComment("The color of the line dividing the messages")]
        public string LineColor { get; set; }

        /// <summary>
        /// The color of the file names
        /// </summary>
        [TomlInlineComment("The color of the file names")]
        public string FileNameColor { get; set; }

        /// <summary>
        /// The color of an old version of a Melon
        /// </summary>
        [TomlInlineComment("The color of an old version of a Melon")]
        public string OldVersionColor { get; set; }

        /// <summary>
        /// The color of a new version of a Melon
        /// </summary>
        [TomlInlineComment("The color of a new version of a Melon")]
        public string NewVersionColor { get; set; }

        /// <summary>
        /// The color of the current version of a Melon
        /// </summary>
        [TomlInlineComment("The color of the current version of a Melon")]
        public string CurrentVersionColor { get; set; }

        /// <summary>
        /// The color of the message saying that the version is up to date / newer than in the API
        /// </summary>
        [TomlInlineComment("The color of the message saying that the version is up to date / newer than in the API")]
        public string UpToDateVersionColor { get; set; }

        /// <summary>
        /// The color of the text indicating how many Melons got installed
        /// </summary>
        [TomlInlineComment("The color of the text indicating how many Melons got installed")]
        public string DownloadCountColor { get; set; }

        /// <summary>
        /// The color of the extension name
        /// </summary>
        [TomlInlineComment("The color of the extension name")]
        public string ExtensionNameDefaultColor { get; set; }

        /// <summary>
        /// The color of links
        /// </summary>
        [TomlInlineComment("The color of links")]
        public string LinkColor { get; set; }

        /// <summary>
        /// Setup the theme, this sets the defaults if needed
        /// </summary>
        public void Setup()
        {
            Instance = this;
            var properties = this.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.GetProperty);
            foreach (var property in properties)
            {
                string val = (string)property.GetValue(this, null);
                MelonAutoUpdater.logger.DebugMsg($"{property.Name}: {(string.IsNullOrEmpty(val) ? "empty" : val)}");
                if (string.IsNullOrEmpty(val) && Defaults.ContainsKey(property.Name))
                {
                    MelonAutoUpdater.logger.DebugWarning("Property is empty, setting default");
                    property.SetValue(this, Defaults[property.Name], null);
                }
            }
        }
    }
}