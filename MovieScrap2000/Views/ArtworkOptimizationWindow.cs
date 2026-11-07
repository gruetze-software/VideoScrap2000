using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using VideoScrap2000.ViewModels;
using static VideoScrap2000.Localization.Locale;

namespace VideoScrap2000.Views;

internal sealed class ArtworkOptimizationWindow : Window
{
    private readonly TextBox folder = new();
    private readonly ComboBox profile = new() { ItemsSource = new SettingsViewModel().OptimizationProfiles, HorizontalAlignment = HorizontalAlignment.Stretch };
    private readonly Button browse = new() { Content = T("Ordner auswählen") };
    private readonly Button analyze = new() { Content = T("Vorschau erstellen") };
    private readonly Button apply = new() { Content = T("Optimierte Bilder speichern"), IsEnabled = false };
    private readonly Button cancel = new() { Content = T("Schließen") };
    private readonly TextBlock status = new() { TextWrapping = TextWrapping.Wrap };
    private readonly ProgressBar progress = new() { Minimum = 0, Maximum = 100, Height = 6 };
    private readonly ListBox results = new();
    private readonly Image before = new() { Stretch = Stretch.Uniform, Height = 240 };
    private readonly Image after = new() { Stretch = Stretch.Uniform, Height = 240 };
    private CancellationTokenSource? operation;
    private CancellationTokenSource? previewOperation;
    private List<ArtworkOptimizationPlan> plans = [];
    private bool closeRequested;
    private ArtworkProfile plannedProfile;
    private int completed;

