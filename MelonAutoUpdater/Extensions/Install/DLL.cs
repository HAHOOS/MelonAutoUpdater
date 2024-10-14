extern alias ml065;

using ml065::Semver;

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
            var (_, error) = InstallPackage(path, MelonData.LatestVersion);
            if (error) return (false, 1, 0);
            else return (true, 0, 1);
        }
    }
}