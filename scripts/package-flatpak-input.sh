#!/usr/bin/env bash
# Add installation metadata to an existing release without rebuilding its app.
set -euo pipefail
root=$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")/.." && pwd)
[[ $# == 2 ]] || { echo 'Usage: package-flatpak-input.sh RELEASE.tar.gz OUTPUT.tar.gz' >&2; exit 2; }
archive=$(realpath -- "$1")
output=$(realpath -m -- "$2")
[[ $archive != "$output" ]] || { echo 'Use a separate output archive.' >&2; exit 2; }
work=$(mktemp -d)
trap 'rm -rf -- "$work"' EXIT
tar -xzf "$archive" -C "$work"
app="$work/VideoScrap2000"
test -x "$app/VideoScrap2000"
test -f "$app/LICENSE"
test -f "$app/THIRD-PARTY-NOTICES.md"
test -d "$app/licenses"
integration="$app/linux-integration"
install -d "$integration"
app_id=de.gruetze_software.VideoScrap2000
install -m644 "$root/branding/linux/$app_id.desktop" "$root/branding/linux/$app_id.metainfo.xml" "$integration/"
install -m644 "$root/MovieScrap2000/Assets/videoscrap-2000.png" "$integration/$app_id.png"
install -m644 "$root/scripts/install-flatpak-binary.sh" "$integration/install.sh"
mkdir -p -- "$(dirname -- "$output")"
tar -czf "$output" -C "$work" VideoScrap2000
sha256sum "$output"
