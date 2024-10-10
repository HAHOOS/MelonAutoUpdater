using System;

namespace MelonAutoUpdater.Extensions
{
    /// <summary>
    /// Class for extensions that exited with an exception
    /// </summary>
    public class RottenExtension
    {
        /// <summary>
        /// The extension that exited
        /// </summary>
        public ExtensionBase Extension { get; internal set; }

        /// <summary>
        /// The exception that made the extension exit
        /// </summary>
        public Exception Exception { get; internal set; }

        /// <summary>
        /// User-friendly information about why the extension is rotten
        /// </summary>
        public string Message { get; internal set; }

        internal RottenExtension(ExtensionBase extension, Exception exception, string message)
        {
            this.Extension = extension;
            this.Exception = exception;
            this.Message = message;
        }

        internal RottenExtension(ExtensionBase extension, Exception exception)
        {
            this.Extension = extension;
            this.Exception = exception;
        }

        internal RottenExtension(ExtensionBase extension, string message)
        {
            this.Extension = extension;
            this.Message = message;
        }
    }
}