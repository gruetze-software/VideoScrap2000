using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using MovieScrap2000;
using VideoScrap2000.Localization;
using VideoScrap2000.ViewModels;

namespace VideoScrap2000.Views;

public partial class SeriesView : UserControl
{
    private void RemoveArtwork(object? sender, RoutedEventArgs e)
    {
        if (DataContext is SeriesViewModel model && (sender as Control)?.DataContext is SeriesArtworkTile tile)
        {
            model.RemoveArtwork(tile);
            ListSelection.FocusSelectedItem(SeriesArtworkList);
        }
        e.Handled = true;
    }
    private void StopArtworkDoubleTap(object? sender, TappedEventArgs e) => e.Handled = true;
    public SeriesView() => InitializeComponent();
    private async void OpenMetadataSource(object? sender, RoutedEventArgs e)
    {
        if (TopLevel.GetTopLevel(this) is not { } owner) return;
        try { await owner.Launcher.LaunchUriAsync(new Uri("https://www.thetvdb.com/")); }
        catch (Exception ex) { System.Diagnostics.Trace.WriteLine(ex); }
    }

    private async void SearchArtwork(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not SeriesViewModel { SelectedShow: { } show } model
            || TopLevel.GetTopLevel(this) is not Window owner) return;
        var season = model.SelectedSeason?.Number;
        var dialog = model.CreateArtworkSearch();
        if (dialog is null) return;
        await dialog.ShowDialog(owner);
        if (dialog.IsAccepted && ReferenceEquals(show, model.SelectedShow))
            model.StageSeriesArtwork(dialog.Results, season);
    }

    private async void AddEpisode(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not SeriesViewModel { SelectedShow: { } show } model || TopLevel.GetTopLevel(this) is not { } owner) return;
        var files = await owner.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = Locale.T("Episode hinzufügen"),
            SuggestedStartLocation = await owner.StorageProvider.TryGetFolderFromPathAsync(show.Show.FolderPath),
            FileTypeFilter = [new FilePickerFileType(Locale.T("Videodateien")) { Patterns = MovieManager.VideoExtensions.Select(extension => "*" + extension).ToArray() }],
            AllowMultiple = false
        });
        if (ReferenceEquals(show, model.SelectedShow) && files.FirstOrDefault()?.TryGetLocalPath() is { } file)
            await model.AddEpisodeAsync(file);
    }

    private async void SearchShow(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not SeriesViewModel { ShowEditor: { } editor } model || TopLevel.GetTopLevel(this) is not Window owner) return;
        ScrapeWindow? dialog = null;
        dialog = new ScrapeWindow(editor.Title, (title, token) => model.SearchShowTitlesAsync(title, token, dialog?.SelectedProvider), model.Scrapers);
        await dialog.ShowDialog(owner);
        if (dialog.SeriesResult is { Id: > 0 } selected && ReferenceEquals(editor, model.ShowEditor))
            await model.ApplyShowMatchAsync(selected.Id, dialog.SelectedProvider);
    }
    private async void SearchEpisode(object? sender, RoutedEventArgs e)
    {
        if (DataContext is SeriesViewModel model) await model.FindEpisodeAsync();
    }

    private async void AddActor(object? sender, RoutedEventArgs e) => await EditActorAsync(null);

    private async void EditActor(object? sender, RoutedEventArgs e)
    {
        if ((sender as Control)?.DataContext is ActorEditor actor) await EditActorAsync(actor);
    }

    private async Task EditActorAsync(ActorEditor? actor)
    {
        if (DataContext is not SeriesViewModel { ShowEditor: { } editor } model || TopLevel.GetTopLevel(this) is not Window owner) return;
        var dialog = new ActorEditWindow(actor ?? new ActorEditor());
        await dialog.ShowDialog(owner);
        if (!dialog.IsAccepted || !ReferenceEquals(editor, model.ShowEditor)) return;
        if (actor is null)
        {
            actor = dialog.Draft;
            editor.Actors.Add(actor);
        }
        else
        {
            actor.Name = dialog.Draft.Name;
            actor.Role = dialog.Draft.Role;
            actor.TmdbId = dialog.Draft.TmdbId;
        }
        model.SelectedActor = actor;
        model.RefreshActorPreview();
    }

    private void RemoveActor(object? sender, RoutedEventArgs e)
    {
        if (DataContext is SeriesViewModel { ShowEditor: { } editor } && (sender as Control)?.DataContext is ActorEditor actor)
        {
            var index = editor.Actors.IndexOf(actor);
            if (index < 0) return;
            SeriesCast.SelectedItems?.Clear();
            editor.Actors.Remove(actor);
            SeriesCast.SelectedIndex = Math.Min(index, editor.Actors.Count - 1);
            ListSelection.FocusSelectedItem(SeriesCast);
        }
        e.Handled = true;
    }

    private async void ActorKeyDown(object? sender, KeyEventArgs e)
    {
        if (DataContext is not SeriesViewModel { ShowEditor: { } editor }) return;
        var selected = SeriesCast.SelectedItems?.OfType<ActorEditor>().ToArray() ?? [];
        if (e.Key == Key.Delete && selected.Length > 0)
        {
            var index = selected.Min(editor.Actors.IndexOf);
            SeriesCast.SelectedItems?.Clear();
            foreach (var actor in selected) editor.Actors.Remove(actor);
            SeriesCast.SelectedIndex = Math.Min(index, editor.Actors.Count - 1);
            ListSelection.FocusSelectedItem(SeriesCast);
            e.Handled = true;
        }
        else if (e.Key == Key.F2 && selected.Length == 1)
        {
            e.Handled = true;
            await EditActorAsync(selected[0]);
        }
    }

    private async void OpenPicture(object? sender, TappedEventArgs e)
    {
        if (sender is MoviePosterThumbnail picture && TopLevel.GetTopLevel(this) is { } owner) await OpenPictureAsync(picture, owner);
        e.Handled = true;
    }

    private async void OpenActorPicture(object? sender, TappedEventArgs e)
    {
        if (DataContext is SeriesViewModel model && (sender as Control)?.DataContext is ActorEditor actor && TopLevel.GetTopLevel(this) is { } owner)
        {
            model.SelectedActor = actor;
            await OpenPictureAsync(SeriesActorPhoto, owner);
        }
        e.Handled = true;
    }

    private static async Task OpenPictureAsync(MoviePosterThumbnail picture, TopLevel owner)
    {
        try { await picture.OpenImageAsync(owner); }
        catch (Exception ex) { System.Diagnostics.Trace.WriteLine(ex); }
    }

}
