using System;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using WebMConverter.Core;

namespace WebMConverter.Avalonia.ViewModels;

public partial class DownloadViewModel : ViewModelBase
{
    private CancellationTokenSource? _cts;
    private string? _downloadedFile;

    [ObservableProperty]
    private string _log = string.Empty;

    [ObservableProperty]
    private double _progressPercent;

    [ObservableProperty]
    private bool _progressIndeterminate = true;

    [ObservableProperty]
    private string _status = "Resolving filename…";

    [ObservableProperty]
    private bool _isRunning = true;

    [ObservableProperty]
    private bool _succeeded;

    public Action? CloseAction { get; set; }
    public Action? LoadAction { get; set; }
    public string? DownloadedFile { get; private set; }

    private void AppendLog(string line)
    {
        string updated = Log.Length == 0 ? line : Log + Environment.NewLine + line;
        if (updated.Length > 1_000_000)
            updated = updated.Substring(updated.Length / 2);
        Log = updated;
    }

    public async Task RunDownloadAsync(string url)
    {
        _cts = new CancellationTokenSource();

        var progress = new Progress<double>(v => Dispatcher.UIThread.Post(() =>
        {
            ProgressIndeterminate = false;
            ProgressPercent = v * 100;
        }));
        void LogLine(string line) => Dispatcher.UIThread.Post(() => AppendLog(line));

        var service = new DownloadService();
        DownloadResult result = await service
            .DownloadAsync(url, progress, LogLine, _cts.Token)
            .ConfigureAwait(false);

        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            IsRunning = false;
            if (result.Cancelled)
            {
                Status = "Download cancelled.";
            }
            else if (result.Succeeded)
            {
                Succeeded = true;
                _downloadedFile = result.OutputFile;
                Status = "Video downloaded successfully!";
            }
            else
            {
                Status = $"Download failed (exit code {result.ExitCode}).";
            }
        });

        DownloadedFile = result.Succeeded ? result.OutputFile : null;
    }

    public void Cancel() => _cts?.Cancel();

    [RelayCommand]
    private void CancelRun() => Cancel();

    [RelayCommand]
    private void CloseWindow() => CloseAction?.Invoke();

    [RelayCommand]
    private void LoadFile() => LoadAction?.Invoke();
}
