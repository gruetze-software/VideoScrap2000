using System.Xml.Linq;
using System.Net;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
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
