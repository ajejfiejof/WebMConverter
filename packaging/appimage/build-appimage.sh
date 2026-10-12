#!/usr/bin/env bash
# Build a WebMConverter AppImage (requires network + linuxdeploy).
# Usage: ./packaging/appimage/build-appimage.sh [output-dir]
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
OUT="${1:-$ROOT/dist}"
APPID="io.github.ajejfiejof.WebMConverter"
APPDIR="$OUT/AppDir"

command -v linuxdeploy >/dev/null || {
  echo "linuxdeploy not found. Get it from https://github.com/linuxdeploy/linuxdeploy/releases" >&2
  exit 1
}
command -v dotnet >/dev/null || { echo "dotnet SDK 8 required" >&2; exit 1; }

export DOTNET_CLI_TELEMETRY_OPTOUT=1 DOTNET_NOLOGO=1

rm -rf "$APPDIR"
mkdir -p "$APPDIR/usr/bin" "$OUT"

dotnet publish "$ROOT/WebMConverter.Avalonia/WebMConverter.Avalonia.csproj" \
  -c Release -r linux-x64 --self-contained true -o "$APPDIR/usr/bin"

cp "$ROOT/packaging/$APPID.desktop" "$APPDIR/"
cp "$ROOT/packaging/icon.png" "$APPDIR/$APPID.png"

# Bundle ffmpeg/ffprobe/ffplay + yt-dlp if present on the build host.
for bin in ffmpeg ffprobe ffplay yt-dlp; do
  if command -v "$bin" >/dev/null; then
    cp "$(command -v "$bin")" "$APPDIR/usr/bin/"
    echo "bundled: $bin"
  else
    echo "warning: $bin not found on host, AppImage will rely on PATH" >&2
  fi
done

linuxdeploy --appdir "$APPDIR" --desktop-file "$APPDIR/$APPID.desktop" --icon-file "$APPDIR/$APPID.png" \
  --output appimage

echo "AppImage written to $OUT"
