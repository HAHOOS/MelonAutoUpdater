extern alias ml070;

using ml070.MelonLoader.TinyJSON;

using Newtonsoft.Json;

using System.Collections.Generic;

namespace MelonAutoUpdater.JSONObjects
{
    /// <summary>
    /// Object used for database
    /// </summary>
    public class MimeTypeDB
    {
#pragma warning disable IDE1006 // Naming Styles

        /// <summary>
        /// List of all mime-types
        /// </summary>
        public Dictionary<string, MimeType> mimeTypes { get; internal set; }

#pragma warning restore IDE1006 // Naming Styles
    }

    /// <summary>
    /// Mime-Type object used in the database
    /// </summary>
    public class MimeType
    {
#pragma warning disable IDE1006 // Naming Styles

        /// <summary>
        /// Source from where the mime-type was defined
        /// </summary>
        [JsonProperty]
        public string source { get; internal set; }

        /// <summary>
        /// The default charset associated with this type, if any
        /// </summary>
        [JsonProperty]
        public string charset { get; internal set; }

        /// <summary>
        /// Known file extensions associated with this mime type.
        /// </summary>
        [JsonProperty]
        public string[] extensions { get; internal set; }

        /// <summary>
        /// Whether a file of this type can be gzipped
        /// </summary>
        [JsonProperty]
        public bool compressible { get; internal set; }

#pragma warning restore IDE1006 // Naming Styles
    }
}