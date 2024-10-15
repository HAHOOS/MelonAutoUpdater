extern alias ml065;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using ml065::MelonLoader;
using ml065::Semver;
using Mono.Cecil;
using static ml065::MelonLoader.MelonPlatformAttribute;
using static ml065::MelonLoader.MelonPlatformDomainAttribute;

namespace MelonAutoUpdater.Utils
{
    /// <summary>
    /// Class with all methods for getting MelonLoader attributes from <see cref="AssemblyDefinition"/>
    /// </summary>
    public static class MelonAttribute
    {
        /// <summary>
        /// Get value from a custom attribute
        /// </summary>
        /// <typeparam name="T"><see cref="Type"/> that will be returned as value</typeparam>
        /// <param name="customAttribute">The custom attribute you want to get value from</param>
        /// <param name="index">Index of the value</param>
        /// <returns>A value from the Custom Attribute with provided <see cref="Type"/></returns>
        internal static T Get<T>(this CustomAttribute customAttribute, int index)
        {
            if (customAttribute == null || !customAttribute.HasConstructorArguments || customAttribute.ConstructorArguments.Count <= 0) return default;
            return (T)customAttribute.ConstructorArguments[index].Value;
        }

        /// <summary>
        /// Retrieve information from the <see cref="MelonInfoAttribute"/> in a file using Mono.Cecil
        /// </summary>
        /// <param name="assembly"><see cref="AssemblyDefinition"/> of the file</param>
        /// <returns>If present, returns a <see cref="MelonInfoAttribute"/></returns>

        internal static MelonInfoAttribute GetMelonInfo(this AssemblyDefinition assembly)
        {
            foreach (var attr in assembly.CustomAttributes)
            {
#pragma warning disable CS0618 // Type or member is obsolete
                if (attr.AttributeType.Name == nameof(MelonInfoAttribute)
                    || attr.AttributeType.Name == nameof(MelonModInfoAttribute)
                    || attr.AttributeType.Name == nameof(MelonPluginInfoAttribute))
                {
                    var _type = Get<TypeDefinition>(attr, 0);
                    Type type = _type.BaseType.Name == "MelonMod" ? typeof(MelonMod) : _type.BaseType.Name == "MelonPlugin" ? typeof(MelonPlugin) : null;
                    string Name = Get<string>(attr, 1);
                    string Version = Get<string>(attr, 2);
                    string Author = Get<string>(attr, 3);
                    string DownloadLink = Get<string>(attr, 4);

                    assembly.Dispose();

                    return new MelonInfoAttribute(type: type, name: Name, version: Version, author: Author, downloadLink: DownloadLink);
                }
#pragma warning restore CS0618 // Type or member is obsolete
            }
            assembly.Dispose();
            return null;
        }

        /// <summary>
        /// Retrieve information from the <see cref="VerifyLoaderVersionAttribute"/> in a file using Mono.Cecil
        /// </summary>
        /// <param name="assembly"><see cref="Assembly"/> of the file</param>
        /// <returns>If present, returns a <see cref="VerifyLoaderVersionAttribute"/></returns>
        internal static VerifyLoaderVersionAttribute GetLoaderVersionRequired(this AssemblyDefinition assembly)
        {
            foreach (var attr in assembly.CustomAttributes)
            {
                if (attr.AttributeType.Name == nameof(VerifyLoaderVersionAttribute))
                {
                    try
                    {
                        int major = Get<int>(attr, 0);
                        int minor = Get<int>(attr, 1);
                        int patch = Get<int>(attr, 2);
                        bool isMinimum = Get<bool>(attr, 3);
                        return new VerifyLoaderVersionAttribute(major, minor, patch, isMinimum);
                    }
                    catch (Exception)
                    {
                        string version = Get<string>(attr, 0);
                        bool isMinimum = Get<bool>(attr, 1);
                        assembly.Dispose();
                        return new VerifyLoaderVersionAttribute(version, isMinimum);
                    }
                }
            }
            assembly.Dispose();
            return null;
        }

