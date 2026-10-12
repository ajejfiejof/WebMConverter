using System;

namespace WebMConverter.Core;

/// <summary>Fractional (0..1) progress plus optional human-readable status.</summary>
public sealed class ConvertProgress
{
    public double Fraction { get; set; }
    public string? Status { get; set; }
}

public sealed class ConvertResult
{
    public bool Succeeded { get; set; }
    public int ExitCode { get; set; }
    public bool Cancelled { get; set; }
    public string? OutputFile { get; set; }
    public string Log { get; set; } = string.Empty;

    /// <summary>
    /// Set when the output probes shorter than the input (filesize-constraint
    /// clipping). Positive seconds of video lost at the tail.
    /// </summary>
    public double DurationMismatchSeconds { get; set; }

    /// <summary>Non-fatal hints for the user (e.g. the thread-crash advisory).</summary>
    public string? Note { get; set; }
}

public sealed class DownloadProgress
{
    public double Fraction { get; set; }
}

public sealed class DownloadResult
{
    public bool Succeeded { get; set; }
    public int ExitCode { get; set; }
    public bool Cancelled { get; set; }
    public string? OutputFile { get; set; }
    public string Log { get; set; } = string.Empty;
}

public sealed class ProbeException(string message, string? probeOutput = null) : Exception(message)
{
    public string? ProbeOutput { get; } = probeOutput;
}
