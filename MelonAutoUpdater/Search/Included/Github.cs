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

namespace MelonAutoUpdater.Search.Included
{
    internal class Github : MAUSearch
    {
        public override string Name => "Github";

        public override SemVersion Version => new SemVersion(1, 0, 0);

        public override string Author => "HAHOOS";

        public override string Link => "https://github.com";

        /// <summary>
        /// This is used to prevent from rate-limiting the API
        /// </summary>
        private bool disableGithubAPI = false;

        /// <summary>
        /// The time (in Unix time seconds) when the rate limit will disappear
        /// </summary>
        private long githubResetDate;

        public override Task<ModData> Search(string url, SemVersion currentVersion)
        {
            Regex regex = new Regex(@"(?<=(?<=http:\/\/|https:\/\/)github.com\/)(.*?)(?>\/)(.*?)(?=\/|$)");
            var match = regex.Match(url);
            if (match.Success)
            {
                string[] split = match.Value.Split('/');
                string packageName = split[0];
                string namespaceName = split[1];
                HttpClient client = new HttpClient();
                client.DefaultRequestHeaders.Add("Accept", "application/vnd.github+json");
                client.DefaultRequestHeaders.Add("User-Agent", UserAgent);
                if (disableGithubAPI && DateTimeOffset.UtcNow.ToUnixTimeSeconds() > githubResetDate) disableGithubAPI = false;
                if (!disableGithubAPI)
                {
                    var response = client.GetAsync($"https://api.github.com/repos/{namespaceName}/{packageName}/releases/latest", HttpCompletionOption.ResponseContentRead);
                    response.Wait();
                    if (response.Result.IsSuccessStatusCode)
                    {
                        int remaining = int.Parse(response.Result.Headers.GetValues("x-ratelimit-remaining").First());
                        long reset = long.Parse(response.Result.Headers.GetValues("x-ratelimit-reset").First());
                        if (remaining <= 1)
                        {
                            Logger.Warning("Due to rate limits nearly reached, any attempt to send an API call to Github during this session will be aborted");
                            githubResetDate = reset;
                            disableGithubAPI = true;
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
                                downloadURLs.Add(new FileData() { URL = (string)file["browser_download_url"], ContentType = (string)file["content_type"], FileName = Path.GetFileNameWithoutExtension((string)file["browser_download_url"]) });
                            }

                            client.Dispose();
                            response.Dispose();
                            body.Dispose();
                            bool isSemVerSuccess = SemVersion.TryParse(version, out SemVersion semver);
                            if (!isSemVerSuccess)
                            {
                                Logger.Error($"Failed to parse version");
                                return ReturnEmpty();
                            }
                            return Task.Factory.StartNew(() => new ModData()
                            {
                                LatestVersion = semver,
                                DownloadFiles = downloadURLs,
                            });
                        }
                        else
                        {
                            Logger.Error("Github API returned no body, unable to fetch package information");

                            client.Dispose();
                            response.Dispose();
                            body.Dispose();

                            return ReturnEmpty();
                        }
                    }
                    else
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
                        client.Dispose();
                        response.Dispose();

                        return ReturnEmpty();
                    }
                }
                else
                {
                    Logger.Warning(
                         "Github API access is currently disabled and this check will be aborted, you should be good to use the API at " + DateTimeOffsetHelper.FromUnixTimeSeconds(githubResetDate).ToLocalTime().ToString("t"));
                }
            }
            return ReturnEmpty();
        }
    }
}