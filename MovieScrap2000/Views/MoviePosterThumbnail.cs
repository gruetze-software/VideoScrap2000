using Avalonia;
using Avalonia.Controls;
using Avalonia.Media.Imaging;
using Avalonia.VisualTree;
using MovieScrap2000;
using VideoScrap2000.ViewModels;

namespace VideoScrap2000.Views;

public sealed class MoviePosterThumbnail : Image
{
    private static readonly SemaphoreSlim LoadSlots = new(4);
    private CancellationTokenSource? loading;
    private Bitmap? thumbnail;

    public MoviePosterThumbnail()
    {
        AttachedToVisualTree += (_, _) => LoadPoster();
        DetachedFromVisualTree += (_, _) => ClearPoster();
        DataContextChanged += (_, _) =>
        {
            if (this.IsAttachedToVisualTree()) LoadPoster();
        };
    }

    private async void LoadPoster()
    {
        ClearPoster();
        var (folder, video) = DataContext switch
        {
            MovieViewModel movie => (movie.FolderPath, movie.VideoFilePath),
            SeriesShowRow series => (series.Show.FolderPath, "tvshow"),
            _ => (null, null)
        };
        if (folder is null || video is null) return;

        using var request = new CancellationTokenSource();
        loading = request;
        var token = request.Token;
        try
        {
            await LoadSlots.WaitAsync(token);
            Bitmap? bitmap;
            try
            {
                bitmap = await Task.Run(async () =>
                {
                    var path = MainViewModel.FindArtwork(folder, video, ImageTypes.Poster).FirstOrDefault().Path;
                    if (path is null) return null;
                    var bytes = await File.ReadAllBytesAsync(path, token);
                    token.ThrowIfCancellationRequested();
                    return ArtworkDecoder.DecodePreview(bytes, 88);
                }, token);
            }
            finally { LoadSlots.Release(); }

            if (token.IsCancellationRequested) bitmap?.Dispose();
            else Source = thumbnail = bitmap;
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
    }
}
