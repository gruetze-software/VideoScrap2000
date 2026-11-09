using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Xml.Linq;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.VisualTree;
using MovieScrap2000;
using MovieScrap2000.FanArtTvScraper;
using MovieScrap2000.TMDbScraper;
using VideoScrap2000.ViewModels;
using VideoScrap2000.Views;
using Xunit;

namespace VideoScrap2000.Tests;

public sealed class ScraperProviderTests
{
    [AvaloniaTheory]
    [InlineData("de")]
    [InlineData("en")]
    public void ScraperDialogMasksCredentialsAndCancelDoesNotChangeSettings(string language)
    {
        var previous = VideoScrap2000.Localization.Locale.IsEnglish ? "en" : "de";
        VideoScrap2000.Localization.Locale.Apply(language);
        var settings = new ScraperSettings { TmdbReadToken = "secret", FanartPersonalKey = "personal", TvdbApiKey = "project", TvdbPin = "pin" };
        var dialog = new ScraperSettingsWindow(settings);
        dialog.Show();
        try
        {
            var inputs = dialog.GetVisualDescendants().OfType<TextBox>().Where(input => input.Classes.Contains("apiCredential")).ToArray();
            Assert.Equal(3, inputs.Length);
            Assert.All(inputs, input => Assert.Equal('●', input.PasswordChar));
            inputs[0].Text = "changed";
            var cancel = dialog.GetVisualDescendants().OfType<Button>().Single(button => button.IsCancel);
            cancel.RaiseEvent(new(Button.ClickEvent));
            Assert.Equal("secret", settings.TmdbReadToken);
        }
        finally { dialog.Close(); VideoScrap2000.Localization.Locale.Apply(previous); }
    }

    [Fact]
    public async Task TvdbAuthenticatesTranslatesAndKeepsIdsSeparateThroughNfoRoundtrip()
    {
        var handler = new TvdbHandler();
        using var provider = new TvdbScraper(new() { EnableTvdb = true, TvdbApiKey = "project", TvdbPin = "personal" }, new(handler));
        var token = TestContext.Current.CancellationToken;
        var results = await provider.SearchAsync("Example & more", "de-DE", token);
        Assert.Single(results);
        Assert.Equal("Beispiel", results[0].Name);
        Assert.Equal(42, results[0].Id);
        Assert.Contains("query=Example%20%26%20more", handler.Urls[1]);
        var details = await provider.GetShowAsync(42, "de-DE", token);
        var editor = new SeriesNfoEditor(new XElement("tvshow"), false);
        editor.ApplyShow(details, 42, "de-DE");
        Assert.Equal("Deutscher Titel", editor.Title);
        Assert.Equal("Deutsche Handlung", editor.Plot);
        Assert.Equal("42", editor.TvdbId);
        Assert.Equal("123", editor.TmdbId);
        Assert.Equal("tt1234", editor.ImdbId);
        Assert.Equal("FSK 16", editor.Certification);
        Assert.Equal("TheTVDB", new SeriesNfoEditor(editor.ToXml(false), true).MetadataProvider);
        var actor = Assert.Single(editor.Actors);
        Assert.Null(actor.TmdbId);
        Assert.Equal("https://artworks.thetvdb.com/person.jpg", actor.Thumb);
        var pictures = details.GetProperty("tvdb_artworks").Deserialize<List<TvdbArtwork>>()!;
        Assert.Contains(pictures, image => image.Type == ImageTypes.Poster && image.Season == 1);
        Assert.Contains(pictures, image => image.Type == ImageTypes.Fanart && image.Season == 2);
        Assert.DoesNotContain(pictures, image => image.Type == ImageTypes.Poster && image.Season == 2);
        var season = await provider.GetSeasonAsync(42, 1, "de-DE", token);
        var episodes = season.GetProperty("episodes").EnumerateArray().ToArray();
        Assert.Equal(2, episodes.Length);
        var episode = new SeriesNfoEditor(new XElement("episodedetails"), false);
        episode.ApplyEpisode(episodes[0]);
        Assert.Equal("Deutsche Folge", episode.Title);
        Assert.Equal("501", episode.TvdbId);
        Assert.Equal("", episode.TmdbId);
        Assert.Equal("501", episode.ToXml(true).Elements("uniqueid").Single(id => (string?)id.Attribute("type") == "tvdb").Value);
        Assert.Equal(1, handler.Logins);
        Assert.Contains(handler.Urls, url => url.Contains("page=1"));
        Assert.DoesNotContain(handler.Urls, url => url.Contains("personal") || url.Contains("project"));
    }

