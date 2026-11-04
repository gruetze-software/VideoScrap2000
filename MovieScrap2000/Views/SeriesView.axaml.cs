using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Platform.Storage;
using MovieScrap2000;
using MovieScrap2000.TMDbScraper;
using VideoScrap2000.Localization;
using VideoScrap2000.ViewModels;

namespace VideoScrap2000.Views;

public partial class SeriesView : UserControl
{
    public SeriesView() => InitializeComponent();

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
        var matches = await model.FindShowsAsync(editor.Title);
        if (matches.Count == 0 || !ReferenceEquals(editor, model.ShowEditor) || !owner.IsVisible) return;
        var dialog = CreateMatchDialog(matches);
        var selected = await dialog.ShowDialog<SearchTv?>(owner);
        if (selected is { Id: > 0 } && ReferenceEquals(editor, model.ShowEditor)) await model.ApplyShowMatchAsync(selected.Id);
    }

    private static Window CreateMatchDialog(IReadOnlyList<SearchTv> matches)
    {
        var dialog = new Window
        {
            Title = Locale.T("Serien nach TMDB durchsuchen"), Width = 540, Height = 420,
            WindowStartupLocation = WindowStartupLocation.CenterOwner, ShowInTaskbar = false
        };
        var results = new ListBox
        {
            ItemsSource = matches.Select(show => new ShowMatch(show)).ToArray(), SelectedIndex = 0,
            DisplayMemberBinding = new Binding(nameof(ShowMatch.Label))
        };
        var cancel = new Button { Content = Locale.T("Abbrechen"), IsCancel = true };
        var accept = new Button { Content = Locale.T("Metadaten übernehmen"), IsDefault = true, Classes = { "primary" } };
        cancel.Click += (_, _) => dialog.Close();
        void Accept() { if (results.SelectedItem is ShowMatch match) dialog.Close(match.Show); }
        accept.Click += (_, _) => Accept();
        results.DoubleTapped += (_, _) => Accept();
        var actions = new StackPanel
        {
            Orientation = Orientation.Horizontal, Spacing = 8, HorizontalAlignment = HorizontalAlignment.Right,
            Margin = new Thickness(0, 12, 0, 0), Children = { cancel, accept }
        };
        Grid.SetRow(actions, 1);
        dialog.Content = new Grid { RowDefinitions = new RowDefinitions("*,Auto"), Margin = new Thickness(20), Children = { results, actions } };
        return dialog;
    }

    private async void SearchEpisode(object? sender, RoutedEventArgs e)
    {
        if (DataContext is SeriesViewModel model) await model.FindEpisodeAsync();
    }

    private async void ShowSeasonCast(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not SeriesViewModel model || TopLevel.GetTopLevel(this) is not Window owner) return;
        var season = model.SelectedSeason;
        var cast = await model.FindSeasonCastAsync();
        if (cast.Count == 0 || !ReferenceEquals(season, model.SelectedSeason) || !owner.IsVisible) return;

        var dialog = new Window
        {
            Title = $"{Locale.T("Staffelbesetzung")} · {season!.Name}", Width = 480, Height = 450,
            WindowStartupLocation = WindowStartupLocation.CenterOwner, ShowInTaskbar = false
        };
        var list = new ListBox { ItemsSource = cast.Select(actor => string.IsNullOrWhiteSpace(actor.Role) ? actor.Name : $"{actor.Name} · {actor.Role}").ToArray() };
        var close = new Button { Content = Locale.T("Schließen"), IsCancel = true, IsDefault = true, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0, 12, 0, 0) };
        close.Click += (_, _) => dialog.Close();
        Grid.SetRow(close, 1);
        dialog.Content = new Grid { RowDefinitions = new RowDefinitions("*,Auto"), Margin = new Thickness(20), Children = { list, close } };
        await dialog.ShowDialog(owner);
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
            editor.Actors.Remove(actor);
        e.Handled = true;
    }

    private async void ActorKeyDown(object? sender, KeyEventArgs e)
    {
        if (DataContext is not SeriesViewModel { ShowEditor: { } editor }) return;
        var selected = SeriesCast.SelectedItems?.OfType<ActorEditor>().ToArray() ?? [];
        if (e.Key == Key.Delete)
        {
            foreach (var actor in selected) editor.Actors.Remove(actor);
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

    private sealed record ShowMatch(SearchTv Show)
    {
        public string Label => Show.FirstAirDate is { } date ? $"{Show.Name} ({date:yyyy})" : Show.Name ?? "";
    }
}
