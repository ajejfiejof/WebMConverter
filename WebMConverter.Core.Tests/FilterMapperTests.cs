using System;
using WebMConverter.Core;
using WebMConverter.Core.Filters;
using Xunit;

namespace WebMConverter.Core.Tests;

public sealed class FilterMapperTests
{
    [Fact]
    public void CropChain()
    {
        var filters = new FilterSet { Crop = new CropFilter(10, 20, -10, -20) };
        Assert.Equal("crop=1900:1040:10:20", FfmpegFilterMapper.GetVideoFilter(filters, 1920, 1080));
    }

    [Fact]
    public void SubtitlePathEscaping()
    {
        var filters = new FilterSet { Subtitle = new SubtitleFilter(@"C:\Vids\a'b:c.ass", SubtitleType.TextSub) };
        Assert.Equal(@"subtitles='C\:/Vids/a\'b\:c.ass'", FfmpegFilterMapper.GetVideoFilter(filters, 640, 480));
    }

    [Fact]
    public void CombinedChainOrder()
    {
        var filters = new FilterSet
        {
            Resize = new ResizeFilter(1280, 720),
            Deinterlace = new DeinterlaceFilter(),
            Denoise = new DenoiseFilter(),
            Rotate = new RotateFilter(RotateMode.Right, flipHorizontal: true, flipVertical: false),
            Reverse = new ReverseFilter(),
        };
        Assert.Equal(
            "scale=1280:720:flags=lanczos,yadif,hqdn3d,transpose=1,hflip,reverse",
            FfmpegFilterMapper.GetVideoFilter(filters, 1920, 1080));
        Assert.Equal("areverse", FfmpegFilterMapper.GetAudioFilter(filters));
    }

    [Fact]
    public void EmptyFiltersMapToNull()
    {
        var filters = new FilterSet();
        Assert.Null(FfmpegFilterMapper.GetVideoFilter(filters, 640, 480));
        Assert.Null(FfmpegFilterMapper.GetAudioFilter(filters));
    }

    [Fact]
    public void AvisynthOnlyFiltersHaveNoMapping()
    {
        var filters = new FilterSet
        {
            Fade = new FadeFilter(start: true, end: true, frames: 30, keepAudio: true),
            Rate = new RateFilter(150),
            Dub = new DubFilter("a.wav", "a.ffindex", DubMode.JustDub),
        };
        Assert.Null(FfmpegFilterMapper.GetVideoFilter(filters, 640, 480));
    }

    [Fact]
    public void CaptionMapsToDrawtext()
    {
        var filters = new FilterSet
        {
            Caption = new CaptionFilter(new FilterPoint(20, 30), "Hello: world", fontSize: 24),
        };
        Assert.Equal(
            "drawtext=font='DejaVu Sans':text='Hello\\: world':x=20:y=30:fontsize=24:fontcolor=0xFFFFFF:borderw=1:bordercolor=0x000000",
            FfmpegFilterMapper.GetVideoFilter(filters, 640, 480));
    }

    [Fact]
    public void CaptionEscaping()
    {
        Assert.Equal(@"a\:b\,c\'d\%e\\f", CaptionFilter.EscapeText("a:b,c'd%e\\f"));
        Assert.Equal(@"line1\nline2", CaptionFilter.EscapeText("line1\nline2"));
        Assert.Equal(0xFFFF0000u, CaptionFilter.ParseHexColor("#FF0000"));
        Assert.Equal(0x80FF0000u, CaptionFilter.ParseHexColor("80FF0000"));
        Assert.Throws<ArgumentException>(() => CaptionFilter.ParseHexColor("red"));
    }

    [Fact]
    public void ModelToStringShapes()
    {
        Assert.Equal("Trim(10, 20)", new TrimFilter(10, 20).ToString());
        Assert.Equal("Crop(10, 20, -10, -20)", new CropFilter(10, 20, -10, -20).ToString());
        Assert.Equal("Crop(10, 20, -8, -18)", new CropFilter(11, 21, -9, -19).ToString()); // int-division truncates toward zero
        Assert.Equal("LanczosResize(1280, 720)", new ResizeFilter(1280, 720).ToString());
        Assert.Equal("AssumeScaledFPS(150, 100, true)", new RateFilter(150).ToString());
        Assert.Equal("Reverse()", new ReverseFilter().ToString());
        Assert.Equal("tdeint()", new DeinterlaceFilter().ToString());
        Assert.Equal("hqdn3d()", new DenoiseFilter().ToString());
        Assert.Equal("ColorYUV(levels=\"TV->PC\")", new LevelsFilter(LevelsConversion.TVtoPC).ToString());
    }
}
