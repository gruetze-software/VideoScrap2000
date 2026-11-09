using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using MovieScrap2000.TMDbScraper;

namespace MovieScrap2000;

public sealed class TvdbScraper : IDisposable
{
    private readonly HttpClient client;
    private readonly ScraperSettings settings;
    private string? bearer;
    public TvdbScraper(ScraperSettings settings, HttpClient? client = null)
    {
        this.settings = settings.Copy();
        this.client = client ?? new HttpClient { Timeout = TimeSpan.FromSeconds(20) };
        this.client.BaseAddress = new Uri("https://api4.thetvdb.com/v4/");
    }
    public static string Language(string language) => CultureInfo.GetCultureInfo(LanguagePreferences.ResolveContent(language)).ThreeLetterISOLanguageName;
    private static string? Text(JsonElement data, string name) => data.ValueKind == JsonValueKind.Object && data.TryGetProperty(name, out var value) && value.ValueKind is JsonValueKind.Number or JsonValueKind.String && !string.IsNullOrWhiteSpace(value.ToString()) ? value.ToString() : null;
    private static IEnumerable<JsonElement> Array(JsonElement data, string name) => data.ValueKind == JsonValueKind.Object && data.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.Array ? value.EnumerateArray() : [];
    private static JsonElement Property(JsonElement data, string name) => data.ValueKind == JsonValueKind.Object && data.TryGetProperty(name, out var value) ? value : default;
    private static string? CountryCode(string? value)
    {
        if (value?.Length != 3) return value?.ToUpperInvariant();
        foreach (var culture in CultureInfo.GetCultures(CultureTypes.SpecificCultures))
        {
            var region = new RegionInfo(culture.Name);
            if (region.ThreeLetterISORegionName.Equals(value, StringComparison.OrdinalIgnoreCase)) return region.TwoLetterISORegionName;
        }
        return value;
    }
    private async Task<string> LoginAsync(CancellationToken token)
    {
        if (!settings.EnableTvdb || string.IsNullOrWhiteSpace(settings.TvdbApiKey)) throw new InvalidOperationException("Bitte TheTVDB in den Scraper-Einstellungen aktivieren und den Projekt-API-Key eintragen.");
        using var response = await client.PostAsJsonAsync("login", new { apikey = settings.TvdbApiKey, pin = string.IsNullOrWhiteSpace(settings.TvdbPin) ? null : settings.TvdbPin }, token);
        Check(response);
        var root = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: token);
        return Text(Property(root, "data"), "token") ?? throw new InvalidOperationException("TheTVDB hat keinen Zugangstoken geliefert.");
    }
    private static void Check(HttpResponseMessage response)
    {
        if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
            throw new HttpRequestException("TheTVDB-Zugang abgelehnt. Bitte Projekt-API-Key und gegebenenfalls Abo-PIN prüfen.", null, response.StatusCode);
        response.EnsureSuccessStatusCode();
    }
    private async Task<JsonElement> ReadAsync(string path, CancellationToken token)
    {
        bearer ??= await LoginAsync(token);
        for (var attempt = 0; ; attempt++)
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, path);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearer);
            using var response = await client.SendAsync(request, token);
            if (response.StatusCode == HttpStatusCode.Unauthorized && attempt == 0) { bearer = await LoginAsync(token); continue; }
            Check(response);
            return await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: token);
        }
    }
    public async Task<IReadOnlyList<SearchTv>> SearchAsync(string query, string language, CancellationToken token)
    {
        var root = await ReadAsync($"search?type=series&query={Uri.EscapeDataString(query)}&limit=50", token);
        var code = Language(language);
        return Array(root, "data").Where(item => int.TryParse(Text(item, "tvdb_id"), out var id) && id > 0)
            .Select(item => new SearchTv
            {
                Id = int.Parse(Text(item, "tvdb_id")!, CultureInfo.InvariantCulture),
                Name = Text(Property(item, "translations"), code) ?? Text(item, "name"),
                PosterPath = Text(item, "image_url"),
                FirstAirDate = DateTime.TryParse(Text(item, "first_air_time") ?? Text(item, "year"), CultureInfo.InvariantCulture, DateTimeStyles.None, out var date) ? date : int.TryParse(Text(item, "year"), out var year) && year is >= 1 and <= 9999 ? new DateTime(year, 1, 1) : null
            }).ToArray();
    }
    private async Task<JsonElement> TranslationAsync(string type, int id, string language, CancellationToken token)
    {
        try { return Property(await ReadAsync($"{type}/{id}/translations/{Language(language)}", token), "data"); }
        catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.NotFound) { return default; }
    }
    public async Task<JsonElement> GetShowAsync(int id, string language, CancellationToken token)
    {
        var data = Property(await ReadAsync($"series/{id}/extended", token), "data");
        var translated = await TranslationAsync("series", id, language, token);
        var types = Array(await ReadAsync("artwork/types", token), "data").ToDictionary(item => Text(item, "id") ?? "", item => Text(item, "name") ?? "");
        var seasons = Array(data, "seasons").Where(item => Text(Property(item, "type"), "type") is null or "official")
            .GroupBy(item => Text(item, "id") ?? "").ToDictionary(group => group.Key, group => Text(group.First(), "number"));
        string? ArtworkType(JsonElement image)
        {
            var type = types.GetValueOrDefault(Text(image, "type") ?? "", "").Replace(" ", "").ToLowerInvariant();
            return type.Contains("poster") ? ImageTypes.Poster : type.Contains("banner") ? ImageTypes.Banner
                : type.Contains("background") || type.Contains("fanart") ? ImageTypes.Fanart
                : type.Contains("clearlogo") ? ImageTypes.ClearLogo : type.Contains("clearart") ? ImageTypes.ClearArt : null;
        }
        var pictures = Array(data, "artworks").Where(image => ArtworkType(image) is not null)
            .Where(image => Text(image, "seasonId") is null or "0" || seasons.ContainsKey(Text(image, "seasonId")!))
            .Select(image => new TvdbArtwork(ArtworkType(image)!, int.TryParse(seasons.GetValueOrDefault(Text(image, "seasonId") ?? ""), out var season) ? season : null,
                Text(image, "image") ?? "", Text(image, "language"))).ToList();
        foreach (var item in Array(data, "seasons"))
            if (seasons.ContainsKey(Text(item, "id") ?? "") && int.TryParse(Text(item, "number"), out var number) && Text(item, "image") is { } image
                && !pictures.Any(picture => picture.Season == number && picture.Type == ImageTypes.Poster))
                pictures.Add(new(ImageTypes.Poster, number, image, null));
        if (!pictures.Any(image => image.Type == ImageTypes.Poster && image.Season is null) && Text(data, "image") is { Length: > 0 } poster)
            pictures.Add(new(ImageTypes.Poster, null, poster, null));
        string? Remote(string source) => Array(data, "remoteIds").Where(item => Text(item, "sourceName")?.Contains(source, StringComparison.OrdinalIgnoreCase) == true).Select(item => Text(item, "id")).FirstOrDefault();
        return JsonSerializer.SerializeToElement(new
        {
            _provider = "TheTVDB", tvdb_id = id,
            name = Text(translated, "name") ?? Text(data, "name"), original_name = Text(data, "name"),
            overview = Text(translated, "overview") ?? Text(data, "overview"), first_air_date = Text(data, "firstAired"),
            status = Text(Property(data, "status"), "name"),
            external_ids = new { imdb_id = Remote("IMDB"), tvdb_id = id, tmdb_id = Remote("TheMovieDB") },
            genres = Array(data, "genres").ToArray(), production_companies = Array(data, "companies").ToArray(),
            production_countries = new[] { new { name = Text(data, "originalCountry") ?? Text(data, "country") } },
            credits = new { cast = Array(data, "characters").Where(item => Text(item, "peopleType") is null or "Actor").Select(item => new { name = Text(item, "personName"), character = Text(item, "name"), profile_url = Text(item, "personImgURL") }).ToArray() },
            content_ratings = new { results = Array(data, "contentRatings").Select(item => new { iso_3166_1 = CountryCode(Text(item, "country")), rating = Text(item, "name") }).ToArray() },
            tvdb_artworks = pictures, tvdb_seasons = Array(data, "seasons").ToArray(),
            tvdb_image = Text(data, "image")
        });
    }
    public Task<JsonElement> GetSeasonAsync(int id, int season, string language, CancellationToken token) => GetEpisodesAsync(id, season, null, language, token);
    private async Task<JsonElement> GetEpisodesAsync(int id, int season, int? number, string language, CancellationToken token)
    {
        var episodes = new List<JsonElement>();
        var page = 0;
        while (true)
        {
            var root = await ReadAsync($"series/{id}/episodes/official?season={season}&page={page}" + (number.HasValue ? $"&episodeNumber={number.Value}" : ""), token);
            foreach (var episode in Array(Property(root, "data"), "episodes"))
            {
                if (Text(episode, "seasonNumber") != season.ToString(CultureInfo.InvariantCulture) || !int.TryParse(Text(episode, "id"), out var episodeId)) continue;
                var translated = await TranslationAsync("episodes", episodeId, language, token);
                episodes.Add(JsonSerializer.SerializeToElement(new { _provider = "TheTVDB", tvdb_id = episodeId,
                    name = Text(translated, "name") ?? Text(episode, "name"), overview = Text(translated, "overview") ?? Text(episode, "overview"),
                    air_date = Text(episode, "aired"), runtime = Text(episode, "runtime"), episode_number = Text(episode, "number"),
                    still_url = Text(episode, "image") }));
            }
            var next = Text(Property(root, "links"), "next");
            if (string.IsNullOrWhiteSpace(next)) break;
            // Construct requests ourselves so bearer credentials can never follow an arbitrary pagination URL.
            if (++page > 1000) throw new InvalidOperationException("TheTVDB lieferte zu viele Ergebnisseiten.");
        }
        return JsonSerializer.SerializeToElement(new { episodes });
    }
    public async Task<JsonElement> GetEpisodeAsync(int id, int season, int number, string language, CancellationToken token)
    {
        var data = await GetEpisodesAsync(id, season, number, language, token);
        var result = Array(data, "episodes").FirstOrDefault(item => Text(item, "episode_number") == number.ToString(CultureInfo.InvariantCulture));
        if (result.ValueKind != JsonValueKind.Object) throw new HttpRequestException("Kein Treffer", null, HttpStatusCode.NotFound);
        return result;
    }
    public void Dispose() => client.Dispose();
}

public sealed record TvdbArtwork(string Type, int? Season, string Url, string? Language);