        /// <summary>
        /// Retrieve information from the <see cref="MelonGameAttribute"/> in a file using Mono.Cecil
        /// </summary>
        /// <param name="assembly"><see cref="AssemblyDefinition"/> of the file</param>
        /// <returns>If present, returns a <see cref="MelonGameAttribute"/></returns>
        internal static MelonGameAttribute[] GetMelonGameAttribute(this AssemblyDefinition assembly)
        {
            List<MelonGameAttribute> games = new List<MelonGameAttribute>();
            foreach (var attr in assembly.CustomAttributes)
            {
#pragma warning disable CS0618 // Type or member is obsolete
                if (attr.AttributeType.Name == nameof(MelonGameAttribute)
                    || attr.AttributeType.Name == nameof(MelonModGameAttribute)
                    || attr.AttributeType.Name == nameof(MelonPluginGameAttribute))
                {
                    string developer = Get<string>(attr, 0);
                    string name = Get<string>(attr, 1);
                    games.Add(new MelonGameAttribute(developer, name));
                }
#pragma warning restore CS0618 // Type or member is obsolete
            }
            assembly.Dispose();
            return games.ToArray();
        }

        /// <summary>
        /// Retrieve information from the <see cref="MelonProcessAttribute"/> in a file using Mono.Cecil
        /// </summary>
        /// <param name="assembly"><see cref="AssemblyDefinition"/> of the file</param>
        /// <returns>If present, returns a <see cref="MelonProcessAttribute"/></returns>
        internal static MelonProcessAttribute[] GetMelonProcessAttribute(this AssemblyDefinition assembly)
        {
            List<MelonProcessAttribute> games = new List<MelonProcessAttribute>();
            foreach (var attr in assembly.CustomAttributes)
            {
                if (attr.AttributeType.Name == nameof(MelonProcessAttribute))
                {
                    string exe = Get<string>(attr, 0);
                    games.Add(new MelonProcessAttribute(exe));
                }
            }
            assembly.Dispose();
            return games.ToArray();
        }

        /// <summary>
        /// Retrieve information from the <see cref="MelonPlatformAttribute"/> in a file using Mono.Cecil
        /// </summary>
        /// <param name="assembly"><see cref="AssemblyDefinition"/> of the file</param>
        /// <returns>If present, returns a <see cref="MelonPlatformAttribute"/></returns>
        internal static MelonPlatformAttribute GetMelonPlatformAttribute(this AssemblyDefinition assembly)
        {
            foreach (var attr in assembly.CustomAttributes)
            {
                if (attr.AttributeType.Name == nameof(MelonPlatformAttribute))
                {
                    CompatiblePlatforms platforms = Get<CompatiblePlatforms>(attr, 0);
                    assembly.Dispose();
                    return new MelonPlatformAttribute(platforms);
                }
            }
            assembly.Dispose();
            return null;
        }

        /// <summary>
        /// Retrieve information from the <see cref="VerifyLoaderBuildAttribute"/> in a file using Mono.Cecil
        /// </summary>
        /// <param name="assembly"><see cref="AssemblyDefinition"/> of the file</param>
        /// <returns>If present, returns a <see cref="VerifyLoaderBuildAttribute"/></returns>
        internal static VerifyLoaderBuildAttribute GetVerifyLoaderBuildAttribute(this AssemblyDefinition assembly)
        {
            foreach (var attr in assembly.CustomAttributes)
            {
                if (attr.AttributeType.Name == nameof(VerifyLoaderBuildAttribute))
                {
                    string build = Get<string>(attr, 0);
                    assembly.Dispose();
                    return new VerifyLoaderBuildAttribute(build);
                }
            }
            assembly.Dispose();
            return null;
        }

        /// <summary>
        /// Retrieve information from the <see cref="MelonPlatformDomainAttribute"/> in a file using Mono.Cecil
        /// </summary>
        /// <param name="assembly"><see cref="AssemblyDefinition"/> of the file</param>
        /// <returns>If present, returns a <see cref="MelonPlatformDomainAttribute"/></returns>
        internal static MelonPlatformDomainAttribute GetMelonPlatformDomainAttribute(this AssemblyDefinition assembly)
        {
            foreach (var attr in assembly.CustomAttributes)
            {
                if (attr.AttributeType.Name == nameof(MelonPlatformDomainAttribute))
                {
                    CompatibleDomains domains = Get<CompatibleDomains>(attr, 0);
                    assembly.Dispose();
                    return new MelonPlatformDomainAttribute(domains);
                }
            }
            assembly.Dispose();
            return null;
        }

