using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using VideoScrap2000.ViewModels;
namespace VideoScrap2000.Views;

public partial class SettingsWindow : Window
{
    private readonly MainViewModel? main;
    private readonly SettingsViewModel draft;
    public bool SwitchDemoRequested { get; private set; }
    public SettingsWindow() : this(null) { }
    public SettingsWindow(MainViewModel? main)
    {
        InitializeComponent();
        this.main = main;
        draft = new() { IsDemo = main?.IsDemo ?? false, LibraryPath = main?.LibraryPath ?? "", SeriesPath = main?.SeriesPath ?? "", ThemeIndex = main?.ThemeIndex ?? 0,
            LanguageIndex = main?.LanguageIndex ?? 0, MetadataLanguageIndex = main?.MetadataLanguageIndex ?? 0, ArtworkLanguageIndex = main?.ArtworkLanguageIndex ?? 0,
            UseFanartBackground = main?.UseFanartBackground ?? false, ArtworkOptimizationProfile = main?.ArtworkOptimizationProfile ?? 0 };
        DataContext = draft;
        if (main is not null)
        {
            draft.MovieFolders.Clear(); foreach (var path in main.MovieFolders) draft.MovieFolders.Add(path);
            draft.SeriesFolders.Clear(); foreach (var path in main.SeriesFolders) draft.SeriesFolders.Add(path);
        }
    }
    private async Task ChooseAsync(bool series)
    {
        try
        {
            var folders = await StorageProvider.OpenFolderPickerAsync(new() { Title = Localization.Locale.T(series ? "Serienordner auswählen" : "Filmordner auswählen"), AllowMultiple = true });
            var target = series ? draft.SeriesFolders : draft.MovieFolders;
            foreach (var path in MovieScrap2000.LibrarySources.Normalize(folders.Select(folder => folder.TryGetLocalPath()).OfType<string>()))
            {
                if (!target.Contains(path, MovieScrap2000.LibrarySources.PathComparer)) target.Add(path);
            }
        }
        catch (Exception ex) { draft.Status = ex.Message; }
    }
    private async void ChooseMovies(object? sender, RoutedEventArgs e) => await ChooseAsync(false);
    private async void ChooseSeries(object? sender, RoutedEventArgs e) => await ChooseAsync(true);
    private void RemoveMovies(object? sender, RoutedEventArgs e) => RemoveSelected(MovieSources, draft.MovieFolders);
    private void RemoveSeries(object? sender, RoutedEventArgs e) => RemoveSelected(SeriesSources, draft.SeriesFolders);
    private static void RemoveSelected(ListBox list, System.Collections.ObjectModel.ObservableCollection<string> paths)
    {
        var index = list.SelectedIndex;
        foreach (var path in list.SelectedItems!.OfType<string>().ToArray()) paths.Remove(path);
        if (paths.Count > 0) { list.SelectedIndex = Math.Clamp(index, 0, paths.Count - 1); ListSelection.FocusSelectedItem(list); }
    }
    private void SourcesKeyDown(object? sender, Avalonia.Input.KeyEventArgs e)
    {
        if (e.Key != Avalonia.Input.Key.Delete || sender is not ListBox list) return;
        RemoveSelected(list, ReferenceEquals(list, MovieSources) ? draft.MovieFolders : draft.SeriesFolders);
        e.Handled = true;
    }
    private void Save(object? sender, RoutedEventArgs e)
    {
        try { main?.ApplySettings(draft.MovieFolders, draft.SeriesFolders, draft.ThemeIndex, draft.LanguageIndex, draft.MetadataLanguageIndex, draft.ArtworkLanguageIndex, draft.UseFanartBackground, draft.ArtworkOptimizationProfile); Close(); }
        catch (Exception ex) { draft.Status = $"Einstellungen konnten nicht gespeichert werden: {ex.Message}"; }
    }
    private void Cancel(object? sender, RoutedEventArgs e) => Close();
    private async void OptimizeExisting(object? sender, RoutedEventArgs e)
        => await new ArtworkOptimizationWindow("", draft.ArtworkOptimizationProfile).ShowDialog(this);
    private void SwitchDemo(object? sender, RoutedEventArgs e) { SwitchDemoRequested = true; Close(); }
}
