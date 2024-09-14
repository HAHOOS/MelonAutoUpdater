using Tomlet.Attributes;

namespace MelonAutoUpdater
{
    internal class Theme
    {
        [TomlInlineComment("The color of the line dividing the messages")]
        public string LineColor { get; set; } = "#FF1E90FF";

        [TomlInlineComment("The color of the file names")]
        public string FileNameColor { get; set; } = "#FFB22222";

        [TomlInlineComment("The color of an old version of a plugin/mod")]
        public string OldVersionColor { get; set; } = "#FFFF0000";

        [TomlInlineComment("The color of a new version of a plugin/mod")]
        public string NewVersionColor { get; set; } = "#FF00FA9A";

        [TomlInlineComment("The color of the message saying that the version is up to date / newer than in the API")]
        public string UpToDateVersionColor { get; set; } = "#FF90EE90";

        [TomlInlineComment("The color of the text indicating how many mods/plugin got installed")]
        public string DownloadCountColor { get; set; } = "#FF008000";

        [TomlInlineComment("The color of the extension name")]
        public string ExtensionNameDefaultColor { get; set; } = "#FF00FFFF";
    }
}