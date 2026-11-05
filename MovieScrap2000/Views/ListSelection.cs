using Avalonia.Controls;

namespace VideoScrap2000.Views;

internal static class ListSelection
{
    internal static void FocusSelectedItem(ListBox list)
    {
        list.Focus();
        if (list.SelectedItem is not { } selected) return;
        list.ScrollIntoView(selected);
        list.UpdateLayout();
        list.ContainerFromItem(selected)?.Focus();
    }
}
