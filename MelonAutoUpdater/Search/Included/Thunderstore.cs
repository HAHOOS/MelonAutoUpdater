using MelonLoader.TinyJSON;
using Semver;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace MelonAutoUpdater.Search.Included
{
    internal class Thunderstore : MAUSearch
    {
        public override string Name => "Thunderstore";

        public override SemVersion Version => new SemVersion(1, 0, 0);

        public override string Author => "HAHOOS";

        public override string Link => "https://thunderstore.io";

        public override Task<ModData> Search(string url)
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
                HttpClient request = new HttpClient();
                request.DefaultRequestHeaders.Add("User-Agent", UserAgent);
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
                            return ReturnEmpty();
                        }

                        return Task.Factory.StartNew<ModData>(() => new ModData()
                        {
                            LatestVersion = semver,
                            DownloadFiles = files,
                        });
                    }
                    else
                    {
                        Logger.Error("Thunderstore API returned no body, unable to fetch package information");

                        request.Dispose();
                        response.Dispose();
                        body.Dispose();

                        return ReturnEmpty();
                    }
                }
                else
                {
                    if (response.Result.StatusCode == System.Net.HttpStatusCode.NotFound)
                    {
                        Logger.Warning("Thunderstore API could not locate the mod/plugin");
                    }
                    else
                    {
                        Logger.Error
                            ($"Failed to fetch package information from Thunderstore, returned {response.Result.StatusCode} with following message:\n{response.Result.ReasonPhrase}");
                    }
                    request.Dispose();
                    response.Dispose();

                    return ReturnEmpty();
                }
            }
            return ReturnEmpty();
        }
    }
}