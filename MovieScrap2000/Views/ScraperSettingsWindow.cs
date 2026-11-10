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
        Title = T("Scraper"); Width = 560; Height = 510; MinWidth = 450; MinHeight = 400;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        var panel = new StackPanel { Spacing = 10 };
        void Label(string text) => panel.Children.Add(new TextBlock { Text = T(text), TextWrapping = Avalonia.Media.TextWrapping.Wrap });
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
        Label("Die Projektzugänge sind enthalten. Eigene API-Keys sind nicht nötig.");
        var tmdbImages = Toggle("Bilder von TMDB", current.EnableTmdbArtwork);
        Link("TMDB ↗", "https://www.themoviedb.org/");
        var fanart = Toggle("Bilder von Fanart.tv", current.EnableFanart);
        Link("Fanart.tv ↗", "https://fanart.tv/");
        var maze = Toggle("TVmaze · zusätzliche Episodenbilder (ohne API-Key)", current.EnableTvmaze);
        Link("TVmaze ↗", "https://www.tvmaze.com/api");
        var tvdb = Toggle("TheTVDB für Serien aktivieren", current.EnableTvdb);
        Link("TheTVDB ↗", "https://www.thetvdb.com/");
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
            var result = current.Copy();
            result.EnableTmdbArtwork = tmdbImages.IsChecked == true; result.EnableFanart = fanart.IsChecked == true;
            result.EnableTvmaze = maze.IsChecked == true; result.EnableTvdb = tvdb.IsChecked == true;
            result.SeriesProvider = source.SelectedIndex == 1 ? "TheTVDB" : "TMDB";
            try { result.Normalize(); Close(result); } catch (InvalidOperationException ex) { status.Text = T(ex.Message); }
        };
        buttons.Children.Add(cancel); buttons.Children.Add(save);
        var grid = new Grid { Margin = new Thickness(24), RowDefinitions = new RowDefinitions("*,Auto") };
        grid.Children.Add(new ScrollViewer { Content = panel, HorizontalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Disabled });
        Grid.SetRow(buttons, 1); grid.Children.Add(buttons); Content = grid;
    }
    private readonly TextBlock status = new() { TextWrapping = Avalonia.Media.TextWrapping.Wrap };
}
