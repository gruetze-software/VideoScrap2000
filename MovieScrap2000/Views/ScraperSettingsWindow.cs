using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using MovieScrap2000;
using static VideoScrap2000.Localization.Locale;

namespace VideoScrap2000.Views;

public sealed class ScraperSettingsWindow : Window
{
    public ScraperSettingsWindow(ScraperSettings current)
    {
        Title = T("Scraper und API-Zugänge"); Width = 660; Height = 650; MinWidth = 500; MinHeight = 420;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        var panel = new StackPanel { Spacing = 10 };
        void Label(string text) => panel.Children.Add(new TextBlock { Text = T(text), TextWrapping = Avalonia.Media.TextWrapping.Wrap });
        TextBox Secret(string text, string value)
        {
            Label(text);
            var input = new TextBox { Text = value, PasswordChar = '●', PlaceholderText = T("Optional") };
            input.Classes.Add("apiCredential");
            Avalonia.Automation.AutomationProperties.SetName(input, T(text));
            panel.Children.Add(input); return input;
        }
        CheckBox Toggle(string text, bool value)
        {
            var check = new CheckBox { Content = T(text), IsChecked = value }; panel.Children.Add(check); return check;
        }
        void Link(string text, string url)
        {
            var button = new Button { Content = T(text), HorizontalAlignment = HorizontalAlignment.Left };
            button.Click += async (_, _) => { try { await Launcher.LaunchUriAsync(new Uri(url)); } catch { status.Text = T("Der Link konnte nicht geöffnet werden."); } };
            panel.Children.Add(button);
        }
        Label("In offiziellen Builds brauchst du keine eigenen API-Keys. TMDB, Fanart.tv und TheTVDB verwenden die enthaltenen Projektzugänge; TVmaze benötigt keinen Key. Eigene Zugangsdaten sind optional. Sie werden nur lokal und ohne Verschlüsselung gespeichert. Teile deine Einstellungsdatei nicht.");
        var tmdb = Secret("TMDB · API Read Access Token (v4)", current.TmdbReadToken);
        Link("TMDB-Zugang beantragen ↗", "https://www.themoviedb.org/settings/api");
        var tmdbImages = Toggle("Bilder von TMDB", current.EnableTmdbArtwork);
        var fanart = Toggle("Bilder von Fanart.tv", current.EnableFanart);
        var personal = Secret("Fanart.tv · persönlicher API-Key", current.FanartPersonalKey);
        Link("Fanart.tv · persönlichen Key erstellen ↗", "https://fanart.tv/get-an-api-key/");
        var maze = Toggle("TVmaze · zusätzliche Episodenbilder (ohne API-Key)", current.EnableTvmaze);
        Link("TVmaze ↗", "https://www.tvmaze.com/api");
        var tvdb = Toggle("TheTVDB für Serien aktivieren", current.EnableTvdb);
        Label("Der TheTVDB-Projektzugang ist in offiziellen Builds enthalten. Ein eigener API-Key ist nicht erforderlich. Eine persönliche PIN wird nur benötigt, wenn der Anbieter sie für den Projektzugang verlangt.");
        var pin = Secret("TheTVDB · persönliche PIN", current.TvdbPin);
        Link("TheTVDB · Zugang und Kosten ↗", "https://www.thetvdb.com/api-information");
        Label("Standardquelle für Serien");
        var source = new ComboBox { ItemsSource = new[] { "TMDB", "TheTVDB" }, SelectedIndex = current.SeriesProvider == "TheTVDB" ? 1 : 0 };
        source.IsEnabled = tvdb.IsChecked == true;
        tvdb.PropertyChanged += (_, change) =>
        {
            if (change.Property != Avalonia.Controls.Primitives.ToggleButton.IsCheckedProperty) return;
            source.IsEnabled = tvdb.IsChecked == true;
            if (!source.IsEnabled) source.SelectedIndex = 0;
        };
        panel.Children.Add(source);
        panel.Children.Add(status);
        var buttons = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0, 16, 0, 0) };
        var cancel = new Button { Content = T("Abbrechen"), IsCancel = true };
        cancel.Click += (_, _) => Close();
        var save = new Button { Content = T("Übernehmen") };
        save.Click += (_, _) =>
        {
            var result = new ScraperSettings { TmdbReadToken = tmdb.Text ?? "", FanartPersonalKey = personal.Text ?? "", TvdbApiKey = current.TvdbApiKey, TvdbPin = pin.Text ?? "", EnableTmdbArtwork = tmdbImages.IsChecked == true, EnableFanart = fanart.IsChecked == true, EnableTvmaze = maze.IsChecked == true, EnableTvdb = tvdb.IsChecked == true, SeriesProvider = source.SelectedIndex == 1 ? "TheTVDB" : "TMDB" };
            try { result.Normalize(); Close(result); } catch (InvalidOperationException ex) { status.Text = T(ex.Message); }
        };
        buttons.Children.Add(cancel); buttons.Children.Add(save);
        var grid = new Grid { Margin = new Thickness(24), RowDefinitions = new RowDefinitions("*,Auto") };
        grid.Children.Add(new ScrollViewer { Content = panel, HorizontalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Disabled });
        Grid.SetRow(buttons, 1); grid.Children.Add(buttons); Content = grid;
    }
    private readonly TextBlock status = new() { TextWrapping = Avalonia.Media.TextWrapping.Wrap };
}
