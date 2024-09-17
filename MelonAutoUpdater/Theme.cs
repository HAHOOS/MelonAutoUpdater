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
        /// The color of the line dividing the messages
        /// </summary>
        [TomlInlineComment("The color of the line dividing the messages")]
        public string LineColor { get; internal set; } = "#FF1E90FF";

        /// <summary>
        /// The color of the file names
        /// </summary>
        [TomlInlineComment("The color of the file names")]
        public string FileNameColor { get; internal set; } = "#FFB22222";

        /// <summary>
        /// The color of an old version of a plugin/mod
        /// </summary>
        [TomlInlineComment("The color of an old version of a plugin/mod")]
        public string OldVersionColor { get; internal set; } = "#FFFF0000";

        /// <summary>
        /// The color of a new version of a plugin/mod
        /// </summary>
        [TomlInlineComment("The color of a new version of a plugin/mod")]
        public string NewVersionColor { get; internal set; } = "#FF00FA9A";

        /// <summary>
        /// The color of the message saying that the version is up to date / newer than in the API
        /// </summary>
        [TomlInlineComment("The color of the message saying that the version is up to date / newer than in the API")]
        public string UpToDateVersionColor { get; internal set; } = "#FF90EE90";

        /// <summary>
        /// The color of the text indicating how many mods/plugin got installed
        /// </summary>
        [TomlInlineComment("The color of the text indicating how many mods/plugin got installed")]
        public string DownloadCountColor { get; internal set; } = "#FF008000";

        /// <summary>
        /// The color of the extension name
        /// </summary>
        [TomlInlineComment("The color of the extension name")]
        public string ExtensionNameDefaultColor { get; internal set; } = "#FF00FFFF";
    }
}