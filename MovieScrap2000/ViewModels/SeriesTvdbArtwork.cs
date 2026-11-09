using System.Text.Json;
using MovieScrap2000;
using MovieScrap2000.TMDbScraper;

namespace VideoScrap2000.ViewModels;

public partial class SeriesViewModel
{
    private async Task<int> DownloadTvdbArtworkAsync(SeriesShowRow show, JsonElement details, TMDbScraperManager scraper, CancellationToken token)
    {
        var count = 0;
        var images = details.GetProperty("tvdb_artworks").Deserialize<List<TvdbArtwork>>() ?? [];
        var language = main.ArtworkLanguage.Split('-')[0];
        foreach (var group in images.GroupBy(image => (image.Type, image.Season)))
        {
            var name = group.Key.Season.HasValue ? SeasonArtworkName(group.Key.Season, group.Key.Type).ToLowerInvariant() : group.Key.Type.ToLowerInvariant();
            if (show.Artwork.ContainsKey(name)) continue;
            foreach (var image in group.OrderBy(image => LanguagePreferences.ImagePriority(TvdbArtworkLanguage(image.Language), language)))
            {
                if (!Uri.TryCreate(image.Url, UriKind.Absolute, out var url) || url.Scheme != "https") continue;
                try
                {
                    var bytes = await scraper.GetImageFromFanArtTvByteArrayAsync(image.Url, token);
                    if (!await IsUsableImageAsync(bytes, token)) continue;
                    show.Artwork[name] = new(Path.Combine(show.Show.FolderPath, name + ".jpg"), bytes);
                    count++; break;
                }
                catch (Exception ex) when (IsOptionalImageFailure(ex, token)) { System.Diagnostics.Trace.WriteLine(ex); }
            }
        }
        // Other enabled sources can fill gaps using correctly matched remote IDs.
        int.TryParse(show.Editor.TmdbId, out var tmdbId);
        count += await DownloadArtworkAsync(show, tmdbId, default, scraper, token);
        RefreshArtworkPreviews(refreshTiles: true);
        return count;
    }
    internal static string? TvdbArtworkLanguage(string? value)
    {
        if (value?.Length != 3) return value;
        return System.Globalization.CultureInfo.GetCultures(System.Globalization.CultureTypes.NeutralCultures)
            .FirstOrDefault(culture => culture.ThreeLetterISOLanguageName == value)?.TwoLetterISOLanguageName ?? value;
    }
}
