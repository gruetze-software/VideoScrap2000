#!/usr/bin/env bash
set -euo pipefail

if [[ $# -ne 3 ]]; then
    echo "Usage: Package-Deb.sh <self-contained Linux x64 publish directory> <version> <output directory>" >&2
    exit 1
fi
publish_dir=$(realpath -e -- "$1")
version=$2
[[ $version =~ ^[0-9]+\.[0-9]+\.[0-9]+$ ]] || { echo 'Invalid version' >&2; exit 1; }
[[ -f "$publish_dir/VideoScrap2000" && -f "$publish_dir/libcoreclr.so" ]] || { echo 'A self-contained publish is required' >&2; exit 1; }
readelf -h "$publish_dir/VideoScrap2000" | grep -q 'Advanced Micro Devices X86-64' || { echo 'Linux x64 publish required' >&2; exit 1; }
repository=$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")/.." && pwd)
mkdir -p -- "$3"
output=$(realpath -e -- "$3")
stage=$(mktemp -d "$output/.deb-stage.XXXXXX")
trap 'rm -rf -- "$stage"' EXIT

mkdir -p "$stage/opt/videoscrap2000" "$stage/usr/bin" "$stage/usr/share/applications" "$stage/usr/share/pixmaps"
cp -a "$publish_dir/." "$stage/opt/videoscrap2000/"
cp "$repository/MovieScrap2000/Assets/videoscrap-2000.png" "$stage/usr/share/pixmaps/videoscrap2000.png"
cat > "$stage/usr/bin/videoscrap2000" <<'EOF'
#!/bin/sh
export LD_LIBRARY_PATH="/opt/videoscrap2000${LD_LIBRARY_PATH:+:$LD_LIBRARY_PATH}"
exec /opt/videoscrap2000/VideoScrap2000 "$@"
EOF
cat > "$stage/usr/share/applications/videoscrap2000.desktop" <<'EOF'
[Desktop Entry]
Version=1.0
Type=Application
Name=VideoScrap 2000
GenericName=Movie and TV Library Manager
GenericName[de]=Film- und Serienverwaltung
Comment=Manage metadata and artwork for your movies and TV shows
Comment[de]=Metadaten und Bilder deiner Filme und Serien verwalten
Exec=videoscrap2000
Icon=videoscrap2000
Terminal=false
Categories=AudioVideo;Video;
Keywords=Movies;TV;Kodi;Metadata;Artwork;
StartupNotify=true
EOF
desktop-file-validate "$stage/usr/share/applications/videoscrap2000.desktop"
find "$stage" -type d -exec chmod 755 {} +
find "$stage" -type f -exec chmod 644 {} +
chmod 755 "$stage/usr/bin/videoscrap2000" "$stage/opt/videoscrap2000/VideoScrap2000"
installed_size=$(du -sk "$stage" | cut -f1)
mkdir "$stage/DEBIAN"
cat > "$stage/DEBIAN/control" <<EOF
Package: videoscrap2000
Version: $version
Section: video
Priority: optional
Architecture: amd64
Maintainer: Grütze-Software
Homepage: https://github.com/gruetze-software/VideoScrap2000
Installed-Size: $installed_size
Depends: ca-certificates, libc6 (>= 2.35), libgcc-s1, libstdc++6, libgssapi-krb5-2, libicu70 | libicu72 | libicu74 | libicu76 | libicu78, libssl3 | libssl3t64, tzdata, zlib1g, libx11-6, libice6, libsm6, libfontconfig1, libfreetype6, libxext6, libxrender1, libxcursor1, libxi6, libxrandr2, libxkbcommon0, libgl1, libcurl3-gnutls | libcurl3t64-gnutls
Description: Movie and TV-show metadata and artwork manager
 Manage local movie and TV-show libraries and save Kodi-compatible NFO files
 and artwork beside your videos. Includes the .NET 10 runtime.
EOF
chmod 644 "$stage/DEBIAN/control"
package="$output/videoscrap2000_${version}_amd64.deb"
dpkg-deb --root-owner-group -Zxz -z9 --build "$stage" "$package"
(cd "$output" && sha256sum "${package##*/}" > "${package##*/}.sha256")
dpkg-deb --info "$package"
echo "Package: $package"
