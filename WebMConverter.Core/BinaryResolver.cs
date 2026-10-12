using System;
using System.IO;
using System.Runtime.InteropServices;

namespace WebMConverter.Core;

/// <summary>
/// Resolves helper binary paths. Mirrors the legacy Program.GetBinariesPath /
/// GetBinaryPath semantics, with two fixes: the default folder is auto-detected
/// for the current platform (the old "Win32" default no longer exists), and
/// bare names fall back to a PATH lookup so system ffmpeg works on Linux.
/// </summary>
public static class BinaryResolver
{
    public static string AppDirectory { get; set; } = AppContext.BaseDirectory;

    public static string GetBinariesPath(string? folder = null)
    {
        folder ??= DefaultBinariesFolder();

        string path = Path.Combine(AppDirectory, "Binaries", folder);
        if (Directory.Exists(path))
            return path;
        return Path.Combine(Environment.CurrentDirectory, "Binaries", folder);
    }

    public static string DefaultBinariesFolder()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return Environment.Is64BitProcess ? "x64" : "x86";
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            return Environment.Is64BitProcess ? "linux-x64" : "linux-x86";
        return Environment.Is64BitProcess ? "x64" : "x86";
    }

    /// <summary>
    /// Resolves a helper binary. Search order per candidate name: app Binaries
    /// folder, app directory, working-directory Binaries folder, then PATH.
    /// Returns the first hit, else the preferred bare name (PATH resolution at
    /// process start).
    /// </summary>
    public static string GetBinaryPath(string filename, string? folder = null)
    {
        folder ??= DefaultBinariesFolder();

        foreach (var name in CandidateNames(filename))
        {
            string path = Path.Combine(AppDirectory, "Binaries", folder, name);
            if (File.Exists(path))
                return path;

            path = Path.Combine(AppDirectory, name);
            if (File.Exists(path))
                return path;

            path = Path.Combine(Environment.CurrentDirectory, "Binaries", folder, name);
            if (File.Exists(path))
                return path;

            string? onPath = FindOnPath(name);
            if (onPath is not null)
                return onPath;
        }

        return CandidateNames(filename)[0];
    }

    private static string[] CandidateNames(string filename)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            if (filename.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
                return [filename];
            return [filename + ".exe", filename];
        }

        if (filename.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
            return [filename.Substring(0, filename.Length - 4), filename];
        return [filename];
    }

    private static string? FindOnPath(string name)
    {
        if (name.IndexOfAny([Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar]) >= 0)
            return File.Exists(name) ? Path.GetFullPath(name) : null;

        string? pathEnv = Environment.GetEnvironmentVariable("PATH");
        if (string.IsNullOrEmpty(pathEnv))
            return null;

        foreach (var dir in pathEnv.Split(Path.PathSeparator))
        {
            try
            {
                string candidate = Path.Combine(dir.Trim(), name);
                if (File.Exists(candidate))
                    return candidate;
            }
            catch (Exception)
            {
                // Malformed PATH entries must not break resolution.
            }
        }

        return null;
    }
}
