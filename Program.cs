using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WebMConverter
{
    #region Filters

    static class Filters
    {
        internal static CaptionFilter Caption = null;
        internal static CropFilter Crop = null;
        internal static DeinterlaceFilter Deinterlace = null;
        internal static DenoiseFilter Denoise = null;
        internal static DubFilter Dub;
        internal static FadeFilter Fade;
        internal static LevelsFilter Levels = null;
        internal static MultipleTrimFilter MultipleTrim = null;
        internal static OverlayFilter Overlay = null;
        internal static RateFilter Rate = null;
        internal static ResizeFilter Resize = null;
        internal static ReverseFilter Reverse = null;
        internal static RotateFilter Rotate;
        internal static SubtitleFilter Subtitle = null;
        internal static TrimFilter Trim = null;

        internal static void ResetFilters()
        {
            Caption = null;
            Crop = null;
            Deinterlace = null;
            Denoise = null;
            Dub = null;
            Fade = null;
            Levels = null;
            MultipleTrim = null;
            Overlay = null;
            Rate = null;
            Resize = null;
            Reverse = null;
            Rotate = null;
            Subtitle = null;
            Trim = null;
        }

        internal static string GetFFmpegVideoFilter(int inputWidth = 0, int inputHeight = 0)
        {
            var filters = new List<string>();

            if (Crop != null && inputWidth > 0 && inputHeight > 0)
            {
                int w = inputWidth - Crop.Left + Crop.Right;
                int h = inputHeight - Crop.Top + Crop.Bottom;
                if (w > 0 && h > 0)
                    filters.Add($"crop={w}:{h}:{Crop.Left}:{Crop.Top}");
            }

            if (Resize != null)
            {
                filters.Add($"scale={Resize.TargetWidth}:{Resize.TargetHeight}:flags=lanczos");
            }

            if (Deinterlace != null)
            {
                filters.Add("yadif");
            }

            if (Denoise != null)
            {
                filters.Add("hqdn3d");
            }

            if (Rotate != null)
            {
                switch (Rotate.Mode)
                {
                    case RotateFilter.RotateMode.Right:
                        filters.Add("transpose=1");
                        break;
                    case RotateFilter.RotateMode.Twice:
                        filters.Add("transpose=1,transpose=1");
                        break;
                    case RotateFilter.RotateMode.Left:
                        filters.Add("transpose=2");
                        break;
                }
                if (Rotate.FlipHorizontal)
                    filters.Add("hflip");
                if (Rotate.FlipVertical)
                    filters.Add("vflip");
            }

            if (Reverse != null)
            {
                filters.Add("reverse");
            }

            if (Subtitle != null && !string.IsNullOrEmpty(Subtitle.FileName))
            {
                string path = Subtitle.FileName.Replace("\\", "/").Replace(":", "\\:").Replace("'", "\\'");
                filters.Add($"subtitles='{path}'");
            }

            return filters.Count > 0 ? string.Join(",", filters) : null;
        }

        internal static string GetFFmpegAudioFilter()
        {
            var filters = new List<string>();
            if (Reverse != null)
            {
                filters.Add("areverse");
            }
            return filters.Count > 0 ? string.Join(",", filters) : null;
        }
    }
    
    #endregion

    static class Program
    {
        public static FFMSSharp.VideoSource VideoSource;
        public static FFMSSharp.ColorRange VideoColorRange;
        public static bool VideoInterlaced;
        public static string InputFile;
        public static FileType InputType;
        public static bool InputHasAudio;
        public static bool InputHasWeirdPixelFormat; // for something that xy-VSFilter can't render on
        public static string FileMd5;
        public static string AttachmentDirectory;
        public static Dictionary<int, Tuple<string, SubtitleType, string>> SubtitleTracks; // stream id, <tag:title OR codec_name, textsub/vobsub, extension>
        public static List<string> AttachmentList;

        public static bool AviSynthAvailable { get; private set; }

        public static string AppDirectory => AppDomain.CurrentDomain.BaseDirectory;

        public static string GetBinariesPath(string folder = "Win32")
        {
            string path = Path.Combine(AppDirectory, "Binaries", folder);
            if (Directory.Exists(path))
                return path;
            return Path.Combine(Environment.CurrentDirectory, "Binaries", folder);
        }

        public static string GetBinaryPath(string filename, string folder = "Win32")
        {
            string path = Path.Combine(AppDirectory, "Binaries", folder, filename);
            if (File.Exists(path))
                return path;
            path = Path.Combine(AppDirectory, filename);
            if (File.Exists(path))
                return path;
            path = Path.Combine(Environment.CurrentDirectory, "Binaries", folder, filename);
            if (File.Exists(path))
                return path;
            return filename;
        }

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            // Check for AviSynth availability without preventing application startup
            try
            {
                AviSynthAvailable = NativeMethods.LoadLibrary("avisynth") != IntPtr.Zero;
            }
            catch
            {
                AviSynthAvailable = false;
            }

            // Upgrade Application Settings if applicable
            if (Properties.Settings.Default.TryUpgrade)
            {
                Properties.Settings.Default.Upgrade();
                Properties.Settings.Default.TryUpgrade = false;
                Properties.Settings.Default.Save();
            }

            try
            {
                ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;
            }
            catch
            {
                // Rely on system default
            }

            Task.Factory.StartNew(VideoDownload.CheckEnabled);
            Task.Factory.StartNew(ShareXUpload.CheckEnabled);

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }
    }
}
