extern alias ml070;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using ml070::MelonLoader;
using ml070::Semver;

using Mono.Cecil;

using static ml070::MelonLoader.MelonPlatformAttribute;
using static ml070::MelonLoader.MelonPlatformDomainAttribute;

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
            if (customAttribute?.HasConstructorArguments != true || customAttribute.ConstructorArguments.Count == 0) return default;
            return (T)customAttribute.ConstructorArguments[index].Value;
        }

        /// <summary>
        /// Get attribute from <see cref="AssemblyDefinition"/>
        /// <para><b>WARNING: This does not work all the time and may throw errors, especially when there are Types</b></para>
        /// </summary>
        /// <typeparam name="T">The attribute to get</typeparam>
        /// <param name="assembly">Assembly to get the attribute from</param>
        /// <returns>The requested Attribute if found</returns>
        internal static T[] GetAttributes<T>(this AssemblyDefinition assembly)
        {
            MelonAutoUpdater.logger.DebugMsg($"Attribute name: {typeof(T).Name}");
            var attributes = assembly.CustomAttributes.Where(x => x.AttributeType.Name == typeof(T).Name);
            if (attributes.Any())
            {
                MelonAutoUpdater.logger.DebugMsg("Found attribute(s)");
                List<T> result = new List<T>();
                foreach (var attr in attributes)
                {
                    MelonAutoUpdater.logger.DebugMsg("Adding attribute to list");
                    object[] args = new object[attr.ConstructorArguments.Count];
                    foreach (var item in attr.ConstructorArguments)
                    {
                        MelonAutoUpdater.logger.DebugMsg($"Constructor Argument: ({item.Type.Name}) {item.Value}");
                        item.Type.Resolve();
                        args.Append(item.Value);
                    }
                    try
                    {
                        var val = (T)Activator.CreateInstance(typeof(T), args);
                        result.Add(val);
                    }
                    catch (MissingMethodException ex)
                    {
                        MelonAutoUpdater.logger.DebugError($"Cannot find constructor for {typeof(T).Name}, exception:\n{ex}");
                    }
                }
                return result.ToArray();
            }
            else
            {
                return null;
            }
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
                    || attr.AttributeType.Name == "MelonModInfoAttribute"
                    || attr.AttributeType.Name == "MelonPluginInfoAttribute")
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
                        string prerelease = Get<string>(attr, 3);
                        bool isMinimum = Get<bool>(attr, 4);
                        return new VerifyLoaderVersionAttribute(new SemVersion(major, minor, patch, prerelease), isMinimum);
                    }
                    catch (Exception)
                    {
                        try
                        {
                            int major = Get<int>(attr, 0);
                            int minor = Get<int>(attr, 1);
                            int patch = Get<int>(attr, 2);
                            bool isMinimum = Get<bool>(attr, 3);
                            return new VerifyLoaderVersionAttribute(new SemVersion(major, minor, patch), isMinimum);
                        }
                        catch (Exception)
                        {
                            try
                            {
                                string version = Get<string>(attr, 0);
                                bool isMinimum = Get<bool>(attr, 1);
                                return new VerifyLoaderVersionAttribute(version, isMinimum);
                            }
                            catch (Exception)
                            {
                                string version = Get<string>(attr, 0);
                                return new VerifyLoaderVersionAttribute(version);
                            }
                        }
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
                    || attr.AttributeType.Name == "MelonModGameAttribute"
                    || attr.AttributeType.Name == "MelonPluginGameAttribute")
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