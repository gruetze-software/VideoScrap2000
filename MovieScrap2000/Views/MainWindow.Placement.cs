using Avalonia;
using Avalonia.Controls;

namespace VideoScrap2000.Views;

public partial class MainWindow
{
    private WindowPlacementStore? placementStore;
    private Size normalSize;
    private bool wasMaximized;
    private bool placementOpened;

    private void ConfigureWindowPlacement(WindowPlacementStore? store)
    {
        placementStore = store;
        if (store is null) return;
        var saved = store.Load();
        var screen = Screens.ScreenFromWindow(this) ?? Screens.Primary;
        if (screen is { WorkingArea: { Width: > 0, Height: > 0 }, Scaling: > 0 } display)
        {
            var availableWidth = Math.Max(1, display.WorkingArea.Width / display.Scaling);
            var availableHeight = Math.Max(1, display.WorkingArea.Height / display.Scaling - 40);
            MinWidth = Math.Min(MinWidth, availableWidth);
            MinHeight = Math.Min(MinHeight, availableHeight);
            Width = Math.Clamp(saved.Width, MinWidth, availableWidth);
            Height = Math.Clamp(saved.Height, MinHeight, availableHeight);
        }
        else
        {
            Width = Math.Max(MinWidth, saved.Width);
            Height = Math.Max(MinHeight, saved.Height);
        }
        normalSize = new(Width, Height);
        wasMaximized = saved.Maximized;
        if (saved.Maximized) WindowState = WindowState.Maximized;
        Opened += (_, _) => placementOpened = true;
        Resized += (_, e) =>
        {
            if (placementOpened && WindowState == WindowState.Normal && e.ClientSize is { Width: > 0, Height: > 0 })
                normalSize = e.ClientSize;
        };
        PropertyChanged += (_, e) =>
        {
            if (placementOpened && WindowState == WindowState.Normal && (e.Property == WidthProperty || e.Property == HeightProperty))
                normalSize = new(double.IsFinite(Width) && Width > 0 ? Width : normalSize.Width,
                    double.IsFinite(Height) && Height > 0 ? Height : normalSize.Height);
            if (e.Property == WindowStateProperty && WindowState != WindowState.Minimized)
                wasMaximized = WindowState == WindowState.Maximized;
        };
    }

    private void SaveWindowPlacement()
    {
        if (placementStore is null || !placementOpened) return;
        try { placementStore.Save(new(normalSize.Width, normalSize.Height, wasMaximized)); }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        { System.Diagnostics.Trace.WriteLine($"Window placement could not be saved: {ex.Message}"); }
    }
}
