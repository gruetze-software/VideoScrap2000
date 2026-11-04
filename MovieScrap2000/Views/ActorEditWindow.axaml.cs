using Avalonia.Controls;
using Avalonia.Interactivity;
using VideoScrap2000.ViewModels;
namespace VideoScrap2000.Views;
public partial class ActorEditWindow : Window
{
    public ActorEditor Draft { get; }
    public bool IsAccepted { get; private set; }
    public ActorEditWindow() : this(new ActorEditor()) { }
    public ActorEditWindow(ActorEditor actor)
    {
        InitializeComponent(); Draft = ActorEditor.From(actor.ToActor()); DataContext = Draft;
    }
    private void Save(object? sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(Draft.Name)) return;
        IsAccepted = true; Close();
    }
    private void Cancel(object? sender, RoutedEventArgs e) => Close();
}
