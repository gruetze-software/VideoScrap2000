using System.Globalization;
using System.Text.Json;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MovieScrap2000;
using MovieScrap2000.TMDbScraper;
using VideoScrap2000.Views;

namespace VideoScrap2000.ViewModels;

public sealed record SeriesPicture(string Path, byte[]? PendingData = null, string? FallbackPath = null);

public partial class SeriesViewModel
{
    private const string FanartName = "fanart";
    [ObservableProperty] private SeriesPicture? fanartPreview;
    [ObservableProperty] private SeriesPicture? seasonBannerPreview;
    [ObservableProperty] private ActorEditor? selectedActor;
    [ObservableProperty] private SeriesPicture? actorPreview;

    internal static string SeasonBannerName(int? season) => season switch
    {
        null => "season-all-banner",
        0 => "season-specials-banner",
        _ => $"season{season:00}-banner"
    };

    private void RefreshArtworkPreviews()
    {
        FanartPreview = SelectedShow?.Artwork.GetValueOrDefault(FanartName);
        SeasonBannerPreview = SelectedShow?.Artwork.GetValueOrDefault(SeasonBannerName(SelectedSeason?.Number));
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
            if (id > 0 && !show.Artwork.ContainsKey(FanartName))
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
        var missingBanners = show.Episodes.Select(episode => episode.Editor.SeasonNumber).Append(null)
            .Distinct().Where(season => !show.Artwork.ContainsKey(SeasonBannerName(season))).ToArray();
        var missingFanart = !show.Artwork.ContainsKey(FanartName);
        if (!missingFanart && missingBanners.Length == 0) return 0;
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
            if (missingFanart)
            {
                var urls = RankImages(artwork, "showbackground", language);
                var data = await DownloadFirstImageAsync(urls, scraper, token);
                if (data is null && SeriesNfoEditor.Text(details, "backdrop_path") is { Length: > 0 } backdrop)
                {
                    try
                    {
                        var bytes = await scraper.GetImageFromTMDbByteArrayAsync(backdrop, ImageSize.Original, ImageType.Backdrop, token);
                        if (await IsUsableImageAsync(bytes, token)) data = bytes;
                    }
                    catch (Exception ex) when (ex is not OperationCanceledException || !token.IsCancellationRequested) { System.Diagnostics.Trace.WriteLine(ex); }
                }
                if (data is not null) { show.Artwork[FanartName] = new(FanartName, data); count++; }
            }
            foreach (var season in missingBanners)
            {
                var urls = RankImages(artwork, "seasonbanner", language, image =>
                {
                    var value = SeriesNfoEditor.Text(image, "season");
                    return season is null ? string.Equals(value, "all", StringComparison.OrdinalIgnoreCase)
                        : int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out var number) && number == season;
                });
                if (await DownloadFirstImageAsync(urls, scraper, token) is not { } data) continue;
                var name = SeasonBannerName(season);
                show.Artwork[name] = new(name, data);
                count++;
            }
        }
        finally { RefreshArtworkPreviews(); }
        return count;
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
