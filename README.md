# VideoScrap 2000

<img src="MovieScrap2000/Assets/videoscrap-2000.png" alt="VideoScrap 2000" width="88" />

VideoScrap 2000 is a desktop library manager for movies and TV shows on Windows, Linux, and macOS. Browse your collection, find metadata and artwork, and save the results as NFO and image files beside your videos. It works with local folders and mounted network shares.

[GitHub Releases](https://github.com/gruetze-software/VideoScrap2000/releases)

## Features

- **Browse your collection.** See poster thumbnails, release years, and ratings on a five-star scale with half stars. Filter by title, year, or genre. Sort by name, release year, or the creation date of the movie's NFO file.
- **Find movies that need attention.** Films without metadata appear in green at the top of the list. If a folder is unreachable, the cached title, year, and rating remain visible. Use the context menu to remove stale entries from the local cache.
- **Match films with TMDB.** Search by title, year, or IMDb ID. For movies without NFOs, the app infers a title and year from the filename when it can. Poster thumbnails in the search dialog help distinguish similarly named films.
- **Edit metadata and cast.** Work with titles, taglines, genres, plot summaries, release dates, ratings, certifications, countries, studios, collections, and IMDb and TMDB IDs. The cast tab shows roles and available portraits. Select multiple cast entries to remove them together.
- **Find or import artwork.** Choose posters, fanart, clear logos, clear art, banners, disc art, key art, and landscape images from the available sources, or select a local file. Search for one type or use **All** to select the first result for each type. On the first TMDB match, or when the film has no artwork, the app automatically searches for missing types.
- **Choose images while previews load.** Pick a result and apply it while other previews are still downloading. Double-click a local artwork tile to open the image in your default viewer.
- **Save changes in the movie folder.** Metadata and selected artwork are staged until you choose **Save changes**. Existing NFOs and replaced artwork are backed up as `.bak` files. NFO fields managed by other tools are preserved when possible.
- **Customize the interface.** Use light, dark, or system appearance and optionally show dimmed fanart behind the movie details. The interface supports English and German. Metadata and artwork language preferences can be set separately, and preferred artwork languages rank ahead of other matches.
- **Manage TV shows separately.** The **TV shows** tab groups videos by show and season. Edit show and episode metadata separately or fetch it from TMDB. Manage the cast in its own tab with roles, TMDB IDs, and portraits. See show fanart and season banners. Matching a show also looks for missing artwork from Fanart.tv, with a TMDB backdrop as a fallback for fanart.
- **Draft a short outline in your browser.** The ChatGPT button copies the plot with a prompt for a spoiler-free outline of up to 239 characters and opens ChatGPT in your browser. You choose whether to send the prompt and paste the result back into the app.

## Get started

1. Download and extract the archive for your operating system and architecture from **GitHub Releases** when a release is available. Launch `VideoScrap2000.exe` on Windows or `VideoScrap2000` on Linux or macOS.
2. Open **Settings** and select your movie folder. Use one subfolder per movie. If a folder contains multiple supported video files, the scanner uses the largest one.
3. Choose **Scan library** to read existing NFOs and list the movies. Use **Find metadata** to match a film, then review the metadata and artwork before saving.

The local SQLite cache keeps the movie collection searchable between scans. Movie NFOs and artwork stay in their film folders. To refresh a Kodi library after editing, use Kodi's own library update.

For TV, choose a **TV folder** in Settings, with one subfolder per show and optional season folders. Open **TV shows** and choose **Scan library**. Episode names such as `Show S01E02.mkv` or `Show 1x02.mkv` supply season and episode numbers when there is no NFO. You can also add a video from within the show folder and enter its numbers manually. Show metadata goes into `tvshow.nfo`. Each episode uses an NFO with the same basename as its video. The editor handles one episode per video file.

## Metadata and artwork sources

[<img src="docs/tmdb.svg" alt="TMDB" width="104" />](https://www.themoviedb.org/)

[TMDB](https://www.themoviedb.org/) provides movie details, cast information, and artwork. [Fanart.tv](https://fanart.tv/) provides additional artwork types. Packages built with the Fanart.tv project key work without per-client setup. You can optionally set your [personal Fanart.tv key](https://fanart.tv/get-an-api-key/) in `FANART_TV_API_KEY` and restart the app.

> This product uses the TMDB API but is not endorsed or certified by TMDB.

## Build and release

Install the .NET 10 SDK to build from source.

```powershell
dotnet run --project MovieScrap2000/MovieScrap2000.csproj
dotnet test VideoScrap2000.Tests/VideoScrap2000.Tests.csproj -m:1
./scripts/Publish.ps1 -Runtime win-x64
```

The desktop app lives in `MovieScrap2000`, the platform-independent core in `VideoScrap2000.Core`, and the test suite in `VideoScrap2000.Tests`. The app continues to use the existing SQLite cache in its `MovieScrap2000` application data folder.

GitHub Actions tests on Windows, Linux, and macOS. The packaging workflow builds separate x64 and ARM64 archives for each platform and checks for debug symbols and libraries for other operating systems. Archives include the appropriate .NET runtime by default. Use `-FrameworkDependent` for a smaller local build that requires an installed .NET 10 runtime. Linux uses X11 or XWayland. macOS packages contain an executable directory.

To prepare a release, push a version tag such as `v0.2.0`. After the tests and package checks succeed, the workflow creates a GitHub release draft with all six archives. Review the draft before publishing. To build the archives without creating a release, run **Build and platform packages** manually from **Actions**.

For builds with Fanart.tv enabled, set the GitHub Actions repository secret `FANART_TV_PROJECT_API_KEY` to the app's project key. The packaging workflow uses it on `main` and version tags. Local release builds accept the same environment variable. The project key is embedded in the resulting binaries and can be extracted, so use the app's project key only.

[Icon source for Paint.NET](docs/VideoScrap-2000-icon.png) · [Third-party notices](THIRD-PARTY-NOTICES.md) · [Grütze-Software](https://github.com/gruetze-software) · [Ko-fi](https://ko-fi.com/gruetzesoftware)