        /// <summary>
        /// Retrieve information from the <see cref="MelonGameVersionAttribute"/> in a file using Mono.Cecil
        /// </summary>
        /// <param name="assembly"><see cref="AssemblyDefinition"/> of the file</param>
        /// <returns>If present, returns a <see cref="MelonGameAttribute"/></returns>
        internal static MelonGameVersionAttribute[] GetMelonGameVersionAttribute(this AssemblyDefinition assembly)
        {
            List<MelonGameVersionAttribute> versions = new List<MelonGameVersionAttribute>();
            foreach (var attr in assembly.CustomAttributes)
            {
                if (attr.AttributeType.Name == nameof(MelonGameVersionAttribute))
                {
                    string version = Get<string>(attr, 0);
                    versions.Add(new MelonGameVersionAttribute(version));
                }
            }
            assembly.Dispose();
            return versions.ToArray();
        }

        /// <summary>
        /// Check if an assembly is a <see cref="MelonMod"/>, a <see cref="MelonPlugin"/> or something else
        /// </summary>
        /// <param name="assembly"><see cref="AssemblyDefinition"/> of the file</param>
        /// <returns>A FileType, either <see cref="MelonMod"/>, <see cref="MelonPlugin"/> or Other</returns>
        public static FileType GetFileType(this AssemblyDefinition assembly)
        {
            MelonInfoAttribute infoAttribute = GetMelonInfo(assembly);

            if (infoAttribute != null)
            {
                return infoAttribute.SystemType == typeof(MelonMod) ? FileType.MelonMod : infoAttribute.SystemType == typeof(MelonPlugin) ? FileType.MelonPlugin : FileType.Other;
            }

            return FileType.Other;
        }

        /// <summary>
        /// Check if an assembly is a <see cref="MelonMod"/>, a <see cref="MelonPlugin"/> or something else
        /// </summary>
        /// <param name="infoAttribute"><see cref="MelonInfoAttribute"/> of the assembly</param>
        /// <returns>A FileType, either <see cref="MelonMod"/>, <see cref="MelonPlugin"/> or Other</returns>
        public static FileType GetFileType(this MelonInfoAttribute infoAttribute)
        {
            if (infoAttribute != null)
            {
                return infoAttribute.SystemType == typeof(MelonMod) ? FileType.MelonMod : infoAttribute.SystemType == typeof(MelonPlugin) ? FileType.MelonPlugin : FileType.Other;
            }

            return FileType.Other;
        }

        #region Backwards compatibility

        public static bool IsCompatible(this VerifyLoaderVersionAttribute attribute, SemVersion version)
           => attribute.SemVer == null || version == null || (attribute.IsMinimum ? attribute.SemVer <= version : attribute.SemVer == version);

        public static bool IsCompatible(this VerifyLoaderVersionAttribute attribute, string version)
            => !SemVersion.TryParse(version, out SemVersion ver) || IsCompatible(attribute, ver);

        public static bool IsCompatible(this VerifyLoaderBuildAttribute attr, string hashCode)
             => attr == null || string.IsNullOrEmpty(attr.HashCode) || string.IsNullOrEmpty(hashCode) || attr.HashCode == hashCode;

        public static bool IsCompatible(this MelonPlatformDomainAttribute attr, CompatibleDomains domain)
           => attr.Domain == CompatibleDomains.UNIVERSAL || domain == CompatibleDomains.UNIVERSAL || attr.Domain == domain;

        public static bool IsCompatible(this MelonPlatformAttribute attr, CompatiblePlatforms platform)
            => attr.Platforms == null || attr.Platforms.Length == 0 || attr.Platforms.Contains(platform);

        public static bool IsCompatible(this MelonProcessAttribute attr, string processName)
            => attr.Universal || string.IsNullOrEmpty(processName) || (RemoveExtension(processName) == attr.EXE_Name);

        private static string RemoveExtension(string name)
            => name == null ? null : (name.EndsWith(".exe") ? name.Remove(name.Length - 4) : name);

        #endregion Backwards compatibility
    }
}