#!/usr/bin/env bash
# Install a prebuilt Linux release using the caller's runtime-built libraries.
set -euo pipefail
integration=$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)
source_dir=$(dirname -- "$integration")
prefix=${1:-/app}
app_id=de.gruetze_software.VideoScrap2000
[[ $(uname -s) == Linux ]] || { echo 'A Linux environment is required.' >&2; exit 1; }
[[ $prefix == /* && $prefix != / ]] || { echo 'Expected an absolute installation prefix other than /.' >&2; exit 2; }
test -x "$source_dir/VideoScrap2000"
for library in libmediainfo.so libzen.so.0; do
    test -f "$prefix/lib/$library" || { echo "Missing runtime-built library: $prefix/lib/$library" >&2; exit 1; }
done
destination="$prefix/lib/videoscrap2000"
install -d "$destination" "$prefix/bin"
# Keep installation metadata out of the application directory.
find "$source_dir" -mindepth 1 -maxdepth 1 ! -name linux-integration \
    -exec cp -a -t "$destination" -- {} +
find "$destination" -maxdepth 1 \( -name 'libmediainfo.so*' -o -name 'libzen.so*' -o -name 'libmms.so*' \) -delete
ln -s ../libmediainfo.so "$destination/libmediainfo.so"
ln -s ../libzen.so.0 "$destination/libzen.so.0"
# Flatpak searches /app/lib for transitive dependencies; .NET also uses the
# app-local links above. Do not override the runtime's library search path.
dependencies=$(ldd "$destination/libmediainfo.so")
printf '%s\n' "$dependencies"
if grep -q 'not found' <<<"$dependencies"; then
    echo 'MediaInfo has unresolved dependencies.' >&2
    exit 1
fi
cat > "$prefix/bin/videoscrap2000" <<'EOF'
#!/bin/sh
bin=$(CDPATH= cd -- "$(dirname -- "$0")" && pwd)
prefix=$(dirname -- "$bin")
exec "$prefix/lib/videoscrap2000/VideoScrap2000" "$@"
EOF
chmod 755 "$prefix/bin/videoscrap2000"
install -Dm644 "$integration/$app_id.desktop" "$prefix/share/applications/$app_id.desktop"
install -Dm644 "$integration/$app_id.metainfo.xml" "$prefix/share/metainfo/$app_id.metainfo.xml"
install -Dm644 "$integration/$app_id.png" "$prefix/share/icons/hicolor/256x256/apps/$app_id.png"
install -Dm644 "$source_dir/LICENSE" "$prefix/share/licenses/$app_id/LICENSE"
install -Dm644 "$source_dir/THIRD-PARTY-NOTICES.md" "$prefix/share/licenses/$app_id/THIRD-PARTY-NOTICES.md"
install -d "$prefix/share/licenses/$app_id/third-party"
cp -a "$source_dir/licenses/." "$prefix/share/licenses/$app_id/third-party/"
echo "Installed prebuilt VideoScrap 2000 under $prefix"
