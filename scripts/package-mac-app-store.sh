#!/usr/bin/env bash
set -euo pipefail
[[ $(uname -s) == Darwin && $# == 3 ]] || {
  echo 'Usage on macOS: package-mac-app-store.sh APP_ZIP PROFILE OUTPUT.pkg' >&2; exit 2;
}
for name in MAC_APP_STORE_APPLICATION_IDENTITY MAC_APP_STORE_INSTALLER_IDENTITY; do
  [[ -n ${!name:-} ]] || { echo "Missing setting: $name" >&2; exit 1; }
done
ROOT=$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")/.." && pwd)
ARCHIVE=$(cd -- "$(dirname -- "$1")" && pwd)/$(basename -- "$1")
PROFILE=$(cd -- "$(dirname -- "$2")" && pwd)/$(basename -- "$2")
OUTPUT=$(cd -- "$(dirname -- "$3")" && pwd)/$(basename -- "$3")
WORK=$(mktemp -d "${TMPDIR:-/tmp}/videoscrap-store.XXXXXX")
trap 'rm -rf -- "$WORK"' EXIT
ditto -x -k "$ARCHIVE" "$WORK"
APP="$WORK/VideoScrap 2000.app"
[[ -f "$APP/Contents/MacOS/libcoreclr.dylib" ]] || {
  echo 'A self-contained macOS ZIP with bundled .NET is required.' >&2; exit 1;
}
security cms -D -i "$PROFILE" > "$WORK/profile.plist"
python3 - "$APP" "$WORK/profile.plist" "$ROOT/branding/macos/store-entitlements.plist" "$WORK/entitlements.plist" <<'PY'
import datetime, pathlib, plistlib, sys
app, profile_path, template, output = map(pathlib.Path, sys.argv[1:])
info = plistlib.loads((app / 'Contents/Info.plist').read_bytes())
profile = plistlib.loads(profile_path.read_bytes())
entitlements = plistlib.loads(template.read_bytes())
granted = profile['Entitlements']
identifier = granted.get('com.apple.application-identifier', granted.get('application-identifier', ''))
team = granted.get('com.apple.developer.team-identifier', '')
if not team or identifier != f"{team}.{info['CFBundleIdentifier']}":
    raise SystemExit('Profile must match the explicit VideoScrap 2000 bundle ID.')
if profile['ExpirationDate'] <= datetime.datetime.now(datetime.timezone.utc).replace(tzinfo=None):
    raise SystemExit('Provisioning profile has expired.')
if granted.get('get-task-allow', False):
    raise SystemExit('Use a Mac App Store distribution profile.')
for key in ('com.apple.application-identifier', 'application-identifier', 'com.apple.developer.team-identifier'):
    if key in granted:
        entitlements[key] = granted[key]
output.write_bytes(plistlib.dumps(entitlements))
PY
ditto "$PROFILE" "$APP/Contents/embedded.provisionprofile"
printf 'Mac App Store\n' > "$APP/Contents/Resources/mac-app-store"
while IFS= read -r -d '' binary; do
  [[ "$binary" == "$APP/Contents/MacOS/VideoScrap2000" ]] && continue
  if file -b "$binary" | grep -q 'Mach-O'; then
    codesign --force --timestamp --options runtime --sign "$MAC_APP_STORE_APPLICATION_IDENTITY" "$binary"
  fi
done < <(find "$APP/Contents/MacOS" -type f -print0)
codesign --force --timestamp --options runtime --sign "$MAC_APP_STORE_APPLICATION_IDENTITY" --entitlements "$WORK/entitlements.plist" "$APP"
codesign --verify --deep --strict "$APP"
productbuild --component "$APP" /Applications --sign "$MAC_APP_STORE_INSTALLER_IDENTITY" "$OUTPUT"
pkgutil --check-signature "$OUTPUT"
echo "App Store Connect package: $OUTPUT"
