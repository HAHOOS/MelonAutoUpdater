using MelonAutoUpdater.Helper;
using MelonLoader.TinyJSON;
using Semver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;

namespace MelonAutoUpdater.Search.Included.Thunderstore
{
    internal class Thunderstore : MAUSearch
    {
        public override string Name => "Thunderstore";

        public override SemVersion Version => new SemVersion(1, 0, 1);

        public override string Author => "HAHOOS";

        public override string Link => "https://thunderstore.io";

        public override bool BruteCheckEnabled => true;

        private bool disableAPI = false;
        private long apiReset;

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
                try
                {
                    response = request.DownloadString($"https://thunderstore.io/api/experimental/package/{namespaceName}/{packageName}/");
                }
                catch (WebException e)
                {
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
            Regex regex = new Regex(@"(https:\/\/|http:\/\/)(?:.+\.)?thunderstore\.io");
            if (regex.IsMatch(url))
            {
                string[] split = url.Split('/');
                string packageName;
                string namespaceName;
                if (url.EndsWith("/"))
                {
                    packageName = split[split.Length - 2];
                    namespaceName = split[split.Length - 3];
                }
                else
                {
                    packageName = split[split.Length - 1];
                    namespaceName = split[split.Length - 2];
                }
                return Check(packageName, namespaceName);
            }
            return null;
        }

        public override MelonData BruteCheck(string name, string author, SemVersion currentVersion)
        {
            return Check(name.Replace(" ", ""), author.Replace(" ", ""));
        }
    }
}