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
        public string LineColor { get; set; } = "#FF1E90FF";

        /// <summary>
        /// The color of the file names
        /// </summary>
        [TomlInlineComment("The color of the file names")]
        public string FileNameColor { get; set; } = "#FFB22222";

        /// <summary>
        /// The color of an old version of a Melon
        /// </summary>
        [TomlInlineComment("The color of an old version of a Melon")]
        public string OldVersionColor { get; set; } = "#FFFF0000";

        /// <summary>
        /// The color of a new version of a Melon
        /// </summary>
        [TomlInlineComment("The color of a new version of a Melon")]
        public string NewVersionColor { get; set; } = "#FF00FA9A";

        /// <summary>
        /// The color of the message saying that the version is up to date / newer than in the API
        /// </summary>
        [TomlInlineComment("The color of the message saying that the version is up to date / newer than in the API")]
        public string UpToDateVersionColor { get; set; } = "#FF90EE90";

        /// <summary>
        /// The color of the text indicating how many Melons got installed
        /// </summary>
        [TomlInlineComment("The color of the text indicating how many Melons got installed")]
        public string DownloadCountColor { get; set; } = "#FF008000";

        /// <summary>
        /// The color of the extension name
        /// </summary>
        [TomlInlineComment("The color of the extension name")]
        public string ExtensionNameDefaultColor { get; set; } = "#FF00FFFF";

        /// <summary>
        /// The color of links
        /// </summary>
        public string LinkColor { get; set; } = "#00FFFF";
    }
}