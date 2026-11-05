using System.Xml.Linq;
using Avalonia.Headless.XUnit;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.LogicalTree;
using Avalonia.VisualTree;
using Avalonia.Media.Imaging;
using MovieScrap2000;
using VideoScrap2000.ViewModels;
using VideoScrap2000.Views;
using Xunit;

namespace VideoScrap2000.Tests;
public class MetadataEditorTests
{
    [AvaloniaTheory]
    [InlineData("de")] [InlineData("en")]
    public async Task ExtendedMetadataAndActorEditsSurviveSavingAndReadingNfo(string language)
    {
        var root = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N")); Directory.CreateDirectory(root);
        var database = new DatabaseManager(Path.Combine(root, "cache.sqlite"));
        await database.InitializeDatabaseAsync();
        using var service = new MetadataService(database);
        var video = Path.Combine(root, "Film.mkv"); File.WriteAllBytes(video, [0]);
        try
        {
            await service.SaveMovieAsync(new Movie { Title = "Film", SortTitle = "Original sort", Premiered = "2020-01-02", Tags = ["Old tag"],
                Set = new MovieSetInfo { Name = "Old set", Overview = "Keep this overview" }, Genres = ["Drama"],
                FileInfo = new() { StreamDetails = new() { VideoStream = new() { Width = 1920, Height = 1080, Codec = "h264" }, AudioStreams = [new() { Channels = 6, Codec = "ac3", Language = "deu" }] } }
            }, root, video, false, TestContext.Current.CancellationToken, writeFiles: false);
            using var model = new MainViewModel(database, new AppSettings { Language = language });
            await model.InitializeAsync();
            for (var i = 0; !model.IsDetailReady && i < 200; i++) await Task.Delay(10, TestContext.Current.CancellationToken);
            Assert.True(model.IsDetailReady);
            Assert.Equal("Original sort", model.SortTitle);
            Assert.Contains("1920 × 1080", model.TechnicalDetails);
            model.SortTitle = "Film, Der"; model.Premiere = new DateTimeOffset(2020, 2, 3, 0, 0, 0, TimeSpan.Zero);
            model.Outline = "Kurzinhalt"; model.Credits = "Writer One; Writer Two"; model.Tmdb = "550";
            model.Collection = "New set"; model.CollectionId = "42"; model.Certification = "FSK 12";
            model.Countries = "Germany; United States"; model.Tags = "Test; Science"; model.Runtime = "110"; model.Rating = "7,5";
            model.Genres = "Drama; Science Fiction";
            model.Actors.Add(new ActorEditor { Name = "Example Actor", Role = "Lead", TmdbId = 123, Order = 0 });
            model.Actors.Add(new ActorEditor { Name = "Remove One", Role = "Extra", Order = 1 });
            model.Actors.Add(new ActorEditor { Name = "Remove Two", Role = "Extra", Order = 2 });
            var window = new MainWindow(model, false) { Width = 1280, Height = 900 };
            window.Show();
            try
            {
                var tabs = window.FindControl<TabControl>("MovieDetailTabs")!;
                tabs.SelectedIndex = 1; window.UpdateLayout();
                var actorList = window.FindControl<ListBox>("ActorList")!;
                actorList.Selection.Select(1); actorList.Selection.Select(2);
                Assert.Equal(2, model.SelectedActors.Count);
                var delete = new KeyEventArgs { RoutedEvent = InputElement.KeyDownEvent, Key = Key.Delete };
                actorList.RaiseEvent(delete);
                Assert.True(delete.Handled);
                Assert.Single(model.Actors);
                Assert.Equal("Example Actor", model.Actors[0].Name);
                Assert.Same(model.Actors[0], actorList.SelectedItem);
                Assert.True(actorList.ContainerFromItem(actorList.SelectedItem!)!.IsFocused);
                model.Actors.Add(new ActorEditor { Name = "Middle" });
                model.Actors.Add(new ActorEditor { Name = "Last" });
                actorList.SelectedIndex = 1;
                window.UpdateLayout();
                actorList.ContainerFromIndex(1)!.Focus();
                actorList.RaiseEvent(new KeyEventArgs { RoutedEvent = InputElement.KeyDownEvent, Key = Key.Delete });
                Assert.Equal("Last", ((ActorEditor)actorList.SelectedItem!).Name);
                Assert.True(actorList.ContainerFromItem(actorList.SelectedItem!)!.IsFocused);
                actorList.RaiseEvent(new KeyEventArgs { RoutedEvent = InputElement.KeyDownEvent, Key = Key.Delete });
                Assert.Same(model.Actors[0], actorList.SelectedItem);
                Assert.True(actorList.ContainerFromItem(actorList.SelectedItem!)!.IsFocused);
                var temporary = new ActorEditor { Name = "Remove with x" };
                model.Actors.Add(temporary); window.UpdateLayout();
                var remove = actorList.GetVisualDescendants().OfType<Button>().FirstOrDefault(b => ReferenceEquals(b.DataContext, temporary) && b.Content?.ToString() == "×");
                Assert.NotNull(remove);
                remove.RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(Button.ClickEvent));
                Assert.Single(model.Actors);
                Assert.Same(model.Actors[0], actorList.SelectedItem);
                Assert.True(actorList.ContainerFromItem(actorList.SelectedItem!)!.IsFocused);
                Assert.False(File.Exists(Path.Combine(root, "Film.nfo")));
                window.UpdateLayout();
                var table = window.FindControl<Border>("ActorTable")!;
                var portrait = window.FindControl<Border>("ActorPortrait")!;
                Assert.True(portrait.TranslatePoint(default, window)!.Value.X >= table.TranslatePoint(default, window)!.Value.X + table.Bounds.Width);
                Assert.Equal(language == "de" ? new[] { "Metadaten", "Darsteller", "Details", "Artwork" } : new[] { "Metadata", "Cast", "Details", "Artwork" }, tabs.Items.OfType<TabItem>().Select(t => t.Header?.ToString()));
                var screenshots = Path.Combine(AppContext.BaseDirectory, "screenshots"); Directory.CreateDirectory(screenshots);
                foreach (var index in new[] { 0, 1, 2, 3 })
                {
                    tabs.SelectedIndex = index; window.UpdateLayout();
                    using var image = new RenderTargetBitmap(new PixelSize(1280, 900), new Vector(96, 96));
                    image.Render(window); image.Save(Path.Combine(screenshots, $"details-{language}-{index}.png"), PngBitmapEncoderOptions.Default);
                }
            }
            finally { window.Hide(); }
            await model.SaveCommand.ExecuteAsync(null);
            // Saving refreshes the selected movie asynchronously. Wait before cleaning up SQLite.
            for (var i = 0; (!model.IsDetailReady || model.IsImagesLoading) && i < 500; i++)
                await Task.Delay(10, TestContext.Current.CancellationToken);
            Assert.True(model.IsDetailReady);
            Assert.False(model.IsImagesLoading);
            var xml = XDocument.Load(Path.Combine(root, "Film.nfo"));
            Assert.Equal("Film, Der", xml.Root!.Element("sorttitle")!.Value);
            Assert.Equal("2020-02-03", xml.Root.Element("premiered")!.Value);
            Assert.Equal("New set", xml.Root.Element("set")!.Element("name")!.Value);
            Assert.Equal("Keep this overview", xml.Root.Element("set")!.Element("overview")!.Value);
            Assert.Equal(2, xml.Root.Elements("credits").Count());
            Assert.Equal(2, xml.Root.Elements("tag").Count());
            Assert.Equal(new[] { "Drama", "Science Fiction" }, xml.Root.Elements("genre").Select(element => element.Value));
            Assert.Equal("Lead", xml.Root.Element("actor")!.Element("role")!.Value);
            Assert.Equal("123", xml.Root.Element("actor")!.Element("tmdbid")!.Value);
            Assert.Equal("7.5", xml.Root.Element("rating")!.Value);
            Assert.Null(xml.Root.Element("trailer"));
            Assert.Equal("1920", xml.Root.Element("fileinfo")!.Element("streamdetails")!.Element("video")!.Element("width")!.Value);
            window.Close();
        }
        finally { Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools(); Directory.Delete(root, true); }
    }
    [Theory]
    [InlineData("<set>Legacy set</set>", "Legacy set")]
    [InlineData("<set><name>Kodi set</name><overview>Overview</overview></set>", "Kodi set")]
    public void ReadsBothLegacyAndStructuredMovieSets(string setXml, string expected)
    {
        var serializer = new System.Xml.Serialization.XmlSerializer(typeof(Movie));
        using var reader = new StringReader("<movie>" + setXml + "</movie>");
        var movie = Assert.IsType<Movie>(serializer.Deserialize(reader));
        Assert.Equal(expected, movie.MovieSet);
        var restored = NfoSerializer.Deserialize<Movie>(NfoSerializer.Serialize(movie)!);
        Assert.Equal(expected, restored!.MovieSet);
    }
}
