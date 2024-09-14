using System;

namespace MelonAutoUpdater.Attributes
{
    /// <summary>
    /// Attribute that indicates whether an assembly is a MAU Search Extension or not
    /// </summary>
    [AttributeUsage(AttributeTargets.Assembly)]
    public class IsMAUSearchExtensionAttribute : Attribute
    {
        /// <summary>
        /// If true, assembly is a MAU Search Extension
        /// </summary>
        public bool Value;

        /// <summary>
        /// Creates a new instance of IsMAUSearchExtension Attribute
        /// </summary>
        /// <param name="value">If true, assembly is a MAU Search Extension</param>
        public IsMAUSearchExtensionAttribute(bool value = false) => Value = value;
    }
}