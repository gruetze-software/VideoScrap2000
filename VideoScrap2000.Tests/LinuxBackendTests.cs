using VideoScrap2000;
using Xunit;

namespace VideoScrap2000.Tests;

public sealed class LinuxBackendTests
{
    [Theory]
    [InlineData(null, "Auto")]
    [InlineData("", "Auto")]
    [InlineData("auto", "Auto")]
    [InlineData("Wayland", "Wayland")]
    [InlineData(" x11 ", "X11")]
    [InlineData(" WAYLAND ", "Wayland")]
    public void BackendOverrideIsExplicitAndCaseInsensitive(string? input, string expected)
        => Assert.Equal(expected, LinuxDesktop.ParseBackend(input).ToString());

    [Fact]
    public void InvalidOverrideIsReportedInsteadOfSilentlySelectingX11()
        => Assert.Throws<ArgumentException>(() => LinuxDesktop.ParseBackend("invalid"));
}
