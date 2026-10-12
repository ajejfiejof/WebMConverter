using System;
using System.Globalization;
using System.Text;

namespace WebMConverter.Core.Filters;

/// <summary>
/// On-screen text caption. The legacy GDI+ PNG-overlay variant was
/// Windows-only; this portable model renders through ffmpeg's drawtext
/// filter on the direct path, which works on every platform.
/// </summary>
public sealed class CaptionFilter
{
    public FilterPoint Placement { get; }
    public string Text { get; }
    public string FontFamily { get; }
    public string? FontFile { get; }
    public int FontSize { get; }
    public uint TextColorArgb { get; }
    public uint BorderColorArgb { get; }
    public int BorderThickness { get; }

    public CaptionFilter(
        FilterPoint placement,
        string text,
        string fontFamily = "DejaVu Sans",
        string? fontFile = null,
        int fontSize = 24,
        uint textColorArgb = 0xFFFFFFFF,
        uint borderColorArgb = 0xFF000000,
        int borderThickness = 1)
    {
        Placement = placement;
        Text = text;
        FontFamily = fontFamily;
        FontFile = fontFile;
        FontSize = fontSize;
        TextColorArgb = textColorArgb;
        BorderColorArgb = borderColorArgb;
        BorderThickness = borderThickness;
    }

    /// <summary>Parses #RRGGBB or #AARRGGBB (leading # optional).</summary>
    public static uint ParseHexColor(string hex)
    {
        string h = hex.Trim().TrimStart('#');
        if (h.Length == 6)
            h = "FF" + h;
        if (h.Length != 8 || !uint.TryParse(h, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out uint argb))
            throw new ArgumentException($"Caption color must be #RRGGBB (got '{hex}').");
        return argb;
    }

    public static string ToHex(uint argb) => $"#{argb:X8}";

    private static string ToDrawtextColor(uint argb) =>
        $"0x{(argb >> 16) & 0xFF:X2}{(argb >> 8) & 0xFF:X2}{argb & 0xFF:X2}";

    /// <summary>Escapes a drawtext text= value (\ : , ' % and newlines).</summary>
    public static string EscapeText(string text)
    {
        var sb = new StringBuilder(text.Length);
        foreach (char c in text)
        {
            switch (c)
            {
                case '\\': sb.Append(@"\\"); break;
                case ':': sb.Append(@"\:"); break;
                case ',': sb.Append(@"\,"); break;
                case '\'': sb.Append(@"\'"); break;
                case '%': sb.Append(@"\%"); break;
                case '\n': sb.Append(@"\n"); break;
                case '\r': break;
                default: sb.Append(c); break;
            }
        }

        return sb.ToString();
    }

    private static string EscapePath(string path) =>
        path.Replace("\\", "/").Replace(":", "\\:").Replace("'", "\\'").Replace(",", "\\,");

    /// <summary>Single drawtext= chain element (no -vf wrapper).</summary>
    public string ToDrawtext()
    {
        var sb = new StringBuilder("drawtext=");
        if (!string.IsNullOrWhiteSpace(FontFile))
            sb.Append($"fontfile='{EscapePath(FontFile)}':");
        else if (!string.IsNullOrWhiteSpace(FontFamily))
            sb.Append($"font='{FontFamily.Replace(":", "\\:").Replace("'", "\\'")}':");
        sb.Append($"text='{EscapeText(Text)}':");
        sb.Append($"x={Placement.X}:y={Placement.Y}:");
        sb.Append($"fontsize={FontSize}:");
        sb.Append($"fontcolor={ToDrawtextColor(TextColorArgb)}:");
        sb.Append($"borderw={BorderThickness}:bordercolor={ToDrawtextColor(BorderColorArgb)}");
        return sb.ToString();
    }
}
