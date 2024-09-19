using MelonAutoUpdater.JSONObjects;
using MelonLoader.TinyJSON;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace MelonAutoUpdater
{
    /// <summary>
    /// Content Type that can be retrieved from Mime Type or File Extension
    /// </summary>
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
        /// Extension associated with the Mime Type, <see langword="null"/> if no extension was found to be associated with the Mime Type
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

        /// <summary>
        /// Parse mime type/file extension string to <see cref="ContentType"/>
        /// </summary>
        /// <param name="type">Way of parsing, either by Mime Type or File Extension</param>
        /// <param name="value">The value to parse</param>
        /// <returns><see cref="ContentType"/> of provided mime-type/file extension</returns>
        /// <exception cref="KeyNotFoundException">Mime Type was not found</exception>
        /// <exception cref="InvalidOperationException">An unknown <see cref="ContentType_Parse"/> enum was found</exception>
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

        /// <summary>
        /// Parse mime type/file extension string to <see cref="ContentType"/>
        /// </summary>
        /// <param name="type">Way of parsing, either by Mime Type or File Extension</param>
        /// <param name="value">The value to parse</param>
        /// <param name="contentType">The parsed <see cref="ContentType"/>, if not found, returns <see langword="null"/></param>
        /// <returns><see langword="true"/>, if found, otherwise <see langword="false"/></returns>
        /// <exception cref="KeyNotFoundException">Mime Type was not found</exception>
        /// <exception cref="InvalidOperationException">An unknown <see cref="ContentType_Parse"/> enum was found</exception>
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

    /// <summary>
    /// Type of value that should be parsed
    /// </summary>
    public enum ContentType_Parse
    {
        /// <summary>
        /// <see cref="ContentType"/> will be found from provided Mime Type
        /// </summary>
        MimeType,

        /// <summary>
        /// <see cref="ContentType"/> will be found from provided file extension
        /// </summary>
        Extension
    }
}