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
- [x] Updated `VideoDownload.cs` for platform-specific names (yt-dlp.exe vs yt-dlp)
- [x] Updated `FFmpeg.cs`, `FFplay.cs`, `FFprobe.cs`, `mkvextract.cs` for new folder names
- [x] Removed hardcoded `.exe` from error messages
- [x] `GetBinaryPath()` handles platform extensions correctly

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

### Platform-Agnostic Binary Structure
- ✅ Renamed `Binaries/Win32` → `Binaries/x86`
- ✅ Renamed `Binaries/Win64` → `Binaries/x64`
- ✅ Updated `GetBinariesPath()` to auto-detect platform folder
- ✅ Updated all binary path calls

### Executable Extension Handling
- ✅ Updated `VideoDownload.cs` for platform-specific executable names
- ✅ Updated `FFmpeg.cs`, `FFplay.cs`, `FFprobe.cs`, `mkvextract.cs` for platform extensions
- ✅ Removed hardcoded `.exe` from error messages
- ✅ `GetBinaryPath()` handles platform extensions correctly

### Cross-Platform Runtime Checks (Previously Done)
- ✅ Removed System.Management dependency for process killing
- ✅ Added Windows platform checks for TaskbarManager initialization
- ✅ Added null-conditional operators (`?.`) to all TaskbarManager calls
- ✅ Added Windows platform checks for font loading/unloading
- ✅ Added Windows platform checks for registry access (ShareX)
- ✅ Added Windows platform checks for SendMessage calls

### Current Status
- **Build**: Succeeds with 0 errors
- **Phase 1.1-1.2**: Complete
- **Phase 1.3**: In Progress (PlatformServices abstraction - complex, may require redesign)
- **Phase 2**: BLOCKER - Windows Forms is Windows-only, requires UI framework migration
- **Phase 3-4**: Not Started

The fundamental blocker for full GNU/Linux support is the Windows Forms UI framework. Without rewriting the UI in a cross-platform framework (Avalonia, Eto.Gui, Gtk#, etc.), the application cannot run natively on Linux.