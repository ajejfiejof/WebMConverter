using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using WebMConverter.Core;

namespace WebMConverter.Avalonia.ViewModels;

public partial class ConvertProgressViewModel : ViewModelBase
{
    private CancellationTokenSource? _cts;
    private string _outputFile = string.Empty;
    private ConvertResult? _result;

    [ObservableProperty]
    private string _log = string.Empty;

    [ObservableProperty]
    private double _progressPercent;

    [ObservableProperty]
    private string _status = "Starting…";

    [ObservableProperty]
    private bool _isRunning = true;

    [ObservableProperty]
    private bool _showUpload;

    [ObservableProperty]
    private bool _succeeded;

    public Action? CloseAction { get; set; }

    private void AppendLog(string line)
    {
        string updated = Log.Length == 0 ? line : Log + Environment.NewLine + line;
        // Cap memory: drop the oldest half when huge.
        if (updated.Length > 1_000_000)
            updated = updated.Substring(updated.Length / 2);
        Log = updated;
    }

    public async Task<ConvertResult> RunConversionAsync(string input, string output, string options, bool hqMode)
    {
        _cts = new CancellationTokenSource();
        _outputFile = output;

        var progress = new Progress<double>(v =>
            Dispatcher.UIThread.Post(() => ProgressPercent = v * 100));
        void LogLine(string line) => Dispatcher.UIThread.Post(() => AppendLog(line));

        var service = new ConversionService();
        ConvertResult result = await service
            .ConvertAsync(input, output, options, hqMode, progress, LogLine, _cts.Token)
            .ConfigureAwait(false);

        await Dispatcher.UIThread.InvokeAsync(() => Finish(result));
        return result;
    }

    private void Finish(ConvertResult result)
    {
        _result = result;
        IsRunning = false;
        Succeeded = result.Succeeded;

        if (result.Cancelled)
        {
            Status = "Conversion cancelled.";
            return;
        }

        if (!result.Succeeded)
        {
            Status = $"ffmpeg exited with code {result.ExitCode}.";
            return;
        }

        if (result.DurationMismatchSeconds > 0)
            Status = $"Converted, but {result.DurationMismatchSeconds:0.##}s lost to size constraints.";
        else if (!string.IsNullOrWhiteSpace(result.Note))
            Status = "Converted with warnings — see log.";
        else
            Status = "Video converted successfully!";
    }

    public void Cancel() => _cts?.Cancel();

    [RelayCommand]
    private void CancelRun() => Cancel();

    [RelayCommand]
    private void CloseWindow() => CloseAction?.Invoke();

    [RelayCommand]
    private void OpenFile()
    {
        try
        {
            Process.Start(new ProcessStartInfo(_outputFile) { UseShellExecute = true });
        }
        catch (Exception)
        {
            // Best effort.
        }
    }

    [RelayCommand]
    private void OpenFolder()
    {
        try
        {
            string? dir = Path.GetDirectoryName(_outputFile);
            if (dir is not null)
                Process.Start(new ProcessStartInfo(dir) { UseShellExecute = true });
        }
        catch (Exception)
        {
            // Best effort.
        }
    }

    [RelayCommand]
    private void Upload()
    {
        try
        {
            if (_result?.Succeeded == true && File.Exists(_outputFile))
                using (var proc = new ShareXProcess(_outputFile))
                    proc.Start();
        }
        catch (Exception ex)
        {
            AppendLog("Upload failed: " + ex.Message);
        }
    }
}
