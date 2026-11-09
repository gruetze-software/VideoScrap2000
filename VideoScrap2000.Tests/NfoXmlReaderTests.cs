using System.Text;
using System.Xml;
using MovieScrap2000;
using Xunit;

namespace VideoScrap2000.Tests;

public sealed class NfoXmlReaderTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void LegacyDeclarationDoesNotThrowFirstChanceXmlExceptions(bool bom)
    {
        var folder = Path.Combine(Path.GetTempPath(), "VideoScrap2000-nfo", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        var path = Path.Combine(folder, "movie.nfo");
        var created = new DateTime(2025, 2, 3, 4, 5, 6, DateTimeKind.Utc);
        File.WriteAllText(path, $"<?xml version=\"1.0\" encoding=\"utf-16\"?><movie videoscrap2000-created-utc=\"{created:O}\"><title>Grüße 日本</title></movie>", new UTF8Encoding(bom));
        var thread = Environment.CurrentManagedThreadId;
        var exceptions = 0;
        EventHandler<System.Runtime.ExceptionServices.FirstChanceExceptionEventArgs> handler = (_, e) =>
        {
            if (Environment.CurrentManagedThreadId == thread && e.Exception is XmlException) exceptions++;
        };
        AppDomain.CurrentDomain.FirstChanceException += handler;
        try
        {
            Assert.Equal(created, MovieFileFacts.Read(folder, Path.Combine(folder, "movie.mkv")).NfoCreatedAtUtc);
            Assert.Equal("Grüße 日本", MovieManager.LoadExistingNfoDocument(path).Root!.Element("title")!.Value);
            Assert.Equal(0, exceptions);
        }
        finally
        {
            AppDomain.CurrentDomain.FirstChanceException -= handler;
            Directory.Delete(folder, true);
        }
    }

    [Theory]
    [InlineData("utf-8")]
    [InlineData("utf-16")]
    [InlineData("utf-16BE")]
    [InlineData("utf-32")]
    [InlineData("iso-8859-1")]
    public void RealDeclaredEncodingsRemainReadable(string name)
    {
        var path = Path.GetTempFileName();
        try
        {
            File.WriteAllText(path, $"<?xml version=\"1.0\" encoding=\"{name}\"?><movie><title>Grüße</title></movie>", Encoding.GetEncoding(name));
            Assert.Equal("Grüße", MovieManager.LoadExistingNfoDocument(path).Root!.Element("title")!.Value);
        }
        finally { File.Delete(path); }
    }
}
