# macOS signing and notarization

The **Build signed and notarized macOS packages** workflow can run on its own and is also called by the main build/release workflow. It uses native Intel and Apple Silicon runners. Both self-contained and framework-dependent variants are real `VideoScrap 2000.app` bundles distributed in ZIP archives. Self-contained variants additionally have a drag-to-Applications DMG, the recommended download.

Required repository secrets match BingWallE:

- `MACOS_CERTIFICATE_P12_BASE64`: base64-encoded Developer ID Application certificate with its private key.
- `MACOS_CERTIFICATE_PASSWORD`: password protecting that P12.
- `APPLE_API_KEY_P8`: complete multiline App Store Connect API private key.
- `APPLE_API_KEY_ID`: API key ID.
- `APPLE_API_ISSUER_ID`: API issuer ID.

`FANART_TV_PROJECT_API_KEY` continues to enable Fanart.tv in the compiled app. Never put signing credentials into source files or artifacts.

Like BingWallE, CI imports the P12 into a temporary keychain, selects exactly one valid Developer ID Application identity, and removes the raw P12/password/P8 environment variables before invoking the build. A trap restores the original keychain search list and deletes the temporary keychain and private files, including after failures. Signing secrets are passed only on `main` and version tags. Official macOS builds fail rather than silently producing an unsigned download if credentials are missing.

Architecture thinning and package-content checks happen before signing. Nested bundle files are signed before the enclosing application. Hardened runtime is enabled with only the JIT entitlement needed by .NET. The macOS bundle ID is `de.gruetze-software.VideoScrap2000`; Linux keeps its existing underscore-based ID. The ICNS is generated from the user's 1024-pixel app icon. Only the generated ICNS is packaged, not the branding source or README screenshots.

Apple notarization is submitted once per app/DMG, with bounded waits on the same submission ID. Rejections fail the build and display Apple's diagnostic report. Acceptance must be followed by successful stapling and ticket validation. Gatekeeper assessment and verification of the extracted ZIP must also succeed. ZIP creation uses Apple's `ditto` to preserve signed bundle metadata; generic tar/ZIP creation is not used for macOS.

The workflow also exercises credential cleanup, failed imports, missing settings, rejected/timed-out notarization and failed stapling using fake credentials and Apple services. Actual signing/notarization runs separately with the repository secrets. Apple service availability can affect build duration.

This is Developer ID distribution through GitHub, not Mac App Store submission. App Sandbox and Store-specific signing/review would be a separate task. Local `Publish.ps1 -Runtime osx-x64` or `osx-arm64` builds on macOS may use ad-hoc signing; these are development artifacts and are not accepted as official release installers.
