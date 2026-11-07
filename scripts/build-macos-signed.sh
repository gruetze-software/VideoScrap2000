#!/usr/bin/env bash
# CI entry point: keep the certificate and notary key in a disposable keychain.
set +x
set -euo pipefail
[[ "$(uname -s)" == Darwin ]] || { echo 'Signing requires macOS.' >&2; exit 1; }
ROOT="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")/.." && pwd)"
for name in MACOS_CERTIFICATE_P12_BASE64 MACOS_CERTIFICATE_PASSWORD APPLE_API_KEY_P8 APPLE_API_KEY_ID APPLE_API_ISSUER_ID RUNNER_TEMP; do
  [[ -n "${!name:-}" ]] || { echo "Missing signing setting: $name" >&2; exit 1; }
done
umask 077
ORIGINAL_KEYCHAINS=()
while IFS= read -r keychain; do
  keychain="${keychain#*\"}"
  keychain="${keychain%\"*}"
  [[ -z "$keychain" ]] || ORIGINAL_KEYCHAINS+=("$keychain")
done < <(security list-keychains -d user)
WORK="$(mktemp -d "$RUNNER_TEMP/videoscrap-signing.XXXXXX")"
export MACOS_SIGNING_KEYCHAIN="$WORK/signing.keychain-db"
cleanup() {
  security list-keychains -d user -s "${ORIGINAL_KEYCHAINS[@]}" >/dev/null 2>&1 || true
  security delete-keychain "$MACOS_SIGNING_KEYCHAIN" >/dev/null 2>&1 || true
  rm -rf -- "$WORK"
}
trap cleanup EXIT
trap 'exit 130' INT
trap 'exit 143' TERM
printf '%s' "$MACOS_CERTIFICATE_P12_BASE64" | base64 --decode > "$WORK/certificate.p12"
export MACOS_NOTARY_KEY_PATH="$WORK/AuthKey.p8"
printf '%s\n' "$APPLE_API_KEY_P8" > "$MACOS_NOTARY_KEY_PATH"
KEYCHAIN_PASSWORD="$(openssl rand -hex 32)"
security create-keychain -p "$KEYCHAIN_PASSWORD" "$MACOS_SIGNING_KEYCHAIN"
security set-keychain-settings -lut 21600 "$MACOS_SIGNING_KEYCHAIN"
security unlock-keychain -p "$KEYCHAIN_PASSWORD" "$MACOS_SIGNING_KEYCHAIN"
security list-keychains -d user -s "$MACOS_SIGNING_KEYCHAIN" "${ORIGINAL_KEYCHAINS[@]}"
security import "$WORK/certificate.p12" -P "$MACOS_CERTIFICATE_PASSWORD" \
  -t cert -f pkcs12 -k "$MACOS_SIGNING_KEYCHAIN" -T /usr/bin/codesign
security set-key-partition-list -S apple-tool:,apple:,codesign: -s \
  -k "$KEYCHAIN_PASSWORD" "$MACOS_SIGNING_KEYCHAIN" >/dev/null
MACOS_SIGNING_IDENTITY="$(security find-identity -v -p codesigning "$MACOS_SIGNING_KEYCHAIN" |
  awk '/"Developer ID Application:/{print $2}')"
[[ "$MACOS_SIGNING_IDENTITY" =~ ^[[:xdigit:]]{40}$ ]] || {
  echo 'Expected exactly one valid Developer ID Application identity in the P12.' >&2; exit 1;
}
export MACOS_SIGNING_IDENTITY MACOS_SIGNING_MODE=developer-id
unset MACOS_CERTIFICATE_P12_BASE64 MACOS_CERTIFICATE_PASSWORD APPLE_API_KEY_P8 KEYCHAIN_PASSWORD
bash "$ROOT/scripts/build-macos.sh" "$@"
