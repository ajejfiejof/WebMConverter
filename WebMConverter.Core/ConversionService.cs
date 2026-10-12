using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using WebMConverter.Core.Processes;

namespace WebMConverter.Core;

/// <summary>
/// Runs ffmpeg conversions (single or HQ two-pass) with progress reporting.
/// Port of Dialogs.ConverterDialog minus all WinForms/taskbar code.
/// The AviSynth pipe path is Windows-only; .avs input elsewhere throws.
/// </summary>
public sealed class ConversionService
{
    private static readonly Regex TimeRegex = new(@"time=([^ ]+)", RegexOptions.Compiled);

    public async Task<ConvertResult> ConvertAsync(
        string input,
        string output,
        string options,
        bool hqMode,
        IProgress<double>? progress = null,
        Action<string>? log = null,
        CancellationToken cancellationToken = default)
    {
        if (input == output)
            throw new Exception("Input and output files are the same!");
        FfmpegArgumentBuilder.ValidateInputFile(input);
        FfmpegArgumentBuilder.ValidateOutputFile(output);

        bool isAvs = input.EndsWith(".avs", StringComparison.OrdinalIgnoreCase);
        if (isAvs && !RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            throw new NotSupportedException("AviSynth scripts require Windows (AviSynth + 32-bit proxy). Convert the script to video first.");

        // .vpy scripts are frame-served through vspipe on every OS (ffmpeg
        // VapourSynth demuxers are rarely compiled in), mirroring the legacy
        // AviSynth proxy architecture.
        bool isVpy = input.EndsWith(".vpy", StringComparison.OrdinalIgnoreCase);

        double inDuration = -1;
        try
        {
            inDuration = ProbeService.ProbeDuration(input);
        }
        catch (ProbeException ex)
        {
            log?.Invoke("Duration probe failed, progress will be indeterminate: " + ex.Message);
        }

        string? passLogBase = null;
        if (hqMode)
            passLogBase = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));

        string[] passArguments = FfmpegArgumentBuilder.BuildPassArguments(output, options, hqMode, passLogBase ?? "");
        var fullLog = new System.Text.StringBuilder();
        void LogLine(string? line)
        {
            if (line is null)
                return;
            fullLog.AppendLine(line);
            log?.Invoke(line);
        }

        int exitCode = 0;
        try
        {
            for (int pass = 0; pass < passArguments.Length; pass++)
            {
                bool lastPass = pass == passArguments.Length - 1;
                string argument = WithInput(input, isAvs, isVpy, passArguments[pass]);
                LogLine(passArguments.Length > 1 ? $"Arguments for pass {pass + 1}: {argument}" : $"Arguments: {argument}");

                using var ffmpeg = new FfmpegProcess(argument);
                VspipeProcess? vspipe = null;
                try
                {
                    ffmpeg.ErrorDataReceived += (_, e) =>
                    {
                        LogLine(e.Data);
                        if (e.Data is not null && lastPass && inDuration > 0 && e.Data.StartsWith("frame="))
                            ReportProgress(e.Data, inDuration, progress);
                    };
                    ffmpeg.OutputDataReceived += (_, e) => LogLine(e.Data);
                    ffmpeg.Start();
                    if (isVpy)
                        vspipe = StartVspipePipe(input, ffmpeg, LogLine);

                    try
                    {
                        await ffmpeg.WaitForExitAsync(cancellationToken).ConfigureAwait(false);
                    }
                    catch (OperationCanceledException)
                    {
                        ProcessUtils.KillProcessTree(ffmpeg.Id);
                        if (vspipe is not null)
                            ProcessUtils.KillProcessTree(vspipe.Id);
                        throw;
                    }

                    exitCode = ffmpeg.ExitCode;
                }
                finally
                {
                    vspipe?.Dispose();
                }

                LogLine("--- FFMPEG HAS EXITED ---");
                if (exitCode != 0)
                    break;
            }
        }
        catch (OperationCanceledException)
        {
            return new ConvertResult { Cancelled = true, ExitCode = exitCode, OutputFile = output, Log = fullLog.ToString() };
        }
        finally
        {
            if (passLogBase is not null)
            {
                try
                {
                    File.Delete(passLogBase + "-0.log");
                }
                catch (Exception)
                {
                    // Best effort temp cleanup.
                }
            }
        }

        var result = new ConvertResult
        {
            Succeeded = exitCode == 0,
            ExitCode = exitCode,
            OutputFile = output,
            Log = fullLog.ToString(),
        };

        if (exitCode == -1073741819) // STATUS_ACCESS_VIOLATION; usually libvpx thread crash
            result.Note = "ffmpeg crashed, likely a thread error. Set threads to 1 in the Advanced tab and try again.";

        if (exitCode == 0 && inDuration > 0)
        {
            try
            {
                double outDuration = ProbeService.ProbeDuration(output);
                double lost = Math.Abs(inDuration - outDuration);
                if (lost > 0.01)
                    result.DurationMismatchSeconds = lost;
            }
            catch (ProbeException)
            {
                // Output missing/unprobable; leave mismatch unset.
            }
        }

        return result;
    }

    private static string WithInput(string input, bool isAvs, bool isVpy, string argument)
    {
        if (isVpy)
            return $"-f yuv4mpegpipe -i pipe:0 {argument}";
        if (isAvs)
            return $@"-f avisynth -i ""{input}"" {argument}";
        return $@"-i ""{input}"" {argument}";
    }

    /// <summary>
    /// Serves a .vpy script through vspipe into ffmpeg's stdin.
    /// Throws InvalidOperationException when vspipe is not installed.
    /// </summary>
    private static VspipeProcess StartVspipePipe(string script, FfmpegProcess ffmpeg, Action<string?> log)
    {
        var vspipe = new VspipeProcess($@"--y4m ""{script}"" -");
        vspipe.ErrorDataReceived += (_, e) => log(e.Data);
        try
        {
            vspipe.Start();
        }
        catch (Exception ex)
        {
            vspipe.Dispose();
            throw new InvalidOperationException(
                "vspipe was not found. Install VapourSynth to convert .vpy scripts.", ex);
        }

        Task.Run(async () =>
        {
            try
            {
                await vspipe.StandardOutput.BaseStream.CopyToAsync(ffmpeg.StandardInput.BaseStream).ConfigureAwait(false);
            }
            catch (Exception)
            {
                // Consumer went away; ignore.
            }
        });
        vspipe.Exited += (_, _) =>
        {
            try
            {
                ffmpeg.StandardInput.Close();
            }
            catch (Exception)
            {
                // Already closed; ignore.
            }
        };
        return vspipe;
    }

    internal static void ReportProgress(string line, double totalSeconds, IProgress<double>? progress)
    {
        var match = TimeRegex.Match(line);
        if (!match.Success)
            return;
        if (!TimeSpan.TryParse(match.Groups[1].Value, out TimeSpan time))
            return;
        progress?.Report(Math.Min(time.TotalSeconds / totalSeconds, 1));
    }
}
