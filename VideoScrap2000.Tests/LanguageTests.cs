using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.VisualTree;
using MovieScrap2000;
using VideoScrap2000.Localization;
using VideoScrap2000.ViewModels;
using VideoScrap2000.Views;
using Xunit;

namespace VideoScrap2000.Tests;
public class LanguageTests
{
    [Theory]
    [InlineData("System", "fr-CA", "fr-CA")]
    [InlineData("System", "de-AT", "de-AT")]
    [InlineData("System", "pt-PT", "pt-PT")]
    [InlineData("System", "zh-Hans-CN", "zh-CN")]
    [InlineData("System", "haw-US", "en-US")]
    [InlineData("fr", "de-DE", "fr-FR")]
    [InlineData("ja", "en-US", "ja-JP")]
    [InlineData("pt-BR", "de-DE", "pt-BR")]
    public void ContentKeepsClientLanguageAndRegionIndependentlyOfUi(string preference, string client, string expected) =>
        Assert.Equal(expected, LanguagePreferences.ResolveContent(preference, client));

    [Theory]
    [InlineData("fr")]
    [InlineData("pl")]
    [InlineData("ja")]
    [InlineData("pt-PT")]
    [InlineData("zh-TW")]
    public void ContentSelectionRoundTripsLanguageCodes(string code) =>
        Assert.Equal(code, LanguagePreferences.ContentFromIndex(LanguagePreferences.ContentToIndex(code)));

    [AvaloniaTheory]
    [InlineData("de")]
    [InlineData("en")]
    public void UiLanguageDoesNotOverrideIndependentContentLanguages(string ui)
    {
        using var model = new MainViewModel(settings: new AppSettings { Language = ui, MetadataLanguage = "fr-CA", ArtworkLanguage = "ja" });
        Assert.Equal("fr-CA", model.MetadataLanguage);
        Assert.Equal("ja-JP", model.ArtworkLanguage);
        Assert.Equal(2, LanguagePreferences.ContentToIndex("en"));
        Assert.Equal(1, LanguagePreferences.ContentToIndex("de"));
    }

    [Theory]
    [InlineData("FR", "fr-CA", 0)]
    [InlineData("fr-FR", "fr-CA", 0)]
    [InlineData("00", "fr-CA", 1)]
    [InlineData(null, "fr-CA", 1)]
    [InlineData("en", "fr-CA", 2)]
    public void ArtworkLanguageRanksPreferredThenNeutralThenOthers(string? language, string preferred, int priority) =>
        Assert.Equal(priority, LanguagePreferences.ImagePriority(language, preferred));

    [Fact]
    public async Task AllMetadataEndpointsReceiveSelectedLanguageWithoutFilteringArtwork()
    {
        using var handler = new LanguageRequestHandler();
        using var scraper = new MovieScrap2000.TMDbScraper.TMDbScraperManager(new HttpClient(handler));
        await scraper.SearchMoviesAsync("Example", cancellationToken: TestContext.Current.CancellationToken, language: "fr-CA");
        await scraper.GetMovieFullDetailsAsync(1, "fr-CA");
        await scraper.SearchSeriesAsync("Example", "fr-CA", CancellationToken.None);
        await scraper.GetSeriesDetailsAsync(1, "fr-CA", CancellationToken.None);
        await scraper.GetSeasonDetailsAsync(1, 1, "fr-CA", CancellationToken.None);
        await scraper.GetEpisodeDetailsAsync(1, 1, 1, "fr-CA", CancellationToken.None);
        Assert.Equal(6, handler.Requests.Count);
        Assert.All(handler.Requests, uri => Assert.Contains("language=fr-CA", uri.Query));
        handler.Requests.Clear();
        await scraper.GetMovieImagesAsync(1, TestContext.Current.CancellationToken);
        await scraper.GetSeriesImagesAsync(1, null, CancellationToken.None);
        Assert.All(handler.Requests, uri => Assert.Empty(uri.Query));
    }

    private sealed class LanguageRequestHandler : HttpMessageHandler
    {
        public List<Uri> Requests { get; } = [];
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
        {
            Requests.Add(request.RequestUri!);
            return Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            { Content = new StringContent("{\"id\":1,\"results\":[]}") });
        }
    }
    [AvaloniaTheory]
    [InlineData("en", "Settings", "Room for great movies.")]
    [InlineData("de", "Einstellungen", "Platz für großes Kino.")]
    public void InterfaceUsesRequestedLanguage(string language, string settings, string empty)
    {
        using var model = new MainViewModel(settings: new AppSettings { Language = language, MetadataLanguage = "en", ArtworkLanguage = "de" });
        var window = new MainWindow(model, false); window.Show();
        try
        {
            window.UpdateLayout();
            Assert.Contains(window.GetVisualDescendants().OfType<Button>(), b => b.Content?.ToString() == settings);
            Assert.Equal(empty, model.EmptyTitle);
            Assert.Equal("en-US", model.MetadataLanguage); Assert.Equal("de-DE", model.ArtworkLanguage);
            Assert.Equal(language == "en" ? "New" : "Neu", Locale.T("Neu"));
            Assert.Equal(language == "en" ? "Loaded 42 movies." : "42 Filme geladen.", Locale.T("42 Filme geladen."));
        }
        finally { window.Close(); }
    }
    [AvaloniaFact]
    public void ExistingWindowLabelsChangeWhenLanguageChanges()
    {
        using var model = new MainViewModel(settings: new AppSettings { Language = "de" });
        var window = new MainWindow(model, false); window.Show();
        try
        {
            Locale.Apply("en"); window.UpdateLayout();
            Assert.Contains(window.GetVisualDescendants().OfType<Button>(), b => b.Content?.ToString() == "Settings");
            Locale.Apply("de"); window.UpdateLayout();
            Assert.Contains(window.GetVisualDescendants().OfType<Button>(), b => b.Content?.ToString() == "Einstellungen");
        }
        finally { window.Close(); }
    }
    [Theory]
    [InlineData("en", "de", "en-US")]
    [InlineData("de", "en", "de-DE")]
    [InlineData("System", "en", "en-US")]
    [InlineData("System", "de", "de-DE")]
    public void ExplicitLanguageOverridesFollowingInterface(string preference, string ui, string expected) => Assert.Equal(expected, LanguagePreferences.Resolve(preference, ui));
}
