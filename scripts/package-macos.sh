#!/usr/bin/env bash
# Wrap the already checked, architecture-specific publish output in an app.
set -euo pipefail
[[ $# == 5 ]] || { echo 'Usage: package-macos.sh PUBLISH_DIR RID VERSION PACKAGE_BASE SELF_CONTAINED' >&2; exit 2; }
[[ $(uname -s) == Darwin ]] || { echo 'App packaging requires macOS.' >&2; exit 1; }
PUBLISH=$1; RID=$2; VERSION=$3; BASE=$4; SELF_CONTAINED=$5
ROOT=$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")/.." && pwd)
case "$RID" in osx-x64|osx-arm64) ;; *) exit 2 ;; esac
[[ "$VERSION" =~ ^[0-9]+\.[0-9]+\.[0-9]+$ ]] || exit 2
MODE=${MACOS_SIGNING_MODE:-adhoc}
retry_assessment() {
  local attempt
  for attempt in 1 2 3; do
    if "$@"; then return 0; fi
    [[ $attempt == 3 ]] || sleep 10
  done
  return 1
}
case "$MODE" in
  adhoc) SIGN=(--force --sign -) ;;
  developer-id)
    for name in MACOS_SIGNING_IDENTITY MACOS_SIGNING_KEYCHAIN MACOS_NOTARY_KEY_PATH APPLE_API_KEY_ID APPLE_API_ISSUER_ID; do
      [[ -n ${!name:-} ]] || { echo "Missing signing setting: $name" >&2; exit 1; }
    done
    SIGN=(--force --sign "$MACOS_SIGNING_IDENTITY" --keychain "$MACOS_SIGNING_KEYCHAIN" --timestamp --options runtime)
    ;;
  *) echo 'Invalid signing mode' >&2; exit 2 ;;
esac
WORK=$(mktemp -d "$(dirname -- "$BASE")/macos-bundle.XXXXXX")
trap 'rm -rf -- "$WORK"' EXIT
APP="$WORK/VideoScrap 2000.app"
mkdir -p "$APP/Contents/MacOS" "$APP/Contents/Resources"
ditto "$PUBLISH" "$APP/Contents/MacOS"
for item in LICENSE THIRD-PARTY-NOTICES.md licenses; do
  mv "$APP/Contents/MacOS/$item" "$APP/Contents/Resources/$item"
done
ICONSET="$WORK/videoscrap.iconset"
mkdir "$ICONSET"
for size in 16 32 128 256 512; do
  sips -z "$size" "$size" "$ROOT/branding/macos/videoscrap-2000.png" --out "$ICONSET/icon_${size}x${size}.png" >/dev/null
  doubled=$((size * 2))
  sips -z "$doubled" "$doubled" "$ROOT/branding/macos/videoscrap-2000.png" --out "$ICONSET/icon_${size}x${size}@2x.png" >/dev/null
done
iconutil -c icns "$ICONSET" -o "$APP/Contents/Resources/videoscrap-2000.icns"
cat > "$APP/Contents/Info.plist" <<PLIST
<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0"><dict>
<key>CFBundleName</key><string>VideoScrap 2000</string>
<key>CFBundleDisplayName</key><string>VideoScrap 2000</string>
<key>CFBundleExecutable</key><string>VideoScrap2000</string>
<key>CFBundleIdentifier</key><string>de.gruetze-software.VideoScrap2000</string>
<key>CFBundlePackageType</key><string>APPL</string>
<key>CFBundleShortVersionString</key><string>$VERSION</string>
<key>CFBundleVersion</key><string>$VERSION</string>
<key>CFBundleIconFile</key><string>videoscrap-2000.icns</string>
<key>LSApplicationCategoryType</key><string>public.app-category.video</string>
<key>CFBundleDevelopmentRegion</key><string>en</string>
<key>CFBundleLocalizations</key><array><string>en</string><string>de</string></array>
<key>NSHighResolutionCapable</key><true/>
<key>LSMinimumSystemVersion</key><string>14.0</string>
<key>NSPrincipalClass</key><string>NSApplication</string>
<key>NSHumanReadableCopyright</key><string>© 2026 Grütze-Software</string>
</dict></plist>
PLIST
chmod 755 "$APP/Contents/MacOS/VideoScrap2000"
chmod -R a+rX "$APP"
while IFS= read -r -d '' binary; do
  [[ "$binary" == "$APP/Contents/MacOS/VideoScrap2000" ]] && continue
  codesign "${SIGN[@]}" "$binary"
done < <(find "$APP/Contents/MacOS" -type f -print0)
if [[ $MODE == developer-id ]]; then
  codesign "${SIGN[@]}" --entitlements "$ROOT/branding/macos/entitlements.plist" "$APP"
else
  codesign "${SIGN[@]}" "$APP"
fi
codesign --verify --deep --strict "$APP"
plutil -lint "$APP/Contents/Info.plist"
if [[ ( $RID == osx-arm64 && $(uname -m) == arm64 ) || ( $RID == osx-x64 && $(uname -m) == x86_64 ) ]]; then
  "$APP/Contents/MacOS/VideoScrap2000" --startup-check
fi
if [[ $MODE == developer-id ]]; then
  ditto -c -k --sequesterRsrc --keepParent "$APP" "$WORK/notarization.zip"
  bash "$ROOT/scripts/notarize-macos.sh" "$WORK/notarization.zip" "$APP"
  retry_assessment spctl --assess --type execute "$APP"
fi
ditto -c -k --sequesterRsrc --keepParent "$APP" "$BASE.zip"
# Verify the distributed archive preserves the stapled ticket and signatures.
ditto -x -k "$BASE.zip" "$WORK/extracted"
codesign --verify --deep --strict "$WORK/extracted/VideoScrap 2000.app"
if [[ $MODE == developer-id ]]; then retry_assessment xcrun stapler validate "$WORK/extracted/VideoScrap 2000.app"; fi
if [[ $SELF_CONTAINED == true ]]; then
  mkdir "$WORK/dmg"
  chmod 755 "$WORK/dmg"
  ditto "$APP" "$WORK/dmg/VideoScrap 2000.app"
  ln -s /Applications "$WORK/dmg/Applications"
  hdiutil create -volname "VideoScrap 2000 $VERSION" -srcfolder "$WORK/dmg" -ov -format UDZO "$BASE.dmg"
  if [[ $MODE == developer-id ]]; then
    codesign --force --sign "$MACOS_SIGNING_IDENTITY" --keychain "$MACOS_SIGNING_KEYCHAIN" --timestamp "$BASE.dmg"
    bash "$ROOT/scripts/notarize-macos.sh" "$BASE.dmg" "$BASE.dmg"
    retry_assessment spctl --assess --type open --context context:primary-signature "$BASE.dmg"
  fi
fi
python3 - "$APP" "$BASE.bundle-info.json" <<'PY'
import json, os, pathlib, sys
files = [path for path in pathlib.Path(sys.argv[1]).rglob('*') if path.is_file()]
pathlib.Path(sys.argv[2]).write_text(json.dumps({'files':len(files), 'installedBytes':sum(os.path.getsize(path) for path in files)}))
PY
