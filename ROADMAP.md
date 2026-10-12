# WebMConverter Cross-Platform Roadmap

## Goal: Enable native GNU/Linux support

**Note**: Windows Forms is Windows-only. Full Linux support requires UI framework migration (Phase 2). Phase 1 focuses on platform-agnostic foundation that can be done before UI migration.

## Phase 1: Platform-Agnostic Foundation (No UI Changes)

### 1.1 Platform-Agnostic Binary Structure ✅
- [x] Renamed `Binaries/Win32` → `Binaries/x86`
- [x] Renamed `Binaries/Win64` → `Binaries/x64`
- [x] Updated `GetBinariesPath()` to auto-detect platform folder:
  - Windows: `x86`/`x64`
  - Linux: `linux-x86`/`linux-x64`
- [x] Updated all `GetBinariesPath()` calls to use auto-detection

### 1.2 Executable Extension Handling ✅
- [x] Updated `VideoDownload.cs` to use platform-appropriate executable name
- [x] Updated `FFmpeg.cs`, `FFplay.cs`, `FFprobe.cs`, `mkvextract.cs` for platform extensions
- [x] Updated error messages to not hardcode `.exe`
- [x] Ensure `GetBinaryPath()` handles all cases correctly

### 1.3 Abstract Platform-Specific Operations (In Progress)
- [ ] Create `IPlatformServices` interface for:
  - Font loading/unloading
  - Taskbar progress (no-op on Linux)
  - Registry access (no-op on Linux)
  - Native window messaging (no-op on Linux)
- [ ] Implement `WindowsPlatformServices` and `LinuxPlatformServices`
- [ ] Replace direct NativeMethods calls with service interface
- [ ] Replace direct TaskbarManager calls with service interface

### 1.4 AviSynth Abstraction (Not Started)
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

## Phase 3: Linux-Specific Features (Not Started)

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

## Phase 4: Testing & Polish (Not Started)

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

## Summary of Completed Work

### Phase 1.1 - Platform-Agnostic Binary Structure ✅
- ✅ Renamed `Binaries/Win32` → `Binaries/x86`
- ✅ Renamed `Binaries/Win64` → `Binaries/x64`
- ✅ Updated `GetBinariesPath()` to auto-detect platform folder
- ✅ Updated all `GetBinariesPath()` calls

### Phase 1.2 - Executable Extension Handling ✅
- ✅ Updated `VideoDownload.cs` for platform-specific executable names
- ✅ Updated `FFmpeg.cs`, `FFplay.cs`, `FFprobe.cs`, `mkvextract.cs` for platform extensions
- ✅ Removed hardcoded `.exe` from error messages
- ✅ `GetBinaryPath()` handles platform extensions correctly

### Phase 1.3 - Platform Abstraction Layer (In Progress)
- ✅ Created `Platform/IPlatformServices.cs` interface
- ✅ Created `Platform/WindowsPlatformServices.cs` implementation
- ✅ Created `Platform/LinuxPlatformServices.cs` implementation
- ✅ Created `Platform/PlatformServices.cs` factory
- ✅ Updated `MainForm.cs` to use `PlatformServices.Instance` instead of direct NativeMethods/TaskbarManager calls
- ✅ Updated `ShareXUpload.cs` to use `PlatformServices.Instance.GetRegistryValue()` instead of direct Registry access
- ⚠️ Dialog files (DownloadDialog.cs, ConverterDialog.cs) need updating - complex due to UI coupling

### Cross-Platform Runtime Checks (Previously Done)
- ✅ Removed System.Management dependency
- ✅ Added Windows platform checks for TaskbarManager initialization
- ✅ Added null-conditional operators (`?.`) to all TaskbarManager calls
- ✅ Added Windows platform checks for font loading/unloading
- ✅ Added Windows platform checks for registry access (ShareX)
- ✅ Added Windows platform checks for SendMessage calls

## Phase 2: Cross-Platform UI Framework Migration (BLOCKER)

### 2.1 Framework Selection
The fundamental blocker is Windows Forms. Options:
- **Avalonia UI**: Modern, cross-platform, XAML-based. Supports Windows, Linux, macOS. Has learning curve but active development.
- **Eto.Gui**: Cross-platform, wraps native UI toolkits (GTK#, WinForms, Cocoa). More native feel but potentially more complex.
- **Gtk#**: Mature Linux native UI. Windows/macOS support via third-party. Limited active development.

### 2.2 UI Migration
- Migrate `MainForm` to chosen framework
- Migrate `ConverterDialog` to chosen framework
- Migrate `DownloadDialog` to chosen framework
- Migrate all other dialogs and custom controls

### 2.3 Project Configuration
- Change `WebMConverter.csproj` from `net8.0-windows` to `net8.0`
- Change `Updater/Updater.csproj` from `net8.0-windows` to `net8.0`
- Make `Microsoft-WindowsAPICodePack-Shell` conditional on Windows target

## Phase 3: Linux-Specific Features

### 3.1 Font Loading
- Implement Linux font loading using fontconfig library
- Test font attachment loading from video files (AviSynth/VapourSynth scripts)

### 3.2 Package Manager Integration
- Create AppImage/Flatpak/Snap packaging
- Implement update mechanism using package manager or built-in updater
- Handle dependencies (ffmpeg, ffprobe, vapoursynth, etc.)

### 3.3 VapourSynth Support
- Implement VapourSynth script generator (AviSynth alternative)
- Test VapourSynth plugin loading
- Document VapourSynth setup for users

## Phase 4: Testing & Polish

### 4.1 Linux Testing
- Test on major distributions (Ubuntu 22.04+, Fedora 38+, Arch Linux)
- Test all conversion workflows
- Test video download functionality (yt-dlp)
- Test subtitle handling (VobSub, PGS, SSA/ASS)

### 4.2 Documentation
- Update README with Linux installation instructions
- Document Linux-specific requirements (ffmpeg, ffprobe, yt-dlp, optional: vapoursynth)
- Create troubleshooting guide

### 4.3 CI/CD
- Add Linux build to CI pipeline
- Add Linux testing to CI pipeline
- Set up automated package building (AppImage, Flatpak)

## Current Status

### Completed
- ✅ Phase 1.1: Platform-Agnostic Binary Structure
- ✅ Phase 1.2: Executable Extension Handling
- ✅ Phase 1.3: Platform Abstraction Layer (interface + Windows impl + Linux impl + factory)
- ✅ Updated MainForm.cs to use PlatformServices
- ✅ Updated ShareXUpload.cs to use PlatformServices
- ✅ Cross-Platform Runtime Checks (TaskbarManager, fonts, registry, SendMessage)

### In Progress
- ⚠️ Dialog files (DownloadDialog.cs, ConverterDialog.cs) need PlatformServices integration
- ⚠️ Full UI framework migration (Phase 2) is the fundamental blocker

### Not Started
- ❌ Phase 3: Linux-Specific Features
- ❌ Phase 4: Testing & Polish

## Next Steps

1. **Complete Phase 1.3**: Update DownloadDialog.cs and ConverterDialog.cs to use PlatformServices (taskbar progress, font loading, registry)
2. **Start Phase 2**: Evaluate UI frameworks (Avalonia recommended) and create proof-of-concept
3. **Plan Phase 3-4**: Linux font loading, package management, testing infrastructure

The platform abstraction layer (Phase 1.3) is now in place and working for MainForm and ShareXUpload. The remaining dialog files need similar treatment, after which the UI framework migration can proceed.