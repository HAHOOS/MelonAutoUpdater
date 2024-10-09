extern alias ml065;

using ml065.MelonLoader.TinyJSON;
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
        [Include]
        public string source { get; internal set; }

        /// <summary>
        /// The default charset associated with this type, if any
        /// </summary>
        [Include]
        public string charset { get; internal set; }

        /// <summary>
        /// Known file extensions associated with this mime type.
        /// </summary>
        [Include]
        public string[] extensions { get; internal set; }

        /// <summary>
        /// Whether a file of this type can be gzipped
        /// </summary>
        [Include]
        public bool compressible { get; internal set; }

#pragma warning restore IDE1006 // Naming Styles
    }
}