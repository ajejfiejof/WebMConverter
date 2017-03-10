using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Management;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml.XPath;

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
    }
    
    #endregion

    static class NativeMethods
    {
        [DllImport("kernel32", SetLastError = true)]
        public static extern IntPtr LoadLibrary(string lpFileName);
        [DllImport("kernel32", CharSet = CharSet.Auto)]
        public static extern int GetShortPathName([MarshalAs(UnmanagedType.LPTStr)] string path, [MarshalAs(UnmanagedType.LPTStr)] StringBuilder shortPath, int shortPathLength);
        [DllImport("gdi32", CharSet = CharSet.Auto)]
        public static extern int AddFontResourceEx(string lpszFilename, uint fl, IntPtr pdv);
        [DllImport("gdi32", CharSet = CharSet.Auto)]
        public static extern bool RemoveFontResourceEx(string lpFileName, uint fl, IntPtr pdv);
        [DllImport("user32.dll")]
        public static extern IntPtr SendMessage(IntPtr window, int message, int wparam, int lparam);
    }

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

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            // Check for AviSynth
            if (NativeMethods.LoadLibrary("avisynth") == IntPtr.Zero)
            {
                var errorMessage = new Win32Exception(Marshal.GetLastWin32Error()).Message;
                MessageBox.Show(
                   $"Failed to load AviSynth: {errorMessage}.{Environment.NewLine}" + 
                    "I'll open the download page, go ahead and install it.",
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Process.Start("http://avisynth.nl/index.php/Main_Page#Official_builds");
                return;
            }

            // Upgrade Application Settings if applicable
            if (Properties.Settings.Default.TryUpgrade)
            {
                Properties.Settings.Default.Upgrade();
                Properties.Settings.Default.TryUpgrade = false;
                Properties.Settings.Default.Save();
            }

            Task.Factory.StartNew(VideoDownload.CheckIfDownloadIsEnabled);

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }
    }
}
