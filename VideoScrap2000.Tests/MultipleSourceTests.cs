using System.Text.Json;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Microsoft.Data.Sqlite;
using MovieScrap2000;
using VideoScrap2000.ViewModels;
using VideoScrap2000.Views;
using Xunit;

namespace VideoScrap2000.Tests;

public sealed class MultipleSourceTests : IDisposable
{
    private readonly string root = Path.Combine(Path.GetTempPath(), "VideoScrap2000-sources", Guid.NewGuid().ToString("N"));
    private static CancellationToken Ct => TestContext.Current.CancellationToken;
    public MultipleSourceTests() => Directory.CreateDirectory(root);

    [Fact]
    public void LegacyPathsMigrateAndExplicitlyEmptyListsStayEmptyAfterSerialization()
    {
        var first = Path.Combine(root, "First");
        var second = Path.Combine(root, "Second");
        var legacy = JsonSerializer.Deserialize<AppSettings>(JsonSerializer.Serialize(new { LibraryPath = first, SeriesPath = second }))!;
        Assert.Equal(new[] { first }, legacy.GetMovieFolders());
        Assert.Equal(new[] { second }, legacy.GetSeriesFolders());
        legacy.MovieFolders = [first, second];
        legacy.SeriesFolders = [];
        var restored = JsonSerializer.Deserialize<AppSettings>(JsonSerializer.Serialize(legacy))!;
        Assert.Equal(new[] { first, second }, restored.GetMovieFolders());
        Assert.Empty(restored.GetSeriesFolders());
        Assert.Equal(new[] { first }, LibrarySources.Normalize([first, first + Path.DirectorySeparatorChar, Path.Combine(first, "."), " "]));
    }

    [AvaloniaFact]
    public async Task MovieSourcesCombineSkipDuplicatesAndKeepOtherSourcesWhenOneIsOffline()
    {
        var first = CreateMovieSource("First");
        var second = CreateMovieSource("Second");
        var database = new DatabaseManager(Path.Combine(root, "cache.sqlite"));
        await database.InitializeDatabaseAsync();
        using var metadata = new MetadataService(database);
        var scanner = new MovieManager(metadata);
        var progress = new List<ScanProgress>();
        scanner.DetailedProgress += (_, value) => progress.Add(value);
        await scanner.ScanDirectoriesAsync([first, second, Path.Combine(first, "."), Path.Combine(root, "Offline")], Ct);
        Assert.Equal(1, scanner.ScanErrors);
        Assert.Equal(2, progress[^1].Completed);
        Assert.Equal(2, progress[^1].Total);
        Assert.Equal(2, (await metadata.LoadMoviesAsync()).Count);
        var settings = new AppSettings { MovieFolders = [first, second] };
        using var model = new MainViewModel(database, settings);
        await model.InitializeAsync();
        await model.ArtworkLoadingTask;
        Assert.Equal(2, model.Movies.Count);
        Assert.Equal(2, model.Movies.Select(movie => movie.VersionLabel).Distinct().Count());
        settings.MovieFolders = [second];
        await model.ReloadAsync(Ct);
        await model.ArtworkLoadingTask;
        Assert.StartsWith(second, Assert.Single(model.Movies).FolderPath);
        settings.MovieFolders = [];
        await model.ReloadAsync(Ct);
        await model.ArtworkLoadingTask;
        Assert.Empty(model.Movies);
        Assert.Equal(2, (await metadata.LoadMoviesAsync()).Count);
        Assert.True(File.Exists(Path.Combine(first, "Shared", "film.mkv")));
        Assert.True(File.Exists(Path.Combine(second, "Shared", "film.mkv")));
    }

    [Fact]
    public void SeriesSourcesCombineAndReportUnavailableSourcesWithoutDuplicates()
    {
        var first = CreateSeriesSource("First");
        var second = CreateSeriesSource("Second");
        var errors = new List<string>();
        var progress = new List<ScanProgress>();
        var shows = SeriesLibrary.ScanRoots([first, second, first, Path.Combine(root, "Offline")], Ct,
            (path, _) => errors.Add(path), value => progress.Add(value));
        Assert.Equal(2, shows.Count);
        Assert.Equal(2, shows.Select(show => show.FolderPath).Distinct().Count());
        Assert.Single(errors);
        Assert.Equal(2, progress[^1].Completed);
        Assert.Equal(2, progress[^1].Total);
        Assert.Equal(1, progress[^1].Errors);
    }

    [AvaloniaFact]
    public async Task AddingASecondSeriesSourceInvalidatesTheScannedSourceSet()
    {
        var first = CreateSeriesSource("First");
        var second = CreateSeriesSource("Second");
        var settings = new AppSettings { SeriesFolders = [first] };
        using var main = new MainViewModel(settings: settings);
        await main.Series.ScanAsync();
        Assert.Single(main.Series.Shows);
        Assert.False(main.Series.NeedsScan);
        settings.SeriesFolders.Add(second);
        Assert.True(main.Series.NeedsScan);
        await main.Series.ScanAsync();
        Assert.Equal(2, main.Series.Shows.Count);
        Assert.False(main.Series.NeedsScan);
    }

    [AvaloniaFact]
    public void SettingsListsSupportMultipleSelectionDeleteAndCancellation()
    {
        var sources = new[] { Path.Combine(root, "One"), Path.Combine(root, "Two"), Path.Combine(root, "Three") };
        using var main = new MainViewModel(settings: new AppSettings { MovieFolders = [.. sources], SeriesFolders = [.. sources] });
        var window = new SettingsWindow(main);
        window.Show();
        try
        {
            var draft = Assert.IsType<SettingsViewModel>(window.DataContext);
            var list = window.FindControl<ListBox>("MovieSources")!;
            list.Selection.Select(0); list.Selection.Select(1);
            list.RaiseEvent(new KeyEventArgs { RoutedEvent = InputElement.KeyDownEvent, Key = Key.Delete });
            Assert.Equal(new[] { sources[2] }, draft.MovieFolders);
            Assert.Equal(sources[2], list.SelectedItem);
            list.RaiseEvent(new KeyEventArgs { RoutedEvent = InputElement.KeyDownEvent, Key = Key.Delete });
            Assert.Empty(draft.MovieFolders);
            var series = window.FindControl<ListBox>("SeriesSources")!;
            series.Selection.Select(1);
            series.RaiseEvent(new KeyEventArgs { RoutedEvent = InputElement.KeyDownEvent, Key = Key.Delete });
            Assert.Equal(2, draft.SeriesFolders.Count);
            Assert.Equal(3, main.MovieFolders.Count);
            Assert.Equal(3, main.SeriesFolders.Count);
        }
        finally { window.Close(); }
        Assert.Equal(3, main.MovieFolders.Count);
    }

    private string CreateMovieSource(string name)
    {
        var source = Path.Combine(root, name);
        var folder = Path.Combine(source, "Shared");
        Directory.CreateDirectory(folder);
        File.WriteAllBytes(Path.Combine(folder, "film.mkv"), [0]);
        File.WriteAllText(Path.Combine(folder, "movie.nfo"), "<movie><title>Shared</title><year>2024</year><uniqueid type=\"imdb\">tt12345</uniqueid></movie>");
        return source;
    }

    private string CreateSeriesSource(string name)
    {
        var source = Path.Combine(root, name);
        var folder = Path.Combine(source, "Show");
        Directory.CreateDirectory(folder);
        File.WriteAllBytes(Path.Combine(folder, "Show.S01E01.mkv"), [0]);
        return source;
    }

    public void Dispose()
    {
        SqliteConnection.ClearAllPools();
        Directory.Delete(root, true);
    }
}
