using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using WebMConverter.Core.Filters;

namespace WebMConverter.Core;

/// <summary>
/// Builds the ffmpeg command line. Exact port of MainForm.GenerateArguments
/// plus the pass assembly in MainForm.Convert, with two platform fixes:
/// pass-1 output uses /dev/null off Windows, and input/output validation is
/// shared here instead of living in the form.
/// </summary>
public static class FfmpegArgumentBuilder
{
    /// <summary>{0} output, {1} options, {2} pass argument or blank.</summary>
    public const string Template = " {1}{2} -f webm -y \"{0}\"";

    /// <summary>{0} pass number, {1} pass .log prefix.</summary>
    public const string PassArgument = " -pass {0} -passlogfile \"{1}\"";

    public const string TemplateArguments = "{0} -c:v {5} -pix_fmt yuv420p -threads {1} -slices {2}{3}{4}{6}{7}{8}";
    public const string ConstantVideoArguments = " -minrate:v {0}k -b:v {0}k -maxrate:v {0}k -bufsize {2}k -rc_init_occupancy {3}k -qcomp 0{1}";
    public const string ConstantAudioArguments = " -b:a {0}k";
    public const string VariableVideoArguments = " -qmin {0} -crf {1} -qmax {2} -qcomp 1 -b:v 0";
    public const string VariableAudioArguments = " -qscale:a {0}";

    private static readonly char[] InvalidChars = Path.GetInvalidPathChars();

    public static void ValidateInputFile(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            throw new Exception("No input file!");
        if (InvalidChars.Any(input.Contains))
            throw new Exception($"Input path contains invalid characters!{Environment.NewLine}Invalid characters: " + string.Join(" ", InvalidChars));
        if (!File.Exists(input))
            throw new Exception("Input file doesn't exist!");
    }

    public static void ValidateOutputFile(string output)
    {
        if (string.IsNullOrWhiteSpace(output))
            throw new Exception("No output file!");
        if (InvalidChars.Any(output.Contains))
            throw new Exception("Output path contains invalid characters!\nInvalid characters: " + string.Join(" ", InvalidChars));
    }

    /// <summary>
    /// Effective output duration in seconds (Trim / MultipleTrim / full length,
    /// divided by Rate). Port of the "easy way" in MainForm.GetDuration.
    /// </summary>
    public static double EffectiveDuration(MediaInfo media, FilterSet filters)
    {
        double duration;
        if (filters.Trim is not null)
            duration = filters.Trim.GetDuration(media.FrameToTime);
        else if (filters.MultipleTrim is not null)
            duration = filters.MultipleTrim.GetDuration(media.FrameToTime);
        else
            duration = media.FrameToTime(Math.Max(0, media.FrameCount - 1)).TotalSeconds;

        if (filters.Rate is not null)
            duration /= (float)filters.Rate.Multiplier / 100;

        return duration;
    }

    /// <summary>Output resolution after Resize (and Crop, mirroring legacy).</summary>
    public static (int Width, int Height) EffectiveResolution(MediaInfo media, FilterSet filters)
    {
        if (filters.Resize is not null)
            return (filters.Resize.TargetWidth, filters.Resize.TargetHeight);

        int width = media.Width;
        int height = media.Height;
        if (filters.Crop is not null)
        {
            width = width - filters.Crop.Left + filters.Crop.Right;
            height = height - filters.Crop.Top + filters.Crop.Bottom;
        }

        return (width, height);
    }

    public static int SlicesForResolution(int width, int height)
    {
        long pixels = (long)width * height;
        if (pixels >= 2073600) // 1080p
            return 4;
        if (pixels >= 921600) // 720p
            return 3;
        if (pixels >= 307200) // 480p
            return 2;
        return 1;
    }

