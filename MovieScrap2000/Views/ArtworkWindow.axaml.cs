using static VideoScrap2000.Localization.Locale;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using MovieScrap2000;
using MovieScrap2000.TMDbScraper;
using MovieScrap2000.FanArtTvScraper;
using System.Collections.ObjectModel;

namespace VideoScrap2000.Views;

public partial class ArtworkWindow : Window
{
    private readonly TMDbScraperManager scraper;
    private readonly CancellationTokenSource lifetime = new();
    private readonly ObservableCollection<ArtworkCandidate> candidates = [];
    private readonly int tmdbId;
    private readonly string language;
    private readonly IReadOnlySet<string>? automaticTypes;
    private readonly int? tvdbId;
    private readonly int? season;
    private readonly IReadOnlyList<string> searchTypes;
    private bool busy;
    private bool accepting;
    private CancellationTokenSource? previewLoading;
    private MovieScrap2000.TMDbScraper.TMDbScraper.TMDbImages? movieImages;
    private FanArtMovieResponse? fanartDetails;
    private bool fanartLoaded;
    private string? providerError;
    public List<ImageCacheItem> Results { get; } = [];
    public bool IsAccepted { get; private set; }
    public ImageCacheItem? Result { get; private set; }
    public Task LoadingTask { get; private set; } = Task.CompletedTask;
    public ArtworkWindow() : this(0) { }
    public ArtworkWindow(int tmdbId, string type = ImageTypes.Poster, TMDbScraperManager? scraper = null, string language = "de-DE",
        IReadOnlySet<string>? automaticTypes = null, int? tvdbId = null, int? season = null)
    {
        InitializeComponent();
        this.tmdbId = tmdbId;
        this.language = language;
        this.automaticTypes = automaticTypes;
        this.tvdbId = tvdbId;
        this.season = season;
        searchTypes = tvdbId.HasValue ? SeriesArtworkCatalog.Types(season) : ImageTypes.AllTypes;
        this.scraper = scraper ?? new();
        ImageType.ItemsSource = new[] { "Alle" }.Concat(searchTypes).ToArray();
        ImageType.SelectedItem = type == "Alle" || searchTypes.Contains(type) ? type : ImageTypes.Poster;
        Candidates.ItemsSource = candidates;
        Opened += (_, _) => StartLoading();
        ImageType.SelectionChanged += (_, _) => { if (IsVisible && !busy) StartLoading(); };
        Closed += (_, _) => { lifetime.Cancel(); this.scraper.Dispose(); foreach (var candidate in candidates) candidate.Dispose(); };
    }
    private void StartLoading()
    {
        if ((tmdbId > 0 || tvdbId > 0) && !busy) LoadingTask = LoadImagesAsync();
    }
    private void LoadImages(object? sender, RoutedEventArgs e)
    {
        if (busy) return;
        movieImages = null; fanartDetails = null; fanartLoaded = false; providerError = null;
        seriesFanart = default;
        StartLoading();
    }
    private async Task LoadImagesAsync()
    {
        if (busy || ImageType.SelectedItem is not string type) return;
        using var loading = CancellationTokenSource.CreateLinkedTokenSource(lifetime.Token);
        previewLoading = loading;
        var loadingToken = loading.Token;
        Results.Clear();
        SetBusy(true);
        foreach (var candidate in candidates) candidate.Dispose();
        candidates.Clear();
        StatusText.Text = T("Artwork wird geladen …");
        try
        {
            if (type == "Alle")
            {
                foreach (var imageType in searchTypes)
                {
                    if (automaticTypes is not null && !automaticTypes.Contains(imageType)) continue;
                    loadingToken.ThrowIfCancellationRequested();
                    StatusText.Text = T($"{imageType} wird gesucht …");
                    try
                    {
                        var first = (await FindSourcesAsync(imageType, loadingToken)).FirstOrDefault();
                        if (first is null) continue;
                        var bytes = await ReadOriginalAsync(first, loadingToken);
                        if (bytes is not { Length: > 0 }) continue;
                        using var preview = await Task.Run(() => ArtworkDecoder.DecodePreview(bytes, 300), loadingToken);
                        if (preview is not null) Results.Add(new() { Type = imageType, Data = bytes });
                    }
                    catch (Exception ex) when (ex is not OperationCanceledException) { StatusText.Text = T($"{imageType}: {ex.Message}"); }
                }
                if (Results.Count > 0) { loadingToken.ThrowIfCancellationRequested(); IsAccepted = true; Close(); }
                else if (automaticTypes is not null) Close();
                else StatusText.Text = T("Keine passenden Bilder gefunden. Du kannst einen einzelnen Bildtyp auswählen oder erneut laden.") + " " + providerError;
                return;
            }
            var source = await FindSourcesAsync(type, loadingToken);
            source = source.Where(s => s.Path is not null || s.Url is not null).DistinctBy(s => s.Path ?? s.Url).ToList();
            if (source.Count == 1 && providerError is null)
            {
                StatusText.Text = T("Ein Bild gefunden – Original wird übernommen …");
                await TakeSourceAsync(source[0], type, loadingToken);
                return;
            }
            if (source.Any(s => s.Path is not null)) await scraper.SetConfigAsync().WaitAsync(loadingToken);
            await Parallel.ForEachAsync(source, new ParallelOptions { MaxDegreeOfParallelism = 4, CancellationToken = loadingToken }, async (item, token) =>
            {
                try
                {
                    var bytes = item.Path is not null ? await scraper.GetImageFromTMDbByteArrayAsync(item.Path, ImageSize.Small, item.Kind, token).WaitAsync(token)
                        : await scraper.GetImageFromFanArtTvByteArrayAsync(item.Url, token).WaitAsync(token);
                    if (bytes is not { Length: > 0 }) return;
                    var preview = await ArtworkPreviewCache.Shared.AcquireAsync(item.Path ?? item.Url!, 300, bytes, token, retainOriginal: false);
                    if (preview is null) return;
                    await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        if (token.IsCancellationRequested) { preview.Dispose(); return; }
                        var position = candidates.Count(c => source.IndexOf(c.Source) < source.IndexOf(item));
                        candidates.Insert(position, new(item, type, preview.Image) { CachedPreview = preview });
                        StatusText.Text = T($"{candidates.Count} Vorschauen geladen …");
                    });
                }
                catch (OperationCanceledException) when (token.IsCancellationRequested)
                {
                    // Stopping previews is part of accepting the selected artwork.
                }
                catch (Exception ex) when (ex is not OperationCanceledException) { }
            });
            StatusText.Text = T($"{candidates.Count} Bilder zur Auswahl. {providerError}");
        }
        catch (OperationCanceledException) { if (!lifetime.IsCancellationRequested) StatusText.Text = T("Laden gestoppt. Bereits geladene Bilder können übernommen werden."); }
        catch (Exception ex) { StatusText.Text = T(ex.Message); }
        finally { previewLoading = null; SetBusy(false); }
    }
    private async Task<List<ArtworkSource>> FindSourcesAsync(string type, CancellationToken token)
    {
        if (tvdbId.HasValue) return await FindSeriesSourcesAsync(type, token);
        movieImages ??= await scraper.GetMovieImagesAsync(tmdbId, token);
        var paths = type switch { ImageTypes.Poster => movieImages.Posters, ImageTypes.Fanart => movieImages.Backdrops, ImageTypes.ClearLogo => movieImages.Logos, _ => null };
        var kind = type switch { ImageTypes.Fanart => MovieScrap2000.TMDbScraper.ImageType.Backdrop, ImageTypes.ClearLogo => MovieScrap2000.TMDbScraper.ImageType.Logo, _ => MovieScrap2000.TMDbScraper.ImageType.Poster };
        var source = paths?
            .Select(p => new ArtworkSource(p.FilePath, null, kind, $"TMDb · {p.Iso6391 ?? "neutral"} · {p.Width} × {p.Height}", LanguagePriority(p.Iso6391))).ToList() ?? [];
        if (!fanartLoaded)
        {
            try { fanartDetails = await scraper.GetFanartTvMovieAsync(tmdbId, token).WaitAsync(token); }
            catch (Exception ex) when (ex is not OperationCanceledException) { providerError = T("Fanart.tv ist derzeit nicht erreichbar."); }
            fanartLoaded = true;
        }
        var fanart = fanartDetails;
        var list = type switch { ImageTypes.Poster => fanart?.Poster, ImageTypes.Fanart => fanart?.Background, ImageTypes.ClearLogo => fanart is null ? null : fanart.HdLogo.Concat(fanart.Logo).ToList(), ImageTypes.ClearArt => fanart?.ClearArt, ImageTypes.Banner => fanart?.Banner, ImageTypes.DiskArt => fanart?.Disc, ImageTypes.KeyArt or ImageTypes.Landscape => fanart?.Thumb, _ => null };
        if (list is not null) source.AddRange(list.Select(p => new ArtworkSource(null, p.Url, kind, $"Fanart.tv · {p.Language}", LanguagePriority(p.Language))));
        return source.Where(s => s.Path is not null || s.Url is not null).OrderBy(s => s.Priority).DistinctBy(s => s.Path ?? s.Url).ToList();
    }
    private Task<byte[]?> ReadOriginalAsync(ArtworkSource source, CancellationToken token) => source.Path is not null
        ? scraper.GetImageFromTMDbByteArrayAsync(source.Path, ImageSize.Original, source.Kind, token).WaitAsync(token)
        : scraper.GetImageFromFanArtTvByteArrayAsync(source.Url, token).WaitAsync(token);
    private void SetBusy(bool value)
    {
        LoadingProgress.IsVisible = value;
        busy = value; LoadButton.IsEnabled = !value; ImageType.IsEnabled = !value;
        StopLoadingButton.IsVisible = value && previewLoading is not null;
        AcceptButton.IsEnabled = !accepting && (!value || previewLoading is not null) && Candidates.SelectedItem is ArtworkCandidate;
    }
    private void SelectionChanged(object? sender, SelectionChangedEventArgs e) => AcceptButton.IsEnabled = !accepting && (!busy || previewLoading is not null) && Candidates.SelectedItem is ArtworkCandidate;
    private async void OpenPreview(object? sender, Avalonia.Input.TappedEventArgs e)
    {
        e.Handled = true;
        if (busy || (sender as Control)?.DataContext is not ArtworkCandidate candidate) return;
        SetBusy(true); StatusText.Text = T("Originalbild wird geöffnet …");
        try
        {
            var source = candidate.Source;
            var bytes = source.Path is not null ? await scraper.GetImageFromTMDbByteArrayAsync(source.Path, ImageSize.Original, source.Kind).WaitAsync(lifetime.Token)
                : await scraper.GetImageFromFanArtTvByteArrayAsync(source.Url).WaitAsync(lifetime.Token);
            if (bytes is not { Length: > 0 }) { StatusText.Text = T("Das Bild konnte nicht geladen werden."); return; }
            var image = new VideoScrap2000.ViewModels.ArtworkViewModel(candidate.Type, "", candidate.Preview, bytes);
            var path = await image.GetOpenPathAsync();
            var file = await StorageProvider.TryGetFileFromPathAsync(path);
            StatusText.Text = T(file is not null && await DefaultFileLauncher.LaunchAsync(this, file) ? "Originalbild in der Standard-App geöffnet." : "Das Bild konnte nicht geöffnet werden.");
        }
        catch (OperationCanceledException) { }
        catch (Exception ex) { StatusText.Text = T(ex.Message); }
        finally { SetBusy(false); }
    }
    private async void Accept(object? sender, RoutedEventArgs e)
    {
        if (accepting || (busy && previewLoading is null) || Candidates.SelectedItem is not ArtworkCandidate candidate) return;
        accepting = true;
        AcceptButton.IsEnabled = false;
        await StopPreviewLoadingAsync();
        if (lifetime.IsCancellationRequested) return;
        SetBusy(true); StatusText.Text = T("Originalbild wird geladen …");
        try
        {
            await TakeSourceAsync(candidate.Source, candidate.Type, lifetime.Token);
        }
        catch (OperationCanceledException) { }
        catch (Exception ex) { StatusText.Text = T(ex.Message); }
        finally { accepting = false; SetBusy(false); }
    }
    private async Task TakeSourceAsync(ArtworkSource source, string type, CancellationToken token)
    {
        var bytes = source.Path is not null ? await scraper.GetImageFromTMDbByteArrayAsync(source.Path, ImageSize.Original, source.Kind, token).WaitAsync(token)
            : await scraper.GetImageFromFanArtTvByteArrayAsync(source.Url, token).WaitAsync(token);
        if (bytes is not { Length: > 0 }) { StatusText.Text = T("Das Bild konnte nicht geladen werden. Du kannst es erneut versuchen."); return; }
        using var preview = await Task.Run(() => ArtworkDecoder.DecodePreview(bytes, 300), token);
        if (preview is null) { StatusText.Text = T("Das Bild konnte nicht geladen werden. Du kannst es erneut versuchen."); return; }
        token.ThrowIfCancellationRequested();
        Result = new() { Type = type, Data = bytes };
        Results.Add(Result);
        IsAccepted = true;
        Close();
    }
    private void StopLoading(object? sender, RoutedEventArgs e) => previewLoading?.Cancel();
    private async Task StopPreviewLoadingAsync()
    {
        if (previewLoading is null) return;
        previewLoading.Cancel();
        await LoadingTask;
    }
    private void Cancel(object? sender, RoutedEventArgs e) => Close();
}

public sealed record ArtworkSource(string? Path, string? Url, ImageType Kind, string Caption, int Priority = 0);
public sealed record ArtworkCandidate(ArtworkSource Source, string Type, Bitmap Preview) : IDisposable
{
    public string Caption => Source.Caption;
    internal ArtworkPreviewCache.Lease? CachedPreview { get; init; }
    public void Dispose()
    {
        if (CachedPreview is { } preview) preview.Dispose();
        else Preview.Dispose();
    }
}
