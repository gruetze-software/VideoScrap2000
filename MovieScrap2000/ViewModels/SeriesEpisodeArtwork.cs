using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json;
using CommunityToolkit.Mvvm.ComponentModel;
using MovieScrap2000;
using MovieScrap2000.TMDbScraper;
using VideoScrap2000.Views;

namespace VideoScrap2000.ViewModels;

public partial class SeriesViewModel
{
    private static readonly HttpClient TvmazeClient = new() { Timeout = TimeSpan.FromSeconds(15) };
    private static readonly string[] EpisodePictureExtensions = [".jpg", ".jpeg", ".png", ".webp"];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasEpisodePicture))]
    private SeriesPicture? episodePicture;
    public bool HasEpisodePicture => EpisodePicture is not null;

    private static string EpisodePictureStem(SeriesEpisodeRow row) =>
        Path.Combine(Path.GetDirectoryName(row.Episode.VideoPath)!,
            Path.GetFileNameWithoutExtension(row.Episode.VideoPath) + "-thumb");

    private static string? FindEpisodePicture(SeriesEpisodeRow row)
    {
        var stem = EpisodePictureStem(row);
        return EpisodePictureExtensions.Select(extension => stem + extension).FirstOrDefault(File.Exists);
    }

    private void RefreshEpisodePicture()
    {
        EpisodePicture = SelectedEpisode is { } row
            ? row.PendingPicture ?? (FindEpisodePicture(row) is { } path ? new(path) : null)
            : null;
    }

    private async Task SaveEpisodePictureAsync(SeriesEpisodeRow row, CancellationToken token)
    {
        if (row.PendingPicture?.PendingData is not { } bytes) return;
        var stem = EpisodePictureStem(row);
        var path = await Task.Run(() => ArtworkOptimizer.SaveAsync(Path.GetDirectoryName(stem)!, Path.GetFileName(stem), bytes,
            "thumb", (ArtworkProfile)main.ArtworkOptimizationProfile, token), token);
        ArtworkPreviewCache.Shared.InvalidateFolder(Path.GetDirectoryName(stem)!);
        row.PendingPicture = null;
        if (ReferenceEquals(row, SelectedEpisode)) EpisodePicture = new(path);
    }

    private Lazy<Task<JsonElement>> CreateTvmazeLookup(SeriesShowRow show, CancellationToken token) =>
        new(() => LookupTvmazeEpisodesAsync(show.Editor.ImdbId, show.Editor.TvdbId, token));

    private async Task<JsonElement> LookupTvmazeEpisodesAsync(string? imdbId, string? tvdbId, CancellationToken token)
    {
        if (!main.Scrapers.EnableTvmaze) return default;
        foreach (var (key, id) in new[] { ("imdb", imdbId), ("thetvdb", tvdbId) })
        {
            if (string.IsNullOrWhiteSpace(id) || id == "0" || id == "-1") continue;
            using var response = await EpisodeImageClient.GetAsync(
                $"https://api.tvmaze.com/lookup/shows?{key}={Uri.EscapeDataString(id)}", token);
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound) continue;
            response.EnsureSuccessStatusCode();
            var show = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: token);
            if (!int.TryParse(SeriesNfoEditor.Text(show, "id"), NumberStyles.None, CultureInfo.InvariantCulture, out var mazeId)) continue;
            return await EpisodeImageClient.GetFromJsonAsync<JsonElement>(
                $"https://api.tvmaze.com/shows/{mazeId}/episodes?specials=1", token);
        }
        return default;
    }

    private async Task DownloadEpisodePictureAsync(SeriesEpisodeRow row, JsonElement episode, TMDbScraperManager scraper,
        Lazy<Task<JsonElement>> tvmaze, CancellationToken token)
    {
        if (row.PendingPicture is not null || await Task.Run(() => FindEpisodePicture(row), token) is not null) return;

        byte[]? bytes = null;
        if (SeriesNfoEditor.Text(episode, "still_url") is { Length: > 0 } stillUrl)
        {
            try
            {
                var data = await scraper.GetImageFromFanArtTvByteArrayAsync(stillUrl, token);
                if (await IsUsableImageAsync(data, token)) bytes = data;
            }
            catch (Exception ex) when (IsOptionalImageFailure(ex, token)) { System.Diagnostics.Trace.WriteLine(ex); }
        }
        if (scraper.ArtworkEnabled && SeriesNfoEditor.Text(episode, "still_path") is { Length: > 0 } stillPath)
        {
            try
            {
                var data = await scraper.GetImageFromTMDbByteArrayAsync(stillPath, ImageSize.Original, ImageType.Still, token);
                if (await IsUsableImageAsync(data, token)) bytes = data;
            }
            catch (Exception ex) when (IsOptionalImageFailure(ex, token)) { System.Diagnostics.Trace.WriteLine(ex); }
        }
        if (bytes is null)
        {
            try
            {
                var episodes = await tvmaze.Value;
                if (episodes.ValueKind != JsonValueKind.Array) return;
                var season = row.Editor.SeasonNumber?.ToString(CultureInfo.InvariantCulture);
                var number = row.Editor.EpisodeNumber?.ToString(CultureInfo.InvariantCulture);
                var aired = SeriesNfoEditor.Text(episode, "air_date");
                foreach (var candidate in episodes.EnumerateArray())
                {
                    if (SeriesNfoEditor.Text(candidate, "season") != season || SeriesNfoEditor.Text(candidate, "number") != number
                        || !string.IsNullOrEmpty(aired) && SeriesNfoEditor.Text(candidate, "airdate") is { Length: > 0 } date && date != aired
                        || !candidate.TryGetProperty("image", out var image)
                        || !Uri.TryCreate(SeriesNfoEditor.Text(image, "original"), UriKind.Absolute, out var url)
                        || url.Scheme != Uri.UriSchemeHttps) continue;
                    var data = await EpisodeImageClient.GetByteArrayAsync(url, token);
                    if (await IsUsableImageAsync(data, token)) bytes = data;
                    break;
                }
            }
            catch (Exception ex) when (IsOptionalImageFailure(ex, token)) { System.Diagnostics.Trace.WriteLine(ex); }
        }
        token.ThrowIfCancellationRequested();
        if (bytes is null) return;
        row.PendingPicture = new(EpisodePictureStem(row) + ".jpg", bytes);
        if (ReferenceEquals(row, SelectedEpisode)) EpisodePicture = row.PendingPicture;
    }

    private static bool IsOptionalImageFailure(Exception ex, CancellationToken token) =>
        ex is HttpRequestException or JsonException or IOException or OperationCanceledException && !token.IsCancellationRequested;
}