    public static string BuildOptions(EncodeSettings settings, FilterSet filters, MediaInfo media)
    {
        string qualityArguments;
        switch (settings.Mode)
        {
            case EncodingMode.Constant:
                float limit = 0;
                string limitTo = string.Empty;
                if (!string.IsNullOrWhiteSpace(settings.SizeLimitMb))
                {
                    if (!float.TryParse(settings.SizeLimitMb.Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out limit))
                        throw new ArgumentException("Invalid size limit!");

                    // known issue for 2 years: https://trac.ffmpeg.org/ticket/1771
                    limit -= 0.01f;
                    // also, ffmpeg's filesize interpreter is unreliable, so we expand the filesize ourselves.
                    limit *= 1024 * 1024;

                    limitTo = $@" -fs {(int)limit}";
                }

                int audiobitrate = -1;
                if (settings.AudioEnabled)
                    audiobitrate = 64;

                if (!string.IsNullOrWhiteSpace(settings.AudioBitrateK))
                {
                    if (!int.TryParse(settings.AudioBitrateK, out audiobitrate))
                        throw new ArgumentException("Invalid audio bitrate!");
                    if (audiobitrate < 45)
                        throw new ArgumentException("Audio bitrate is too low! It has to be at least 45Kb/s");
                    if (audiobitrate > 500)
                        throw new ArgumentException("Audio bitrate is too high! It can not be higher than 500Kb/s");
                }

                int videobitrate = 900;
                if (!string.IsNullOrWhiteSpace(settings.VideoBitrateK))
                {
                    if (!int.TryParse(settings.VideoBitrateK, out videobitrate))
                        throw new ArgumentException("Invalid video bitrate!");
                }
                else if (limitTo != string.Empty)
                {
                    double duration = EffectiveDuration(media, filters);
                    if (duration > 0)
                        videobitrate = (int)(limit / duration / 1024 * 8) - audiobitrate;
                    if (videobitrate < 0)
                        throw new ArgumentException("Your size constraints are too tight! Trim your video or lower your audio bitrate.");
                }

                int bufsize = videobitrate * 6 / 10;
                int initoccupancy = videobitrate * 4;

                qualityArguments = string.Format(CultureInfo.InvariantCulture, ConstantVideoArguments, videobitrate, limitTo, bufsize, initoccupancy);
                if (audiobitrate != -1)
                    qualityArguments += string.Format(CultureInfo.InvariantCulture, ConstantAudioArguments, audiobitrate);
                break;

            case EncodingMode.Variable:
                int qmin = Math.Max(0, (int)(settings.Crf - settings.CrfTolerance));
                int qmax = Math.Min(63, (int)(settings.Crf + settings.CrfTolerance));

                qualityArguments = string.Format(CultureInfo.InvariantCulture, VariableVideoArguments, qmin, settings.Crf, qmax);
                if (settings.AudioEnabled && settings.Codec != VideoCodec.Vp9 && settings.Codec != VideoCodec.Av1) // only for vorbis
                    qualityArguments += string.Format(CultureInfo.InvariantCulture, VariableAudioArguments, settings.AudioQuality);
                break;

            default:
                throw new NotImplementedException();
        }

        string metadataTitle = "";
        if (!string.IsNullOrWhiteSpace(settings.Title))
            metadataTitle = string.Format(CultureInfo.InvariantCulture, @" -metadata title=""{0}""", settings.Title.Replace("\"", "\\\""));

        string hq = "";
        if (settings.HqMode)
            hq = @" -lag-in-frames 16 -auto-alt-ref 1";

        string vcodec;
        string acodec;
        if (settings.Codec == VideoCodec.Av1)
        {
            vcodec = "libsvtav1";
            acodec = "libopus";
            hq += " -preset 6 -svtav1-params tune=0";
        }
        else if (settings.Codec == VideoCodec.Vp9)
        {
            vcodec = "libvpx-vp9";
            acodec = "libopus";
            hq += " -row-mt 1";
        }
        else
        {
            vcodec = "libvpx";
            acodec = "libvorbis";
        }

        string audio;
        if (settings.AudioEnabled)
        {
            audio = "";
            acodec = " -ac 2 -c:a " + acodec;
        }
        else
        {
            audio = " -an";
            acodec = "";
        }

        string framerate = "";
        if (!string.IsNullOrWhiteSpace(settings.FrameRate))
            framerate = @" -r " + settings.FrameRate;

        // Direct ffmpeg path (the AviSynth path is Windows-only and handled elsewhere).
        var (resW, resH) = EffectiveResolution(media, filters);
        string vf = FfmpegFilterMapper.GetVideoFilter(filters, resW, resH) ?? "";
        string af = FfmpegFilterMapper.GetAudioFilter(filters) ?? "";
        string extraFilters = "";
        if (!string.IsNullOrEmpty(vf))
            extraFilters += $" -vf \"{vf}\"";
        if (!string.IsNullOrEmpty(af) && settings.AudioEnabled)
            extraFilters += $" -af \"{af}\"";

        if (filters.Trim is not null)
        {
            double start = media.FrameToTime(filters.Trim.TrimStart).TotalSeconds;
            double dur = filters.Trim.GetDuration(media.FrameToTime);
            extraFilters = string.Format(CultureInfo.InvariantCulture, " -ss {0:0.###} -t {1:0.###}", start, dur) + extraFilters;
        }

        string options = string.Format(
            CultureInfo.InvariantCulture,
            TemplateArguments, audio, settings.Threads, settings.Slices,
            metadataTitle, hq, vcodec, acodec, framerate, qualityArguments) + extraFilters;

        if (!string.IsNullOrWhiteSpace(settings.ExtraArguments))
            options += " " + settings.ExtraArguments.Trim();

        return options;
    }

    /// <summary>
    /// Assembles the one (or two, for HQ) full ffmpeg argument strings.
    /// Port of the tail of MainForm.Convert.
    /// </summary>
    public static string[] BuildPassArguments(string output, string options, bool hqMode, string passLogBase)
    {
        if (!hqMode)
            return [string.Format(CultureInfo.InvariantCulture, Template, output, options, "")];

        string volatileOutput = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "NUL" : "/dev/null";
        var arguments = new string[2];
        arguments[0] = string.Format(CultureInfo.InvariantCulture, Template, volatileOutput, options, string.Format(CultureInfo.InvariantCulture, PassArgument, 1, passLogBase));
        arguments[1] = string.Format(CultureInfo.InvariantCulture, Template, output, options, string.Format(CultureInfo.InvariantCulture, PassArgument, 2, passLogBase));

        if (!arguments[0].Contains("-an")) // skip audio encoding on the first pass
        {
            arguments[0] = arguments[0].Replace("-c:v libvpx-vp9", "-an -c:v libvpx-vp9")
                .Replace("-c:v libsvtav1", "-an -c:v libsvtav1")
                .Replace("-c:v libvpx", "-an -c:v libvpx");
        }

        return arguments;
    }
}
