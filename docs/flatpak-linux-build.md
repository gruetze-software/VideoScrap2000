# Build and test on Linux

The upstream installer, desktop integration, icon, AppStream metadata and offline NuGet sources are included. Native Wayland and X11 fallback have passed window/dialog checks. A complete Flatpak build still depends on the publisher's independently maintained manifest and native dependency inputs.

## Prepare the human-authored manifest

Review `packaging/flatpak/de.gruetze_software.VideoScrap2000.yml` before building:

- Its current Git commit `f204fb505a17cecda5f982e29af187f44d5e64b0` does not identify this application's prepared source. Select a real commit containing the Wayland changes and `scripts/install-flatpak.sh`; `f04c98c80d4f6ce01e066dbc0996b9225ad280b5` contains both. The existing `v1.0.0` tag points to an older, different revision and cannot identify this newer source.
- The shipped MediaInfo binary requires `libcurl-gnutls.so.4` and `libmms.so.0`, which were absent in the matching SDK check. Supply compatible native libraries and their dependencies, or build MediaInfo against the chosen runtime. Installing these libraries on the Linux host does not add them to the Flatpak sandbox. Do not rename an incompatible curl library to satisfy the loader.
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
