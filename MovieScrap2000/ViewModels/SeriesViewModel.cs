using System.Collections.ObjectModel;
using System.Globalization;
using System.Text.Json;
using System.Xml.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MovieScrap2000;
using MovieScrap2000.TMDbScraper;
using VideoScrap2000.Localization;
using VideoScrap2000.Views;

namespace VideoScrap2000.ViewModels;

public partial class SeriesViewModel(MainViewModel main, Func<TMDbScraperManager>? createScraper = null,
    HttpClient? tvmazeClient = null) : ObservableObject
{
    private List<SeriesShowRow> allShows = [];
    private string? scannedPath;
    private TMDbScraperManager CreateScraper() => createScraper?.Invoke() ?? new TMDbScraperManager();
    private HttpClient EpisodeImageClient => tvmazeClient ?? TvmazeClient;
    private bool UseDemoData()
    {
        if (!main.IsDemo) return false;
        main.Status = Locale.T("Die Demo verwendet ausschließlich mitgelieferte Beispieldaten.");
        return true;
    }

    public ObservableCollection<SeriesShowRow> Shows { get; } = [];
    public ObservableCollection<SeriesSeason> Seasons { get; } = [];
    public ObservableCollection<SeriesEpisodeRow> Episodes { get; } = [];

    [ObservableProperty] private string query = "";
    [ObservableProperty] private int detailTabIndex;
    partial void OnDetailTabIndexChanged(int value) => SelectFirstDetailEntry();
    internal void SelectFirstDetailEntry()
    {
        switch (DetailTabIndex)
        {
            case 1: SelectedActor = null; SelectedActor = ShowEditor?.Actors.FirstOrDefault(); break;
            case 2: SelectedEpisode = Episodes.FirstOrDefault(); break;
            case 3: SelectedSeriesArtwork = null; SelectedSeriesArtwork = ArtworkTiles.FirstOrDefault(); break;
        }
    }
    [ObservableProperty] private SeriesShowRow? selectedShow;
    [ObservableProperty] private SeriesSeason? selectedSeason;
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasSelectedEpisode))]
    [NotifyPropertyChangedFor(nameof(SelectedEpisodePath))]
    private SeriesEpisodeRow? selectedEpisode;
    public bool HasSelectedEpisode => SelectedEpisode is not null;
    public string SelectedEpisodePath => main.IsDemo && SelectedEpisode is { } demoEpisode
        ? "Demo / TV Shows / " + Path.GetFileName(demoEpisode.Episode.VideoPath)
        : SelectedEpisode?.Episode.VideoPath ?? "";
    [ObservableProperty] private SeriesNfoEditor? showEditor;
    [ObservableProperty] private SeriesNfoEditor? episodeEditor;

    public bool NeedsScan => !string.IsNullOrWhiteSpace(main.SeriesPath) && scannedPath != main.SeriesPath;

    internal void ResetLibrary()
    {
        scannedPath = null;
        allShows = [];
        FilterShows(null);
    }

    public async Task ScanAsync()
    {
        if (string.IsNullOrWhiteSpace(main.SeriesPath))
        {
            main.Status = "Wähle zuerst einen Serienordner in den Einstellungen.";
            return;
        }

        var path = main.SeriesPath;
        await main.RunAsync(async token =>
        {
            main.Status = "Serien werden eingelesen …";
            var shows = await Task.Run(() => SeriesLibrary.Scan(path, token).Select(show => new SeriesShowRow(show)).ToList(), token);
            ArtworkPreviewCache.Shared.RefreshFileState();
            var previous = SelectedShow?.Show.FolderPath;
            allShows = shows;
            scannedPath = path;
            FilterShows(previous);
            main.Status = shows.Count == 0 ? "Keine Serien gefunden." : "Seriensammlung geladen.";
        }, reloadAfterCancellation: false);
    }

    partial void OnQueryChanged(string value) => FilterShows(SelectedShow?.Show.FolderPath);

    private void FilterShows(string? selectedPath)
    {
        var matches = allShows.Where(show => show.Title.Contains(Query.Trim(), StringComparison.CurrentCultureIgnoreCase)).ToArray();
        var desired = matches.ToHashSet();
        for (var index = Shows.Count - 1; index >= 0; index--)
            if (!desired.Contains(Shows[index])) Shows.RemoveAt(index);
        for (var index = 0; index < matches.Length; index++)
        {
            if (index < Shows.Count && ReferenceEquals(Shows[index], matches[index])) continue;
            var previous = Shows.IndexOf(matches[index]);
            if (previous < 0) Shows.Insert(index, matches[index]);
            else Shows.Move(previous, index);
        }
        SelectedShow = Shows.FirstOrDefault(show => show.Show.FolderPath == selectedPath) ?? Shows.FirstOrDefault();
    }

    partial void OnSelectedShowChanged(SeriesShowRow? value)
    {
        SelectedSeriesArtwork = null;
        ArtworkSourceWarning = "";
        SelectedActor = null;
        ShowEditor = value?.Editor;
        RefreshSeasons();
        RefreshArtworkPreviews(refreshTiles: true);
        SelectFirstDetailEntry();
        OnPropertyChanged(nameof(HasPendingEpisodeUpdates));
    }

    private void RefreshSeasons()
    {
        var previousSeason = SelectedSeason?.Number;
        Seasons.Clear();
        Seasons.Add(new(null, Locale.T("Alle Staffeln")));
        if (SelectedShow is { } show)
            foreach (var number in show.Episodes.Select(episode => episode.Editor.SeasonNumber).OfType<int>().Distinct().Order())
                Seasons.Add(new(number, number == 0 ? Locale.T("Spezialfolgen") : string.Format(Locale.T("Staffel {0}"), number)));
        SelectedSeason = Seasons.FirstOrDefault(season => season.Number == previousSeason) ?? Seasons[0];
    }

    partial void OnSelectedSeasonChanged(SeriesSeason? value)
    {
        OnPropertyChanged(nameof(SeriesArtworkTypes));
        if (!SeriesArtworkTypes.Contains(ArtworkSearchType)) ArtworkSearchType = ImageTypes.Poster;
        RefreshEpisodes();
        RefreshArtworkPreviews();
    }

    private void RefreshEpisodes()
    {
        var previous = SelectedEpisode;
        Episodes.Clear();
        if (SelectedShow is not { } show) { SelectedEpisode = null; return; }
        var season = SelectedSeason?.Number;
        foreach (var episode in show.Episodes.Where(episode => season is null || episode.Editor.SeasonNumber == season)
            .OrderBy(episode => episode.Editor.SeasonNumber ?? int.MaxValue)
            .ThenBy(episode => episode.Editor.EpisodeNumber ?? int.MaxValue).ThenBy(episode => episode.Title))
            Episodes.Add(episode);
        SelectedEpisode = previous is not null && Episodes.Contains(previous) ? previous : Episodes.FirstOrDefault();
    }

    partial void OnSelectedEpisodeChanged(SeriesEpisodeRow? value)
    {
        EpisodeEditor = value?.Editor;
        RefreshEpisodePicture();
        RefreshArtworkPreviews();
    }

    public async Task AddEpisodeAsync(string file)
    {
        if (SelectedShow is not { } show) return;
        var relative = Path.GetRelativePath(show.Show.FolderPath, file);
        if (Path.IsPathRooted(relative) || relative == ".." || relative.StartsWith(".." + Path.DirectorySeparatorChar, StringComparison.Ordinal)
            || !MovieManager.VideoExtensions.Contains(Path.GetExtension(file).ToLowerInvariant()))
        {
            main.Status = "Wähle eine Videodatei im Ordner der ausgewählten Serie.";
            return;
        }
        var comparer = OperatingSystem.IsWindows() ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal;
        var row = show.Episodes.FirstOrDefault(episode => comparer.Equals(episode.Episode.VideoPath, file));
        if (row is null)
        {
            row = new(await Task.Run(() => SeriesLibrary.ReadEpisode(file)));
            show.Episodes.Add(row);
        }
        if (!ReferenceEquals(show, SelectedShow)) return;
        RefreshSeasons();
        SelectedSeason = Seasons[0];
        SelectedEpisode = row;
    }

    [RelayCommand]
    private async Task SaveShowAsync() => await SaveChangesAsync();

    [RelayCommand]
    private async Task SaveEpisodeAsync() => await SaveChangesAsync();

    private async Task SaveChangesAsync()
    {
        if (SelectedShow is not { } show) return;
        if (string.IsNullOrWhiteSpace(show.Editor.Title)) { main.Status = "Bitte einen Titel eingeben."; return; }
        if (show.PendingEpisodeUpdates.Count > 0)
        {
            if (SelectedEpisode is { } current) show.PendingEpisodeUpdates.Add(current);
            await SaveEpisodeUpdatesAsync();
            RefreshSeasons();
            return;
        }
        if (SelectedEpisode is not { } episode)
        {
            await SaveAsync(show.Show.NfoPath, show.Editor, episode: false, show);
            return;
        }
        if (await SaveAsync(episode.Episode.NfoPath, episode.Editor, episode: true, show, episodeRow: episode))
        {
            SelectedShow?.PendingEpisodeUpdates.Remove(episode);
            OnPropertyChanged(nameof(HasPendingEpisodeUpdates));
            RefreshSeasons();
        }
    }

    private async Task<bool> SaveAsync(string path, SeriesNfoEditor editor, bool episode, SeriesShowRow? show = null,
        SeriesEpisodeRow? episodeRow = null)
    {
        if (string.IsNullOrWhiteSpace(editor.Title)) { main.Status = "Bitte einen Titel eingeben."; return false; }
        if (episode && (editor.SeasonNumber is null || editor.EpisodeNumber is null))
        {
            main.Status = "Bitte gültige Staffel- und Episodennummern eingeben.";
            return false;
        }
        var data = editor.ToXml(episode);
        var saved = false;
        await main.RunAsync(async token =>
        {
            main.Status = "Metadaten werden gespeichert …";
            var fields = episode ? SeriesNfoEditor.EpisodeFields : SeriesNfoEditor.ShowFields;
            if (show is not null) await SaveShowFilesAsync(show, token);
            if (episode || show is null)
                await Task.Run(() => SeriesLibrary.SaveAsync(path, data, fields, token), token);
            editor.HasNfo = true;
            if (episodeRow is not null) await SaveEpisodePictureAsync(episodeRow, token);
            saved = true;
            main.Status = episode ? "Episode gespeichert." : "Serie gespeichert.";
        }, reloadAfterCancellation: false);
        return saved;
    }

    private async Task SaveShowFilesAsync(SeriesShowRow show, CancellationToken token)
    {
        var data = show.Editor.ToXml(isEpisode: false);
        await Task.Run(() => SeriesLibrary.SaveAsync(show.Show.NfoPath, data, SeriesNfoEditor.ShowFields, token), token);
        show.Editor.HasNfo = true;
        var pending = show.Artwork.Where(item => item.Value.PendingData is not null).ToArray();
        try
        {
            foreach (var (name, picture) in pending)
            {
                var file = await Task.Run(() => ArtworkFiles.SaveNamedAsync(show.Show.FolderPath, name, picture.PendingData!, token), token);
                show.Artwork[name] = new(file);
            }
        }
        finally { if (pending.Length > 0) ArtworkPreviewCache.Shared.InvalidateFolder(show.Show.FolderPath); }
        RefreshArtworkPreviews(refreshTiles: true);
    }

    public async Task<IReadOnlyList<SearchTv>> FindShowsAsync(string title)
    {
        if (UseDemoData()) return [];
        List<SearchTv> results = [];
        var query = MovieNameParser.ParseSearchTitle(title).Title;
        if (query.Length == 0) return results;
        await main.RunAsync(async token =>
        {
            main.Status = "TMDb wird durchsucht …";
            results = (await SearchShowTitlesAsync(query, token)).ToList();
            main.Status = results.Count switch { 0 => "Kein Treffer", 1 => "Ein Treffer", _ => $"{results.Count} Treffer" };
        }, reloadAfterCancellation: false);
        return results;
    }

    internal async Task<IReadOnlyList<SearchTv>> SearchShowTitlesAsync(string title, CancellationToken token)
    {
        if (UseDemoData()) return [];
        var query = MovieNameParser.ParseSearchTitle(title).Title;
        if (query.Length == 0) return [];
        using var scraper = CreateScraper();
        return await scraper.SearchSeriesAsync(query, main.MetadataLanguage, token);
    }

    public async Task ApplyShowMatchAsync(int id)
    {
        if (UseDemoData()) return;
        if (SelectedShow is not { } show) return;
        await main.RunAsync(async token =>
        {
            main.Status = "Seriendetails werden geladen …";
            using var scraper = CreateScraper();
            var result = await scraper.GetSeriesDetailsAsync(id, main.MetadataLanguage, token);
            token.ThrowIfCancellationRequested();
            SelectedActor = null;
            show.Editor.ApplyShow(result, id, main.MetadataLanguage);
            if (DetailTabIndex == 1) SelectFirstDetailEntry();
            var count = await DownloadArtworkAsync(show, id, result, scraper, token);
            main.Status = count > 0 ? "Seriendaten und Bilder übernommen. Speichere, um sie abzulegen."
                : "Seriendaten übernommen. Speichere, um die NFO zu schreiben.";
            if (ArtworkSourceWarning.Length > 0) main.Status += " " + ArtworkSourceWarning;
        }, reloadAfterCancellation: false);
    }

    public async Task FindEpisodeAsync()
    {
        if (UseDemoData()) return;
        if (SelectedEpisode is not { } row || SelectedShow is not { } show) return;
        var episode = row.Editor;
        if (!int.TryParse(ShowEditor?.TmdbId, out var id) || id <= 0)
        {
            main.Status = "Ordne die Serie zuerst einem TMDB-Treffer zu.";
            return;
        }
        if (episode.SeasonNumber is not { } season || episode.EpisodeNumber is not { } number)
        {
            main.Status = "Bitte gültige Staffel- und Episodennummern eingeben.";
            return;
        }
        await main.RunAsync(async token =>
        {
            main.Status = "Episodendetails werden geladen …";
            using var scraper = CreateScraper();
            try
            {
                var result = await scraper.GetEpisodeDetailsAsync(id, season, number, main.MetadataLanguage, token);
                token.ThrowIfCancellationRequested();
                episode.ApplyEpisode(result);
                await DownloadEpisodePictureAsync(row, result, scraper, CreateTvmazeLookup(show, token), token);
                main.Status = "Episodendaten übernommen. Speichere, um die NFO zu schreiben.";
            }
            catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                main.Status = "Kein Treffer";
            }
        }, reloadAfterCancellation: false);
    }

    public bool HasPendingEpisodeUpdates => SelectedShow?.PendingEpisodeUpdates.Count > 0;

    [RelayCommand]
    private async Task FindAllEpisodesAsync()
    {
        if (UseDemoData()) return;
        if (SelectedShow is not { } show) return;
        if (!int.TryParse(show.Editor.TmdbId, out var id) || id <= 0)
        {
            main.Status = "Ordne die Serie zuerst einem TMDB-Treffer zu.";
            return;
        }
        var groups = show.Episodes.Where(row => row.Editor.SeasonNumber.HasValue && row.Editor.EpisodeNumber.HasValue)
            .GroupBy(row => row.Editor.SeasonNumber!.Value).ToArray();
        if (groups.Length == 0) { main.Status = "Bitte gültige Staffel- und Episodennummern eingeben."; return; }

        await main.RunAsync(async token =>
        {
            main.Status = "Episodendetails werden geladen …";
            using var scraper = CreateScraper();
            var tvmaze = CreateTvmazeLookup(show, token);
            var loaded = 0;
            try
            {
                for (var index = 0; index < groups.Length; index++)
                {
                    token.ThrowIfCancellationRequested();
                    var group = groups[index];
                    JsonElement season;
                    try { season = await scraper.GetSeasonDetailsAsync(id, group.Key, main.MetadataLanguage, token); }
                    catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
                    {
                        main.Progress = (index + 1) * 100 / groups.Length;
                        continue;
                    }
                    var episodes = SeriesNfoEditor.Array(season, "episodes")
                        .ToLookup(item => SeriesNfoEditor.Text(item, "episode_number"), StringComparer.Ordinal);
                    foreach (var row in group)
                    {
                        token.ThrowIfCancellationRequested();
                        var number = row.Editor.EpisodeNumber!.Value.ToString(CultureInfo.InvariantCulture);
                        var result = episodes[number].FirstOrDefault();
                        if (result.ValueKind != JsonValueKind.Object) continue;
                        row.Editor.ApplyEpisode(result);
                        show.PendingEpisodeUpdates.Add(row);
                        await DownloadEpisodePictureAsync(row, result, scraper, tvmaze, token);
                        loaded++;
                    }
                    main.Progress = (index + 1) * 100 / groups.Length;
                }
                main.Status = $"{loaded} Episoden übernommen, {show.Episodes.Count - loaded} übersprungen.";
            }
            finally { OnPropertyChanged(nameof(HasPendingEpisodeUpdates)); }
        }, reloadAfterCancellation: false);
    }

    private async Task SaveEpisodeUpdatesAsync()
    {
        if (SelectedShow is not { } show || show.PendingEpisodeUpdates.Count == 0) return;
        var updates = show.PendingEpisodeUpdates.ToArray();
        await main.RunAsync(async token =>
        {
            main.Status = "Episoden werden gespeichert …";
            await SaveShowFilesAsync(show, token);
            var saved = 0;
            try
            {
                for (var index = 0; index < updates.Length; index++)
                {
                    token.ThrowIfCancellationRequested();
                    var row = updates[index];
                    if (row.Editor.SeasonNumber.HasValue && row.Editor.EpisodeNumber.HasValue && !string.IsNullOrWhiteSpace(row.Editor.Title))
                    {
                        var data = row.Editor.ToXml(isEpisode: true);
                        try
                        {
                            await Task.Run(() => SeriesLibrary.SaveAsync(row.Episode.NfoPath, data, SeriesNfoEditor.EpisodeFields, token), token);
                            row.Editor.HasNfo = true;
                            await SaveEpisodePictureAsync(row, token);
                            show.PendingEpisodeUpdates.Remove(row);
                            saved++;
                        }
                        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or System.Xml.XmlException or System.Text.DecoderFallbackException)
                        {
                            System.Diagnostics.Trace.WriteLine(ex);
                        }
                    }
                    main.Progress = (index + 1) * 100 / updates.Length;
                }
                main.Status = $"{saved} Episoden gespeichert, {updates.Length - saved} übersprungen.";
            }
            finally { OnPropertyChanged(nameof(HasPendingEpisodeUpdates)); }
        }, reloadAfterCancellation: false);
    }
}

