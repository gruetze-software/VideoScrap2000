using System.Diagnostics;
using Avalonia.Headless.XUnit;
using SkiaSharp;
using Avalonia.Media.Imaging;
using Avalonia.Controls;
using VideoScrap2000.ViewModels;
using VideoScrap2000.Views;
using Xunit;

namespace VideoScrap2000.Tests;

public sealed class ArtworkCacheTests
{
    [AvaloniaFact]
    public async Task RecreatedListThumbnailUsesTheSameCachedBitmap()
    {
        var folder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        var window = new Window();
        try
        {
            var path = Path.Combine(folder, "poster.png");
            await File.WriteAllBytesAsync(path, CreateImage(SKColors.Green), TestContext.Current.CancellationToken);
            var first = new MoviePosterThumbnail { DataContext = new SeriesPicture(path) };
            window.Content = first;
            window.Show();
            for (var i = 0; first.Source is null && i < 300; i++)
                await Task.Delay(10, TestContext.Current.CancellationToken);
            var image = first.Source;
            Assert.NotNull(image);
            window.Content = null;
            Assert.Null(first.Source);
            var recreated = new MoviePosterThumbnail { DataContext = new SeriesPicture(path) };
            window.Content = recreated;
            for (var i = 0; recreated.Source is null && i < 300; i++)
                await Task.Delay(10, TestContext.Current.CancellationToken);
            Assert.Same(image, recreated.Source);
        }
        finally
        {
            window.Close();
            ArtworkPreviewCache.Shared.InvalidateFolder(folder);
            Directory.Delete(folder, recursive: true);
        }
    }

    [AvaloniaTheory]
    [InlineData(SKEncodedImageFormat.Png)]
    [InlineData(SKEncodedImageFormat.Jpeg)]
    [InlineData(SKEncodedImageFormat.Webp)]
    public void LargeImagesDecodeAtPreviewSize(SKEncodedImageFormat format)
    {
        using var original = new SKBitmap(1920, 1080);
        original.Erase(SKColors.Blue);
        using var image = SKImage.FromBitmap(original);
        using var encoded = image.Encode(format, 90);
        using var preview = ArtworkDecoder.DecodePreview(encoded.ToArray(), 300);
        Assert.NotNull(preview);
        Assert.Equal(300, preview.PixelSize.Width);
        Assert.InRange(preview.PixelSize.Height, 168, 170);
    }

    [AvaloniaFact]
    public async Task ConcurrentRequestsAndRepeatedScrollingReuseDecodedImage()
    {
        var cache = new ArtworkPreviewCache();
        var data = CreateImage(SKColors.Red);
        var leases = await Task.WhenAll(Enumerable.Range(0, 12)
            .Select(_ => cache.AcquireAsync("poster", 88, data, TestContext.Current.CancellationToken)));
        var image = leases[0]!.Image;
        foreach (var lease in leases) { Assert.Same(image, lease!.Image); lease.Dispose(); }
        using var lightweight = await cache.AcquireAsync("poster", 88, data,
            TestContext.Current.CancellationToken, retainOriginal: false);
        Assert.Null(lightweight!.OriginalData);
        var watch = Stopwatch.StartNew();
        for (var index = 0; index < 1000; index++)
        {
            using var lease = await cache.AcquireAsync("poster", 88, data, TestContext.Current.CancellationToken);
            Assert.Same(image, lease!.Image);
        }
        Console.WriteLine($"1000 cached preview requests: {watch.Elapsed.TotalMilliseconds:F1} ms");
    }

    [AvaloniaFact]
    public async Task EvictingAnImageKeepsItsDisplayedLeaseAlive()
    {
        var cache = new ArtworkPreviewCache(capacity: 1);
        var data = CreateImage(SKColors.Blue);
        using var first = await cache.AcquireAsync("poster", 88, data, TestContext.Current.CancellationToken);
        using var second = await cache.AcquireAsync("poster", 88, data, TestContext.Current.CancellationToken);
        Assert.NotSame(first!.Image, second!.Image);
        using var encoded = new MemoryStream();
        first.Image.Save(encoded, PngBitmapEncoderOptions.Default);
        Assert.True(encoded.Length > 0);
    }

    [AvaloniaFact]
    public async Task ReplacedFilesInvalidatePreviewsEvenWhenFileTimesStayTheSame()
    {
        var folder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        try
        {
            var cache = new ArtworkPreviewCache();
            var path = Path.Combine(folder, "poster.png");
            await File.WriteAllBytesAsync(path, CreateImage(SKColors.Red), TestContext.Current.CancellationToken);
            var timestamp = File.GetLastWriteTimeUtc(path);
            using var first = await cache.AcquireAsync(path, 88, token: TestContext.Current.CancellationToken);
            await File.WriteAllBytesAsync(path, CreateImage(SKColors.Green), TestContext.Current.CancellationToken);
            File.SetLastWriteTimeUtc(path, timestamp);
            cache.InvalidateFolder(folder);
            using var replacement = await cache.AcquireAsync(path, 88, token: TestContext.Current.CancellationToken);
            Assert.NotSame(first!.Image, replacement!.Image);
            using var encoded = new MemoryStream();
            first.Image.Save(encoded, PngBitmapEncoderOptions.Default);
            Assert.True(encoded.Length > 0);
        }
        finally { Directory.Delete(folder, recursive: true); }
    }

    [AvaloniaFact]
    public async Task NewPendingDataAtTheSamePathUsesANewPreview()
    {
        var cache = new ArtworkPreviewCache();
        var original = CreateImage(SKColors.Red);
        using var first = await cache.AcquireAsync("poster", 88, original, TestContext.Current.CancellationToken);
        using var sameContent = await cache.AcquireAsync("poster", 88, original.ToArray(), TestContext.Current.CancellationToken);
        using var changed = await cache.AcquireAsync("poster", 88, CreateImage(SKColors.Blue), TestContext.Current.CancellationToken);
        Assert.Same(first!.Image, sameContent!.Image);
        Assert.NotSame(first.Image, changed!.Image);
    }

    [AvaloniaFact]
    public async Task CancelledRequestDoesNotBlockLaterRequests()
    {
        var cache = new ArtworkPreviewCache();
        var data = CreateImage(SKColors.Red);
        using var cancelled = new CancellationTokenSource();
        cancelled.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => cache.AcquireAsync("poster", 88, data, cancelled.Token));
        using var loaded = await cache.AcquireAsync("poster", 88, data, TestContext.Current.CancellationToken);
        Assert.NotNull(loaded);
    }

    private static byte[] CreateImage(SKColor color)
    {
        using var bitmap = new SKBitmap(80, 120);
        bitmap.Erase(color);
        using var image = SKImage.FromBitmap(bitmap);
        using var encoded = image.Encode(SKEncodedImageFormat.Png, 100);
        return encoded.ToArray();
    }
}
