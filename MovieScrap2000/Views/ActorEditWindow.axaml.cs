using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using MovieScrap2000;
using MovieScrap2000.TMDbScraper;
using SkiaSharp;
using VideoScrap2000.ViewModels;
using static VideoScrap2000.Localization.Locale;

namespace VideoScrap2000.Views;

public partial class ActorEditWindow : Window
{
    private readonly CancellationTokenSource lifetime = new();
    private readonly string? folder;
    private readonly ScraperSettings? scrapers;
    private readonly string language;
    private readonly ArtworkProfile profile;

    public ActorEditor Draft { get; }
    public bool IsAccepted { get; private set; }
    public ActorEditWindow() : this(new ActorEditor()) { }
    public ActorEditWindow(ActorEditor actor, string? folder = null, ScraperSettings? scrapers = null,
        string language = "de-DE", ArtworkProfile profile = ArtworkProfile.Original)
    {
        InitializeComponent();
        this.folder = folder;
        this.scrapers = scrapers;
        this.language = language;
        this.profile = profile;
        Draft = ActorEditor.From(actor.ToActor());
        Draft.PortraitPreviewData = actor.PortraitPreviewData;
        DataContext = Draft;
        ChoosePhotoButton.Content = T("Datei auswählen …");
        SearchPhotoButton.Content = T("TMDB-Suche …");
        SearchPhotoButton.IsVisible = scrapers?.EnableTmdbArtwork == true;
        RefreshPortrait();
        Closed += (_, _) => { lifetime.Cancel(); lifetime.Dispose(); };
    }

    private void RefreshPortrait()
    {
        var fallback = folder is null || string.IsNullOrWhiteSpace(Draft.Name)
            ? null : Path.Combine(folder, Actor.PortraitFolder, Draft.Name.Replace(' ', '_') + ".jpg");
        var path = Draft.Thumb;
        if (folder is not null && path.Length > 0 && !Uri.IsWellFormedUriString(path, UriKind.Absolute))
            path = Path.Combine(folder, path);
        if (path.Length == 0) path = fallback ?? "";
        Portrait.DataContext = path.Length > 0 || Draft.ThumbData is { Length: > 0 }
            ? new SeriesPicture(path, Draft.PortraitPreviewData ?? Draft.ThumbData, fallback) : null;
    }

