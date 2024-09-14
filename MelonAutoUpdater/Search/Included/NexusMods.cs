﻿using MelonAutoUpdater.Helper;
using MelonLoader;
using MelonLoader.TinyJSON;
using Semver;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
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

        private bool disableAPI = false;
        private long apiReset;

        #region Config

        internal MelonPreferences_Category MainCategory;

        internal MelonPreferences_Entry Entry_APIKey;

        #endregion Config

        private readonly string placeholderAPI = "put-api-key-here";

        public static bool IsRequestSuccess(HttpStatusCode statusCode) => statusCode == HttpStatusCode.OK || (statusCode == HttpStatusCode.Created || (statusCode == HttpStatusCode.Accepted || (statusCode == HttpStatusCode.NonAuthoritativeInformation || statusCode == HttpStatusCode.NoContent || (statusCode == HttpStatusCode.ResetContent || (statusCode == HttpStatusCode.PartialContent)))));

        public override void OnInitialization()
        {
            MainCategory = CreateCategory();
            Entry_APIKey = MainCategory.CreateEntry<string>("APIKey", placeholderAPI, "API Key",
                description: "API Key that can be retrieved in https://next.nexusmods.com/settings/api-keys or by authorizing when prompted");
            MainCategory.SaveToFile();

            Logger.Msg("Checking for API Key");

            string apiKey = GetEntryValue<string>(Entry_APIKey);
            if (!string.IsNullOrEmpty(apiKey) && apiKey != placeholderAPI)
            {
                Logger.Msg("Found API Key, validating");
                var res = HttpRequest("https://api.nexusmods.com/v1/users/validate.json");
                res.Wait();
                if (res.Result != null && IsRequestSuccess(res.Result.StatusCode))
                {
                    Variant body = GetBodyFromResponse(res.Result);
                    if (body != null)
                    {
                        string name = body["name"].ToString();
                        bool isPremium = body["is_premium"].ToBoolean(null);
                        Logger.Msg(Color.Green, $"API Key validation successful, user belonging to key: {name} (" + (isPremium ? "Premium" : "Non-Premium") + ")");
                    }
                    else
                    {
                        Logger.Error("Nexus API returned no body while validating API Key");
                    }
                }
                else
                {
                    if (res.Result != null)
                    {
                        Logger.Error($"Nexus API returned code {(int)res.Result.StatusCode} while validating API Key with following reason: {res.Result.ReasonPhrase}");
                    }
                    else
                    {
                        Logger.Error("No response was found while validating API Key");
                    }
                }
            }
        }

        internal static Variant GetBodyFromResponse(HttpResponseMessage response)
        {
            var content = response.Content.ReadAsStringAsync();
            content.Wait();
            Variant body = JSON.Load(content.Result);
            return body;
        }

        internal Task<HttpResponseMessage> HttpRequest(string url)
        {
            HttpClient client = new HttpClient();
            client.DefaultRequestHeaders.Add("User-Agent", UserAgent);
            client.DefaultRequestHeaders.Add("apikey", GetEntryValue<string>(Entry_APIKey));
            client.DefaultRequestHeaders.Add("Application-Name", "MelonAutoUpdater");
            client.DefaultRequestHeaders.Add("Application-Version", GetMAUVersion());
            if (disableAPI && DateTimeOffset.UtcNow.ToUnixTimeSeconds() > apiReset) disableAPI = false;
            if (!disableAPI)
            {
                Task<HttpResponseMessage> response = client.GetAsync(url);
                response.Wait();

                if (response.Result.IsSuccessStatusCode)
                {
                    if (response.Result.Headers.GetValues("x-rl-hourly-remaining").Any() && response.Result.Headers.GetValues("x-rl-hourly-limit").Any() && response.Result.Headers.GetValues("x-rl-hourly-reset").Any())
                    {
                        int remaining = int.Parse(response.Result.Headers.GetValues("x-rl-hourly-remaining").FirstOrDefault());
                        int limit = int.Parse(response.Result.Headers.GetValues("x-rl-hourly-limit").FirstOrDefault());
                        string reset = response.Result.Headers.GetValues("x-rl-hourly-reset").FirstOrDefault();
                        if (remaining <= 10)
                        {
                            Logger.Warning("To protect you from a possible rate limit, the usage of the API will be disabled until reset");
                            TaskCompletionSource<HttpResponseMessage> _res = new TaskCompletionSource<HttpResponseMessage>();
                            _res.SetResult(null);
                            return _res.Task;
                        }
                    }
                    client.Dispose();
                    response.Dispose();

                    return Task.Factory.StartNew(() => response.Result);
                }
                else
                {
                    int remaining = int.Parse(response.Result.Headers.GetValues("x-rl-hourly-remaining").First());
                    int limit = int.Parse(response.Result.Headers.GetValues("x-rl-hourly-limit").First());
                    string reset = response.Result.Headers.GetValues("x-rl-hourly-reset").First();
                    if (remaining <= 0)
                    {
                        Logger.Error($"You've reached the rate limit of NexusMods API ({limit}) and you will be able to use the NexusMods API again at {DateTime.Parse(reset).ToLocalTime():t}");
                        apiReset = DateTimeOffset.Parse(reset).ToUnixTimeSeconds();
                        disableAPI = true;
                    }

                    client.Dispose();
                    response.Dispose();

                    return Task.Factory.StartNew(() => response.Result);
                }
            }
            TaskCompletionSource<HttpResponseMessage> res = new TaskCompletionSource<HttpResponseMessage>();
            res.SetResult(null);
            return res.Task;
        }

        public override Task<ModData> Search(string url, SemVersion currentVersion)
        {
            string apiKey = GetEntryValue<string>(Entry_APIKey);
            if (!string.IsNullOrEmpty(apiKey) && apiKey != placeholderAPI)
            {
                string[] split = url.Split('/');
                string modId;
                string gameName;
                if (url.EndsWith("/"))
                {
                    modId = split[split.Length - 2];
                    gameName = split[split.Length - 4];
                }
                else
                {
                    modId = split[split.Length - 1];
                    gameName = split[split.Length - 3];
                }
                var request1 = HttpRequest($"https://api.nexusmods.com/v1/games/{gameName}/mods/{modId}.json");
                request1.Wait();
                if (request1.Result != null && IsRequestSuccess(request1.Result.StatusCode))
                {
                    Variant body = GetBodyFromResponse(request1.Result);
                    if (body != null)
                    {
                        bool parseSuccess = SemVersion.TryParse(body["version"].ToString(), out SemVersion latestVersion);
                        if (parseSuccess)
                        {
                            if (latestVersion > currentVersion)
                            {
                                Logger.Msg("New version found, fetching files");
                                var request2 = HttpRequest($"https://api.nexusmods.com/v1/games/{gameName}/mods/{modId}/files.json?category=main");
                                request2.Wait();
                                if (request2.Result != null && IsRequestSuccess(request2.Result.StatusCode))
                                {
                                    Variant body2 = GetBodyFromResponse(request2.Result);
                                    if (body2 != null)
                                    {
                                        if (body2["files"] is ProxyArray fileArray)
                                        {
                                            List<FileData> files = new List<FileData>();
                                            if (fileArray.Count > 0)
                                            {
                                                Logger.Msg("More than 1 file detected, user attention required");
                                                foreach (var file in fileArray)
                                                {
                                                    Logger.Msg($"Would you like to install {file["name"].ToString().Pastel(Color.Aqua)}? (Y/N)");
                                                    var key = Console.ReadKey();
                                                    if (key.KeyChar.ToString().ToLower() == "y")
                                                    {
                                                        Logger.Msg("Adding to install queue...");

                                                        Logger.Msg($"{file["name"].ToString().Pastel(Color.Aqua)} added to install queue");
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                                else
                                {
                                }
                            }
                            else
                            {
                                Logger.Error($"Failed to parse string '{body["version"]}' to Semversion");
                                return Empty();
                            }
                        }
                    }
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
                }
                return Empty();
            }
            return Empty();
        }
    }
}