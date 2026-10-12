using System;
using System.IO;
using WebMConverter.Core;
using WebMConverter.Core.Media;
using Xunit;

namespace WebMConverter.Core.Tests;

public sealed class MediaIntelligenceTests
{
    [Fact]
    public void SarCompensationMath()
    {
        // Square pixels: no compensation.
        Assert.Null(ProbeService.TryCompensateSar(1920, 1080, "1:1", "16:9"));
        // Zeroes: no compensation.
        Assert.Null(ProbeService.TryCompensateSar(1920, 1080, "0:0", "0:0"));
        // Tall DAR (darNum < darDen): scale height down by SAR.
        var tall = ProbeService.TryCompensateSar(720, 576, "12:11", "3:4");
        Assert.NotNull(tall);
        Assert.Equal(720, tall!.Value.Width);
        Assert.Equal((int)(576 / (12f / 11f)), tall.Value.Height);
        // Wide DAR: scale width up by SAR.
        var wide = ProbeService.TryCompensateSar(720, 480, "32:27", "16:9");
        Assert.NotNull(wide);
        Assert.Equal((int)(720 * (32f / 27f)), wide!.Value.Width);
        Assert.Equal(480, wide.Value.Height);
    }

    [Fact]
    public void SubtitleCodecMapping()
    {
        Assert.Equal((SubtitleType.VobSub, ".idx"), MediaAttachmentService.MapSubtitleCodec("dvdsub"));
        Assert.Equal((SubtitleType.PgsSub, ".sup"), MediaAttachmentService.MapSubtitleCodec("pgssub"));
        Assert.Equal((SubtitleType.TextSub, ".ass"), MediaAttachmentService.MapSubtitleCodec("ass"));
    }

    [Fact]
    public void FingerprintIsDeterministic()
    {
        string file = Path.GetTempFileName();
        try
        {
            File.WriteAllText(file, "hello");
            string a = MediaAttachmentService.Fingerprint(file);
            string b = MediaAttachmentService.Fingerprint(file);
            Assert.Equal(a, b);
            Assert.Equal(32, a.Length);
        }
        finally
        {
            File.Delete(file);
        }
    }

    [Fact]
    public void FontInstallRemoveCycle()
    {
        if (System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Windows))
            return; // Windows path touches session font table; covered by inspection.

        // Use a throwaway fonts dir, never the real user one.
        string fontsDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        string srcDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(srcDir);
        string? previous = FontInstaller.FontsDirectory;
        FontInstaller.FontsDirectory = fontsDir;
        try
        {
            string systemFont = "/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf";
            if (!File.Exists(systemFont))
                return; // No font to test with; environment limitation.

            File.Copy(systemFont, Path.Combine(srcDir, "TestFont.ttf"));
            using (FontInstaller.Install(["TestFont.ttf"], srcDir, log: _ => { }))
            {
                Assert.True(File.Exists(Path.Combine(fontsDir, "TestFont.ttf")));
            }

            Assert.False(File.Exists(Path.Combine(fontsDir, "TestFont.ttf")));
        }
        finally
        {
            FontInstaller.FontsDirectory = previous!;
            try
            {
                Directory.Delete(srcDir, recursive: true);
                Directory.Delete(fontsDir, recursive: true);
            }
            catch (Exception)
            {
                // Best effort.
            }
        }
    }
}
