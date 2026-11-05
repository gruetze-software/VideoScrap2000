# Languages and availability

The interface supports **English and German**. Metadata and artwork languages are independent of the interface language: their automatic setting preserves the client's original language and region, such as `fr-CA` or `pt-PT`, even when the interface falls back to English. Settings show the detected client culture, the current metadata request language, and the preferred artwork language. You can override metadata and artwork languages separately from a list of ISO-639-1 languages, including selected regional variants. Unsupported client language codes fall back to English.

TMDB supports [localized metadata in many languages](https://developer.themoviedb.org/docs/languages), but translations vary by title and field. A missing translation can produce an original-language value or an empty field; names and roles are not consistently localized. VideoScrap 2000 does not automatically translate missing text.

TMDB and Fanart.tv supply images in the languages available for the title. Results are ranked **preferred language → language-neutral → other languages**; other languages remain visible. TMDB currently tags image languages without regional variants, so `fr-CA` prefers `fr` images. Text inside images is not translated. Episode stills and most backdrops are language-neutral. See [TMDB image languages](https://developer.themoviedb.org/docs/image-languages) and the [Fanart.tv API](https://fanart.tv/api-docs/api-v3/).

