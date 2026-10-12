using System.Collections.Generic;

namespace WebMConverter.Core.Filters;

/// <summary>
/// Translates a <see cref="FilterSet"/> into ffmpeg -vf/-af chains.
/// Exact port of Program.Filters.GetFFmpegVideoFilter/GetFFmpegAudioFilter.
/// Note: Overlay/Trim/MultipleTrim/Rate/Fade/Dub/Levels are
/// AviSynth-only and intentionally have no mapping here. Caption maps to
/// drawtext (cross-platform); the legacy GDI+ PNG variant is gone.
/// </summary>
public static class FfmpegFilterMapper
{
    public static string? GetVideoFilter(FilterSet filters, int inputWidth = 0, int inputHeight = 0)
    {
        var chain = new List<string>();

        if (filters.Crop is not null && inputWidth > 0 && inputHeight > 0)
        {
            int w = inputWidth - filters.Crop.Left + filters.Crop.Right;
            int h = inputHeight - filters.Crop.Top + filters.Crop.Bottom;
            if (w > 0 && h > 0)
                chain.Add($"crop={w}:{h}:{filters.Crop.Left}:{filters.Crop.Top}");
        }

        if (filters.Resize is not null)
            chain.Add($"scale={filters.Resize.TargetWidth}:{filters.Resize.TargetHeight}:flags=lanczos");

        if (filters.Deinterlace is not null)
            chain.Add("yadif");

        if (filters.Denoise is not null)
            chain.Add("hqdn3d");

        if (filters.Rotate is not null)
        {
            switch (filters.Rotate.Mode)
            {
                case RotateMode.Right:
                    chain.Add("transpose=1");
                    break;
                case RotateMode.Twice:
                    chain.Add("transpose=1,transpose=1");
                    break;
                case RotateMode.Left:
                    chain.Add("transpose=2");
                    break;
            }

            if (filters.Rotate.FlipHorizontal)
                chain.Add("hflip");
            if (filters.Rotate.FlipVertical)
                chain.Add("vflip");
        }

        if (filters.Reverse is not null)
            chain.Add("reverse");

        if (filters.Subtitle is not null && !string.IsNullOrEmpty(filters.Subtitle.FileName))
        {
            string path = filters.Subtitle.FileName.Replace("\\", "/").Replace(":", "\\:").Replace("'", "\\'");
            chain.Add($"subtitles='{path}'");
        }

        if (filters.Caption is not null && !string.IsNullOrEmpty(filters.Caption.Text))
            chain.Add(filters.Caption.ToDrawtext());

        return chain.Count > 0 ? string.Join(",", chain) : null;
    }

    public static string? GetAudioFilter(FilterSet filters)
    {
        var chain = new List<string>();
        if (filters.Reverse is not null)
            chain.Add("areverse");
        return chain.Count > 0 ? string.Join(",", chain) : null;
    }
}
