using MelonLoader;
using System;
using System.Reflection;
using System.Runtime.InteropServices;

#region Melon Attributes

[assembly: MelonInfo(typeof(MelonAutoUpdater.MelonAutoUpdater), "MelonAutoUpdater", MelonAutoUpdater.MelonAutoUpdater.Version, "HAHOOS", "https://github.com/HAHOOS/MelonAutoUpdater")]
#pragma warning disable CS0618 // Type or member is obsolete
// Using ConsoleColor for backwards compatibility
[assembly: MelonColor(ConsoleColor.Green)]
#pragma warning restore CS0618 // Type or member is obsolete
[assembly: VerifyLoaderVersion("0.5.3", true)]
// They are not optional, but this is to remove the warning as NuGet will install them
// Not in use anymore
//[assembly: MelonOptionalDependencies("Net35.Http", "Rackspace.Threading", "System.Threading")]

#endregion Melon Attributes

#region Assembly Attributes

// Version
[assembly: AssemblyVersion(MelonAutoUpdater.MelonAutoUpdater.Version)]
[assembly: AssemblyFileVersion(MelonAutoUpdater.MelonAutoUpdater.Version)]
[assembly: AssemblyInformationalVersion(MelonAutoUpdater.MelonAutoUpdater.Version)]
[assembly: AssemblyProduct("MelonAutoUpdater")]
[assembly: AssemblyCompany("HAHOOS")]
[assembly: AssemblyDescription("An automatic updater for all your MelonLoader mods!")]
[assembly: AssemblyTitle("An automatic updater for all your MelonLoader mods!")]
[assembly: AssemblyCopyright("Copyright © HAHOOS 2024")]
[assembly: AssemblyCulture("")]
#if DEBUG
[assembly: AssemblyConfiguration("Debug")]
#elif RELEASE
[assembly: AssemblyConfiguration("Release")]
#endif

#endregion Assembly Attributes

#region Other

[assembly: ComVisible(true)]
[assembly: Guid("d284d92d-43d6-4847-a396-eab64fbad19d")]

#endregion Other