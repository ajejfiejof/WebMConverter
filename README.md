# WebMConverter

A cross-platform GUI wrapper around FFmpeg for converting and editing videos
for the WebM format — no command-line tools required.

![Linux](https://img.shields.io/badge/Linux-supported-brightgreen)
![Windows](https://img.shields.io/badge/Windows-supported-brightgreen)
![.NET 8](https://img.shields.io/badge/.NET-8-blue)
[![CI](https://github.com/ajejfiejof/WebMConverter/actions/workflows/ci.yml/badge.svg)](https://github.com/ajejfiejof/WebMConverter/actions/workflows/ci.yml)

Built with [Avalonia UI](https://avaloniaui.net/) (XAML + MVVM). All conversion
logic lives in the UI-free `WebMConverter.Core` library, covered by xUnit
tests.

## Features

* **Modern codecs** — VP9 (`libvpx-vp9`, recommended), AV1 (`libsvtav1`,
  experimental) and legacy VP8, with Opus/Vorbis audio; constant (bitrate) and
  variable (CRF) quality modes plus HQ two-pass.
* **Native FFmpeg filters** — trim / multi-trim, crop, resize, rotate/flip,
  reverse, deinterlace, denoise, levels, fade, rate, subtitle burn-in,
  image overlay, audio dub, and text captions (`drawtext`). No AviSynth
  installation needed for standard conversions.
* **Smart input handling** — ffprobe-based media detection, automatic slice
  count, SAR compensation, FRAPS level fix, embedded-subtitle discovery, and
  font-attachment installation via fontconfig.
* **VapourSynth support** — generate `.vpy` scripts from your filter stack and
  convert them through `vspipe` (requires a VapourSynth install).
* **Downloader** — fetch online videos with `yt-dlp` (youtube-dl fallback)
  straight into the converter.
* **Previews** — one-click `ffplay` preview with current filters applied.
* **ShareX upload** — post-conversion upload button when ShareX is installed
  (Windows).

## Install

### GNU/Linux

You need `ffmpeg`, `ffprobe` and `ffplay`:

```bash
# Debian/Ubuntu
sudo apt-get install ffmpeg
# Fedora
sudo dnf install ffmpeg
# Arch
sudo pacman -S ffmpeg
```

Optional: `yt-dlp` for URL downloads, `mkvtoolnix` (provides `mkvextract`)
for embedded-subtitle/font extraction, `vspipe` (VapourSynth) for `.vpy`
input. Then run a published build (see below), the Flatpak/AppImage in
`packaging/`, or `dotnet run` from source.

`ffmpeg` is resolved from `Binaries/linux-x64`, else from your `PATH` — a
distro install Just Works.

### Windows

Install the [.NET 8 Desktop Runtime](https://dotnet.microsoft.com/download/dotnet/8.0)
for framework-dependent builds, or use a self-contained publish. Bundled
`Binaries/x64` ffmpeg is used when present. Optional: AviSynth+ (`.avs`
scripts), VapourSynth (`vspipe` + `.vpy` scripts), ShareX (upload button).

### macOS

Same as Linux via Homebrew (`brew install ffmpeg yt-dlp`); `net8.0` build
runs unmodified. `.dmg` packaging is not provided yet — contributions welcome.

## Build & run from source

```bash
dotnet build WebMConverter.sln -c Release
dotnet test WebMConverter.Core.Tests/WebMConverter.Core.Tests.csproj -c Release
dotnet run --project WebMConverter.Avalonia -c Release
```

Self-contained publish (no runtime needed on the target):

```bash
dotnet publish WebMConverter.Avalonia -c Release -r linux-x64 --self-contained true
dotnet publish WebMConverter.Avalonia -c Release -r win-x64 --self-contained true
```

Flatpak and AppImage recipes live in [`packaging/`](packaging/):

```bash
flatpak-builder --force-clean build-dir packaging/flatpak/io.github.ajejfiejof.WebMConverter.json
./packaging/appimage/build-appimage.sh   # needs linuxdeploy
```

## Usage

1. Open a video (browse, drag-drop, or paste an `http(s)` URL to download).
2. Pick filters on the **Processing** tab — trim ranges, crop, subtitles…
3. Tune **Encoding** (CRF for quality, or bitrate + optional size limit) and
   **Advanced** (threads, HQ two-pass, extra ffmpeg args).
4. **Convert** — watch the live ffmpeg log; Play / Open folder / Upload
   afterwards. **Preview** checks filters instantly via ffplay.

Settings persist as JSON (`~/.config/WebMConverter/settings.json` on Linux).

## Project layout

```
WebMConverter.Core/          UI-free engine: probing, argument builder,
                             conversion/download/preview services, filters,
                             attachments, VapourSynth, settings
WebMConverter.Core.Tests/    xUnit suite (unit + ffmpeg-gated end-to-end)
WebMConverter.Avalonia/      Avalonia UI: views, view models, dialogs
packaging/                   .desktop, AppStream, Flatpak, AppImage, icon
.github/workflows/ci.yml     Linux+Windows build/test, CycloneDX SBOM
```

## Known limitations

* `.avs` AviSynth scripts need Windows (AviSynth + 32-bit proxy); `.vpy`
  needs a `vspipe` install anywhere.
* The built-in updater from the WinForms era is gone — update via releases
  or your package manager.
* Crop output changed vs the legacy app: the old code applied the crop
  twice; this is fixed here (see CHANGELOG).

## Contributing

See [CONTRIBUTING.md](CONTRIBUTING.md) (Conventional Commits required) and
the [Code of Conduct](CODE_OF_CONDUCT.md). Security reports and SBOM policy:
[SECURITY.md](SECURITY.md).

## License

MIT — see [LICENSE.md](LICENSE.md).
