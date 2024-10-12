extern alias ml065;

using MelonAutoUpdater.Helper;
using ml065.MelonLoader.TinyJSON;
using ml065.Semver;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using ml065.MelonLoader;
using System.Drawing;
using MelonAutoUpdater.Utils;
using System.Net;
using System.Diagnostics;
using System.Net.Http;
using System.Threading.Tasks;

namespace MelonAutoUpdater.Extensions.Included.Github
{
    internal class Github : SearchExtension
    {
        public override string Name => "Github";

        public override SemVersion Version => new SemVersion(1, 1, 0);

        public override string Author => "HAHOOS";

        public override string Link => "https://github.com";

        public override bool BruteCheckEnabled => true;

        /// <summary>
        /// The time (in Unix time seconds) when the rate limit will disappear
        /// </summary>
        private long GithubResetDate;

        private readonly string ClientID = "Iv23lii0ysyknh3Vf51t";

        internal string AccessToken;

        private readonly char[] disallowedChars =
            { '!', '@', '#', '$', '%', '^', '&', '*', '(', ')', '+', '=', '[', '{', '}', ']', ':', ';', '\'', '\"', '|', '\\', '<', ',', '>', '/', '?', '~', '`', ' ' };

        // Melon Preferences

        private MelonPreferences_Category category;

        private MelonPreferences_Entry entry_useDeviceFlow;
        private MelonPreferences_Entry entry_accessToken;
        private MelonPreferences_Entry entry_validateToken;

        internal void CheckRateLimit()
        {
            Logger.Msg("Checking rate limit");
            HttpClient client = new HttpClient();
            client.DefaultRequestHeaders.Add("User-Agent", UserAgent);
            client.DefaultRequestHeaders.Add("Authorization", "Bearer " + GetEntryValue<string>(entry_accessToken));
            client.DefaultRequestHeaders.Add("Accept", "application/vnd.github+json");
            var res = client.GetAsync("https://api.github.com/rate_limit");
            res.Wait();
            Variant json = null;
            if (res.Result.IsSuccessStatusCode)
            {
                Task<string> _body = res.Result.Content.ReadAsStringAsync();
                _body.Wait();
                if (_body.Result != null)
                {
                    json = JSON.Load(_body.Result);
                }
                else
                {
                    Logger.Msg("No body returned while checking rate limit, aborting");
                    client.Dispose();
                    res.Dispose();
                    _body.Dispose();
                    return;
                }
            }
            else
            {
                if (res.Result.StatusCode != HttpStatusCode.Unauthorized) Logger.Error($"Unable to check current rate limit, Github API returned {res.Result.StatusCode} status code with following message:\n{res.Result.ReasonPhrase}");
                else
                {
                    client.DefaultRequestHeaders.Remove("Authorization");
                    var res2 = client.GetAsync("https://api.github.com/rate_limit");
                    res2.Wait();
                    if (res2.Result.IsSuccessStatusCode)
                    {
                        Task<string> _body = res2.Result.Content.ReadAsStringAsync();
                        _body.Wait();
                        if (_body.Result != null)
                        {
                            json = JSON.Load(_body.Result);
                        }
                        else
                        {
                            Logger.Msg("No body returned while checking rate limit, aborting");
                            client.Dispose();
                            res.Dispose();
                            res2.Dispose();
                            _body.Dispose();
                            return;
                        }
                    }
                }
            }

            if (json != null)

            {
                try
                {
                    var core = json["resources"]["core"];
                    var remaining = (int)core["remaining"];
                    var reset = (long)core["reset"];
                    var limit = (long)core["limit"];
                    if (remaining <= 1)
                    {
                        GithubResetDate = reset;
                        Logger.Warning($"Disabled the use of the API till {DateTimeOffset.FromUnixTimeSeconds(reset):t}");
                    }
                    else
                    {
                        Logger.Msg($"Remaining requests: {remaining}/{limit}");
                    }
                }
                catch (Exception ex)
                {
                    Logger.Error("Failed to parse JSON from response when checking rate limit, exception:\n" + ex);
                    return;
                }
            }
        }

        private static bool ShouldNotUseWriter()
        {
            try
            {
                Console.Write(string.Empty);
                return false;
            }
            catch
            {
                return true;
            }
        }

