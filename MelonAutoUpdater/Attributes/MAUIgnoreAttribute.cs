using System;

namespace MelonAutoUpdater.Attributes
{
    /// <summary>
    /// <see cref="Attribute" /> that is used to tell MAU to ignore and continue without checking and/or updating the plugin/mod
    /// </summary>
    [AttributeUsage(AttributeTargets.Assembly)]
    public class MAUIgnoreAttribute : Attribute
    {
        /// <summary>
        /// If <see langword="true" />, the mod/plugin will not be checked and/or updated
        /// </summary>
        public bool Ignore;

        /// <summary>
        /// Creates an instance of <see cref="MAUIgnoreAttribute" />
        /// </summary>
        /// <param name="Ignore">If <see langword="true" />, the mod/plugin will not be checked and/or updated</param>
        public MAUIgnoreAttribute(bool Ignore = false) => this.Ignore = Ignore;
    }
}