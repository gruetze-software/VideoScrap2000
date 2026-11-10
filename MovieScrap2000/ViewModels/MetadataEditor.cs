using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Avalonia.Media.Imaging;
using MovieScrap2000;
using VideoScrap2000.Views;

namespace VideoScrap2000.ViewModels;

public partial class MainViewModel
{
    [ObservableProperty] private string sortTitle = "";
    [ObservableProperty] private DateTimeOffset? premiere;
    [ObservableProperty] private string outline = "";
    [ObservableProperty] private string credits = "";
    [ObservableProperty] private string tmdb = "";
    [ObservableProperty] private string collection = "";
    [ObservableProperty] private string collectionId = "";
    [ObservableProperty] private string certification = "";
    [ObservableProperty] private string countries = "";
    [ObservableProperty] private string studios = "";
    [ObservableProperty] private string tags = "";
    [ObservableProperty] private string runtime = "";
    [ObservableProperty] private string rating = "";
    [ObservableProperty] private string technicalDetails = "";
    [ObservableProperty] private ActorEditor? selectedActor;
    [ObservableProperty] private Bitmap? actorPhoto;
    [ObservableProperty] private bool isActorPhotoLoading;
    public ObservableCollection<ActorEditor> Actors { get; } = [];
    public ObservableCollection<ActorEditor> SelectedActors { get; } = [];
    private CancellationTokenSource? actorLoading;
    private int actorVersion;
    private Task actorPhotoTask = Task.CompletedTask;
    private ArtworkPreviewCache.Lease? actorPhotoLease;
    [ObservableProperty] private string actorPhotoResolution = "";

    public bool HasSelectedActor => SelectedActor is not null;
    partial void OnSelectedActorChanged(ActorEditor? value)
    {
        OnPropertyChanged(nameof(HasSelectedActor));
        actorPhotoTask = LoadActorPhotoAsync(value);
    }
    [RelayCommand] private void AddActor()
    {
        var actor = new ActorEditor { Name = Localization.Locale.T("Neuer Darsteller"), Order = Actors.Count };
        Actors.Add(actor); SelectedActors.Clear(); SelectedActors.Add(actor);
    }
    private bool CanRemoveActors() => CanEdit && SelectedActors.Count > 0;
    public void RemoveActorEntry(ActorEditor actor)
    {
        if (!CanEdit) return;
        var index = Actors.IndexOf(actor);
        if (index < 0) return;
        SelectedActors.Clear(); Actors.Remove(actor);
        if (Actors.Count > 0) SelectedActors.Add(Actors[Math.Min(index, Actors.Count - 1)]);
    }
    [RelayCommand(CanExecute = nameof(CanRemoveActors))] private void RemoveActor()
    {
        if (!CanRemoveActors()) return;
        var selected = SelectedActors.ToArray();
        var next = selected.Min(Actors.IndexOf);
        SelectedActors.Clear();
        foreach (var actor in selected) Actors.Remove(actor);
        if (Actors.Count > 0) SelectedActors.Add(Actors[Math.Clamp(next, 0, Actors.Count - 1)]);
    }

    public async Task<string?> GetActorOpenPathAsync(ActorEditor actor)
    {
        SelectedActor = actor;
        await actorPhotoTask;
        if (actor.ThumbData is not { Length: > 0 } bytes) return null;
        return await new ArtworkViewModel("Actor", "", null!, bytes).GetOpenPathAsync();
    }

