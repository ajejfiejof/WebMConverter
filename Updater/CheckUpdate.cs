using System;
using System.IO;
using System.Net;
using System.Text.RegularExpressions;
using Semver;

namespace WebMConverter.Updater
{
    public class ReleaseInfo
    {
        public string Version { get; set; }
        public string Changelog { get; set; }
        public string DownloadUrl { get; set; }
    }

    static partial class Program
    {
        public static ReleaseInfo FetchLatestRelease()
        {
            var request = (HttpWebRequest)WebRequest.Create(GitHubApiUrl);
            request.UserAgent = UserAgent;
            request.Method = "GET";

            using (var response = (HttpWebResponse)request.GetResponse())
            using (var stream = response.GetResponseStream())
            using (var reader = new StreamReader(stream))
            {
                var json = reader.ReadToEnd();

                var tagMatch = Regex.Match(json, @"""tag_name""\s*:\s*""([^""]+)""");
                if (!tagMatch.Success)
                    throw new Exception("Unable to find release tag in GitHub response.");

                var tag = tagMatch.Groups[1].Value.TrimStart('v', 'V');

                var bodyMatch = Regex.Match(json, @"""body""\s*:\s*""((?:\\.|[^""\\])*)""");
                var body = bodyMatch.Success ? Regex.Unescape(bodyMatch.Groups[1].Value) : "";

                var assetMatch = Regex.Match(json, @"""browser_download_url""\s*:\s*""([^""]+\.zip)""");
                var downloadUrl = assetMatch.Success ? assetMatch.Groups[1].Value : null;

                if (string.IsNullOrEmpty(downloadUrl))
                {
                    var zipballMatch = Regex.Match(json, @"""zipball_url""\s*:\s*""([^""]+)""");
                    downloadUrl = zipballMatch.Success ? zipballMatch.Groups[1].Value : null;
                }

                return new ReleaseInfo
                {
                    Version = tag,
                    Changelog = body,
                    DownloadUrl = downloadUrl
                };
            }
        }

        public static bool CheckUpdate(string currentVerString, out string output)
        {
            output = "";

            try
            {
                var currentVersion = SemVersion.Parse(currentVerString.TrimStart('v', 'V'), SemVersionStyles.Any);
                var release = FetchLatestRelease();
                var latestVersion = SemVersion.Parse(release.Version, SemVersionStyles.Any);

                if (latestVersion.ComparePrecedenceTo(currentVersion) > 0)
                {
                    output = latestVersion.ToString() + Environment.NewLine + release.Changelog;
                }

                return true;
            }
            catch (Exception e)
            {
                output = e.Message;
                return false;
            }
        }
    }
}