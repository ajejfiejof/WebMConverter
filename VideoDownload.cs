
using System;
using System.Diagnostics;
using System.Windows.Forms;
using WebMConverter.Dialogs;

namespace WebMConverter
{
    static class VideoDownload
    {
        public static bool DownloadEnabled { get; private set; }

        public static void CheckIfDownloadIsEnabled()
        {
            try
            {
                var proc = new YoutubeDL("");
                proc.Start(false);
            }
            catch (Exception)
            {
                DownloadEnabled = false;
                return;
            }
            DownloadEnabled = true;
        }
    }

    class YoutubeDL : Process
    {
        public YoutubeDL(string arguments)
        {
            StartInfo.FileName = "youtube-dl.exe";
            StartInfo.Arguments = arguments;
            StartInfo.RedirectStandardInput = true;
            StartInfo.RedirectStandardOutput = true;
            StartInfo.RedirectStandardError = true;
            StartInfo.UseShellExecute = false; //Required to redirect IO streams
            StartInfo.CreateNoWindow = true; //Hide console
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
