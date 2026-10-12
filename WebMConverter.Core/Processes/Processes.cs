using System.Collections.Generic;
using System;
using System.Diagnostics;
using System.Text;

namespace WebMConverter.Core.Processes;

public sealed class FfmpegProcess : Process
{
    public string FfmpegPath;

    public FfmpegProcess(string arguments, bool force32Bit = false)
    {
        string folder = force32Bit ? "x86" : BinaryResolver.DefaultBinariesFolder();
        FfmpegPath = BinaryResolver.GetBinaryPath("ffmpeg", folder);

        StartInfo.FileName = FfmpegPath;
        StartInfo.Arguments = "-hide_banner " + arguments;
        StartInfo.RedirectStandardInput = true;
        StartInfo.RedirectStandardOutput = true;
        StartInfo.RedirectStandardError = true;
        StartInfo.UseShellExecute = false;
        StartInfo.CreateNoWindow = true;
        EnableRaisingEvents = true;
    }

    public new void Start() => Start(outputReadLine: true);

    public void Start(bool outputReadLine)
    {
        base.Start();
        BeginErrorReadLine();
        if (outputReadLine)
            BeginOutputReadLine();
    }
}

public sealed class FfprobeProcess : Process
{
    public string FfprobePath;

    public FfprobeProcess(string inputFile, string format = "", List<string>? dataToProbe = null, string? argument = null)
    {
        FfprobePath = BinaryResolver.GetBinaryPath("ffprobe");

        if (argument is null)
        {
            dataToProbe ??= ["streams"];
            var sb = new StringBuilder();
            foreach (string type in dataToProbe)
                sb.Append(" -show_").Append(type);
            StartInfo.Arguments = $"{format} \"{inputFile}\" -of xml{sb}";
        }
        else
        {
            StartInfo.Arguments = $"{format} \"{inputFile}\" -of xml {argument}";
        }

        StartInfo.FileName = FfprobePath;
        StartInfo.RedirectStandardInput = true;
        StartInfo.RedirectStandardOutput = true;
        StartInfo.RedirectStandardError = true;
        StartInfo.UseShellExecute = false;
        StartInfo.CreateNoWindow = true;
        EnableRaisingEvents = true;
    }

    public new void Start()
    {
        base.Start();
        BeginErrorReadLine();
        BeginOutputReadLine();
    }

    public string Probe()
    {
        var output = new StringBuilder();
        OutputDataReceived += (_, args) => output.AppendLine(args.Data);

        Start();
        WaitForExit();

        return output.ToString();
    }
}

public sealed class FfplayProcess : Process
{
    private readonly StringBuilder _errorLog = new();

    public string FfplayPath;
    public string ErrorLog => _errorLog.ToString().Trim();

    public FfplayProcess(string arguments)
    {
        FfplayPath = BinaryResolver.GetBinaryPath("ffplay");

        ErrorDataReceived += (_, args) => _errorLog.AppendLine(args.Data);

        StartInfo.FileName = FfplayPath;
        StartInfo.Arguments = arguments;
        StartInfo.RedirectStandardInput = true;
        StartInfo.RedirectStandardOutput = true;
        StartInfo.RedirectStandardError = true;
        StartInfo.UseShellExecute = false;
        StartInfo.CreateNoWindow = true;
        EnableRaisingEvents = true;
    }

    public new void Start()
    {
        base.Start();
        BeginErrorReadLine();
        BeginOutputReadLine();
    }
}

public sealed class MkvExtractProcess : Process
{
    public MkvExtractProcess(string arguments)
    {
        if (string.IsNullOrEmpty(arguments))
            throw new ArgumentNullException(nameof(arguments));

        StartInfo.FileName = BinaryResolver.GetBinaryPath("mkvextract");
        StartInfo.Arguments = arguments;
        StartInfo.RedirectStandardInput = true;
        StartInfo.RedirectStandardOutput = true;
        StartInfo.RedirectStandardError = true;
        StartInfo.UseShellExecute = false;
        StartInfo.CreateNoWindow = true;
        EnableRaisingEvents = true;
    }

    public new void Start()
    {
        base.Start();
        BeginErrorReadLine();
        BeginOutputReadLine();
    }
}

public sealed class YoutubeDlProcess : Process
{
    public YoutubeDlProcess(string arguments, string? executable = null)
    {
        StartInfo.FileName = executable ?? VideoDownload.ExecutableName;
        StartInfo.Arguments = arguments;
        StartInfo.RedirectStandardInput = true;
        StartInfo.RedirectStandardOutput = true;
        StartInfo.RedirectStandardError = true;
        StartInfo.UseShellExecute = false;
        StartInfo.CreateNoWindow = true;
        EnableRaisingEvents = true;
    }

    public new void Start() => Start(outputReadLine: true);

    public void Start(bool outputReadLine)
    {
        base.Start();
        BeginErrorReadLine();
        if (outputReadLine)
            BeginOutputReadLine();
    }
}
