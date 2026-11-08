using Microsoft.Data.Sqlite;
using MovieScrap2000;
using Xunit;
using Avalonia.Headless.XUnit;
using VideoScrap2000.ViewModels;

namespace VideoScrap2000.Tests;

public sealed class ScanRegressionTests : IDisposable
{
    private readonly string root = Path.Combine(Path.GetTempPath(), "VideoScrap2000-scan", Guid.NewGuid().ToString("N"));
    private static CancellationToken Ct => TestContext.Current.CancellationToken;
    public ScanRegressionTests() => Directory.CreateDirectory(root);

    [Theory]
    [InlineData("tt1234567")]
    [InlineData("")]
    public async Task VersionsInDifferentFoldersRemainIndependent(string imdb)
    {
        var database = new DatabaseManager(Path.Combine(root, "cache.sqlite"));
        await database.InitializeDatabaseAsync();
        using var metadata = new MetadataService(database);
        var library = Path.Combine(root, "library");
        foreach (var edition in new[] { "Theatrical", "Extended" })
        {
            var folder = Path.Combine(library, edition);
            Directory.CreateDirectory(folder);
            await File.WriteAllBytesAsync(Path.Combine(folder, "Film.mkv"), [0], Ct);
            await File.WriteAllTextAsync(Path.Combine(folder, "movie.nfo"),
                $"<movie><title>Same movie</title><plot>{edition}</plot><uniqueid type=\"imdb\">{imdb}</uniqueid></movie>", Ct);
        }
        var scanner = new MovieManager(metadata);
        await scanner.ScanDirectoryAsync(library, Ct);
        await scanner.ScanDirectoryAsync(library, Ct);
        Assert.Equal(0, scanner.ScanErrors);
        var movies = await metadata.LoadMoviesAsync();
        Assert.Equal(2, movies.Count);
        Assert.Equal(2, movies.Select(m => m.Id).Distinct().Count());
        foreach (var movie in movies)
        {
            await movie.LoadFullDetailsAsync(metadata);
            Assert.Equal(Path.GetFileName(movie.FolderPath), movie.FullMovieNFO!.Plot);
        }
    }

    [Fact]
    public async Task LegacyUniqueImdbMigrationPreservesIdsRelationshipsAndImages()
    {
        var database = new DatabaseManager(Path.Combine(root, "legacy.sqlite"));
        await database.InitializeDatabaseAsync();
        using var metadata = new MetadataService(database);
        var nfo = new Movie { Title = "Existing", Actors = [new Actor { Name = "Actor", ThumbData = [1, 2, 3] }] };
        await metadata.SaveMovieAsync(nfo, root, Path.Combine(root, "film.mkv"), false, Ct, writeFiles: false);
        var id = await metadata.CheckMovieExistsAsync(root, Ct);
        await database.ExecuteAsync(async connection =>
        {
            using var command = connection.CreateCommand();
            command.CommandText = "SELECT sql FROM sqlite_master WHERE name='Movie';";
            var schema = (string)(await command.ExecuteScalarAsync())!;
            var legacySchema = System.Text.RegularExpressions.Regex.Replace(schema,
                @"^CREATE TABLE\s+(?:IF NOT EXISTS\s+)?Movie", "CREATE TABLE Movie_legacy");
            legacySchema = legacySchema.Replace("ImdbId TEXT,", "ImdbId TEXT UNIQUE,");
            command.CommandText = "PRAGMA foreign_keys=OFF;" + legacySchema + "; INSERT INTO Movie_legacy SELECT * FROM Movie; DROP TABLE Movie; ALTER TABLE Movie_legacy RENAME TO Movie; PRAGMA foreign_keys=ON;";
            await command.ExecuteNonQueryAsync();
            command.CommandText = "INSERT INTO ImageCache(MovieId,Type,Data) VALUES(@id,'Poster',X'010203');";
            command.Parameters.AddWithValue("@id", id);
            await command.ExecuteNonQueryAsync();
        });
        await database.InitializeDatabaseAsync();
        await database.InitializeDatabaseAsync();
        var movie = Assert.Single(await metadata.LoadMoviesAsync());
        Assert.Equal(id, movie.Id);
        await movie.LoadFullDetailsAsync(metadata);
        var actor = Assert.Single(movie.Actors!);
        Assert.Equal(new byte[] { 1, 2, 3 }, await metadata.GetActorPictureAsync(actor.Id, Ct));
        await database.ExecuteAsync(async connection =>
        {
            using var command = connection.CreateCommand();
            command.CommandText = "SELECT COUNT(*) FROM ImageCache WHERE MovieId=@id;";
            command.Parameters.AddWithValue("@id", id);
            Assert.Equal(1L, await command.ExecuteScalarAsync());
            command.CommandText = "PRAGMA foreign_key_check;";
            using var reader = await command.ExecuteReaderAsync();
            Assert.False(await reader.ReadAsync());
        });
        await database.ExecuteAsync(async connection =>
        {
            using var command = connection.CreateCommand();
            command.CommandText = "UPDATE Movie SET ImdbId='tt123'; INSERT INTO Movie(ImdbId,Title,FolderPath,DateAdded,DateModified) VALUES('tt123','Other','other','2026-01-01','2026-01-01');";
            await command.ExecuteNonQueryAsync();
        });
        Assert.Equal(2, (await metadata.LoadMoviesAsync()).Count);
    }

