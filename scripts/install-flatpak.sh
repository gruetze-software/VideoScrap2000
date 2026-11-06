#!/usr/bin/env bash
# Upstream application installer, not a Flathub manifest. The caller supplies
# the runtime, offline packages, native dependencies and sandbox permissions.
set -euo pipefail
root=$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")/.." && pwd)
prefix=${FLATPAK_DEST:-/app}
feed="$root/nuget-sources"
while (($#)); do
    case "$1" in
        --prefix|--nuget-source)
            [[ $# -ge 2 && -n "$2" && "$2" != --* ]] || { echo 'Missing option value' >&2; exit 2; }
            if [[ $1 == --prefix ]]; then prefix=$2; else feed=$2; fi
            shift 2 ;;
        --help|-h)
            echo 'Usage: bash scripts/install-flatpak.sh [--prefix ABSOLUTE_PATH] [--nuget-source DIRECTORY]'
            exit 0 ;;
        *) echo "Unknown option: $1" >&2; exit 2 ;;
    esac
done
[[ $(uname -s) == Linux ]] || { echo 'A Linux build environment is required' >&2; exit 1; }
[[ $prefix == /* && $prefix != / ]] || { echo 'Invalid install prefix' >&2; exit 2; }
[[ -d $feed ]] || { echo "Offline NuGet feed missing: $feed" >&2; exit 1; }
feed=$(cd -- "$feed" && pwd)
case $(uname -m) in
    x86_64) rid=linux-x64 ;;
    aarch64|arm64) rid=linux-arm64 ;;
    *) echo 'Unsupported CPU architecture' >&2; exit 1 ;;
esac
work=$(mktemp -d)
trap 'rm -rf -- "$work"' EXIT
mkdir -p "$work/publish"
cd "$root"
sources=(--source "$feed")
if [[ -d /usr/lib/sdk/dotnet10/nuget/packages ]]; then
    sources+=(--source /usr/lib/sdk/dotnet10/nuget/packages)
fi
if [[ -z ${FANART_TV_PROJECT_API_KEY:-} ]]; then
    echo 'Note: no Fanart.tv project key supplied to this source build.' >&2
fi
dotnet publish MovieScrap2000/MovieScrap2000.csproj -c Release -r "$rid" --self-contained false \
    "${sources[@]}" -p:NuGetAudit=false -p:UseAppHost=true -p:PublishTrimmed=false \
    -p:PublishSingleFile=false -p:PublishReadyToRun=false -p:UseSharedCompilation=false \
    -p:RestorePackagesPath="$work/packages" -m:1 -nr:false -o "$work/publish"
test -x "$work/publish/VideoScrap2000"
# NuGet supplies distribution-specific native binaries. Flatpak uses the
# libraries built by the preceding native modules against its own runtime.
# Keep links in the application directory for .NET's app-local native loader.
for library in libmediainfo.so libzen.so.0; do
    [[ -f "$prefix/lib/$library" ]] || {
        echo "Missing Flatpak-built library: $prefix/lib/$library. Build the native modules before the application." >&2
        exit 1
    }
done
find "$work/publish" -maxdepth 1 \( -name 'libmediainfo.so*' -o -name 'libzen.so*' \) -delete
ln -s ../libmediainfo.so "$work/publish/libmediainfo.so"
ln -s ../libzen.so.0 "$work/publish/libzen.so.0"
dependencies=$(LD_LIBRARY_PATH="$prefix/lib${LD_LIBRARY_PATH:+:$LD_LIBRARY_PATH}" ldd "$prefix/lib/libmediainfo.so")
printf '%s\n' "$dependencies"
if printf '%s\n' "$dependencies" | grep -q 'not found'; then
    echo 'Flatpak-built MediaInfo has unresolved native dependencies.' >&2
    exit 1
fi
forbidden=$(find "$work/publish" -type f \( -name '*.pdb' -o -name '*.dbg' -o -name '*.debug' \
    -o -name '*.dylib' -o -name '*.lib' -o -name '*.exp' -o -name '*.a' \
    -o -name 'Avalonia.Win32*.dll' -o -name 'Avalonia.Native.dll' -o -name 'testhost*' \) -print)
[[ -z $forbidden ]] || { echo "Unexpected package files: $forbidden" >&2; exit 1; }
app_id=de.gruetze_software.VideoScrap2000
install -d "$prefix/lib/videoscrap2000" "$prefix/bin"
cp -a "$work/publish/." "$prefix/lib/videoscrap2000/"
cat > "$prefix/bin/videoscrap2000" <<'EOF'
#!/bin/sh
bin=$(CDPATH= cd -- "$(dirname -- "$0")" && pwd)
prefix=$(dirname -- "$bin")
export LD_LIBRARY_PATH="$prefix/lib/videoscrap2000:$prefix/lib${LD_LIBRARY_PATH:+:$LD_LIBRARY_PATH}"
exec "$prefix/lib/videoscrap2000/VideoScrap2000" "$@"
EOF
chmod 755 "$prefix/bin/videoscrap2000"
install -Dm644 "branding/linux/$app_id.desktop" "$prefix/share/applications/$app_id.desktop"
install -Dm644 "branding/linux/$app_id.metainfo.xml" "$prefix/share/metainfo/$app_id.metainfo.xml"
install -Dm644 MovieScrap2000/Assets/videoscrap-2000.png "$prefix/share/icons/hicolor/256x256/apps/$app_id.png"
install -Dm644 LICENSE "$prefix/share/licenses/$app_id/LICENSE"
install -Dm644 THIRD-PARTY-NOTICES.md "$prefix/share/licenses/$app_id/THIRD-PARTY-NOTICES.md"
install -d "$prefix/share/licenses/$app_id/third-party"
cp -a licenses/. "$prefix/share/licenses/$app_id/third-party/"
echo "Installed VideoScrap 2000 under $prefix using $rid"
