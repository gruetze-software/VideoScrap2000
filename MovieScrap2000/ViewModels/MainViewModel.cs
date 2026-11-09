using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Avalonia.Media.Imaging;
using MovieScrap2000;
using VideoScrap2000.Localization;
using VideoScrap2000.Views;

namespace VideoScrap2000.ViewModels;

public partial class MainViewModel : ObservableObject, IDisposable
{
    private readonly DatabaseManager database;
    private readonly MetadataService metadata;
    private readonly MovieManager scanner;
    private readonly AppSettings settings;
    public bool IsDemo { get; }
    public string DemoLabel => Locale.T("Demo-Modus");
    private List<MovieViewModel> allMovies = [];
    private CancellationTokenSource? operation;
    private int selectionVersion;
    private CancellationTokenSource? imageLoading;
    internal Task ArtworkLoadingTask { get; private set; } = Task.CompletedTask;
    internal Task TechnicalDetailsLoadingTask { get; private set; } = Task.CompletedTask;
    private CancellationTokenSource? factsLoading;
    public ObservableCollection<MovieViewModel> Movies { get; } = [];
    public SeriesViewModel Series { get; }
    public ObservableCollection<ArtworkViewModel> Artwork { get; } = [];
    public IReadOnlyList<string> ArtworkTypes { get; } = new[] { "Alle" }.Concat(ImageTypes.AllTypes).ToArray();
    [ObservableProperty] private string artworkType = ImageTypes.Poster;
    [ObservableProperty] private ArtworkViewModel? selectedArtwork;
    partial void OnSelectedArtworkChanged(ArtworkViewModel? value)
    {
        if (value is not null) ArtworkType = value.Type;
    }
    public Action<string>? ThemeRequested { get; set; }
    public string Version => typeof(MainViewModel).Assembly.GetName().Version?.ToString(3) ?? "1.0.0";
    [ObservableProperty] private string libraryPath;
    [ObservableProperty] private string seriesPath;
    [ObservableProperty] private bool isImagesLoading;
    [ObservableProperty] private int themeIndex;
    [ObservableProperty] private int languageIndex;
    [ObservableProperty] private int metadataLanguageIndex;
    [ObservableProperty] private int artworkLanguageIndex;
    public int ArtworkOptimizationProfile => Math.Clamp(settings.ArtworkOptimizationProfile, 0, 2);
    public string MetadataLanguage => LanguagePreferences.ResolveContent(settings.MetadataLanguage);
    public string ArtworkLanguage => LanguagePreferences.ResolveContent(settings.ArtworkLanguage);
    partial void OnStatusChanged(string value)
    {
        var translated = Locale.T(value);
        if (translated != value) Status = translated;
    }
    [ObservableProperty] private string query = "";
    [ObservableProperty] private int movieSortIndex;
    [ObservableProperty] private int detailTabIndex;
    partial void OnDetailTabIndexChanged(int value) => SelectFirstDetailEntry();
    internal void SelectFirstDetailEntry()
    {
        if (DetailTabIndex == 1)
        {
            SelectedActors.Clear();
            if (Actors.FirstOrDefault() is { } actor) SelectedActors.Add(actor);
        }
        else if (DetailTabIndex == 3) { SelectedArtwork = null; SelectedArtwork = Artwork.FirstOrDefault(); }
    }
    partial void OnMovieSortIndexChanged(int value) => Filter();
    [ObservableProperty] private MovieViewModel? selectedMovie;
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanGenerateOutline))]
    private bool isBusy;
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowEmptyState))]
    private bool isInitializing;
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanGenerateOutline))]
    private bool isDetailReady;
    [ObservableProperty] private string status = "Bereit. Wähle deinen Filmordner aus.";
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsProgressIndeterminate))]
    private int progress;
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsProgressIndeterminate))]
    private bool hasScanProgress;
    [ObservableProperty] private string scanProgressText = "";
    [ObservableProperty] private string scanFolder = "";
    public bool IsProgressIndeterminate => Progress == 0 && !HasScanProgress;
    [ObservableProperty] private string title = "";
    [ObservableProperty] private string originalTitle = "";
    [ObservableProperty] private string year = "";
    [ObservableProperty] private string genres = "";
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanGenerateOutline))]
    private string plot = "";
    [ObservableProperty] private string tagline = "";
    [ObservableProperty] private string imdb = "";
    [ObservableProperty] private string directors = "";
    [ObservableProperty] private Bitmap? poster;
    [ObservableProperty] private Bitmap? fanartBackground;
    [ObservableProperty] private bool useFanartBackground;
    public string CountLabel => Locale.T(Movies.Count == 1 ? "1 Film" : $"{Movies.Count} Filme");
    public bool HasSelection => SelectedMovie is not null;
    public bool IsEmpty => Movies.Count == 0;
    public bool ShowEmptyState => IsEmpty && !IsInitializing;
    public string EmptyTitle => Locale.T(allMovies.Count == 0 ? "Platz für großes Kino." : "Kein passender Film.");
    public string EmptyDescription => Locale.T(allMovies.Count == 0
        ? "Wähle deinen Filmordner und lies deine Sammlung ein. Vorhandene NFOs und Bilder bleiben beim Einlesen unverändert."
        : "Ändere deinen Suchbegriff, um weitere Filme aus deiner Sammlung zu sehen.");
    public bool CanEdit => IsDetailReady && !IsBusy;
    public bool CanGenerateOutline => CanEdit && !string.IsNullOrWhiteSpace(Plot);
    public string SelectionPath => IsDemo && SelectedMovie is { } demoMovie
        ? "Demo / Movies / " + Path.GetFileName(demoMovie.FolderPath) + " / " + Path.GetFileName(demoMovie.VideoFilePath)
        : SelectedMovie?.VideoFilePath ?? "";

    public MainViewModel(DatabaseManager? database = null, AppSettings? settings = null, bool isDemo = false)
    {
        IsDemo = isDemo;
        this.database = database ?? new();
        this.settings = settings ?? AppSettings.Load();
        Series = new(this);
        SelectedActors.CollectionChanged += (_, _) =>
        {
            SelectedActor = SelectedActors.LastOrDefault();
            RemoveActorCommand.NotifyCanExecuteChanged();
        };
        languageIndex = LanguagePreferences.ToIndex(this.settings.Language);
        metadataLanguageIndex = LanguagePreferences.ContentToIndex(this.settings.MetadataLanguage);
        artworkLanguageIndex = LanguagePreferences.ContentToIndex(this.settings.ArtworkLanguage);
        useFanartBackground = this.settings.UseFanartBackground;
        Locale.Apply(this.settings.Language);
        status = Locale.T(status);
        libraryPath = this.settings.LibraryPath;
        seriesPath = this.settings.SeriesPath;
        themeIndex = this.settings.Theme switch { "Light" => 1, "Dark" => 2, _ => 0 };
        metadata = new MetadataService(this.database);
        scanner = new MovieManager(metadata);
        scanner.DetailedProgress += (_, value) => Avalonia.Threading.Dispatcher.UIThread.Post(() =>
        {
            if (!IsBusy) return;
            HasScanProgress = true;
            Progress = value.Total == 0 ? 90 : 90 * value.Completed / value.Total;
            ScanProgressText = Locale.T($"{value.Completed} von {value.Total} Ordnern geprüft · {value.Errors} Fehler");
            ScanFolder = value.Folder.Length == 0 ? "" : Locale.T($"Zuletzt geprüft: {Path.GetFileName(value.Folder)}");
        });
        scanner.LogMessage += (_, log) =>
        {
            if (log.Level is LogLevel.Error or LogLevel.Critical)
                Avalonia.Threading.Dispatcher.UIThread.Post(() => Status = log.Message);
        };
    }

    public async Task InitializeAsync() => await RunAsync(async token =>
    {
        IsInitializing = true;
        Status = "Filmsammlung wird geladen …";
        try
        {
            await Task.Run(database.InitializeDatabaseAsync, token);
            if (IsDemo) await Task.Run(() => scanner.ScanDirectoryAsync(LibraryPath, token), token);
            await ReloadAsync(token, reportProgress: true);
            SelectedMovie = Movies.FirstOrDefault();
        }
        finally { IsInitializing = false; }
        Status = allMovies.Count == 0 ? "Deine Filmsammlung beginnt mit einem Ordner." : allMovies.Count == 1 ? "1 Film geladen." : $"{allMovies.Count} Filme geladen.";
    }, reloadAfterCancellation: false);

    partial void OnQueryChanged(string value) => Filter();
    partial void OnSeriesPathChanged(string value) => Series.ResetLibrary();
    public IReadOnlyList<string> MovieFolders => LibrarySources.Normalize(settings.MovieFolders ?? [LibraryPath]);
    public IReadOnlyList<string> SeriesFolders => LibrarySources.Normalize(settings.SeriesFolders ?? [SeriesPath]);
    partial void OnUseFanartBackgroundChanged(bool value) => Series.RefreshAppearance();
    partial void OnThemeIndexChanged(int value)
    {
        settings.Theme = value switch { 1 => "Light", 2 => "Dark", _ => "System" };
        ThemeRequested?.Invoke(settings.Theme);
    }
    public void ApplySettings(string movies, string series, int theme, int language = 0, int metadataLanguage = 0, int artworkLanguage = 0, bool useFanartBackground = false, int artworkOptimizationProfile = 0)
        => ApplySettings(new[] { movies }, new[] { series }, theme, language, metadataLanguage, artworkLanguage, useFanartBackground, artworkOptimizationProfile);

    public void ApplySettings(IEnumerable<string> movies, IEnumerable<string> series, int theme, int language = 0, int metadataLanguage = 0, int artworkLanguage = 0, bool useFanartBackground = false, int artworkOptimizationProfile = 0)
    {
        var previousSeries = SeriesFolders;
        if (!IsDemo)
        {
            settings.MovieFolders = LibrarySources.Normalize(movies);
            settings.SeriesFolders = LibrarySources.Normalize(series);
            settings.LibraryPath = settings.MovieFolders.FirstOrDefault() ?? "";
            settings.SeriesPath = settings.SeriesFolders.FirstOrDefault() ?? "";
        }
        settings.Theme = theme switch { 1 => "Light", 2 => "Dark", _ => "System" };
        settings.Language = LanguagePreferences.FromIndex(language);
        settings.MetadataLanguage = LanguagePreferences.ContentFromIndex(metadataLanguage);
        settings.ArtworkLanguage = LanguagePreferences.ContentFromIndex(artworkLanguage);
        settings.UseFanartBackground = useFanartBackground;
        settings.ArtworkOptimizationProfile = Math.Clamp(artworkOptimizationProfile, 0, 2);
        if (!IsDemo) settings.Save();
        UseFanartBackground = useFanartBackground;
        LanguageIndex = language; MetadataLanguageIndex = metadataLanguage; ArtworkLanguageIndex = artworkLanguage;
        Locale.Apply(settings.Language);
        foreach (var movie in allMovies) if (movie.FileFacts is { } facts) movie.UpdateFileFacts(facts);
        foreach (var property in new[] { nameof(CountLabel), nameof(EmptyTitle), nameof(EmptyDescription), nameof(MetadataLanguage), nameof(ArtworkLanguage) }) OnPropertyChanged(property);
        TechnicalDetails = Locale.T(TechnicalDetails);
        LibraryPath = settings.LibraryPath;
        SeriesPath = settings.SeriesPath;
        if (!previousSeries.SequenceEqual(SeriesFolders, LibrarySources.PathComparer)) Series.ResetLibrary();
        OnPropertyChanged(nameof(MovieFolders)); OnPropertyChanged(nameof(SeriesFolders));
        if (!IsDemo)
        {
            allMovies = allMovies.Where(movie => LibrarySources.ContainsFolder(MovieFolders, movie.FolderPath)).ToList();
            Filter(SelectedMovie?.Id);
        }
        ThemeIndex = theme;
        Status = "Einstellungen gespeichert.";
    }

    public async Task RefreshFileFactsAsync()
    {
        factsLoading?.Cancel();
        using var cts = new CancellationTokenSource();
        factsLoading = cts;
        var snapshot = allMovies;
        try
        {
            await Parallel.ForEachAsync(snapshot, new ParallelOptions { CancellationToken = cts.Token, MaxDegreeOfParallelism = 4 }, async (movie, token) =>
            {
                var facts = await Task.Run(() => MovieFileFacts.Read(movie.FolderPath, movie.VideoFilePath), token);
                token.ThrowIfCancellationRequested();
                await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
                {
                    if (!cts.IsCancellationRequested && ReferenceEquals(snapshot, allMovies)) movie.UpdateFileFacts(facts);
                });
            });
            if (!cts.IsCancellationRequested && ReferenceEquals(snapshot, allMovies)) Filter();
        }
        catch (OperationCanceledException) { }
        catch (Exception ex) { Status = $"Dateistatus konnte nicht geladen werden: {ex.Message}"; }
        finally { if (ReferenceEquals(factsLoading, cts)) factsLoading = null; }
    }
    partial void OnIsBusyChanged(bool value)
    {
        OnPropertyChanged(nameof(CanEdit));
        ScanCommand.NotifyCanExecuteChanged();
        SaveCommand.NotifyCanExecuteChanged();
        RemoveActorCommand.NotifyCanExecuteChanged();
    }
    partial void OnIsDetailReadyChanged(bool value)
    {
        OnPropertyChanged(nameof(CanEdit));
        SaveCommand.NotifyCanExecuteChanged();
        RemoveActorCommand.NotifyCanExecuteChanged();
    }
    private bool updatingMovieList;

    partial void OnSelectedMovieChanged(MovieViewModel? value)
    {
        if (!updatingMovieList) UpdateSelectedMovie(value);
    }

    private void UpdateSelectedMovie(MovieViewModel? value)
    {
        OnPropertyChanged(nameof(HasSelection));
        OnPropertyChanged(nameof(SelectionPath));
        ArtworkLoadingTask = LoadDetailsAsync(value);
    }

    private void Filter(int? selectedId = null)
    {
        var selected = SelectedMovie;
        var query = Query.Trim();
        var grouped = allMovies.Where(movie => query.Length == 0 ||
            $"{movie.Title} {movie.Year} {movie.GenresString}".Contains(query, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(movie => movie.MissingMetadata);
        var sorted = MovieSortIndex switch
        {
            1 => grouped.ThenBy(m => m.Title, StringComparer.CurrentCultureIgnoreCase),
            2 => grouped.ThenByDescending(m => int.TryParse(m.Year, out var year) ? year : int.MinValue),
            _ => grouped.ThenByDescending(m => m.MetadataCapturedAtUtc)
        };
        var desired = sorted.ThenBy(m => m.Title, StringComparer.CurrentCultureIgnoreCase).ToList();
        var desiredSet = desired.ToHashSet();
        updatingMovieList = true;
        try
        {
            for (var i = Movies.Count - 1; i >= 0; i--)
                if (!desiredSet.Contains(Movies[i])) Movies.RemoveAt(i);
            for (var i = 0; i < desired.Count; i++)
            {
                if (i < Movies.Count && ReferenceEquals(Movies[i], desired[i])) continue;
                var current = Movies.IndexOf(desired[i]);
                if (current < 0) Movies.Insert(i, desired[i]);
                else Movies.Move(current, i);
            }
            SelectedMovie = selectedId.HasValue ? desired.FirstOrDefault(m => m.Id == selectedId) ?? desired.FirstOrDefault()
                : selected is not null && desiredSet.Contains(selected) ? selected : desired.FirstOrDefault();
        }
        finally { updatingMovieList = false; }
        if (!ReferenceEquals(selected, SelectedMovie)) UpdateSelectedMovie(SelectedMovie);
        OnPropertyChanged(nameof(CountLabel));
        OnPropertyChanged(nameof(IsEmpty));
        OnPropertyChanged(nameof(ShowEmptyState));
        OnPropertyChanged(nameof(EmptyTitle));
        OnPropertyChanged(nameof(EmptyDescription));
    }

    public async Task ReloadAsync(CancellationToken cancellationToken = default, bool reportProgress = false)
    {
        var id = SelectedMovie?.Id;
        factsLoading?.Cancel();
        var loadedMovies = await Task.Run(() => metadata.LoadMoviesAsync(), cancellationToken);
        if (settings.MovieFolders is not null || MovieFolders.Count > 0)
            loadedMovies = loadedMovies.Where(movie => LibrarySources.ContainsFolder(MovieFolders, movie.FolderPath)).ToList();
        cancellationToken.ThrowIfCancellationRequested();
        foreach (var movie in loadedMovies.Where(m => m.IsPlaceholder))
        {
            var inferred = MovieNameParser.Parse(movie.VideoFilePath);
            if (!string.IsNullOrWhiteSpace(inferred.Title)) movie.Title = inferred.Title;
            if (inferred.Year.HasValue) movie.Year = inferred.Year.Value.ToString();
        }
        // Keep the current list visible until all sort keys and the new/NFO status are known.
        var prepared = 0;
        var progressLock = new object();
        var progressStart = Progress;
        if (reportProgress)
        {
            HasScanProgress = true;
            ScanProgressText = Locale.T($"{0} von {loadedMovies.Count} Einträgen vorbereitet");
            ScanFolder = "";
        }
        await Parallel.ForEachAsync(loadedMovies, new ParallelOptions { MaxDegreeOfParallelism = 4, CancellationToken = cancellationToken }, async (movie, token) =>
        {
            var facts = await Task.Run(() => MovieFileFacts.Read(movie.FolderPath, movie.VideoFilePath), token);
            movie.UpdateFileFacts(facts);
            if (reportProgress)
            {
                lock (progressLock)
                {
                    var done = ++prepared;
                    Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                    {
                        if (!IsBusy) return;
                        Progress = progressStart + (100 - progressStart) * done / Math.Max(1, loadedMovies.Count);
                        ScanProgressText = Locale.T($"{done} von {loadedMovies.Count} Einträgen vorbereitet");
                    });
                }
            }
        });
        cancellationToken.ThrowIfCancellationRequested();
        allMovies = loadedMovies;
        foreach (var group in loadedMovies.GroupBy(m => (m.Title.ToUpperInvariant(), m.Year)).Where(g => g.Count() > 1))
        {
            foreach (var movie in group)
            {
                var folder = Path.TrimEndingDirectorySeparator(movie.FolderPath);
                movie.VersionLabel = Path.GetFileName(folder);
                if (group.Count(other => LibrarySources.PathComparer.Equals(Path.GetFileName(Path.TrimEndingDirectorySeparator(other.FolderPath)), movie.VersionLabel)) > 1)
                    movie.VersionLabel = Path.GetFileName(Path.GetDirectoryName(folder)) + " / " + Path.GetFileName(folder);
            }
            foreach (var duplicates in group.GroupBy(movie => movie.VersionLabel, LibrarySources.PathComparer).Where(items => items.Count() > 1))
                foreach (var movie in duplicates) movie.VersionLabel = movie.FolderPath;
        }
        Filter(id);
    }

    private async Task LoadDetailsAsync(MovieViewModel? movie)
    {
        imageLoading?.Cancel();
        var cts = new CancellationTokenSource();
        imageLoading = cts;
        var token = cts.Token;
        var version = ++selectionVersion;
        IsImagesLoading = movie is not null;
        IsDetailReady = false;
        SelectedArtwork = null;
        Poster = null;
        FanartBackground = null;
        foreach (var image in Artwork) image.Dispose();
        Artwork.Clear();
        if (movie is null) { imageLoading = null; cts.Dispose(); return; }
        try
        {
            await Task.Run(() => movie.LoadFullDetailsAsync(metadata), token);
            if (version != selectionVersion) return;
            var nfo = movie.FullMovieNFO ?? new Movie { Title = movie.Title, Year = movie.Year, Plot = movie.Plot };
            movie.FullMovieNFO = nfo;
            Title = nfo.Title ?? movie.Title;
            OriginalTitle = nfo.OriginalTitle ?? "";
            Year = nfo.Year ?? movie.Year;
            Genres = nfo.DisplayGenres ?? movie.GenresString.Replace(", ", "; ");
            Plot = nfo.Plot ?? movie.Plot;
            Tagline = nfo.Tagline ?? "";
            Imdb = nfo.Id ?? nfo.ImdbIdFromUnique ?? "";
            Directors = nfo.DisplayDirectors ?? "";
            LoadExtendedMetadata(nfo);
            IsDetailReady = true;
            var technicalDetailsTask = LoadTechnicalDetailsAsync(movie, nfo, version, token);
            TechnicalDetailsLoadingTask = technicalDetailsTask;
            var paths = await ArtworkPreviewCache.Shared.FindArtworkAsync(movie.FolderPath, movie.VideoFilePath, token);
            if (version != selectionVersion) return;
            foreach (var item in paths)
            {
                try
                {
                    token.ThrowIfCancellationRequested();
                    var preview = await ArtworkPreviewCache.Shared.AcquireAsync(item.Path, ArtworkPreviewWidth(item.Type), token: token);
                    if (preview is null) continue;
                    if (token.IsCancellationRequested || version != selectionVersion) { preview.Dispose(); return; }
                    var bitmap = preview.Image;
                    Artwork.Add(new(item.Type, item.Path, bitmap) { CachedPreview = preview });
                    if (DetailTabIndex == 3 && SelectedArtwork is null) SelectedArtwork = Artwork[0];
                    if (item.Type == ImageTypes.Poster) Poster = bitmap;
                    if (item.Type == ImageTypes.Fanart) FanartBackground = bitmap;
                }
                catch (Exception ex) when (ex is IOException or ArgumentException or NotSupportedException) { Status = $"Bild nicht lesbar: {Path.GetFileName(item.Path)}"; }
            }
            IsImagesLoading = false;
            await technicalDetailsTask;
            if (version != selectionVersion) return;
            IsDetailReady = true;
        }
        catch (OperationCanceledException) { }
        catch (Exception ex) { if (version == selectionVersion) Status = $"Film konnte nicht geladen werden: {ex.Message}"; }
        finally
        {
            if (version == selectionVersion) { IsImagesLoading = false; imageLoading = null; }
            cts.Dispose();
        }
    }

    internal static IEnumerable<(string Type, string Path)> FindArtwork(MovieViewModel movie, string? requestedType = null)
        => FindArtwork(movie.FolderPath, movie.VideoFilePath, requestedType);

    internal static IEnumerable<(string Type, string Path)> FindArtwork(string folder, string video, string? requestedType = null)
    {
        if (!Directory.Exists(folder)) yield break;
        var basename = Path.GetFileNameWithoutExtension(video);
        var files = Directory.EnumerateFiles(folder).GroupBy(p => Path.GetFileName(p)!, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);
        foreach (var type in ImageTypes.AllTypes)
        {
            if (requestedType is not null && type != requestedType) continue;
            var suffix = type switch { ImageTypes.ClearLogo => "logo", ImageTypes.DiskArt => "discart", _ => type.ToLowerInvariant() };
            var stems = new[] { $"{basename}-{suffix}", suffix, type == ImageTypes.ClearLogo ? "clearlogo" : suffix };
            var path = stems.SelectMany(stem => new[] { ".jpg", ".png", ".jpeg", ".webp" }.Select(ext => stem + ext)).Select(name => files.GetValueOrDefault(name)).FirstOrDefault(path => path is not null);
            if (path is not null) yield return (type, path);
        }
    }

    private bool CanScan() => !IsBusy;
    public async Task RemoveUnavailableMovieAsync(MovieViewModel movie)
    {
        if (IsBusy || !movie.IsFolderUnavailable || !allMovies.Contains(movie)) return;
        await RunAsync(async token =>
        {
            Status = "Eintrag wird entfernt …";
            await metadata.RemoveCachedMovieAsync(movie.Id, token);
            var selectedId = SelectedMovie?.Id;
            var index = Movies.IndexOf(movie);
            if (ReferenceEquals(SelectedMovie, movie) && index >= 0 && Movies.Count > 1)
                selectedId = Movies[index + 1 < Movies.Count ? index + 1 : index - 1].Id;
            allMovies = allMovies.Where(item => item.Id != movie.Id).ToList();
            Filter(selectedId);
            Status = "Eintrag aus der Filmliste entfernt.";
        }, reloadAfterCancellation: false);
    }

    [RelayCommand(CanExecute = nameof(CanScan))]
    private async Task ScanAsync() => await RunAsync(async token =>
    {
        if (MovieFolders.Count == 0) throw new DirectoryNotFoundException("Bitte wähle einen erreichbaren Filmordner aus.");
        settings.LibraryPath = LibraryPath;
        if (!IsDemo) settings.Save();
        Status = "Filmsammlung wird eingelesen …";
        await Task.Run(() => scanner.ScanDirectoriesAsync(MovieFolders, token), token);
        Status = "Filmsammlung wird geladen …";
        ArtworkPreviewCache.Shared.RefreshFileState();
        await ReloadAsync(token, reportProgress: true);
        Status = scanner.ScanErrors == 0 ? $"Scan abgeschlossen. {allMovies.Count} Filme in deiner Sammlung."
            : $"Scan beendet: {allMovies.Count} Filme, {scanner.ScanErrors} Fehler. Prüfe die betroffenen Ordner.";
    });

    private bool CanSave() => CanEdit;
    [RelayCommand(CanExecute = nameof(CanSave))]
    private async Task SaveAsync() => await RunAsync(async token =>
    {
        Status = Artwork.Any(image => image.PendingData is not null)
            ? "Metadaten und Artwork werden gespeichert …"
            : "Metadaten werden gespeichert …";
        var movie = SelectedMovie!;
        var nfo = movie.FullMovieNFO!;
        await TechnicalDetailsLoadingTask.WaitAsync(token);
        if (!TechnicalMediaReader.HasVideoDetails(nfo.FileInfo))
        {
            var info = await TechnicalMediaReader.ReadAsync(movie.VideoFilePath, token);
            if (info is not null) nfo.FileInfo = info;
        }
        ApplyExtendedMetadata(nfo);
        nfo.Title = Title; nfo.OriginalTitle = OriginalTitle; nfo.Year = Year;
        nfo.Plot = Plot; nfo.Tagline = Tagline; nfo.Id = Imdb;
        nfo.Genres = Split(Genres).Distinct(StringComparer.CurrentCultureIgnoreCase).ToList();
        nfo.Directors = Split(Directors);
        var artworkChanged = Artwork.Any(image => image.PendingData is not null);
        var pendingArtworkTypes = Artwork.Where(image => image.PendingData is not null).Select(image => image.Type).ToHashSet();
        try { await Task.Run(() => metadata.SaveMovieAsync(nfo, movie.FolderPath, movie.VideoFilePath, false, token,
            saveImage: (folder, stem, image, ct) => pendingArtworkTypes.Contains(image.Type)
                ? ArtworkOptimizer.SaveMovieImageAsync(folder, stem, image, (ArtworkProfile)ArtworkOptimizationProfile, ct)
                : Task.CompletedTask), token); }
        finally { if (artworkChanged) ArtworkPreviewCache.Shared.InvalidateFolder(movie.FolderPath); }
        await RefreshSavedMovieAsync(movie.Id);
        Status = "Metadaten gespeichert. Die vorherige NFO wurde als .bak gesichert.";
    });

    private async Task RefreshSavedMovieAsync(int id)
    {
        var updated = await Task.Run(async () =>
        {
            var movie = (await metadata.LoadMoviesAsync([id])).Single();
            movie.UpdateFileFacts(MovieFileFacts.Read(movie.FolderPath, movie.VideoFilePath));
            return movie;
        });

        var index = allMovies.FindIndex(movie => movie.Id == id);
        if (index < 0) return;
        allMovies[index] = updated;
        Filter(id);
    }

    [RelayCommand] private void Cancel() => operation?.Cancel();
    private static List<string> Split(string value) => value.Split(';', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries).ToList();
    public void StageArtwork(ImageCacheItem image)
    {
        if (SelectedMovie?.FullMovieNFO is not { } nfo) return;
        imageLoading?.Cancel(); imageLoading = null; selectionVersion++;
        IsImagesLoading = false;
        using var stream = new MemoryStream(image.Data);
        var bitmap = Bitmap.DecodeToWidth(stream, ArtworkPreviewWidth(image.Type));
        nfo.FanArtPics.RemoveAll(p => p.ImageType == image.Type);
        nfo.FanArtPics.Add(new() { ImageType = image.Type, ImageData = image.Data });
        var previous = Artwork.FirstOrDefault(p => p.Type == image.Type);
        var staged = new ArtworkViewModel(image.Type, Locale.T("Neue Auswahl · noch nicht gespeichert"), bitmap, image.Data);
        if (previous is not null)
        {
            Artwork[Artwork.IndexOf(previous)] = staged;
        }
        else
        {
            var order = ArtworkTypeOrder(image.Type);
            var position = Artwork.TakeWhile(item => ArtworkTypeOrder(item.Type) < order).Count();
            Artwork.Insert(position, staged);
        }
        SelectedArtwork = staged;
        if (image.Type == ImageTypes.Poster) Poster = bitmap;
        if (image.Type == ImageTypes.Fanart) FanartBackground = bitmap;
        previous?.Dispose();
        Status = "Artwork übernommen. Mit Änderungen speichern wird das Bild in den Filmordner geschrieben.";
    }
    private static int ArtworkTypeOrder(string type)
    {
        var index = ImageTypes.AllTypes.IndexOf(type);
        return index >= 0 ? index : int.MaxValue;
    }
    private static int ArtworkPreviewWidth(string type) => type switch
    {
        ImageTypes.Poster => 340,
        ImageTypes.Fanart => 960,
        _ => 480
    };

    public async Task RunAsync(Func<CancellationToken, Task> action, bool reloadAfterCancellation = true)
    {
        if (IsBusy) return;
        using var cts = new CancellationTokenSource(); operation = cts;
        Progress = 0;
        HasScanProgress = false; ScanProgressText = ""; ScanFolder = "";
        IsBusy = true;
        try { await action(cts.Token); }
        catch (OperationCanceledException)
        {
            if (reloadAfterCancellation) await ReloadAsync();
            Status = "Vorgang abgebrochen.";
        }
        catch (Exception ex) { Status = Utils.GetExcMsg(ex); }
        finally { operation = null; IsBusy = false; Progress = 0; HasScanProgress = false; ScanProgressText = ""; ScanFolder = ""; }
    }
    public void Dispose()
    {
        operation?.Cancel();
        imageLoading?.Cancel(); selectionVersion++;
        factsLoading?.Cancel();
        actorLoading?.Cancel(); actorVersion++;
        actorPhotoLease?.Dispose(); actorPhotoLease = null; ActorPhoto = null;
        FanartBackground = null;
        foreach (var image in Artwork) image.Dispose();
        metadata.Dispose();
    }
}

public sealed record ArtworkViewModel(string Type, string Path, Bitmap Image, byte[]? PendingData = null) : IDisposable
{
    internal ArtworkPreviewCache.Lease? CachedPreview { get; init; }
    public async Task<string> GetOpenPathAsync()
    {
        if (PendingData is null) return Path;
        var extension = PendingData.Length > 2 && PendingData[0] == 255 && PendingData[1] == 216 ? ".jpg"
            : PendingData.Length > 8 && PendingData[0] == 137 ? ".png" : ".webp";
        Directory.CreateDirectory(Utils.GetTempPath());
        var path = System.IO.Path.Combine(Utils.GetTempPath(), "preview-" + Guid.NewGuid().ToString("N") + extension);
        await File.WriteAllBytesAsync(path, PendingData);
        return path;
    }
    public void Dispose()
    {
        if (CachedPreview is { } preview) preview.Dispose();
        else Image.Dispose();
    }
}
