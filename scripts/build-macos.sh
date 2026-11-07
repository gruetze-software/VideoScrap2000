#!/usr/bin/env bash
set -euo pipefail
ARCH=${1:-arm64}
case "$ARCH" in arm64|x64) ;; *) echo 'Expected arm64 or x64' >&2; exit 2 ;; esac
[[ $(uname -s) == Darwin ]] || { echo 'This script requires macOS.' >&2; exit 1; }
ROOT=$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")/.." && pwd)
pwsh -NoProfile -File "$ROOT/scripts/Publish.ps1" -Runtime "osx-$ARCH"
pwsh -NoProfile -File "$ROOT/scripts/Publish.ps1" -Runtime "osx-$ARCH" -FrameworkDependent
