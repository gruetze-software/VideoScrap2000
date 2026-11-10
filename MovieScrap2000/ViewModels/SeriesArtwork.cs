using System.Globalization;
using System.Text.Json;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MovieScrap2000;
using MovieScrap2000.TMDbScraper;
using TmdbImages = MovieScrap2000.TMDbScraper.TMDbScraper.TMDbImages;
using TmdbImage = MovieScrap2000.TMDbScraper.TMDbScraper.TMDbImageData;
using VideoScrap2000.Localization;
using VideoScrap2000.Views;

namespace VideoScrap2000.ViewModels;

public sealed record SeriesPicture(string Path, byte[]? PendingData = null, string? FallbackPath = null);
public sealed record SeriesArtworkTile(string Name, SeriesPicture Picture, string Key)
{
    public int? Season
    {
        get
        {
            if (!Key.StartsWith("season", StringComparison.OrdinalIgnoreCase)) return null;
            var separator = Key.LastIndexOf('-');
            if (separator < 6) return null;
            var value = Key[6..separator];
            if (value == "-specials") return 0;
            return int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out var number) ? number : null;
        }
    }

    public string Type => SeriesArtworkCatalog.Types(Season)
        .FirstOrDefault(type => type.Equals(Key[(Key.LastIndexOf('-') + 1)..], StringComparison.OrdinalIgnoreCase))
        ?? ImageTypes.Poster;
}

public partial class SeriesViewModel
{
    private const string FanartName = "fanart";
    private bool refreshingArtworkTiles;
    [ObservableProperty] private SeriesPicture? fanartPreview;
    [ObservableProperty] private SeriesPicture? backgroundPreview;
    [ObservableProperty] private SeriesPicture? seasonBannerPreview;
    [ObservableProperty] private SeriesPicture? seasonPosterPreview;
    [ObservableProperty] private IReadOnlyList<SeriesArtworkTile> artworkTiles = [];
    [ObservableProperty] private SeriesArtworkTile? selectedSeriesArtwork;
    [ObservableProperty] private ActorEditor? selectedActor;
    [ObservableProperty] private SeriesPicture? actorPreview;
    [ObservableProperty] private string artworkSearchType = ImageTypes.Poster;
    [ObservableProperty] private string artworkSourceWarning = "";
    public IReadOnlyList<string> SeriesArtworkTypes => new[] { "Alle" }
        .Concat(SeriesArtworkCatalog.Types(SelectedSeason?.Number)).ToArray();

    public bool UseFanartBackground => main.UseFanartBackground;
    internal void RefreshAppearance() => OnPropertyChanged(nameof(UseFanartBackground));

    internal static string SeasonArtworkName(int? season, string type) => season switch
    {
        null => $"season-all-{type}",
        0 => $"season-specials-{type}",
        _ => $"season{season:00}-{type}"
    };

    partial void OnSelectedSeriesArtworkChanged(SeriesArtworkTile? value)
    {
        if (value is null || refreshingArtworkTiles) return;
        var season = Seasons.FirstOrDefault(item => item.Number == value.Season);
        if (season is null && value.Season is { } number)
        {
            season = new(number, number == 0 ? Locale.T("Spezialfolgen") : string.Format(Locale.T("Staffel {0}"), number));
            var position = Seasons.TakeWhile(item => item.Number is null || item.Number < number).Count();
            Seasons.Insert(position, season);
        }
        SelectedSeason = season;
        ArtworkSearchType = value.Type;
    }

