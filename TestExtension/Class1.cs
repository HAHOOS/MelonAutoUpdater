using MelonAutoUpdater;
using MelonAutoUpdater.Extensions;
using Semver;
using System.Drawing;
using System.IO;
using System.Reflection;
using MelonAutoUpdater.Utils;
using System.Runtime.CompilerServices;

namespace TestExtension
{
    // You need to derive from SearchExtension, otherwise the extension will not load
    public class SearchExample : SearchExtension
    {
        public override string Name => "Test"; // Name of extension that will be displayed in console

        public override SemVersion Version => new(1, 0, 0); // Version of extension that will be displayed in console

        public override string Author => "HAHOOS"; // Author of extension that will be displayed in console

        public override Color NameColor => Color.Red; // Color that will be used when displaying the name of extension

        public override Color AuthorColor => Color.Green; // Color that will be used when displaying the author of extension

        public override string ID => "test-extension"; // The ID of the extension, can be set to use multiple extensions of the same name at once

        public override string Link => "https://hahoos.pl"; // Link to the website which will be searched for an update

        public override bool BruteCheckEnabled => true; // If true, brute check will be used. You will need to configure the BruteCheck method

        // Triggered when extension gets loaded into plugin
        public override void OnInitialization()
        {
            var category = CreateCategory("MyNewExtension!"); // Creates category in preferences in UserData/MelonAutoUpdater/SearchExtensions/Config/[name].cfg
            var entry = category.CreateEntry<bool>("DoYouLikeMe", false, "Do you like me?", description: "Default response: false"); // Creates new entry with name "Do you like me?" with boolean value
            category.SaveToFile(false); // It is recommended u do that every time creating category so the file creates even when u wont change anything

            Logger.Msg("Use this to log information to console and logs"); // U can use the Logger variable to send messages to console and file logs
            // This is recommended over MelonLogger

            Logger.Msg($"Current MAU Version: {GetMAUVersion()}"); // Get's the current version of MelonAutoUpdater, example: 0.3.0
            Logger.Msg($"Current DoYouLikeMe value: {GetEntryValue<bool>(entry)}"); // Get's value of entry with provided type

            if (ContentType.TryParse(ParseType.MimeType, "application/zip", out ContentType contentType)) // You can use this in case you want to know what extension does a Mime-Type use
            {
                if (contentType != null)
                {
                    if (contentType.Extension != null)
                    {
                        Logger.Msg($"{contentType.MimeType} has extension: {contentType.Extension}");
                    }
                    else
                    {
                        Logger.Warning($"{contentType.MimeType} has no extension :(");
                    }
                }
            }

            if (ContentType.TryParse(ParseType.Extension, "txt", out ContentType contentType1)) // You can use this in case you want to know what mime-type does an extension use
            {
                if (contentType1 != null)
                {
                    if (contentType1.MimeType != null)
                    {
                        Logger.Msg($"{contentType1.Extension} has mime-type: {contentType1.MimeType}");
                    }
                    else
                    {
                        Logger.Warning($"{contentType1.Extension} has no mime-type :(");
                    }
                }
            }

            Logger.Msg($"My file name is {Path.GetFileName(Assembly.GetExecutingAssembly().Location).Pastel(Theme.Instance.FileNameColor)}");
            // ^^ The example above uses two things
            // 1. Pastel method, which is available only since MelonLoader v0.6.0, but is in MAU regardless of ML version. The method adds ANSI colors to text
            // 2. Theme object, which holds data regarding used theme. Themes in MAU let you customize colors of things like the Dividing line, File name etc.
            // U can get the data using Theme.Instance, the colors are in HEX

            Logger.Msg(MelonAutoUpdater.Utils.ConsoleExtensions.PastelBg("My background is read!", Color.Red));
            // Sets the text background to red using ANSI
        }

        // Triggered when plugin is performing a search with your extension
        public override MelonData Search(string url, SemVersion latestVersion)
        {
            Logger.Msg(Color.Red, "I don't like you >:(");
            return null; // When null is returned, it means that nothing was found
        }

        // Triggered when plugin is performing a brute check with your extension
        public override MelonData BruteCheck(string name, string author, SemVersion currentVersion)
        {
            Logger.Msg(Color.Red, "I don't really know what you're talking about");
            return null;
        }
    }

    public class InstallExample : InstallExtension
    {
        public override string[] FileExtensions => throw new System.NotImplementedException();

        public override string Name => throw new System.NotImplementedException();

        public override SemVersion Version => throw new System.NotImplementedException();

        public override string Author => throw new System.NotImplementedException();

        public override (bool handled, int success, int failed) Install(string path)
        {
            throw new System.NotImplementedException();
        }
    }
}