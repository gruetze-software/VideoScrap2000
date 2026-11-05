using System.Xml.Linq;
using System.Net;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.VisualTree;
using MovieScrap2000;
using MovieScrap2000.TMDbScraper;
using VideoScrap2000.ViewModels;
using VideoScrap2000.Views;
using Xunit;

namespace VideoScrap2000.Tests;

public sealed class SeriesLibraryTests
{
    [AvaloniaTheory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task EitherSaveButtonPersistsShowAndArtworkAlongsideSelectedEpisode(bool useShowButton)
    {
        var root = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        var folder = Path.Combine(root, "Fargo");
        Directory.CreateDirectory(folder);
        try
        {
            File.WriteAllBytes(Path.Combine(folder, "Fargo S01E01.mkv"), [0]);
            using var main = new MainViewModel(settings: new AppSettings { SeriesPath = root, Language = "en" });
            var series = new SeriesViewModel(main);
            await series.ScanAsync();
            series.ShowEditor!.Title = "Fargo";
            series.ShowEditor.Plot = "Saved show description";
            series.ShowEditor.TmdbId = "60622";
            series.ShowEditor.Actors.Add(new ActorEditor { Name = "Example Actor", Role = "Lead" });
            series.EpisodeEditor!.Title = "The pilot";
            using var resource = typeof(Movie).Assembly.GetManifestResourceStream("VideoScrap2000.Empty.png")!;
            using var buffer = new MemoryStream();
            resource.CopyTo(buffer);
            series.StageSeriesArtwork([new ImageCacheItem { Type = ImageTypes.Fanart, Data = buffer.ToArray() }], null);

            if (useShowButton) await series.SaveShowCommand.ExecuteAsync(null);
            else await series.SaveEpisodeCommand.ExecuteAsync(null);

            Assert.True(File.Exists(Path.Combine(folder, "tvshow.nfo")), main.Status);
            Assert.True(File.Exists(Path.Combine(folder, "fanart.png")));
            Assert.True(File.Exists(Path.Combine(folder, "Fargo S01E01.nfo")));
            var reopened = new SeriesViewModel(main);
            await reopened.ScanAsync();
            Assert.Equal("Fargo", reopened.ShowEditor!.Title);
            Assert.Equal("60622", reopened.ShowEditor.TmdbId);
            Assert.Equal("Saved show description", reopened.ShowEditor.Plot);
            Assert.Equal("Example Actor", Assert.Single(reopened.ShowEditor.Actors).Name);
            Assert.Equal("The pilot", reopened.EpisodeEditor!.Title);
            Assert.NotNull(reopened.FanartPreview);
        }
        finally { Directory.Delete(root, recursive: true); }
    }

    [AvaloniaFact]
    public async Task SeriesTabGroupsEpisodesAndSavesShowAndEpisodeSeparately()
    {
        var root = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        var showFolder = Path.Combine(root, "Love and Death");
        var seasonFolder = Path.Combine(showFolder, "Staffel 01");
        Directory.CreateDirectory(seasonFolder);
        var showNfo = Path.Combine(showFolder, "tvshow.nfo");
        var firstVideo = Path.Combine(seasonFolder, "Love and Death S01E01.mkv");
        var secondVideo = Path.Combine(seasonFolder, "02 - The next episode.mkv");
        var specialVideo = Path.Combine(showFolder, "Love and Death 0x01.mkv");
        try
        {
            foreach (var file in new[] { firstVideo, secondVideo, specialVideo }) File.WriteAllBytes(file, [0]);
            using (var image = typeof(Movie).Assembly.GetManifestResourceStream("VideoScrap2000.Empty.png")!)
            using (var bannerFile = File.Create(Path.Combine(showFolder, "season01-banner.png"))) image.CopyTo(bannerFile);
            File.Copy(Path.Combine(showFolder, "season01-banner.png"), Path.Combine(showFolder, "season01-poster.png"));
            File.Copy(Path.Combine(showFolder, "season01-banner.png"), Path.Combine(showFolder, "banner.png"));
            var original = "<tvshow><title>Love &amp; Death</title><status>Ended</status><uniqueid type=\"custom\">88</uniqueid><custom>Keep</custom></tvshow>";
            File.WriteAllText(showNfo, original);
            File.WriteAllText(Path.ChangeExtension(firstVideo, ".nfo"), "<episodedetails><title>The pilot</title><season>1</season><episode>1</episode></episodedetails>");

            using var main = new MainViewModel(settings: new AppSettings { SeriesPath = root, Language = "en" });
            var window = new MainWindow(main, initialize: false);
            window.Show();
            try
            {
                var tabs = window.FindControl<TabControl>("LibraryTabs")!;
                Assert.Equal(new[] { "Movies", "TV shows" }, tabs.Items.OfType<TabItem>().Select(tab => tab.Header?.ToString()));
                tabs.SelectedIndex = 1;
                window.UpdateLayout();
                for (var i = 0; (main.Series.ShowEditor is null || main.IsBusy) && i < 500; i++)
                    await Task.Delay(10, TestContext.Current.CancellationToken);
                Assert.Equal("Love & Death", main.Series.ShowEditor!.Title);
                var listChanges = new List<System.Collections.Specialized.NotifyCollectionChangedAction>();
                main.Series.Shows.CollectionChanged += (_, change) => listChanges.Add(change.Action);
                main.Series.Query = "Love";
                Assert.Empty(listChanges);
                Assert.Equal(new int?[] { null, 0, 1 }, main.Series.Seasons.Select(season => season.Number));
                main.Series.SelectedSeason = main.Series.Seasons.Single(season => season.Number == 1);
                Assert.Equal(new[] { "S01E01", "S01E02" }, main.Series.Episodes.Select(episode => episode.Editor.EpisodeCode));
                var seriesView = Assert.Single(window.GetVisualDescendants().OfType<SeriesView>());
                seriesView.FindControl<TabControl>("SeriesDetailTabs")!.SelectedIndex = 2;
                window.UpdateLayout();
                var banner = Assert.Single(seriesView.GetVisualDescendants().OfType<MoviePosterThumbnail>(),
                    image => image.Name == "SeasonBanner");
                for (var i = 0; banner.Source is null && i < 200; i++) await Task.Delay(10, TestContext.Current.CancellationToken);
                window.UpdateLayout();
                Assert.NotNull(banner.Source);
                Assert.Equal(Avalonia.Media.Stretch.Uniform, banner.Stretch);
                Assert.True(banner.Bounds.Height <= 160);
                var detailTabs = seriesView.FindControl<TabControl>("SeriesDetailTabs")!;
                var episodeButton = detailTabs.Items.OfType<TabItem>().ElementAt(2);
                var bannerPosition = banner.TranslatePoint(default, seriesView)!.Value;
                var buttonPosition = episodeButton.TranslatePoint(default, seriesView)!.Value;
                Assert.True(bannerPosition.Y + banner.Bounds.Height <= buttonPosition.Y);
                var bannerFrame = (Control)banner.Parent!;
                var previousWidth = bannerFrame.Bounds.Width;
                window.Width += 180;
                await Task.Delay(30, TestContext.Current.CancellationToken);
                window.UpdateLayout();
                Assert.True(bannerFrame.Bounds.Width > previousWidth + 100,
                    $"Banner frame width before {previousWidth}, after {bannerFrame.Bounds.Width}, window {window.ClientSize}");
                Assert.False(seriesView.FindControl<TextBlock>("EpisodeEmptyHint")!.IsVisible);
                var episodeList = seriesView.FindControl<ListBox>("EpisodeList")!;
                Assert.True(episodeList.Bounds.Height >= 120, $"Episode list height: {episodeList.Bounds.Height}");
                episodeList.SelectedIndex = 1;
                Assert.Equal("S01E02", main.Series.EpisodeEditor!.EpisodeCode);
                episodeList.SelectedIndex = 0;
                Assert.Equal("S01E01", main.Series.EpisodeEditor!.EpisodeCode);
                var detailSections = seriesView.FindControl<TabControl>("SeriesDetailTabs")!;
                detailSections.SelectedIndex = 3;
                window.UpdateLayout();
                var artworkList = seriesView.FindControl<ListBox>("SeriesArtworkList")!;
                var seasonPoster = main.Series.ArtworkTiles.Single(tile => tile.Key == "season01-poster");
                main.Series.SelectedSeason = main.Series.Seasons.First();
                artworkList.SelectedItem = seasonPoster;
                window.UpdateLayout();
                Assert.Equal(1, main.Series.SelectedSeason!.Number);
                Assert.Equal(ImageTypes.Poster, main.Series.ArtworkSearchType);
                var selectedCard = Assert.Single(artworkList.ContainerFromItem(seasonPoster)!.GetVisualDescendants()
                    .OfType<Border>(), border => border.Classes.Contains("card"));
                Assert.Equal(new Thickness(3), selectedCard.BorderThickness);
                artworkList.SelectedItem = main.Series.ArtworkTiles.Single(tile => tile.Key == "banner");
                Assert.Null(main.Series.SelectedSeason!.Number);
                Assert.Equal(ImageTypes.Banner, main.Series.ArtworkSearchType);
                detailSections.SelectedIndex = 2;
                main.Series.SelectedSeason = main.Series.Seasons.Single(season => season.Number == 0);
                Assert.Null(main.Series.SeasonBannerPreview);
                main.Series.SelectedSeason = main.Series.Seasons.Single(season => season.Number == 1);

                main.Series.ShowEditor.TmdbId = "123";
                await main.Series.SaveShowCommand.ExecuteAsync(null);
                Assert.Equal(original, File.ReadAllText(showNfo + ".bak"));
                var show = XDocument.Load(showNfo).Root!;
                Assert.Equal("Keep", (string?)show.Element("custom"));
                Assert.Contains(show.Elements("uniqueid"), id => (string?)id.Attribute("type") == "custom" && id.Value == "88");
                Assert.Contains(show.Elements("uniqueid"), id => (string?)id.Attribute("type") == "tmdb" && id.Value == "123");

                main.Series.SelectedEpisode = main.Series.Episodes.Single(episode => episode.Episode.VideoPath == secondVideo);
                main.Series.EpisodeEditor!.Title = "New episode title";
                await main.Series.SaveEpisodeCommand.ExecuteAsync(null);
                var saved = XDocument.Load(Path.ChangeExtension(secondVideo, ".nfo")).Root!;
                Assert.Equal("episodedetails", saved.Name.LocalName);
                Assert.Equal("New episode title", (string?)saved.Element("title"));
                Assert.Equal("2", (string?)saved.Element("episode"));
            }
            finally { window.Close(); }
        }
        finally { Directory.Delete(root, recursive: true); }
    }

    [Fact]
    public async Task EpisodeLookupUsesShowIdSeasonEpisodeAndLanguage()
    {
        using var handler = new EpisodeHandler();
        using var scraper = new TMDbScraperManager(new HttpClient(handler));
        var details = await scraper.GetEpisodeDetailsAsync(345, 0, 2, "de-DE", TestContext.Current.CancellationToken);
        var editor = new SeriesNfoEditor(new XElement("episodedetails", new XElement("season", 0), new XElement("episode", 2)), false);
        editor.ApplyEpisode(details);
        Assert.Equal("/3/tv/345/season/0/episode/2?language=de-DE", handler.PathAndQuery);
        Assert.Equal("Spezialfolge", editor.Title);
        Assert.Equal("S00E02", editor.EpisodeCode);
    }

    [AvaloniaFact]
    public async Task MatchingSeriesStagesCastFanartAndOnlyMissingSeasonBanners()
    {
        var root = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        var folder = Path.Combine(root, "Example");
        Directory.CreateDirectory(folder);
        try
        {
            using var resource = typeof(Movie).Assembly.GetManifestResourceStream("VideoScrap2000.Empty.png")!;
            using var memory = new MemoryStream(); resource.CopyTo(memory);
            var image = memory.ToArray();
            var existing = Path.Combine(folder, "season01-banner.png");
            File.WriteAllBytes(existing, image);
            foreach (var name in new[] { "Example S01E01.mkv", "Example S02E01.mkv" }) File.WriteAllBytes(Path.Combine(folder, name), [0]);
            using var handler = new SeriesArtworkHandler(image);
            using var main = new MainViewModel(settings: new AppSettings { SeriesPath = root, Language = "de" });
            using var tvmaze = new HttpClient(handler, false);
            var series = new SeriesViewModel(main, () => new TMDbScraperManager(new HttpClient(handler, false),
                new MovieScrap2000.FanArtTvScraper.FanArtTvScraper(new HttpClient(handler, false))), tvmaze);
            await series.ScanAsync();
            await series.ApplyShowMatchAsync(345);
            Assert.Equal(new[] { "/fanart-de.png", "/tv-banner.png", "/logo-de.png", "/clearart.png", "/landscape.png", "/character.png",
                "/season-2-poster.png", "/season-2.png", "/season-2-fanart.png" }, handler.Downloads);
            Assert.NotNull(series.FanartPreview?.PendingData);
            series.SelectedSeason = series.Seasons.Single(season => season.Number == 1);
            Assert.Equal(existing, series.SeasonBannerPreview?.Path);
            Assert.Same(series.FanartPreview, series.BackgroundPreview);
            series.SelectedSeason = series.Seasons.Single(season => season.Number == 2);
            Assert.NotNull(series.SeasonBannerPreview?.PendingData);
            Assert.Equal("season02-fanart", series.BackgroundPreview?.Path);
            Assert.Contains(series.ArtworkTiles, tile => tile.Picture.Path == "season02-poster");
            Assert.Equal("season02-poster", series.SelectedShow?.Poster?.Path);
            series.SelectedSeason = series.Seasons.First(season => season.Number is null);
            series.SelectedEpisode = series.Episodes.Single(row => row.Editor.SeasonNumber == 2);
            Assert.Equal("season02-poster", series.SeasonPosterPreview?.Path);
            Assert.Equal("banner", series.SeasonBannerPreview?.Path);
            var generalBanner = series.SelectedShow!.Artwork["banner"];
            series.SelectedShow.Artwork.Remove("banner");
            series.SelectedEpisode = null;
            series.SelectedEpisode = series.Episodes.Single(row => row.Editor.SeasonNumber == 2);
            Assert.Equal("season02-banner", series.SeasonBannerPreview?.Path);
            series.SelectedShow.Artwork["banner"] = generalBanner;
            series.SelectedEpisode = null;
            Assert.Equal("banner", series.SeasonBannerPreview?.Path);
            var actor = Assert.Single(series.ShowEditor!.Actors);
            Assert.Equal(123, actor.TmdbId);
            Assert.EndsWith("/portrait.jpg", actor.Thumb);
            actor.Role = "Edited role";
            Assert.False(File.Exists(Path.Combine(folder, "fanart.png")));

            await series.SaveShowCommand.ExecuteAsync(null);
            Assert.True(File.Exists(Path.Combine(folder, "fanart.png")));
            Assert.True(File.Exists(Path.Combine(folder, "season02-banner.png")));
            Assert.True(File.Exists(Path.Combine(folder, "season02-poster.png")));
            Assert.True(File.Exists(Path.Combine(folder, "season02-fanart.png")));
            Assert.True(File.Exists(Path.Combine(folder, "clearlogo.png")));
            Assert.True(File.Exists(Path.Combine(folder, "clearart.png")));
            Assert.True(File.Exists(Path.Combine(folder, "landscape.png")));
            Assert.True(File.Exists(Path.Combine(folder, "characterart.png")));
            Assert.False(File.Exists(existing + ".bak"));
            var savedActor = XDocument.Load(Path.Combine(folder, "tvshow.nfo")).Root!.Element("actor")!;
            Assert.Equal("Edited role", (string?)savedActor.Element("role"));
            Assert.Equal("123", (string?)savedActor.Element("tmdbid"));

            await series.FindAllEpisodesCommand.ExecuteAsync(null);
            Assert.Equal(2, handler.SeasonRequests);
            Assert.Equal(1, handler.TvmazeEpisodeRequests);
            Assert.Contains("/original/still.png", handler.Downloads);
            Assert.Contains("/maze-still.png", handler.Downloads);
            series.SelectedEpisode = series.Episodes.Single(row => row.Editor.SeasonNumber == 2);
            Assert.NotNull(series.EpisodePicture?.PendingData);
            Assert.False(File.Exists(Path.Combine(folder, "Example S02E01-thumb.png")));
            Assert.True(series.HasPendingEpisodeUpdates);
            Assert.False(File.Exists(Path.Combine(folder, "Example S02E01.nfo")));
            series.EpisodeEditor!.Title = "Edited after lookup";
            series.ShowEditor.Plot = "Updated show plot";
            await series.SaveEpisodeCommand.ExecuteAsync(null);
            Assert.False(series.HasPendingEpisodeUpdates);
            Assert.Equal("Edited after lookup", (string?)XDocument.Load(Path.Combine(folder, "Example S02E01.nfo")).Root!.Element("title"));
            Assert.Equal("Fetched episode", (string?)XDocument.Load(Path.Combine(folder, "Example S01E01.nfo")).Root!.Element("title"));
            Assert.Equal("Updated show plot", (string?)XDocument.Load(Path.Combine(folder, "tvshow.nfo")).Root!.Element("plot"));
            var still = Path.Combine(folder, "Example S02E01-thumb.png");
            Assert.Equal(image, File.ReadAllBytes(still));
            series.SelectedEpisode = null;
            Assert.Null(series.EpisodePicture);
            series.SelectedEpisode = series.Episodes.Single(row => row.Editor.SeasonNumber == 2);
            Assert.Equal(still, series.EpisodePicture?.Path);
        }
        finally { Directory.Delete(root, recursive: true); }
    }

    private sealed class SeriesArtworkHandler(byte[] image) : HttpMessageHandler
    {
        public List<string> Downloads { get; } = [];
        public int SeasonRequests { get; private set; }
        public int TvmazeEpisodeRequests { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var uri = request.RequestUri!;
            if (uri.AbsolutePath.Contains("/season/", StringComparison.Ordinal))
            {
                SeasonRequests++;
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(uri.AbsolutePath.EndsWith("/1", StringComparison.Ordinal)
                        ? """{"episodes":[{"episode_number":1,"name":"Fetched episode","still_path":"/still.png"}]}"""
                        : """{"episodes":[{"episode_number":1,"name":"Fetched episode","air_date":"2023-04-27"}]}""")
                });
            }
            if (uri.Host == "images.test")
            {
                Downloads.Add(uri.AbsolutePath);
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent(image) });
            }
            if (uri.Host == "api.tvmaze.com" && uri.AbsolutePath.EndsWith("/episodes", StringComparison.Ordinal))
                TvmazeEpisodeRequests++;
            var json = uri.AbsolutePath switch
            {
                "/3/configuration" => """{"images":{"secure_base_url":"https://images.test/","still_sizes":["w92","w185","w300","original"]}}""",
                "/lookup/shows" => """{"id":42}""",
                "/shows/42/episodes" => """
                    [{"season":2,"number":1,"airdate":"2023-04-20","image":{"original":"https://images.test/wrong.png"}},
                     {"season":2,"number":1,"airdate":"2023-04-27","image":{"original":"https://images.test/maze-still.png"}}]
                    """,
                _ when uri.Host == "webservice.fanart.tv" && uri.Query == "?source=tmdb" => """
                {"showbackground":[{"url":"https://images.test/fanart-en.png","lang":"en"},{"url":"https://images.test/fanart-de.png","lang":"de"},{"url":"https://images.test/season-2-fanart.png","season":"2","lang":"de"}],
                 "tvbanner":[{"url":"https://images.test/tv-banner.png","lang":"de"}],
                 "hdtvlogo":[{"url":"https://images.test/logo-en.png","lang":"en"}],
                 "clearlogo":[{"url":"https://images.test/logo-de.png","lang":"de"}],
                 "hdclearart":[{"url":"https://images.test/clearart.png","lang":"en"}],
                 "tvthumb":[{"url":"https://images.test/landscape.png","lang":"de"}],
                 "characterart":[{"url":"https://images.test/character.png","lang":"00"}],
                 "seasonposter":[{"url":"https://images.test/season-2-poster.png","season":"2"}],
                 "seasonbanner":[{"url":"https://images.test/season-1.png","season":"1"},{"url":"https://images.test/season-2.png","season":"2"}]}
                """,
                _ => """
                {"name":"Example","external_ids":{"imdb_id":"tt12345"},"credits":{"cast":[{"name":"Example Actor","character":"Lead","id":123,"profile_path":"/portrait.jpg"}]}}
                """
            };
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(json) });
        }
    }

    private sealed class EpisodeHandler : HttpMessageHandler
    {
        public string? PathAndQuery { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            PathAndQuery = request.RequestUri?.PathAndQuery;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{\"id\":123,\"name\":\"Spezialfolge\",\"air_date\":\"2023-04-27\"}")
            });
        }
    }
}
