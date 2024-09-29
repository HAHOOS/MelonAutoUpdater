using MelonAutoUpdater.Helper;
using MelonLoader.TinyJSON;
using Semver;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using MelonLoader;
using System.Drawing;
using MelonAutoUpdater.Utils;
using System.Net;
using System.Collections.Specialized;
using System.Text;

namespace MelonAutoUpdater.Search.Included.Github
{
    internal class Github : MAUSearch
    {
        public override string Name => "Github";

        public override SemVersion Version => new SemVersion(1, 0, 1);

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
            entry_useDeviceFlow = category.CreateEntry("UseDeviceFlow", true, "Use Device Flow",
                description: "If enabled, you will be prompted to authenticate using Github's Device Flow to make authenticated requests if access token is not registered or valid (will raise request limit from 60 to 1000)");
            entry_accessToken = category.CreateEntry("AccessToken", string.Empty, "Access Token",
                description: "Access Token used to make authenticated requests (Do not edit if you do not know what you're doing)");

            category.SaveToFile();

            Logger.Msg("Checking if Access Token exists");

            var use = GetEntryValue<bool>(entry_useDeviceFlow);
            if (use)
            {
                var accessToken = GetEntryValue<string>(entry_accessToken);
                if (!string.IsNullOrEmpty(accessToken))
                {
                    AccessToken = accessToken;
                    Logger.Msg("Access token found, validating");
                    WebClient client2 = new WebClient();
                    client2.Headers.Add("Accept", "application/json");
                    client2.Headers.Add("User-Agent", UserAgent);
                    client2.Headers.Add("Authorization", "Bearer " + accessToken);
                    string response2 = null;
                    bool threwError2 = false;
                    try
                    {
                        response2 = client2.DownloadString("https://api.github.com/user");
                    }
                    catch (WebException e)
                    {
                        threwError2 = true;
                        HttpStatusCode statusCode = ((HttpWebResponse)e.Response).StatusCode;
                        string statusDescription = ((HttpWebResponse)e.Response).StatusDescription;
                        if (statusCode == System.Net.HttpStatusCode.Unauthorized || statusCode == System.Net.HttpStatusCode.Forbidden)
                        {
                            Logger.Warning("Access token expired or is incorrect");
                        }
                        else
                        {
                            Logger.Error
                                    ($"Failed to validate access token, returned {statusCode} with following message:\n{statusDescription}");
                            client2.Dispose();
                        }
                    }
                    catch (Exception e)
                    {
                        threwError2 = true;
                        Logger.Error
                            ($"Failed to validate access token, unexpected error occured:\n{e}");
                    }
                    if (!threwError2)
                    {
                        if (!string.IsNullOrEmpty(response2))
                        {
                            var data = JSON.Load(response2).Make<Dictionary<string, string>>();
                            Logger.Msg($"Successfully validated access token, belongs to {data["name"]} ({data["followers"]} Followers)");
                            return;
                        }
                    }
                }
                Logger.Msg("Requesting Device Flow");
                WebClient client = new WebClient();
                string scopes = "read:user";
                client.Headers.Add("Accept", "application/json");
                client.Headers.Add("User-Agent", UserAgent);

                var _params = new NameValueCollection
                {
                    { "client_id", ClientID },
                    { "scope", scopes }
                };

                byte[] response = null;
                bool threwError = false;
                try
                {
                    response = client.UploadValues("https://github.com/login/device/code", "POST", _params);
                }
                catch (WebException e)
                {
                    threwError = true;
                    HttpStatusCode statusCode = ((HttpWebResponse)e.Response).StatusCode;
                    string statusDescription = ((HttpWebResponse)e.Response).StatusDescription;
                    Logger.Msg("Error");
                    Logger.Error
                            ($"Failed to use Device Flow using Github, returned {statusCode} with following message:\n{statusDescription}");
                    client.Dispose();
                }
                catch (Exception e)
                {
                    threwError = true;
                    Logger.Error
                        ($"Failed to use Device Flow using Github, unexpected error occured:\n{e}");
                }
                if (!threwError)
                {
                    string body = Encoding.UTF8.GetString(response);
                    if (body != null)
                    {
                        var data = JSON.Load(body).Make<Dictionary<string, string>>();
                        Logger.MsgPastel($@"To use Github in the plugin, it is recommended that you make authenticated requests, to do that:

Go to {data["verification_uri"].ToString().Pastel(Color.Cyan)} and enter {data["user_code"].ToString().Pastel(Color.Aqua)}, when you do that press any key
You have {Math.Round((decimal)(int.Parse(data["expires_in"]) / 60))} minutes to enter the code before it expires!
Press any key to continue, press N to continue without using authenticated requests (You will be limited to 60 requests, instead of 1000)

If you do not want to do this, go to UserData/MelonAutoUpdater/ExtensionsConfig and open Github.json, in there set 'UseDeviceFlow' to false");
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
                                    return;
                                }
                                else
                                {
                                    Logger.Msg("Checking if authorized");

                                    var _params2 = new NameValueCollection
                                    {
                                        { "client_id", ClientID },
                                        { "device_code", data["device_code"] },
                                        { "grant_type", "urn:ietf:params:oauth:grant-type:device_code" }
                                    };

                                    byte[] res = null;
                                    bool threwError2 = false;
                                    WebClient client2 = new WebClient();
                                    client2.Headers.Add("Accept", "application/json");
                                    client2.Headers.Add("User-Agent", UserAgent);
                                    try
                                    {
                                        res = client2.UploadValues($"https://github.com/login/oauth/access_token", "POST", _params2);
                                    }
                                    catch (WebException e)
                                    {
                                        threwError2 = true;
                                        HttpStatusCode statusCode = ((HttpWebResponse)e.Response).StatusCode;
                                        string statusDescription = ((HttpWebResponse)e.Response).StatusDescription;
                                        Logger.Error
                                                   ($"Failed to validate if authorized using Github, returned {statusCode} with following message:\n{statusDescription}");
                                    }
                                    catch (Exception e)
                                    {
                                        threwError2 = true;
                                        Logger.Error
                                            ($"Failed to validate if authorized using Github, unexpected error occured:\n{e}");
                                    }
                                    if (!threwError2)
                                    {
                                        string _body = Encoding.UTF8.GetString(res);
                                        if (_body != null)
                                        {
                                            var _data = JSON.Load(_body).Make<Dictionary<string, string>>();
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
                                                    Logger.MsgPastel("Successfully retrieved access token".Pastel(Color.LawnGreen));

                                                    client.Dispose();
                                                    client2.Dispose();

                                                    return;
                                                }
                                            }
                                            else
                                            {
                                                Logger.Warning("Missing required data from request");
                                            }
                                        }
                                    }
                                    client2.Dispose();
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
            }
            else
            {
                Logger.Msg("Device Flow is disabled, using unauthenticated requests");
            }
        }

