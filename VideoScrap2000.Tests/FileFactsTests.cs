using MovieScrap2000;
using Xunit;

namespace VideoScrap2000.Tests;
public sealed class FileFactsTests
{
    [Theory]
    [InlineData("movie.nfo")] [InlineData("movie.XML")] [InlineData("")]
    public void FileDateAndMissingMetadataReflectActualFolder(string metadataName)
    {
        var root = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            var video = Path.Combine(root, "Film.mkv"); File.WriteAllBytes(video, [0]);
            var date = new DateTime(2024, 4, 3, 12, 30, 0); File.SetLastWriteTime(video, date);
            if (metadataName.Length > 0) File.WriteAllText(Path.Combine(root, metadataName), "<movie/>");
            var facts = MovieFileFacts.Read(root, video);
            var movie = new MovieViewModel(); movie.UpdateFileFacts(facts);
            Assert.True(facts.IsAvailable);
            Assert.Equal(metadataName.Length > 0, facts.HasMetadata);
            Assert.Equal(metadataName.Length == 0, movie.MissingMetadata);
            Assert.Equal(metadataName.Length == 0 ? "Neu" : "", movie.MetadataStatus);
            Assert.Equal(metadataName.Length == 0, movie.HasMetadataStatus);
            Assert.Equal("03.04.2024 12:30", movie.FileDateText);
        }
        finally { Directory.Delete(root, true); }
    }
    [Fact]
    public void UnreachableFolderIsNotMarkedAsMissingMetadata()
    {
        var movie = new MovieViewModel();
        movie.UpdateFileFacts(MovieFileFacts.Read(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString()), "missing.mkv"));
        Assert.False(movie.MissingMetadata);
        Assert.Equal("Ordner nicht erreichbar", movie.MetadataStatus);
    }
}
