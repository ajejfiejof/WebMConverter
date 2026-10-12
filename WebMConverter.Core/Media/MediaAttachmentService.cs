using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Xml.XPath;
using WebMConverter.Core.Processes;

namespace WebMConverter.Core.Media;

public sealed class EmbeddedSubtitle
{
    public int StreamIndex { get; set; }
    public string Name { get; set; } = string.Empty;
    public SubtitleType Type { get; set; }
    public string File { get; set; } = string.Empty;
}

public sealed class AttachmentResult
{
    public List<EmbeddedSubtitle> Subtitles { get; } = new();
    public List<string> FontFiles { get; } = new();
}

/// <summary>
/// Extracts embedded subtitles and font attachments from matroska inputs.
/// Port of the attachment loop in legacy indexing (ffprobe streams +
/// mkvextract), minus all UI. Degrades gracefully when mkvextract is absent.
/// </summary>
public static class MediaAttachmentService
{
    /// <summary>Stable per-file key (legacy: filename bytes + first 4K, MD5).</summary>
    public static string Fingerprint(string path)
    {
        string name = Path.GetFileNameWithoutExtension(path) ?? "";
        var buffer = new byte[4096];
        byte[] nameBytes = Encoding.UTF8.GetBytes(name);
        Array.Copy(nameBytes, buffer, Math.Min(nameBytes.Length, buffer.Length));
        try
        {
            using var stream = File.OpenRead(path);
            stream.Read(buffer, Math.Min(nameBytes.Length, buffer.Length - 1), buffer.Length - Math.Min(nameBytes.Length, buffer.Length - 1));
        }
        catch (Exception)
        {
            // Hash what we have.
        }

        using var md5 = MD5.Create();
        return BitConverter.ToString(md5.ComputeHash(buffer)).Replace("-", "");
    }

    public static string AttachmentDirectoryFor(string inputFile) =>
        Path.Combine(Path.GetTempPath(), Fingerprint(inputFile) + ".attachments");

    public static AttachmentResult ExtractAttachments(
        string inputFile,
        string attachmentDir,
        Action<string>? log = null,
        string? mkvextractPath = null)
    {
        var result = new AttachmentResult();
        Directory.CreateDirectory(attachmentDir);

        mkvextractPath ??= BinaryResolver.GetBinaryPath("mkvextract");
        bool haveExtractor = File.Exists(mkvextractPath) || !Path.IsPathRooted(mkvextractPath);
        if (!haveExtractor)
        {
            log?.Invoke("mkvextract not found, skipping attachment extraction.");
            return result;
        }

        string xml;
        using (var prober = new FfprobeProcess(inputFile, format: "", argument: "-show_streams"))
            xml = prober.Probe();

        using var reader = new StringReader(xml);
        var nav = new XPathDocument(reader).CreateNavigator();
        int attachIndex = 0; // mkvextract separates track and attachment indices

        foreach (XPathNavigator? stream in nav.Select("//ffprobe/streams/stream"))
        {
            if (stream is null)
                continue;

            if (!int.TryParse(stream.GetAttribute("index", ""), out int streamIndex))
                continue;

            switch (stream.GetAttribute("codec_type", ""))
            {
                case "subtitle":
                    ExtractSubtitle(inputFile, attachmentDir, stream, streamIndex, result, log, mkvextractPath);
                    break;
                case "attachment":
                    attachIndex++; // incremented for every attachment, like legacy
                    ExtractFont(inputFile, attachmentDir, stream, attachIndex, result, log, mkvextractPath);
                    break;
            }
        }

        return result;
    }

    internal static (SubtitleType Type, string Extension) MapSubtitleCodec(string codec) => codec switch
    {
        "dvdsub" => (SubtitleType.VobSub, ".idx"),
        "pgssub" => (SubtitleType.PgsSub, ".sup"),
        _ => (SubtitleType.TextSub, "." + codec), // YOLO, like legacy
    };

    private static void ExtractSubtitle(
        string inputFile, string attachmentDir, XPathNavigator stream, int streamIndex,
        AttachmentResult result, Action<string>? log, string mkvextractPath)
    {
        string codec = stream.GetAttribute("codec_name", "");
        var (type, extension) = MapSubtitleCodec(codec);

        string file = Path.Combine(attachmentDir, $"sub{streamIndex}{extension}");
        log?.Invoke($"Found subtitle track #{streamIndex} ({codec})");

        if (!File.Exists(file))
        {
            log?.Invoke("Extracting...");
            using var extract = new MkvExtractProcess($@"tracks ""{inputFile}"" ""{streamIndex}:{file}""");
            OverrideBinary(extract, mkvextractPath);
            extract.Start();
            extract.WaitForExit();
        }
        else
        {
            log?.Invoke("Already extracted, skipping.");
        }

        if (!File.Exists(file))
            return;

        string name = codec;
        var titleTag = stream.SelectSingleNode(".//tag[@key='title']");
        if (titleTag is not null)
        {
            string title = titleTag.GetAttribute("value", "");
            if (!string.IsNullOrEmpty(title))
                name = title;
        }

        result.Subtitles.Add(new EmbeddedSubtitle
        {
            StreamIndex = streamIndex,
            Name = $"#{streamIndex}: {name}",
            Type = type,
            File = file,
        });
    }

    private static void ExtractFont(
        string inputFile, string attachmentDir, XPathNavigator stream, int attachIndex,
        AttachmentResult result, Action<string>? log, string mkvextractPath)
    {
        var tags = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (XPathNavigator? tag in stream.Select(".//tag"))
        {
            if (tag is null)
                continue;
            tags[tag.GetAttribute("key", "")] = tag.GetAttribute("value", "");
        }

        tags.TryGetValue("filename", out string? filename);
        tags.TryGetValue("mimetype", out string? mimetype);
        if (string.IsNullOrEmpty(filename))
            return;

        log?.Invoke($"Found attachment '{filename}'");
        if (mimetype is null || !mimetype.Contains("font"))
        {
            log?.Invoke("Not a font, skipping.");
            return;
        }

        result.FontFiles.Add(filename);
        string file = Path.Combine(attachmentDir, filename);
        if (File.Exists(file))
        {
            log?.Invoke("Already extracted, skipping.");
            return;
        }

        log?.Invoke("Extracting...");
        using var extract = new MkvExtractProcess($@"attachments ""{inputFile}"" ""{attachIndex}:{file}""");
        OverrideBinary(extract, mkvextractPath);
        extract.Start();
        extract.WaitForExit();
    }

    private static void OverrideBinary(System.Diagnostics.Process process, string path)
    {
        // MkvExtractProcess resolves its own binary; allow explicit override.
        if (File.Exists(path))
            process.StartInfo.FileName = path;
    }
}
