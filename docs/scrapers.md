# Scrapers (1.4)

Open **Settings → Scrapers**. Official builds include project access for TMDB, Fanart.tv and TheTVDB. **You do not need your own API keys.** Choose providers with the checkboxes and follow the links for provider information.

| Provider | Use | Access |
| --- | --- | --- |
| TMDB | Movie and TV-show metadata; artwork | Included project access |
| Fanart.tv | Movie, show and season artwork | Included project access |
| TVmaze | Fallback episode stills | No API key |
| TheTVDB | Optional TV-show search, metadata, episodes and artwork | Included project access; no personal API key required |

Movies continue to use TMDB. For TV shows, choose a default metadata provider or switch providers in the search dialog. The chosen provider is saved with the show; single-episode and whole-library episode updates use that provider and keep its IDs separate. Episode lookup currently uses TheTVDB's official aired order.

Artwork from TMDB, Fanart.tv and TVmaze can be enabled or disabled independently. Enabling TheTVDB also makes its available series artwork searchable. The configured artwork language is ranked first; other languages remain available. Translated TVDB titles and descriptions use the configured metadata language, falling back to the original data when no translation exists.

TheTVDB remains an optional source. The application's project key has been tested successfully without a subscriber PIN. No separate registration is needed for the tested project access. [TheTVDB](https://www.thetvdb.com/).

The dialog has no credential fields. Project access is provided by the build; access tokens returned by TheTVDB are kept in memory only. Existing credential overrides in older settings files remain compatible.

Changes in the scraper dialog are applied to the settings draft; choose **Save** in the main Settings window to persist them. Cancel leaves the active configuration unchanged.

## Building from source

GitHub builds receive `TVDB_PROJECT_API_KEY` through the repository secret and embed the project access in the application. The project key is not saved in users' settings files or committed as a literal in source code. Local developers can supply the same build/runtime environment variable. A source build without it reports that TheTVDB project access is unavailable.

Flatpak build environments must explicitly forward the variable into the build sandbox; setting a GitHub secret alone does not make it available inside Flatpak. The separately maintained manifest controls that forwarding. External Flathub builders do not automatically have access to this repository's GitHub secrets.
