using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using WebMConverter.Core.Processes;

namespace WebMConverter.Core;

/// <summary>Locates a yt-dlp / youtube-dl executable (bundled, app-local, or PATH).</summary>
public static class VideoDownload
{
    public static bool Enabled { get; private set; }
    public static string ExecutableName { get; private set; } = "yt-dlp";

    public static void CheckEnabled()
    {
        string[] names = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ? ["yt-dlp.exe", "yt-dlp", "youtube-dl.exe", "youtube-dl"]
            : ["yt-dlp", "youtube-dl"];

        var candidates = new List<string>();
        foreach (var name in names)
        {
            candidates.Add(Path.Combine(BinaryResolver.AppDirectory, name));
            candidates.Add(Path.Combine(BinaryResolver.AppDirectory, "Binaries", name));
            candidates.Add(name); // Search PATH
        }

        foreach (var candidate in candidates)
        {
            if (Path.IsPathRooted(candidate) && !File.Exists(candidate))
                continue;

            try
            {
                using var proc = new YoutubeDlProcess("", candidate);
                proc.Start(outputReadLine: false);
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
