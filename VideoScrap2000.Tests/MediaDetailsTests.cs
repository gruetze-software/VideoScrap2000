using Avalonia.Headless.XUnit;
using MovieScrap2000;
using VideoScrap2000.ViewModels;
using VideoScrap2000.Views;
using Xunit;

namespace VideoScrap2000.Tests;

public sealed class MediaDetailsTests
{
    [AvaloniaFact]
    public void ActionGlyphContoursHaveMatchingTopBottomAndHorizontalCenter()
    {
        foreach (var path in new[] { "M3,1 L13,7 L3,13 Z", "M1,3 L6,3 L8,5 L15,5 L15,13 L1,13 Z" })
        {
            var glyph = new MediaActionGlyph(path) { Width = 20, Height = 14 };
            glyph.Measure(new Avalonia.Size(20, 14));
            glyph.Arrange(new Avalonia.Rect(0, 0, 20, 14));
            using var rendered = new Avalonia.Media.Imaging.RenderTargetBitmap(new Avalonia.PixelSize(20, 14), new Avalonia.Vector(96, 96));
            rendered.Render(glyph);
            using var png = new MemoryStream(); rendered.Save(png, Avalonia.Media.Imaging.PngBitmapEncoderOptions.Default); png.Position = 0;
            using var pixels = SkiaSharp.SKBitmap.Decode(png);
            var visible = Enumerable.Range(0, 14).SelectMany(y => Enumerable.Range(0, 20).Select(x => (x, y)))
                .Where(point => pixels.GetPixel(point.x, point.y).Alpha > 32).ToArray();
            Assert.NotEmpty(visible);
            Assert.Equal(0, visible.Min(point => point.y));
            Assert.Equal(13, visible.Max(point => point.y));
            Assert.InRange((visible.Min(point => point.x) + visible.Max(point => point.x)) / 2d, 9, 10);
        }
    }

    [Theory]
    [InlineData("deu", "de")]
    [InlineData("ger", "de")]
    [InlineData("de-CH", "ch")]
    [InlineData("eng", "gb")]
    [InlineData("en-US", "us")]
    [InlineData("fre", "fr")]
    [InlineData("und", null)]
    [InlineData("not-a-language", null)]
    public void TrackFlagsHandleIsoCodesAliasesAndUnknownLanguages(string language, string? country) =>
        Assert.Equal(country, MediaIconAssets.Country(language));

    [AvaloniaFact]
    public void MediaHeaderUsesVectorIconsAndOneFlagPerAudioTrack()
    {
        var control = new MediaBadgesView { Info = new() { StreamDetails = new() { VideoStream = new() { Width = 1920, Height = 1080, Codec = "h264", Aspect = 1.78m }, AudioStreams = [new() { Language = "ger", Codec = "eac3", Channels = 6 }, new() { Language = "eng", Codec = "aac", Channels = 2 }] } } };
        var viewbox = Assert.IsType<Avalonia.Controls.Viewbox>(control.Content);
        var panel = Assert.IsType<Avalonia.Controls.StackPanel>(viewbox.Child);
        Assert.Equal(Avalonia.Layout.Orientation.Horizontal, panel.Orientation);
        Assert.Equal(22, panel.Height);
        Assert.Equal(5, panel.Children.OfType<Avalonia.Controls.Shapes.Path>().Count());
        Assert.Equal(2, panel.Children.OfType<Avalonia.Controls.Image>().Count());
        Assert.Empty(panel.Children.OfType<Avalonia.Controls.TextBlock>());
        Assert.Equal(2, panel.Children.OfType<Avalonia.Controls.Button>().Count());
    }

    [AvaloniaFact]
    public async Task ImageResolutionComesFromSourceRatherThanDecodedPreview()
    {
        using var bitmap = new SkiaSharp.SKBitmap(1200, 800);
        bitmap.Erase(SkiaSharp.SKColors.Blue);
        using var image = SkiaSharp.SKImage.FromBitmap(bitmap);
        using var encoded = image.Encode(SkiaSharp.SKEncodedImageFormat.Jpeg, 90);
        using var preview = await ArtworkPreviewCache.Shared.AcquireAsync("resolution-test", 88, encoded.ToArray(), TestContext.Current.CancellationToken);
        Assert.NotNull(preview);
        Assert.Equal(88, preview.Image.PixelSize.Width);
        Assert.Equal("1200 × 800", preview.Resolution);
    }

    [Fact]
    public void BadgesDescribeRealValuesAndHideUnknownValues()
    {
        Assert.Empty(MediaPresentation.Badges(null));
        Assert.Empty(MediaPresentation.Badges(new()));
        var info = new MovieFileInfo { StreamDetails = new() { VideoStream = new() { Width = 3840, Height = 1600, Aspect = 2.4m, Codec = "hevc" }, AudioStreams = [new() { Codec = "dts", Channels = 8 }] } };
        Assert.Equal(new[] { "4K", "2.4:1", "H.265", "DTS", "7.1" }, MediaPresentation.Badges(info).Select(badge => badge.Label));
        Assert.All(MediaPresentation.Badges(info), badge => Assert.False(string.IsNullOrWhiteSpace(badge.Description)));
    }

    [AvaloniaFact]
    public async Task EpisodeReadAgainPersistsKodiStreamDetailsWithoutChangingMetadata()
    {
        var root = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        var folder = Path.Combine(root, "Series"); Directory.CreateDirectory(folder);
        var video = Path.Combine(folder, "Series.S01E01.mp4");
        File.Copy(Path.Combine(AppContext.BaseDirectory, "Fixtures", "technical.mp4"), video);
        File.WriteAllText(Path.Combine(folder, "tvshow.nfo"), "<tvshow><title>Series</title></tvshow>");
        File.WriteAllText(Path.ChangeExtension(video, ".nfo"), "<episodedetails><title>Pilot</title><season>1</season><episode>1</episode><plot>Keep this description</plot></episodedetails>");
        try
        {
            using var main = new MainViewModel(settings: new AppSettings { SeriesPath = root });
            await main.Series.ScanAsync();
            await main.Series.RefreshEpisodeMediaCommand.ExecuteAsync(null);
            Assert.True(TechnicalMediaReader.HasVideoDetails(main.Series.EpisodeEditor!.MediaInfo));
            Assert.Equal("Keep this description", main.Series.EpisodeEditor.Plot);
            await main.Series.SaveEpisodeCommand.ExecuteAsync(null);
            var metadata = System.Xml.Linq.XDocument.Load(Path.ChangeExtension(video, ".nfo")).Root!;
            var reopened = new SeriesNfoEditor(metadata, true);
            Assert.Equal(160, reopened.MediaInfo!.StreamDetails.VideoStream.Width);
            Assert.Equal("Pilot", reopened.Title);
            Assert.Equal("Keep this description", reopened.Plot);
        }
        finally { Directory.Delete(root, true); }
    }
}
