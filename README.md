WebM Converter
==============
A GUI wrapper around FFmpeg designed for converting and editing videos for WebM without needing command-line tools.

Cross-platform: runs on GNU/Linux, Windows, and macOS via Avalonia UI, with
all conversion logic in a shared `WebMConverter.Core` library.

### Features
* **Modern Codecs**: Supports **AV1** (`libsvtav1`) and **VP9** (`libvpx-vp9`) with high-fidelity **Opus** audio, with fallback to legacy VP8/Vorbis.
* **Native filtergraphs**: Cropping, resizing, trimming, rotating, deinterlacing, denoising, subtitles, overlays, fades, and more via native FFmpeg filters. No AviSynth installation required for standard conversions.
* **Video Downloader**: Built-in support for downloading online videos via **yt-dlp** (with youtube-dl fallback).
* **ShareX upload**: "Upload" button after conversion when ShareX is installed (Windows).

### Requirements
* [.NET 8 Runtime](https://dotnet.microsoft.com/download/dotnet/8.0) (only for framework-dependent builds)
* `ffmpeg`, `ffprobe`, `ffplay` — either installed on PATH or placed under `Binaries/linux-x64` (Linux), `Binaries/x64` (Windows)
* *(Optional)* `yt-dlp` for URL downloads
* *(Windows only)* AviSynth+ for `.avs` script files; GDI+ caption rendering

### Build & run
```
dotnet build WebMConverter.sln -c Release
dotnet run --project WebMConverter.Avalonia -c Release
```

Self-contained publish (no runtime needed on the target machine):
```
dotnet publish WebMConverter.Avalonia -c Release -r linux-x64 --self-contained true
dotnet publish WebMConverter.Avalonia -c Release -r win-x64 --self-contained true
```

Settings are stored as JSON (`~/.config/WebMConverter/settings.json` on Linux).

### Known limitations (ported faithfully from the WinForms version)
* `.avs` AviSynth scripts and the Caption filter require Windows.
* Crop dimensions are applied twice for the ffmpeg `-vf` chain (the probe-time
  resolution feeds the filter mapper). Kept bug-compatible with the legacy
  app; scheduled for a fix.
* No built-in auto-updater yet; update from releases or your package manager.

### License
Released under the MIT License. See [LICENSE.md](LICENSE.md).

Issues and pull requests can be opened at [https://github.com/ajejfiejof/WebMConverter](https://github.com/ajejfiejof/WebMConverter).
