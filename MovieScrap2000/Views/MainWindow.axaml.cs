using Avalonia;
using Avalonia.Controls;
using Avalonia.Input.Platform;
using Avalonia.Platform;
using Avalonia.Platform.Storage;
using Avalonia.Styling;
using Avalonia.Interactivity;
using System.ComponentModel;
using VideoScrap2000.ViewModels;
using MovieScrap2000;

namespace VideoScrap2000.Views;

public partial class MainWindow : Window
{
    private readonly MainViewModel model;
    private LibraryProgressWindow? progressWindow;
    public MainWindow() : this(new MainViewModel(), true) { }
    public MainWindow(MainViewModel model, bool initialize)
    {
        InitializeComponent();
        Icon = new WindowIcon(AssetLoader.Open(new Uri("avares://VideoScrap2000/Assets/videoscrap-2000.png")));
        this.model = model;
        DataContext = model;
        model.ThemeRequested = ApplyTheme;
        ApplyTheme(model.ThemeIndex switch { 1 => "Light", 2 => "Dark", _ => "System" });
        model.PropertyChanged += OnModelPropertyChanged;
        if (initialize) Opened += async (_, _) => await model.InitializeAsync();
        Closing += (_, e) =>
        {
            if (model.IsBusy) { model.CancelCommand.Execute(null); e.Cancel = true; model.Status = "Vorgang wird abgebrochen. Bitte danach erneut schließen."; }
        };
        Closed += (_, _) =>
        {
            model.PropertyChanged -= OnModelPropertyChanged;
            model.Dispose();
        };
    }
    private async void OnModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(MainViewModel.IsBusy) || !IsVisible) return;
        if (!model.IsBusy)
        {
            progressWindow?.Close();
            return;
        }

        var dialog = new LibraryProgressWindow { DataContext = model };
        progressWindow = dialog;
        try { await dialog.ShowDialog(this); }
        finally { if (ReferenceEquals(progressWindow, dialog)) progressWindow = null; }
    }
    private static void ApplyTheme(string theme)
    {
        if (Application.Current is { } app)
            app.RequestedThemeVariant = theme switch { "Light" => ThemeVariant.Light, "Dark" => ThemeVariant.Dark, _ => ThemeVariant.Default };
    }
    private async void ShowAbout(object? sender, RoutedEventArgs e) => await new AboutWindow().ShowDialog(this);
    private async void ShowSettings(object? sender, RoutedEventArgs e)
    {
        await new SettingsWindow(model).ShowDialog(this);
        if (LibraryTabs.SelectedIndex == 1 && model.Series.NeedsScan) await model.Series.ScanAsync();
    }

    private async void ScanActiveLibrary(object? sender, RoutedEventArgs e)
    {
        if (LibraryTabs.SelectedIndex == 1) await model.Series.ScanAsync();
        else await model.ScanCommand.ExecuteAsync(null);
    }

    private async void LibraryTabChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (model is not null && IsVisible && ReferenceEquals(e.Source, LibraryTabs) && LibraryTabs.SelectedIndex == 1 && model.Series.NeedsScan)
            await model.Series.ScanAsync();
    }
    private void PrepareMovieRow(object? sender, ContainerPreparedEventArgs e)
    {
        if (e.Container is not ListBoxItem { DataContext: MovieViewModel movie } row) return;
        var remove = new MenuItem { DataContext = movie };
        remove.Click += RemoveUnavailableMovie;
        var menu = new ContextMenu { Items = { remove } };
        menu.Opening += (_, _) =>
        {
            remove.Header = Localization.Locale.T("Aus Liste entfernen");
            remove.IsEnabled = movie.IsFolderUnavailable;
        };
        row.ContextMenu = menu;
    }

    private void ClearMovieRow(object? sender, ContainerClearingEventArgs e)
    {
        e.Container.ContextMenu?.Close();
        e.Container.ContextMenu = null;
    }

    private async void RemoveUnavailableMovie(object? sender, RoutedEventArgs e)
    {
        if ((sender as Control)?.DataContext is MovieViewModel movie) await model.RemoveUnavailableMovieAsync(movie);
    }
    private async void ActorListKeyDown(object? sender, Avalonia.Input.KeyEventArgs e)
    {
        if (e.Key == Avalonia.Input.Key.Delete && model.RemoveActorCommand.CanExecute(null))
        {
            model.RemoveActorCommand.Execute(null); e.Handled = true;
        }
        else if (e.Key == Avalonia.Input.Key.F2 && model.CanEdit && model.SelectedActors.Count == 1)
        {
            e.Handled = true; await EditActorAsync(model.SelectedActors[0]);
        }
    }
    private void RemoveActorEntry(object? sender, RoutedEventArgs e)
    {
        if ((sender as Control)?.DataContext is ActorEditor actor) model.RemoveActorEntry(actor);
        e.Handled = true;
    }
    private async void AddActorEntry(object? sender, RoutedEventArgs e)
    {
        if (!model.CanEdit) return;
        var dialog = new ActorEditWindow(new ActorEditor { Order = model.Actors.Count });
        await dialog.ShowDialog(this);
        if (!dialog.IsAccepted) return;
        model.Actors.Add(dialog.Draft); model.SelectedActors.Clear(); model.SelectedActors.Add(dialog.Draft);
    }
    private async Task EditActorAsync(ActorEditor actor)
    {
        if (!model.CanEdit) return;
        var dialog = new ActorEditWindow(actor);
        await dialog.ShowDialog(this);
        if (!dialog.IsAccepted) return;
        actor.Name = dialog.Draft.Name; actor.Role = dialog.Draft.Role; actor.TmdbId = dialog.Draft.TmdbId;
    }
    private async void EditActorEntry(object? sender, RoutedEventArgs e)
    {
        if ((sender as Control)?.DataContext is ActorEditor actor) await EditActorAsync(actor);
    }
    private async Task OpenActorAsync(ActorEditor? actor)
    {
        if (actor is null) return;
        try
        {
            var path = await model.GetActorOpenPathAsync(actor);
            var file = path is null ? null : await StorageProvider.TryGetFileFromPathAsync(path);
            if (file is null || !await Launcher.LaunchFileAsync(file)) model.Status = "Kein Darstellerbild verfügbar.";
        }
        catch (Exception ex) { model.Status = ex.Message; }
    }
    private async void OpenActorPhoto(object? sender, Avalonia.Input.TappedEventArgs e)
    {
        e.Handled = true;
        await OpenActorAsync((sender as Control)?.DataContext as ActorEditor);
    }
    private async void OpenSelectedActorPhoto(object? sender, Avalonia.Input.TappedEventArgs e)
    {
        e.Handled = true;
        await OpenActorAsync(model.SelectedActor);
    }
    private async Task OpenImageAsync(ArtworkViewModel? image)
    {
        if (image is null) return;
        try
        {
            var path = await image.GetOpenPathAsync();
            var file = await StorageProvider.TryGetFileFromPathAsync(path);
            if (file is null || !await Launcher.LaunchFileAsync(file)) model.Status = "Das Bild konnte nicht mit der Standard-App geöffnet werden.";
        }
        catch (Exception ex) { model.Status = ex.Message; }
    }
    private async void OpenArtwork(object? sender, Avalonia.Input.TappedEventArgs e)
    {
        e.Handled = true;
        await OpenImageAsync((sender as Control)?.DataContext as ArtworkViewModel);
    }
    private async void OpenPoster(object? sender, Avalonia.Input.TappedEventArgs e)
    {
        e.Handled = true;
        await OpenImageAsync(model.Artwork.FirstOrDefault(p => p.Type == ImageTypes.Poster));
    }
    private async void ChooseLocalArtwork(object? sender, RoutedEventArgs e)
    {
        if (model.ArtworkType == "Alle") { model.Status = "Bitte wähle für ein lokales Bild zuerst den gewünschten Bildtyp."; return; }
        try
        {
            var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = Localization.Locale.T("Artwork auswählen"), AllowMultiple = false,
                FileTypeFilter = [new FilePickerFileType(Localization.Locale.T("Bilder")) { Patterns = ["*.jpg", "*.jpeg", "*.png", "*.webp"] }]
            });
            if (files.FirstOrDefault()?.TryGetLocalPath() is { } path)
                model.StageArtwork(new() { Type = model.ArtworkType, Data = await File.ReadAllBytesAsync(path) });
        }
        catch (Exception ex) { model.Status = ex.Message; }
    }
    private async void ChooseOnlineArtwork(object? sender, RoutedEventArgs e)
    {
        if (model.SelectedMovie?.FullMovieNFO is not { } nfo) return;
        if (!int.TryParse(nfo.TmdbId, out var id)) id = nfo.TmdbIdFromUnique ?? 0;
        if (id <= 0) { model.Status = "Bitte ordne den Film zuerst über Metadaten suchen einem TMDb-Treffer zu."; return; }
        await ImportOnlineArtworkAsync(id, model.ArtworkType);
    }
    private async Task ImportOnlineArtworkAsync(int id, string requestedType, IReadOnlySet<string>? automaticTypes = null)
    {
        var selectedMovie = model.SelectedMovie;
        var dialog = new ArtworkWindow(id, requestedType, language: model.ArtworkLanguage, automaticTypes: automaticTypes);
        await dialog.ShowDialog(this);
        if (!dialog.IsAccepted || !ReferenceEquals(selectedMovie, model.SelectedMovie)) return;
        var imported = 0;
        foreach (var image in dialog.Results)
        {
            try { model.StageArtwork(image); imported++; }
            catch (Exception ex) { model.Status = ex.Message; }
        }
        if (requestedType == "Alle")
        {
            model.ArtworkType = "Alle";
            if (imported > 0) model.Status = $"{imported} Artwork-Bilder übernommen. Mit Änderungen speichern werden sie lokal gespeichert.";
        }
    }
    private async void OpenOutlinePrompt(object? sender, RoutedEventArgs e)
    {
        if (!model.CanEdit) return;
        var plot = model.Plot.Trim();
        if (plot.Length == 0)
        {
            model.Status = "Für den Kurzinhalt fehlt die Handlung.";
            return;
        }

        const int maxLength = 239;
        var instruction = model.MetadataLanguage.StartsWith("de", StringComparison.OrdinalIgnoreCase)
            ? $"Fasse die folgende Filmhandlung spoilerfrei auf Deutsch in höchstens {maxLength} Zeichen inklusive Leerzeichen zusammen. Verwende nur Informationen aus der Handlung. Gib nur den Kurzinhalt aus."
            : $"Summarize the following movie plot in English in at most {maxLength} characters including spaces. Avoid spoilers and use only facts from the plot. Return only the outline.";

        if (Clipboard is not { } clipboard) return;
        try { await clipboard.SetTextAsync($"{instruction}\n\n{plot}"); }
        catch (Exception)
        {
            model.Status = "Der Prompt konnte nicht in die Zwischenablage kopiert werden.";
            return;
        }

        var opened = false;
        try { opened = await Launcher.LaunchUriAsync(new Uri("https://chatgpt.com/")); }
        catch (Exception ex) { System.Diagnostics.Trace.WriteLine(ex); }
        model.Status = opened
            ? "Prompt kopiert. Füge ihn im Browser in ChatGPT ein."
            : "Prompt kopiert. ChatGPT konnte nicht im Browser geöffnet werden.";
    }

    private async void ShowScrape(object? sender, RoutedEventArgs e)
    {
        var selectedMovie = model.SelectedMovie;
        if (selectedMovie?.FullMovieNFO is not { } nfo) return;
        var firstLookup = (!int.TryParse(nfo.TmdbId, out var existingId) || existingId <= 0)
            && nfo.TmdbIdFromUnique.GetValueOrDefault() <= 0;
        var dialog = new ScrapeWindow(model.Title, model.Imdb, model.Year, model.MetadataLanguage);
        await dialog.ShowDialog(this);
        if (dialog.Result is not { } movie || !ReferenceEquals(selectedMovie, model.SelectedMovie)) return;
        model.ApplyScrapedDetails(movie);
        if (movie.Id <= 0) return;

        await model.ArtworkLoadingTask;
        if (!ReferenceEquals(selectedMovie, model.SelectedMovie) || model.IsBusy) return;
        if (!firstLookup && model.Artwork.Count != 0) return;
        var missing = ImageTypes.AllTypes.Except(model.Artwork.Select(image => image.Type)).ToHashSet();
        if (missing.Count != 0) await ImportOnlineArtworkAsync(movie.Id, "Alle", missing);
    }
}
