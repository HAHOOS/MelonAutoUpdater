extern alias ml065;

using MelonAutoUpdater.Helper;
using ml065::Harmony;
using ml065.MelonLoader;
using ml065.MelonLoader.TinyJSON;
using ml065.Semver;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace MelonAutoUpdater.Extensions.Included.Thunderstore
{
    internal class Thunderstore : SearchExtension
    {
        public override string Name => "Thunderstore";

        public override SemVersion Version => new SemVersion(1, 0, 1);

        public override string Author => "HAHOOS";

        public override string Link => "https://thunderstore.io";

        public override bool BruteCheckEnabled => true;

        private bool disableAPI = false;
        private long apiReset;

        private readonly char[] disallowedChars =
            { '!', '@', '#', '$', '%', '^', '&', '*', '(', ')', '-', '+', '=', '[', '{', '}', ']', ':', ';', '\'', '\"', '|', '\\', '<', ',', '>', '.', '/', '?', '~', '`', ' ' };

        internal MelonData Check(string packageName, string namespaceName)
        {
            HttpClient request = new HttpClient();
            request.DefaultRequestHeaders.Add("User-Agent", UserAgent);
            if (disableAPI && DateTimeOffset.UtcNow.ToUnixTimeSeconds() > apiReset) disableAPI = false;
            if (!disableAPI)
            {
                Task<HttpResponseMessage> response = request.GetAsync($"https://thunderstore.io/api/experimental/package/{namespaceName}/{packageName}/");
                response.Wait();
                if (response.Result.IsSuccessStatusCode)
                {
                    Task<string> body = response.Result.Content.ReadAsStringAsync();
                    body.Wait();
                    if (body.Result != null)
                    {
                        var _data = JSON.Load(body.Result);

                        request.Dispose();
                        response.Dispose();
                        body.Dispose();

                        List<FileData> files = new List<FileData>();

                        FileData fileData = new FileData
                        {
                            FileName = packageName,
                            URL = (string)_data["latest"]["download_url"]
                        };

                        files.Add(fileData);

                        bool isSemVerSuccess = SemVersion.TryParse((string)_data["latest"]["version_number"], out SemVersion semver);
                        if (!isSemVerSuccess)
                        {
                            Logger.Error($"Failed to parse version");
                            return null;
                        }
                        var communityListings = _data["community_listings"] as ProxyArray;
                        string community = communityListings.First()["community"];
                        return new MelonData(semver, files, new Uri($"https://thunderstore.io/c/{community}/p/{namespaceName}/{packageName}"));
                    }
                    else
                    {
                        Logger.Error("Thunderstore API returned no body, unable to fetch package information");

                        request.Dispose();
                        response.Dispose();
                        body.Dispose();

                        return null;
                    }
                }
                else
                {
                    if (response.Result.StatusCode == System.Net.HttpStatusCode.NotFound)
                    {
                        Logger.Warning("Thunderstore API could not locate the mod/plugin");
                    }
                    else if (response.Result.StatusCode == System.Net.HttpStatusCode.Forbidden || (int)response.Result.StatusCode == 429)
                    {
                        disableAPI = true;
                        apiReset = DateTimeOffset.Now.AddMinutes(1).ToUnixTimeSeconds();
                    }
                    else
                    {
                        Logger.Error
                            ($"Failed to fetch package information from Thunderstore, returned {response.Result.StatusCode} with following message:\n{response.Result.ReasonPhrase}");
                    }
                    request.Dispose();
                    response.Dispose();

                    return null;
                }
            }
            else
            {
                Logger.Warning("Thunderstore API is currently disabled for a minute to avoid rate limit");
            }
            return null;
        }

        public override MelonData Search(string url, SemVersion currentVersion)
        {
            Stopwatch stopwatch = null;
            if (MelonAutoUpdater.Debug) stopwatch = Stopwatch.StartNew();
            Regex regex = new Regex(@"thunderstore.io(?:/c/[\w]+/p/|/package/)(?!_)([\w]+)(?!_)/(?!_)([\w]+)(?!_)");
            var match = regex.Match(url);
            if (match.Success && match.Length >= 1 && match.Groups.Count == 3)
            {
                string namespaceName = match.Groups[1].Value;
                string packageName = match.Groups[2].Value;
                var check = Check(packageName, namespaceName);
                if (MelonAutoUpdater.Debug)
                {
                    stopwatch.Stop();
                    MelonAutoUpdater.ElapsedTime.Add($"ThunderstoreCheck-{namespaceName}/{packageName}", stopwatch.ElapsedMilliseconds);
                }
                return check;
            }
            if (MelonAutoUpdater.Debug)
            {
                stopwatch.Stop();
                MelonAutoUpdater.ElapsedTime.Add($"ThunderstoreCheck-{MelonUtils.RandomString(10)}", stopwatch.ElapsedMilliseconds);
            }
            return null;
        }

        private bool IsValid(string nameOrAuthor)
        {
            if (nameOrAuthor.StartsWith("_")) return false;
            else if (nameOrAuthor.EndsWith("_")) return false;
            else if (nameOrAuthor.ToCharArray().Where(x => disallowedChars.Contains(x)).Any()) return false;
            else return true;
        }

        public override MelonData BruteCheck(string name, string author, SemVersion currentVersion)
        {
            Stopwatch stopwatch = null;
            if (MelonAutoUpdater.Debug) stopwatch = Stopwatch.StartNew();
            if (!IsValid(name) || !IsValid(author))
            {
                Logger.Warning("Disallowed characters found in Name or Author, cannot brute check");
                if (MelonAutoUpdater.Debug)
                {
                    stopwatch.Stop();
                    MelonAutoUpdater.ElapsedTime.Add($"ThunderstoreCheck-{author}/{name}-failed (with Brute Check)", stopwatch.ElapsedMilliseconds);
                }
                return null;
            }

            var check = Check(name.Replace(" ", ""), author.Replace(" ", ""));

            if (MelonAutoUpdater.Debug)
            {
                stopwatch.Stop();
                MelonAutoUpdater.ElapsedTime.Add($"ThunderstoreCheck-{author}/{name} (with Brute Check)", stopwatch.ElapsedMilliseconds);
            }
            return check;
        }
    }
}