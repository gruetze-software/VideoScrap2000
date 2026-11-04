using Avalonia.Controls;
using Avalonia.Interactivity;
using VideoScrap2000.Localization;
using VideoScrap2000.ViewModels;

namespace VideoScrap2000.Views;

public partial class LibraryProgressWindow : Window
{
    public LibraryProgressWindow()
    {
        InitializeComponent();
        Title = Locale.T("Bitte warten");
        Closing += (_, e) =>
        {
            if (DataContext is not MainViewModel { IsBusy: true }) return;
            e.Cancel = true;
            RequestCancellation();
        };
    }

    private void Cancel(object? sender, RoutedEventArgs e) => RequestCancellation();

    private void RequestCancellation()
    {
        if (!CancelButton.IsEnabled || DataContext is not MainViewModel model) return;
        CancelButton.IsEnabled = false;
        model.Status = "Vorgang wird abgebrochen …";
        model.CancelCommand.Execute(null);
    }
}
