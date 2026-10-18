#!/usr/bin/env bash
# Tarkam Arena — Linux packages: AppImage, .deb and .rpm from a linux-x64 publish.
#   packaging/linux/build-linux-packages.sh <version> <publish-dir> <output-dir>
# Needs: dpkg-deb, rpmbuild (package "rpm"), and network access to fetch appimagetool.
set -euo pipefail

VERSION="${1:-0.1.0}"
PUBLISH="$(realpath "${2:-out/linux-x64}")"
OUT="$(realpath -m "${3:-out/installers}")"
HERE="$(cd "$(dirname "$0")" && pwd)"
ROOT="$(cd "$HERE/../.." && pwd)"
WORK="$(mktemp -d)"
mkdir -p "$OUT"
chmod +x "$PUBLISH/TarkamArena"

# ---------------- AppImage ----------------
APPDIR="$WORK/TarkamArena.AppDir"
mkdir -p "$APPDIR/usr/lib/tarkam-arena" "$APPDIR/usr/share/applications" "$APPDIR/usr/share/icons/hicolor/256x256/apps"
cp -a "$PUBLISH/." "$APPDIR/usr/lib/tarkam-arena/"
cp "$HERE/tarkam-arena.desktop" "$APPDIR/tarkam-arena.desktop"
cp "$HERE/tarkam-arena.desktop" "$APPDIR/usr/share/applications/"
cp "$ROOT/assets/ui/icon.png" "$APPDIR/tarkam-arena.png"
cp "$ROOT/assets/ui/icon.png" "$APPDIR/usr/share/icons/hicolor/256x256/apps/tarkam-arena.png"
cat > "$APPDIR/AppRun" <<'EOF'
#!/bin/sh
HERE="$(dirname "$(readlink -f "$0")")"
exec "$HERE/usr/lib/tarkam-arena/TarkamArena" "$@"
EOF
chmod +x "$APPDIR/AppRun"
if [ ! -x "$WORK/appimagetool" ]; then
  curl -fsSL -o "$WORK/appimagetool" https://github.com/AppImage/appimagetool/releases/download/continuous/appimagetool-x86_64.AppImage
  chmod +x "$WORK/appimagetool"
fi
ARCH=x86_64 APPIMAGE_EXTRACT_AND_RUN=1 "$WORK/appimagetool" "$APPDIR" "$OUT/TarkamArena.AppImage"

# ---------------- .deb ----------------
DEB="$WORK/deb"
mkdir -p "$DEB/DEBIAN" "$DEB/opt/tarkam-arena" "$DEB/usr/bin" "$DEB/usr/share/applications" "$DEB/usr/share/icons/hicolor/256x256/apps"
cp -a "$PUBLISH/." "$DEB/opt/tarkam-arena/"
ln -s /opt/tarkam-arena/TarkamArena "$DEB/usr/bin/tarkam-arena"
sed 's|^Exec=.*|Exec=/opt/tarkam-arena/TarkamArena|' "$HERE/tarkam-arena.desktop" > "$DEB/usr/share/applications/tarkam-arena.desktop"
cp "$ROOT/assets/ui/icon.png" "$DEB/usr/share/icons/hicolor/256x256/apps/tarkam-arena.png"
cat > "$DEB/DEBIAN/control" <<EOF
Package: tarkam-arena
Version: $VERSION
Section: games
Priority: optional
Architecture: amd64
Maintainer: Syubadubin Studios <noreply@syubadubin.studio>
Depends: libfontconfig1, libvulkan1 | mesa-vulkan-drivers, libasound2 | libasound2t64
Description: Tarkam Arena - Legendary Fighting Tournament from Indonesia
 3D fighting game made by Ariana Mischa Fadhila of Syubadubin Studios.
EOF
dpkg-deb --build --root-owner-group "$DEB" "$OUT/TarkamArena.deb"

# ---------------- .rpm ----------------
RPMTOP="$WORK/rpm"
mkdir -p "$RPMTOP"/{BUILD,RPMS,SOURCES,SPECS,SRPMS,BUILDROOT}
cat > "$RPMTOP/SPECS/tarkam-arena.spec" <<EOF
Name:           tarkam-arena
Version:        $VERSION
Release:        1
Summary:        Tarkam Arena - Legendary Fighting Tournament from Indonesia
License:        MIT
URL:            https://github.com/SyubadubinStudios/TarkamArena
AutoReqProv:    no
%global debug_package %{nil}
%global __strip /bin/true

%description
3D fighting game made by Ariana Mischa Fadhila of Syubadubin Studios.

%install
mkdir -p %{buildroot}/opt/tarkam-arena %{buildroot}/usr/bin %{buildroot}/usr/share/applications %{buildroot}/usr/share/icons/hicolor/256x256/apps
cp -a $PUBLISH/. %{buildroot}/opt/tarkam-arena/
ln -s /opt/tarkam-arena/TarkamArena %{buildroot}/usr/bin/tarkam-arena
sed 's|^Exec=.*|Exec=/opt/tarkam-arena/TarkamArena|' $HERE/tarkam-arena.desktop > %{buildroot}/usr/share/applications/tarkam-arena.desktop
cp $ROOT/assets/ui/icon.png %{buildroot}/usr/share/icons/hicolor/256x256/apps/tarkam-arena.png

%files
/opt/tarkam-arena
/usr/bin/tarkam-arena
/usr/share/applications/tarkam-arena.desktop
/usr/share/icons/hicolor/256x256/apps/tarkam-arena.png
EOF
rpmbuild --define "_topdir $RPMTOP" -bb "$RPMTOP/SPECS/tarkam-arena.spec"
cp "$RPMTOP"/RPMS/*/*.rpm "$OUT/TarkamArena.rpm"

rm -rf "$WORK"
ls -la "$OUT"
