using System;
using System.Collections.Generic;

namespace WebMConverter.Core.Filters;

/// <summary>
/// Portable 2D point (replaces System.Drawing.Point in filter models).
/// </summary>
public readonly record struct FilterPoint(int X, int Y);

/// <summary>
/// Pure filter parameter models with AviSynth script generation.
/// Exact port of the model classes in Filters/*.cs (editor Forms excluded).
/// CaptionFilter is intentionally absent: its state and BeforeEncode render
/// depend on GDI+ (System.Drawing) and stay Windows-only for now.
/// </summary>
public sealed class TrimFilter
{
    public int TrimStart { get; }
    public int TrimEnd { get; }

    public TrimFilter(int trimStart, int trimEnd)
    {
        TrimStart = trimStart;
        TrimEnd = trimEnd;
    }

    public double GetDuration(Func<int, TimeSpan> frameToTime) =>
        (frameToTime(TrimEnd) - frameToTime(TrimStart)).TotalSeconds;

    public override string ToString() => $"Trim({TrimStart}, {TrimEnd})";
}

public sealed class CropFilter
{
    public int Left { get; }
    public int Top { get; }
    public int Right { get; }
    public int Bottom { get; }

    public CropFilter(int left, int top, int right, int bottom)
    {
        Left = (left / 2) * 2; // Make it even
        Top = (top / 2) * 2;
        Right = (right / 2) * 2;
        Bottom = (bottom / 2) * 2;
    }

    public override string ToString() => $"Crop({Left}, {Top}, {Right}, {Bottom})";
}

public sealed class SubtitleFilter
{
    public string FileName { get; }
    public SubtitleType Type { get; }
    public int Track { get; }

    public SubtitleFilter(string fileName, SubtitleType type, int track = -1)
    {
        FileName = fileName;
        Type = type;
        Track = track;
    }

    public string ToAvisynth(bool weirdPixelFormat)
    {
        string conversion = weirdPixelFormat ? "ConvertToYV12() " : "";
        string shortFileName = PathCompat.GetCompatiblePath(FileName);

        return Type switch
        {
            SubtitleType.TextSub => conversion + $@"textsub(""{shortFileName}"")",
            SubtitleType.VobSub => conversion + $@"vobsub(""{shortFileName}"")",
            SubtitleType.PgsSub => conversion + $@"suptitle(""{FileName}"")",
            _ => throw new NotImplementedException(),
        };
    }

    public override string ToString() => ToAvisynth(weirdPixelFormat: false);
}

public sealed class ResizeFilter
{
    public int TargetWidth { get; }
    public int TargetHeight { get; }

    public ResizeFilter(int targetWidth, int targetHeight)
    {
        TargetWidth = (targetWidth / 2) * 2; // make it even
        TargetHeight = (targetHeight / 2) * 2;
    }

    public override string ToString() => $"LanczosResize({TargetWidth}, {TargetHeight})";
}

public sealed class DubFilter
{
    public string AudioFileName { get; }
    public string IndexFileName { get; }
    public DubMode Mode { get; }

    public DubFilter(string audioFileName, string indexFileName, DubMode mode)
    {
        AudioFileName = audioFileName;
        IndexFileName = indexFileName;
        Mode = mode;
    }

    public override string ToString()
    {
        return Mode switch
        {
            DubMode.TrimAudio => $@"Trim(AudioDub(FFAudioSource(""{AudioFileName}"",cachefile=""{IndexFileName}"")), 0, last.FrameCount)",
            DubMode.LoopVideo => $@"dub = FFAudioSource(""{AudioFileName}"",cachefile=""{IndexFileName}""){Environment.NewLine}" +
                "Loop(-1).AudioDub(dub).Trim(0,int(dub.AudioLength / dub.AudioRate * last.FrameRate))",
            DubMode.JustDub => $@"AudioDub(FFAudioSource(""{AudioFileName}"",cachefile=""{IndexFileName}""))",
            _ => throw new NotImplementedException(),
        };
    }
}

