using System;
using System.Globalization;
using System.IO;
using System.Xml.XPath;
using WebMConverter.Core.Processes;

namespace WebMConverter.Core;

/// <summary>
/// Media probing via ffprobe XML output. Replaces the FFMSSharp-backed
/// GetDuration/GetResolution on machines without FFMS (i.e. Linux).
/// </summary>
public static class ProbeService
{
    public static MediaInfo ProbeVideoInfo(string inputFile)
    {
        using var prober = new FfprobeProcess(inputFile, format: "", argument: "-show_format -show_streams");
        string xml = prober.Probe();
        return ParseVideoInfo(xml, inputFile);
    }

    public static double ProbeDuration(string inputFile)
    {
        using var prober = new FfprobeProcess(inputFile, format: "", argument: "-show_format");
        string xml = prober.Probe();
        return ParseFormatDuration(xml, inputFile);
    }

    internal static double ParseFormatDuration(string xml, string inputFile)
    {
        try
        {
            using var reader = new StringReader(xml);
            var doc = new XPathDocument(reader);
            var format = doc.CreateNavigator().SelectSingleNode("//ffprobe/format");
            if (format is null)
                throw new ProbeException($"ffprobe returned no format section for '{inputFile}'.", xml);

            if (!double.TryParse(format.GetAttribute("duration", ""), NumberStyles.Float, CultureInfo.InvariantCulture, out double duration))
                throw new ProbeException($"ffprobe returned no duration for '{inputFile}'.", xml);

            double startTime = 0;
            string startAttr = format.GetAttribute("start_time", "");
            if (!string.IsNullOrEmpty(startAttr))
                double.TryParse(startAttr, NumberStyles.Float, CultureInfo.InvariantCulture, out startTime);

            return duration - startTime;
        }
        catch (ProbeException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new ProbeException($"Failed to get duration from file '{inputFile}': {ex.Message}", xml);
        }
    }

    internal static MediaInfo ParseVideoInfo(string xml, string inputFile)
    {
        var info = new MediaInfo();
        try
        {
            using var reader = new StringReader(xml);
            var nav = new XPathDocument(reader).CreateNavigator();

            var format = nav.SelectSingleNode("//ffprobe/format");
            if (format is not null
                && double.TryParse(format.GetAttribute("duration", ""), NumberStyles.Float, CultureInfo.InvariantCulture, out double duration))
            {
                double startTime = 0;
                string startAttr = format.GetAttribute("start_time", "");
                if (!string.IsNullOrEmpty(startAttr))
                    double.TryParse(startAttr, NumberStyles.Float, CultureInfo.InvariantCulture, out startTime);
                info.DurationSeconds = duration - startTime;
            }

            var video = nav.SelectSingleNode("//ffprobe/streams/stream[@codec_type='video']");
            if (video is null)
                throw new ProbeException($"ffprobe found no video stream in '{inputFile}'.", xml);

            int.TryParse(video.GetAttribute("width", ""), out int width);
            int.TryParse(video.GetAttribute("height", ""), out int height);
            info.Width = width;
            info.Height = height;
            info.Fps = ParseFrameRate(video.GetAttribute("avg_frame_rate", ""));
            if (info.Fps <= 0)
                info.Fps = ParseFrameRate(video.GetAttribute("r_frame_rate", ""));

            info.HasAudio = nav.SelectSingleNode("//ffprobe/streams/stream[@codec_type='audio']") is not null;

            string codec = video.GetAttribute("codec_name", "");
            string pixFmt = video.GetAttribute("pix_fmt", "");
            if (codec == "fraps" && pixFmt == "yuvj420p")
                info.IsFrapsYuvj420p = true;
            if (pixFmt.StartsWith("yuv444p"))
                info.HasWeirdPixelFormat = true;

            var sarComp = TryCompensateSar(
                info.Width, info.Height,
                video.GetAttribute("sample_aspect_ratio", ""),
                video.GetAttribute("display_aspect_ratio", ""));
            if (sarComp is not null)
            {
                info.SarCompensatedWidth = sarComp.Value.Width;
                info.SarCompensatedHeight = sarComp.Value.Height;
            }

            var titleTag = nav.SelectSingleNode("//ffprobe/format/tag[@key='title']");
            if (titleTag is not null)
            {
                string title = titleTag.GetAttribute("value", "");
                if (!string.IsNullOrEmpty(title))
                    info.ContainerTitle = title;
            }

            if (info.Width <= 0 || info.Height <= 0)
                throw new ProbeException($"ffprobe returned no resolution for '{inputFile}'.", xml);

            return info;
        }
        catch (ProbeException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new ProbeException($"Failed to probe file '{inputFile}': {ex.Message}", xml);
        }
    }

    /// <summary>
    /// Legacy SAR compensation: non-1:1 sample aspect ratios get a resize to
    /// the display dimensions. Returns null when no compensation is needed.
    /// </summary>
    internal static (int Width, int Height)? TryCompensateSar(int width, int height, string sar, string dar)
    {
        string[] s = (sar ?? "").Split(':');
        string[] d = (dar ?? "").Split(':');
        if (s.Length != 2 || d.Length != 2)
            return null;
        if (!float.TryParse(s[0], NumberStyles.Float, CultureInfo.InvariantCulture, out float sarNum)
            || !float.TryParse(s[1], NumberStyles.Float, CultureInfo.InvariantCulture, out float sarDen)
            || !float.TryParse(d[0], NumberStyles.Float, CultureInfo.InvariantCulture, out float darNum)
            || !float.TryParse(d[1], NumberStyles.Float, CultureInfo.InvariantCulture, out float darDen))
            return null;
        if ((sarNum == 1 && sarDen == 1) || sarNum == 0 || sarDen == 0)
            return null;
        if (sarDen == 0)
            return null;

        int compW = width;
        int compH = height;
        if (darNum < darDen)
            compH = (int)(compH / (sarNum / sarDen));
        else
            compW = (int)(compW * (sarNum / sarDen));
        return (compW, compH);
    }

    internal static double ParseFrameRate(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return 0;
        string[] parts = value.Split('/');
        if (parts.Length == 2
            && double.TryParse(parts[0], NumberStyles.Float, CultureInfo.InvariantCulture, out double num)
            && double.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out double den)
            && den != 0)
            return num / den;
        if (double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out double flat))
            return flat;
        return 0;
    }
}
