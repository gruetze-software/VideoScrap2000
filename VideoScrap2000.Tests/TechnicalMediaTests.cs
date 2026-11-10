using System.Xml.Linq;
using Avalonia.Headless.XUnit;
using MovieScrap2000;
using VideoScrap2000.ViewModels;
using Xunit;

namespace VideoScrap2000.Tests;

public sealed class TechnicalMediaTests
{
    [AvaloniaFact]
    public async Task NewMovieReadsVideoAndAudioAndWritesKodiStreamDetailsOnSave()
    {
        var root = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        var video = Path.Combine(root, "Film.mp4");
        File.Copy(Path.Combine(AppContext.BaseDirectory, "Fixtures", "technical.mp4"), video);
        try
        {
            var database = new DatabaseManager(Path.Combine(root, "cache.sqlite"));
            await database.InitializeDatabaseAsync();
            using var metadata = new MetadataService(database);
            await metadata.SaveMovieAsync(new Movie { Title = "Film" }, root, video, false,
                TestContext.Current.CancellationToken, writeFiles: false);
            using var model = new MainViewModel(database, new AppSettings { Language = "en" });
            await model.InitializeAsync();
            for (var i = 0; !model.IsDetailReady && i < 300; i++)
                await Task.Delay(10, TestContext.Current.CancellationToken);
            Assert.True(model.IsDetailReady);
            for (var i = 0; !TechnicalMediaReader.HasVideoDetails(model.SelectedMovie?.FullMovieNFO?.FileInfo) && i < 500; i++)
                await Task.Delay(10, TestContext.Current.CancellationToken);
            var info = model.SelectedMovie!.FullMovieNFO!.FileInfo!;
            Assert.True(TechnicalMediaReader.HasVideoDetails(info), model.TechnicalDetails);
            Assert.Equal("h264", info.StreamDetails.VideoStream.Codec);
            Assert.Equal(160, info.StreamDetails.VideoStream.Width);
            Assert.Equal(90, info.StreamDetails.VideoStream.Height);
            Assert.Equal(1, info.StreamDetails.VideoStream.DurationInSeconds);
            var audio = Assert.Single(info.StreamDetails.AudioStreams);
            Assert.Equal("aac", audio.Codec);
            Assert.Equal(2, audio.Channels);
            Assert.Contains(audio.Language, new[] { "de", "deu", "ger" });
            Assert.True(audio.Bitrate > 1000, $"Audio bitrate in bits/s: {audio.Bitrate}");
            Assert.False(File.Exists(Path.ChangeExtension(video, ".nfo")));
            info.StreamDetails.VideoStream.Width = 9999;
            Assert.True(model.RefreshMediaInfoCommand.CanExecute(null));
            await model.RefreshMediaInfoCommand.ExecuteAsync(null);
            Assert.Equal(160, model.MediaInfo!.StreamDetails.VideoStream.Width);
            Assert.Equal("Film", model.Title);
            await model.SaveCommand.ExecuteAsync(null);
            var streamDetails = XDocument.Load(Path.ChangeExtension(video, ".nfo")).Root!.Element("fileinfo")!.Element("streamdetails")!;
            Assert.Equal("h264", (string?)streamDetails.Element("video")!.Element("codec"));
            Assert.Equal("2", (string?)streamDetails.Element("audio")!.Element("channels"));
            for (var i = 0; !model.IsDetailReady && i < 300; i++)
                await Task.Delay(10, TestContext.Current.CancellationToken);
        }
        finally
        {
            Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
            Directory.Delete(root, recursive: true);
        }
    }
}
