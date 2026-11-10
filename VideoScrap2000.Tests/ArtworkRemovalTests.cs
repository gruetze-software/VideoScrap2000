using Avalonia.Headless.XUnit;
using MovieScrap2000;
using VideoScrap2000.ViewModels;
using Xunit;

namespace VideoScrap2000.Tests;

public sealed class ArtworkRemovalTests : IDisposable
{
    private readonly string root = Path.Combine(Path.GetTempPath(), "VideoScrap2000-artwork-remove", Guid.NewGuid().ToString("N"));
    private static CancellationToken Token => TestContext.Current.CancellationToken;
    public ArtworkRemovalTests() => Directory.CreateDirectory(root);
    private static byte[] Image()
    {
        using var input = typeof(Movie).Assembly.GetManifestResourceStream("VideoScrap2000.Empty.png")!;
        using var output = new MemoryStream(); input.CopyTo(output); return output.ToArray();
    }

    [Fact]
    public void RemovalBacksUpOnlyMatchingImageNamesAndKeepsOtherFiles()
    {
        foreach (var name in new[] { "Test-poster.jpg", "poster.png", "fanart.jpg", "poster.nfo", "unrelated.png" })
            File.WriteAllText(Path.Combine(root, name), name);
        ArtworkFiles.RemoveNamed(root, ArtworkFiles.MovieStems("Test.mkv", ImageTypes.Poster), Token);
        Assert.False(File.Exists(Path.Combine(root, "Test-poster.jpg")));
        Assert.Equal("Test-poster.jpg", File.ReadAllText(Path.Combine(root, "Test-poster.jpg.bak")));
        Assert.True(File.Exists(Path.Combine(root, "poster.png.bak")));
        Assert.True(File.Exists(Path.Combine(root, "fanart.jpg")));
        Assert.True(File.Exists(Path.Combine(root, "poster.nfo")));
        Assert.True(File.Exists(Path.Combine(root, "unrelated.png")));
        Assert.Throws<ArgumentException>(() => ArtworkFiles.RemoveNamed(root, ["../outside"], Token));
    }

    [AvaloniaFact]
    public async Task MovieRemovalIsStagedThenSavedWithoutReturningFromCache()
    {
        var folder = Path.Combine(root, "Movie"); Directory.CreateDirectory(folder);
        var video = Path.Combine(folder, "Test.mp4");
        File.Copy(Path.Combine(AppContext.BaseDirectory, "Fixtures", "technical.mp4"), video);
        var poster = Path.Combine(folder, "poster.png"); File.WriteAllBytes(poster, Image());
        var fanart = Path.Combine(folder, "fanart.png"); File.WriteAllBytes(fanart, Image());
        var database = new DatabaseManager(Path.Combine(root, "cache.sqlite"));
        await database.InitializeDatabaseAsync();
        using var metadata = new MetadataService(database);
        await metadata.SaveMovieAsync(new Movie { Title = "Test" }, folder, video, false, Token);
        using var main = new MainViewModel(database, new AppSettings { MovieFolders = [root] });
        await main.InitializeAsync(); await main.ArtworkLoadingTask;
        await metadata.UpsertImageCacheItemsAsync(main.SelectedMovie!.Id, [new() { ImageType = "poster", ImageData = Image() }], Token);
        Assert.NotNull(main.Poster);
        var image = main.Artwork.Single(item => item.Type == ImageTypes.Poster);
        main.RemoveArtwork(image);
        Assert.True(File.Exists(poster));
        Assert.False(File.Exists(poster + ".bak"));
        Assert.Null(main.Poster);
        Assert.DoesNotContain(main.Artwork, item => item.Type == ImageTypes.Poster);
        Assert.Contains(main.Artwork, item => item.Type == ImageTypes.Fanart);
        await main.SaveCommand.ExecuteAsync(null); await main.ArtworkLoadingTask;
        Assert.False(File.Exists(poster));
        Assert.True(File.Exists(poster + ".bak"));
        Assert.True(File.Exists(fanart));
        Assert.DoesNotContain(main.Artwork, item => item.Type == ImageTypes.Poster);
        var cache = await metadata.GetImageCacheItemsWithOutDataAsync(main.SelectedMovie!.Id, folder, video, Token);
        Assert.DoesNotContain(cache, item => string.Equals(item.Type, ImageTypes.Poster, StringComparison.OrdinalIgnoreCase));
    }

    [AvaloniaFact]
    public async Task SeriesRemovalKeepsOtherSeasonsAndCanBeReplacedBeforeSaving()
    {
        var folder = Path.Combine(root, "Show"); Directory.CreateDirectory(folder);
        File.WriteAllText(Path.Combine(folder, "tvshow.nfo"), "<tvshow><title>Show</title></tvshow>");
        File.WriteAllBytes(Path.Combine(folder, "Show.S01E01.mkv"), [0]);
        foreach (var name in new[] { "poster.png", "season01-poster.png", "season02-poster.png" }) File.WriteAllBytes(Path.Combine(folder, name), Image());
        using var main = new MainViewModel(settings: new AppSettings { SeriesPath = root });
        await main.Series.ScanAsync();
        var show = main.Series.SelectedShow!;
        main.Series.RemoveArtwork(main.Series.ArtworkTiles.Single(tile => tile.Key == "season01-poster"));
        Assert.True(File.Exists(Path.Combine(folder, "season01-poster.png")));
        Assert.DoesNotContain(main.Series.ArtworkTiles, tile => tile.Key == "season01-poster");
        Assert.Contains(main.Series.ArtworkTiles, tile => tile.Key == "season02-poster");
        main.Series.StageSeriesArtwork([new() { Type = ImageTypes.Poster, Data = Image() }], 1);
        Assert.DoesNotContain("season01-poster", show.RemovedArtwork);
        main.Series.RemoveArtwork(main.Series.ArtworkTiles.Single(tile => tile.Key == "season01-poster"));
        await main.Series.SaveShowCommand.ExecuteAsync(null);
        Assert.False(File.Exists(Path.Combine(folder, "season01-poster.png")));
        Assert.True(File.Exists(Path.Combine(folder, "season01-poster.png.bak")));
        Assert.True(File.Exists(Path.Combine(folder, "season02-poster.png")));
        Assert.True(File.Exists(Path.Combine(folder, "poster.png")));
    }

    public void Dispose()
    {
        Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
        if (Directory.Exists(root)) Directory.Delete(root, true);
    }
}