    internal ArtworkOptimizationWindow(string initialFolder, int initialProfile)
    {
        Title = T("Artwork-Optimierung"); Width = 980; Height = 690; MinWidth = 760; MinHeight = 560;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        folder.Text = initialFolder;
        profile.SelectedIndex = initialProfile == 0 ? 1 : initialProfile;
        var root = new Grid { Margin = new Thickness(24), RowDefinitions = new("Auto,Auto,Auto,*,Auto,Auto,Auto"), RowSpacing = 12 };
        Content = root;
        root.Children.Add(new TextBlock { Text = T("Vorschau prüfen und anschließend speichern. Originale werden in .artwork-originals gesichert. Die Kodi-Datenbank bleibt unverändert."), TextWrapping = TextWrapping.Wrap });
        var pathRow = new Grid { ColumnDefinitions = new("*,Auto"), ColumnSpacing = 8 };
        pathRow.Children.Add(folder); pathRow.Children.Add(browse); Grid.SetColumn(browse, 1); Add(root, pathRow, 1);
        var options = new Grid { ColumnDefinitions = new("*,Auto"), ColumnSpacing = 8 };
        options.Children.Add(profile); options.Children.Add(analyze); Grid.SetColumn(analyze, 1); Add(root, options, 2);
        var body = new Grid { ColumnDefinitions = new("*,*"), ColumnSpacing = 16 };
        results.ItemTemplate = new FuncDataTemplate<ArtworkOptimizationPlan>((item, _) => new TextBlock
        {
            Text = item is null ? "" : $"{Path.GetFileName(item.Path)}\n{item.Before / 1024:N0} KB → {item.After / 1024:N0} KB · {item.Width} × {item.Height}",
            TextWrapping = TextWrapping.Wrap, Margin = new Thickness(4, 8)
        });
        body.Children.Add(results);
        var pictures = new StackPanel { Spacing = 6 };
        pictures.Children.Add(new TextBlock { Text = T("Original") }); pictures.Children.Add(before);
        pictures.Children.Add(new TextBlock { Text = T("Optimiert") }); pictures.Children.Add(after);
        var scroll = new ScrollViewer { Content = pictures }; Grid.SetColumn(scroll, 1); body.Children.Add(scroll); Add(root, body, 3);
        Add(root, status, 4); Add(root, progress, 5);
        var buttons = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, HorizontalAlignment = HorizontalAlignment.Right };
        apply.Classes.Add("primary"); buttons.Children.Add(cancel); buttons.Children.Add(apply); Add(root, buttons, 6);
        browse.Click += async (_, _) =>
        {
            var selection = await StorageProvider.OpenFolderPickerAsync(new() { Title = T("Ordner auswählen"), AllowMultiple = false });
            if (selection.FirstOrDefault()?.TryGetLocalPath() is { } path) folder.Text = path;
        };
        folder.TextChanged += (_, _) => Reset();
        profile.SelectionChanged += (_, _) => Reset();
        analyze.Click += async (_, _) => await AnalyzeAsync();
        apply.Click += async (_, _) => await ApplyAsync();
        cancel.Click += (_, _) => { if (operation is not null) operation.Cancel(); else Close(); };
        results.SelectionChanged += async (_, _) => await PreviewAsync();
        Closing += (_, e) => { if (operation is not null) { closeRequested = true; operation.Cancel(); e.Cancel = true; } };
        Closed += (_, _) => { previewOperation?.Cancel(); ClearPictures(); };
    }

    private static void Add(Grid grid, Control item, int row) { Grid.SetRow(item, row); grid.Children.Add(item); }
    private void ClearPictures()
    {
        (before.Source as IDisposable)?.Dispose(); before.Source = null;
        (after.Source as IDisposable)?.Dispose(); after.Source = null;
    }
    private void Reset()
    {
        previewOperation?.Cancel(); plans = []; results.ItemsSource = null; apply.IsEnabled = false; ClearPictures(); status.Text = "";
    }
    private void Busy(bool value)
    {
        folder.IsEnabled = profile.IsEnabled = browse.IsEnabled = analyze.IsEnabled = !value;
        apply.IsEnabled = !value && plans.Count > 0;
        cancel.Content = T(value ? "Abbrechen" : "Schließen");
    }
    private async Task AnalyzeAsync()
    {
        Reset();
        if (!Directory.Exists(folder.Text)) { status.Text = T("Bitte einen vorhandenen Ordner auswählen."); return; }
        if (profile.SelectedIndex <= 0) { status.Text = T("Bitte Kompakt oder Ausgewogen auswählen."); return; }
        plannedProfile = (ArtworkProfile)profile.SelectedIndex;
        var path = folder.Text!;
        using var cts = new CancellationTokenSource(); operation = cts; Busy(true); progress.IsIndeterminate = true;
        var updates = new Progress<string>(file => status.Text = Path.GetFileName(file));
        try
        {
            plans = await Task.Run(() => ArtworkOptimizer.AnalyzeAsync(path, plannedProfile, file => ((IProgress<string>)updates).Report(file), cts.Token));
            results.ItemsSource = plans;
            var saving = plans.Sum(plan => plan.Before - plan.After) / 1024;
            status.Text = string.Format(T("{0} Bilder · Größenänderung: {1} KB. Originale benötigen zusätzlichen Speicherplatz."), plans.Count, saving);
            results.SelectedIndex = plans.Count > 0 ? 0 : -1;
        }
        catch (OperationCanceledException) { status.Text = T("Vorgang abgebrochen."); }
        catch (Exception ex) { status.Text = ex.Message; }
        finally { operation = null; Busy(false); progress.IsIndeterminate = false; if (closeRequested) Close(); }
    }
    private async Task ApplyAsync()
    {
        previewOperation?.Cancel();
        using var cts = new CancellationTokenSource(); operation = cts; completed = 0; Busy(true);
        var snapshot = plans.ToArray();
        var updates = new Progress<int>(count => { progress.Value = 100d * count / snapshot.Length; status.Text = $"{count} / {snapshot.Length}"; });
        try
        {
            await Task.Run(async () =>
            {
                foreach (var plan in snapshot)
                {
                    cts.Token.ThrowIfCancellationRequested();
                    await ArtworkOptimizer.ApplyAsync(plan, plannedProfile, cts.Token);
                    ArtworkPreviewCache.Shared.InvalidateFolder(Path.GetDirectoryName(plan.Path)!);
                    ((IProgress<int>)updates).Report(++completed);
                }
            });
            status.Text = string.Format(T("{0} Bilder optimiert. Originale sind gesichert. Artwork in Kodi bei Bedarf neu auswählen."), completed);
        }
        catch (OperationCanceledException) { status.Text = string.Format(T("Abgebrochen. {0} Bilder wurden bereits optimiert; Originale sind gesichert."), completed); }
        catch (Exception ex) { status.Text = $"{completed} / {plans.Count}: {ex.Message}"; }
        finally { plans = []; operation = null; Busy(false); apply.IsEnabled = false; if (closeRequested) Close(); }
    }
    private async Task PreviewAsync()
    {
        previewOperation?.Cancel();
        if (results.SelectedItem is not ArtworkOptimizationPlan plan) return;
        using var cts = new CancellationTokenSource(); previewOperation = cts;
        try
        {
            var bytes = await File.ReadAllBytesAsync(plan.Path, cts.Token);
            var converted = await Task.Run(() => ArtworkOptimizer.Optimize(bytes, plan.Kind, plannedProfile), cts.Token);
            cts.Token.ThrowIfCancellationRequested();
            ClearPictures();
            before.Source = ArtworkDecoder.DecodePreview(bytes, 400);
            after.Source = ArtworkDecoder.DecodePreview(converted.Bytes, 400);
        }
        catch (OperationCanceledException) { }
        catch (Exception ex) { status.Text = ex.Message; }
        finally { if (ReferenceEquals(previewOperation, cts)) previewOperation = null; }
    }
}
