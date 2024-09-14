using MelonAutoUpdater.JSONObjects;
using MelonLoader.TinyJSON;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace MelonAutoUpdater
{
    public class ContentType
    {
        /// <summary>
        /// Database of all recognized Mime Types
        /// </summary>
        private static MimeTypeDB _db;

        /// <summary>
        /// A Mime Type, for example: <c>application/zip</c>
        /// </summary>
        public string MimeType { get; private set; }

        /// <summary>
        /// Extension associated with the Mime Type, null if no extension was found to be associated with the Mime Type
        /// </summary>
        public string Extension { get; private set; }

        private ContentType(string mimeType, string extension)
        {
            MimeType = mimeType;
            Extension = extension;
        }

        /// <summary>
        /// Loads all Mime Types saved in <c>mime-types.json</c>
        /// </summary>
        internal static void Load()
        {
            Core.logger.Msg("Loading saved Mime-Types");
            System.Reflection.Assembly assembly = System.Reflection.Assembly.GetExecutingAssembly();
            var stream = assembly.GetManifestResourceStream($"{assembly.GetName().Name}.Embedded.mime-types.json");
            StreamReader streamReader = new StreamReader(stream);
            string text_json = streamReader.ReadToEnd();
            _db = new MimeTypeDB() { mimeTypes = JSON.Load(text_json).Make<Dictionary<string, MimeType>>() };
            Core.logger.Msg($"Successfully loaded {_db.mimeTypes.Count} Mime-Types with {_db.mimeTypes.Where(x => x.Value.extensions != null).Count()} of them having a file extension associated!");
        }

        public static ContentType Parse(ContentType_Parse type, string value)
        {
            if (type == ContentType_Parse.MimeType)
            {
                if (_db.mimeTypes.ContainsKey(value))
                {
                    var mime = _db.mimeTypes[value];
                    if (mime.extensions != null || mime.extensions.Length > 0)
                    {
                        return new ContentType(value, mime.extensions.First());
                    }
                    else
                    {
                        return new ContentType(value, null);
                    }
                }
                else
                {
                    throw new KeyNotFoundException("There is no mime type found using provided information");
                }
            }
            else if (type == ContentType_Parse.Extension)
            {
                var mimes = _db.mimeTypes.Where(x => x.Value.extensions != null && x.Value.extensions.Contains(value));
                if (mimes.Any())
                {
                    var mime = mimes.First();
                    return new ContentType(mime.Key, value);
                }
                else
                {
                    throw new KeyNotFoundException("There is no mime type found using provided information");
                }
            }
            throw new InvalidOperationException("Provided unrecognized ContentType_Parse Type");
        }

        public static bool TryParse(ContentType_Parse type, string value, out ContentType contentType)
        {
            try
            {
                ContentType _contentType = Parse(type, value);
                contentType = _contentType;
                return true;
            }
            catch (Exception e)
            {
                Core.logger.Error(e);
                contentType = null;
                return false;
            }
        }
    }

    public enum ContentType_Parse
    {
        MimeType,
        Extension
    }
}

namespace MelonAutoUpdater.JSONObjects
{
    public class MimeType
    {
#pragma warning disable IDE1006 // Naming Styles

        [Include]
        public string source { get; internal set; }

        [Include]
        public string charset { get; internal set; }

        [Include]
        public string[] extensions { get; internal set; }

        [Include]
        public bool compressible { get; internal set; }

#pragma warning restore IDE1006 // Naming Styles
    }

    public class MimeTypeDB
    {
#pragma warning disable IDE1006 // Naming Styles
        public Dictionary<string, MimeType> mimeTypes { get; internal set; }
#pragma warning restore IDE1006 // Naming Styles
    }
}