    private async void ChoosePhoto(object? sender, RoutedEventArgs e)
    {
        await ChangePortraitAsync(async token =>
        {
            var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = T("Bilddatei auswählen …"),
                FileTypeFilter = [new(T("Bilddateien")) { Patterns = ["*.jpg", "*.jpeg", "*.png", "*.webp"], MimeTypes = ["image/jpeg", "image/png", "image/webp"] }]
            });
            if (files.Count == 0) return;
            await using var source = await files[0].OpenReadAsync();
            using var buffer = new MemoryStream();
            await source.CopyToAsync(buffer, token);
            await StagePortraitAsync(buffer.ToArray(), token);
        });
    }

    private async void SearchPhoto(object? sender, RoutedEventArgs e)
    {
        await ChangePortraitAsync(async token =>
        {
            var id = Draft.TmdbId.GetValueOrDefault();
            if (id <= 0)
            {
                var name = Draft.Name.Trim();
                if (name.Length == 0)
                {
                    PhotoStatus.Text = T("Gib zuerst den Namen des Darstellers ein.");
                    return;
                }
                using var lookup = new TMDbScraperManager(settings: scrapers);
                PhotoStatus.Text = T("Darsteller wird bei TMDB gesucht …");
                var people = await lookup.SearchPeopleAsync(name, language, token);
                token.ThrowIfCancellationRequested();
                if (people.Count == 0)
                {
                    PhotoStatus.Text = T("Bei TMDB wurde keine passende Person gefunden.");
                    return;
                }
                PhotoStatus.Text = "";
                id = people.Count == 1 ? people[0].Id : await ChoosePersonAsync(people);
                if (id <= 0) return;
            }
            PhotoStatus.Text = "";
            var gallery = new ArtworkWindow(id, scraper: new TMDbScraperManager(settings: scrapers), language: language, person: true);
            await gallery.ShowDialog(this);
            if (!gallery.IsAccepted || gallery.Result is not { } result) return;
            await StagePortraitAsync(result.Data, token);
            Draft.TmdbId = id;
        });
    }

    private Task<int> ChoosePersonAsync(IReadOnlyList<(int Id, string Name, string KnownFor)> people)
    {
        var dialog = new Window
        {
            Title = T("Darsteller auswählen"),
            Width = 500,
            Height = 380,
            MinWidth = 360,
            WindowStartupLocation = WindowStartupLocation.CenterOwner
        };
        var list = new ListBox();
        ScrollViewer.SetHorizontalScrollBarVisibility(list, Avalonia.Controls.Primitives.ScrollBarVisibility.Disabled);
        foreach (var person in people)
        {
            var row = new StackPanel { Spacing = 4, Margin = new(4, 6) };
            row.Children.Add(new TextBlock
            {
                Text = person.Name,
                FontWeight = Avalonia.Media.FontWeight.SemiBold,
                TextWrapping = Avalonia.Media.TextWrapping.Wrap
            });
            if (!string.IsNullOrWhiteSpace(person.KnownFor))
                row.Children.Add(new TextBlock
                {
                    Text = person.KnownFor,
                    TextWrapping = Avalonia.Media.TextWrapping.Wrap,
                    Classes = { "secondary" }
                });
            row.Children.Add(new TextBlock { Text = $"TMDB {person.Id}", FontSize = 11, Classes = { "secondary" } });
            list.Items.Add(new ListBoxItem
            {
                Content = row,
                HorizontalContentAlignment = Avalonia.Layout.HorizontalAlignment.Stretch
            });
        }
        var accept = new Button { Content = T("Übernehmen"), IsDefault = true, IsEnabled = false };
        var cancel = new Button { Content = T("Abbrechen"), IsCancel = true };
        accept.Click += (_, _) => { if (list.SelectedIndex >= 0) dialog.Close(people[list.SelectedIndex].Id); };
        cancel.Click += (_, _) => dialog.Close(0);
        list.SelectionChanged += (_, _) => accept.IsEnabled = list.SelectedIndex >= 0;
        var actions = new StackPanel
        {
            Orientation = Avalonia.Layout.Orientation.Horizontal,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right,
            Margin = new(0, 12, 0, 0),
            Spacing = 8,
            Children = { cancel, accept }
        };
        var layout = new DockPanel { Margin = new(16), Children = { actions, list } };
        DockPanel.SetDock(actions, Dock.Bottom);
        dialog.Content = layout;
        return dialog.ShowDialog<int>(this);
    }

    internal async Task StagePortraitAsync(byte[] bytes, CancellationToken token)
    {
        var prepared = await Task.Run(() =>
        {
            using var stream = new MemoryStream(bytes, writable: false);
            using var codec = SKCodec.Create(stream);
            if (codec?.EncodedFormat is not (SKEncodedImageFormat.Jpeg or SKEncodedImageFormat.Png or SKEncodedImageFormat.Webp))
                throw new InvalidDataException("Unterstützt werden PNG-, JPEG- und WebP-Bilder.");
            if ((long)codec.Info.Width * codec.Info.Height > 100_000_000)
                throw new InvalidDataException("Das Bild ist zu groß.");
            var optimized = ArtworkOptimizer.Optimize(bytes, "actor", profile);
            using var preview = ArtworkDecoder.DecodePreview(optimized.Bytes, 240);
            if (preview is null) throw new InvalidDataException("Das Bild konnte nicht gelesen werden.");
            return optimized.Bytes;
        }, token);
        token.ThrowIfCancellationRequested();
        Draft.ThumbData = bytes;
        Draft.PortraitPreviewData = prepared;
        Draft.HasPendingPortrait = true;
        RefreshPortrait();
        PhotoStatus.Text = T("Neues Darstellerbild übernommen.");
    }

    private async Task ChangePortraitAsync(Func<CancellationToken, Task> change)
    {
        var token = lifetime.Token;
        ChoosePhotoButton.IsEnabled = SearchPhotoButton.IsEnabled = SaveButton.IsEnabled = false;
        PhotoStatus.Text = "";
        try { await change(token); }
        catch (OperationCanceledException) when (token.IsCancellationRequested) { }
        catch (Exception ex)
        {
            if (!token.IsCancellationRequested)
                PhotoStatus.Text = T($"Darstellerbild konnte nicht geladen werden: {T(ex.Message)}");
        }
        finally
        {
            if (!token.IsCancellationRequested)
                ChoosePhotoButton.IsEnabled = SearchPhotoButton.IsEnabled = SaveButton.IsEnabled = true;
        }
    }

    private void Save(object? sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(Draft.Name)) return;
        IsAccepted = true; Close();
    }
    private void Cancel(object? sender, RoutedEventArgs e) => Close();
}
