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
    private CancellationTokenSource? loading;
    private Bitmap? thumbnail;
    private ArtworkPreviewCache.Lease? preview;
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
            var path = picture?.Path ?? (await ArtworkPreviewCache.Shared.FindArtworkAsync(folder!, video!, token))
                .FirstOrDefault(item => item.Type == ImageTypes.Poster).Path;
            if (path is null) return;
            ArtworkPreviewCache.Lease? result;
            try { result = await ArtworkPreviewCache.Shared.AcquireAsync(path, width, picture?.PendingData, token); }
            catch (FileNotFoundException) when (picture?.FallbackPath is not null)
            {
                result = await ArtworkPreviewCache.Shared.AcquireAsync(picture.FallbackPath, width, token: token);
            }
            if (token.IsCancellationRequested) result?.Dispose();
            else
            {
                preview = result;
                Source = thumbnail = result?.Image;
                loadedPath = result?.Path;
                downloaded = result?.OriginalData;
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
        preview?.Dispose();
        preview = null;
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
        if (await owner.StorageProvider.TryGetFileFromPathAsync(path) is { } file) await DefaultFileLauncher.LaunchAsync(owner, file);
    }
}
