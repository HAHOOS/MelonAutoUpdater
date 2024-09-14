using System;

namespace MelonAutoUpdater.Attributes
{
    /// <summary>
    /// Attribute that is used to tell MAU to ignore and continue without checking and/or updating the plugin/mod
    /// </summary>
    [AttributeUsage(AttributeTargets.Assembly)]
    public class MAUIgnoreAttribute : Attribute
    {
        /// <summary>
        /// If true, the mod/plugin will not be checked and/or updated
        /// </summary>
        public bool Ignore;

        /// <summary>
        /// Creates an instance of MAUIgnore Attribute
        /// </summary>
        /// <param name="Ignore">If true, the mod/plugin will not be checked and/or updated</param>
        public MAUIgnoreAttribute(bool Ignore = false) => this.Ignore = Ignore;
    }
}