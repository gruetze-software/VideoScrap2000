using Avalonia;
using System.Runtime.InteropServices;
using MovieScrap2000;
namespace VideoScrap2000;
internal static class Program
{
    [STAThread]
    public static int Main(string[] args)
    {
        try
        {
            if (args.Contains("--startup-check", StringComparer.OrdinalIgnoreCase))
            {
                BuildAvaloniaApp().SetupWithoutStarting();
                return 0;
            }
            return BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
        }
        catch (Exception exception)
        {
            var details = exception.ToString();
            try
            {
                var log = Path.Combine(Utils.GetOptionsPath(), "startup-error.log");
                File.WriteAllText(log, details);
                details += "\n\nLog: " + log;
            }
            catch { /* The original startup error must remain visible if logging fails. */ }
            Console.Error.WriteLine(details);
#if TARGET_WINDOWS
            MessageBoxW(IntPtr.Zero, "VideoScrap 2000 could not start. / VideoScrap 2000 konnte nicht gestartet werden.\n\n" + details,
                "VideoScrap 2000", 0x10);
#endif
            return 1;
        }
    }
#if TARGET_WINDOWS
    [DllImport("user32.dll", CharSet = CharSet.Unicode, ExactSpelling = true)]
    private static extern int MessageBoxW(IntPtr window, string text, string caption, uint type);
#endif
    public static AppBuilder BuildAvaloniaApp()
    {
        var builder = AppBuilder.Configure<App>().WithInterFont().UseHarfBuzz().LogToTrace();
#if TARGET_WINDOWS
        return builder.UseWin32().UseSkia();
#elif TARGET_LINUX
        return LinuxDesktop.Configure(builder.UseX11().UseSkia());
#elif TARGET_MACOS
        return builder.With(new MacOSPlatformOptions { DisableDefaultApplicationMenuItems = true })
            .UseAvaloniaNative().UseSkia();
#else
        if (OperatingSystem.IsMacOS())
            builder.With(new MacOSPlatformOptions { DisableDefaultApplicationMenuItems = true });
        return LinuxDesktop.Configure(builder.UsePlatformDetect());
#endif
    }
}
