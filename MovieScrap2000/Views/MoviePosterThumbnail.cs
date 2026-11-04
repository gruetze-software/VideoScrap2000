using Avalonia;
using Avalonia.Controls;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using Avalonia.VisualTree;
using MovieScrap2000;
using VideoScrap2000.ViewModels;

namespace VideoScrap2000.Views;

public sealed class MoviePosterThumbnail : Image
{
    private static readonly SemaphoreSlim LoadSlots = new(4);
    private static readonly HttpClient ImageClient = new() { Timeout = TimeSpan.FromSeconds(15) };
    private CancellationTokenSource? loading;
    private Bitmap? thumbnail;
    private string? loadedPath;
    private byte[]? downloaded;
    private Task loadTask = Task.CompletedTask;
    public int DecodeWidth { get; set; } = 88;
    public double AspectRatio { get; set; }

    protected override Size MeasureOverride(Size availableSize)
    {
        if (AspectRatio > 0 && double.IsFinite(availableSize.Width))
            return new Size(availableSize.Width, Math.Min(availableSize.Height, availableSize.Width / AspectRatio));
        return base.MeasureOverride(availableSize);
    }

    public MoviePosterThumbnail()
    {
        AttachedToVisualTree += (_, _) => loadTask = LoadPosterAsync();
        DetachedFromVisualTree += (_, _) => ClearPoster();
        DataContextChanged += (_, _) =>
        {
            if (this.IsAttachedToVisualTree()) loadTask = LoadPosterAsync();
        };
    }

    private async Task LoadPosterAsync()
    {
        ClearPoster();
        var picture = DataContext as SeriesPicture;
        var (folder, video) = DataContext switch
        {
            MovieViewModel movie => (movie.FolderPath, movie.VideoFilePath),
            SeriesShowRow series => (series.Show.FolderPath, "tvshow"),
            _ => (null, null)
        };
        if (picture is null && (folder is null || video is null)) return;

        using var request = new CancellationTokenSource();
        loading = request;
        var token = request.Token;
        var width = DecodeWidth;
        try
        {
            await LoadSlots.WaitAsync(token);
            (Bitmap? Bitmap, string? Path, byte[]? Downloaded) result;
            try
            {
                result = await Task.Run(async () =>
                {
                    var path = picture?.Path ?? MainViewModel.FindArtwork(folder!, video!, ImageTypes.Poster).FirstOrDefault().Path;
                    if (path is null) return default;
                    var bytes = picture?.PendingData;
                    var pending = bytes is not null;
                    if (bytes is null && Uri.TryCreate(path, UriKind.Absolute, out var uri) && uri.Scheme is "http" or "https")
                    {
                        bytes = await ImageClient.GetByteArrayAsync(uri, token);
                        pending = true;
                    }
                    if (bytes is null)
                    {
                        if (!File.Exists(path) && picture?.FallbackPath is { } fallback) path = fallback;
                        if (!File.Exists(path)) return default;
                        bytes = await File.ReadAllBytesAsync(path, token);
                    }
                    token.ThrowIfCancellationRequested();
                    return (ArtworkDecoder.DecodePreview(bytes, width), path, pending ? bytes : null);
                }, token);
            }
            finally { LoadSlots.Release(); }

            if (token.IsCancellationRequested) result.Bitmap?.Dispose();
            else
            {
                Source = thumbnail = result.Bitmap;
                loadedPath = result.Path;
                downloaded = result.Downloaded;
            }
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested) { }
        catch (Exception ex) { System.Diagnostics.Trace.WriteLine(ex); }
        finally { if (ReferenceEquals(loading, request)) loading = null; }
    }

    private void ClearPoster()
    {
        loading?.Cancel();
        Source = null;
        thumbnail?.Dispose();
        thumbnail = null;
        loadedPath = null;
        downloaded = null;
    }

    public async Task OpenImageAsync(TopLevel owner)
    {
        var requested = loadTask;
        await requested;
        if (!ReferenceEquals(requested, loadTask)) return;
        if (thumbnail is null || loadedPath is null) return;
        var path = await new ArtworkViewModel("", loadedPath, thumbnail, downloaded).GetOpenPathAsync();
        if (await owner.StorageProvider.TryGetFileFromPathAsync(path) is { } file) await owner.Launcher.LaunchFileAsync(file);
    }
}
