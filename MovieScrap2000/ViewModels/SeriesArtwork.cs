using System.Globalization;
using System.Text.Json;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MovieScrap2000;
using MovieScrap2000.TMDbScraper;
using VideoScrap2000.Localization;
using VideoScrap2000.Views;

namespace VideoScrap2000.ViewModels;

public sealed record SeriesPicture(string Path, byte[]? PendingData = null, string? FallbackPath = null);
public sealed record SeriesArtworkTile(string Name, SeriesPicture Picture);

public partial class SeriesViewModel
{
    private const string FanartName = "fanart";
    [ObservableProperty] private SeriesPicture? fanartPreview;
    [ObservableProperty] private SeriesPicture? backgroundPreview;
    [ObservableProperty] private SeriesPicture? seasonBannerPreview;
    [ObservableProperty] private IReadOnlyList<SeriesArtworkTile> artworkTiles = [];
    [ObservableProperty] private ActorEditor? selectedActor;
    [ObservableProperty] private SeriesPicture? actorPreview;

    public bool UseFanartBackground => main.UseFanartBackground;
    internal void RefreshAppearance() => OnPropertyChanged(nameof(UseFanartBackground));

    private static string SeasonArtworkName(int? season, string type) => season switch
    {
        null => $"season-all-{type}",
        0 => $"season-specials-{type}",
        _ => $"season{season:00}-{type}"
    };

    private void RefreshArtworkPreviews(bool refreshTiles = false)
    {
        var artwork = SelectedShow?.Artwork;
        var season = SelectedSeason?.Number;
        FanartPreview = artwork?.GetValueOrDefault(FanartName);
        BackgroundPreview = artwork?.GetValueOrDefault(SeasonArtworkName(season, FanartName)) ?? FanartPreview;
        SeasonBannerPreview = artwork?.GetValueOrDefault(SeasonArtworkName(season, "banner"));
        if (refreshTiles)
            ArtworkTiles = artwork is null ? [] : artwork.OrderBy(item => item.Key, StringComparer.OrdinalIgnoreCase)
                .Select(item => new SeriesArtworkTile(ArtworkLabel(item.Key), item.Value)).ToArray();
    }

    private static string ArtworkLabel(string name)
    {
        if (!name.StartsWith("season", StringComparison.OrdinalIgnoreCase)) return Locale.T(name);
        var split = name.LastIndexOf('-');
        var season = name[6..split];
        var label = season switch
        {
            "-all" => Locale.T("Alle Staffeln"),
            "-specials" => Locale.T("Spezialfolgen"),
            _ => string.Format(Locale.T("Staffel {0}"), season)
        };
        return $"{label} · {Locale.T(name[(split + 1)..])}";
    }

    partial void OnSelectedActorChanged(ActorEditor? value) => RefreshActorPreview();

    public void RefreshActorPreview()
    {
        ActorPreview = null;
        if (SelectedActor is not { } actor || SelectedShow is not { } show) return;
        var folder = show.Show.FolderPath;
        var fallback = Path.Combine(folder, ".actors", actor.Name.Replace(' ', '_') + ".jpg");
        var path = actor.Thumb;
        if (string.IsNullOrWhiteSpace(path)) path = fallback;
        else if (!Uri.IsWellFormedUriString(path, UriKind.Absolute)) path = Path.Combine(folder, path);
        ActorPreview = new(path, FallbackPath: fallback);
    }

    [RelayCommand]
    private async Task SearchArtworkAsync()
    {
        if (SelectedShow is not { } show) return;
        int.TryParse(show.Editor.TmdbId, out var id);
        int.TryParse(show.Editor.TvdbId, out var tvdbId);
        if (id <= 0 && tvdbId <= 0)
        {
            main.Status = "Für die Bildsuche braucht die Serie eine TMDB- oder TVDB-ID.";
            return;
        }
        await main.RunAsync(async token =>
        {
            using var scraper = CreateScraper();
            JsonElement details = default;
            if (id > 0)
            {
                try { details = await scraper.GetSeriesDetailsAsync(id, main.MetadataLanguage, token); }
                catch (Exception ex) when (ex is HttpRequestException or JsonException || ex is OperationCanceledException && !token.IsCancellationRequested)
                {
                    System.Diagnostics.Trace.WriteLine(ex);
                }
            }
            var count = await DownloadArtworkAsync(show, id, details, scraper, token);
            main.Status = count == 0 ? "Keine fehlenden Serienbilder gefunden." : "Serienbilder übernommen. Speichere, um sie abzulegen.";
        }, reloadAfterCancellation: false);
    }

