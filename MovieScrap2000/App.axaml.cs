using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using VideoScrap2000.Localization;
using VideoScrap2000.Views;
namespace VideoScrap2000;
public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
        Locale.Apply("System");
        if (OperatingSystem.IsMacOS()) NativeMenu.SetMenu(this, CreateMacMenu());
    }
    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            desktop.MainWindow = desktop.Args?.Contains("--demo", StringComparer.OrdinalIgnoreCase) == true
                ? new MainWindow(DemoLibrary.CreateModel(), true) : new MainWindow();
        base.OnFrameworkInitializationCompleted();
    }

    private NativeMenu CreateMacMenu()
    {
        var about = new NativeMenuItem(Locale.T("Über VideoScrap 2000"));
        var settings = new NativeMenuItem(Locale.T("Einstellungen") + " …") { Gesture = new(Key.OemComma, KeyModifiers.Meta) };
        var scan = new NativeMenuItem(Locale.T("Einlesen")) { Gesture = new(Key.R, KeyModifiers.Meta) };
        var quit = new NativeMenuItem(Locale.T("VideoScrap 2000 beenden")) { Gesture = new(Key.Q, KeyModifiers.Meta) };
        var menu = new NativeMenu { Items = { about, new NativeMenuItemSeparator(), settings, scan, new NativeMenuItemSeparator(), quit } };

        MainWindow? GetAvailableWindow()
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop &&
                desktop.MainWindow is MainWindow { CanUseAppMenu: true } window &&
                !desktop.Windows.Any(dialog => dialog.IsVisible && dialog.Owner == window))
                return window;
            return null;
        }

        menu.NeedsUpdate += (_, _) =>
        {
            about.Header = Locale.T("Über VideoScrap 2000");
            settings.Header = Locale.T("Einstellungen") + " …";
            scan.Header = Locale.T("Einlesen");
            quit.Header = Locale.T("VideoScrap 2000 beenden");
            about.IsEnabled = settings.IsEnabled = scan.IsEnabled = GetAvailableWindow() is not null;
        };
        about.Click += async (_, _) => { if (GetAvailableWindow() is { } window) await window.OpenAboutAsync(); };
        settings.Click += async (_, _) => { if (GetAvailableWindow() is { } window) await window.OpenSettingsAsync(); };
        scan.Click += async (_, _) => { if (GetAvailableWindow() is { } window) await window.ScanActiveLibraryAsync(); };
        quit.Click += (_, _) => (ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.TryShutdown();
        return menu;
    }
}
