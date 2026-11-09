using static VideoScrap2000.Localization.Locale;
using Avalonia.Controls;
using Avalonia.Interactivity;
namespace VideoScrap2000.Views;
public partial class AboutWindow : Window
{
    public AboutWindow()
    {
        InitializeComponent();
        VersionText.Text = $"Version {typeof(AboutWindow).Assembly.GetName().Version?.ToString(3)}";
    }
    private async Task OpenAsync(string url)
    {
        try { if (!await Launcher.LaunchUriAsync(new Uri(url))) LinkStatus.Text = T("Der Link konnte nicht geöffnet werden."); }
        catch (Exception ex) { LinkStatus.Text = T(ex.Message); }
    }
    private async void OpenKofi(object? sender, RoutedEventArgs e) => await OpenAsync("https://ko-fi.com/gruetzesoftware");
    private async void OpenManufacturer(object? sender, RoutedEventArgs e) => await OpenAsync("https://www.gruetze-software.de/");
    private async void OpenProject(object? sender, RoutedEventArgs e) => await OpenAsync("https://github.com/gruetze-software/VideoScrap2000");
    private async void OpenTmdb(object? sender, RoutedEventArgs e) => await OpenAsync("https://www.themoviedb.org/");
    private async void OpenFanart(object? sender, RoutedEventArgs e) => await OpenAsync("https://fanart.tv/");
    private async void OpenTvmaze(object? sender, RoutedEventArgs e) => await OpenAsync("https://www.tvmaze.com/");
    private async void OpenTvdb(object? sender, RoutedEventArgs e) => await OpenAsync("https://www.thetvdb.com/");
    private void CloseWindow(object? sender, RoutedEventArgs e) => Close();
}
