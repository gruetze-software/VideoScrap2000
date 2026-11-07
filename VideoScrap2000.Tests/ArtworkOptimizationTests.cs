using MovieScrap2000;
using SkiaSharp;
using VideoScrap2000;
using Xunit;

namespace VideoScrap2000.Tests;

public sealed class ArtworkOptimizationTests : IDisposable
{
    private readonly string folder = Path.Combine(Path.GetTempPath(), "VideoScrap-artwork-tests-" + Guid.NewGuid().ToString("N"));
    public ArtworkOptimizationTests() => Directory.CreateDirectory(folder);
    public void Dispose() => Directory.Delete(folder, recursive: true);
    private static byte[] Picture(int width, int height, bool transparent = false)
    {
        using var bitmap = new SKBitmap(width, height, SKColorType.Bgra8888, transparent ? SKAlphaType.Premul : SKAlphaType.Opaque);
        bitmap.Erase(transparent ? new SKColor(200, 20, 40, 100) : new SKColor(200, 20, 40));
        using var image = SKImage.FromBitmap(bitmap);
        using var data = image.Encode(transparent ? SKEncodedImageFormat.Png : SKEncodedImageFormat.Jpeg, 95);
        return data.ToArray();
    }

    [Theory]
    [InlineData(ArtworkProfile.Compact, "poster", 1200, 1800, 600, 900)]
    [InlineData(ArtworkProfile.Balanced, "poster", 1200, 1800, 1000, 1500)]
    [InlineData(ArtworkProfile.Compact, "fanart", 2560, 1440, 1280, 720)]
    [InlineData(ArtworkProfile.Balanced, "fanart", 2560, 1440, 1920, 1080)]
    [InlineData(ArtworkProfile.Compact, "banner", 2000, 400, 1280, 256)]
    [InlineData(ArtworkProfile.Compact, "thumb", 320, 180, 320, 180)]
    public void ProfilesKeepAspectAndNeverUpscale(ArtworkProfile profile, string kind, int width, int height, int expectedWidth, int expectedHeight)
    {
        var output = ArtworkOptimizer.Optimize(Picture(width, height), kind, profile);
        using var codec = SKCodec.Create(new MemoryStream(output.Bytes));
        Assert.Equal(expectedWidth, codec.Info.Width);
        Assert.Equal(expectedHeight, codec.Info.Height);
        Assert.Equal(SKEncodedImageFormat.Jpeg, codec.EncodedFormat);
        Assert.True(HasJpegMarker(output.Bytes, 0xc0));
        Assert.False(HasJpegMarker(output.Bytes, 0xc2));
    }

    [Fact]
    public void OriginalAndTransparentArtworkRemainByteIdentical()
    {
        var jpeg = Picture(120, 180);
        Assert.Same(jpeg, ArtworkOptimizer.Optimize(jpeg, "poster", ArtworkProfile.Original).Bytes);
        var png = Picture(120, 180, transparent: true);
        Assert.Same(png, ArtworkOptimizer.Optimize(png, "poster", ArtworkProfile.Compact).Bytes);
        Assert.Same(png, ArtworkOptimizer.Optimize(png, "clearart", ArtworkProfile.Compact).Bytes);
    }

    [Fact]
    public void AppliesExifRotationBeforeRemovingMetadata()
    {
        var jpeg = Picture(120, 60);
        byte[] orientation = Convert.FromHexString("FFE1002245786966000049492A0008000000010012010300010000000600000000000000");
        var withExif = jpeg[..2].Concat(orientation).Concat(jpeg[2..]).ToArray();
        var output = ArtworkOptimizer.Optimize(withExif, "poster", ArtworkProfile.Compact);
        Assert.Equal(60, output.Width);
        Assert.Equal(120, output.Height);
        using var codec = SKCodec.Create(new MemoryStream(output.Bytes));
        Assert.Equal(SKEncodedOrigin.TopLeft, codec.EncodedOrigin);
    }

    [Fact]
    public async Task SavePreservesEveryDistinctOriginalAndExistingArtwork()
    {
        var previous = Picture(20, 30);
        var incoming = Picture(120, 180);
        await File.WriteAllBytesAsync(Path.Combine(folder, "poster.jpg"), previous, TestContext.Current.CancellationToken);
        await ArtworkOptimizer.SaveAsync(folder, "poster", incoming, "poster", ArtworkProfile.Compact, TestContext.Current.CancellationToken);
        var backups = Directory.GetFiles(Path.Combine(folder, ArtworkOptimizer.OriginalFolder));
        Assert.Equal(2, backups.Length);
        var contents = await Task.WhenAll(backups.Select(path => File.ReadAllBytesAsync(path, TestContext.Current.CancellationToken)));
        Assert.Contains(contents, bytes => bytes.SequenceEqual(previous));
        Assert.Contains(contents, bytes => bytes.SequenceEqual(incoming));
        Assert.Empty(Directory.GetFiles(folder, "*.tmp", SearchOption.AllDirectories));
    }

