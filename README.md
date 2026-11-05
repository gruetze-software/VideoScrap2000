# VideoScrap 2000

<img src="docs/videoscrap-2000-icon-64.png" alt="VideoScrap 2000" width="64" height="64" />

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
- **Manage TV shows separately.** The **TV shows** tab groups videos by show and season. Edit show and episode metadata or fetch it from TMDB. Manage the show cast with roles, TMDB IDs, and portraits. The artwork section offers a search dialog with a choice of image types for the show or a selected season, including an **All** option that takes the first available result per type. Browse previews from TMDB and Fanart.tv, with your preferred language listed first. Selected artwork is written when you save. A responsive season banner spans the detail area above the section buttons. The season poster and the selected episode's still sit beside each other above the editable details and scroll with the form, leaving the episode list available for navigation. In **All seasons**, the images follow the selected episode. Use **Find all episodes** to fetch metadata for the local episodes of the selected show. Every **Save changes** button saves the show metadata, pending artwork, the selected episode, and any fetched episode updates together. The optional backdrop uses season fanart when available. Matching a show searches for missing artwork from Fanart.tv and uses available TMDB posters and show backdrops as fallbacks.
- **Draft a short outline with AI.** The **Generate plot outline with AI** button opens a dialog with instructions and an editable prompt for a spoiler-free outline of up to 239 characters. The prompt is copied to the clipboard automatically. Open an AI service, such as ChatGPT using the dialog's button, paste the prompt, and copy the result back into the app's Plot outline field.
- **Read technical video details automatically.** When a movie has no usable stream information in its NFO, MediaInfo reads the video file in the background. Video codec, resolution, aspect ratio, duration, audio tracks, and subtitles appear in Details and are included when you save. Click a displayed movie file path to open it in its default application.
- **Reuse previews while browsing.** Covers, artwork, cast portraits, and search previews share a bounded runtime cache. Decoded previews use up to 96 MiB of cache space, with a separate 32 MiB cache for online image downloads. Replaced artwork is invalidated when saved. External file changes are checked after 30 seconds on the next access, or when you rescan the library.

## Get started

1. Download and extract the archive for your operating system and architecture from **GitHub Releases** when a release is available. Launch `VideoScrap2000.exe` on Windows or `VideoScrap2000` on Linux or macOS.
2. Open **Settings** and select your movie folder. Use one subfolder per movie. If a folder contains multiple supported video files, the scanner uses the largest one.
3. Choose **Scan library** to read existing NFOs and list the movies. Use **Find metadata** to match a film, then review the metadata and artwork before saving.

The local SQLite cache keeps the movie collection searchable between scans. Movie NFOs and artwork stay in their film folders. To refresh a Kodi library after editing, use Kodi's own library update.

For TV, choose a **TV folder** in Settings, with one subfolder per show and optional season folders. Open **TV shows** and choose **Scan library**. Episode names such as `Show S01E02.mkv` or `Show 1x02.mkv` supply season and episode numbers when there is no NFO. You can also add a video from within the show folder and enter its numbers manually. Show metadata goes into `tvshow.nfo`. Each episode uses an NFO with the same basename as its video. The editor handles one episode per video file.

## Metadata and artwork sources

[<img src="docs/tmdb.svg" alt="TMDB" width="104" />](https://www.themoviedb.org/)

[TMDB](https://www.themoviedb.org/) provides movie details, cast information, and artwork. [Fanart.tv](https://fanart.tv/) provides additional artwork types. Packages built with the Fanart.tv project key work without per-client setup. You can optionally set your [personal Fanart.tv key](https://fanart.tv/get-an-api-key/) in `FANART_TV_API_KEY` and restart the app.

Episode updates fetch available stills from TMDB, with [TVmaze](https://www.tvmaze.com/) as a fallback for shows matched by IMDb or TheTVDB ID. TVmaze API data is provided under [CC BY-SA](https://www.tvmaze.com/api#licensing). The selected episode's still appears beside the season poster. Saving writes new stills alongside their videos as `<video basename>-thumb` image files.

> This product uses the TMDB API but is not endorsed or certified by TMDB.

## Build and release

Install the .NET 10 SDK to build from source.

```powershell
dotnet run --project MovieScrap2000/MovieScrap2000.csproj
dotnet test VideoScrap2000.Tests/VideoScrap2000.Tests.csproj -m:1
./scripts/Publish.ps1 -Runtime win-x64
```

The desktop app lives in `MovieScrap2000`, the platform-independent core in `VideoScrap2000.Core`, and the test suite in `VideoScrap2000.Tests`. The app continues to use the existing SQLite cache in its `MovieScrap2000` application data folder.

GitHub Actions tests on Windows, Linux, and macOS. The packaging workflow builds separate x64 and ARM64 archives for each platform. Each platform has two downloads. **Self-contained** packages include .NET 10 and run after extraction. Smaller **framework-dependent** packages require the matching .NET 10 Runtime to be installed. Use `-FrameworkDependent` to build this smaller variant locally. Packages are checked for debug symbols, test components, editor files, and libraries for other operating systems. Archives use maximum compression and include separate SHA-256 checksums and size manifests. Universal macOS libraries are reduced to the selected CPU architecture when packaging on macOS. Linux uses X11 or XWayland. macOS packages contain an executable directory.

To prepare a release, push the version tag `v0.3.0`. After the tests and package checks succeed, the workflow creates a GitHub release draft with all twelve archives, checksums, and manifests. The release notes list measured download and extracted sizes for every package. Review the draft before publishing. To build the archives without creating a release, run **Build and platform packages** manually from **Actions**.

For builds with Fanart.tv enabled, set the GitHub Actions repository secret `FANART_TV_PROJECT_API_KEY` to the app's project key. The packaging workflow uses it on `main` and version tags. Local release builds accept the same environment variable. For local development and debugging, set FANART_TV_PROJECT_API_KEY in the environment inherited by the app and restart your IDE. The GitHub secret is available inside GitHub Actions only. The project key is embedded in the resulting binaries and can be extracted, so use the app's project key only.

[Icon PNG](docs/VideoScrap-2000-icon.png) · [Layered Paint.NET source](docs/VideoScrap-2000-icon.pdn) · [Paint.NET drawing guide (German)](docs/Paint.NET-Anleitung-App-Symbol.md) · [Third-party notices](THIRD-PARTY-NOTICES.md) · [Grütze-Software](https://github.com/gruetze-software) · [Ko-fi](https://ko-fi.com/gruetzesoftware)
