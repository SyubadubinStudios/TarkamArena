#!/usr/bin/env bash
# Tarkam Arena — macOS .app bundle and .dmg from an osx-arm64 publish (run on macOS).
#   packaging/macos/build-dmg.sh <version> <publish-dir> <output-dir>
set -euo pipefail

VERSION="${1:-0.1.0}"
PUBLISH="$(cd "${2:-out/osx-arm64}" && pwd)"
OUT="${3:-out/installers}"
HERE="$(cd "$(dirname "$0")" && pwd)"
ROOT="$(cd "$HERE/../.." && pwd)"
mkdir -p "$OUT"
OUT="$(cd "$OUT" && pwd)"
WORK="$(mktemp -d)"
APP="$WORK/Tarkam Arena.app"

mkdir -p "$APP/Contents/MacOS" "$APP/Contents/Resources"
cp -a "$PUBLISH/." "$APP/Contents/MacOS/"
chmod +x "$APP/Contents/MacOS/TarkamArena"
sed "s/@VERSION@/$VERSION/g" "$HERE/Info.plist" > "$APP/Contents/Info.plist"

# Icon: build an .icns from the 256 px PNG.
ICONSET="$WORK/TarkamArena.iconset"
mkdir -p "$ICONSET"
for size in 16 32 64 128 256; do
  sips -z $size $size "$ROOT/assets/ui/icon.png" --out "$ICONSET/icon_${size}x${size}.png" >/dev/null
done
cp "$ICONSET/icon_32x32.png" "$ICONSET/icon_16x16@2x.png"
cp "$ICONSET/icon_64x64.png" "$ICONSET/icon_32x32@2x.png"
cp "$ICONSET/icon_256x256.png" "$ICONSET/icon_128x128@2x.png"
rm "$ICONSET/icon_64x64.png"
iconutil -c icns "$ICONSET" -o "$APP/Contents/Resources/TarkamArena.icns"

# Ad-hoc signature so Apple Silicon will launch the unsigned build (right-click > Open the first time).
codesign --force --deep --sign - "$APP" || true

STAGE="$WORK/dmg"
mkdir -p "$STAGE"
cp -R "$APP" "$STAGE/"
ln -s /Applications "$STAGE/Applications"
hdiutil create -volname "Tarkam Arena" -srcfolder "$STAGE" -ov -format UDZO "$OUT/TarkamArena.dmg"
rm -rf "$WORK"
ls -la "$OUT"
