using Avalonia.Headless.XUnit;
using Avalonia.Controls;
using MovieScrap2000;
using MovieScrap2000.TMDbScraper;
using MovieScrap2000.FanArtTvScraper;
using VideoScrap2000.Views;
using VideoScrap2000.ViewModels;
using System.Net;
using System.Text;
using Xunit;

namespace VideoScrap2000.Tests;
public class ArtworkWorkflowTests
{
    [AvaloniaFact]
    public async Task AutomaticAllDownloadsOnlyMissingTypes()
    {
        using var handler = new ArtworkHandler();
        var window = new ArtworkWindow(550, "Alle", CreateManager(handler), automaticTypes: new HashSet<string> { ImageTypes.Banner });
        window.Show();
        try
        {
            await window.LoadingTask;
            Assert.True(window.IsAccepted);
            Assert.Equal(ImageTypes.Banner, Assert.Single(window.Results).Type);
            Assert.Equal("/banner.png", Assert.Single(handler.Downloads));
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public async Task OpeningSelectedTypeAutomaticallyImportsItsOnlyImage()
    {
        using var handler = new ArtworkHandler();
        var manager = CreateManager(handler);
        var window = new ArtworkWindow(550, ImageTypes.ClearArt, manager);
        window.Show();
        try
        {
            await window.LoadingTask;
            Assert.True(window.IsAccepted);
            var result = Assert.Single(window.Results);
            Assert.Equal(ImageTypes.ClearArt, result.Type);
            Assert.Equal(handler.ImageBytes, result.Data);
            Assert.Equal(new[] { "/first.png" }, handler.Downloads);
        }
        finally { window.Close(); }
    }
    [AvaloniaFact]
    public async Task AllImportsOnlyFirstChoiceForEachAvailableType()
    {
        using var handler = new ArtworkHandler();
        var window = new ArtworkWindow(550, "Alle", CreateManager(handler));
        window.Show();
        try
        {
            await window.LoadingTask;
            Assert.True(window.IsAccepted);
            Assert.Equal(new[] { ImageTypes.ClearArt, ImageTypes.Banner }, window.Results.Select(p => p.Type));
            Assert.Equal(new[] { "/first.png", "/banner.png" }, handler.Downloads);
            Assert.Equal(1, handler.MovieRequests);
            Assert.Equal(1, handler.FanartRequests);
        }
        finally { window.Close(); }
    }
    [AvaloniaFact]
    public void SelectingArtworkChangesSearchType()
    {
        using var model = new MainViewModel(settings: new AppSettings());
        // No image allocation is needed to verify selection and search synchronization.
        var item = new ArtworkViewModel(ImageTypes.DiskArt, "disc.png", null!);
        model.SelectedArtwork = item;
        Assert.Equal(ImageTypes.DiskArt, model.ArtworkType);
        Assert.Contains("Alle", model.ArtworkTypes);
    }
    [AvaloniaTheory]
    [InlineData("de-DE", "/first.png")]
    [InlineData("en-US", "/english.png")]
    public async Task ArtworkHonorsRequestedLanguage(string language, string expected)
    {
        using var handler = new ArtworkHandler();
        handler.OtherLanguages = true;
        var window = new ArtworkWindow(550, "Alle", CreateManager(handler), language);
        window.Show();
        try
        {
            await window.LoadingTask;
            Assert.True(window.IsAccepted);
            Assert.Equal(expected, handler.Downloads[0]);
            Assert.Equal("", handler.MovieQuery);
        }
        finally { window.Close(); }
    }
    [AvaloniaTheory]
    [InlineData("de-DE", "/first.png")]
    [InlineData("en-US", "/english.png")]
    public async Task OtherLanguagesRemainVisibleWithPreferredLanguageFirst(string language, string first)
    {
        using var stream = typeof(Movie).Assembly.GetManifestResourceStream("VideoScrap2000.Empty.png")!;
        using var bytes = new MemoryStream(); stream.CopyTo(bytes);
        using var handler = new ArtworkHandler { OtherLanguages = true, ImageBytes = bytes.ToArray() };
        var window = new ArtworkWindow(550, ImageTypes.ClearArt, CreateManager(handler), language);
        window.Show();
        try
        {
            await window.LoadingTask;
            Assert.False(window.IsAccepted);
            var candidates = window.FindControl<ListBox>("Candidates")!.Items.OfType<ArtworkCandidate>().ToList();
            Assert.Equal(2, candidates.Count);
            Assert.EndsWith(first, candidates[0].Source.Url);
            Assert.Equal(2, handler.Downloads.Count);
        }
        finally { window.Close(); }
    }
    private static byte[] ReadTestImage()
    {
        using var stream = typeof(Movie).Assembly.GetManifestResourceStream("VideoScrap2000.Empty.png")!;
        using var bytes = new MemoryStream(); stream.CopyTo(bytes); return bytes.ToArray();
    }
    [AvaloniaTheory]
    [InlineData(false, "ClearArt")]
    [InlineData(true, "ClearArt")]
    [InlineData(true, "Alle")]
    public async Task InvalidImageResponsesAreNotDecodedOrImported(bool multiple, string type)
    {
        using var handler = new ArtworkHandler { OtherLanguages = multiple, ImageBytes = Encoding.UTF8.GetBytes("<html>Not an image</html>") };
        var window = new ArtworkWindow(550, type, CreateManager(handler));
        window.Show();
        try
        {
            await window.LoadingTask;
            Assert.False(window.IsAccepted);
            Assert.Empty(window.Results);
            Assert.Empty(window.FindControl<ListBox>("Candidates")!.Items);
            Assert.True(window.FindControl<Button>("LoadButton")!.IsEnabled);
        }
        finally { window.Close(); }
    }
    [AvaloniaTheory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task CanStopOrAcceptWhileAnotherPreviewIsStillDownloading(bool acceptDirectly)
    {
        using var handler = new ArtworkHandler { OtherLanguages = true, BlockEnglish = true };
        var window = new ArtworkWindow(550, ImageTypes.ClearArt, CreateManager(handler));
        window.Show();
        try
        {
            await handler.EnglishStarted.Task.WaitAsync(TimeSpan.FromSeconds(3));
            var list = window.FindControl<ListBox>("Candidates")!;
            for (var i = 0; list.Items.Count == 0 && i < 200; i++) await Task.Delay(10, TestContext.Current.CancellationToken);
            Assert.Single(list.Items);
            Assert.False(window.LoadingTask.IsCompleted);
            list.SelectedIndex = 0;
            var accept = window.FindControl<Button>("AcceptButton")!;
            var stop = window.FindControl<Button>("StopLoadingButton")!;
            Assert.True(stop.IsVisible);
            Assert.True(accept.IsEnabled);
            if (!acceptDirectly)
            {
                stop.RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(Button.ClickEvent));
                await window.LoadingTask.WaitAsync(TimeSpan.FromSeconds(3));
                Assert.True(window.IsVisible);
                Assert.Single(list.Items);
                Assert.False(stop.IsVisible);
                Assert.True(accept.IsEnabled);
            }
            accept.RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(Button.ClickEvent));
            for (var i = 0; !window.IsAccepted && i < 200; i++) await Task.Delay(10, TestContext.Current.CancellationToken);
            Assert.True(window.IsAccepted);
            Assert.Equal(ImageTypes.ClearArt, Assert.Single(window.Results).Type);
            Assert.False(handler.SlowImage.Task.IsCompleted);
            await handler.EnglishCanceled.Task.WaitAsync(TimeSpan.FromSeconds(3));
        }
        finally { handler.SlowImage.TrySetResult(); window.Close(); }
    }
    private static TMDbScraperManager CreateManager(HttpMessageHandler handler) => new(
        new HttpClient(handler, false), new FanArtTvScraper(new HttpClient(handler, false)));
    private sealed class ArtworkHandler : HttpMessageHandler
    {
        public byte[] ImageBytes { get; set; } = ReadTestImage();
        public bool OtherLanguages { get; set; }
        public bool BlockEnglish { get; set; }
        public TaskCompletionSource EnglishStarted { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource EnglishCanceled { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource SlowImage { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public List<string> Downloads { get; } = [];
        public int MovieRequests { get; private set; }
        public int FanartRequests { get; private set; }
        public string MovieQuery { get; private set; } = "";
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var path = request.RequestUri!.AbsolutePath;
            string json;
            if (path.Contains("/movie/")) { MovieRequests++; MovieQuery = request.RequestUri.Query; json = "{\"id\":550}"; }
            else if (path.Contains("/movies/"))
            {
                FanartRequests++;
                json = "{\"hdmovieclearart\":[{\"url\":\"https://art.test/first.png\",\"lang\":\"de\"},{\"url\":\"https://art.test/english.png\",\"lang\":\"en\"}],\"moviebanner\":[{\"url\":\"https://art.test/banner.png\",\"lang\":\"de\"},{\"url\":\"https://art.test/second.png\",\"lang\":\"en\"}]}";
            }
            else
            {
                lock (Downloads) Downloads.Add(path);
                if (BlockEnglish && path == "/english.png") { EnglishStarted.TrySetResult(); try { await SlowImage.Task.WaitAsync(cancellationToken); } catch (OperationCanceledException) { EnglishCanceled.TrySetResult(); throw; } }
                return new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent(ImageBytes) };
            }
            if (!OtherLanguages && path.Contains("/movies/"))
                json = "{\"hdmovieclearart\":[{\"url\":\"https://art.test/first.png\",\"lang\":\"de\"}],\"moviebanner\":[{\"url\":\"https://art.test/banner.png\",\"lang\":\"de\"}]}";
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(json, Encoding.UTF8, "application/json") };
        }
    }
}
