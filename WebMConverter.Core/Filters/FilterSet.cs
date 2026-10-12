namespace WebMConverter.Core.Filters;

/// <summary>
/// Holds the active processing filters for one conversion.
/// Port of the static Program.Filters holder (instance-based for testability).
/// </summary>
public sealed class FilterSet
{
    public CropFilter? Crop { get; set; }
    public DeinterlaceFilter? Deinterlace { get; set; }
    public DenoiseFilter? Denoise { get; set; }
    public DubFilter? Dub { get; set; }
    public FadeFilter? Fade { get; set; }
    public LevelsFilter? Levels { get; set; }
    public MultipleTrimFilter? MultipleTrim { get; set; }
    public OverlayFilter? Overlay { get; set; }
    public RateFilter? Rate { get; set; }
    public ResizeFilter? Resize { get; set; }
    public ReverseFilter? Reverse { get; set; }
    public RotateFilter? Rotate { get; set; }
    public SubtitleFilter? Subtitle { get; set; }
    public TrimFilter? Trim { get; set; }

    public void Reset()
    {
        Crop = null;
        Deinterlace = null;
        Denoise = null;
        Dub = null;
        Fade = null;
        Levels = null;
        MultipleTrim = null;
        Overlay = null;
        Rate = null;
        Resize = null;
        Reverse = null;
        Rotate = null;
        Subtitle = null;
        Trim = null;
    }

    public bool IsEmpty =>
        Crop is null && Deinterlace is null && Denoise is null && Dub is null
        && Fade is null && Levels is null && MultipleTrim is null && Overlay is null
        && Rate is null && Resize is null && Reverse is null && Rotate is null
        && Subtitle is null && Trim is null;
}
