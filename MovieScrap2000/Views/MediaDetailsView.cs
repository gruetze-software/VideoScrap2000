using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using MovieScrap2000;
using System.Globalization;
using System.Windows.Input;
using static VideoScrap2000.Localization.Locale;

namespace VideoScrap2000.Views;

public sealed record MediaBadge(string Label, string Description, string Icon = "chip");
public static class MediaPresentation
{
    public static IReadOnlyList<MediaBadge> Badges(MovieFileInfo? info)
    {
        var badges = new List<MediaBadge>();
        if (info?.StreamDetails is not { } streams) return badges;
        var video = streams.VideoStream;
        if (video is { Width: > 0, Height: > 0 })
        {
            var resolution = video.Width >= 7000 || video.Height >= 4000 ? "8K" : video.Width >= 3800 || video.Height >= 2100 ? "4K" : video.Width >= 1900 || video.Height >= 1000 ? "1080" : video.Width >= 1200 || video.Height >= 700 ? "720" : "SD";
            badges.Add(new(resolution, $"{T("Auflösung")}: {video.Width} × {video.Height}", resolution == "4K" ? "video-4k-box" : resolution == "SD" ? "standard-definition" : resolution == "8K" ? "filmstrip" : "high-definition"));
        }
        if (video?.Aspect > 0) badges.Add(new(video.Aspect.ToString("0.##", CultureInfo.InvariantCulture) + ":1", T("Bildformat"), "aspect-ratio"));
        if (!string.IsNullOrWhiteSpace(video?.Codec)) badges.Add(new(Codec(video.Codec), T("Video-Codec"), "chip"));
        var audio = streams.AudioStreams.FirstOrDefault(stream => !string.IsNullOrWhiteSpace(stream.Codec));
        if (audio is not null) badges.Add(new(Codec(audio.Codec!), T("Audio-Codec"), audio.Codec?.ToLowerInvariant() is "ac3" or "eac3" or "truehd" ? "dolby" : "surround-sound"));
        if (audio?.Channels > 0) badges.Add(new(audio.Channels switch { 1 => "1.0", 2 => "2.0", 6 => "5.1", 8 => "7.1", _ => audio.Channels.ToString(CultureInfo.InvariantCulture) + "ch" }, $"{T("Kanäle")}: {audio.Channels}", audio.Channels switch { 2 => "surround-sound-2-0", 6 => "surround-sound-5-1", 8 => "surround-sound-7-1", _ => "surround-sound" }));
        return badges;
    }
    public static string Codec(string value) => value.ToLowerInvariant() switch { "h264" or "avc" => "H.264", "hevc" or "h265" => "H.265", "ac3" => "AC-3", "eac3" => "E-AC-3", "truehd" => "TrueHD", _ => value.ToUpperInvariant() };
}

public sealed class MediaBadgesView : UserControl
{
    public static readonly StyledProperty<MovieFileInfo?> InfoProperty = AvaloniaProperty.Register<MediaBadgesView, MovieFileInfo?>(nameof(Info));
    public static readonly StyledProperty<string?> FilePathProperty = AvaloniaProperty.Register<MediaBadgesView, string?>(nameof(FilePath));
    public MovieFileInfo? Info { get => GetValue(InfoProperty); set => SetValue(InfoProperty, value); }
    public string? FilePath { get => GetValue(FilePathProperty); set => SetValue(FilePathProperty, value); }
    public MediaBadgesView() => Refresh();
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == InfoProperty || change.Property == FilePathProperty) Refresh();
    }
    private void Refresh()
    {
        var panel = new WrapPanel { HorizontalAlignment = HorizontalAlignment.Right, VerticalAlignment = VerticalAlignment.Center };
        foreach (var badge in MediaPresentation.Badges(Info))
        {
            var icon = MediaIconAssets.Icon(badge.Icon, this);
            icon.Margin = new Thickness(0, 0, 7, 4);
            ToolTip.SetTip(icon, badge.Description + ": " + badge.Label);
            Avalonia.Automation.AutomationProperties.SetName(icon, badge.Description + ": " + badge.Label);
            panel.Children.Add(icon);
        }
        if (Info?.StreamDetails is { } streams)
            foreach (var (audio, index) in streams.AudioStreams.Select((audio, index) => (audio, index)))
            {
                var flag = MediaIconAssets.Flag(MediaIconAssets.Country(audio.Language) ?? MediaIconAssets.Country(audio.LongLanguage), this);
                flag.Margin = new Thickness(0, 3, 6, 4);
                var description = $"{T("Spur")} {index + 1}: {audio.LongLanguage ?? audio.Language ?? T("Unbekannt")} · {MediaPresentation.Codec(audio.Codec ?? "")} · {audio.Channels} {T("Kanäle")}";
                ToolTip.SetTip(flag, description); Avalonia.Automation.AutomationProperties.SetName(flag, description); panel.Children.Add(flag);
            }
        AddAction(panel, "M3,1 L13,7 L3,13 Z", "Abspielen", false);
        AddAction(panel, "M1,3 L6,3 L8,5 L15,5 L15,13 L1,13 Z", "Ordner öffnen", true);
        Content = panel;
    }
    private void AddAction(Panel panel, string geometry, string label, bool folder)
    {
        var button = new Button { Content = new Avalonia.Controls.Shapes.Path { Data = Geometry.Parse(geometry), Width = 15, Height = 15, Stretch = Stretch.Uniform, Fill = Brushes.Teal }, Padding = new Thickness(6), Margin = new Thickness(0, 0, 5, 4), IsEnabled = !string.IsNullOrWhiteSpace(FilePath) };
        Avalonia.Automation.AutomationProperties.SetName(button, T(label)); ToolTip.SetTip(button, T(label));
        button.Click += async (_, _) =>
        {
            if (string.IsNullOrWhiteSpace(FilePath) || TopLevel.GetTopLevel(this) is not { } owner) return;
            try
            {
                if (folder)
                {
                    if (await owner.StorageProvider.TryGetFolderFromPathAsync(System.IO.Path.GetDirectoryName(FilePath)!) is { } directory) await DefaultFileLauncher.LaunchAsync(owner, directory);
                }
                else if (await owner.StorageProvider.TryGetFileFromPathAsync(FilePath) is { } file) await DefaultFileLauncher.LaunchAsync(owner, file);
            }
            catch (Exception ex) { ToolTip.SetTip(button, T("Die Datei oder der Ordner konnte nicht geöffnet werden.")); System.Diagnostics.Trace.WriteLine(ex); }
        };
        panel.Children.Add(button);
    }
}