    private void LoadExtendedMetadata(Movie nfo)
    {
        SortTitle = nfo.SortTitle ?? "";
        Premiere = DateTime.TryParse(nfo.Premiered ?? nfo.ReleaseDate, CultureInfo.InvariantCulture, DateTimeStyles.None, out var date) ? new DateTimeOffset(date) : null;
        Outline = nfo.Outline ?? ""; Credits = nfo.DisplayCredits ?? "";
        Tmdb = nfo.TmdbId ?? nfo.TmdbIdFromUnique?.ToString() ?? "";
        Collection = nfo.MovieSet ?? ""; CollectionId = nfo.TmdbCollectionId ?? nfo.TmdbCollectionIdFromUnique ?? "";
        Certification = nfo.Mpaa ?? nfo.Certification ?? "";
        Countries = nfo.DisplayCountries ?? ""; Studios = nfo.DisplayStudios ?? "";
        Tags = string.Join("; ", nfo.Tags); Runtime = nfo.Runtime > 0 ? nfo.Runtime.ToString() : ""; Rating = nfo.RatingString ?? "";
        SelectedActors.Clear(); SelectedActor = null; Actors.Clear();
        foreach (var actor in nfo.Actors) Actors.Add(ActorEditor.From(actor));
        if (DetailTabIndex == 1) SelectFirstDetailEntry();
        MediaInfo = nfo.FileInfo;
        TechnicalDetails = Localization.Locale.T(DescribeStreams(nfo.FileInfo));
    }

    private void ApplyExtendedMetadata(Movie nfo)
    {
        if (Tmdb.Length > 0 && (!int.TryParse(Tmdb, out var id) || id <= 0)) throw new InvalidOperationException("Bitte eine gültige TMDb-ID eingeben oder das Feld leeren.");
        if (Runtime.Length > 0 && (!int.TryParse(Runtime, out var minutes) || minutes < 0)) throw new InvalidOperationException("Die Laufzeit muss eine positive Minutenzahl sein.");
        if (Rating.Length > 0 && (!decimal.TryParse(Rating.Replace(',', '.'), NumberStyles.Number, CultureInfo.InvariantCulture, out var score) || score < 0 || score > 10)) throw new InvalidOperationException("Die Bewertung muss zwischen 0 und 10 liegen.");
        nfo.SortTitle = SortTitle; nfo.Premiered = Premiere?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        nfo.ReleaseDate = nfo.Premiered; nfo.Outline = Outline; nfo.Credits = Split(Credits);
        nfo.TmdbId = string.IsNullOrWhiteSpace(Tmdb) ? null : Tmdb.Trim();
        nfo.UniqueIds.RemoveAll(u => u.Type is not null && (u.Type.Equals("tmdb", StringComparison.OrdinalIgnoreCase) || u.Type.Equals("imdb", StringComparison.OrdinalIgnoreCase) || u.Type.Equals("tmdbcollection", StringComparison.OrdinalIgnoreCase)));
        if (!string.IsNullOrWhiteSpace(Tmdb)) nfo.UniqueIds.Add(new() { Type = "tmdb", IdValue = Tmdb.Trim(), Default = true });
        if (!string.IsNullOrWhiteSpace(Imdb)) nfo.UniqueIds.Add(new() { Type = "imdb", IdValue = Imdb.Trim() });
        if (!string.IsNullOrWhiteSpace(CollectionId)) nfo.UniqueIds.Add(new() { Type = "tmdbcollection", IdValue = CollectionId.Trim() });
        if (string.IsNullOrWhiteSpace(Collection)) nfo.Set = null; else nfo.MovieSet = Collection.Trim();
        nfo.TmdbCollectionId = CollectionId;
        nfo.Mpaa = Certification; nfo.Certification = Certification;
        nfo.Country = Split(Countries); nfo.Studios = Split(Studios); nfo.Tags = Split(Tags);
        nfo.Runtime = int.TryParse(Runtime, out var value) ? value : 0; nfo.RatingString = Rating.Replace(',', '.');
        nfo.Actors = Actors.Where(a => !string.IsNullOrWhiteSpace(a.Name)).Select(a => a.ToActor()).ToList();
    }