    [Theory]
    [InlineData(12)]
    [InlineData(680)]
    public async Task ScanDoesNotDownloadActorImagesAndReportsEveryFolderInOrder(int folderCount)
    {
        var database = new DatabaseManager(Path.Combine(root, "cache.sqlite"));
        await database.InitializeDatabaseAsync();
        using var handler = new CountingHandler();
        using var client = new HttpClient(handler);
        using var metadata = new MetadataService(database, client);
        var library = Path.Combine(root, "library");
        for (var i = 0; i < folderCount; i++)
        {
            var folder = Path.Combine(library, $"Film{i}");
            Directory.CreateDirectory(folder);
            await File.WriteAllBytesAsync(Path.Combine(folder, "film.mkv"), [0], Ct);
            await File.WriteAllTextAsync(Path.Combine(folder, "movie.nfo"),
                $"<movie><title>Film{i}</title><actor><name>Actor{i}</name><thumb>https://images.example/{i}.jpg</thumb></actor></movie>", Ct);
        }
        var scanner = new MovieManager(metadata);
        var progress = new List<ScanProgress>();
        scanner.DetailedProgress += (_, value) => progress.Add(value);
        await scanner.ScanDirectoryAsync(library, Ct);
        await scanner.ScanDirectoryAsync(library, Ct);
        Assert.Equal(0, scanner.ScanErrors);
        Assert.Equal(0, handler.Requests);
        Assert.Equal(Enumerable.Range(0, folderCount + 1).Concat(Enumerable.Range(0, folderCount + 1)), progress.Select(p => p.Completed));
        Assert.All(progress, p => Assert.Equal(folderCount, p.Total));
        Assert.Equal(folderCount, (await metadata.LoadMoviesAsync()).Count);
        var movie = (await metadata.LoadMoviesAsync())[0];
        await movie.LoadFullDetailsAsync(metadata);
        Assert.StartsWith("https://images.example/", Assert.Single(movie.Actors!).Thumb);
    }

    private sealed class CountingHandler : HttpMessageHandler
    {
        public int Requests;
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Interlocked.Increment(ref Requests);
            return Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.OK) { Content = new ByteArrayContent([1]) });
        }
    }

    [AvaloniaFact]
    public async Task PortraitLoadedOnDemandIsCachedForLaterSelections()
    {
        var database = new DatabaseManager(Path.Combine(root, "cache.sqlite"));
        await database.InitializeDatabaseAsync();
        using var metadata = new MetadataService(database);
        var portraitPath = Path.Combine(root, "portrait.png");
        using (var bitmap = new SkiaSharp.SKBitmap(2, 2))
        {
            bitmap.Erase(SkiaSharp.SKColors.Red);
            using var image = SkiaSharp.SKImage.FromBitmap(bitmap);
            using var encoded = image.Encode(SkiaSharp.SKEncodedImageFormat.Png, 100);
            await File.WriteAllBytesAsync(portraitPath, encoded.ToArray(), Ct);
        }
        var movie = new Movie { Title = "Portrait", Actors = [new Actor { Name = "Example", Thumb = "portrait.png" }] };
        await metadata.SaveMovieAsync(movie, root, Path.Combine(root, "movie.mkv"), false, Ct, writeFiles: false);
        using var model = new MainViewModel(database, new AppSettings { Language = VideoScrap2000.Localization.Locale.IsEnglish ? "en" : "de" });
        await model.InitializeAsync();
        await model.ArtworkLoadingTask;
        var actor = Assert.Single(model.Actors);
        var firstPath = await model.GetActorOpenPathAsync(actor);
        Assert.NotNull(firstPath);
        Assert.NotNull(await metadata.GetActorPictureAsync(actor.Id, Ct));
        File.Delete(portraitPath);
        model.SelectedActor = null;
        actor.ThumbData = null;
        var secondPath = await model.GetActorOpenPathAsync(actor);
        Assert.NotNull(secondPath);
        Assert.NotNull(model.ActorPhoto);
        File.Delete(firstPath);
        File.Delete(secondPath);
    }

    public void Dispose()
    {
        SqliteConnection.ClearAllPools();
        Directory.Delete(root, true);
    }
}
