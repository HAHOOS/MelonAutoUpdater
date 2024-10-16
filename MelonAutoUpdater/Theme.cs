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

        public void Setup()
        {
            Instance = this;
            if (string.IsNullOrEmpty(LinkColor))
            {
                LinkColor = "#00FFFF";
            }

            if (string.IsNullOrEmpty(ExtensionNameDefaultColor))
            {
                ExtensionNameDefaultColor = "#FF00FFFF";
            }

            if (string.IsNullOrEmpty(DownloadCountColor))
            {
                DownloadCountColor = "#FF008000";
            }

            if (string.IsNullOrEmpty(UpToDateVersionColor))
            {
                UpToDateVersionColor = "#FF90EE90";
            }

            if (string.IsNullOrEmpty(CurrentVersionColor))
            {
                CurrentVersionColor = "#0DC681";
            }

            if (string.IsNullOrEmpty(NewVersionColor))
            {
                NewVersionColor = "#FF00FA9A";
            }

            if (string.IsNullOrEmpty(OldVersionColor))
            {
                OldVersionColor = "#FFFF0000";
            }

            if (string.IsNullOrEmpty(FileNameColor))
            {
                FileNameColor = "#FFB22222";
            }

            if (string.IsNullOrEmpty(LineColor))
            {
                LineColor = "#FF1E90FF";
            }
        }
    }
}