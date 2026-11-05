# Kodi support

VideoScrap 2000 prepares local metadata and artwork for Kodi. Save your edits in VideoScrap 2000, then let Kodi import the files from the same media folders, including folders on a NAS or mounted network share. VideoScrap 2000 has its own local cache; it does not connect to or modify Kodi's SQLite or shared MySQL/MariaDB database, and does not send remote update commands to Kodi.

### Metadata and files

- **Movies:** Kodi-compatible XML NFOs normally use the video's basename, for example `Arrival.mkv` and `Arrival.nfo`. An existing imported `movie.nfo` is retained when saving. Supported fields include titles, release dates, plot and plot outline, tagline, genres, ratings, certification, countries, studios, collections, cast and roles, and provider IDs.
- **TV shows:** show details are saved in `tvshow.nfo` in the show's root folder. Each episode has an `<episodedetails>` NFO beside its video, for example `Fargo S01E01.mkv` and `Fargo S01E01.nfo`, including its season and episode numbers. The editor currently handles one episode per video file.
- **Artwork:** selected posters, fanart and other artwork are stored locally. TV artwork includes show-level and season-specific images such as `poster.jpg`, `fanart.jpg`, `banner.jpg`, `season01-poster.jpg` and `season01-banner.jpg`, where available. Episode stills use `<video basename>-thumb` image files. The artwork types Kodi displays depend on its skin and artwork settings.
- **Technical movie details:** video codec, resolution, aspect ratio, duration, audio tracks and subtitles are written to the NFO's `<fileinfo><streamdetails>` section when available.
- **Date added:** a new movie NFO receives the first-save date in `<dateadded>`. Subsequent saves preserve a valid existing value; an existing NFO without one uses its creation date. The release year remains a separate field. Kodi's own import settings determine how the date is used in its library.
- **Existing files:** replaced NFOs and artwork receive `.bak` backups. Unmanaged movie NFO elements and unedited TV NFO fields are preserved where possible.

### Import into Kodi

1. Add the media folders as video sources in Kodi and set the correct content type: **Movies** or **TV shows**. If VideoScrap 2000 supplies all metadata, choose **Local information only** as the information provider.
2. For **new movies or episodes**, use **Update library** or **Scan for new content** in Kodi. Kodi reads the local NFOs and artwork during import.
3. For **changes to an existing entry**, open its **Information** page and choose **Refresh**. Keep the local information when Kodi offers to ignore it and search online. A normal library update discovers new items but does not reread changed NFOs for existing entries.

For a shared Kodi database on a NAS, the import or refresh still runs through Kodi. Each client must be able to access the media paths used by that library.

See the official Kodi guides for [NFO files](https://kodi.wiki/view/NFO_files), [TV-show NFOs](https://kodi.wiki/view/NFO_files/TV_shows), and [updating versus refreshing a library](https://kodi.wiki/view/Updating_or_removing_videos).

