# WebMConverter Cross-Platform Roadmap

## Goal: Enable native GNU/Linux support

**Note**: Windows Forms is Windows-only. Full Linux support requires UI framework migration (Phase 2). Phase 1 focuses on platform-agnostic foundation that can be done before UI migration.

## Phase 1: Platform-Agnostic Foundation (No UI Changes)

### 1.1 Platform-Agnostic Binary Structure
- [x] Rename `Binaries/Win32` to `Binaries/x86` or platform-neutral name
- [x] Rename `Binaries/Win64` to `Binaries/x64` or platform-neutral name
- [x] Update `GetBinariesPath()` to detect platform and select appropriate folder
- [ ] Document expected binary structure for Linux (e.g., `Binaries/linux-x86_64`)

### 1.2 Executable Extension Handling
- [x] Update `VideoDownload.cs` to use platform-appropriate executable name
- [x] Update `FFmpeg.cs`, `FFplay.cs`, `FFprobe.cs`, `mkvextract.cs` for platform extensions
- [x] Update error messages to not hardcode `.exe`
- [x] Ensure `GetBinaryPath()` handles all cases correctly

### 1.3 Abstract Platform-Specific Operations
- [ ] Create `IPlatformServices` interface for:
  - Font loading/unloading
  - Taskbar progress (no-op on Linux)
  - Registry access (no-op on Linux)
  - Native window messaging (no-op on Linux)
- [ ] Implement `WindowsPlatformServices` and `LinuxPlatformServices`
- [ ] Replace direct NativeMethods calls with service interface
- [ ] Replace direct TaskbarManager calls with service interface

### 1.4 AviSynth Abstraction
- [ ] Create `IScriptGenerator` interface
- [ ] Implement `AvisynthScriptGenerator` (Windows-only)
- [ ] Implement `VapourSynthScriptGenerator` (Linux alternative)
- [ ] Update `WriteAvisynthScript()` to use appropriate generator
- [ ] Disable AviSynth features on Linux until VapourSynth implemented

## Phase 2: Cross-Platform UI Framework Migration (BLOCKER)

### 2.1 Framework Selection
- [ ] Evaluate Avalonia UI vs Eto.Gui vs Gtk#
- [ ] Create proof-of-concept for chosen framework
- [ ] Document migration strategy

### 2.2 UI Migration
- [ ] Migrate `MainForm` to new framework
- [ ] Migrate `ConverterDialog` to new framework
- [ ] Migrate `DownloadDialog` to new framework
- [ ] Migrate all other dialogs
- [ ] Migrate custom controls and components

### 2.3 Designer Migration
- [ ] Recreate UI layouts in new framework designer
- [ ] Ensure all event handlers are connected
- [ ] Test all UI interactions

### 2.4 Project Configuration
- [ ] Change `WebMConverter.csproj` from `net8.0-windows` to `net8.0`
- [ ] Change `Updater/Updater.csproj` from `net8.0-windows` to `net8.0`
- [ ] Make `Microsoft-WindowsAPICodePack-Shell` conditional on Windows target
- [ ] Test build on both platforms

## Phase 3: Linux-Specific Features

### 3.1 Font Loading
- [ ] Implement Linux font loading using fontconfig
- [ ] Test font attachment loading from video files

### 3.2 Package Manager Integration
- [ ] Create AppImage/Flatpak/Snap packaging
- [ ] Implement update mechanism using package manager or built-in updater
- [ ] Handle dependencies (ffmpeg, ffprobe, etc.)

### 3.3 VapourSynth Support
- [ ] Implement VapourSynth script generator
- [ ] Test VapourSynth plugin loading
- [ ] Document VapourSynth setup for users

## Phase 4: Testing & Polish

### 4.1 Linux Testing
- [ ] Test on major distributions (Ubuntu, Fedora, Arch)
- [ ] Test all conversion workflows
- [ ] Test video download functionality
- [ ] Test subtitle handling

### 4.2 Documentation
- [ ] Update README with Linux installation instructions
- [ ] Document Linux-specific requirements
- [ ] Create troubleshooting guide

### 4.3 CI/CD
- [ ] Add Linux build to CI pipeline
- [ ] Add Linux testing to CI pipeline
- [ ] Set up automated package building

## Current Status: Phase 1.1 - Platform-Agnostic Binary Structure
