using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Threading;
using MovieScrap2000;
using VideoScrap2000;
using VideoScrap2000.ViewModels;
using VideoScrap2000.Views;

if (args.Length != 1 || args[0] is not ("Wayland" or "X11"))
    throw new ArgumentException("Usage: DisplaySmoke Wayland|X11");
SmokeApplication.ExpectedBackend = args[0];
return LinuxDesktop.Configure(AppBuilder.Configure<SmokeApplication>().UsePlatformDetect())
    .WithInterFont().UseHarfBuzz().LogToTrace().StartWithClassicDesktopLifetime([]);

public sealed class SmokeApplication : App
{
    public static string ExpectedBackend { get; set; } = "";

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
            throw new InvalidOperationException("Desktop lifetime missing.");
        // Bypass production initialization and use the isolated XDG directories
        // supplied by the harness. Do not load personal settings or scan media.
        var model = new MainViewModel(settings: new AppSettings { Language = "en" });
        var window = new MainWindow(model, initialize: false);
        desktop.MainWindow = window;
        window.Opened += (_, _) => DispatcherTimer.RunOnce(async () =>
        {
            try
            {
                CheckBackend(window);
                await CheckDialog(window, new SettingsWindow(model));
                await CheckDialog(window, new AboutWindow());
                // An empty query deliberately avoids an online search.
                await CheckDialog(window, new ScrapeWindow());
                Console.WriteLine($"SMOKE PASS: {LinuxDesktop.Describe(window)}; main window and modal Settings, Info and search dialogs.");
                desktop.Shutdown();
            }
            catch (Exception exception)
            {
                Console.Error.WriteLine(exception);
                desktop.Shutdown(1);
            }
        }, TimeSpan.FromSeconds(1));
    }

    private static void CheckBackend(Window window)
    {
        var backend = LinuxDesktop.Describe(window);
        if (!backend.Contains(ExpectedBackend, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException($"Expected {ExpectedBackend}, got {backend}.");
    }

    private static async Task CheckDialog(Window owner, Window dialog)
    {
        var closed = dialog.ShowDialog(owner);
        try
        {
            await Task.Delay(300);
            if (!dialog.IsVisible) throw new InvalidOperationException("Modal dialog is not visible.");
            CheckBackend(dialog);
        }
        finally { dialog.Close(); }
        await closed;
    }
}
