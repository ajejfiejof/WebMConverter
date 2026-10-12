using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using WebMConverter.Avalonia.Services;
using WebMConverter.Avalonia.Views;
using WebMConverter.Core;
using WebMConverter.Core.Filters;
using WebMConverter.Core.Media;
using WebMConverter.Core.VapourSynth;

namespace WebMConverter.Avalonia.ViewModels;

public partial class TrimRangeEntry : ViewModelBase
{
    [ObservableProperty]
    private double _startSeconds;

    [ObservableProperty]
    private double _endSeconds;
}

public partial class EmbeddedSubtitleEntry : ViewModelBase
{
    public string Name { get; set; } = string.Empty;
    public string File { get; set; } = string.Empty;
    public SubtitleType Type { get; set; }

    public override string ToString() => Name;
}

public partial class MainViewModel : ViewModelBase
{
    private readonly AppSettings _settings = SettingsStore.Load();
    private MediaInfo? _media;
    private CancellationTokenSource? _probeCts;
    private FontInstaller.InstalledFonts? _installedFonts;

    [ObservableProperty]
    private string _inputFile = string.Empty;

    [ObservableProperty]
    private string _outputFile = string.Empty;

    [ObservableProperty]
    private string _title = string.Empty;

    [ObservableProperty]
    private int _encodingModeIndex = 1; // Variable (CRF) by default.

    [ObservableProperty]
    private int _codecIndex; // VP9 by default.

    [ObservableProperty]
    private string _sizeLimitMb = string.Empty;

    [ObservableProperty]
    private string _videoBitrateK = string.Empty;

    [ObservableProperty]
    private string _audioBitrateK = string.Empty;

    [ObservableProperty]
    private double _crf = 30;

    [ObservableProperty]
    private double _crfTolerance = 2;

    [ObservableProperty]
    private double _audioQuality = 3;

    [ObservableProperty]
    private bool _audioEnabled = true;

    [ObservableProperty]
    private bool _useHqMode;

    [ObservableProperty]
    private double _threads = 4;

    [ObservableProperty]
    private double _slices = 4;

    [ObservableProperty]
    private string _frameRate = string.Empty;

    [ObservableProperty]
    private string _extraArguments = string.Empty;

    [ObservableProperty]
    private string _mediaSummary = string.Empty;

    [ObservableProperty]
    private string _statusMessage = Lang.Resources.Get("StatusReady");

    /// <summary>True while the status bar shows the default "Ready" text, so a
    /// language switch can re-render it.</summary>
    private bool _statusIsDefault = true;

    [ObservableProperty]
    private bool _isBusy;

    // ---- Processing filters ----

    [ObservableProperty]
    private bool _useTrim;

    [ObservableProperty]
    private double _trimStartSeconds;

    [ObservableProperty]
    private double _trimEndSeconds;

    [ObservableProperty]
    private bool _useCrop;

    [ObservableProperty]
    private double _cropLeft;

    [ObservableProperty]
    private double _cropTop;

    [ObservableProperty]
    private double _cropRight;

    [ObservableProperty]
    private double _cropBottom;

    [ObservableProperty]
    private bool _useResize;

    [ObservableProperty]
    private double _resizeWidth;

    [ObservableProperty]
    private double _resizeHeight;

    [ObservableProperty]
    private bool _useSubtitle;

    [ObservableProperty]
    private string _subtitleFile = string.Empty;

    [ObservableProperty]
    private int _subtitleTypeIndex;

    [ObservableProperty]
    private bool _useRate;

    [ObservableProperty]
    private double _rateMultiplier = 100;

    [ObservableProperty]
    private bool _useFade;

    [ObservableProperty]
    private bool _fadeStart = true;

    [ObservableProperty]
    private bool _fadeEnd = true;

    [ObservableProperty]
    private double _fadeFrames = 30;

    [ObservableProperty]
    private bool _fadeKeepAudio = true;

