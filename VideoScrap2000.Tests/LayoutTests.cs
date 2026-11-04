using Avalonia;
using Avalonia.Controls;
using Avalonia.LogicalTree;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Styling;
using Avalonia.VisualTree;
using MovieScrap2000;
using VideoScrap2000.Views;
using VideoScrap2000.ViewModels;
using Xunit;

[assembly: AvaloniaTestApplication(typeof(VideoScrap2000.Tests.TestApp))]
namespace VideoScrap2000.Tests;
public static class TestApp
{
    public static AppBuilder BuildAvaloniaApp() => AppBuilder.Configure<App>().UseSkia().WithInterFont().UseHeadless(new AvaloniaHeadlessPlatformOptions { UseHeadlessDrawing = false });
}
public class LayoutTests
{
    [AvaloniaFact]
    public async Task CancellingTheModalProgressDialogStopsTheOperationAndClosesIt()
    {
        using var model = new MainViewModel(settings: new AppSettings { Language = "de" });
        var window = new MainWindow(model, initialize: false);
        window.Show();
        try
        {
            var operation = model.RunAsync(async token =>
            {
                model.Status = "Filmsammlung wird geladen …";
                await Task.Delay(Timeout.Infinite, token);
            }, reloadAfterCancellation: false);

            var dialog = Assert.IsType<LibraryProgressWindow>(Assert.Single(window.OwnedWindows));
            Assert.True(dialog.GetVisualDescendants().OfType<ProgressBar>().Single().IsIndeterminate);
            var cancel = dialog.FindControl<Button>("CancelButton")!;
            cancel.RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(Button.ClickEvent));

            await operation.WaitAsync(TimeSpan.FromSeconds(3));
            Assert.Empty(window.OwnedWindows);
            Assert.False(model.IsBusy);
            Assert.Equal("Vorgang abgebrochen.", model.Status);
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public async Task UnreachableMovieFolderKeepsCachedDetailsAndStopsImageLoading()
    {
        var root = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N")); Directory.CreateDirectory(root);
        var database = new DatabaseManager(Path.Combine(root, "cache.sqlite"));
        await database.InitializeDatabaseAsync();
        using var service = new MetadataService(database);
        var folder = Path.Combine(root, "not available");
        try
        {
            await service.SaveMovieAsync(new Movie { Title = "Cached title", Plot = "Cached plot" }, folder, Path.Combine(folder, "movie.mkv"), false, TestContext.Current.CancellationToken, writeFiles: false);
            using var model = new MainViewModel(database, new AppSettings { Language = "de" });
            await model.InitializeAsync();
            for (var i = 0; (model.IsImagesLoading || !model.IsDetailReady) && i < 200; i++) await Task.Delay(10, TestContext.Current.CancellationToken);
            Assert.True(model.IsDetailReady);
            Assert.False(model.IsImagesLoading);
            Assert.Equal("Cached title", model.Title);
            Assert.Equal("Cached plot", model.Plot);
            Assert.Empty(model.Artwork);
            var unavailable = Assert.Single(model.Movies);
            Assert.True(unavailable.IsFolderUnavailable);
            var window = new MainWindow(model, initialize: false);
            window.Show();
            window.UpdateLayout();
            var row = Assert.Single(window.GetVisualDescendants().OfType<ListBoxItem>(), item => ReferenceEquals(item.DataContext, unavailable));
            Assert.NotNull(row.ContextMenu);
            await model.RemoveUnavailableMovieAsync(unavailable);
            await model.ReloadAsync();
            Assert.Empty(model.Movies);
            Assert.Null(model.SelectedMovie);
            window.Close();
        }
        finally { Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools(); Directory.Delete(root, true); }
    }
    [AvaloniaFact]
    public void SettingsEditsStayInDialogUntilSaved()
    {
        using var model = new MainViewModel(settings: new AppSettings { LibraryPath = "movies", SeriesPath = "series", Theme = "Dark" });
        var window = new SettingsWindow(model);
        var draft = Assert.IsType<SettingsViewModel>(window.DataContext);
        draft.LibraryPath = "different"; draft.SeriesPath = "different series"; draft.ThemeIndex = 1;
        window.Close();
        Assert.Equal("movies", model.LibraryPath);
        Assert.Equal("series", model.SeriesPath);
        Assert.Equal(2, model.ThemeIndex);
        Assert.DoesNotContain(window.GetLogicalDescendants().OfType<TextBlock>(), t => t.Text is "Ganz nach deinem Geschmack" or "Make it yours");
    }

    [AvaloniaFact]
    public async Task ClearingSelectionStopsLoadingIndicatorAndDiscardsOldImages()
    {
        using var model = new MainViewModel(settings: new AppSettings { Language = "de" });
        model.SelectedMovie = new MovieViewModel { Title = "Test", FolderPath = Path.GetTempPath() };
        Assert.True(model.IsImagesLoading);
        model.SelectedMovie = null;
        Assert.False(model.IsImagesLoading);
        await Task.Delay(100, TestContext.Current.CancellationToken);
        Assert.Empty(model.Artwork);
        Assert.Null(model.Poster);
    }
    [AvaloniaFact]
    public async Task MissingNfoSortsFirstWithoutChangingSelectionOrEdits()
    {
        var root = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N")); Directory.CreateDirectory(root);
        var database = new DatabaseManager(Path.Combine(root, "cache.sqlite"));
        using var metadata = new MetadataService(database);
        await database.InitializeDatabaseAsync();
        try
        {
            foreach (var title in new[] { "A mit XML", "Z ohne NFO" })
            {
                var folder = Path.Combine(root, title); Directory.CreateDirectory(folder);
                var video = Path.Combine(folder, "Film.mkv"); File.WriteAllBytes(video, [0]);
                if (title.StartsWith('A')) File.WriteAllText(Path.Combine(folder, "movie.xml"), "<movie/>");
                await metadata.SaveMovieAsync(new Movie { Title = title }, folder, video, false, TestContext.Current.CancellationToken, writeFiles: false);
            }
            using var model = new MainViewModel(database, new AppSettings());
            await model.InitializeAsync();
            var selected = model.SelectedMovie;
            await model.RefreshFileFactsAsync();
            Assert.Equal("Z ohne NFO", model.Movies[0].Title);
            Assert.True(model.Movies[0].MissingMetadata);
            Assert.Same(selected, model.SelectedMovie);
            for (var i = 0; !model.IsDetailReady && i < 100; i++) await Task.Delay(10, TestContext.Current.CancellationToken);
            model.Title = "Noch nicht gespeichert";
            await model.RefreshFileFactsAsync();
            Assert.Equal("Noch nicht gespeichert", model.Title);
        }
        finally { Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools(); Directory.Delete(root, true); }
    }
    [AvaloniaTheory]
    [InlineData(false)] [InlineData(true)]
    public void MainWindowAndDialogsLayoutInBothThemes(bool dark)
    {
        using var model = new MainViewModel(settings: new AppSettings { Language = "de" });
        var main = new MainWindow(model, false) { Width = 960, Height = 680 };
        Application.Current!.RequestedThemeVariant = dark ? ThemeVariant.Dark : ThemeVariant.Light;
        main.Show();
        try
        {
            main.UpdateLayout();
            Assert.Contains(main.GetVisualDescendants().OfType<TextBlock>(), t => t.Text == "Platz für großes Kino.");
            foreach (var window in new Window[] { new SettingsWindow(model), new AboutWindow(), new ScrapeWindow(), new ArtworkWindow() })
            {
                window.Show(); window.UpdateLayout();
                Assert.True(window.Bounds.Width > 0);
                Assert.DoesNotContain(window.GetVisualDescendants().OfType<Control>(), c => double.IsNaN(c.Bounds.Width));
                window.Close();
            }
        }
        finally { main.Close(); }
    }
}
