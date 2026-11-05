using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using VideoScrap2000.Views;
namespace VideoScrap2000;
public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
        Localization.Locale.Apply("System");
    }
    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            desktop.MainWindow = desktop.Args?.Contains("--demo", StringComparer.OrdinalIgnoreCase) == true
                ? new MainWindow(DemoLibrary.CreateModel(), true) : new MainWindow();
        base.OnFrameworkInitializationCompleted();
    }
}