    [ObservableProperty]
    private bool _useRotate;

    [ObservableProperty]
    private int _rotateModeIndex;

    [ObservableProperty]
    private bool _rotateFlipH;

    [ObservableProperty]
    private bool _rotateFlipV;

    [ObservableProperty]
    private bool _useReverse;

    [ObservableProperty]
    private bool _useDeinterlace;

    [ObservableProperty]
    private bool _useDenoise;

    [ObservableProperty]
    private bool _useLevels;

    [ObservableProperty]
    private int _levelsIndex;

    [ObservableProperty]
    private bool _useOverlay;

    [ObservableProperty]
    private string _overlayFile = string.Empty;

    [ObservableProperty]
    private double _overlayX;

    [ObservableProperty]
    private double _overlayY;

    [ObservableProperty]
    private bool _useDub;

    [ObservableProperty]
    private string _dubFile = string.Empty;

    [ObservableProperty]
    private int _dubModeIndex;

    [ObservableProperty]
    private bool _useCaption;

    [ObservableProperty]
    private string _captionText = string.Empty;

    [ObservableProperty]
    private double _captionX = 20;

    [ObservableProperty]
    private double _captionY = 20;

    [ObservableProperty]
    private double _captionFontSize = 24;

    [ObservableProperty]
    private string _captionFont = "DejaVu Sans";

    [ObservableProperty]
    private string _captionFg = "#FFFFFF";

    [ObservableProperty]
    private string _captionBg = "#000000";

    [ObservableProperty]
    private double _captionBorder = 1;

    public ObservableCollection<TrimRangeEntry> MultiTrimRanges { get; } = new();
    public ObservableCollection<string> ActiveFilters { get; } = new();
    public ObservableCollection<EmbeddedSubtitleEntry> EmbeddedSubtitles { get; } = new();

    [ObservableProperty]
    private EmbeddedSubtitleEntry? _selectedEmbeddedSubtitle;

    partial void OnSelectedEmbeddedSubtitleChanged(EmbeddedSubtitleEntry? value)
    {
        if (value is not null)
        {
            SubtitleFile = value.File;
            SubtitleTypeIndex = (int)value.Type;
            UseSubtitle = true;
        }
    }

    public EncodingMode EncodingMode => EncodingModeIndex == 0 ? EncodingMode.Constant : EncodingMode.Variable;

    public VideoCodec Codec => CodecIndex switch
    {
        1 => VideoCodec.Vp8,
        2 => VideoCodec.Av1,
        _ => VideoCodec.Vp9,
    };

    public MainViewModel()
    {
        _threads = Math.Max(1, Environment.ProcessorCount);
        if (_settings.EncodingMode == nameof(Core.EncodingMode.Constant))
            _encodingModeIndex = 0;
        _audioEnabled = _settings.AudioEnabled;

        // Apply the persisted language (also refreshes bindings via the service).
        SelectedLanguage = _settings.Language;
    }

    public ObservableCollection<LanguageOption> Languages => LocalizationService.Instance.AvailableLanguages;

    [ObservableProperty]
    private string _selectedLanguage = "en";

    partial void OnSelectedLanguageChanged(string value)
    {
        LocalizationService.Instance.CurrentCulture = value;
        if (_statusIsDefault)
            StatusMessage = Lang.Resources.Get("StatusReady");
        _settings.Language = value;
        SettingsStore.Save(_settings);
    }

    [RelayCommand]
    private void SetLanguage(string? code)
    {
        if (!string.IsNullOrEmpty(code))
            SelectedLanguage = code;
    }

    private bool CanStart => !IsBusy
        && !string.IsNullOrWhiteSpace(InputFile)
        && !string.IsNullOrWhiteSpace(OutputFile);

    partial void OnInputFileChanged(string value)
    {
        ConvertCommand.NotifyCanExecuteChanged();
        PreviewCommand.NotifyCanExecuteChanged();
        _ = ProbeInputAsync(value);
    }