    [Fact]
    public async Task BatchSkipsBackupsAndRejectsChangedFiles()
    {
        await File.WriteAllBytesAsync(Path.Combine(folder, "Film-poster.jpg"), Picture(120, 180), TestContext.Current.CancellationToken);
        var backups = Path.Combine(folder, ArtworkOptimizer.OriginalFolder); Directory.CreateDirectory(backups);
        await File.WriteAllBytesAsync(Path.Combine(backups, "Film-poster.jpg"), Picture(120, 180), TestContext.Current.CancellationToken);
        await File.WriteAllBytesAsync(Path.Combine(folder, "Film-logo.png"), Picture(120, 180, true), TestContext.Current.CancellationToken);
        var plan = Assert.Single(await ArtworkOptimizer.AnalyzeAsync(folder, ArtworkProfile.Compact, _ => { }, TestContext.Current.CancellationToken));
        Assert.Equal(120, plan.Width);
        Assert.Equal(180, plan.Height);
        Assert.Single(Directory.GetFiles(backups));
        await File.WriteAllBytesAsync(plan.Path, Picture(100, 150), TestContext.Current.CancellationToken);
        await Assert.ThrowsAsync<IOException>(() => ArtworkOptimizer.ApplyAsync(plan, ArtworkProfile.Compact, TestContext.Current.CancellationToken));
        using var canceled = new CancellationTokenSource(); canceled.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => ArtworkOptimizer.AnalyzeAsync(folder, ArtworkProfile.Compact, _ => { }, canceled.Token));
    }

    [Fact]
    public async Task DefaultProfileKeepsDiskArtworkNameAndBytes()
    {
        var bytes = Picture(20, 30, true);
        await ArtworkOptimizer.SaveMovieImageAsync(folder, "Film", new ImageCacheItem { Type = ImageTypes.DiskArt, Data = bytes },
            ArtworkProfile.Original, TestContext.Current.CancellationToken);
        Assert.Equal(bytes, await File.ReadAllBytesAsync(Path.Combine(folder, "Film-discart.png"), TestContext.Current.CancellationToken));
        Assert.False(Directory.Exists(Path.Combine(folder, ArtworkOptimizer.OriginalFolder)));
    }

    [Fact]
    public void TruncatedJpegIsRejectedBeforeSaving()
    {
        var bytes = Picture(1200, 1800);
        Assert.Throws<InvalidDataException>(() => ArtworkOptimizer.Optimize(bytes[..(bytes.Length / 2)], "poster", ArtworkProfile.Compact));
    }

    [Avalonia.Headless.XUnit.AvaloniaFact]
    public async Task MetadataOnlySaveDoesNotRewriteExistingArtworkWithCompactEnabled()
    {
        var video = Path.Combine(folder, "Film.mkv");
        await File.WriteAllBytesAsync(video, [0], TestContext.Current.CancellationToken);
        var database = new DatabaseManager(Path.Combine(folder, "cache.sqlite"));
        await database.InitializeDatabaseAsync();
        using (var service = new MetadataService(database))
            await service.SaveMovieAsync(new Movie { Title = "Film", FanArtPics = [new() { ImageType = ImageTypes.Poster, ImageData = Picture(1200, 1800) }] },
                folder, video, false, TestContext.Current.CancellationToken);
        var poster = Path.Combine(folder, "Film-poster.jpg");
        var originalBytes = await File.ReadAllBytesAsync(poster, TestContext.Current.CancellationToken);
        File.SetLastWriteTimeUtc(poster, new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc));
        using (var model = new VideoScrap2000.ViewModels.MainViewModel(database, new AppSettings { ArtworkOptimizationProfile = 1 }))
        {
            await model.InitializeAsync();
            await model.ArtworkLoadingTask;
            Assert.True(model.CanEdit);
            model.Title = "Updated title";
            await model.SaveCommand.ExecuteAsync(null);
            await model.ArtworkLoadingTask;
            Assert.Equal(originalBytes, await File.ReadAllBytesAsync(poster, TestContext.Current.CancellationToken));
            Assert.Equal(2020, File.GetLastWriteTimeUtc(poster).Year);
            Assert.False(Directory.Exists(Path.Combine(folder, ArtworkOptimizer.OriginalFolder)));
        }
        Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
    }

    [Avalonia.Headless.XUnit.AvaloniaFact]
    public void SettingsAndBatchDialogExposeTheOptionalProfiles()
    {
        var settings = new AppSettings();
        Assert.Equal(0, settings.ArtworkOptimizationProfile);
        var json = System.Text.Json.JsonSerializer.Serialize(new AppSettings { ArtworkOptimizationProfile = 2 });
        Assert.Equal(2, System.Text.Json.JsonSerializer.Deserialize<AppSettings>(json)!.ArtworkOptimizationProfile);
        var draft = new VideoScrap2000.ViewModels.SettingsViewModel();
        Assert.Equal(3, draft.OptimizationProfiles.Count);
        var dialog = new VideoScrap2000.Views.ArtworkOptimizationWindow(folder, 1);
        dialog.Show();
        Assert.True(dialog.IsVisible);
        dialog.Close();
    }

    [Fact]
    public async Task InvalidImageDoesNotReplaceExistingFile()
    {
        var original = Picture(20, 30);
        var path = Path.Combine(folder, "poster.jpg"); await File.WriteAllBytesAsync(path, original, TestContext.Current.CancellationToken);
        await Assert.ThrowsAsync<InvalidDataException>(() => ArtworkOptimizer.SaveAsync(folder, "poster", [255, 216, 255, 0], "poster", ArtworkProfile.Compact, TestContext.Current.CancellationToken));
        Assert.Equal(original, await File.ReadAllBytesAsync(path, TestContext.Current.CancellationToken));
    }

    private static bool HasJpegMarker(byte[] bytes, byte marker)
    {
        for (var position = 2; position + 3 < bytes.Length;)
        {
            if (bytes[position] != 255) return false;
            var current = bytes[position + 1];
            if (current == marker) return true;
            if (current == 0xda || current == 0xd9) return false;
            position += 2 + (bytes[position + 2] << 8 | bytes[position + 3]);
        }
        return false;
    }
}