    public void ApplyScrapedDetails(MovieScrap2000.TMDbScraper.TMDbMovie movie)
    {
        Title = movie.Title ?? Title; OriginalTitle = movie.OriginalTitle ?? OriginalTitle;
        Year = movie.ReleaseDate?.Year.ToString() ?? Year; Plot = movie.Overview ?? Plot;
        Tagline = movie.Tagline ?? Tagline; Genres = movie.GenresString; Directors = movie.DirectorsString;
        Imdb = movie.ImdbId ?? Imdb; Tmdb = movie.Id.ToString();
        if (SelectedMovie?.FullMovieNFO is { } nfo) nfo.TmdbId = Tmdb;
        if (movie.ReleaseDate.HasValue) Premiere = new DateTimeOffset(movie.ReleaseDate.Value);
        if (movie.Runtime.HasValue) Runtime = movie.Runtime.Value.ToString();
        Rating = movie.VoteAverage.ToString(CultureInfo.InvariantCulture);
        Credits = string.Join("; ", movie.Credits.Crew.Where(c => c.Department == "Writing" || c.Job is "Writer" or "Screenplay" or "Story").Select(c => c.Name).Where(n => !string.IsNullOrWhiteSpace(n)).Distinct());
        if (movie.BelongsToCollection is { } set && set.Id > 0) { Collection = set.Name ?? ""; CollectionId = set.Id.ToString(); }
        Studios = movie.ProductionCompaniesString; Countries = movie.ProductionCountriesString;
        if (movie.Keywords.Keywords.Count > 0) Tags = string.Join("; ", movie.Keywords.Keywords.Select(k => k.Name));
        var country = new RegionInfo(MetadataLanguage).TwoLetterISORegionName;
        var german = country == "DE";
        var certification = movie.ReleaseDates.Results.Where(r => r.Iso31661 == country).SelectMany(r => r.ReleaseDates).Select(r => r.Certification).FirstOrDefault(c => !string.IsNullOrWhiteSpace(c));
        if (certification is not null) Certification = german ? "FSK " + certification : certification;
        if (movie.Credits.Cast.Count > 0)
        {
            SelectedActors.Clear(); SelectedActor = null; Actors.Clear();
            foreach (var cast in movie.Credits.Cast)
                Actors.Add(new() { Name = cast.Name ?? "", Role = cast.Character ?? "", TmdbId = cast.Id, Order = Actors.Count,
                    Thumb = cast.ProfilePath is { } path ? "https://image.tmdb.org/t/p/w185" + path : "" });
            if (DetailTabIndex == 1) SelectFirstDetailEntry();
        }
        Status = "TMDb-Daten übernommen. Prüfe die Angaben und speichere deine Änderungen.";
    }

