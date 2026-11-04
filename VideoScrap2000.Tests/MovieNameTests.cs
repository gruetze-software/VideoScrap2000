using System.Net;
using System.Text;
using MovieScrap2000;
using MovieScrap2000.TMDbScraper;
using Xunit;

namespace VideoScrap2000.Tests;
public class MovieNameTests
{
    [Theory]
    [InlineData("Blade Runner 2049 (2017) (Final Cut)", "Blade Runner 2049", 2017)]
    [InlineData("The End of Oak Street (Edition (Deutsch)) (1080p)", "The End of Oak Street", null)]
    public void SearchRemovesParentheticalSuffixesAndUsesYearSeparately(string input, string title, int? year)
    {
        var result = MovieNameParser.ParseSearchTitle(input);
        Assert.Equal(title, result.Title);
        Assert.Equal(year, result.Year);
    }

    [Theory]
    [InlineData("horse.girl.2020.german.webrip.x264-sov", "Horse Girl", 2020)]
    [InlineData("horse.girl.2020.german.webrip.x264-sov.mkv", "Horse Girl", 2020)]
    [InlineData("The.Matrix.1999.1080p.BluRay.x264.mkv", "The Matrix", 1999)]
    [InlineData("1917.2019.German.1080p.mkv", "1917", 2019)]
    [InlineData("2001.A.Space.Odyssey.1968.BluRay.mkv", "2001 A Space Odyssey", 1968)]
    [InlineData("Blade Runner 2049 (2017).mkv", "Blade Runner 2049", 2017)]
    public void ParsesReleaseNamesWithoutLosingNumericTitles(string file, string title, int year)
    {
        var result = MovieNameParser.Parse(file);
        Assert.Equal(title, result.Title); Assert.Equal(year, result.Year);
    }
    [Theory]
    [InlineData("horse.girl.german.webrip.x264-sov.mkv", "Horse Girl")]
    [InlineData("1917.mkv", "1917")]
    [InlineData("German Lessons.mkv", "German Lessons")]
    public void LeavesYearUnknownWhenItCannotBeInferred(string file, string title)
    {
        var result = MovieNameParser.Parse(file);
        Assert.Equal(title, result.Title); Assert.Null(result.Year);
    }
    [Fact]
    public async Task SearchSendsTitleAndYearAsSeparateApiParameters()
    {
        using var handler = new SearchHandler();
        using var scraper = new TMDbScraperManager(new HttpClient(handler));
        await scraper.SearchMoviesAsync("Horse Girl", TestContext.Current.CancellationToken, 2020);
        Assert.Contains("query=Horse%20Girl", handler.Request!.Query);
        Assert.Contains("&year=2020", handler.Request.Query);
    }
    private sealed class SearchHandler : HttpMessageHandler
    {
        public Uri? Request { get; private set; }
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
        {
            Request = request.RequestUri;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("{\"results\":[]}", Encoding.UTF8, "application/json") });
        }
    }
    [Fact]
    public async Task SearchWithoutYearKeepsMoviesWithUnknownReleaseDates()
    {
        using var handler = new UnknownDatesHandler();
        using var scraper = new TMDbScraperManager(new HttpClient(handler));
        var results = await scraper.SearchMoviesAsync("Example", TestContext.Current.CancellationToken);
        Assert.Equal(4, results.Count);
        Assert.Equal(2020, results[0].ReleaseDate!.Value.Year);
        Assert.All(results.Skip(1), movie => Assert.Null(movie.ReleaseDate));
        Assert.DoesNotContain("year=", handler.Request!.Query);
    }
    private sealed class UnknownDatesHandler : HttpMessageHandler
    {
        public Uri? Request { get; private set; }
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
        {
            Request = request.RequestUri;
            const string json = "{\"results\":[{\"id\":1,\"release_date\":\"2020-04-03\"},{\"id\":2,\"release_date\":\"\"},{\"id\":3,\"release_date\":null},{\"id\":4}]}";
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(json, Encoding.UTF8, "application/json") });
        }
    }
}
