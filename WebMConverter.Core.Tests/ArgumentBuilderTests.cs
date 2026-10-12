using System;
using WebMConverter.Core;
using WebMConverter.Core.Filters;
using Xunit;

namespace WebMConverter.Core.Tests;

/// <summary>
/// Argument-builder vectors verified against the legacy WinForms behavior.
/// </summary>
public sealed class ArgumentBuilderTests
{
    private static MediaInfo TestMedia() => new()
    {
        DurationSeconds = 60,
        Width = 1920,
        Height = 1080,
        Fps = 30,
        HasAudio = true,
    };

    [Fact]
    public void VariableVp9Default()
    {
        var settings = new EncodeSettings { Threads = 8, Slices = 4 };
        string options = FfmpegArgumentBuilder.BuildOptions(settings, new FilterSet(), TestMedia());
        Assert.Equal(
            " -c:v libvpx-vp9 -pix_fmt yuv420p -threads 8 -slices 4 -row-mt 1 -ac 2 -c:a libopus -qmin 28 -crf 30 -qmax 32 -qcomp 1 -b:v 0",
            options);
    }

    [Fact]
    public void ConstantVp8NoAudio()
    {
        var settings = new EncodeSettings
        {
            Mode = EncodingMode.Constant,
            Codec = VideoCodec.Vp8,
            AudioEnabled = false,
            VideoBitrateK = "900",
            Threads = 8,
            Slices = 4,
        };
        string options = FfmpegArgumentBuilder.BuildOptions(settings, new FilterSet(), TestMedia());
        Assert.Equal(
            " -an -c:v libvpx -pix_fmt yuv420p -threads 8 -slices 4 -minrate:v 900k -b:v 900k -maxrate:v 900k -bufsize 540k -rc_init_occupancy 3600k -qcomp 0",
            options);
    }

    [Fact]
    public void SizeLimitDerivesBitrate()
    {
        var settings = new EncodeSettings { Mode = EncodingMode.Constant, SizeLimitMb = "10", Threads = 8, Slices = 4 };
        string options = FfmpegArgumentBuilder.BuildOptions(settings, new FilterSet(), TestMedia());

        // Recomputed with identical float ops (legacy uses float32 here).
        float lim = 10f - 0.01f;
        lim *= 1024 * 1024;
        int fs = (int)lim;
        int vb = (int)(lim / (1799.0 / 30.0) / 1024 * 8) - 64; // frame-clock duration, not nominal 60s

        Assert.Contains("-fs " + fs, options);
        Assert.Contains("-b:v " + vb + "k", options);
        Assert.Contains("-b:a 64k", options);
    }

    [Fact]
    public void HqPassesUseDevNullOnLinux()
    {
        string[] passes = FfmpegArgumentBuilder.BuildPassArguments(
            "o.webm", "OPTS -c:v libvpx-vp9 -ac 2 -c:a libopus", hqMode: true, "/tmp/xyz");
        Assert.Equal(2, passes.Length);
        Assert.Contains("-pass 1 -passlogfile \"/tmp/xyz\"", passes[0]);
        Assert.Contains("-an -c:v libvpx-vp9", passes[0]);
        Assert.Contains("-pass 2", passes[1]);
        Assert.Contains("\"o.webm\"", passes[1]);

        if (!System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Windows))
            Assert.Contains("\"/dev/null\"", passes[0]);
    }

    [Fact]
    public void SinglePass()
    {
        string[] passes = FfmpegArgumentBuilder.BuildPassArguments("o.webm", "OPTS", hqMode: false, "");
        Assert.Single(passes);
        Assert.Equal(" OPTS -f webm -y \"o.webm\"", passes[0]);
    }

    [Fact]
    public void TrimPrependsSeekAndDuration()
    {
        var filters = new FilterSet { Trim = new TrimFilter(90, 270) }; // 3s..9s @30fps
        string options = FfmpegArgumentBuilder.BuildOptions(
            new EncodeSettings { Threads = 2, Slices = 1 }, filters, TestMedia());
        Assert.EndsWith(" -ss 3 -t 6", options);
        Assert.Equal(6.0, filters.Trim.GetDuration(TestMedia().FrameToTime), precision: 9);
    }

    [Fact]
    public void CropAppliedOnce()
    {
        // Regression: legacy fed the crop-adjusted resolution back into the
        // mapper, shrinking twice (300 -> 280). Fixed: raw input in.
        var filters = new FilterSet { Crop = new CropFilter(10, 20, -10, -20) };
        string options = FfmpegArgumentBuilder.BuildOptions(
            new EncodeSettings { Threads = 2, Slices = 1 }, filters,
            new MediaInfo { DurationSeconds = 4, Width = 320, Height = 240, Fps = 30, HasAudio = true });
        Assert.Contains("-vf \"crop=300:200:10:20\"", options);
    }

    [Fact]
    public void InvalidLimitThrowsLegacyMessage()
    {
        var ex = Assert.Throws<ArgumentException>(() => FfmpegArgumentBuilder.BuildOptions(
            new EncodeSettings { Mode = EncodingMode.Constant, SizeLimitMb = "abc" }, new FilterSet(), TestMedia()));
        Assert.Equal("Invalid size limit!", ex.Message);
    }

    [Fact]
    public void LowAudioBitrateThrowsLegacyMessage()
    {
        var ex = Assert.Throws<ArgumentException>(() => FfmpegArgumentBuilder.BuildOptions(
            new EncodeSettings { Mode = EncodingMode.Constant, AudioBitrateK = "10" }, new FilterSet(), TestMedia()));
        Assert.StartsWith("Audio bitrate is too low!", ex.Message);
    }

    [Theory]
    [InlineData(1920, 1080, 4)]
    [InlineData(1280, 720, 3)]
    [InlineData(640, 480, 2)]
    [InlineData(320, 240, 1)]
    public void SlicesForResolution(int w, int h, int expected) =>
        Assert.Equal(expected, FfmpegArgumentBuilder.SlicesForResolution(w, h));
}
