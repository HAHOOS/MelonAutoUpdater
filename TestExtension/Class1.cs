using MelonAutoUpdater;
using MelonAutoUpdater.Search;
using MelonAutoUpdater.Search.Attributes;
using Semver;
using System.Threading.Tasks;

[assembly: IsMAUSearchExtension(true)]

namespace TestExtension
{
    public class Class1 : MAUSearch
    {
        public override string Name => "Test";

        public override SemVersion Version => new SemVersion(1, 0, 0);

        public override string Author => "HAHOOS";

        public override string Link => "https://www.hahoos.pl/";

        public override Task<ModData> Search(string url)
        {
            Logger.Msg("Test");
            return ReturnEmpty();
        }
    }
}