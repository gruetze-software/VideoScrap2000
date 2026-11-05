# VideoScrap 2000

<img src="docs/videoscrap-2000-icon-64.png" alt="VideoScrap 2000" width="64" height="64" />

VideoScrap 2000 is a desktop library manager for movies and TV shows on Windows, Linux, and macOS. Browse your collection, find metadata and artwork, and save the results as NFO and image files beside your videos. It works with local folders and mounted network shares.

[Download VideoScrap 2000](https://github.com/gruetze-software/VideoScrap2000/releases/latest#downloads)

<img src="branding/screenshots/screenshot1.png" width="724" alt="Movie Mainform" />

<img src="branding/screenshots/screenshot1_2.png" width="724" alt="Movie Mainform Actors" />

<img src="branding/screenshots/screenshot1_1.png" width="724" alt="Movie Plot KI" />

<img src="branding/screenshots/screenshot1_3.png" width="724" alt="Movie Mainform Artworks" />

<img src="branding/screenshots/screenshot2.png" width="724" alt="TV Series Mainform" />

## Features

- **Movies and TV shows:** manage films, seasons and episodes in separate libraries.
- **Metadata and cast:** find details with TMDB and edit descriptions, ratings, genres and cast.
- **Artwork:** download posters, fanart, logos, banners and episode stills from TMDB, Fanart.tv and TVmaze.
- **Kodi support:** save NFOs and artwork beside your videos, with backups of replaced files.
- **AI-assisted plot outlines:** prepare a prompt for ChatGPT or another AI service, then paste the summary back into the app.
- **Your language and style:** English/German interface, independent metadata and artwork languages, light/dark themes and optional fanart backgrounds.
- **Everyday browsing:** poster thumbnails, star ratings, search, sorting and automatic technical media details.

## Get started

1. Download and extract the package for your operating system and architecture from **GitHub Releases**. Choose **self-contained** to include .NET 10, or the smaller **framework-dependent** package if .NET 10 is already installed.
2. Open **Settings** and choose your movie and TV folders. Use one subfolder per movie or show; TV shows may contain season folders.
3. Select **Scan library**, find metadata, review the results and choose **Save changes**.

Works with local folders and mounted NAS shares. Windows, Linux and macOS packages are available for x64 and ARM64.

Want to explore first? Open **Settings → Demo mode → Open demo mode** for a separate fictional collection. [Demo details](docs/demo-assets.md).

## Kodi support

Metadata and artwork are saved locally as movie NFOs, `tvshow.nfo`, episode NFOs and image files. In Kodi, scan for new items or use **Information → Refresh** to reread changes to existing entries. VideoScrap 2000 does not modify Kodi's database directly.

[Read the Kodi guide](docs/kodi.md).

## Metadata and artwork sources

[<img src="docs/tmdb.svg" alt="TMDB" width="104" />](https://www.themoviedb.org/) &nbsp; [<img src="docs/fanart-tv.svg" alt="Fanart.tv artwork" width="128" height="28" />](https://fanart.tv/)

[TMDB](https://www.themoviedb.org/) provides metadata and artwork, [Fanart.tv](https://fanart.tv/) adds artwork, and [TVmaze](https://www.tvmaze.com/) supplies fallback episode images ([CC BY-SA](https://www.tvmaze.com/api#licensing)). Release builds include the Fanart.tv project key.

Metadata and images follow your client language by default, with separate overrides in Settings. Availability varies by title. [Language details](docs/languages.md).

> This product uses the TMDB API but is not endorsed or certified by TMDB.

## Build from source

Requires the .NET 10 SDK.

```powershell
dotnet run --project MovieScrap2000/MovieScrap2000.csproj
```

[Build, test and release instructions](docs/development.md).

Enjoy VideoScrap 2000? [Buy me a small coffee on Ko-fi](https://ko-fi.com/gruetzesoftware) to support its development. Thank you!

[Third-party notices](THIRD-PARTY-NOTICES.md) · [Grütze-Software](https://github.com/gruetze-software)
