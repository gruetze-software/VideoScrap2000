#!/usr/bin/env bash
# Submit once, retry waiting on the same submission, then staple its ticket.
set +x
set -euo pipefail
[[ $# == 2 ]] || { echo 'Usage: notarize-macos.sh ARCHIVE APP_OR_DMG_TO_STAPLE' >&2; exit 2; }
for name in MACOS_NOTARY_KEY_PATH APPLE_API_KEY_ID APPLE_API_ISSUER_ID; do
  [[ -n "${!name:-}" ]] || { echo "Missing notarization setting: $name" >&2; exit 1; }
done
[[ -s "$MACOS_NOTARY_KEY_PATH" ]] || { echo 'Notarization key file missing.' >&2; exit 1; }
WORK="$(mktemp -d)"
trap 'rm -rf -- "$WORK"' EXIT
AUTH=(--key "$MACOS_NOTARY_KEY_PATH" --key-id "$APPLE_API_KEY_ID" --issuer "$APPLE_API_ISSUER_ID")
xcrun notarytool submit "$1" "${AUTH[@]}" --output-format json > "$WORK/submit.json"
ID="$(plutil -extract id raw -o - "$WORK/submit.json")"
[[ "$ID" =~ ^[[:xdigit:]]{8}-[[:xdigit:]]{4}-[[:xdigit:]]{4}-[[:xdigit:]]{4}-[[:xdigit:]]{12}$ ]] || {
  echo 'Apple returned no valid notarization submission ID.' >&2; exit 1;
}
echo "Apple notarization submission: $ID"
STATUS=
for attempt in 1 2; do
  xcrun notarytool wait "$ID" "${AUTH[@]}" --timeout 20m --output-format json > "$WORK/status.json" || true
  STATUS="$(plutil -extract status raw -o - "$WORK/status.json" 2>/dev/null || true)"
  case "$STATUS" in Accepted|Invalid|Rejected) break ;; esac
done
if [[ "$STATUS" != Accepted ]]; then
  echo "Notarization did not succeed: ${STATUS:-no status}. Submission: $ID" >&2
  xcrun notarytool log "$ID" "${AUTH[@]}" || true
  exit 1
fi
for attempt in 1 2 3; do
  if xcrun stapler staple "$2"; then
    xcrun stapler validate "$2"
    exit 0
  fi
  [[ "$attempt" == 3 ]] || sleep 10
done
echo 'Apple accepted the upload, but attaching the ticket failed.' >&2
exit 1
