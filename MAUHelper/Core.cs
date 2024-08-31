using MelonLoader;
using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

[assembly: MelonInfo(typeof(MAUHelper.Core), "MAUHelper", "1.0.0", "HAHOOS", null)]
[assembly: MelonPriority(-100000001)]
#pragma warning disable CS0618 // Type or member is obsolete
[assembly: MelonColor(ConsoleColor.Green)]
[assembly: MelonAuthorColor(ConsoleColor.Yellow)]
#pragma warning restore CS0618 // Type or member is obsolete

namespace MAUHelper
{
    public class Core : MelonPlugin
    {
        private AssemblyDefinition pluginAssembly;

        private void GetPluginAssembly()
        {
#pragma warning disable CS0618 // Type or member is obsolete
            string pluginsDir = Path.Combine(MelonUtils.BaseDirectory, "Plugins");
#pragma warning restore CS0618 // Type or member is obsolete
            List<string> files = Directory.GetFiles(pluginsDir, "*.dll").ToList();
            foreach (string file in files)
            {
                AssemblyDefinition assembly = AssemblyDefinition.ReadAssembly(file);
                if (assembly != null)
                {
                    if (assembly.Name.Name == "MelonAutoUpdater")
                    {
                        pluginAssembly = assembly;
                        return;
                    }
                }
                assembly.Dispose();
            }
        }

        public override void OnPreInitialization()
        {
            LoggerInstance.Msg("Starting check");
            bool isNet6 = Environment.Version.Major >= 6;
            string consoleVer = isNet6 ? ".NET 6" : $".NET Framework {Environment.Version.Major}.{Environment.Version.Minor}";
            LoggerInstance.Msg(System.ConsoleColor.Green, "Runtime Version: " + consoleVer);

            LoggerInstance.Msg("Checking if MAU (Melon Auto Updater) is installed");
            GetPluginAssembly();
            if (pluginAssembly != null)
            {
                LoggerInstance.Msg("MAU found, checking if correct version");
                bool isFramework = pluginAssembly.MainModule.AssemblyReferences.Where(x => x.Name == "mscorlib").Count() > 0;
                LoggerInstance.Msg("Current MAU version: " + (isFramework ? "net35" : "net6"));
                if (isFramework && isNet6)
                {
                    LoggerInstance.Error("Incorrect version of MAU (Melon Auto Updater), make sure to install the net35 version, not the net6 version!");
                    Console.ReadKey();
                }
                else if (!isFramework && !isNet6)
                {
                    LoggerInstance.Error("Incorrect version of MAU (Melon Auto Updater), make sure to install the net6 version, not the net35 version!");
                    Console.ReadKey();
                }
                else
                {
                    LoggerInstance.Msg(System.ConsoleColor.Green, "Correct version of MAU (Melon Auto Updater) is installed!");
                }
            }
            else
            {
                LoggerInstance.Msg("MAU is not installed");
                LoggerInstance.Msg(System.ConsoleColor.Blue, "------------------------");
                LoggerInstance.Msg("Results of checks:");
                if (isNet6)
                {
                    LoggerInstance.Msg("Install net6 version of MAU (Melon Auto Updater), net35 version also works but is not recommended");
                }
                else
                {
                    LoggerInstance.Msg("Install net35 version of MAU (Melon Auto Updater), net6 version will not work");
                }
                LoggerInstance.Msg(System.ConsoleColor.Blue, "------------------------");
                Console.ReadKey();
            }
        }
    }
}