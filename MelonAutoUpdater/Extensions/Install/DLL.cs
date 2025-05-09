extern alias ml070;

using MelonAutoUpdater.Helper;
using MelonAutoUpdater.Utils;

using ml070::Semver;

using Mono.Cecil;

using System.IO;
using System.Linq;

namespace MelonAutoUpdater.Extensions.Install
{
    internal class DLL : InstallExtension
    {
        public override string[] FileExtensions => new string[] { ".dll" };

        public override string Name => "DLL";

        public override SemVersion Version => new SemVersion(1, 0, 0);

        public override string Author => "HAHOOS";

        public override (bool handled, int success, int failed) Install(string path)
        {
            Logger.Msg("Downloaded file is a DLL file, installing content...");
            var ass = AssemblyDefinition.ReadAssembly(path);
            var info = ass.GetMelonInfo();
            if (info != null)
            {
                var conflictingMelon = MelonUpdater.Melons.FirstOrDefault(x => x.Value.Name == info.Name && x.Value.Author == info.Author);
                if (conflictingMelon.Value != null && conflictingMelon.Key != null)
                {
                    Logger.Msg("Found a conflicting package, checking if there are more");
                    var installs = InstallList.Where(x =>
                    {
                        if (!x.Value) return false;
                        if (Path.GetExtension(x.Key) == ".dll")
                        {
                            var _ass = AssemblyDefinition.ReadAssembly(x.Key);
                            var _info = _ass.GetMelonInfo();
                            if (_info != null && _info.Name == info.Name && _info.Author == info.Author)
                            {
                                return true;
                            }
                        }
                        return false;
                    });
                    if (installs.Any())
                    {
                        Logger.Msg("There are more conflicting packages");
                        var package = FindMostOptimalPackage(installs.GetKeys());
                        if (package != null)
                        {
                            var list = installs.ToList();
                            list.RemoveAll(x => x.Key == package);
                            var (_, _error) = InstallPackage(package, MelonData.LatestVersion);
                            if (_error)
                            {
                                return (false, 0, 1);
                            }
                            else
                            {
                                list.ForEach(x => DisallowInstall(x.Key));
                                return (true, 1, 0);
                            }
                        }
                        else
                        {
                            return (false, 0, 0);
                        }
                    }
                    else
                    {
                        Logger.Msg("There are no more conflicting packages");
                        var package = FindMostOptimalPackage(new string[] { path, conflictingMelon.Key });
                        if (package != null)
                        {
                            var list = installs.ToList();
                            list.RemoveAll(x => x.Key == package);
                            var (_, _error) = InstallPackage(package, MelonData.LatestVersion);
                            if (_error)
                            {
                                return (false, 0, 1);
                            }
                            else
                            {
                                list.ForEach(x => DisallowInstall(x.Key));
                                return (true, 1, 0);
                            }
                        }
                        else
                        {
                            return (false, 0, 0);
                        }
                    }
                }
                else
                {
                    var (_, error) = InstallPackage(path, MelonData.LatestVersion);
                    if (error) return (false, 0, 1);
                    else return (true, 1, 0);
                }
            }
            else
            {
                Logger.Msg("The DLL is not a melon, installing in base directory. Cannot handle.");
                return (false, 0, 0);
            }
        }
    }
}