    partial void OnOutputFileChanged(string value)
    {
        ConvertCommand.NotifyCanExecuteChanged();
        PreviewCommand.NotifyCanExecuteChanged();
    }

    partial void OnIsBusyChanged(bool value)
    {
        ConvertCommand.NotifyCanExecuteChanged();
        PreviewCommand.NotifyCanExecuteChanged();
    }

    private async Task ProbeInputAsync(string path)
    {
        _probeCts?.Cancel();
        var cts = _probeCts = new CancellationTokenSource();

        _media = null;
        MediaSummary = string.Empty;

        if (string.IsNullOrWhiteSpace(path) || path.StartsWith("http", StringComparison.OrdinalIgnoreCase) || !File.Exists(path))
            return;

        StatusMessage = Lang.Resources.Get("StatusProbing");
        try
        {
            MediaInfo info = await Task.Run(() => ProbeService.ProbeVideoInfo(path), cts.Token).ConfigureAwait(false);
            if (cts.IsCancellationRequested)
                return;

            // Legacy indexing parity: extract attachments + install fonts.
            AttachmentResult attachments = await Task.Run(() =>
                MediaAttachmentService.ExtractAttachments(
                    path, MediaAttachmentService.AttachmentDirectoryFor(path),
                    log: m => System.Diagnostics.Debug.WriteLine(m)), cts.Token).ConfigureAwait(false);
            if (cts.IsCancellationRequested)
                return;

            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                _media = info;
                if (!info.HasAudio)
                    AudioEnabled = false;
                Slices = FfmpegArgumentBuilder.SlicesForResolution(info.Width, info.Height);
                if (string.IsNullOrWhiteSpace(OutputFile))
                    OutputFile = DefaultOutputFor(path);
                if (string.IsNullOrWhiteSpace(Title))
                    Title = info.ContainerTitle ?? Path.GetFileNameWithoutExtension(path);
                MediaSummary = $"{info.Width}×{info.Height} @ {info.Fps:0.##}fps, {info.DurationSeconds:0.#}s, audio: {(info.HasAudio ? "yes" : "no")}";

                var notes = new System.Collections.Generic.List<string>();
                if (info.IsFrapsYuvj420p)
                {
                    UseLevels = true;
                    LevelsIndex = 1;
                    notes.Add("FRAPS yuvj420p: levels auto-fixed");
                }

                if (info.SarCompensatedWidth is int compW && info.SarCompensatedHeight is int compH)
                {
                    UseResize = true;
                    ResizeWidth = compW;
                    ResizeHeight = compH;
                    notes.Add($"SAR compensated to {compW}×{compH}");
                }

                _installedFonts?.Dispose();
                _installedFonts = FontInstaller.Install(attachments.FontFiles, MediaAttachmentService.AttachmentDirectoryFor(path));
                if (attachments.FontFiles.Count > 0)
                    notes.Add($"{attachments.FontFiles.Count} font(s) installed");

                EmbeddedSubtitles.Clear();
                foreach (var sub in attachments.Subtitles)
                    EmbeddedSubtitles.Add(new EmbeddedSubtitleEntry { Name = sub.Name, File = sub.File, Type = sub.Type });
                if (attachments.Subtitles.Count > 0)
                    notes.Add($"{attachments.Subtitles.Count} embedded subtitle(s) found");

                SetStatus(notes.Count == 0 ? null : " (" + string.Join("; ", notes) + ")");
            });
        }
        catch (OperationCanceledException)
        {
            // Superseded by a newer probe.
        }
        catch (Exception ex)
        {
            await Dispatcher.UIThread.InvokeAsync(() => StatusMessage = string.Format(Lang.Resources.Get("StatusProbeFailed"), ex.Message));
        }
    }

    private string DefaultOutputFor(string inputPath)
    {
        string dir = !string.IsNullOrWhiteSpace(_settings.RememberedFolderOut) && Directory.Exists(_settings.RememberedFolderOut)
            ? _settings.RememberedFolderOut
            : Path.GetDirectoryName(inputPath) ?? "";
        return Path.Combine(dir, Path.GetFileNameWithoutExtension(inputPath) + ".webm");
    }

    private MediaInfo EnsureMedia()
    {
        if (_media is not null)
            return _media;
        if (!File.Exists(InputFile))
            throw new ArgumentException("Select an input file first (probe incomplete).");
        _media = ProbeService.ProbeVideoInfo(InputFile);
        return _media;
    }

    private EncodeSettings BuildEncodeSettings() => new()
    {
        Mode = EncodingMode,
        Codec = Codec,
        AudioEnabled = AudioEnabled,
        SizeLimitMb = string.IsNullOrWhiteSpace(SizeLimitMb) ? null : SizeLimitMb,
        VideoBitrateK = string.IsNullOrWhiteSpace(VideoBitrateK) ? null : VideoBitrateK,
        AudioBitrateK = string.IsNullOrWhiteSpace(AudioBitrateK) ? null : AudioBitrateK,
        Crf = Crf,
        CrfTolerance = CrfTolerance,
        AudioQuality = AudioQuality,
        Threads = Math.Max(1, (int)Threads),
        Slices = Math.Max(1, (int)Slices),
        Title = Title,
        HqMode = UseHqMode,
        FrameRate = string.IsNullOrWhiteSpace(FrameRate) ? null : FrameRate,
        ExtraArguments = string.IsNullOrWhiteSpace(ExtraArguments) ? null : ExtraArguments,
    };

    private FilterSet BuildFilterSet(MediaInfo media)
    {
        var filters = new FilterSet();
        int ToFrames(double seconds) => Math.Max(0, (int)Math.Round(seconds * media.Fps));

        if (UseTrim)
        {
            if (TrimEndSeconds <= TrimStartSeconds)
                throw new ArgumentException("Trim end must be after trim start.");
            filters.Trim = new TrimFilter(ToFrames(TrimStartSeconds), ToFrames(TrimEndSeconds));
        }

        if (MultiTrimRanges.Count > 0)
        {
            if (UseTrim)
                throw new ArgumentException("Use either Trim or Multiple Trim, not both.");
            var trims = MultiTrimRanges
                .Select(r => new TrimFilter(ToFrames(r.StartSeconds), ToFrames(r.EndSeconds)))
                .ToList();
            if (trims.Any(t => t.TrimEnd <= t.TrimStart))
                throw new ArgumentException("Every trim range needs end after start.");
            filters.MultipleTrim = new MultipleTrimFilter(trims);
        }

        if (UseCrop)
            filters.Crop = new CropFilter((int)CropLeft, (int)CropTop, (int)CropRight, (int)CropBottom);
        if (UseResize)
        {
            if (ResizeWidth <= 0 || ResizeHeight <= 0)
                throw new ArgumentException("Resize needs positive width and height.");
            filters.Resize = new ResizeFilter((int)ResizeWidth, (int)ResizeHeight);
        }

        if (UseSubtitle)
        {
            if (string.IsNullOrWhiteSpace(SubtitleFile) || !File.Exists(SubtitleFile))
                throw new ArgumentException("Subtitle file doesn't exist.");
            filters.Subtitle = new SubtitleFilter(SubtitleFile, (SubtitleType)SubtitleTypeIndex);
        }

        if (UseRate)
        {
            if (RateMultiplier <= 0)
                throw new ArgumentException("Rate multiplier must be positive.");
            filters.Rate = new RateFilter((int)RateMultiplier);
        }

        if (UseFade)
        {
            if (FadeFrames <= 0)
                throw new ArgumentException("Fade needs a positive frame count.");
            filters.Fade = new FadeFilter(FadeStart, FadeEnd, (int)FadeFrames, FadeKeepAudio);
        }

        if (UseRotate && (RotateModeIndex != 0 || RotateFlipH || RotateFlipV))
            filters.Rotate = new RotateFilter((RotateMode)RotateModeIndex, RotateFlipH, RotateFlipV);
        if (UseReverse)
            filters.Reverse = new ReverseFilter();
        if (UseDeinterlace)
            filters.Deinterlace = new DeinterlaceFilter();
        if (UseDenoise)
            filters.Denoise = new DenoiseFilter();
        if (UseLevels)
            filters.Levels = new LevelsFilter((LevelsConversion)LevelsIndex);

        if (UseOverlay)
        {
            if (string.IsNullOrWhiteSpace(OverlayFile) || !File.Exists(OverlayFile))
                throw new ArgumentException("Overlay image doesn't exist.");
            filters.Overlay = new OverlayFilter(new FilterPoint((int)OverlayX, (int)OverlayY), OverlayFile);
        }

        if (UseDub)
        {
            if (string.IsNullOrWhiteSpace(DubFile) || !File.Exists(DubFile))
                throw new ArgumentException("Dub audio file doesn't exist.");
            string indexFile = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".ffindex");
            filters.Dub = new DubFilter(DubFile, indexFile, (DubMode)DubModeIndex);
        }

        if (UseCaption)
        {
            if (string.IsNullOrWhiteSpace(CaptionText))
                throw new ArgumentException("Caption text is empty.");
            filters.Caption = new CaptionFilter(
                new FilterPoint((int)CaptionX, (int)CaptionY),
                CaptionText,
                fontFamily: string.IsNullOrWhiteSpace(CaptionFont) ? "DejaVu Sans" : CaptionFont,
                fontSize: Math.Max(1, (int)CaptionFontSize),
                textColorArgb: CaptionFilter.ParseHexColor(CaptionFg),
                borderColorArgb: CaptionFilter.ParseHexColor(CaptionBg),
                borderThickness: Math.Max(0, (int)CaptionBorder));
        }

        return filters;
    }

    /// <summary>Sets a status message, or the localized default when null.</summary>
    private void SetStatus(string? suffix)
    {
        _statusIsDefault = suffix is null;
        StatusMessage = _statusIsDefault
            ? Lang.Resources.Get("StatusReady")
            : Lang.Resources.Get("StatusReady") + suffix;
    }

    public void RefreshActiveFilters()
    {
        ActiveFilters.Clear();
        void Add(bool on, string name)
        {
            if (on)
                ActiveFilters.Add(name);
        }

        Add(UseTrim, $"Trim ({TrimStartSeconds:0.###}s – {TrimEndSeconds:0.###}s)");
        if (MultiTrimRanges.Count > 0)
            ActiveFilters.Add($"Multiple Trim ({MultiTrimRanges.Count} ranges)");
        Add(UseCrop, "Crop");
        Add(UseResize, $"Resize ({ResizeWidth}×{ResizeHeight})");
        Add(UseSubtitle, "Subtitle");
        Add(UseRate, $"Rate ({RateMultiplier}%)");
        Add(UseFade, "Fade");
        Add(UseRotate, "Rotate");
        Add(UseReverse, "Reverse");
        Add(UseDeinterlace, "Deinterlace");
        Add(UseDenoise, "Denoise");
        Add(UseLevels, "Levels");
        Add(UseOverlay, "Overlay");
        Add(UseDub, "Dub");
        Add(UseCaption, "Caption");
    }

    [RelayCommand(CanExecute = nameof(CanStart))]
    private async Task ConvertAsync()
    {
        Window? owner = GetOwner();
        MediaInfo media;
        FilterSet filters;
        EncodeSettings settings = BuildEncodeSettings();
        string options;

        try
        {
            media = EnsureMedia();
            filters = BuildFilterSet(media);
            options = FfmpegArgumentBuilder.BuildOptions(settings, filters, media);
        }
        catch (Exception ex) when (ex is ArgumentException || ex is ProbeException || ex is NotSupportedException)
        {
            StatusMessage = string.Format(Lang.Resources.Get("StatusError"), ex.Message);
            if (owner is not null)
                await MessageDialog.ShowAsync(owner, "Error", ex.Message);
            return;
        }

        if (InputFile == OutputFile)
        {
            StatusMessage = "ERROR: Input and output files are the same!";
            if (owner is not null)
                await MessageDialog.ShowAsync(owner, "Error", "Input and output files are the same!");
            return;
        }

        _settings.EncodingMode = settings.Mode.ToString();
        _settings.AudioEnabled = settings.AudioEnabled;
        SettingsStore.Save(_settings);

        IsBusy = true;
        try
        {
            var dialog = new ConvertProgressView
            {
                DataContext = new ConvertProgressViewModel(),
            };
            var vm = (ConvertProgressViewModel)dialog.DataContext;
            vm.CloseAction = dialog.Close;
            vm.ShowUpload = ShareXClient.Enabled;

            if (owner is not null)
            {
#pragma warning disable CS4014 // dialog runs alongside; result handled via callback below
                dialog.Show(owner);
#pragma warning restore CS4014
            }
            else
            {
                dialog.Show();
            }

            ConvertResult result = await vm.RunConversionAsync(InputFile, OutputFile, options, settings.HqMode);
            StatusMessage = result.Cancelled
                ? "Cancelled."
                : result.Succeeded
                    ? string.IsNullOrWhiteSpace(result.Note) && result.DurationMismatchSeconds <= 0
                        ? "Video converted successfully!"
                        : "Converted with warnings — see log."
                    : "Conversion failed — see log.";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand(CanExecute = nameof(CanStart))]
    private async Task PreviewAsync()
    {
        Window? owner = GetOwner();
        try
        {
            MediaInfo media = EnsureMedia();
            FilterSet filters = BuildFilterSet(media);
            IsBusy = true;
            StatusMessage = Lang.Resources.Get("StatusPreview");
            try
            {
                using var ffplay = await Task.Run(
                    () => PreviewService.StartPreview(InputFile, filters, media, AudioEnabled)).ConfigureAwait(false);
                await ffplay.WaitForExitAsync().ConfigureAwait(false);
                string error = ffplay.ErrorLog;
                await Dispatcher.UIThread.InvokeAsync(async () =>
                {
                    SetStatus(null);
                    if (error.Length > 0 && owner is not null)
                        await MessageDialog.ShowAsync(owner, "Preview", error);
                });
            }
            finally
            {
                await Dispatcher.UIThread.InvokeAsync(() => IsBusy = false);
            }
        }
        catch (Exception ex) when (ex is ArgumentException || ex is ProbeException || ex is NotSupportedException || ex is InvalidOperationException)
        {
            StatusMessage = string.Format(Lang.Resources.Get("StatusError"), ex.Message);
            if (owner is not null)
                await MessageDialog.ShowAsync(owner, "Error", ex.Message);
        }
    }

    [RelayCommand]
    private async Task DownloadFromUrlAsync(string url)
    {
        Window? owner = GetOwner();
        if (owner is null)
            return;

        var dialog = new DownloadView
        {
            DataContext = new DownloadViewModel(),
        };
        var vm = (DownloadViewModel)dialog.DataContext;
        vm.CloseAction = dialog.Close;
        vm.LoadAction = dialog.Close;
        Task runTask = vm.RunDownloadAsync(url);
        await dialog.ShowDialog(owner);
        await runTask;

        string? file = vm.DownloadedFile;
        if (file is null)
        {
            StatusMessage = "Download failed or cancelled — see log.";
            return;
        }

        InputFile = file;
        if (VideoDownload.Enabled)
            Title = url;
        _settings.RememberedFolderOut = Path.GetDirectoryName(OutputFile) ?? "";
        SettingsStore.Save(_settings);
    }

    [RelayCommand]
    private async Task ExportVapourSynth()
    {
        Window? owner = GetOwner();
        if (owner is null)
            return;

        try
        {
            MediaInfo media = EnsureMedia();
            FilterSet filters = BuildFilterSet(media);
            var notes = VapourSynthScript.UnsupportedNotes(filters);
            string cache = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".ffindex");
            var options = VapourSynthScript.FromFilters(InputFile, cache, filters, media.FrameToTime, media.Fps);
            string script = VapourSynthScript.Generate(options);

            string? file = await FilePickerHelper.PickSaveAsync(
                owner, "Export VapourSynth script",
                Path.GetFileName(Path.ChangeExtension(InputFile, ".vpy")),
                RememberedFolderOut,
                new global::Avalonia.Platform.Storage.FilePickerFileType("VapourSynth script") { Patterns = ["*.vpy"] });
            if (file is null)
                return;

            await File.WriteAllTextAsync(file, script);
            StatusMessage = notes.Count == 0
                ? $"Exported {file}"
                : $"Exported {file} ({notes.Count} filter(s) unmapped: {string.Join("; ", notes)})";
        }
        catch (Exception ex) when (ex is ArgumentException || ex is ProbeException || ex is NotSupportedException)
        {
            StatusMessage = string.Format(Lang.Resources.Get("StatusError"), ex.Message);
            await MessageDialog.ShowAsync(owner, "Error", ex.Message);
        }
    }

    [RelayCommand]
    private void AddTrimRange() => MultiTrimRanges.Add(new TrimRangeEntry());

    [RelayCommand]
    private void RemoveTrimRange(TrimRangeEntry? range)
    {
        if (range is not null)
            MultiTrimRanges.Remove(range);
        RefreshActiveFilters();
    }

    [RelayCommand]
    private void ClearFilters()
    {
        UseTrim = UseCrop = UseResize = UseSubtitle = UseRate = UseFade = UseRotate
            = UseReverse = UseDeinterlace = UseDenoise = UseLevels = UseOverlay = UseDub = UseCaption = false;
        MultiTrimRanges.Clear();
        RefreshActiveFilters();
    }

    [RelayCommand]
    private async Task BrowseSubtitle()
    {
        string? file = await FilePickerHelper.PickOpenAsync(GetOwner(), "Select subtitle file", RememberedFolderIn);
        if (file is not null)
        {
            SubtitleFile = file;
            UseSubtitle = true;
        }
    }

    [RelayCommand]
    private async Task BrowseOverlay()
    {
        string? file = await FilePickerHelper.PickOpenAsync(GetOwner(), "Select overlay image", RememberedFolderIn);
        if (file is not null)
        {
            OverlayFile = file;
            UseOverlay = true;
        }
    }

    [RelayCommand]
    private async Task BrowseDub()
    {
        string? file = await FilePickerHelper.PickOpenAsync(GetOwner(), "Select dub audio file", RememberedFolderIn);
        if (file is not null)
        {
            DubFile = file;
            UseDub = true;
        }
    }

    /// <summary>Loads a file chosen outside the browse dialog (Enter key / drag-drop).</summary>
    public void LoadInputFile(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return;
        InputFile = path;
        RememberFolders(System.IO.Path.GetDirectoryName(path), null);
    }

    private Window? GetOwner() => App.Current?.ApplicationLifetime is global::Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop
        ? desktop.MainWindow
        : null;

    internal void RememberFolders(string? inFolder, string? outFolder)
    {
        if (!string.IsNullOrWhiteSpace(inFolder))
            _settings.RememberedFolderIn = inFolder;
        if (!string.IsNullOrWhiteSpace(outFolder))
            _settings.RememberedFolderOut = outFolder;
        SettingsStore.Save(_settings);
    }

    internal string RememberedFolderIn => _settings.RememberedFolderIn;
    internal string RememberedFolderOut => _settings.RememberedFolderOut;
}
