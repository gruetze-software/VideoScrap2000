# Build and test on Linux

## Prebuilt release inputs (1.5.0 and later)

Linux release archives now include `VideoScrap2000/linux-integration/` with the
desktop file, AppStream metadata, application icon and binary installer. License
files and third-party notices are already included with the application. README
screenshots and branding source files are excluded.

For the existing 1.5.0 release, the separately named
`VideoScrap2000-1.5.0-linux-{x64,arm64}-flatpak.tar.gz` assets add these installation
files to the original, unchanged application binaries. Their checksums are listed
in `SHA256SUMS-flatpak.txt`. The original release archives and tag stay unchanged.
Later Linux archives include the integration files directly.

In the publisher's independently maintained binary manifest, keep the native
libzen/libmediainfo modules and select the appropriate archive for each CPU.
Keep `strip-components: 0`, remove the extra application Git source, and replace
the application installation commands with this single command:

```sh
bash VideoScrap2000/linux-integration/install.sh
```

The installer uses the native libraries built into `/app/lib`, creates relative
app-local links for .NET, installs desktop integration and licenses, and checks
MediaInfo's dependencies. Its launcher does not override `LD_LIBRARY_PATH`.

The `Package and test Flatpak binary inputs` workflow repackages the two 1.5.0
architectures, verifies that original files are unchanged, and builds the native
modules from the publisher's manifest. It tests installation, MediaInfo reading
a real audio sample and application startup inside the matching Flatpak SDK.
After updating the manifest, also perform a complete `flatpak-builder` build and
test folder access, metadata and artwork in a graphical session.

## Earlier source-build preparation

The upstream installer, desktop integration, icon, AppStream metadata and offline NuGet sources are included. Native Wayland and X11 fallback have passed window/dialog checks. A complete Flatpak build still depends on the publisher's independently maintained manifest and native dependency inputs.

## Prepare the human-authored manifest

Review `packaging/flatpak/de.gruetze_software.VideoScrap2000.yml` before building:

- Select a source commit containing the updated native-library installer. Commit `344f937db3adb1eedd66aad1703dfcc7732b87b4` includes it; the manifest's previous `e0815db` source still uses the distribution-specific NuGet binaries. The existing `v1.0.0` tag predates this preparation.
- Build libzen and libmediainfo against the selected Flatpak runtime before the application module, as in the human-maintained manifest. The updated installer replaces the distribution-specific NuGet native binaries with relative links to these libraries under `/app/lib`, checks their dependencies, and stops if a library is missing. This avoids the bundled binary's dependency on `libcurl-gnutls.so.4` and `libmms.so.0`. Installing libraries on the Linux host does not supply them inside the sandbox.
- Keep the generated `nuget-sources.json` beside the manifest. It includes Wayland packages and both supported Linux architectures. Regenerate it with the existing official-generator workflow when changing managed dependencies.
- Check access to the selected media directories through the folder picker. Broad host filesystem access should not be added merely to make a test pass.
- Source builds do not inherit GitHub's Fanart.tv project-key secret. Decide how the publisher supplies this feature before distribution; never place a private credential in public packaging files.

Flathub requires independently human-authored manifests and submission messages, and disclosure of AI-assisted upstream material. See the [current requirements](https://docs.flathub.org/docs/for-app-authors/requirements#generative-ai-policy). The helper below neither edits the manifest nor submits anything to Flathub.

## Linux Mint / Ubuntu

Install the build tools once:

```sh
sudo apt update
sudo apt install git flatpak flatpak-builder desktop-file-utils appstream
```

Clone the source, or update an existing checkout with `git pull --ff-only`:

```sh
git clone https://github.com/gruetze-software/VideoScrap2000.git
cd VideoScrap2000
bash scripts/build-flatpak.sh
```

Run the build from a graphical Linux session. The helper adds the user Flathub remote, installs the SDK/runtime specified by the manifest, builds the app, checks MediaInfo dependencies inside the sandbox, exports the bundle, installs it for the current user and checks startup. It stops on failure. The current unresolved manifest inputs listed above must be addressed first. No separately installed host .NET runtime is required; the SDK extension supplies the build tools and the package carries its .NET runtime.

Result:

```text
artifacts/de.gruetze_software.VideoScrap2000.flatpak
```

Start it with:

```sh
flatpak run de.gruetze_software.VideoScrap2000
```

For another test machine:

```sh
flatpak --user remote-add --if-not-exists flathub https://flathub.org/repo/flathub.flatpakrepo
flatpak --user install ./de.gruetze_software.VideoScrap2000.flatpak
```

Test movies and TV shows, folder selection including mounted NAS folders, technical media details, metadata/artwork downloads, writing NFO/artwork, clipboard, opening external files, language and settings after restart. Test both Wayland and X11 sessions where available. A startup check alone does not establish these features work inside confinement.

The first installation also downloads shared Flatpak runtime components. They are shared with other applications and are not included in the bundle's download size. The build produces only the Linux host's CPU architecture; build separately on ARM64 for ARM64 distribution.