public sealed record SeriesSeason(int? Number, string Name);

public sealed class SeriesShowRow(SeriesShow show) : ObservableObject
{
    public SeriesShow Show { get; } = show;
    public SeriesNfoEditor Editor { get; } = new(show.Metadata, show.HasNfo);
    public ObservableCollection<SeriesEpisodeRow> Episodes { get; } = new(show.Episodes.Select(episode => new SeriesEpisodeRow(episode)));
    internal HashSet<SeriesEpisodeRow> PendingEpisodeUpdates { get; } = [];
    public Dictionary<string, SeriesPicture> Artwork { get; } = ReadArtwork(show.FolderPath);
    public string Title => Editor.Title;
    public SeriesPicture? Poster => Artwork.GetValueOrDefault("poster") ?? FirstSeasonPicture("poster");

    internal SeriesPicture? FirstSeasonPicture(string type) => Episodes.Select(episode => episode.Editor.SeasonNumber)
        .Where(season => season.HasValue).Distinct().OrderBy(season => season == 0 ? int.MaxValue : season)
        .Select(season => Artwork.GetValueOrDefault(SeriesViewModel.SeasonArtworkName(season, type)))
        .FirstOrDefault(picture => picture is not null)
        ?? Artwork.Where(item => item.Key.StartsWith("season", StringComparison.OrdinalIgnoreCase)
                && item.Key.EndsWith("-" + type, StringComparison.OrdinalIgnoreCase))
            .OrderBy(item => item.Key, StringComparer.OrdinalIgnoreCase).Select(item => item.Value).FirstOrDefault();

