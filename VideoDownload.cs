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
            string[] names = { "yt-dlp.exe", "yt-dlp", "youtube-dl.exe", "youtube-dl" };
            var candidates = new System.Collections.Generic.List<string>();
            foreach (var name in names)
            {
                candidates.Add(System.IO.Path.Combine(Program.AppDirectory, name));
                candidates.Add(System.IO.Path.Combine(Program.AppDirectory, "Binaries", name));
                candidates.Add(name); // Search PATH
            }

            foreach (var candidate in candidates)
            {
                if (System.IO.Path.IsPathRooted(candidate) && !System.IO.File.Exists(candidate))
                    continue;

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
