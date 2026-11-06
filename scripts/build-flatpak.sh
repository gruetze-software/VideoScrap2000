#!/usr/bin/env bash
# Local upstream build helper; does not generate or modify a submission manifest.
set -euo pipefail
root=$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")/.." && pwd)
cd "$root"
manifest=${1:-packaging/flatpak/de.gruetze_software.VideoScrap2000.yml}
if [[ $manifest == --help || $manifest == -h ]]; then
    echo 'Usage: bash scripts/build-flatpak.sh [human-authored-manifest.yml]'
    exit 0
fi
[[ $(uname -s) == Linux ]] || { echo 'Build this package on Linux.' >&2; exit 1; }
for tool in flatpak flatpak-builder desktop-file-validate appstreamcli; do
    command -v "$tool" >/dev/null || { echo "Missing build tool: $tool" >&2; exit 1; }
done
test -f "$manifest"
app_id=de.gruetze_software.VideoScrap2000
desktop-file-validate "branding/linux/$app_id.desktop"
appstreamcli validate --no-net "branding/linux/$app_id.metainfo.xml"
flatpak --user remote-add --if-not-exists flathub https://flathub.org/repo/flathub.flatpakrepo
mkdir -p artifacts
flatpak-builder --user --default-branch=stable --install-deps-from=flathub \
    --force-clean --repo=artifacts/flatpak-repo artifacts/flatpak-build "$manifest"
# A successful managed startup does not exercise MediaInfo. Check its native
# dependencies separately inside the actual build sandbox before exporting.
flatpak-builder --run artifacts/flatpak-build "$manifest" sh -ec '
    export LD_LIBRARY_PATH="/app/lib/videoscrap2000:/app/lib${LD_LIBRARY_PATH:+:$LD_LIBRARY_PATH}"
    test -f /app/lib/videoscrap2000/Avalonia.Wayland.dll
    dependencies=$(ldd /app/lib/videoscrap2000/libmediainfo.so)
    printf "%s\n" "$dependencies"
    if printf "%s\n" "$dependencies" | grep -q "not found"; then
        echo "Native MediaInfo dependencies are missing; fix the packaging inputs before exporting." >&2
        exit 1
    fi
'
flatpak build-bundle artifacts/flatpak-repo "artifacts/$app_id.flatpak" "$app_id" stable
flatpak --user install --noninteractive -y "artifacts/$app_id.flatpak"
flatpak run "$app_id" --startup-check
echo "Package: $root/artifacts/$app_id.flatpak"
echo "Start: flatpak run $app_id"
