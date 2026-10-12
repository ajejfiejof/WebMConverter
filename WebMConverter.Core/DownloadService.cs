using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using WebMConverter.Core.Processes;

namespace WebMConverter.Core;

/// <summary>
/// Two-phase yt-dlp download: resolve filename, then download.
/// Port of Dialogs.DownloadDialog minus all WinForms/taskbar code.
/// </summary>
public sealed class DownloadService
{
    private static readonly Regex PercentRegex = new(@"([0-9.]+)%", RegexOptions.Compiled);

    public async Task<DownloadResult> DownloadAsync(
        string url,
        IProgress<double>? progress = null,
        Action<string>? log = null,
        CancellationToken cancellationToken = default)
    {
        if (!VideoDownload.Enabled)
            throw new InvalidOperationException("No yt-dlp / youtube-dl executable was found.");

        string quoted = '"' + url.Replace(@"""", @"\""") + '"';
        var fullLog = new System.Text.StringBuilder();
        void LogLine(string? line)
        {
            if (line is null)
                return;
            fullLog.AppendLine(line);
            log?.Invoke(line);
        }

        // Phase 1: resolve the output filename.
        string? resolvedName = null;
        using (var getter = new YoutubeDlProcess("--get-filename " + quoted))
        {
            getter.OutputDataReceived += (_, e) =>
            {
                LogLine(e.Data);
                if (resolvedName is null && !string.IsNullOrWhiteSpace(e.Data))
                    resolvedName = e.Data.Trim();
            };
            getter.ErrorDataReceived += (_, e) => LogLine(e.Data);
            getter.Start();
            try
            {
                await getter.WaitForExitAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                ProcessUtils.KillProcessTree(getter.Id);
                return new DownloadResult { Cancelled = true, Log = fullLog.ToString() };
            }

            if (getter.ExitCode != 0 || string.IsNullOrWhiteSpace(resolvedName))
                return new DownloadResult { ExitCode = getter.ExitCode, Log = fullLog.ToString() };
        }

        string outfile = Path.Combine(Path.GetTempPath(), resolvedName);
        LogLine("Downloading to " + outfile);

        // Phase 2: download.
        using (var downloader = new YoutubeDlProcess($@"-o ""{outfile}"" {quoted}"))
        {
            downloader.OutputDataReceived += (_, e) =>
            {
                LogLine(e.Data);
                if (e.Data is not null && e.Data.StartsWith("[download]"))
                    ReportProgress(e.Data, progress);
            };
            downloader.ErrorDataReceived += (_, e) => LogLine(e.Data);
            downloader.Start();
            try
            {
                await downloader.WaitForExitAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                ProcessUtils.KillProcessTree(downloader.Id);
                return new DownloadResult { Cancelled = true, OutputFile = outfile, Log = fullLog.ToString() };
            }

            if (downloader.ExitCode != 0)
                return new DownloadResult { ExitCode = downloader.ExitCode, OutputFile = outfile, Log = fullLog.ToString() };
        }

        // Workaround for https://github.com/rg3/youtube-dl/issues/11472
        if (!File.Exists(outfile))
            outfile = Path.ChangeExtension(outfile, "mkv");

        return new DownloadResult { Succeeded = true, OutputFile = outfile, Log = fullLog.ToString() };
    }

    internal static void ReportProgress(string line, IProgress<double>? progress)
    {
        var match = PercentRegex.Match(line);
        if (!match.Success)
            return;
        if (double.TryParse(match.Groups[1].Value, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out double percent))
            progress?.Report(percent / 100);
    }
}
