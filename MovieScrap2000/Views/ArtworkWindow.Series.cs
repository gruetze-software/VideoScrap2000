using System.Globalization;
using System.Text.Json;
using MovieScrap2000;
using VideoScrap2000.ViewModels;
using TmdbImageType = MovieScrap2000.TMDbScraper.ImageType;
using static VideoScrap2000.Localization.Locale;

namespace VideoScrap2000.Views;

public partial class ArtworkWindow
{
    private JsonElement seriesFanart;
    private List<TvdbArtwork>? tvdbImages;

    private async Task<List<ArtworkSource>> FindSeriesSourcesAsync(string type, CancellationToken token)
    {
        if (movieImages is null)
        {
            movieImages = new();
            if (tmdbId > 0)
            {
                try { movieImages = await scraper.GetSeriesImagesAsync(tmdbId, season, token); }
                catch (Exception ex) when (ex is HttpRequestException or JsonException)
                {
                    providerError = T("TMDB ist derzeit nicht erreichbar.");
                }
            }
        }
        if (!fanartLoaded)
        {
            try { seriesFanart = await scraper.GetSeriesArtworkAsync(tmdbId, tvdbId!.Value, token); }
            catch (Exception ex) when (ex is HttpRequestException or JsonException)
            {
                providerError = T(ex is HttpRequestException { StatusCode: System.Net.HttpStatusCode.Unauthorized or System.Net.HttpStatusCode.Forbidden }
                    ? "Fanart.tv benötigt einen gültigen Projektschlüssel. Zusätzliche Serienbilder konnten nicht geladen werden."
                    : "Fanart.tv ist derzeit nicht erreichbar.");
            }
            fanartLoaded = true;
        }
        var kind = type switch
        {
            ImageTypes.Fanart or ImageTypes.Landscape or ImageTypes.Banner => TmdbImageType.Backdrop,
            ImageTypes.ClearLogo => TmdbImageType.Logo,
            _ => TmdbImageType.Poster
        };
        var paths = type switch
        {
            ImageTypes.Poster => movieImages.Posters,
            ImageTypes.Fanart => movieImages.Backdrops,
            ImageTypes.ClearLogo => movieImages.Logos,
            _ => null
        };
        var sources = paths?.Select(image => new ArtworkSource(image.FilePath, null, kind,
            $"TMDB · {image.Iso6391 ?? "neutral"} · {image.Width} × {image.Height}",
            LanguagePriority(image.Iso6391))).ToList() ?? [];
        if (scraper.Providers.EnableTvdb && tvdbId > 0)
        {
            if (tvdbImages is null)
            {
                tvdbImages = [];
                try
                {
                    using var tvdb = new TvdbScraper(scraper.Providers);
                    var details = await tvdb.GetShowAsync(tvdbId.Value, language, token);
                    tvdbImages = details.GetProperty("tvdb_artworks").Deserialize<List<TvdbArtwork>>() ?? [];
                }
                catch (Exception ex) when (ex is HttpRequestException or JsonException or InvalidOperationException)
                { providerError = T("TheTVDB-Bilder konnten nicht geladen werden. Bitte den Zugang in den Einstellungen prüfen."); }
            }
            sources.AddRange(tvdbImages.Where(image => image.Type == type && image.Season == season)
                .Select(image => new ArtworkSource(null, image.Url, kind, $"TheTVDB · {image.Language ?? "neutral"}",
                    LanguagePriority(SeriesViewModel.TvdbArtworkLanguage(image.Language)))));
        }
        var keys = SeriesArtworkCatalog.FanartFields(type, season);
        foreach (var key in keys)
        {
            foreach (var image in SeriesNfoEditor.Array(seriesFanart, key).Where(MatchesSeriesSeason))
            {
                var language = SeriesNfoEditor.Text(image, "lang");
                sources.Add(new(null, SeriesNfoEditor.Text(image, "url"), kind,
                    $"Fanart.tv · {language ?? "neutral"}", LanguagePriority(language)));
            }
        }
        return sources.Where(source => !string.IsNullOrWhiteSpace(source.Path) || !string.IsNullOrWhiteSpace(source.Url))
            .OrderBy(source => source.Priority).DistinctBy(source => source.Path ?? source.Url).ToList();
    }

    private bool MatchesSeriesSeason(JsonElement image)
    {
        var value = SeriesNfoEditor.Text(image, "season");
        return season.HasValue
            ? int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var number) && number == season
            : string.IsNullOrWhiteSpace(value) || value.Equals("all", StringComparison.OrdinalIgnoreCase);
    }

    private int LanguagePriority(string? value) => LanguagePreferences.ImagePriority(value, language);
}
