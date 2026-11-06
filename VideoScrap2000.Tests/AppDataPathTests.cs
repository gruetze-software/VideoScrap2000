using MovieScrap2000;
using Xunit;

namespace VideoScrap2000.Tests;

public sealed class AppDataPathTests : IDisposable
{
    private readonly string root = Path.Combine(Path.GetTempPath(), "VideoScrap2000-path-tests", Guid.NewGuid().ToString("N"));

    [Fact]
    public void NewInstallationCreatesCorrectlyNamedFolder()
    {
        var path = Utils.GetOptionsPath(root);
        Assert.Equal(Path.Combine(root, "VideoScrap2000"), path);
        Assert.True(Directory.Exists(path));
    }

    [Fact]
    public void LegacyInstallationMovesAllDataAndCanBeOpenedAgain()
    {
        var legacy = Path.Combine(root, "MovieScrap2000");
        Directory.CreateDirectory(Path.Combine(legacy, "Demo", "v1"));
        File.WriteAllText(Path.Combine(legacy, "settings.json"), "settings");
        File.WriteAllText(Path.Combine(legacy, "MovieScrap2000_Cache.sqlite"), "cache");
        File.WriteAllText(Path.Combine(legacy, "Demo", "v1", "poster.jpg"), "image");
        var path = Utils.GetOptionsPath(root);
        Assert.False(Directory.Exists(legacy));
        Assert.Equal("settings", File.ReadAllText(Path.Combine(path, "settings.json")));
        Assert.Equal("cache", File.ReadAllText(Path.Combine(path, "MovieScrap2000_Cache.sqlite")));
        Assert.Equal("image", File.ReadAllText(Path.Combine(path, "Demo", "v1", "poster.jpg")));
        Assert.Equal(path, Utils.GetOptionsPath(root));
    }

    [Fact]
    public void ExistingNewFolderIsNeverOverwrittenOrMerged()
    {
        var legacy = Path.Combine(root, "MovieScrap2000");
        var current = Path.Combine(root, "VideoScrap2000");
        Directory.CreateDirectory(legacy);
        Directory.CreateDirectory(current);
        File.WriteAllText(Path.Combine(legacy, "settings.json"), "old");
        File.WriteAllText(Path.Combine(current, "settings.json"), "new");
        Assert.Equal(current, Utils.GetOptionsPath(root));
        Assert.Equal("new", File.ReadAllText(Path.Combine(current, "settings.json")));
        Assert.Equal("old", File.ReadAllText(Path.Combine(legacy, "settings.json")));
    }

    public void Dispose()
    {
        if (Directory.Exists(root)) Directory.Delete(root, true);
    }
}
