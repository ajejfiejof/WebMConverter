using System.Globalization;
using WebMConverter.Core.Filters;
using WebMConverter.Core.Processes;

namespace WebMConverter.Core;

/// <summary>
/// Opens an ffplay preview window. Port of the direct (non-AviSynth) branch
/// of MainForm.Preview. The caller owns the returned process (await
/// WaitForExitAsync, then read ErrorLog).
/// </summary>
public static class PreviewService
{
    public static FfplayProcess StartPreview(string input, FilterSet filters, MediaInfo media, bool audioEnabled)
    {
        FfmpegArgumentBuilder.ValidateInputFile(input);

        string disableAudio = audioEnabled ? "" : "-an";
        string? vf = FfmpegFilterMapper.GetVideoFilter(filters, media.Width, media.Height);
        string filterArg = string.IsNullOrEmpty(vf) ? "" : $"-vf \"{vf}\"";

        string trimArg = "";
        if (filters.Trim is not null)
        {
            double start = media.FrameToTime(filters.Trim.TrimStart).TotalSeconds;
            double dur = filters.Trim.GetDuration(media.FrameToTime);
            trimArg = string.Format(CultureInfo.InvariantCulture, "-ss {0:0.###} -t {1:0.###} ", start, dur);
        }

        var ffplay = new FfplayProcess($@"-window_title Preview -loop 0 {trimArg}{disableAudio} -i ""{input}"" {filterArg}");
        ffplay.Start();
        return ffplay;
    }
}