        internal void CheckAccessToken()
        {
            var accessToken = GetEntryValue<string>(entry_accessToken);
            AccessToken = accessToken;
            var use = GetEntryValue<bool>(entry_useDeviceFlow);
            var validate = GetEntryValue<bool>(entry_validateToken);
            if (use && validate)
            {
                if (!string.IsNullOrEmpty(accessToken))
                {
                    Logger.Msg("Access token found, validating");
                    HttpClient client2 = new HttpClient();
                    client2.DefaultRequestHeaders.Add("Accept", "application/json");
                    client2.DefaultRequestHeaders.Add("User-Agent", UserAgent);
                    client2.DefaultRequestHeaders.Add("Authorization", "Bearer " + accessToken);
                    Stopwatch sw = null;
                    if (MelonAutoUpdater.Debug) sw = Stopwatch.StartNew();
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
                            if (MelonAutoUpdater.Debug)
                            {
                                sw.Stop();
                                MelonAutoUpdater.ElapsedTime.Add("ValidateGithub", sw.ElapsedMilliseconds);
                            }
                            return;
                        }
                        else
                        {
                            Logger.Error("No body was returned while validating access token, aborting check");
                            if (MelonAutoUpdater.Debug)
                            {
                                sw.Stop();
                                MelonAutoUpdater.ElapsedTime.Add("ValidateGithub", sw.ElapsedMilliseconds);
                            }
                            client2.Dispose();
                            response2.Dispose();
                            body2.Dispose();
                        }
                    }
                    else
                    {
                        if (response2.Result.StatusCode == System.Net.HttpStatusCode.Unauthorized || response2.Result.StatusCode == System.Net.HttpStatusCode.Forbidden)
                        {
                            Logger.Warning("Access token expired or is incorrect");
                            if (MelonAutoUpdater.Debug)
                            {
                                sw.Stop();
                                MelonAutoUpdater.ElapsedTime.Add("ValidateGithub", sw.ElapsedMilliseconds);
                            }
                            client2.Dispose();
                            response2.Dispose();
                        }
                        else
                        {
                            Logger.Msg("Error");
                            Logger.Error
                                    ($"Failed to validate access token, returned {response2.Result.StatusCode} with following message:\n{response2.Result.ReasonPhrase}");
                            if (MelonAutoUpdater.Debug)
                            {
                                sw.Stop();
                                MelonAutoUpdater.ElapsedTime.Add("ValidateGithub", sw.ElapsedMilliseconds);
                            }
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
                    Logger.DebugMsg("Getting string from bytes");
                    var body = response.Result.Content.ReadAsStringAsync();
                    body.Wait();
                    if (body.Result != null)
                    {
                        Logger.DebugMsg("Body is not empty");
                        if (!ShouldNotUseWriter())
                        {
                            var data = JSON.Load(body.Result).Make<Dictionary<string, string>>();
                            if (!data.ContainsKeys("verification_uri", "user_code", "expires_in", "device_code"))
                            {
                                Logger.Warning("Insufficient data provided by the API, unable to continue");
                                return;
                            }
                            Logger.MsgPastel($@"To use Github in the plugin, it is recommended that you make authenticated requests, to do that:

Go {data["verification_uri"].ToString().Pastel(Theme.Instance.LinkColor).Underline().Blink()} and enter {data["user_code"].ToString().Pastel(Color.Aqua)}, when you do that press any key
You have {Math.Round((decimal)(int.Parse(data["expires_in"]) / 60))} minutes to enter the code before it expires!
Press any key to continue, press N to continue without using authenticated requests (You will be limited to 60 requests / hour, instead of 5000 requests / hour)

If you do not want to do this, go to UserData/MelonAutoUpdater/ExtensionsConfig and open Github.json, in there set 'UseDeviceFlow' to false");
                            bool canUse = true;
                            while (true)
                            {
                                var key = Console.ReadKey(false);
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
                                                        Logger.MsgPastel("Successfully retrieved access token".Pastel(Color.LawnGreen));

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
                                        }
                                        else
                                        {
                                            Logger.Error
                                                ($"Failed to use Device Flow using Github, returned {res.Result.StatusCode} with following message:\n{res.Result.ReasonPhrase}");
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
                        else
                        {
                            var data = JSON.Load(body.Result).Make<Dictionary<string, string>>();
                            Logger.MsgPastel(
                                $"Due to the fact that the console cannot be used, you will have to manually do the process, which should be described in the Wiki on the Github page. Your code is {data["user_code"]} and it expires within {Math.Round((decimal)(int.Parse(data["expires_in"]) / 60))} minutes. If you do not want to do this, go to UserData/MelonAutoUpdater/ExtensionsConfig and open Github.json, in there set 'UseDeviceFlow' to false. This should make the plugin run faster, but authorized requests wont be used (you will be limited to 60 requests / hour, rather than 5000 requests / hour).");
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
                if (!use) Logger.Msg("Device Flow is disabled, using unauthenticated requests");
                else if (!validate) Logger.Msg("Not validating, as set to not in the config");
            }
        }

        public override void OnInitialization()
        {
            category = CreateCategory();
            entry_useDeviceFlow = category.CreateEntry("UseDeviceFlow", true, "Use Device Flow",
                description: "If enabled, you will be prompted to authenticate using Github's Device Flow to make authenticated requests if access token is not registered or valid (will raise request limit from 60 to 5000)\nDefault: true");
            entry_validateToken = category.CreateEntry("ValidateToken", true, "Validate Token",
                description: "If enabled, the access token will be validated, disabling this can result for the plugin to be ~400 ms faster");
            entry_accessToken = category.CreateEntry("AccessToken", string.Empty, "Access Token",
                description: "Access Token used to make authenticated requests (Do not edit if you do not know what you're doing)");

            category.SaveToFile(false);

            Logger.Msg("Checking if Access Token exists");

            CheckAccessToken();
            CheckRateLimit();
        }

        internal MelonData Check(string author, string repo)
        {
            HttpClient client = new HttpClient();
            client.DefaultRequestHeaders.Add("Accept", "application/vnd.github+json");
            client.DefaultRequestHeaders.Add("User-Agent", UserAgent);
            if (!string.IsNullOrEmpty(AccessToken)) client.DefaultRequestHeaders.Add("Authorization", "Bearer " + AccessToken);
            if (DateTimeOffset.UtcNow.ToUnixTimeSeconds() > GithubResetDate)
            {
                var response = client.GetAsync($"https://api.github.com/repos/{author}/{repo}/releases/latest", HttpCompletionOption.ResponseContentRead);
                response.Wait();
                if (response.Result.IsSuccessStatusCode)
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
                            Logger.Error($"You've reached the rate limit of Github API ({limit}) and you will be able to use the Github API again at {DateTimeOffset.FromUnixTimeSeconds(reset).ToLocalTime():t}");
                            GithubResetDate = reset;
                        }
                        Logger.DebugMsg($"Remaining requests until rate-limit: {remaining}/{limit}");
                    }
                    var body = response.Result.Content.ReadAsStringAsync();
                    body.Wait();
                    if (body.Result != null)
                    {
                        var data = JSON.Load(body.Result);
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
                    if (response.Result.Headers.Contains("x-ratelimit-remaining")
                      && response.Result.Headers.Contains("x-ratelimit-reset")
                      && response.Result.Headers.Contains("x-ratelimit-limit"))
                    {
                        int remaining = int.Parse(response.Result.Headers.GetValues("x-ratelimit-remaining").First());
                        int limit = int.Parse(response.Result.Headers.GetValues("x-ratelimit-limit").First());
                        long reset = long.Parse(response.Result.Headers.GetValues("x-ratelimit-reset").First());
                        if (remaining <= 0)
                        {
                            Logger.Error($"You've reached the rate limit of Github API ({limit}) and you will be able to use the Github API again at {DateTimeOffset.FromUnixTimeSeconds(reset).ToLocalTime():t}");
                            GithubResetDate = reset;
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
                    return null;
                }
            }
            else
            {
                Logger.Warning(
                     "Github API access is currently disabled and this check will be aborted, you should be good to use the API at " + DateTimeOffset.FromUnixTimeSeconds(GithubResetDate).ToLocalTime().ToString("t"));
            }

            return null;
        }

        public override MelonData Search(string url, SemVersion currentVersion)
        {
            Stopwatch stopwatch = null;
            if (MelonAutoUpdater.Debug) stopwatch = Stopwatch.StartNew();
            Regex regex = new Regex(@"github\.com\/([\w.-]+)\/([\w.-]+)");
            var match = regex.Match(url);
            if (match.Success && match.Length >= 1 && match.Groups.Count == 3)
            {
                string authorName = match.Groups[1].Value;
                string repoName = match.Groups[2].Value;
                var check = Check(authorName, repoName);
                if (MelonAutoUpdater.Debug)
                {
                    stopwatch.Stop();
                    MelonAutoUpdater.ElapsedTime.Add($"GithubCheck-{authorName}/{repoName}", stopwatch.ElapsedMilliseconds);
                }
                return check;
            }
            if (MelonAutoUpdater.Debug)
            {
                stopwatch.Stop();
                MelonAutoUpdater.ElapsedTime.Add($"GithubCheck-{MelonUtils.RandomString(10)}", stopwatch.ElapsedMilliseconds);
            }
            return null;
        }

        public override MelonData BruteCheck(string name, string author, SemVersion currentVersion)
        {
            Stopwatch stopwatch = null;
            if (MelonAutoUpdater.Debug) stopwatch = Stopwatch.StartNew();
            if (name.ToCharArray().Where(x => disallowedChars.Contains(x)).Any() || author.ToCharArray().Where(x => disallowedChars.Contains(x)).Any())
            {
                Logger.Warning("Disallowed characters found in Name or Author, cannot brute check");
                if (MelonAutoUpdater.Debug)
                {
                    stopwatch.Stop();
                    MelonAutoUpdater.ElapsedTime.Add($"GithubCheck-{author}/{name}-failed (with Brute Check)", stopwatch.ElapsedMilliseconds);
                }
                return null;
            }
            var check = Check(author, name);
            if (MelonAutoUpdater.Debug)
            {
                stopwatch.Stop();
                MelonAutoUpdater.ElapsedTime.Add($"GithubCheck-{author}/{name} (with Brute Check)", stopwatch.ElapsedMilliseconds);
            }
            return check;
        }
    }
}