# Native Wayland on Linux

The source on `main` prefers Avalonia 12.1.4's native Wayland backend. With a usable Wayland compositor it connects directly, without XWayland. X11 remains a fallback for sessions without a compatible compositor, including existing Mint X11 desktops. Windows and macOS keep their native backends. This source change does not replace the already published 1.0.0 packages.

Both display backends use application ID `de.gruetze_software.VideoScrap2000`, matching the Linux desktop entry and icon. DEB packages declare Wayland/EGL dependencies. Snap uses the GNOME content extension and Wayland interface. Flatpak's Wayland plus fallback-X11 sockets now match the application's display capabilities; its human-authored manifest remains unchanged.

For diagnostics, choose a backend explicitly before starting the app:

```sh
VIDEOSCRAP2000_LINUX_BACKEND=wayland videoscrap2000
VIDEOSCRAP2000_LINUX_BACKEND=x11 videoscrap2000
```

The default is `auto`. Explicit `wayland` intentionally reports initialization failure rather than silently using X11.

## Display checks

The `linux-display` CI job runs real windows under a disposable Weston compositor and Xvfb. It checks forced/automatic native Wayland with `DISPLAY` removed, explicit X11, and X11 fallback with a missing Wayland display. Each case opens the main window and modal Settings, Info and metadata-search dialogs and verifies their actual platform implementation. It uses temporary XDG directories and does not scan media or load personal settings. Logs are uploaded as `VideoScrap2000-linux-display-logs`.

Run locally on a Linux test machine with .NET 10, Weston, Xvfb, Mesa/EGL and Wayland libraries installed:

```sh
bash scripts/test-linux-backends.sh
```

The compositor must provide the Wayland protocols required by Avalonia (including `xdg_wm_base` v3). Older compositors can use the X11 fallback. Real desktop testing of input, clipboard, scaling, portals, file access and window restoration is still needed; compositor-controlled positioning can differ from X11.

Wayland is referenced only for Linux or portable development builds. Windows/macOS publishes exclude it, and package checks reject foreign backends and debug symbols. Refresh the automatically generated Flatpak NuGet sources after dependency changes.
