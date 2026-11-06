using static VideoScrap2000.Localization.Locale;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using MovieScrap2000;
using MovieScrap2000.TMDbScraper;
namespace VideoScrap2000.Views;
public partial class ScrapeWindow : Window
{
    private readonly TMDbScraperManager scraper = new();
    private readonly CancellationTokenSource lifetime = new();
    private CancellationTokenSource? posterLoading;
    private bool busy;
    private readonly string language;
    private readonly Func<string, CancellationToken, Task<IReadOnlyList<SearchTv>>>? seriesSearch;
    public TMDbMovie? Result { get; private set; }
    public SearchTv? SeriesResult { get; private set; }
    public ScrapeWindow() : this("", "") { }
    public ScrapeWindow(string title, string imdb, string year = "", string language = "de-DE")
    {
        InitializeComponent();
        this.language = language;
        var parsed = MovieNameParser.ParseSearchTitle(title);
        SearchText.Text = string.IsNullOrWhiteSpace(imdb) ? parsed.Title : imdb;
        YearText.Text = string.IsNullOrWhiteSpace(year) ? parsed.Year?.ToString() : year;
        Opened += async (_, _) => await SearchAsync();
        Closed += (_, _) => { lifetime.Cancel(); ClearResults(); scraper.Dispose(); };
    }
    internal ScrapeWindow(string title, Func<string, CancellationToken, Task<IReadOnlyList<SearchTv>>> search)
        : this(title, "")
    {
        seriesSearch = search;
        SearchText.PlaceholderText = T("Serientitel");
        YearText.Text = "";
        YearText.IsVisible = false;
        SearchForm.ColumnDefinitions[1].Width = new Avalonia.Controls.GridLength(0);
    }
    private async void Search(object? sender, RoutedEventArgs e) => await SearchAsync();

