using System;
using System.Diagnostics;

namespace WebMConverter.Core;

public static class ProcessUtils
{
    /// <summary>Cross-platform process-tree kill (replaces WMI-based killing).</summary>
    public static void KillProcessTree(int pid)
    {
        try
        {
            var proc = Process.GetProcessById(pid);
            if (!proc.HasExited)
                proc.Kill(entireProcessTree: true);
        }
        catch (Exception)
        {
            // Process already exited or access denied.
        }
    }
}
