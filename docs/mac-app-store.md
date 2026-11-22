# Mac App Store submission

VideoScrap 2000 1.5 includes a separate App Store packaging script. The signed and notarized DMGs on GitHub provide direct installation. App Store Connect uses a Store-signed installer package.

## Prepare your Apple account

1. Sign in to Xcode on your Mac with your Apple Developer Program account.
2. In Certificates, Identifiers & Profiles, register the explicit macOS bundle ID **de.gruetze-software.VideoScrap2000**.
3. Create an **Apple Distribution** or **Mac App Distribution** application certificate, plus a **Mac Installer Distribution** certificate. Generate the certificate requests on that Mac so their private keys remain in its keychain.
4. Create a **Mac App Store Connect** distribution profile for that bundle ID and application certificate, and download the .provisionprofile file.
5. In App Store Connect, create a macOS app record with that bundle ID and version **1.5.0**.

See Apple's instructions for [certificates](https://developer.apple.com/help/account/certificates/certificates-overview), [profiles](https://developer.apple.com/help/account/provisioning-profiles/create-an-app-store-provisioning-profile) and [Store packaging](https://developer.apple.com/documentation/xcode/packaging-mac-software-for-distribution).

## Build the upload package

Download the self-contained macOS ZIP matching your architecture from release 1.5.0. Verify its SHA-256 checksum. Obtain this repository at tag v1.5.0 on the same Mac.

Confirm the exact application identity with **security find-identity -v -p codesigning**, and the installer identity in Keychain Access. Substitute the real names and profile path below.

    export MAC_APP_STORE_APPLICATION_IDENTITY='Apple Distribution: YOUR NAME (TEAMID)'
    export MAC_APP_STORE_INSTALLER_IDENTITY='3rd Party Mac Developer Installer: YOUR NAME (TEAMID)'
    bash scripts/package-mac-app-store.sh +      "$HOME/Downloads/VideoScrap2000-1.5.0-osx-arm64.zip" +      "$HOME/Downloads/VideoScrap2000.provisionprofile" +      "$HOME/Downloads/VideoScrap2000-1.5.0-arm64-store.pkg"

The script checks the profile's bundle ID and expiry, embeds the profile, enables App Sandbox, signs native code and the app, and generates the signed .pkg. Signing identities must have their private keys in your Mac's keychain.

The Store variant enables outgoing network access and read/write access to folders chosen through the system picker. Security-scoped bookmarks preserve access to configured library folders after restart. GitHub update checks are hidden.

## Test and submit

1. Use Apple's **Transporter** app to upload the generated .pkg to App Store Connect.
2. Wait for processing, resolve any validation messages, then distribute through **TestFlight**.
3. Select movie and TV folders in Settings. Test reading and saving NFOs, artwork, actor portraits, technical media data and access to network shares. Restart the app and verify access to the same folders. Test opening artwork and videos in another app.
4. Complete screenshots, description, support URL, privacy policy, App Privacy, content rights and age ratings using actual app behaviour.
5. Explain in review notes that users select their own media folders and that the app writes metadata and images beside those media files. Settings includes a bundled demo.
6. Select the tested build and submit it for review.

Store signing, App Store validation and macOS sandbox behaviour still require testing with your new certificates. The existing DMG tests cover direct distribution. Keep the Store package for submission.

For another upload of the same marketing version, increment CFBundleVersion before signing. Each upload needs a new build number. The script preserves the supplied ZIP's architecture. Start with Apple Silicon if that is your test Mac.
