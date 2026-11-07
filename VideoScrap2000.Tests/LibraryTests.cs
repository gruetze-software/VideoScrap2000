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
    public async Task NewNfoRecordsFirstSaveDateInsteadOfOldVideoDate()
    {
        var video = Path.Combine(root, "Film.mkv");
        await File.WriteAllBytesAsync(video, [0], Ct);
        File.SetLastWriteTime(video, new DateTime(2025, 8, 13));
        var before = DateTime.Now.AddSeconds(-1);
        var path = await MovieManager.SaveNfoFileAsync(new Movie { Title = "Film" }, root, video, Ct);
        var added = DateTime.ParseExact(XDocument.Load(path).Root!.Element("dateadded")!.Value,
            "yyyy-MM-dd HH:mm:ss", System.Globalization.CultureInfo.InvariantCulture);
        Assert.InRange(added, before, DateTime.Now);
        await MovieManager.SaveNfoFileAsync(new Movie { Title = "Edited" }, root, video, Ct);
        Assert.Equal(added.ToString("yyyy-MM-dd HH:mm:ss"), XDocument.Load(path).Root!.Element("dateadded")!.Value);
    }

    [Theory]
    [InlineData("")]
    [InlineData("<dateadded>invalid</dateadded>")]
    [InlineData("<dateadded>2024-02-03 12:34:56</dateadded>")]
    public async Task ExistingNfoUsesCreationDateWhenAddedDateIsMissing(string dateTag)
    {
        var path = Path.Combine(root, "Film.nfo");
        await File.WriteAllTextAsync(path, $"<movie><title>Film</title>{dateTag}</movie>", Ct);
        if (OperatingSystem.IsWindows() || OperatingSystem.IsMacOS()) File.SetCreationTime(path, new DateTime(2026, 11, 4, 8, 45, 33));
        var created = File.GetCreationTime(path).ToString("yyyy-MM-dd HH:mm:ss", System.Globalization.CultureInfo.InvariantCulture);
        await MovieManager.SaveNfoFileAsync(new Movie { Title = "Edited" }, root, Path.Combine(root, "Film.mkv"), Ct);
        Assert.Equal(dateTag.Contains("2024", StringComparison.Ordinal) ? "2024-02-03 12:34:56" : created,
            XDocument.Load(path).Root!.Element("dateadded")!.Value);
    }

    [Fact]
    public async Task RepeatedSavesPreservePhysicalCreationDateAndKodiDateIndependently()
    {
        var video = Path.Combine(root, "Film.mkv");
        var path = Path.ChangeExtension(video, ".nfo");
        await File.WriteAllTextAsync(path, "<movie><title>Film</title><dateadded>2019-03-04 12:34:56</dateadded></movie>", Ct);
        if (OperatingSystem.IsWindows() || OperatingSystem.IsMacOS())
            File.SetCreationTimeUtc(path, new DateTime(2021, 5, 6, 7, 8, 9, DateTimeKind.Utc));
        var created = File.GetCreationTimeUtc(path);
        for (var save = 0; save < 2; save++)
        {
            await MovieManager.SaveNfoFileAsync(new Movie { Title = "Edited" }, root, video, Ct);
            if (OperatingSystem.IsWindows() || OperatingSystem.IsMacOS()) Assert.Equal(created, File.GetCreationTimeUtc(path));
            Assert.Equal(created, MovieFileFacts.Read(root, video).NfoCreatedAtUtc);
            Assert.Equal("2019-03-04 12:34:56", XDocument.Load(path).Root!.Element("dateadded")!.Value);
        }
    }

    [Fact]
    public async Task StoredCreationTimeSurvivesPlatformChangesWithoutChangingKodiDate()
    {
        var path = Path.Combine(root, "Film.nfo");
        var video = Path.Combine(root, "Film.mkv");
        var created = new DateTime(2020, 2, 3, 4, 5, 6, DateTimeKind.Utc);
        var document = new XDocument(new XElement("movie",
            new XAttribute(MovieFileFacts.CreationAttribute, created.ToString("O")),
            new XElement("title", "Film"), new XElement("dateadded", "2019-01-02 03:04:05")));
        await File.WriteAllTextAsync(path, document.ToString(), Ct);
        Assert.Equal(created, MovieFileFacts.Read(root, video).NfoCreatedAtUtc);
        await MovieManager.SaveNfoFileAsync(new Movie { Title = "Edited" }, root, video, Ct);
        Assert.Equal(created, MovieFileFacts.Read(root, video).NfoCreatedAtUtc);
        Assert.Equal("2019-01-02 03:04:05", XDocument.Load(path).Root!.Element("dateadded")!.Value);
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
