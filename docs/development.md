# Build and release

Install the .NET 10 SDK to build from source.

Run the following commands from the repository root.

```powershell
dotnet run --project MovieScrap2000/MovieScrap2000.csproj
dotnet test VideoScrap2000.Tests/VideoScrap2000.Tests.csproj -m:1
./scripts/Publish.ps1 -Runtime win-x64
```

The desktop app lives in `MovieScrap2000`, the platform-independent core in `VideoScrap2000.Core`, and the test suite in `VideoScrap2000.Tests`. The app continues to use the existing SQLite cache in its `MovieScrap2000` application data folder.

GitHub Actions tests on Windows, Linux, and macOS. The packaging workflow builds separate x64 and ARM64 archives for each platform. Each platform has two downloads. **Self-contained** packages include .NET 10 and run after extraction. Smaller **framework-dependent** packages require the matching .NET 10 Runtime to be installed. Use `-FrameworkDependent` to build this smaller variant locally. Packages are checked for debug symbols, test components, editor files, and libraries for other operating systems. Archives use maximum compression. CI artifacts retain per-package checksums and size manifests; public releases provide one combined SHA256SUMS.txt. Universal macOS libraries are reduced to the selected CPU architecture when packaging on macOS. Linux uses X11 or XWayland. macOS packages contain an executable directory.

To prepare a release, push a version tag matching the project version, for example `v1.0.0`. Alternatively, run **Build and platform packages** from **Actions** and enable **Create or update the unpublished release draft after packaging**. After tests and package checks succeed, the workflow creates or updates the draft with twelve archives, one combined checksum file and download tables grouped by system and processor. Smaller .NET-dependent downloads and extracted sizes are in collapsible sections. Already published releases cannot be overwritten by this workflow. Review the draft before publishing. Leave the option disabled to build archives without changing a release.

For builds with Fanart.tv enabled, set the GitHub Actions repository secret `FANART_TV_PROJECT_API_KEY` to the app's project key. The packaging workflow uses it on `main` and version tags. Local release builds accept the same environment variable. For local development and debugging, set FANART_TV_PROJECT_API_KEY in the environment inherited by the app and restart your IDE. The GitHub secret is available inside GitHub Actions only. The project key is embedded in the resulting binaries and can be extracted, so use the app's project key only.


macOS builds use Intel runners for reliable availability. Both Intel and ARM64 packages are published with explicit target runtimes; universal libraries are reduced to the selected architecture.