    internal void RefreshPoster() => OnPropertyChanged(nameof(Poster));

    private static Dictionary<string, SeriesPicture> ReadArtwork(string folder)
    {
        var artwork = new Dictionary<string, SeriesPicture>(StringComparer.OrdinalIgnoreCase);
        try
        {
            foreach (var path in Directory.EnumerateFiles(folder))
            {
                if (Path.GetExtension(path).ToLowerInvariant() is not (".jpg" or ".jpeg" or ".png" or ".webp")) continue;
                var stem = Path.GetFileNameWithoutExtension(path);
                if (stem.Equals("tvshow-fanart", StringComparison.OrdinalIgnoreCase)) stem = "fanart";
                if (stem.Equals("tvshow-poster", StringComparison.OrdinalIgnoreCase)) stem = "poster";
                if (stem.StartsWith("season00-", StringComparison.OrdinalIgnoreCase)) stem = "season-specials-" + stem[9..];
                var type = stem.ToLowerInvariant();
                if (type is "poster" or "fanart" or "banner" or "clearlogo" or "clearart" or "landscape" or "characterart" || type.StartsWith("season", StringComparison.Ordinal)
                    && (type.EndsWith("-banner") || type.EndsWith("-poster") || type.EndsWith("-fanart") || type.EndsWith("-landscape")))
                    artwork.TryAdd(stem, new(path));
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { System.Diagnostics.Trace.WriteLine(ex); }
        return artwork;
    }
}

public sealed class SeriesEpisodeRow(SeriesEpisode episode)
{
    internal SeriesPicture? PendingPicture { get; set; }
    public SeriesEpisode Episode { get; } = episode;
    public SeriesNfoEditor Editor { get; } = new(episode.Metadata, episode.HasNfo);
    public string Title => Editor.Title;
}

public partial class SeriesNfoEditor : ObservableObject
{
    internal static readonly string[] ShowFields = ["title", "originaltitle", "plot", "premiered", "status", "genre", "studio", "country", "tag", "mpaa", "uniqueid", "actor"];
    internal static readonly string[] EpisodeFields = ["title", "plot", "season", "episode", "aired", "runtime", "director", "credits", "uniqueid"];

    [ObservableProperty] private string title;
    [ObservableProperty] private string originalTitle;
    [ObservableProperty] private string plot;
    [ObservableProperty] private string date;
    [ObservableProperty] private string status;
    [ObservableProperty] private string genres;
    [ObservableProperty] private string studios;
    [ObservableProperty] private string countries;
    [ObservableProperty] private string tags;
    [ObservableProperty] private string certification;
    [ObservableProperty] private string imdbId;
    [ObservableProperty] private string tmdbId;
    [ObservableProperty] private string tvdbId;
    [ObservableProperty] private string season;
    [ObservableProperty] private string episode;
    [ObservableProperty] private string runtime;
    [ObservableProperty] private string directors;
    [ObservableProperty] private string writers;
    [ObservableProperty] private bool hasNfo;
    public ObservableCollection<ActorEditor> Actors { get; } = [];
    private readonly Dictionary<ActorEditor, XElement> originalActors = [];

    public int? SeasonNumber => ReadNumber(Season);
    public int? EpisodeNumber => ReadNumber(Episode);
    public string EpisodeCode => SeasonNumber is { } s && EpisodeNumber is { } e ? $"S{s:00}E{e:00}" : "—";

    partial void OnSeasonChanged(string value) => OnPropertyChanged(nameof(EpisodeCode));
    partial void OnEpisodeChanged(string value) => OnPropertyChanged(nameof(EpisodeCode));

    public SeriesNfoEditor(XElement metadata, bool exists)
    {
        string Read(string name) => (string?)metadata.Element(name) ?? "";
        string ReadList(string name) => string.Join("; ", metadata.Elements(name).Select(element => element.Value));
        string ReadId(string type) => metadata.Elements("uniqueid").FirstOrDefault(element =>
            string.Equals((string?)element.Attribute("type"), type, StringComparison.OrdinalIgnoreCase))?.Value ?? "";
        title = Read("title"); originalTitle = Read("originaltitle"); plot = Read("plot");
        date = Read(metadata.Name == "tvshow" ? "premiered" : "aired"); status = Read("status");
        genres = ReadList("genre"); studios = ReadList("studio"); certification = Read("mpaa");
        countries = ReadList("country"); tags = ReadList("tag");
        imdbId = ReadId("imdb"); tmdbId = ReadId("tmdb"); tvdbId = ReadId("tvdb");
        season = Read("season"); episode = Read("episode"); runtime = Read("runtime");
        directors = ReadList("director"); writers = ReadList("credits"); hasNfo = exists;
        foreach (var element in metadata.Elements("actor"))
        {
            var actor = new ActorEditor
            {
                Name = (string?)element.Element("name") ?? "",
                Role = (string?)element.Element("role") ?? "",
                Thumb = (string?)element.Element("thumb") ?? "",
                TmdbId = ReadNumber((string?)element.Element("tmdbid") ?? ""),
                Order = ReadNumber((string?)element.Element("order") ?? "") ?? Actors.Count
            };
            originalActors[actor] = new(element);
            Actors.Add(actor);
        }
    }

    internal static string? Text(JsonElement value, string name) => value.ValueKind == JsonValueKind.Object && value.TryGetProperty(name, out var field)
        && field.ValueKind is JsonValueKind.String or JsonValueKind.Number ? field.ToString() : null;

    internal static IEnumerable<JsonElement> Array(JsonElement value, string name) => value.ValueKind == JsonValueKind.Object && value.TryGetProperty(name, out var array)
        && array.ValueKind == JsonValueKind.Array ? array.EnumerateArray() : [];

    public void ApplyShow(JsonElement data, int id, string language)
    {
        Title = Text(data, "name") ?? Title;
        OriginalTitle = Text(data, "original_name") ?? OriginalTitle;
        if (Text(data, "overview") is { Length: > 0 } plot) Plot = plot;
        Date = Text(data, "first_air_date") ?? Date;
        Status = Text(data, "status") ?? Status;
        TmdbId = id.ToString(CultureInfo.InvariantCulture);
        if (data.TryGetProperty("external_ids", out var ids))
        {
            ImdbId = Text(ids, "imdb_id") ?? ImdbId;
            TvdbId = Text(ids, "tvdb_id") ?? TvdbId;
        }
        var genres = Array(data, "genres").Select(item => Text(item, "name")).OfType<string>().ToArray();
        if (genres.Length > 0) Genres = string.Join("; ", genres);
        var studios = Array(data, "production_companies").Select(item => Text(item, "name")).OfType<string>().ToArray();
        if (studios.Length > 0) Studios = string.Join("; ", studios);
        var countries = Array(data, "production_countries").Select(item => Text(item, "name")).OfType<string>().ToArray();
        if (countries.Length > 0) Countries = string.Join("; ", countries);
        var region = new RegionInfo(LanguagePreferences.ResolveContent(language)).TwoLetterISORegionName;
        if (data.TryGetProperty("content_ratings", out var ratings))
            Certification = Array(ratings, "results").Where(item => Text(item, "iso_3166_1") == region)
                .Select(item => Text(item, "rating")).FirstOrDefault(value => !string.IsNullOrWhiteSpace(value)) ?? Certification;
        if (data.TryGetProperty("aggregate_credits", out var credits) || data.TryGetProperty("credits", out credits))
        {
            var actors = ReadCast(credits);
            if (actors.Length > 0)
            {
                Actors.Clear();
                originalActors.Clear();
                foreach (var actor in actors) Actors.Add(actor);
            }
        }
    }

    internal static ActorEditor[] ReadCast(JsonElement credits) => Array(credits, "cast")
        .Where(actor => !string.IsNullOrWhiteSpace(Text(actor, "name")))
        .Select((actor, index) => new ActorEditor
        {
            Name = Text(actor, "name") ?? "",
            Role = Text(actor, "character") ?? string.Join(" / ", Array(actor, "roles")
                .Select(role => Text(role, "character")).Where(role => !string.IsNullOrWhiteSpace(role)).Distinct()),
            TmdbId = ReadNumber(Text(actor, "id") ?? ""), Order = index,
            Thumb = Text(actor, "profile_path") is { Length: > 0 } profile ? "https://image.tmdb.org/t/p/w185" + profile : ""
        }).ToArray();

    public void ApplyEpisode(JsonElement data)
    {
        Title = Text(data, "name") ?? Title;
        if (Text(data, "overview") is { Length: > 0 } plot) Plot = plot;
        Date = Text(data, "air_date") ?? Date;
        Runtime = Text(data, "runtime") ?? Runtime;
        TmdbId = Text(data, "id") ?? TmdbId;
        var crew = Array(data, "crew").ToArray();
        var directors = crew.Where(person => Text(person, "job") == "Director").Select(person => Text(person, "name")).OfType<string>().ToArray();
        var writers = crew.Where(person => Text(person, "department") == "Writing").Select(person => Text(person, "name")).OfType<string>().Distinct().ToArray();
        if (directors.Length > 0) Directors = string.Join("; ", directors);
        if (writers.Length > 0) Writers = string.Join("; ", writers);
    }

    private static int? ReadNumber(string value) => int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out var number) ? number : null;

    public XElement ToXml(bool isEpisode)
    {
        var root = new XElement(isEpisode ? "episodedetails" : "tvshow", new XElement("title", Title.Trim()));
        void Add(string name, string value) { if (!string.IsNullOrWhiteSpace(value)) root.Add(new XElement(name, value.Trim())); }
        void AddList(string name, string value)
        {
            foreach (var item in value.Split(';', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)) Add(name, item);
        }
        Add("plot", Plot);
        if (isEpisode)
        {
            Add("season", Season); Add("episode", Episode); Add("aired", Date); Add("runtime", Runtime);
            AddList("director", Directors); AddList("credits", Writers);
        }
        else
        {
            Add("originaltitle", OriginalTitle); Add("premiered", Date); Add("status", Status); Add("mpaa", Certification);
            AddList("genre", Genres); AddList("studio", Studios);
            AddList("country", Countries); AddList("tag", Tags);
        }
        foreach (var (type, value) in new[] { ("imdb", ImdbId), ("tmdb", TmdbId), ("tvdb", TvdbId) })
            root.Add(new XElement("uniqueid", new XAttribute("type", type), value.Trim()));
        if (!isEpisode)
        {
            var index = 0;
            foreach (var actor in Actors.Where(actor => !string.IsNullOrWhiteSpace(actor.Name)))
            {
                var node = originalActors.TryGetValue(actor, out var original) ? new XElement(original) : new XElement("actor");
                node.SetElementValue("name", actor.Name.Trim());
                node.SetElementValue("role", actor.Role);
                node.SetElementValue("thumb", string.IsNullOrWhiteSpace(actor.Thumb) ? null : actor.Thumb);
                node.SetElementValue("tmdbid", actor.TmdbId);
                node.SetElementValue("order", index++);
                root.Add(node);
            }
        }
        return root;
    }
}
