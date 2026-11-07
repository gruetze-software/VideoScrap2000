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
    }
    private async Task ChooseAsync(bool series)
    {
        try
        {
            var folders = await StorageProvider.OpenFolderPickerAsync(new() { Title = Localization.Locale.T(series ? "Serienordner auswählen" : "Filmordner auswählen"), AllowMultiple = false });
            if (folders.FirstOrDefault()?.TryGetLocalPath() is { } path)
            {
                if (series) draft.SeriesPath = path; else draft.LibraryPath = path;
            }
        }
        catch (Exception ex) { draft.Status = ex.Message; }
    }
    private async void ChooseMovies(object? sender, RoutedEventArgs e) => await ChooseAsync(false);
    private async void ChooseSeries(object? sender, RoutedEventArgs e) => await ChooseAsync(true);
    private void Save(object? sender, RoutedEventArgs e)
    {
        try { main?.ApplySettings(draft.LibraryPath, draft.SeriesPath, draft.ThemeIndex, draft.LanguageIndex, draft.MetadataLanguageIndex, draft.ArtworkLanguageIndex, draft.UseFanartBackground, draft.ArtworkOptimizationProfile); Close(); }
        catch (Exception ex) { draft.Status = $"Einstellungen konnten nicht gespeichert werden: {ex.Message}"; }
    }
    private void Cancel(object? sender, RoutedEventArgs e) => Close();
    private async void OptimizeExisting(object? sender, RoutedEventArgs e)
        => await new ArtworkOptimizationWindow("", draft.ArtworkOptimizationProfile).ShowDialog(this);
    private void SwitchDemo(object? sender, RoutedEventArgs e) { SwitchDemoRequested = true; Close(); }
}