    private async Task LoadActorPhotoAsync(ActorEditor? actor)
    {
        actorLoading?.Cancel(); var version = ++actorVersion;
        actorPhotoLease?.Dispose(); actorPhotoLease = null; ActorPhoto = null; ActorPhotoResolution = ""; IsActorPhotoLoading = actor is not null;
        if (actor is null) { actorLoading = null; return; }
        using var cts = new CancellationTokenSource(); actorLoading = cts;
        var folder = SelectedMovie?.FolderPath;
        try
        {
            byte[]? bytes = actor.ThumbData;
            if (bytes is not { Length: > 0 } && actor.Id > 0) bytes = await metadata.GetActorPictureAsync(actor.Id, cts.Token);
            if (bytes is { Length: > 0 } && metadata.GetEmptyImageBytes() is { } empty && bytes.AsSpan().SequenceEqual(empty)) bytes = null;
            var cacheLoadedPortrait = bytes is not { Length: > 0 };
            if (bytes is not { Length: > 0 } && folder is not null)
            {
                bytes = await Task.Run(() =>
                {
                    var names = new[] { actor.Thumb ?? "", Path.Combine(".actors", actor.Name.Replace(' ', '_') + ".jpg"), Path.Combine(".actors", actor.Name + ".jpg") };
                    var path = names.Where(n => n.Length > 0 && !Uri.IsWellFormedUriString(n, UriKind.Absolute)).Select(n => Path.Combine(folder, n)).FirstOrDefault(File.Exists);
                    return path is null ? null : File.ReadAllBytes(path);
                }, cts.Token);
            }
            if (bytes is not { Length: > 0 } && Uri.TryCreate(actor.Thumb, UriKind.Absolute, out var uri) && uri.Scheme is "https" or "http")
                bytes = await metadata.DownloadImageAsync(uri.AbsoluteUri, cts.Token);
            if (bytes is not { Length: > 0 }) return;
            var preview = await ArtworkPreviewCache.Shared.AcquireAsync(actor.Thumb.Length > 0 ? actor.Thumb : $"actor:{actor.Name}",
                240, bytes, cts.Token, retainOriginal: false);
            if (version != actorVersion || cts.IsCancellationRequested) preview?.Dispose();
            else
            {
                actor.ThumbData = bytes; actorPhotoLease = preview; ActorPhoto = preview?.Image;
                ActorPhotoResolution = preview?.Resolution ?? "";
                if (cacheLoadedPortrait && preview is not null && actor.Id > 0)
                    await metadata.UpdateActorThumbExternalAsync(actor.Id, bytes, cts.Token);
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception ex) { if (version == actorVersion) Status = $"Darstellerbild konnte nicht geladen werden: {ex.Message}"; }
        finally { if (version == actorVersion) { IsActorPhotoLoading = false; actorLoading = null; } }
    }

    private async Task LoadTechnicalDetailsAsync(MovieViewModel movie, Movie nfo, int version, CancellationToken token)
    {
        if (TechnicalMediaReader.HasVideoDetails(nfo.FileInfo)) return;
        TechnicalDetails = Localization.Locale.T("Technische Mediendaten werden gelesen …");
        try
        {
            var info = await TechnicalMediaReader.ReadAsync(movie.VideoFilePath, token);
            if (version != selectionVersion || token.IsCancellationRequested) return;
            if (info is not null)
            {
                nfo.FileInfo = info;
                MediaInfo = info;
                if (string.IsNullOrWhiteSpace(Runtime) && info.StreamDetails.VideoStream.DurationInSeconds > 0)
                    Runtime = Math.Ceiling(info.StreamDetails.VideoStream.DurationInSeconds / 60d).ToString(CultureInfo.InvariantCulture);
                TechnicalDetails = Localization.Locale.T(DescribeStreams(info));
            }
            else TechnicalDetails = Localization.Locale.T("Technische Mediendaten konnten nicht gelesen werden.");
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            System.Diagnostics.Trace.WriteLine(ex);
            if (version == selectionVersion)
                TechnicalDetails = Localization.Locale.T("Technische Mediendaten konnten nicht gelesen werden.");
        }
    }

    private static string DescribeStreams(MovieFileInfo? info)
    {
        if (info?.StreamDetails is not { } streams) return "Keine technischen Mediendaten in der NFO vorhanden.";
        var video = streams.VideoStream;
        var lines = new List<string>();
        if (video is not null) lines.Add($"Video: {video.Codec} · {video.Width} × {video.Height} · Bildformat {video.Aspect} · {video.DurationInSeconds} s · {video.Bitrate} Bit/s\nDateigröße: {video.FileSize:N0} Bytes · Scan: {video.ScanType}");
        lines.Add($"Audiospuren: {streams.AudioStreams.Count}");
        lines.AddRange(streams.AudioStreams.Select((a, i) => $"{i + 1}. {a.Codec} · {a.LongLanguage ?? a.Language} · {a.Channels} Kanäle · {a.Bitrate} Bit/s"));
        lines.Add($"Untertitel: {streams.Subtitles.Count}"); lines.AddRange(streams.Subtitles.Select(s => s.Language ?? "Unbekannt"));
        return string.Join("\n", lines);
    }
}

public partial class ActorEditor : ObservableObject
{
    public int Id { get; set; }
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasNoPicture))]
    private byte[]? thumbData;
    [ObservableProperty] private string name = "";
    [ObservableProperty] private string role = "";
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasNoPicture))]
    private string thumb = "";
    public bool HasNoPicture => string.IsNullOrWhiteSpace(Thumb) && ThumbData is not { Length: > 0 };
    [ObservableProperty] private int order;
    [ObservableProperty] private int? tmdbId;
    public static ActorEditor From(Actor actor) => new() { Id = actor.Id, Name = actor.Name, Role = actor.Role ?? "", Thumb = actor.Thumb ?? "", Order = actor.Order, TmdbId = actor.TmdbId, ThumbData = actor.ThumbData };
    public Actor ToActor() => new() { Id = Id, Name = Name.Trim(), Role = Role, Thumb = Thumb, Order = Order, TmdbId = TmdbId, ThumbData = ThumbData };
}
