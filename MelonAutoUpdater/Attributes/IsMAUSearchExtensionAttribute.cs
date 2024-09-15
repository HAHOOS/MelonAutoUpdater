using System;

namespace MelonAutoUpdater.Attributes
{
    /// <summary>
    /// <see cref="Attribute"/> that indicates whether an assembly is a MAU Search Extension or not
    /// </summary>
    [AttributeUsage(AttributeTargets.Assembly)]
    public class IsMAUSearchExtensionAttribute : Attribute
    {
        /// <summary>
        /// If <see cref="true"/>, assembly is a MAU Search Extension
        /// </summary>
        public bool Value;

        /// <summary>
        /// Creates a new instance of <see cref="IsMAUSearchExtensionAttribute"/> <see cref="Attribute" />
        /// </summary>
        /// <param name="value">If <see cref="true"/>, <see cref="Attribute" /> is a MAU Search Extension</param>
        public IsMAUSearchExtensionAttribute(bool value = false) => Value = value;
    }
}