    [Fact]
    public void ProjectKeyIsNeverSerializedToUserSettings()
    {
        var settings = new AppSettings { Scrapers = new() { TvdbApiKey = "development-only-project-key", TvdbPin = "personal-pin" } };
        var json = JsonSerializer.Serialize(settings);
        Assert.DoesNotContain("development-only-project-key", json);
        Assert.DoesNotContain("TvdbApiKey", json);
        Assert.Contains("personal-pin", json);
        Assert.Equal("development-only-project-key", ProjectApiKeys.Tvdb(settings.Scrapers.TvdbApiKey));
    }

    [Fact]
    public async Task DisabledSourcesMakeNoRequestsAndPersonalCredentialsUseHeaders()
    {
        var handler = new HeaderHandler();
        var options = new ScraperSettings { TmdbReadToken = "my-token", FanartPersonalKey = "my-personal-key", EnableTmdbArtwork = false };
        using var manager = new TMDbScraperManager(new(handler), new(new HttpClient(handler), options), options);
        await manager.SearchSeriesAsync("Test", "en-US", TestContext.Current.CancellationToken);
        Assert.Equal("Bearer my-token", handler.Authorization);
        await manager.GetFanartTvMovieAsync(42, TestContext.Current.CancellationToken);
        Assert.Equal("my-personal-key", handler.Personal);
        var count = handler.Calls;
        await manager.GetMovieImagesAsync(42, TestContext.Current.CancellationToken);
        await manager.GetSeriesImagesAsync(42, null, TestContext.Current.CancellationToken);
        Assert.Equal(count, handler.Calls);
        var disabledHandler = new HeaderHandler();
        var disabled = new FanArtTvScraper(new(disabledHandler), new() { EnableFanart = false });
        Assert.Null(await disabled.GetFanArtMovieAsync(42, TestContext.Current.CancellationToken));
        await disabled.GetSeriesArtworkAsync(42, 24, TestContext.Current.CancellationToken);
        Assert.Equal(0, disabledHandler.Calls);
        disabled.Client.Dispose();
    }

    [Fact]
    public async Task TvdbMissingAccessAndAuthenticationFailureAreActionable()
    {
        var handler = new TvdbHandler();
        using var disabled = new TvdbScraper(new(), new(handler));
        await Assert.ThrowsAsync<InvalidOperationException>(() => disabled.SearchAsync("Test", "en", TestContext.Current.CancellationToken));
        Assert.Equal(0, handler.Logins);
        using var denied = new TvdbScraper(new() { EnableTvdb = true, TvdbApiKey = "project" }, new(new DeniedHandler()));
        var error = await Assert.ThrowsAsync<HttpRequestException>(() => denied.SearchAsync("Test", "en", TestContext.Current.CancellationToken));
        Assert.Equal(HttpStatusCode.Unauthorized, error.StatusCode);
        Assert.Contains("Abo-PIN", error.Message);
    }

    [AvaloniaFact]
    public void SettingsAreOptionalCopiedAndSeriesSourceCanBeChangedInSearch()
    {
        var settings = JsonSerializer.Deserialize<AppSettings>("{\"LibraryPath\":\"\"}")!;
        Assert.True(settings.Scrapers.EnableFanart);
        Assert.False(settings.Scrapers.EnableTvdb);
        var draft = settings.Scrapers.Copy(); draft.TvdbApiKey = " changed ";
        Assert.Equal("", settings.Scrapers.TvdbApiKey);
        draft.EnableTvdb = true; draft.Normalize(); Assert.Equal("changed", draft.TvdbApiKey);
        var queries = new List<string>();
        ScrapeWindow? dialog = null;
        dialog = new ScrapeWindow("Example", (_, _) => { queries.Add(dialog!.SelectedProvider); return Task.FromResult<IReadOnlyList<SearchTv>>([]); }, draft);
        dialog.Show();
        try
        {
            dialog.FindControl<ComboBox>("SourcePicker")!.SelectedItem = "TheTVDB";
            Assert.Equal(new[] { "TMDB", "TheTVDB" }, queries);
        }
        finally { dialog.Close(); }
    }

