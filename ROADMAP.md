# WebMConverter Cross-Platform Roadmap

## Goal: Native GNU/Linux support via Avalonia UI — DONE (UI migration complete)

## Phase 1: Platform-Agnostic Foundation ✅ (superseded by Core where noted)

### 1.1 Platform-Agnostic Binary Structure ✅
- [x] `Binaries/Win32` → `Binaries/x86`, `Binaries/Win64` → `Binaries/x64`
- [x] `BinaryResolver` auto-detects platform folder (`x86`/`x64`, `linux-x86`/`linux-x64`) + PATH fallback

### 1.2 Executable Extension Handling ✅
- [x] `.exe` handling per-platform in `BinaryResolver`; no hardcoded extensions

### 1.3 Platform Abstraction ✅ (via Core design, not IPlatformServices)
- The `IPlatformServices` files from the old plan never landed; instead Core
  isolates platform code: `PathCompat` (8.3 paths, Windows-only),
  `ShareXClient` (registry, Windows-only), `/dev/null` vs `NUL`,
  `RuntimeInformation` guards. No abstraction theater.

### 1.4 AviSynth Abstraction (Deferred)
- [x] Decided: direct-ffmpeg path is the cross-platform default; `.avs` input
  throws a clear error off Windows
- [ ] VapourSynth script generator (future)

## Phase 2: Avalonia UI Migration ✅ COMPLETE

### 2.1 Framework Selection ✅
- [x] Selected **Avalonia UI 12** (XAML, MVVM via CommunityToolkit.Mvvm)

### 2.2 Core Library ✅
- [x] `WebMConverter.Core` (net8.0, UI-free): binary resolution, ffmpeg/ffprobe/
  ffplay/mkvextract/yt-dlp/ShareX wrappers, filter models + `-vf`/`-af` mapping,
  exact-port argument builder (verified against legacy vectors), probe service,
  single/HQ-two-pass conversion service, download service, preview service,
  JSON settings store
- [x] E2E verified on Linux with system ffmpeg (probe, single-pass, two-pass,
  trim+crop constant → valid .webm)

### 2.3 Avalonia Head ✅
- [x] Main window (Input/Output, Processing, Encoding, Advanced), native file
  pickers, drag-drop, URL downloads, progress/download dialogs, ffplay preview
- [x] Filter editors for all portable filters (caption: Windows-only, noted in UI)

### 2.4 WinForms Removal ✅
- [x] Deleted WinForms head (87 files), Updater project, superseded planning docs
- [x] Solution = `WebMConverter.Core` + `WebMConverter.Avalonia`

## Phase 3: Linux-Specific Features (Not Started)
- [ ] Linux font loading (fontconfig) for attachment fonts
- [ ] AppImage/Flatpak packaging
- [ ] VapourSynth support

## Phase 4: Testing & Polish (Partial)
- [x] Linux smoke + E2E conversion tests (this machine)
- [x] README with Linux instructions
- [ ] Multi-distro testing, automated packaging

## Known quirks carried over (bug-compatible, fix later)
- Crop dimensions feed the `-vf` chain twice (probe resolution is already
  crop-adjusted). One-line fix in `EffectiveResolution`/mapper call; needs a
  user decision since it changes output vs legacy.
