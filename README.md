WebM Converter
==============
A GUI wrapper around FFmpeg designed for converting and editing videos for WebM without needing command-line tools.

### Features
* **Modern Codecs**: Supports **AV1** (`libsvtav1`) and **VP9** (`libvpx-vp9`) with high-fidelity **Opus** audio, with fallback to legacy VP8/Vorbis.
* **Standalone & Portable**: Powered by native FFmpeg filtergraphs for cropping, resizing, trimming, rotating, captions, deinterlacing, and subtitles. No external AviSynth installation required for standard conversions.
* **Optional AviSynth+ Support**: Power users can still load `.avs` scripts and write custom AviSynth expressions when AviSynth+ is installed.
* **Modern Windows Integration**:
  * Built on **.NET 8** for high performance and 64-bit architecture.
  * **Per-Monitor V2 DPI Awareness** for sharp scaling on modern 1440p and 4K displays.
  * Long path support (>260 characters).
* **Video Downloader**: Built-in support for downloading online videos via **yt-dlp** (with youtube-dl fallback).
* **Automated Updates**: Seamless updates directly from GitHub Releases.

### Requirements
* Windows 10 or Windows 11 (64-bit recommended)
* [.NET 8 Desktop Runtime](https://dotnet.microsoft.com/download/dotnet/8.0)
* *(Optional)* [AviSynth+](https://github.com/AviSynth/AviSynthPlus/releases) only if you work with `.avs` script files.

### License
Released under the MIT License. See [LICENSE.md](LICENSE.md).

Issues and pull requests can be opened at [https://github.com/ajejfiejof/WebMConverter](https://github.com/ajejfiejof/WebMConverter).
