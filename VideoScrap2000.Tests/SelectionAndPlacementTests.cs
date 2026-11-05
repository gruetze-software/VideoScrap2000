using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.VisualTree;
using MovieScrap2000;
using VideoScrap2000.ViewModels;
using VideoScrap2000.Views;
using Xunit;

namespace VideoScrap2000.Tests;

public sealed class SelectionAndPlacementTests
{
    [AvaloniaFact]
    public async Task SwitchingMovieAndSeriesSectionsSelectsTheFirstEntry()
    {
        var root = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        MainWindow? window = null;
        try
        {
            using var model = VideoScrap2000.DemoLibrary.CreateModel(root);
            await model.InitializeAsync();
            await model.ArtworkLoadingTask;
            Assert.Same(model.Movies[0], model.SelectedMovie);
            window = new MainWindow(model, false);
            window.Show();
            var tabs = window.FindControl<TabControl>("MovieDetailTabs")!;
            tabs.SelectedIndex = 1;
            window.UpdateLayout();
            var cast = window.FindControl<ListBox>("ActorList")!;
            Assert.Same(model.Actors[0], cast.SelectedItem);
            model.SelectedActors.Clear(); model.SelectedActors.Add(model.Actors.Last());
            tabs.SelectedIndex = 0; tabs.SelectedIndex = 1;
            Assert.Same(model.Actors[0], Assert.Single(model.SelectedActors));
            tabs.SelectedIndex = 3;
            Assert.Same(model.Artwork[0], model.SelectedArtwork);
            model.SelectedArtwork = model.Artwork.Last();
            tabs.SelectedIndex = 0; tabs.SelectedIndex = 3;
            Assert.Same(model.Artwork[0], model.SelectedArtwork);
            Assert.Equal(model.Artwork[0].Type, model.ArtworkType);

            var library = window.FindControl<TabControl>("LibraryTabs")!;
            library.SelectedIndex = 1;
            for (var i = 0; (model.IsBusy || model.Series.Shows.Count == 0) && i < 300; i++)
                await Task.Delay(10, TestContext.Current.CancellationToken);
            window.UpdateLayout();
            var series = model.Series;
            Assert.Same(series.Shows[0], series.SelectedShow);
            var seriesView = window.GetVisualDescendants().OfType<SeriesView>().Single();
            var seriesTabs = seriesView.FindControl<TabControl>("SeriesDetailTabs")!;
            seriesTabs.SelectedIndex = 1;
            Assert.Same(series.ShowEditor!.Actors[0], series.SelectedActor);
            series.SelectedActor = series.ShowEditor.Actors.Last();
            seriesTabs.SelectedIndex = 0; seriesTabs.SelectedIndex = 1;
            Assert.Same(series.ShowEditor.Actors[0], series.SelectedActor);
            seriesTabs.SelectedIndex = 2;
            series.SelectedEpisode = series.Episodes.Last();
            seriesTabs.SelectedIndex = 0; seriesTabs.SelectedIndex = 2;
            Assert.Same(series.Episodes[0], series.SelectedEpisode);
            seriesTabs.SelectedIndex = 3;
            series.SelectedSeriesArtwork = series.ArtworkTiles.Last();
            seriesTabs.SelectedIndex = 0; seriesTabs.SelectedIndex = 3;
            Assert.Same(series.ArtworkTiles[0], series.SelectedSeriesArtwork);

            model.SelectedMovie = model.Movies.Last();
            library.SelectedIndex = 0;
            await model.ArtworkLoadingTask;
            Assert.Same(model.Movies[0], model.SelectedMovie);
            window.Close(); window = null;
        }
        finally
        {
            window?.Close();
            Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
            Directory.Delete(root, true);
        }
    }

    [AvaloniaTheory]
    [InlineData(false)]
    [InlineData(true)]
    public void WindowRestoresNormalSizeAndLastNonMinimizedState(bool maximized)
    {
        var root = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            var store = new WindowPlacementStore(Path.Combine(root, "window.json"));
            using var firstModel = new MainViewModel(settings: new AppSettings { Language = "en" });
            var first = new MainWindow(firstModel, false, store);
            first.Show();
            first.Width = 1100; first.Height = 760;
            first.UpdateLayout();
            if (maximized) first.WindowState = WindowState.Maximized;
            first.WindowState = WindowState.Minimized;
            first.Close();
            Assert.Equal(new WindowPlacement(1100, 760, maximized), store.Load());

            using var secondModel = new MainViewModel(settings: new AppSettings { Language = "en" });
            var second = new MainWindow(secondModel, false, store);
            try
            {
                Assert.Equal(1100, second.Width);
                Assert.Equal(760, second.Height);
                Assert.Equal(maximized ? WindowState.Maximized : WindowState.Normal, second.WindowState);
                second.Show();
            }
            finally { second.Close(); }
        }
        finally { Directory.Delete(root, true); }
    }

    [Fact]
    public void InvalidOrMissingWindowPreferencesFallBackToUsableDefaults()
    {
        var root = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        var file = Path.Combine(root, "window.json");
        var store = new WindowPlacementStore(file);
        try
        {
            Assert.Equal(new WindowPlacement(), store.Load());
            File.WriteAllText(file, "not valid JSON");
            Assert.Equal(new WindowPlacement(), store.Load());
            store.Save(new(double.NaN, double.PositiveInfinity, true));
            Assert.Equal(new WindowPlacement(1280, 860, true), store.Load());
            File.WriteAllText(file, "{\"Width\":-5,\"Height\":0}");
            Assert.Equal(new WindowPlacement(), store.Load());
        }
        finally { Directory.Delete(root, true); }
    }
}
