using Avalonia;
using Avalonia.Controls;

namespace VideoScrap2000;

internal enum LinuxDisplayBackend { Auto, Wayland, X11 }

internal static class LinuxDesktop
{
    internal const string AppId = "de.gruetze_software.VideoScrap2000";
    internal const string BackendVariable = "VIDEOSCRAP2000_LINUX_BACKEND";

    internal static LinuxDisplayBackend ParseBackend(string? value) => value?.Trim().ToLowerInvariant() switch
    {
        null or "" or "auto" => LinuxDisplayBackend.Auto,
        "wayland" => LinuxDisplayBackend.Wayland,
        "x11" => LinuxDisplayBackend.X11,
        _ => throw new ArgumentException($"{BackendVariable} must be auto, wayland or x11.")
    };

    internal static AppBuilder Configure(AppBuilder builder)
    {
#if !TARGET_WINDOWS && !TARGET_MACOS
        if (OperatingSystem.IsLinux())
        {
            builder.With(new X11PlatformOptions { WmClass = AppId })
                .With(new WaylandPlatformOptions { AppId = AppId });
            switch (ParseBackend(Environment.GetEnvironmentVariable(BackendVariable)))
            {
                case LinuxDisplayBackend.Auto: builder.UseWaylandWithFallback(); break;
                case LinuxDisplayBackend.Wayland: builder.UseWayland(); break;
                case LinuxDisplayBackend.X11: break;
            }
        }
#endif
        return builder;
    }

    internal static string Describe(Window window) => window.PlatformImpl?.GetType().FullName ?? "unavailable";
}
