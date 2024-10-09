extern alias ml065;

using MelonAutoUpdater.Helper;
using ml065.MelonLoader;
using ml065.MelonLoader.TinyJSON;
using ml065.Semver;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;

namespace MelonAutoUpdater.Search.Included.Thunderstore
{
    internal class Thunderstore : MAUExtension
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
            WebClient request = new WebClient();
            request.Headers.Add("User-Agent", UserAgent);
            if (disableAPI && DateTimeOffset.UtcNow.ToUnixTimeSeconds() > apiReset) disableAPI = false;
            if (!disableAPI)
            {
#pragma warning disable IDE0059 // Unnecessary assignment of a value
                // For some reason Visual Studio doesn't like me doing that
                string response = string.Empty;
#pragma warning restore IDE0059 // Unnecessary assignment of a value
                Stopwatch sw = null;
                try
                {
                    if (MelonAutoUpdater.Debug)
                    {
                        sw = Stopwatch.StartNew();
                    }
                    response = request.DownloadString($"https://thunderstore.io/api/experimental/package/{namespaceName}/{packageName}/");
                    if (MelonAutoUpdater.Debug)
                    {
                        sw.Stop();
                        MelonAutoUpdater.ElapsedTime.Add($"ThunderstoreCheck-{namespaceName}/{packageName}-{MelonUtils.RandomString(5)}", sw.ElapsedMilliseconds);
                    }
                }
                catch (WebException e)
                {
                    if (MelonAutoUpdater.Debug)
                    {
                        sw.Stop();
                        MelonAutoUpdater.ElapsedTime.Add($"ThunderstoreCheck-{namespaceName}/{packageName}-{MelonUtils.RandomString(5)}", sw.ElapsedMilliseconds);
                    }
                    HttpStatusCode statusCode = ((HttpWebResponse)e.Response).StatusCode;
                    string statusDescription = ((HttpWebResponse)e.Response).StatusDescription;
                    if (statusCode == HttpStatusCode.NotFound)
                    {
                        Logger.Warning("Thunderstore API could not locate the mod/plugin");
                    }
                    else if (statusCode == HttpStatusCode.Forbidden || statusCode == (HttpStatusCode)429)
                    {
                        disableAPI = true;
                        apiReset = DateTimeOffset.Now.AddMinutes(1).ToUnixTimeSeconds();
                    }
                    else
                    {
                        Logger.Error
                            ($"Failed to fetch package information from Thunderstore, returned {statusCode} with following message:\n{statusDescription}");
                    }
                    request.Dispose();

                    return null;
                }
                if (!string.IsNullOrEmpty(response))
                {
                    var _data = JSON.Load(response);

                    request.Dispose();

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
                    var first = communityListings.First();
                    var community = first["community"];

                    return new MelonData()
                    {
                        LatestVersion = semver,
                        DownloadFiles = files,
                        DownloadLink = new Uri($"https://thunderstore.io/c/{community}/p/{namespaceName}/{packageName}/")
                    };
                }
                else
                {
                    Logger.Warning("Thunderstore API returned no body");
                }
            }
            return null;
        }

        public override MelonData Search(string url, SemVersion currentVersion)
        {
            Regex regex = new Regex(@"thunderstore.io(?:/c/[\w]+/p/|/package/)(?!_)([\w]+)(?!_)/(?!_)([\w]+)(?!_)");
            var match = regex.Match(url);
            if (match.Success && match.Length >= 1 && match.Groups.Count == 3)
            {
                string namespaceName = match.Groups[1].Value;
                string packageName = match.Groups[2].Value;
                return Check(packageName, namespaceName);
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
            if (!IsValid(name) || !IsValid(author))
            {
                Logger.Warning("Disallowed characters found in Name or Author, cannot brute check");
                return null;
            }

            return Check(name.Replace(" ", ""), author.Replace(" ", ""));
        }
    }
}