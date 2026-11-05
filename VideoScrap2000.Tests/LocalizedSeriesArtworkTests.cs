using System.Net;
using Avalonia.Headless.XUnit;
using MovieScrap2000;
using MovieScrap2000.FanArtTvScraper;
using MovieScrap2000.TMDbScraper;
using VideoScrap2000.ViewModels;
using Xunit;

namespace VideoScrap2000.Tests;

public sealed class LocalizedSeriesArtworkTests
{
    [AvaloniaTheory]
    [InlineData("en-US", "TV-14")]
    [InlineData("fr-FR", "12")]
    public async Task AutomaticTvArtworkPrefersRequestedLanguageAcrossProviders(string metadataLanguage, string certification)
    {
        var root = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        var folder = Path.Combine(root, "Example");
        Directory.CreateDirectory(folder);
        File.WriteAllText(Path.Combine(folder, "tvshow.nfo"), "<tvshow><title>Example</title></tvshow>");
        try
        {
            using var source = typeof(Movie).Assembly.GetManifestResourceStream("VideoScrap2000.Empty.png")!;
            using var memory = new MemoryStream(); source.CopyTo(memory);
            using var handler = new Handler(memory.ToArray());
            using var main = new MainViewModel(settings: new AppSettings
            { Language = "en", SeriesPath = root, MetadataLanguage = metadataLanguage, ArtworkLanguage = "fr-CA" });
            var series = new SeriesViewModel(main, () => new TMDbScraperManager(new HttpClient(handler, false),
                new FanArtTvScraper(new HttpClient(handler, false))));
            await series.ScanAsync();
            await series.ApplyShowMatchAsync(1);
            Assert.Contains("language=" + metadataLanguage, handler.DetailQuery);
            Assert.Equal(certification, series.ShowEditor!.Certification);
            Assert.NotNull(series.SelectedShow!.Artwork.GetValueOrDefault("poster")?.PendingData);
            Assert.Contains("/original/fr.png", handler.Downloads);
            Assert.DoesNotContain(handler.Downloads, path => path.Contains("english", StringComparison.Ordinal));
            Assert.Equal(1, handler.ImageRequests);
        }
        finally { Directory.Delete(root, true); }
    }

    private sealed class Handler(byte[] image) : HttpMessageHandler
    {
        public List<string> Downloads { get; } = [];
        public string DetailQuery { get; private set; } = "";
        public int ImageRequests { get; private set; }
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
        {
            var uri = request.RequestUri!;
            if (uri.Host == "images.test")
            {
                Downloads.Add(uri.AbsolutePath);
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent(image) });
            }
            string json;
            if (uri.Host == "webservice.fanart.tv")
                json = """{"tvposter":[{"url":"https://images.test/english-fanart.png","lang":"en"}],"showbackground":[{"url":"https://images.test/neutral.png","lang":"00"}]}""";
            else if (uri.AbsolutePath.EndsWith("/images", StringComparison.Ordinal))
            {
                ImageRequests++;
                Assert.Empty(uri.Query);
                json = """{"posters":[{"file_path":"/english-tmdb.png","iso_639_1":"en"},{"file_path":"/fr.png","iso_639_1":"fr"}]}""";
            }
            else if (uri.AbsolutePath.EndsWith("/configuration", StringComparison.Ordinal))
                json = """{"images":{"secure_base_url":"https://images.test/","poster_sizes":["w1","w2","w3","w4","w5","w6","original"],"backdrop_sizes":["w1","w2","w3","original"]}}""";
            else
            {
                DetailQuery = uri.Query;
                json = """{"name":"Example","poster_path":"/english-details.png","content_ratings":{"results":[{"iso_3166_1":"US","rating":"TV-14"},{"iso_3166_1":"FR","rating":"12"}]}}""";
            }
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(json) });
        }
    }
}
