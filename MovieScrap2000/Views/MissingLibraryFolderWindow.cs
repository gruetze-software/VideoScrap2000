using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using VideoScrap2000.Localization;

namespace VideoScrap2000.Views;

internal sealed class MissingLibraryFolderWindow : Window
{
    public MissingLibraryFolderWindow(string message)
    {
        Title = Locale.T("Ordner auswählen");
        Width = 420;
        SizeToContent = SizeToContent.Height;
        CanResize = false;
        ShowInTaskbar = false;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        var close = new Button { Content = Locale.T("Schließen"), IsCancel = true };
        close.Click += (_, _) => Close(false);
        var settings = new Button { Name = "OpenSettingsButton", Content = Locale.T("Einstellungen öffnen"), IsDefault = true };
        settings.Classes.Add("primary");
        settings.Click += (_, _) => Close(true);
        Content = new StackPanel
        {
            Margin = new Thickness(24), Spacing = 20,
            Children =
            {
                new TextBlock { Text = message, TextWrapping = TextWrapping.Wrap },
                new StackPanel
                {
                    Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right,
                    Spacing = 8, Children = { close, settings }
                }
            }
        };
    }
}
