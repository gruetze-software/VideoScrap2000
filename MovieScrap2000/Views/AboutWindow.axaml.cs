using static VideoScrap2000.Localization.Locale;
using Avalonia.Controls;
using Avalonia.Interactivity;
namespace VideoScrap2000.Views;
public partial class AboutWindow : Window
{
    private readonly CancellationTokenSource updateLifetime = new();
    private readonly Version currentVersion = new(typeof(AboutWindow).Assembly.GetName().Version!.ToString(3));
    private Version? updateVersion;

    public AboutWindow()
    {
        InitializeComponent();
        VersionText.Text = $"Version {currentVersion}";
        UpdatePanel.IsVisible = ReleaseUpdateCheck.IsAvailable;
        CheckUpdateButton.Content = T("Nach Updates suchen");
        DownloadUpdateButton.Content = T("Zur Download-Seite");
        Closed += (_, _) => { updateLifetime.Cancel(); updateLifetime.Dispose(); };
    }
    private async void CheckUpdates(object? sender, RoutedEventArgs e) => await CheckUpdatesAsync();

    internal async Task CheckUpdatesAsync()
    {
        if (!ReleaseUpdateCheck.IsAvailable || !CheckUpdateButton.IsEnabled) return;
        var token = updateLifetime.Token;
        CheckUpdateButton.IsEnabled = false;
        DownloadUpdateButton.IsVisible = false;
        updateVersion = null;
        UpdateStatus.IsVisible = true;
        UpdateStatus.Text = T("Suche nach Updates …");
        try
        {
            var latest = await ReleaseUpdateCheck.GetLatestVersionAsync(token);
            token.ThrowIfCancellationRequested();
            if (latest > currentVersion)
            {
                updateVersion = latest;
                UpdateStatus.Text = string.Format(T("Version {0} ist verfügbar."), latest);
                DownloadUpdateButton.IsVisible = true;
            }
            else UpdateStatus.Text = T("VideoScrap 2000 ist auf dem neuesten Stand.");
        }
        catch (Exception ex) when (ex is HttpRequestException or IOException or System.Text.Json.JsonException or OperationCanceledException)
        {
            if (!token.IsCancellationRequested) UpdateStatus.Text = T("Die Update-Prüfung ist fehlgeschlagen.");
        }
        finally { if (!token.IsCancellationRequested) CheckUpdateButton.IsEnabled = true; }
    }
    private async void OpenUpdate(object? sender, RoutedEventArgs e)
    {
        if (updateVersion is { } version) await OpenAsync(ReleaseUpdateCheck.DownloadUrl(version));
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
