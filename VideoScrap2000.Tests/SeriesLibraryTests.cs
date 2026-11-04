using System.Xml.Linq;
using System.Net;
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
                Assert.Equal(new int?[] { null, 0, 1 }, main.Series.Seasons.Select(season => season.Number));
                main.Series.SelectedSeason = main.Series.Seasons.Single(season => season.Number == 1);
                Assert.Equal(new[] { "S01E01", "S01E02" }, main.Series.Episodes.Select(episode => episode.Editor.EpisodeCode));
                var seriesView = Assert.Single(window.GetVisualDescendants().OfType<SeriesView>());
                seriesView.FindControl<TabControl>("SeriesDetailTabs")!.SelectedIndex = 2;
                window.UpdateLayout();
                var banner = seriesView.FindControl<MoviePosterThumbnail>("SeasonBanner")!;
                for (var i = 0; banner.Source is null && i < 200; i++) await Task.Delay(10, TestContext.Current.CancellationToken);
                window.UpdateLayout();
                Assert.NotNull(banner.Source);
                Assert.True(banner.Bounds.Width > 400, $"Banner bounds: {banner.Bounds}");
                Assert.True(((Control)((Control)banner.Parent!).Parent!).Bounds.X >= 200);
                Assert.False(seriesView.FindControl<TextBlock>("EpisodeEmptyHint")!.IsVisible);
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
            var series = new SeriesViewModel(main, () => new TMDbScraperManager(new HttpClient(handler, false),
                new MovieScrap2000.FanArtTvScraper.FanArtTvScraper(new HttpClient(handler, false))));
            await series.ScanAsync();
            await series.ApplyShowMatchAsync(345);
            Assert.Equal(new[] { "/fanart-de.png", "/season-2-poster.png", "/season-2.png", "/season-2-fanart.png" }, handler.Downloads);
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
            Assert.Equal("season02-banner", series.SeasonBannerPreview?.Path);
            series.SelectedEpisode = null;
            Assert.Equal(existing, series.SeasonBannerPreview?.Path);
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
            Assert.False(File.Exists(existing + ".bak"));
            var savedActor = XDocument.Load(Path.Combine(folder, "tvshow.nfo")).Root!.Element("actor")!;
            Assert.Equal("Edited role", (string?)savedActor.Element("role"));
            Assert.Equal("123", (string?)savedActor.Element("tmdbid"));

            await series.FindAllEpisodesCommand.ExecuteAsync(null);
            Assert.Equal(2, handler.SeasonRequests);
            Assert.True(series.HasPendingEpisodeUpdates);
            Assert.False(File.Exists(Path.Combine(folder, "Example S02E01.nfo")));
            await series.SaveEpisodeUpdatesCommand.ExecuteAsync(null);
            Assert.False(series.HasPendingEpisodeUpdates);
            Assert.Equal("Fetched episode", (string?)XDocument.Load(Path.Combine(folder, "Example S02E01.nfo")).Root!.Element("title"));
        }
        finally { Directory.Delete(root, recursive: true); }
    }

    private sealed class SeriesArtworkHandler(byte[] image) : HttpMessageHandler
    {
        public List<string> Downloads { get; } = [];
        public int SeasonRequests { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var uri = request.RequestUri!;
            if (uri.AbsolutePath.Contains("/season/", StringComparison.Ordinal))
            {
                SeasonRequests++;
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("{\"episodes\":[{\"episode_number\":1,\"name\":\"Fetched episode\"}]}")
                });
            }
            if (uri.Host == "images.test")
            {
                Downloads.Add(uri.AbsolutePath);
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent(image) });
            }
            var json = uri.Host == "webservice.fanart.tv" && uri.Query == "?source=tmdb" ? """
                {"showbackground":[{"url":"https://images.test/fanart-en.png","lang":"en"},{"url":"https://images.test/fanart-de.png","lang":"de"},{"url":"https://images.test/season-2-fanart.png","season":"2","lang":"de"}],
                 "seasonposter":[{"url":"https://images.test/season-2-poster.png","season":"2"}],
                 "seasonbanner":[{"url":"https://images.test/season-1.png","season":"1"},{"url":"https://images.test/season-2.png","season":"2"}]}
                """ : """
                {"name":"Example","credits":{"cast":[{"name":"Example Actor","character":"Lead","id":123,"profile_path":"/portrait.jpg"}]}}
                """;
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