public enum DubMode
{
    TrimAudio,
    LoopVideo,
    JustDub,
}

public sealed class FadeFilter
{
    public bool Start { get; }
    public bool End { get; }
    public int Frames { get; }
    public bool KeepAudio { get; }

    public FadeFilter(bool start, bool end, int frames, bool keepAudio)
    {
        Start = start;
        End = end;
        Frames = frames;
        KeepAudio = keepAudio;
    }

    public override string ToString()
    {
        string blankClip = KeepAudio ? $"AudioDub(BlankClip(last, {Frames}), last)" : $"BlankClip(last, {Frames})";
        string fadeIn = Start ? $"Dissolve({blankClip}, last, {Frames}) " : "";
        string fadeOut = End ? $"Dissolve(last, {blankClip}, {Frames})" : "";
        return fadeIn + fadeOut;
    }
}

public sealed class RateFilter
{
    public int Multiplier { get; }

    public RateFilter(int multiplier)
    {
        Multiplier = multiplier;
    }

    public override string ToString() => $"AssumeScaledFPS({Multiplier}, 100, true)";
}

public sealed class OverlayFilter
{
    public FilterPoint Placement { get; }
    public string FileName { get; }

    public OverlayFilter(FilterPoint placement, string fileName)
    {
        Placement = placement;
        FileName = fileName;
    }

    public override string ToString()
    {
        string shortFileName = PathCompat.GetCompatiblePath(FileName);
        return $"Overlay(ImageSource(\"{shortFileName}\"), x={Placement.X}, y={Placement.Y}, mask=ImageSource(\"{shortFileName}\",pixel_type=\"RGB32\").ShowAlpha(pixel_type=\"RGB32\"))";
    }
}

public sealed class MultipleTrimFilter
{
    public List<TrimFilter> Trims { get; }

    public MultipleTrimFilter(List<TrimFilter> trims)
    {
        Trims = trims;
    }

    public double GetDuration(Func<int, TimeSpan> frameToTime)
    {
        double duration = 0;
        foreach (TrimFilter trim in Trims)
            duration += trim.GetDuration(frameToTime);
        return duration;
    }

    public override string ToString() => string.Join(" + ", Trims);
}

public sealed class DeinterlaceFilter
{
    public override string ToString() => "tdeint()";
}

public sealed class DenoiseFilter
{
    public override string ToString() => "hqdn3d()";
}

public sealed class LevelsFilter
{
    public LevelsConversion Type { get; }

    public LevelsFilter(LevelsConversion type)
    {
        Type = type;
    }

    public override string ToString()
    {
        return Type switch
        {
            LevelsConversion.TVtoPC => "ColorYUV(levels=\"TV->PC\")",
            LevelsConversion.PCtoTV => "ColorYUV(levels=\"PC->TV\")",
            _ => throw new NotImplementedException(),
        };
    }
}

public enum LevelsConversion
{
    TVtoPC,
    PCtoTV,
}

public sealed class ReverseFilter
{
    public override string ToString() => "Reverse()";
}

public sealed class RotateFilter
{
    public RotateMode Mode { get; }
    public bool FlipHorizontal { get; }
    public bool FlipVertical { get; }

    public RotateFilter(RotateMode mode, bool flipHorizontal, bool flipVertical)
    {
        Mode = mode;
        FlipHorizontal = flipHorizontal;
        FlipVertical = flipVertical;
    }

    public override string ToString()
    {
        string command = Mode switch
        {
            RotateMode.Right => "TurnRight() ",
            RotateMode.Twice => "TurnRight() TurnRight() ",
            RotateMode.Left => "TurnLeft() ",
            _ => "",
        };

        if (FlipHorizontal)
            command += "FlipHorizontal() ";
        if (FlipVertical)
            command += "FlipVertical()";

        return command;
    }
}

public enum RotateMode
{
    None,
    Right,
    Twice,
    Left,
}
