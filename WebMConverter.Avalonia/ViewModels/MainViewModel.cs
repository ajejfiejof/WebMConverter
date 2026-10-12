using System;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace WebMConverter.Avalonia.ViewModels;

public enum EncodingMode
{
    Constant,
    Variable,
}

public enum VideoCodec
{
    Vp9,
    Vp8,
    Av1,
}

public partial class MainViewModel : ViewModelBase
{
    [ObservableProperty]
    private string _inputFile = string.Empty;

    [ObservableProperty]
    private string _outputFile = string.Empty;

    [ObservableProperty]
    private string _title = string.Empty;

    [ObservableProperty]
    private int _encodingModeIndex = 1; // Variable (CRF) by default, mirrors WinForms default.

    [ObservableProperty]
    private int _codecIndex; // VP9 by default (modern WebM standard).

    [ObservableProperty]
    private double _videoBitrate = 1000;

    [ObservableProperty]
    private double _crf = 30;

    [ObservableProperty]
    private double _crfTolerance = 2;

    [ObservableProperty]
    private bool _audioEnabled = true;

    [ObservableProperty]
    private double _audioBitrate = 128;

    [ObservableProperty]
    private bool _useHqMode;

    [ObservableProperty]
    private double _threads;

    [ObservableProperty]
    private double _slices = 4;

    [ObservableProperty]
    private string _extraArguments = string.Empty;

    [ObservableProperty]
    private double _progressValue;

    [ObservableProperty]
    private string _statusMessage = "Ready";

    [ObservableProperty]
    private bool _isBusy;

    public EncodingMode EncodingMode => EncodingModeIndex == 0 ? EncodingMode.Constant : EncodingMode.Variable;

    public VideoCodec Codec => CodecIndex switch
    {
        1 => VideoCodec.Vp8,
        2 => VideoCodec.Av1,
        _ => VideoCodec.Vp9,
    };

    public ObservableCollection<string> ActiveFilters { get; } = new();

    public MainViewModel()
    {
        _threads = Math.Max(1, Environment.ProcessorCount);
    }

    private bool CanStart => !IsBusy
        && !string.IsNullOrWhiteSpace(InputFile)
        && !string.IsNullOrWhiteSpace(OutputFile);

    partial void OnInputFileChanged(string value) => ConvertCommand.NotifyCanExecuteChanged();

    partial void OnOutputFileChanged(string value) => ConvertCommand.NotifyCanExecuteChanged();

    partial void OnIsBusyChanged(bool value)
    {
        ConvertCommand.NotifyCanExecuteChanged();
        PreviewCommand.NotifyCanExecuteChanged();
    }

    [RelayCommand(CanExecute = nameof(CanStart))]
    private async Task ConvertAsync(CancellationToken token)
    {
        // PLACEHOLDER: the conversion engine (FFmpeg argument builder, FFMSSharp
        // probing, filter script generation) ports here in the next commit.
        // This dry run only proves progress/status binding works.
        IsBusy = true;
        try
        {
            for (var i = 0; i <= 100; i += 10)
            {
                token.ThrowIfCancellationRequested();
                ProgressValue = i;
                StatusMessage = $"Dry run - engine port pending ({i}%)";
                await Task.Delay(120, token).ConfigureAwait(false);
            }

            StatusMessage = "Dry run complete. Engine wiring lands with the port.";
        }
        catch (OperationCanceledException)
        {
            StatusMessage = "Cancelled.";
        }
        finally
        {
            IsBusy = false;
            ProgressValue = 0;
        }
    }

    [RelayCommand(CanExecute = nameof(CanStart))]
    private void Preview()
    {
        // PLACEHOLDER: preview (ffplay) wiring lands with the engine port.
        StatusMessage = "Preview lands with the engine port.";
    }

    [RelayCommand]
    private void CancelConvert() => ConvertCommand.Cancel();

    [RelayCommand]
    private void ClearFilters() => ActiveFilters.Clear();
}