    private async Task<int> DownloadArtworkAsync(SeriesShowRow show, int id, JsonElement details, TMDbScraperManager scraper, CancellationToken token)
    {
        var seasons = show.Episodes.Select(episode => episode.Editor.SeasonNumber).Where(season => season.HasValue).Distinct().ToArray();
        var targets = new List<(string Name, string Source, int? Season, string? TmdbPath, ImageType TmdbType)>
        {
            (FanartName, "showbackground", null, SeriesNfoEditor.Text(details, "backdrop_path"), ImageType.Backdrop),
            ("poster", "tvposter", null, SeriesNfoEditor.Text(details, "poster_path"), ImageType.Poster),
            (SeasonArtworkName(null, "banner"), "seasonbanner", null, null, ImageType.Backdrop)
        };
        var tmdbSeasons = SeriesNfoEditor.Array(details, "seasons").ToArray();
        foreach (var season in seasons)
        {
            var number = season!.Value.ToString(CultureInfo.InvariantCulture);
            var tmdbSeason = tmdbSeasons.FirstOrDefault(item => SeriesNfoEditor.Text(item, "season_number") == number);
            targets.Add((SeasonArtworkName(season, "poster"), "seasonposter", season, SeriesNfoEditor.Text(tmdbSeason, "poster_path"), ImageType.Poster));
            targets.Add((SeasonArtworkName(season, "banner"), "seasonbanner", season, null, ImageType.Backdrop));
            targets.Add((SeasonArtworkName(season, FanartName), "showbackground", season, null, ImageType.Backdrop));
        }
        var missing = targets.Where(target => !show.Artwork.ContainsKey(target.Name)).ToArray();
        if (missing.Length == 0) return 0;
        main.Status = "Serienbilder werden gesucht …";

        JsonElement artwork = default;
        int.TryParse(show.Editor.TvdbId, out var tvdbId);
        try { artwork = await scraper.GetSeriesArtworkAsync(id, tvdbId, token); }
        catch (Exception ex) when (ex is HttpRequestException or JsonException || ex is OperationCanceledException && !token.IsCancellationRequested)
        {
            System.Diagnostics.Trace.WriteLine(ex);
        }
        var count = 0;
        var language = main.ArtworkLanguage.Split('-')[0];
        try
        {
            foreach (var target in missing)
            {
                var urls = RankImages(artwork, target.Source, language, image => MatchesSeason(image, target.Season));
                if (target.Source == "showbackground")
                    urls = urls.Concat(RankImages(artwork, "show4kbackground", language, image => MatchesSeason(image, target.Season)));
                var data = await DownloadFirstImageAsync(urls, scraper, token);
                if (data is null && target.TmdbPath is { Length: > 0 } path)
                {
                    try
                    {
                        var bytes = await scraper.GetImageFromTMDbByteArrayAsync(path, ImageSize.Original, target.TmdbType, token);
                        if (await IsUsableImageAsync(bytes, token)) data = bytes;
                    }
                    catch (Exception ex) when (ex is not OperationCanceledException || !token.IsCancellationRequested) { System.Diagnostics.Trace.WriteLine(ex); }
                }
                if (data is not null) { show.Artwork[target.Name] = new(target.Name, data); count++; }
            }
        }
        finally { RefreshArtworkPreviews(refreshTiles: true); }
        return count;
    }

    private static bool MatchesSeason(JsonElement image, int? season)
    {
        var value = SeriesNfoEditor.Text(image, "season");
        return season is null ? string.IsNullOrWhiteSpace(value) || string.Equals(value, "all", StringComparison.OrdinalIgnoreCase)
            : int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out var number) && number == season;
    }

    private static IEnumerable<string> RankImages(JsonElement artwork, string type, string language, Func<JsonElement, bool>? filter = null) =>
        SeriesNfoEditor.Array(artwork, type).Where(image => filter is null || filter(image))
            .OrderBy(image => SeriesNfoEditor.Text(image, "lang") switch
            {
                var value when string.Equals(value, language, StringComparison.OrdinalIgnoreCase) => 0,
                null or "" or "00" => 1,
                _ => 2
            }).Select(image => SeriesNfoEditor.Text(image, "url")).OfType<string>();

    private static async Task<byte[]?> DownloadFirstImageAsync(IEnumerable<string> urls, TMDbScraperManager scraper, CancellationToken token)
    {
        foreach (var url in urls.Take(3))
        {
            token.ThrowIfCancellationRequested();
            if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) || uri.Scheme is not ("http" or "https")) continue;
            try
            {
                var data = await scraper.GetImageFromFanArtTvByteArrayAsync(uri.AbsoluteUri, token);
                if (await IsUsableImageAsync(data, token)) return data;
            }
            catch (Exception ex) when (ex is not OperationCanceledException || !token.IsCancellationRequested) { System.Diagnostics.Trace.WriteLine(ex); }
        }
        return null;
    }

    private static async Task<bool> IsUsableImageAsync(byte[]? bytes, CancellationToken token)
    {
        if (bytes is not { Length: > 0 }) return false;
        using var preview = await Task.Run(() => ArtworkDecoder.DecodePreview(bytes, 96), token);
        return preview is not null;
    }
}