public sealed class MediaDetailsView : UserControl
{
    public static readonly StyledProperty<MovieFileInfo?> InfoProperty = AvaloniaProperty.Register<MediaDetailsView, MovieFileInfo?>(nameof(Info));
    public static readonly StyledProperty<ICommand?> ReadCommandProperty = AvaloniaProperty.Register<MediaDetailsView, ICommand?>(nameof(ReadCommand));
    public MovieFileInfo? Info { get => GetValue(InfoProperty); set => SetValue(InfoProperty, value); }
    public ICommand? ReadCommand { get => GetValue(ReadCommandProperty); set => SetValue(ReadCommandProperty, value); }
    public MediaDetailsView() => Refresh();
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == InfoProperty || change.Property == ReadCommandProperty) Refresh();
    }
    private void Refresh()
    {
        var panel = new StackPanel { Spacing = 7, Margin = new Thickness(0, 12, 0, 8) };
        void Heading(string title) => panel.Children.Add(new TextBlock { Text = T(title), FontWeight = FontWeight.SemiBold, FontSize = 15, Margin = new Thickness(0, 8, 0, 3) });
        void Row(string title, string? value)
        {
            if (string.IsNullOrWhiteSpace(value)) return;
            var row = new Grid { ColumnDefinitions = new ColumnDefinitions("145,*") };
            row.Children.Add(new TextBlock { Text = T(title), Opacity = .75 });
            var text = new TextBlock { Text = value, TextWrapping = TextWrapping.Wrap }; Grid.SetColumn(text, 1); row.Children.Add(text); panel.Children.Add(row);
        }
        Heading("Mediendetails");
        if (Info?.StreamDetails is { } streams && TechnicalMediaReader.HasVideoDetails(Info))
        {
            var video = streams.VideoStream;
            Heading("Video"); Row("Codec", MediaPresentation.Codec(video.Codec!)); Row("Auflösung", $"{video.Width} × {video.Height}");
            Row("Bildformat", video.Aspect > 0 ? video.Aspect.ToString("0.###", CultureInfo.InvariantCulture) + ":1" : null);
            Row("Scan", video.ScanType); Row("Spieldauer", video.DurationInSeconds > 0 ? TimeSpan.FromSeconds(video.DurationInSeconds).ToString(@"hh\:mm\:ss") : null);
            Row("Dateigröße", video.FileSize > 0 ? $"{video.FileSize / 1048576d:N1} MiB" : null);
            Heading("Audio");
            foreach (var (audio, index) in streams.AudioStreams.Select((audio, index) => (audio, index)))
                Row(T("Spur") + " " + (index + 1), $"{audio.LongLanguage ?? audio.Language} · {MediaPresentation.Codec(audio.Codec ?? "")} · {audio.Channels} {T("Kanäle")} · {audio.Bitrate / 1000:N0} kbit/s");
            Heading("Untertitel");
            foreach (var subtitle in streams.Subtitles) Row("Sprache", subtitle.Language ?? T("Unbekannt"));
            if (streams.Subtitles.Count == 0) Row("Untertitel", "0");
        }
        else Row("Video", T("Keine technischen Mediendaten in der NFO vorhanden."));
        var refresh = new Button { Content = "↻ " + T("Neu einlesen"), Command = ReadCommand, HorizontalAlignment = HorizontalAlignment.Left, Margin = new Thickness(0, 8, 0, 0) };
        ToolTip.SetTip(refresh, T("Liest die technischen Mediendaten erneut aus der Videodatei. Online-Metadaten bleiben erhalten."));
        panel.Children.Add(refresh); Content = panel;
    }
}
