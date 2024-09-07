using System;

namespace MelonAutoUpdater.Search.Attributes
{
    [AttributeUsage(AttributeTargets.Assembly)]
    public class IsMAUSearchExtensionAttribute : Attribute
    {
        /// <summary>
        /// If true, assembly is a MAU Search Extension
        /// </summary>
        public bool Value;

        public IsMAUSearchExtensionAttribute(bool value = false) => Value = value;
    }
}