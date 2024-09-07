using MelonLoader;
using MelonLoader.TinyJSON;
using Semver;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using WebSocketDotNet;
using WebSocketDotNet.Messages;

namespace MelonAutoUpdater.Search.Included
{
    internal class NexusMods : MAUSearch
    {
        public override string Name => "NexusMods";

        public override SemVersion Version => new SemVersion(1, 0, 0);

        public override string Author => "HAHOOS";

        public override string Link => "https://www.nexusmods.com";

        #region Config

        internal MelonPreferences_Category MainCategory;

        internal MelonPreferences_Entry Entry_APIKey;

        private T GetPreferenceValue<T>(MelonPreferences_Entry entry)
        {
            if (entry != null && entry.BoxedValue != null)
            {
                try
                {
                    return (T)entry.BoxedValue;
                }
                catch (InvalidCastException)
                {
                    Logger.Error($"Preference '{entry.DisplayName}' is of incorrect type");
                    return default;
                }
            }
            return default;
        }

        #endregion Config

        private readonly string placeholderAPI = "put-api-key-here";

        public override void OnInitialization()
        {
            MainCategory = CreateCategory();
            Entry_APIKey = MainCategory.CreateEntry<string>("APIKey", placeholderAPI, "API Key",
                description: "API Key that can be retrieved in https://next.nexusmods.com/settings/api-keys or by authorizing when prompted");
            MainCategory.SaveToFile();
        }

        public override Task<ModData> Search(string url)
        {
            string apiKey = GetPreferenceValue<string>(Entry_APIKey);
            if (!string.IsNullOrEmpty(apiKey) && apiKey != placeholderAPI)
            {
                throw new NotImplementedException();
            }
            else
            {
                WebSocket websocket = new WebSocket("wss://sso.nexusmods.com", new WebSocketConfiguration() { AutoConnect = true });
                websocket.Connect();

                string UUID = string.Empty;
                string Token = string.Empty;

                websocket.Opened += () =>
                {
                    if (string.IsNullOrEmpty(UUID))
                    {
                        UUID = Guid.NewGuid().ToString();
                    }
                    Dictionary<string, object> data = new Dictionary<string, object>
                    {
                        { "id", UUID },
                        { "token", Token },
                        { "protocol", 2 }
                    };
                    string json = JSON.Dump(data);
                    websocket.Send(new WebSocketTextMessage(json));

                    System.Diagnostics.Process.Start($"https://www.nexusmods.com/sso?id={UUID}&application=vortex");
                };

                websocket.TextReceived += (msg) =>
                {
                    Logger.Msg(msg);
                    Variant json = JSON.Load(msg);
                    if (json && (bool)json["success"])
                    {
                        if (!string.IsNullOrEmpty((string)json["data"]["connection_token"]))
                        {
                            Token = json["connection_token"];
                        }
                        else if (!string.IsNullOrEmpty((string)json["data"]["api_key"]))
                        {
                            Logger.Msg("Api Token: " + json["api_key"]);
                            Entry_APIKey.BoxedValue = json["api_key"];
                            websocket.SendClose();
                        }
                    }
                };
                Console.ReadKey(true);
            }
            return ReturnEmpty();
        }
    }
}