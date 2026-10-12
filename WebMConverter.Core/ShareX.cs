using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace WebMConverter.Core;

/// <summary>
/// ShareX upload integration. Windows-only (registry lookup); everywhere else
/// <see cref="Enabled"/> is false.
/// </summary>
public static class ShareXClient
{
    public static bool Enabled { get; private set; }
    public static string? InstallPath { get; private set; }

    public static void CheckEnabled()
    {
        Enabled = false;
        InstallPath = null;

        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return;

        try
        {
            using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"Software\Classes\*\shell\ShareX\command");
            string? value = key?.GetValue(null) as string;
            if (value is null)
                return;

            // Value looks like: "C:\...\ShareX.exe" "..." — take the first quoted part.
            int first = value.IndexOf('"');
            int second = value.IndexOf('"', first + 1);
            if (first < 0 || second <= first)
                return;

            InstallPath = value.Substring(first + 1, second - first - 1);
            Enabled = true;
        }
        catch (Exception)
        {
            Enabled = false;
        }
    }
}

public sealed class ShareXProcess : Process
{
    public ShareXProcess(string filename)
    {
        if (!ShareXClient.Enabled || ShareXClient.InstallPath is null)
            throw new InvalidOperationException("ShareX is not available on this system.");

        StartInfo.FileName = ShareXClient.InstallPath;
        StartInfo.Arguments = $"\"{filename}\"";
        StartInfo.UseShellExecute = false;
        StartInfo.CreateNoWindow = true;
        EnableRaisingEvents = true;
    }

    public new void Start()
    {
        base.Start();
    }
}
