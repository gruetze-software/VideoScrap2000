using VideoScrap2000.Views;
using Xunit;

namespace VideoScrap2000.Tests;

public class DefaultFileLauncherTests
{
    [Fact]
    public void PortalLaunchKeepsSpecialCharactersInOneFileUriArgument()
    {
        var path = Path.Combine(Path.GetTempPath(), "Film & Serie #1", "Bild ü.png");
        var start = DefaultFileLauncher.CreatePortalStartInfo(path);
        Assert.Equal("gio", start.FileName);
        Assert.False(start.UseShellExecute);
        Assert.Equal(2, start.ArgumentList.Count);
        Assert.Equal("open", start.ArgumentList[0]);
        Assert.Equal(Path.GetFullPath(path), new Uri(start.ArgumentList[1]).LocalPath);
        Assert.Equal("1", start.Environment["GIO_USE_PORTALS"]);
        Assert.True(start.RedirectStandardError);
    }
}
