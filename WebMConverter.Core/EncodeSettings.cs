using System;

namespace WebMConverter.Core;

/// <summary>All knobs that feed the ffmpeg command line (ex-UI control state).</summary>
public sealed class EncodeSettings
{
    public EncodingMode Mode { get; set; } = EncodingMode.Variable;
    public VideoCodec Codec { get; set; } = VideoCodec.Vp9;
    public bool AudioEnabled { get; set; } = true;

    /// <summary>Raw user text (MB), like the old limit box. Null/empty = no limit.</summary>
    public string? SizeLimitMb { get; set; }

    /// <summary>Raw user text (kbit/s). Null/empty = default 900.</summary>
    public string? VideoBitrateK { get; set; }

    /// <summary>Raw user text (kbit/s). Null/empty = default 64 when audio on.</summary>
    public string? AudioBitrateK { get; set; }

    public double Crf { get; set; } = 30;
    public double CrfTolerance { get; set; } = 2;
    public double AudioQuality { get; set; } = 3;

    public int Threads { get; set; } = Environment.ProcessorCount;
    public int Slices { get; set; } = 4;
    public string Title { get; set; } = string.Empty;
    public bool HqMode { get; set; }

    /// <summary>Raw user text, appended as -r. Null/empty = source rate.</summary>
    public string? FrameRate { get; set; }

    /// <summary>Avalonia addition: appended verbatim after generated options.</summary>
    public string? ExtraArguments { get; set; }
}

/// <summary>
/// Probed media properties. Without FFMSSharp on Linux, frame-exact PTS math
/// is approximated from the average frame rate.
/// </summary>
public sealed class MediaInfo
{
    public double DurationSeconds { get; set; } = -1;
    public int Width { get; set; }
    public int Height { get; set; }
    public double Fps { get; set; } = 30;
    public bool HasAudio { get; set; }

    public int FrameCount => DurationSeconds > 0 && Fps > 0
        ? Math.Max(1, (int)Math.Round(DurationSeconds * Fps))
        : 0;

    /// <summary>SAR-compensated dimensions when the container needs it (legacy parity).</summary>
    public int? SarCompensatedWidth { get; set; }
    public int? SarCompensatedHeight { get; set; }
    public bool IsFrapsYuvj420p { get; set; }
    public bool HasWeirdPixelFormat { get; set; }
    public string? ContainerTitle { get; set; }

    public TimeSpan FrameToTime(int frame) =>
        Fps > 0 ? TimeSpan.FromSeconds(frame / Fps) : TimeSpan.Zero;
}
