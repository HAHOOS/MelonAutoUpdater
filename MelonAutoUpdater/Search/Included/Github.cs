using MelonAutoUpdater.Helper;
using MelonLoader.TinyJSON;
using Semver;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using MelonLoader;
using System.Drawing;

namespace MelonAutoUpdater.Search.Included
{
    internal class Github : MAUSearch
    {
        public override string Name => "Github";

        public override SemVersion Version => new SemVersion(1, 0, 0);

        public override string Author => "HAHOOS";

        public override string Link => "https://github.com";

        public override bool BruteCheckEnabled => true;

        /// <summary>
        /// This is used to prevent from rate-limiting the API
        /// </summary>
        private bool disableGithubAPI = false;

        /// <summary>
        /// The time (in Unix time seconds) when the rate limit will disappear
        /// </summary>
        private long githubResetDate;

        private readonly string ClientID = "Iv23lii0ysyknh3Vf51t";

        internal string AccessToken;

        // Melon Preferences

        private MelonPreferences_Category category;

        private MelonPreferences_Entry entry_useDeviceFlow;
        private MelonPreferences_Entry entry_accessToken;

        public override void OnInitialization()
        {
            category = CreateCategory();
            entry_useDeviceFlow = category.CreateEntry<bool>("UseDeviceFlow", true, "Use Device Flow",
                description: "If enabled, you will be prompted to authenticate using Github's Device Flow to make authenticated requests if access token is not registered or valid (will raise request limit from 60 to 1000)");
            entry_accessToken = category.CreateEntry<string>("AccessToken", string.Empty, "Access Token",
                description: "Access Token used to make authenticated requests (Do not edit if you do not know what you're doing)");

            category.SaveToFile();

            Logger.Msg("Checking if Access Token exists");

            var use = GetEntryValue<bool>(entry_useDeviceFlow);
            if (use)
            {
                var accessToken = GetEntryValue<string>(entry_accessToken);
                if (!string.IsNullOrEmpty(accessToken))
                {
                    this.AccessToken = accessToken;
                    Logger.Msg("Access token found, validating");
                    HttpClient client2 = new HttpClient();
                    client2.DefaultRequestHeaders.Add("Accept", "application/json");
                    client2.DefaultRequestHeaders.Add("User-Agent", UserAgent);
                    client2.DefaultRequestHeaders.Add("Authorization", "Bearer " + accessToken);
                    var response2 = client2.GetAsync("https://api.github.com/user");
                    response2.Wait();
                    if (response2.Result.IsSuccessStatusCode)
                    {
                        Task<string> body2 = response2.Result.Content.ReadAsStringAsync();
                        body2.Wait();
                        if (body2.Result != null)
                        {
                            var data = JSON.Load(body2.Result).Make<Dictionary<string, string>>();
                            Logger.Msg($"Successfully validated access token, belongs to {data["name"]} ({data["followers"]} Followers)");
                            return;
                        }
                    }
                    else
                    {
                        if (response2.Result.StatusCode == System.Net.HttpStatusCode.Unauthorized || response2.Result.StatusCode == System.Net.HttpStatusCode.Forbidden)
                        {
                            Logger.Warning("Access token expired or is incorrect");
                        }
                        else
                        {
                            Logger.Msg("Error");
                            Logger.Error
                                    ($"Failed to validate access token, returned {response2.Result.StatusCode} with following message:\n{response2.Result.ReasonPhrase}");
                            client2.Dispose();
                            response2.Dispose();
                        }
                    }
                }
                Logger.Msg("Requesting Device Flow");
                HttpClient client = new HttpClient();
                string scopes = "read:user";
                client.DefaultRequestHeaders.Add("Accept", "application/json");
                client.DefaultRequestHeaders.Add("User-Agent", UserAgent);
                var response = client.PostAsync($"https://github.com/login/device/code?client_id={ClientID}&scope={scopes}", null);
                response.Wait();
                if (response.Result.IsSuccessStatusCode)
                {
                    Task<string> body = response.Result.Content.ReadAsStringAsync();
                    body.Wait();
                    if (body.Result != null)
                    {
                        var data = JSON.Load(body.Result).Make<Dictionary<string, string>>();
                        Logger.Msg($@"To use Github in the plugin, it is recommended that you make authenticated requests, to do that:

Go to {data["verification_uri"].ToString().Pastel(Color.Cyan)} and enter {data["user_code"].ToString().Pastel(Color.Aqua)}, when you do that press any key
You have {Math.Round((decimal)(int.Parse(data["expires_in"]) / 60))} minutes to enter the code before it expires!
Press any key to continue, press N to continue without using authenticated requests (You will be limited to 60 requests, instead of 1000)

If you do not want to do this, go to UserData/MelonAutoUpdater/SearchExtensions/Config and open Github.json, in there set 'UseDeviceFlow' to false");
                        bool canUse = true;
                        while (true)
                        {
                            var key = Console.ReadKey(false);
                            Logger.Msg(canUse);
                            if (!canUse)
                            {
                                Logger.Msg("Cooldown!");
                            }
                            else
                            {
                                if (key.KeyChar.ToString().ToLower() == "N".ToLower())
                                {
                                    Logger.Msg("Aborting Device Flow request");
                                    client.Dispose();
                                    response.Dispose();
                                    return;
                                }
                                else
                                {
                                    Logger.Msg("Checking if authorized");
                                    var res = client.PostAsync($"https://github.com/login/oauth/access_token?client_id={ClientID}&device_code={data["device_code"]}&grant_type=urn:ietf:params:oauth:grant-type:device_code", null);
                                    res.Wait();
                                    if (res.Result.IsSuccessStatusCode)
                                    {
                                        Task<string> _body = res.Result.Content.ReadAsStringAsync();
                                        _body.Wait();
                                        if (_body.Result != null)
                                        {
                                            var _data = JSON.Load(_body.Result).Make<Dictionary<string, string>>();
                                            if (_data != null)
                                            {
                                                if (_data.ContainsKey("error"))
                                                {
                                                    if (_data["error"] == "authorization_pending")
                                                    {
                                                        Logger.Msg("The plugin is not authorized!");
                                                    }
                                                    else
                                                    {
                                                        Logger.Error($"Unexpected error {_data["error"]}, description: {_data["error_description"]}");
                                                        return;
                                                    }
                                                }
                                                else if (_data.ContainsKey("access_token"))
                                                {
                                                    entry_accessToken.BoxedValue = _data["access_token"].ToString();
                                                    AccessToken = _data["access_token"].ToString();
                                                    category.SaveToFile();
                                                    Logger.Msg("Successfully retrieved access token".Pastel(Color.LawnGreen));

                                                    client.Dispose();
                                                    res.Dispose();
                                                    response.Dispose();

                                                    return;
                                                }
                                            }
                                            else
                                            {
                                                Logger.Warning("Missing required data from request");
                                            }
                                        }
                                        else
                                        {
                                            Logger.Error
                                                ($"Failed to use Device Flow using Github, returned {res.Result.StatusCode} with following message:\n{res.Result.ReasonPhrase}");
                                        }
                                    }
                                }
                                canUse = false;
                                System.Timers.Timer timer = new System.Timers.Timer
                                {
                                    Interval = int.Parse(data["interval"]) * 1000
                                };
                                timer.Elapsed += (x, y) =>
                                {
                                    canUse = true;
                                    timer.Stop();
                                };
                                timer.Start();
                            }
                        }
                    }
                }
                else
                {
                    Logger.Msg("Error");
                    Logger.Error
                            ($"Failed to use Device Flow using Github, returned {response.Result.StatusCode} with following message:\n{response.Result.ReasonPhrase}");
                    client.Dispose();
                    response.Dispose();
                }
            }
            else
            {
                Logger.Msg("Device Flow is disabled, using unauthenticated requests");
            }
        }

        internal Task<MelonData> Check(string author, string repo)
        {
            HttpClient client = new HttpClient();
            client.DefaultRequestHeaders.Add("Accept", "application/vnd.github+json");
            client.DefaultRequestHeaders.Add("User-Agent", UserAgent);
            if (!string.IsNullOrEmpty(AccessToken)) client.DefaultRequestHeaders.Add("Authorization", "Bearer " + AccessToken);
            if (disableGithubAPI && DateTimeOffset.UtcNow.ToUnixTimeSeconds() > githubResetDate) disableGithubAPI = false;
            if (!disableGithubAPI)
            {
                var response = client.GetAsync($"https://api.github.com/repos/{author}/{repo}/releases/latest", HttpCompletionOption.ResponseContentRead);
                response.Wait();
                if (response.Result.IsSuccessStatusCode)
                {
                    if (response.Result.Headers.Contains("x-ratelimit-remaining")
                        && response.Result.Headers.Contains("x-ratelimit-reset"))
                    {
                        int remaining = int.Parse(response.Result.Headers.GetValues("x-ratelimit-remaining").First());
                        long reset = long.Parse(response.Result.Headers.GetValues("x-ratelimit-reset").First());
                        if (remaining <= 10)
                        {
                            Logger.Warning("Due to rate limits nearly reached, any attempt to send an API call to Github during this session will be aborted");
                            githubResetDate = reset;
                            disableGithubAPI = true;
                        }
                    }
                    Task<string> body = response.Result.Content.ReadAsStringAsync();
                    body.Wait();
                    if (body.Result != null)
                    {
                        var data = JSON.Load(body.Result);
                        string version = (string)data["tag_name"];
                        List<FileData> downloadURLs = new List<FileData>();

                        foreach (var file in data["assets"] as ProxyArray)
                        {
                            downloadURLs.Add(new FileData((string)file["browser_download_url"], Path.GetFileNameWithoutExtension((string)file["browser_download_url"]), (string)file["content_type"]));
                        }

                        client.Dispose();
                        response.Dispose();
                        body.Dispose();
                        if (version.StartsWith("v"))
                        {
                            version = version.Substring(1);
                        }
                        bool isSemVerSuccess = SemVersion.TryParse(version, out SemVersion semver);
                        if (!isSemVerSuccess)
                        {
                            Logger.Error($"Failed to parse version");
                            return Empty();
                        }
                        return Task.Factory.StartNew(() => new MelonData(semver, downloadURLs));
                    }
                    else
                    {
                        Logger.Error("Github API returned no body, unable to fetch package information");

                        client.Dispose();
                        response.Dispose();
                        body.Dispose();

                        return Empty();
                    }
                }
                else
                {
                    if (response.Result.Headers.Contains("x-ratelimit-remaining")
                       && response.Result.Headers.Contains("x-ratelimit-reset")
                       && response.Result.Headers.Contains("x-ratelimit-limit"))
                    {
                        int remaining = int.Parse(response.Result.Headers.GetValues("x-ratelimit-remaining").First());
                        int limit = int.Parse(response.Result.Headers.GetValues("x-ratelimit-limit").First());
                        long reset = long.Parse(response.Result.Headers.GetValues("x-ratelimit-reset").First());
                        if (remaining <= 0)
                        {
                            Logger.Error($"You've reached the rate limit of Github API ({limit}) and you will be able to use the Github API again at {DateTimeOffsetHelper.FromUnixTimeSeconds(reset).ToLocalTime():t}");
                            githubResetDate = reset;
                            disableGithubAPI = true;
                        }
                        else
                        {
                            if (response.Result.StatusCode == System.Net.HttpStatusCode.NotFound)
                            {
                                Logger.Warning("Github API could not find the mod/plugin");
                            }
                            else
                            {
                                Logger.Error
                                    ($"Failed to fetch package information from Github, returned {response.Result.StatusCode} with following message:\n{response.Result.ReasonPhrase}");
                            }
                        }
                    }
                    else
                    {
                        if (response.Result.StatusCode == System.Net.HttpStatusCode.NotFound)
                        {
                            Logger.Warning("Github API could not find the mod/plugin");
                        }
                        else
                        {
                            Logger.Error
                                ($"Failed to fetch package information from Github, returned {response.Result.StatusCode} with following message:\n{response.Result.ReasonPhrase}");
                        }
                    }
                    client.Dispose();
                    response.Dispose();

                    return Empty();
                }
            }
            else
            {
                Logger.Warning(
                     "Github API access is currently disabled and this check will be aborted, you should be good to use the API at " + DateTimeOffsetHelper.FromUnixTimeSeconds(githubResetDate).ToLocalTime().ToString("t"));
            }

            return Empty();
        }

        public override Task<MelonData> Search(string url, SemVersion currentVersion)
        {
            Regex regex = new Regex(@"(?<=(?<=http:\/\/|https:\/\/)github.com\/)(.*?)(?>\/)(.*?)(?=\/|$)");
            var match = regex.Match(url);
            if (match.Success)
            {
                string[] split = match.Value.Split('/');
                string packageName = split[1];
                string namespaceName = split[0];
                Check(namespaceName, packageName);
            }
            return Empty();
        }

        public override Task<MelonData> BruteCheck(string name, string author, SemVersion currentVersion)
        {
            return Check(author, name);
        }
    }
}