    private async Task SearchAsync()
    {
        if (busy || string.IsNullOrWhiteSpace(SearchText.Text)) return;
        busy = true; SearchButton.IsEnabled = false; AcceptButton.IsEnabled = false;
        ClearResults();
        StatusText.Text = T("TMDb wird durchsucht …");
        try
        {
            var parsedTitle = MovieNameParser.ParseSearchTitle(SearchText.Text);
            var query = parsedTitle.Title;
            if (query.Length == 0) { StatusText.Text = T("Kein Treffer"); return; }
            var year = parsedTitle.Year;
            if (!string.IsNullOrWhiteSpace(YearText.Text))
            {
                if (!int.TryParse(YearText.Text, out var parsed) || parsed < 1888 || parsed > DateTime.UtcNow.Year + 1)
                { StatusText.Text = T("Bitte ein gültiges Erscheinungsjahr eingeben oder das Jahr leeren."); return; }
                year = parsed;
            }
            IReadOnlyList<SearchMovieTvBase> results;
            if (seriesSearch is not null)
                results = await seriesSearch(query, lifetime.Token).WaitAsync(lifetime.Token);
            else
                results = query.StartsWith("tt", StringComparison.OrdinalIgnoreCase)
                    ? await scraper.FindMovieByImdbIdAsync(query, language).WaitAsync(lifetime.Token)
                    : await scraper.SearchMoviesAsync(query, lifetime.Token, year, language);
            lifetime.Token.ThrowIfCancellationRequested();
            var rows = results.Select(movie => new MovieSearchResult(movie)).ToArray();
            Results.ItemsSource = rows;
            if (rows.Length > 0) Results.SelectedIndex = 0;
            posterLoading = CancellationTokenSource.CreateLinkedTokenSource(lifetime.Token);
            _ = LoadPostersAsync(rows, posterLoading.Token);
            StatusText.Text = T(results.Count switch
            {
                0 => "Kein Treffer",
                1 => "Ein Treffer",
                _ => $"{results.Count} Treffer"
            });
        }
        catch (OperationCanceledException) { }
        catch (HttpRequestException ex)
        {
            System.Diagnostics.Trace.WriteLine(ex);
            StatusText.Text = T("Suche derzeit nicht möglich. Bitte die Verbindung prüfen und erneut versuchen.");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Trace.WriteLine(ex);
            StatusText.Text = T("Die Suche konnte nicht abgeschlossen werden. Bitte erneut versuchen.");
        }
        finally { busy = false; SearchButton.IsEnabled = true; AcceptButton.IsEnabled = Results.SelectedItem is MovieSearchResult; }
    }
    private void ClearResults()
    {
        posterLoading?.Cancel();
        posterLoading?.Dispose();
        posterLoading = null;
        var rows = Results.ItemsSource as MovieSearchResult[];
        Results.ItemsSource = null;
        if (rows is not null)
            foreach (var row in rows) row.DisposePoster();
    }
    private async Task LoadPostersAsync(MovieSearchResult[] rows, CancellationToken token)
    {
        var withPosters = rows.Where(row => !string.IsNullOrWhiteSpace(row.Movie.PosterPath)).ToArray();
        if (withPosters.Length == 0) return;
        try
        {
            await scraper.SetConfigAsync().WaitAsync(token);
            await Parallel.ForEachAsync(withPosters, new ParallelOptions { MaxDegreeOfParallelism = 4, CancellationToken = token }, async (row, ct) =>
            {
                try
                {
                    var bytes = await scraper.GetImageFromTMDbByteArrayAsync(row.Movie.PosterPath, ImageSize.Small, ImageType.Poster, ct);
                    if (bytes is not { Length: > 0 }) return;
                    ct.ThrowIfCancellationRequested();
                    var preview = await ArtworkPreviewCache.Shared.AcquireAsync(row.Movie.PosterPath!, 88, bytes, ct, retainOriginal: false);
                    if (preview is null) return;
                    await Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        if (token.IsCancellationRequested) preview.Dispose();
                        else { row.CachedPreview = preview; row.Poster = preview.Image; }
                    });
                }
                catch (Exception ex) when (ex is not OperationCanceledException) { System.Diagnostics.Trace.WriteLine(ex); }
            });
        }
        catch (OperationCanceledException) { }
        catch (Exception ex) { System.Diagnostics.Trace.WriteLine(ex); }
    }
    private void SelectionChanged(object? sender, SelectionChangedEventArgs e) => AcceptButton.IsEnabled = !busy && Results.SelectedItem is MovieSearchResult;
    private async void Accept(object? sender, RoutedEventArgs e)
    {
        if (busy || Results.SelectedItem is not MovieSearchResult row) return;
        if (row.Movie is SearchTv show)
        {
            SeriesResult = show;
            Close();
            return;
        }
        busy = true; AcceptButton.IsEnabled = false; SearchButton.IsEnabled = false;
        StatusText.Text = T("Filmdetails werden geladen …");
        try
        {
            Result = await scraper.GetMovieFullDetailsAsync(row.Movie.Id, language).WaitAsync(lifetime.Token);
            if (Result is not null) Close(); else StatusText.Text = T("Für diesen Film wurden keine Details gefunden.");
        }
        catch (OperationCanceledException) { }
        catch (Exception ex) { StatusText.Text = T(ex.Message); }
        finally { busy = false; SearchButton.IsEnabled = true; AcceptButton.IsEnabled = true; }
    }
    private void Cancel(object? sender, RoutedEventArgs e) => Close();
}

public sealed class MovieSearchResult(SearchMovieTvBase movie) : ObservableObject
{
    internal ArtworkPreviewCache.Lease? CachedPreview { get; set; }
    private Bitmap? poster;
    public SearchMovieTvBase Movie { get; } = movie;
    public string Title => Movie switch { SearchMovie film => film.Title ?? "", SearchTv show => show.Name ?? "", _ => "" };
    public string Year => (Movie switch { SearchMovie film => film.ReleaseDate, SearchTv show => show.FirstAirDate, _ => null })?.ToString("yyyy") ?? "";
    public Bitmap? Poster { get => poster; set => SetProperty(ref poster, value); }
    public void DisposePoster()
    {
        if (CachedPreview is { } preview) preview.Dispose();
        else Poster?.Dispose();
        CachedPreview = null;
        Poster = null;
    }
}
