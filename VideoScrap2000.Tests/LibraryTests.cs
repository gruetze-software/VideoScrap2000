using System.Xml.Linq;
using MovieScrap2000;
using Xunit;

namespace VideoScrap2000.Tests;

public sealed class LibraryTests : IDisposable
{
    private readonly string root = Path.Combine(Path.GetTempPath(), "VideoScrap2000-tests", Guid.NewGuid().ToString("N"));
    private static CancellationToken Ct => TestContext.Current.CancellationToken;
    public LibraryTests() => Directory.CreateDirectory(root);
    private async Task<(MetadataService Metadata, MovieManager Scanner)> CreateAsync()
    {
        var database = new DatabaseManager(Path.Combine(root, "cache.sqlite"));
        await database.InitializeDatabaseAsync();
        var metadata = new MetadataService(database);
        return (metadata, new(metadata));
    }

    [Fact]
    public async Task ScanImportsNfoWithoutChangingAnyLibraryFiles()
    {
        var folder = Path.Combine(root, "Filme", "Testfilm"); Directory.CreateDirectory(folder);
        await File.WriteAllBytesAsync(Path.Combine(folder, "Film.mkv"), [0], Ct);
        var nfoPath = Path.Combine(folder, "movie.nfo");
        var original = "<?xml version=\"1.0\" encoding=\"utf-8\"?><movie><title>Die Rückkehr</title><year>2025</year><plot>Grüße aus Köln</plot><genre>Drama</genre><actor><name>Test Person</name></actor></movie>";
        await File.WriteAllTextAsync(nfoPath, original, Ct);
        var service = await CreateAsync();
        await service.Scanner.ScanDirectoryAsync(Path.Combine(root, "Filme"), Ct);
        var movie = Assert.Single(await service.Metadata.LoadMoviesAsync());
        Assert.Equal("Die Rückkehr", movie.Title);
        Assert.Equal(original, await File.ReadAllTextAsync(nfoPath, Ct));
        Assert.Equal(2, Directory.GetFiles(folder).Length);
        await movie.LoadFullDetailsAsync(service.Metadata);
        Assert.Equal("Grüße aus Köln", movie.FullMovieNFO!.Plot);
        Assert.Single(movie.Actors!);
    }

    [Fact]
    public async Task ScanWithoutNfoCreatesOnlyDatabaseEntry()
    {
        var folder = Path.Combine(root, "Filme", "Neuer Film"); Directory.CreateDirectory(folder);
        await File.WriteAllBytesAsync(Path.Combine(folder, "Film.mkv"), [0], Ct);
        var service = await CreateAsync();
        await service.Scanner.ScanDirectoryAsync(Path.Combine(root, "Filme"), Ct);
        Assert.Single(await service.Metadata.LoadMoviesAsync());
        Assert.Single(Directory.GetFiles(folder));
        var movie = Assert.Single(await service.Metadata.LoadMoviesAsync());
        await movie.LoadFullDetailsAsync(service.Metadata);
        Assert.NotNull(movie.FullMovieNFO);
    }

    [Theory]
    [InlineData("")]
    [InlineData("<?xml version=\"1.0\" encoding=\"utf-16\"?>\n")]
    public async Task SaveUsesExistingNfoAndPreservesBackupWithValidUtf8(string declaration)
    {
        var nfoPath = Path.Combine(root, "movie.nfo");
        var original = declaration + "<movie><title>Original</title><customfield>Grüße bleiben</customfield></movie>";
        await File.WriteAllTextAsync(nfoPath, original, Ct);
        await MovieManager.SaveNfoFileAsync(new Movie { Title = "Grüße für dich" }, root, Path.Combine(root, "Film.mkv"), Ct);
        Assert.Equal(original, await File.ReadAllTextAsync(nfoPath + ".bak", Ct));
        Assert.False(File.Exists(Path.Combine(root, "Film.nfo")));
        var document = XDocument.Load(nfoPath);
        Assert.Equal("Grüße für dich", document.Root!.Element("title")!.Value);
        Assert.Equal("Grüße bleiben", document.Root.Element("customfield")?.Value);
    }

    [Fact]
    public async Task ArtworkUsesActualFileFormatAndBacksUpPreviousFormat()
    {
        var oldPath = Path.Combine(root, "Film-poster.jpg");
        await File.WriteAllTextAsync(oldPath, "old image", Ct);
        byte[] png = [137, 80, 78, 71, 13, 10, 26, 10, 0];
        await ArtworkFiles.SaveAsync(root, "Film", new() { Type = ImageTypes.Poster, Data = png }, TestContext.Current.CancellationToken);
        Assert.Equal("old image", await File.ReadAllTextAsync(oldPath + ".bak", Ct));
        Assert.False(File.Exists(oldPath));
        Assert.Equal(png, await File.ReadAllBytesAsync(Path.Combine(root, "Film-poster.png"), Ct));
    }

    [Fact]
    public async Task MetadataEditPreservesActorDetailsAndExistingActorFiles()
    {
        var folder = Path.Combine(root, "Filme", "Testfilm"); Directory.CreateDirectory(folder);
        var actorsFolder = Path.Combine(folder, ".actors"); Directory.CreateDirectory(actorsFolder);
        await File.WriteAllTextAsync(Path.Combine(actorsFolder, "portrait.jpg"), "existing portrait", Ct);
        var video = Path.Combine(folder, "Film.mkv");
        await File.WriteAllBytesAsync(video, [0], Ct);
        await File.WriteAllTextAsync(Path.Combine(folder, "movie.nfo"), "<movie><title>Original</title><actor><name>Actor</name><tmdbid>17</tmdbid><thumb>portrait.jpg</thumb></actor></movie>", Ct);
        var service = await CreateAsync();
        await service.Scanner.ScanDirectoryAsync(Path.Combine(root, "Filme"), Ct);
        var movie = Assert.Single(await service.Metadata.LoadMoviesAsync());
        await movie.LoadFullDetailsAsync(service.Metadata);
        movie.FullMovieNFO!.Title = "Überarbeitet";
        await service.Metadata.SaveMovieAsync(movie.FullMovieNFO, folder, video, false, Ct);
        var xml = XDocument.Load(Path.Combine(folder, "movie.nfo"));
        Assert.Equal("17", xml.Root!.Element("actor")!.Element("tmdbid")!.Value);
        Assert.Equal("portrait.jpg", xml.Root.Element("actor")!.Element("thumb")!.Value);
        Assert.Equal("existing portrait", await File.ReadAllTextAsync(Path.Combine(actorsFolder, "portrait.jpg"), Ct));
    }

    [Fact]
    public async Task CancelledScanDoesNotReportSuccess()
    {
        var service = await CreateAsync();
        using var cancellation = new CancellationTokenSource(); cancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => service.Scanner.ScanDirectoryAsync(root, cancellation.Token));
    }

    public void Dispose()
    {
        Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
        Directory.Delete(root, true);
    }
}
