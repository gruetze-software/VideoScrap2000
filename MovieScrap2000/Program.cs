using Avalonia;
namespace VideoScrap2000;
internal static class Program
{
    [STAThread]
    public static void Main(string[] args) => BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
    public static AppBuilder BuildAvaloniaApp()
    {
        var builder = AppBuilder.Configure<App>().WithInterFont().LogToTrace();
#if TARGET_WINDOWS
        return builder.UseWin32().UseSkia();
#elif TARGET_LINUX
        return builder.UseX11().UseSkia();
#elif TARGET_MACOS
        return builder.UseAvaloniaNative().UseSkia();
#else
        return builder.UsePlatformDetect();
#endif
    }
}
