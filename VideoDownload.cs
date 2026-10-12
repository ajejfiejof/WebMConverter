using System;
using System.Diagnostics;

namespace WebMConverter
{
    static class VideoDownload
    {
        public static bool Enabled { get; private set; }
        public static string ExecutableName { get; private set; } = "yt-dlp.exe";

        public static void CheckEnabled()
        {
            string[] candidates = { "yt-dlp.exe", "yt-dlp", "youtube-dl.exe", "youtube-dl" };
            foreach (var candidate in candidates)
            {
                try
                {
                    var proc = new YoutubeDL("", candidate);
                    proc.Start(false);
                    ExecutableName = candidate;
                    Enabled = true;
                    return;
                }
                catch (Exception)
                {
                    // Try next candidate
                }
            }
            Enabled = false;
        }
    }

    class YoutubeDL : Process
    {
        public YoutubeDL(string arguments, string executable = null)
        {
            StartInfo.FileName = executable ?? VideoDownload.ExecutableName;
            StartInfo.Arguments = arguments;
            StartInfo.RedirectStandardInput = true;
            StartInfo.RedirectStandardOutput = true;
            StartInfo.RedirectStandardError = true;
            StartInfo.UseShellExecute = false; // Required to redirect IO streams
            StartInfo.CreateNoWindow = true; // Hide console
            EnableRaisingEvents = true;
        }

        new public void Start()
        {
            Start(true);
        }

        public void Start(bool OutputReadLine)
        {
            base.Start();
            BeginErrorReadLine();
            if (OutputReadLine)
                BeginOutputReadLine();
        }
    }
}
