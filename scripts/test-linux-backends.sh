#!/usr/bin/env bash
set -euo pipefail
root=$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")/.." && pwd)
cd "$root"
logs="$root/artifacts/linux-backends"
mkdir -p "$logs"
work=$(mktemp -d)
weston_pid=
cleanup() {
    if [[ -n $weston_pid ]]; then kill "$weston_pid" 2>/dev/null || true; wait "$weston_pid" 2>/dev/null || true; fi
    rm -rf -- "$work"
}
trap cleanup EXIT
export XDG_RUNTIME_DIR="$work/runtime"
mkdir -m 700 "$XDG_RUNTIME_DIR"
export XDG_CONFIG_HOME="$work/config" XDG_DATA_HOME="$work/data" XDG_CACHE_HOME="$work/cache"
export LIBGL_ALWAYS_SOFTWARE=1
dotnet build tools/DisplaySmoke/DisplaySmoke.csproj -c Release
smoke="$root/tools/DisplaySmoke/bin/Release/net10.0/VideoScrap2000.DisplaySmoke.dll"
weston --backend=headless-backend.so --use-gl --width=1280 --height=960 --socket=videoscrap-test --idle-time=0 --no-config >"$logs/weston.log" 2>&1 &
weston_pid=$!
ready=false
for attempt in {1..600}; do
    if [[ -S "$XDG_RUNTIME_DIR/videoscrap-test" ]] && WAYLAND_DISPLAY=videoscrap-test timeout 1s weston-info >"$logs/wayland-globals.log" 2>&1; then ready=true; break; fi
    if ! kill -0 "$weston_pid" 2>/dev/null; then cat "$logs/weston.log"; exit 1; fi
    sleep 0.1
done
if [[ $ready != true ]]; then cat "$logs/weston.log"; echo 'Weston did not become ready in time' >&2; exit 1; fi
check() {
    local name=$1; shift
    if ! timeout 30s "$@" >"$logs/$name.log" 2>&1; then cat "$logs/$name.log"; return 1; fi
    grep -F 'SMOKE PASS:' "$logs/$name.log"
}
check wayland-forced env -u DISPLAY VIDEOSCRAP2000_LINUX_BACKEND=wayland WAYLAND_DISPLAY=videoscrap-test dotnet "$smoke" Wayland
check wayland-auto env -u DISPLAY VIDEOSCRAP2000_LINUX_BACKEND=auto WAYLAND_DISPLAY=videoscrap-test dotnet "$smoke" Wayland
check x11-forced xvfb-run -a env VIDEOSCRAP2000_LINUX_BACKEND=x11 WAYLAND_DISPLAY=videoscrap-test dotnet "$smoke" X11
check x11-fallback xvfb-run -a env VIDEOSCRAP2000_LINUX_BACKEND=auto WAYLAND_DISPLAY=videoscrap-missing dotnet "$smoke" X11
