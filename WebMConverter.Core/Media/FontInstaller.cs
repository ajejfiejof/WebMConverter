using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;

namespace WebMConverter.Core.Media;

/// <summary>
/// Makes extracted attachment fonts visible to subtitle renderers.
/// Windows: session registration via AddFontResourceEx (legacy parity).
/// Linux/macOS: copies into a managed user font dir + fc-cache, removed on
/// dispose (replaces the session-only Windows behavior with the fontconfig
/// equivalent).
/// </summary>
public static class FontInstaller
{
    /// <summary>Overrideable for tests. Default: ~/.local/share/fonts/WebMConverter (Linux/macOS).</summary>
    public static string FontsDirectory { get; set; } = DefaultFontsDirectory();

    public static string DefaultFontsDirectory()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return Path.Combine(Path.GetTempPath(), "WebMConverter-fonts");
        return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            ".local", "share", "fonts", "WebMConverter");
    }

    public static InstalledFonts Install(IEnumerable<string> fontFiles, string sourceDir, Action<string>? log = null)
    {
        var installed = new InstalledFonts();

        foreach (string name in fontFiles)
        {
            string source = Path.Combine(sourceDir, name);
            if (!File.Exists(source))
                continue;

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                log?.Invoke($"Loading font {name}");
                NativeMethods.AddFontResourceEx(source, 0, IntPtr.Zero);
                installed.WindowsFiles.Add(source);
                continue;
            }

            try
            {
                Directory.CreateDirectory(FontsDirectory);
                string dest = Path.Combine(FontsDirectory, name);
                File.Copy(source, dest, overwrite: true);
                installed.CopiedFiles.Add(dest);
                log?.Invoke($"Installed font {name}");
            }
            catch (Exception ex)
            {
                log?.Invoke($"Font install failed for {name}: {ex.Message}");
            }
        }

        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows) && installed.CopiedFiles.Count > 0)
            RefreshFontCache(log);

        return installed;
    }

    public static void RefreshFontCache(Action<string>? log = null)
    {
        try
        {
            string? fcCache = BinaryResolver.GetBinaryPath("fc-cache");
            bool runnable = File.Exists(fcCache) || !Path.IsPathRooted(fcCache);
            if (!runnable)
            {
                log?.Invoke("fc-cache not found, fonts may not register until next login.");
                return;
            }

            using var proc = new Process();
            proc.StartInfo.FileName = fcCache;
            proc.StartInfo.Arguments = $"-f \"{FontsDirectory}\"";
            proc.StartInfo.UseShellExecute = false;
            proc.StartInfo.CreateNoWindow = true;
            proc.Start();
            proc.WaitForExit(30000);
        }
        catch (Exception ex)
        {
            log?.Invoke($"fc-cache failed: {ex.Message}");
        }
    }

    private static class NativeMethods
    {
        [DllImport("gdi32", CharSet = CharSet.Auto, SetLastError = true)]
        internal static extern int AddFontResourceEx(string filename, uint flags, IntPtr reserved);

        [DllImport("gdi32", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool RemoveFontResourceEx(string filename, uint flags, IntPtr reserved);
    }

    public sealed class InstalledFonts : IDisposable
    {
        internal List<string> WindowsFiles { get; } = new();
        internal List<string> CopiedFiles { get; } = new();
        private bool _disposed;

        public void Dispose()
        {
            if (_disposed)
                return;
            _disposed = true;

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                foreach (string file in WindowsFiles)
                {
                    try
                    {
                        NativeMethods.RemoveFontResourceEx(file, 0, IntPtr.Zero);
                    }
                    catch (Exception)
                    {
                        // Best effort.
                    }
                }

                return;
            }

            foreach (string file in CopiedFiles)
            {
                try
                {
                    File.Delete(file);
                }
                catch (Exception)
                {
                    // Best effort.
                }
            }

            if (CopiedFiles.Count > 0)
                RefreshFontCache();
        }
    }
}
