using System.Runtime.InteropServices;
using System.Text;

namespace WebMConverter.Core;

/// <summary>
/// AviSynth and legacy Windows plugins can't deal with UTF-8 paths, so on
/// Windows paths are converted to 8.3 notation when available. Everywhere else
/// this is a passthrough.
/// </summary>
public static class PathCompat
{
    public static string GetCompatiblePath(string input)
    {
        if (string.IsNullOrEmpty(input) || !RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return input;

        var compatible = new StringBuilder(1024);
        int length = NativeMethods.GetShortPathName(@"\\?\" + input, compatible, compatible.Capacity);
        if (length > 0 && compatible.Length > 0)
        {
            var result = compatible.ToString();
            if (result.StartsWith(@"\\?\"))
                result = result.Substring(4);
            return result;
        }

        return input;
    }

    private static class NativeMethods
    {
        [DllImport("kernel32", CharSet = CharSet.Auto, SetLastError = true)]
        internal static extern int GetShortPathName(
            [MarshalAs(UnmanagedType.LPTStr)] string path,
            [MarshalAs(UnmanagedType.LPTStr)] StringBuilder shortPath,
            int shortPathLength);
    }
}
