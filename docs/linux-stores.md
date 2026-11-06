# Snap and Flatpak workflows

These are upstream build tools, not Store registration or acceptance. The project is licensed under MIT. The current application ID is `de.gruetze_software.VideoScrap2000`, based on the publisher's domain. Confirm this permanent ID and domain control before submitting to Flathub.

## Snap

Run **Build and test Snap** in GitHub Actions on `main`. It builds a self-contained AMD64 Snap with .NET 10, installs it with strict confinement, checks startup under Xvfb, and uploads the `.snap` as an Actions artifact. No Store credentials are needed for building, and the workflow does not upload to the Store.

To submit, sign in to the [Snap Store developer dashboard](https://dashboard.snapcraft.io/), register the name `videoscrap2000` if available, and complete the listing with your screenshots, support URL and MIT license. With Snapcraft installed, the publisher can upload the downloaded artifact to a testing channel:

```sh
snapcraft login
snapcraft register videoscrap2000
snapcraft upload videoscrap2000_1.0.0_amd64.snap --release=edge
```

Test the installed Snap before promoting it to stable. Check folder selection, reading and writing NFO/artwork files, network shares, external image/video opening and settings persistence. The `home` and `network` interfaces provide normal home-folder/network access; access to media mounted under `/media` or `/mnt` can require:

```sh
sudo snap connect videoscrap2000:removable-media
```

The Snap uses Canonical's GNOME content extension. Its first installation can therefore download shared platform components in addition to the app. Linux Mint disables Snap by default; consult the [official Mint Snap instructions](https://snapcraft.io/docs/installing-snap-on-linux-mint) if you choose to enable it.

## Flatpak and Flathub

Run **Build and test human-authored Flatpak manifest** after adding your independently human-authored manifest to the repository. Enter its repository-relative path in `manifest_path`. The action validates desktop/AppStream metadata, installs the dependencies specified by that manifest, builds a local repository, installs the app, checks startup and uploads a `.flatpak` bundle. The manifest must use the application ID above, command `videoscrap2000`, and branch `stable` (the workflow default).

Upstream desktop and AppStream sources are in `branding/linux/`. The application icon is in `MovieScrap2000/Assets/videoscrap-2000.png`. The manifest must install the desktop entry, correctly named icon, AppStream file and executable into their proper Flatpak locations. It must provide a maintained runtime and required .NET/native dependencies, offline NuGet inputs, and justified permissions for network access, display and the selected media folders. Validate real folder-picker portals and file permissions on Mint; a startup check alone does not prove library access works in the sandbox.

**No Flathub submission manifest has been generated here.** [Flathub's generative-AI policy](https://docs.flathub.org/docs/for-app-authors/requirements#generative-ai-policy) prohibits AI-generated or AI-assisted manifests. The human publisher must independently author the manifest and submission, including commit messages and review replies. Do not copy an AI-generated manifest into a submission or treat this preparation as Flathub approval.

The policy also requires disclosure of known AI-generated or assisted material, identifying affected parts and approximate extent. This project includes substantial AI-assisted application code, documentation, packaging/workflows and generated fictional demo artwork. The maintainer must review the actual scope and disclose it accurately; acceptance is discretionary. The current hand-drawn app icon and user-maintained screenshots have separate provenance.

Follow the [official submission process](https://docs.flathub.org/docs/for-app-authors/submission/) and run the current [Flathub lint checks](https://docs.flathub.org/docs/for-app-authors/linter) before the human-authored submission. Registration and verification remain human steps; no workflow opens a Flathub PR or sends review messages.