        internal MelonData Check(string author, string repo)
        {
            WebClient client = new WebClient();
            client.Headers.Add("Accept", "application/vnd.github+json");
            client.Headers.Add("User-Agent", UserAgent);
            if (!string.IsNullOrEmpty(AccessToken)) client.Headers.Add("Authorization", "Bearer " + AccessToken);
            if (disableGithubAPI && DateTimeOffset.UtcNow.ToUnixTimeSeconds() > githubResetDate) disableGithubAPI = false;
            if (!disableGithubAPI)
            {
#pragma warning disable IDE0059 // Unnecessary assignment of a value
                // For some reason Visual Studio doesn't like me doing that
                string response = null;
#pragma warning restore IDE0059 // Unnecessary assignment of a value
                try
                {
                    response = client.DownloadString($"https://api.github.com/repos/{author}/{repo}/releases/latest");
                }
                catch (WebException e)
                {
                    HttpStatusCode statusCode = ((HttpWebResponse)e.Response).StatusCode;
                    string statusDescription = ((HttpWebResponse)e.Response).StatusDescription;
                    if (client.ResponseHeaders.Contains("x-ratelimit-remaining")
                        && client.ResponseHeaders.Contains("x-ratelimit-reset")
                        && client.ResponseHeaders.Contains("x-ratelimit-limit"))
                    {
                        int remaining = int.Parse(client.ResponseHeaders.Get("x-ratelimit-remaining"));
                        int limit = int.Parse(client.ResponseHeaders.Get("x-ratelimit-limit"));
                        long reset = long.Parse(client.ResponseHeaders.Get("x-ratelimit-reset"));
                        if (remaining <= 0)
                        {
                            Logger.Error($"You've reached the rate limit of Github API ({limit}) and you will be able to use the Github API again at {DateTimeOffsetHelper.FromUnixTimeSeconds(reset).ToLocalTime():t}");
                            githubResetDate = reset;
                            disableGithubAPI = true;
                        }
                        else
                        {
                            if (statusCode == System.Net.HttpStatusCode.NotFound)
                            {
                                Logger.Warning("Github API could not find the mod/plugin");
                            }
                            else
                            {
                                Logger.Error
                                    ($"Failed to fetch package information from Github, returned {statusCode} with following message:\n{statusDescription}");
                            }
                        }
                    }
                    else
                    {
                        if (statusCode == System.Net.HttpStatusCode.NotFound)
                        {
                            Logger.Warning("Github API could not find the mod/plugin");
                        }
                        else
                        {
                            Logger.Error
                                ($"Failed to fetch package information from Github, returned {statusCode} with following message:\n{statusDescription}");
                        }
                    }
                    client.Dispose();

                    return null;
                }

                if (client.ResponseHeaders.Contains("x-ratelimit-remaining")
                    && client.ResponseHeaders.Contains("x-ratelimit-reset"))
                {
                    int remaining = int.Parse(client.ResponseHeaders.Get("x-ratelimit-remaining"));
                    long reset = long.Parse(client.ResponseHeaders.Get("x-ratelimit-reset"));
                    if (remaining <= 10)
                    {
                        Logger.Warning("Due to rate limits nearly reached, any attempt to send an API call to Github during this session will be aborted");
                        githubResetDate = reset;
                        disableGithubAPI = true;
                    }
                }
                if (!string.IsNullOrEmpty(response))
                {
                    var data = JSON.Load(response);
                    string version = (string)data["tag_name"];
                    List<FileData> downloadURLs = new List<FileData>();

                    foreach (var file in data["assets"] as ProxyArray)
                    {
                        downloadURLs.Add
                            (new FileData((string)file["browser_download_url"],
                            Path.GetFileNameWithoutExtension((string)file["browser_download_url"]),
                            (string)file["content_type"]));
                    }

                    client.Dispose();
                    if (version.StartsWith("v"))
                    {
                        version = version.Substring(1);
                    }
                    bool isSemVerSuccess = SemVersion.TryParse(version, out SemVersion semver);
                    if (!isSemVerSuccess)
                    {
                        Logger.Error($"Failed to parse version");
                        return null;
                    }
                    return new MelonData(semver, downloadURLs, new Uri($"https://github.com/{author}/{repo}"));
                }
                else
                {
                    Logger.Warning("Github API returned no body");
                    return null;
                }
            }
            else
            {
                Logger.Warning(
                     "Github API access is currently disabled and this check will be aborted, you should be good to use the API at " + DateTimeOffsetHelper.FromUnixTimeSeconds(githubResetDate).ToLocalTime().ToString("t"));
            }

            return null;
        }

        public override MelonData Search(string url, SemVersion currentVersion)
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
            return null;
        }

        public override MelonData BruteCheck(string name, string author, SemVersion currentVersion)
        {
            return Check(author, name);
        }
    }
}