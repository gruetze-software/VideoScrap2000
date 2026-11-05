using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;
using MovieScrap2000;
using VideoScrap2000.Views;
using Xunit;

namespace VideoScrap2000.Tests;

public sealed class DemoLibraryTests
{
    [AvaloniaFact]
    public async Task DemoLoadsMoviesEpisodesAndAiDialogWithoutChangingUserSettings()
    {
        var root = Path.Combine(Path.GetTempPath(), "VideoScrap2000-Demo-Test-" + Guid.NewGuid().ToString("N"));
        var settingsPath = Path.Combine(Utils.GetOptionsPath(), "settings.json");
        var originalSettings = File.Exists(settingsPath) ? File.ReadAllText(settingsPath) : null;
        MainWindow? window = null;
        try
        {
            using var model = VideoScrap2000.DemoLibrary.CreateModel(root);
            await model.InitializeAsync();
            for (var i = 0; !model.IsDetailReady && i < 300; i++) await Task.Delay(10, TestContext.Current.CancellationToken);
            await model.ArtworkLoadingTask;
            Assert.True(model.IsDemo);
            Assert.Equal(6, model.Movies.Count);
            var distinctMotifs = Directory.EnumerateFiles(model.LibraryPath, "*-fanart.jpg", SearchOption.AllDirectories)
                .Select(file => Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(File.ReadAllBytes(file)))).Distinct().Count();
            Assert.Equal(6, distinctMotifs);
            Assert.Equal("The Last Signal", model.Title);
            Assert.Equal(VideoScrap2000.DemoLibrary.SignalPlot, model.Plot);
            Assert.True(model.CanGenerateOutline);
            Assert.True(string.IsNullOrEmpty(model.Outline));
            Assert.NotNull(model.Poster);
            Assert.NotNull(model.FanartBackground);
            Assert.Equal(4, model.Artwork.Count);
            Assert.DoesNotContain(root, model.SelectionPath);

            var movieRoot = model.LibraryPath;
            var seriesRoot = model.SeriesPath;
            model.ApplySettings("do-not-use-this-real-movie-folder", "do-not-use-this-real-series-folder", 1, 2, 2, 2, true);
            Assert.Equal(movieRoot, model.LibraryPath);
            Assert.Equal(seriesRoot, model.SeriesPath);
            await model.ScanCommand.ExecuteAsync(null);
            for (var i = 0; !model.IsDetailReady && i < 300; i++) await Task.Delay(10, TestContext.Current.CancellationToken);
            model.Outline = "A demonstration-only edit.";
            await model.SaveCommand.ExecuteAsync(null);
            Assert.Equal("A demonstration-only edit.", (string?)System.Xml.Linq.XDocument.Load(
                Path.ChangeExtension(model.SelectedMovie!.VideoFilePath, ".nfo")).Root!.Element("outline"));
            Assert.Equal(originalSettings, File.Exists(settingsPath) ? File.ReadAllText(settingsPath) : null);

            await model.Series.ScanAsync();
            Assert.Equal(2, model.Series.Shows.Count);
            Assert.Equal("North Harbor", model.Series.SelectedShow?.Title);
            Assert.Equal(12, model.Series.Episodes.Count);
            Assert.NotNull(model.Series.SeasonPosterPreview);
            Assert.NotNull(model.Series.SeasonBannerPreview);
            Assert.Empty(await model.Series.FindShowsAsync("a title that must not go online"));
            Assert.Null(model.Series.CreateArtworkSearch());

            model.ThemeIndex = 2;
            model.SelectedMovie = model.Movies.First(movie => movie.Title == "The Last Signal");
            for (var i = 0; !model.IsDetailReady && i < 300; i++) await Task.Delay(10, TestContext.Current.CancellationToken);
            window = new MainWindow(model, false);
            window.Show();
            var button = window.FindControl<Button>("OutlinePromptButton")!;
            Assert.Equal("Generate plot outline with AI", button.Content);
            button.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            var dialog = Assert.Single(window.OwnedWindows.OfType<OutlinePromptWindow>());
            var prompt = dialog.FindControl<TextBox>("PromptText")!.Text!;
            Assert.Contains("239 characters including spaces", prompt);
            Assert.Contains(VideoScrap2000.DemoLibrary.SignalPlot, prompt);
            dialog.Close();
            window.Close();
            window = null;
        }
        finally
        {
            window?.Close();
            Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
            Directory.Delete(root, true);
        }
    }
}
