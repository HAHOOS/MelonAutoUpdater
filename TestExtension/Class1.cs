using MelonAutoUpdater;
using MelonAutoUpdater.Search;
using MelonAutoUpdater.Attributes;
using Semver;
using System.Threading.Tasks;
using System.Drawing;
using System.IO;
using System.Reflection;
using System;
using MelonAutoUpdater.Helper;
using MelonLoader.Pastel;

// Determines whether or not this is a MAU Search extension, set to true if you want it to be treated like that
[assembly: IsMAUSearchExtension(true)]

namespace TestExtension
{
    // You need to derive from MAUSearch, otherwise the extension will not load
    public class Class1 : MAUSearch
    {
        public override string Name => "Test"; // Name of extension that will be displayed in console

        public override SemVersion Version => new SemVersion(1, 0, 0); // Version of extension that will be displayed in console

        public override string Author => "HAHOOS"; // Author of extension that will be displayed in console

        public override string Link => "https://www.hahoos.pl/"; // Link to website that it will perform search on, right now does not do anything

        public override Color NameColor => Color.Red; // Color that will be used when displaying the name of extension

        public override Color AuthorColor => Color.Green; // Color that will be used when displaying the author of extension

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

            if (ContentType.TryParse(ContentType_Parse.MimeType, "application/zip", out ContentType contentType)) // You can use this in case you want to know what extension does a Mime-Type use
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

            if (ContentType.TryParse(ContentType_Parse.Extension, "txt", out ContentType contentType1)) // You can use this in case you want to know what mime-type does an extension use
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

            Logger.Msg($"My file name is {MelonAutoUpdater.ConsoleExtensions.Pastel(Path.GetFileName(Assembly.GetExecutingAssembly().Location), Theme.Instance.FileNameColor)}");
            // ^^ The example above uses two things
            // 1. Pastel method, which is available only since MelonLoader v0.6.0, but is in MAU regardless of ML version. The method adds ANSI colors to text
            // 2. Theme object, which holds data regarding used theme. Themes in MAU let you customize colors of things like the Dividing line, File name etc.
            // U can get the data using Theme.Instance, the colors are in HEX

            Logger.Msg(MelonAutoUpdater.ConsoleExtensions.PastelBg("My background is read!", Color.Red));
            // Sets the text background to red using ANSI

            Logger.Msg($"Current Unix timestamp in seconds is {DateTimeOffset.Now.ToUnixTimeSeconds()}");
            // The above uses a class called DateTimeOffsetHelper, you might or might not think that Im dumb doing that because it already exists, but net35 doesnt have it
            // This converts DateTimeOffset to a Unix timestamp in seconds

            Logger.Msg($"Unix timestamp in seconds 946681201 was in {DateTimeOffsetHelper.FromUnixTimeSeconds(946681201).ToString("G")}");
            // Converts Unix timestamp in seconds to DateTimeOffset
        }

        // Triggered when plugin is performing a search with your extension
        public override Task<ModData> Search(string url, SemVersion latestVersion)
        {
            Logger.Msg(Color.Red, "I don't like you >:(");
            return Empty(); // Returns a task with an empty ModData object, use this instead of null
        }

        // Triggered when plugin is performing a brute check with your extension
        public override Task<ModData> BruteCheck(string name, string author, SemVersion currentVersion)
        {
            Logger.Msg(Color.Red, "I don't really know what you're talking about");
            return Empty();
        }
    }
}