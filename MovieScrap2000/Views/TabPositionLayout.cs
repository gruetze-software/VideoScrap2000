using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Media;

namespace VideoScrap2000.Views;

public sealed class TabPositionLayout : AvaloniaObject
{
    public static readonly AttachedProperty<int> PositionProperty =
        AvaloniaProperty.RegisterAttached<TabPositionLayout, TabControl, int>("Position", -1);

    static TabPositionLayout()
    {
        PositionProperty.Changed.AddClassHandler<TabControl>((tabs, _) =>
        {
            Apply(tabs);
            tabs.AttachedToVisualTree -= OnAttached;
            tabs.AttachedToVisualTree += OnAttached;
        });
    }

    public static int GetPosition(TabControl tabs) => tabs.GetValue(PositionProperty);
    public static void SetPosition(TabControl tabs, int value) => tabs.SetValue(PositionProperty, value);

    private static void OnAttached(object? sender, VisualTreeAttachmentEventArgs e)
    {
        if (sender is TabControl tabs) Apply(tabs);
    }

    private static void Apply(TabControl tabs)
    {
        var position = Math.Clamp(GetPosition(tabs), 0, 2);
        tabs.TabStripPlacement = position switch { 0 => Dock.Top, 2 => Dock.Right, _ => Dock.Left };
        foreach (var item in tabs.Items.OfType<TabItem>())
        {
            item.HeaderTemplate = new FuncDataTemplate<object>((header, _) =>
                new LayoutTransformControl
                {
                    LayoutTransform = new RotateTransform(position == 0 ? 0 : position == 1 ? -90 : 90),
                    Child = new TextBlock { Text = header?.ToString() }
                });
            item.Padding = position == 0 ? new Thickness(12, 9) : new Thickness(9, 12);
            item.Margin = position switch
            {
                0 => new Thickness(0, 0, 6, 8),
                1 => new Thickness(0, 0, 8, 6),
                _ => new Thickness(8, 0, 0, 6)
            };
        }
    }
}
