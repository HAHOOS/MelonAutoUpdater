using System;

namespace MelonAutoUpdater.Search
{
    /// <summary>
    /// Class for extensions that exited with an exception
    /// </summary>
    public class RottenExtension
    {
        /// <summary>
        /// The extension that exited
        /// </summary>
        public MAUExtension Extension { get; internal set; }

        /// <summary>
        /// The exception that made the extension exit
        /// </summary>
        public Exception Exception { get; internal set; }

        /// <summary>
        /// User-friendly information about why the extension is rotten
        /// </summary>
        public string Message { get; internal set; }

        internal RottenExtension(MAUExtension extension, Exception exception, string message)
        {
            this.Extension = extension;
            this.Exception = exception;
            this.Message = message;
        }

        internal RottenExtension(MAUExtension extension, Exception exception)
        {
            this.Extension = extension;
            this.Exception = exception;
        }

        internal RottenExtension(MAUExtension extension, string message)
        {
            this.Extension = extension;
            this.Message = message;
        }
    }
}