    private void RefreshArtworkPreviews(bool refreshTiles = false)
    {
        var artwork = SelectedShow?.Artwork;
        var filterSeason = SelectedSeason?.Number;
        var season = filterSeason ?? SelectedEpisode?.Editor.SeasonNumber;
        FanartPreview = artwork?.GetValueOrDefault(FanartName);
        BackgroundPreview = artwork?.GetValueOrDefault(SeasonArtworkName(season, FanartName)) ?? FanartPreview;
        SeasonBannerPreview = artwork?.GetValueOrDefault(SeasonArtworkName(season, "banner"));
        SeasonPosterPreview = artwork?.GetValueOrDefault(SeasonArtworkName(season, "poster"));
        if (filterSeason is null && SelectedShow is { } show)
        {
            SeasonBannerPreview = artwork?.GetValueOrDefault("banner")
                ?? artwork?.GetValueOrDefault("season-all-banner") ?? SeasonBannerPreview ?? show.FirstSeasonPicture("banner");
            SeasonPosterPreview ??= show.Poster;
        }
        if (refreshTiles)
        {
            var selectedKey = SelectedSeriesArtwork?.Key;
            SelectedShow?.RefreshPoster();
            refreshingArtworkTiles = true;
            try
            {
                ArtworkTiles = artwork is null ? [] : artwork.OrderBy(item => item.Key, StringComparer.OrdinalIgnoreCase)
                    .Select(item => new SeriesArtworkTile(ArtworkLabel(item.Key), item.Value, item.Key)).ToArray();
                SelectedSeriesArtwork = ArtworkTiles.FirstOrDefault(tile => tile.Key == selectedKey);
            }
            finally { refreshingArtworkTiles = false; }
            if (DetailTabIndex == 3 && SelectedSeriesArtwork is null) SelectFirstDetailEntry();
        }
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

    public ArtworkWindow? CreateArtworkSearch()
    {
        if (UseDemoData()) return null;
        if (SelectedShow is not { } show) return null;
        int.TryParse(show.Editor.TmdbId, out var id);
        int.TryParse(show.Editor.TvdbId, out var tvdbId);
        if (id <= 0 && tvdbId <= 0)
        {
            main.Status = "Für die Bildsuche braucht die Serie eine TMDB- oder TVDB-ID.";
            return null;
        }
        return new ArtworkWindow(id, ArtworkSearchType, CreateScraper(), main.ArtworkLanguage,
            tvdbId: tvdbId, season: SelectedSeason?.Number);
    }

    public void StageSeriesArtwork(IEnumerable<ImageCacheItem> images, int? season)
    {
        if (SelectedShow is not { } show) return;
        var results = images.ToArray();
        if (results.Length == 1) ArtworkSearchType = results[0].Type;
        foreach (var image in results)
        {
            var type = image.Type.ToLowerInvariant();
            var name = season.HasValue ? SeasonArtworkName(season, type) : type;
            show.RemovedArtwork.Remove(name);
            show.Artwork[name] = new(name, image.Data);
        }
        RefreshArtworkPreviews(refreshTiles: true);
        main.Status = "Serienbilder übernommen. Speichere, um sie abzulegen.";
    }

    public void RemoveArtwork(SeriesArtworkTile tile)
    {
        if (main.IsBusy || SelectedShow is not { } show || !show.Artwork.TryGetValue(tile.Key, out var picture) || !ReferenceEquals(picture, tile.Picture)) return;
        show.Artwork.Remove(tile.Key);
        var index = ArtworkTiles.ToList().FindIndex(item => item.Key == tile.Key);
        show.RemovedArtwork.Add(tile.Key);
        RefreshArtworkPreviews(refreshTiles: true);
        SelectedSeriesArtwork = ArtworkTiles.Count > 0 ? ArtworkTiles[Math.Clamp(index, 0, ArtworkTiles.Count - 1)] : null;
        main.Status = "Artwork zum Entfernen vorgemerkt. Mit Änderungen speichern wird es entfernt und als .bak gesichert.";
    }

    private async Task<int> DownloadArtworkAsync(SeriesShowRow show, int id, JsonElement details, TMDbScraperManager scraper, CancellationToken token)
    {
        ArtworkSourceWarning = "";
        var seasons = show.Episodes.Select(episode => episode.Editor.SeasonNumber).Where(season => season.HasValue).Distinct().ToArray();
        var targets = new List<(string Name, string Type, int? Season, string? TmdbPath, ImageType TmdbType)>
        {
            (FanartName, ImageTypes.Fanart, null, SeriesNfoEditor.Text(details, "backdrop_path"), ImageType.Backdrop),
            ("poster", ImageTypes.Poster, null, SeriesNfoEditor.Text(details, "poster_path"), ImageType.Poster),
            ("banner", ImageTypes.Banner, null, null, ImageType.Backdrop),
            ("clearlogo", ImageTypes.ClearLogo, null, null, ImageType.Logo),
            ("clearart", ImageTypes.ClearArt, null, null, ImageType.Poster),
            ("landscape", ImageTypes.Landscape, null, null, ImageType.Backdrop),
            ("characterart", ImageTypes.CharacterArt, null, null, ImageType.Poster)
        };
        var tmdbSeasons = SeriesNfoEditor.Array(details, "seasons").ToArray();
        foreach (var season in seasons)
        {
            var number = season!.Value.ToString(CultureInfo.InvariantCulture);
            var tmdbSeason = tmdbSeasons.FirstOrDefault(item => SeriesNfoEditor.Text(item, "season_number") == number);
            targets.Add((SeasonArtworkName(season, "poster"), ImageTypes.Poster, season, SeriesNfoEditor.Text(tmdbSeason, "poster_path"), ImageType.Poster));
            targets.Add((SeasonArtworkName(season, "banner"), ImageTypes.Banner, season, null, ImageType.Backdrop));
            targets.Add((SeasonArtworkName(season, FanartName), ImageTypes.Fanart, season, null, ImageType.Backdrop));
            targets.Add((SeasonArtworkName(season, "landscape"), ImageTypes.Landscape, season, null, ImageType.Backdrop));
        }
        var missing = targets.Where(target => !show.Artwork.ContainsKey(target.Name) && !show.RemovedArtwork.Contains(target.Name)).ToArray();
        if (missing.Length == 0) return 0;
        main.Status = "Serienbilder werden gesucht …";

        JsonElement artwork = default;
        int.TryParse(show.Editor.TvdbId, out var tvdbId);
        try { artwork = await scraper.GetSeriesArtworkAsync(id, tvdbId, token); }
        catch (Exception ex) when (ex is HttpRequestException or JsonException || ex is OperationCanceledException && !token.IsCancellationRequested)
        {
            System.Diagnostics.Trace.WriteLine(ex);
            ArtworkSourceWarning = Locale.T(ex is HttpRequestException { StatusCode: System.Net.HttpStatusCode.Unauthorized or System.Net.HttpStatusCode.Forbidden }
                ? "Fanart.tv benötigt einen gültigen Projektschlüssel. Zusätzliche Serienbilder konnten nicht geladen werden."
                : "Fanart.tv ist derzeit nicht erreichbar.");
        }
        var count = 0;
        var language = main.ArtworkLanguage.Split('-')[0];
        var tmdbImages = new Dictionary<int, TmdbImages>();
        try
        {
            foreach (var target in missing)
            {
                var fanartImages = SeriesArtworkCatalog.FanartFields(target.Type, target.Season)
                    .SelectMany(type => SeriesNfoEditor.Array(artwork, type))
                    .Where(image => MatchesSeason(image, target.Season)).ToArray();
                var data = await DownloadFirstImageAsync(fanartImages
                    .Where(image => LanguagePreferences.ImagePriority(SeriesNfoEditor.Text(image, "lang"), language) == 0)
                    .Select(image => SeriesNfoEditor.Text(image, "url")).OfType<string>(), scraper, token);
                if (data is null)
                {
                    var sources = fanartImages.Select(image => (Path: SeriesNfoEditor.Text(image, "url"), Remote: true,
                        Priority: LanguagePreferences.ImagePriority(SeriesNfoEditor.Text(image, "lang"), language)))
                        .Where(source => source.Priority != 0).ToList();
                    if (scraper.ArtworkEnabled && target.Type is (ImageTypes.Poster or ImageTypes.Fanart or ImageTypes.ClearLogo))
                    {
                        var key = target.Season ?? -1;
                        if (!tmdbImages.TryGetValue(key, out var images))
                        {
                            try { images = await scraper.GetSeriesImagesAsync(id, target.Season, token); }
                            catch (Exception ex) when (ex is HttpRequestException or JsonException) { System.Diagnostics.Trace.WriteLine(ex); images = new(); }
                            tmdbImages[key] = images;
                        }
                        IEnumerable<TmdbImage> matching = target.Type switch
                        {
                            ImageTypes.Poster => images.Posters,
                            ImageTypes.Fanart => images.Backdrops,
                            _ => images.Logos
                        };
                        sources.AddRange(matching.Select(image => (image.FilePath, false, LanguagePreferences.ImagePriority(image.Iso6391, language))));
                    }
                    foreach (var source in sources.Where(source => !string.IsNullOrWhiteSpace(source.Path)).OrderBy(source => source.Priority).Take(6))
                    {
                        try
                        {
                            var bytes = source.Remote ? await scraper.GetImageFromFanArtTvByteArrayAsync(source.Path!, token)
                                : await scraper.GetImageFromTMDbByteArrayAsync(source.Path!, ImageSize.Original, target.TmdbType, token);
                            if (await IsUsableImageAsync(bytes, token)) { data = bytes; break; }
                        }
                        catch (Exception ex) when (ex is not OperationCanceledException || !token.IsCancellationRequested) { System.Diagnostics.Trace.WriteLine(ex); }
                    }
                }
                if (data is null && scraper.ArtworkEnabled && target.TmdbPath is { Length: > 0 } path)
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
