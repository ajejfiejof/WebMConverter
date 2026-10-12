using System;
using WebMConverter.Core;
using Xunit;

namespace WebMConverter.Core.Tests;

public sealed class ProgressParsingTests
{
    private sealed class SyncProgress : IProgress<double>
    {
        public double Value = double.NaN;
        public void Report(double value) => Value = value;
    }

    [Fact]
    public void FfmpegTimeParsesToFraction()
    {
        var progress = new SyncProgress();
        ConversionService.ReportProgress(
            "frame=  121 fps= 48 q=0.0 size=     552kB time=00:00:05.08 bitrate= 888.7kbits/s",
            10.16, progress);
        Assert.Equal(0.5, progress.Value, precision: 9);
    }

    [Fact]
    public void ProgressClampsAboveOne()
    {
        var progress = new SyncProgress();
        ConversionService.ReportProgress("frame= 999 time=00:00:20.00 bitrate= 1kbits/s", 10.0, progress);
        Assert.Equal(1.0, progress.Value, precision: 9);
    }

    [Fact]
    public void NonProgressLinesIgnored()
    {
        var progress = new SyncProgress();
        ConversionService.ReportProgress("frame= 1 time=n/a", 10.0, progress);
        ConversionService.ReportProgress("Stream mapping:", 10.0, progress);
        Assert.True(double.IsNaN(progress.Value));
    }

    [Fact]
    public void YtdlpPercentParses()
    {
        var progress = new SyncProgress();
        DownloadService.ReportProgress("[download]  51.5% of ~3.85MiB at 20.49MiB/s ETA 00:00", progress);
        Assert.Equal(0.515, progress.Value, precision: 9);
    }

    [Fact]
    public void FrameRateFractions()
    {
        Assert.Equal(30000.0 / 1001.0, ProbeService.ParseFrameRate("30000/1001"), precision: 9);
        Assert.Equal(25.0, ProbeService.ParseFrameRate("25/1"), precision: 9);
        Assert.Equal(0.0, ProbeService.ParseFrameRate("0/0"), precision: 9);
        Assert.Equal(0.0, ProbeService.ParseFrameRate(""), precision: 9);
    }

    [Fact]
    public void FormatDurationSubtractsStartTime()
    {
        const string xml = "<ffprobe><format duration=\"61.5\" start_time=\"1.5\"/></ffprobe>";
        Assert.Equal(60.0, ProbeService.ParseFormatDuration(xml, "x"), precision: 9);
    }
}
