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
    # pip-installed scripts often carry a brittle absolute python shebang;
    # normalize it so the bundle works wherever python3 exists.
    if head -1 "$APPDIR/usr/bin/$bin" 2>/dev/null | grep -q "^#!.*python"; then
      sed -i '1s|^#!.*python.*|#!/usr/bin/env python3|' "$APPDIR/usr/bin/$bin"
    fi
    echo "bundled: $bin"
  else
    echo "warning: $bin not found on host, AppImage will rely on PATH" >&2
  fi
done

# libcoreclrtraceptprovider is .NET's optional tracing provider and the only
# file pulling in liblttng-ust (which modern distros ship as .so.1 while
# .NET 8 wants .so.0). The runtime works fine without it, so drop it before
# linuxdeploy walks the tree.
rm -f "$APPDIR/usr/bin/libcoreclrtraceptprovider.so"

linuxdeploy --appdir "$APPDIR" --desktop-file "$APPDIR/$APPID.desktop" --icon-file "$APPDIR/$APPID.png" \
  --output appimage

# linuxdeploy writes the AppImage to the current directory; relocate it.
mv -f ./*.AppImage "$OUT/" 2>/dev/null || true

echo "AppImage written to $OUT"
