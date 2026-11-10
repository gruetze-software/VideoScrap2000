using Avalonia.Headless.XUnit;
using MovieScrap2000;
using VideoScrap2000.ViewModels;
using VideoScrap2000.Views;
using Xunit;

namespace VideoScrap2000.Tests;

public sealed class MediaDetailsTests
{
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
