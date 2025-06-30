using MelonLoader;

[assembly: MelonInfo(typeof(MelonAutoUpdater.Core), "MelonAutoUpdater", "1.0.0", "HAHOOS", null)]
[assembly: MelonGame("Stress Level Zero", "BONELAB")]

namespace MelonAutoUpdater
{
    public class Core : MelonPlugin
    {
        public override void OnPreInitialization()
        {
            LoggerInstance.Msg("Pre-initialization.");
        }

        public override void OnInitializeMelon()
        {
            LoggerInstance.Msg("Initialized.");
        }
    }
}