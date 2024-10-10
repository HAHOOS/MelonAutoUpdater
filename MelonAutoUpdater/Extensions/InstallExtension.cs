using System;

namespace MelonAutoUpdater.Extensions
{
    /// <summary>
    /// Extension for installing file types from downloaded files if update found
    /// </summary>
    // TODO: make this work
    public abstract class InstallExtension : ExtensionBase
    {
        internal override Type Type => typeof(InstallExtension);
        // WIP
    }
}