# Scrapers and API access (1.3)

Open **Settings → Scrapers and API access**. Existing installations continue to use the bundled TMDB and Fanart.tv project access without additional setup.

| Provider | Use | Optional credentials |
| --- | --- | --- |
| TMDB | Movie and TV-show metadata; artwork | Your **API Read Access Token (v4)**, not the v3 API key |
| Fanart.tv | Movie, show and season artwork | Your personal API key, used alongside the bundled project key |
| TVmaze | Fallback episode stills | No API key |
| TheTVDB | Optional TV-show search, metadata, episodes and artwork | A v4 project API key approved for this application; a subscriber PIN if the key uses user-supported access |

Movies continue to use TMDB. For TV shows, choose a default metadata provider or switch providers in the search dialog. The chosen provider is saved with the show; single-episode and whole-library episode updates use that provider and keep its IDs separate. Episode lookup currently uses TheTVDB's official aired order.

Artwork from TMDB, Fanart.tv and TVmaze can be enabled or disabled independently. Enabling TheTVDB also makes its available series artwork searchable. The configured artwork language is ranked first; other languages remain available. Translated TVDB titles and descriptions use the configured metadata language, falling back to the original data when no translation exists.

TheTVDB access is optional and is **not included** in the app. A personal PIN alone does not provide API access. Check [TheTVDB's current access models](https://www.thetvdb.com/api-information) before subscribing or applying for a key. Keys issued to another application should not be reused.

Provider links are available next to the credential fields. Blank TMDB and Fanart.tv fields restore bundled access. Credentials are stored in your local settings file **without encryption** and displayed as password fields. Do not share the settings file or include it in bug reports. Access tokens returned by TheTVDB are kept in memory only.

Changes in the scraper dialog are applied to the settings draft; choose **Save** in the main Settings window to persist them. Cancel leaves the active configuration unchanged.
