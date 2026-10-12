using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using WebMConverter.Core.Filters;
using Xunit;
using Xunit.Abstractions;

namespace WebMConverter.Core.Tests;

/// <summary>
/// End-to-end conversion against a real ffmpeg. Skipped when no ffmpeg is
/// installed (e.g. bare CI images); the fast unit tests above always run.
/// </summary>
public sealed class EndToEndTests : IAsyncLifetime
{
    private readonly ITestOutputHelper _output;

    public EndToEndTests(ITestOutputHelper output)
    {
        _output = output;
    }

    private static readonly string Input = Path.Combine(Path.GetTempPath(), "wmc-e2e-in.mp4");
    private static readonly string WorkDir = Path.Combine(Path.GetTempPath(), "wmc-e2e");

    public static bool HaveFfmpeg =>
        FindOnPath("ffmpeg") && FindOnPath("ffprobe");

    private static bool FindOnPath(string name)
    {
        string? path = Environment.GetEnvironmentVariable("PATH");
        if (path is null)
            return false;
        foreach (var dir in path.Split(Path.PathSeparator))
        {
            try
            {
                if (File.Exists(Path.Combine(dir.Trim(), name)))
                    return true;
            }
            catch (Exception)
            {
                // ignore
            }
        }

        return false;
    }

    public async Task InitializeAsync()
    {
        if (!HaveFfmpeg)
        {
            _output.WriteLine("SKIPPED: ffmpeg/ffprobe not installed");
            return;
        }
        Directory.CreateDirectory(WorkDir);

        if (!File.Exists(Input))
        {
            using var gen = new Process();
            gen.StartInfo.FileName = "ffmpeg";
            gen.StartInfo.Arguments = "-hide_banner -loglevel error -f lavfi -i testsrc=duration=4:size=320x240:rate=30 -f lavfi -i sine=frequency=440:duration=4 -pix_fmt yuv420p -shortest " +
                $"-y \"{Input}\"";
            gen.StartInfo.UseShellExecute = false;
            gen.StartInfo.CreateNoWindow = true;
            gen.Start();
            await gen.WaitForExitAsync();
        }
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task ProbeConvertSingleAndTwoPass()
    {
        if (!HaveFfmpeg)
        {
            _output.WriteLine("SKIPPED: ffmpeg/ffprobe not installed");
            return;
        }

        MediaInfo info = ProbeService.ProbeVideoInfo(Input);
        Assert.Equal(320, info.Width);
        Assert.Equal(240, info.Height);
        Assert.True(info.HasAudio);
        Assert.InRange(info.DurationSeconds, 3.5, 4.5);

        var settings = new EncodeSettings { Threads = 2, Slices = 1 };
        string options = FfmpegArgumentBuilder.BuildOptions(settings, new FilterSet(), info);
        var svc = new ConversionService();

        string out1 = Path.Combine(WorkDir, "out1.webm");
        ConvertResult r1 = await svc.ConvertAsync(Input, out1, options, hqMode: false);
        Assert.True(r1.Succeeded, r1.Log);
        Assert.True(File.Exists(out1));

        string out2 = Path.Combine(WorkDir, "out2.webm");
        ConvertResult r2 = await svc.ConvertAsync(Input, out2, options, hqMode: true);
        Assert.True(r2.Succeeded, r2.Log);
        Assert.True(File.Exists(out2));
    }

    [Fact]
    public async Task TrimCropConstant()
    {
        if (!HaveFfmpeg)
        {
            _output.WriteLine("SKIPPED: ffmpeg/ffprobe not installed");
            return;
        }

        MediaInfo info = ProbeService.ProbeVideoInfo(Input);
        var settings = new EncodeSettings { Mode = EncodingMode.Constant, VideoBitrateK = "500", Threads = 2, Slices = 1 };
        var filters = new FilterSet { Trim = new TrimFilter(30, 90), Crop = new CropFilter(0, 0, -20, -20) };
        string options = FfmpegArgumentBuilder.BuildOptions(settings, filters, info);

        string output = Path.Combine(WorkDir, "out3.webm");
        ConvertResult result = await new ConversionService().ConvertAsync(Input, output, options, hqMode: false);
        Assert.True(result.Succeeded, result.Log);

        MediaInfo probed = ProbeService.ProbeVideoInfo(output);
        Assert.Equal(300, probed.Width); // crop applied exactly once
        Assert.InRange(probed.DurationSeconds, 1.6, 2.4);
    }
}
