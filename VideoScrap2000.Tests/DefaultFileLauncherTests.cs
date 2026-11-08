using VideoScrap2000.Views;
using Xunit;

namespace VideoScrap2000.Tests;

public class DefaultFileLauncherTests
{
    [Theory]
    [InlineData(true, false, "/snap/videoscrap2000/1", true)]
    [InlineData(true, true, null, true)]
    [InlineData(true, false, null, false)]
    [InlineData(false, false, "/snap/videoscrap2000/1", false)]
    public void SandboxedLinuxExportsFilesThroughPortal(bool linux, bool flatpak, string? snap, bool expected)
    {
        Assert.Equal(expected, DefaultFileLauncher.RequiresDesktopPortal(linux, flatpak, snap));
    }

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