    private sealed class DeniedHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.Unauthorized));
    }
    private sealed class HeaderHandler : HttpMessageHandler
    {
        public int Calls; public string? Authorization; public string? Personal;
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
        {
            Calls++;
            if (request.Headers.Authorization is { } auth) Authorization = auth.ToString();
            if (request.Headers.TryGetValues("client-key", out var values)) Personal = values.Single();
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("{\"results\":[]}", System.Text.Encoding.UTF8, "application/json") });
        }
    }
    private sealed class TvdbHandler : HttpMessageHandler
    {
        public int Logins; public List<string> Urls = [];
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
        {
            var url = request.RequestUri!.PathAndQuery; Urls.Add(url);
            string json;
            if (url.EndsWith("login"))
            {
                Logins++;
                var body = await request.Content!.ReadFromJsonAsync<JsonElement>(cancellationToken: token);
                Assert.Equal("project", body.GetProperty("apikey").GetString());
                Assert.Equal("personal", body.GetProperty("pin").GetString());
                json = "{\"data\":{\"token\":\"temporary-token\"}}";
            }
            else
            {
                Assert.Equal("Bearer temporary-token", request.Headers.Authorization?.ToString());
                json = url switch
                {
                    var value when value.Contains("/search?") => """{"data":[{"tvdb_id":"42","name":"Example","year":"2020","translations":{"deu":"Beispiel"}}]}""",
                    "/v4/series/42/extended" => """{"data":{"id":42,"name":"Example","overview":"English","firstAired":"2020-01-01","remoteIds":[{"sourceName":"IMDB","id":"tt1234"},{"sourceName":"TheMovieDB.com","id":"123"}],"characters":[{"peopleType":"Actor","personName":"Person","name":"Role","personImgURL":"https://artworks.thetvdb.com/person.jpg"}],"artworks":[{"type":7,"seasonId":81,"image":"https://artworks.thetvdb.com/season.jpg","language":"deu"}],"seasons":[{"id":81,"number":1,"type":{"type":"official"}}]}}""",
                    "/v4/series/42/translations/deu" => """{"data":{"name":"Deutscher Titel","overview":"Deutsche Handlung"}}""",
                    "/v4/artwork/types" => """{"data":[{"id":7,"name":"Poster"},{"id":8,"name":"Background"}]}""",
                    var value when value.Contains("/episodes/official?") => value.Contains("page=1") ? """{"data":{"episodes":[{"id":502,"seasonNumber":1,"number":2,"name":"Second"}]},"links":{"next":null}}""" : """{"data":{"episodes":[{"id":501,"seasonNumber":1,"number":1,"name":"First"}]},"links":{"next":"https://api4.thetvdb.com/v4/series/42/episodes/official?page=1"}}""",
                    "/v4/episodes/501/translations/deu" => """{"data":{"name":"Deutsche Folge","overview":"Inhalt"}}""",
                    "/v4/episodes/502/translations/deu" => """{"data":{"name":"Zweite Folge"}}""",
                    _ => throw new InvalidOperationException(url)
                };
            }
            if (url == "/v4/series/42/extended")
            {
                var node = System.Text.Json.Nodes.JsonNode.Parse(json)!;
                node["data"]!["contentRatings"] = JsonSerializer.SerializeToNode(new[] { new { country = "deu", name = "FSK 16" } });
                node["data"]!["seasons"]!.AsArray().Add(JsonSerializer.SerializeToNode(new { id = 82, number = 2, type = new { type = "official" }, image = "https://artworks.thetvdb.com/background.jpg", imageType = 8 }));
                json = node.ToJsonString();
            }
            return new(HttpStatusCode.OK) { Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json") };
        }
    }
}
