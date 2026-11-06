using System.Diagnostics;
namespace MovieScrap2000;
public static class Utils
{
    private static readonly object OptionsPathLock = new();
    public static string GetOptionsPath()
    {
        return GetOptionsPath(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData));
    }
    internal static string GetOptionsPath(string applicationData)
    {
        lock (OptionsPathLock)
        {
            var path = Path.Combine(applicationData, "VideoScrap2000");
            var legacyPath = Path.Combine(applicationData, "MovieScrap2000");
            // Move the complete collection, including demo data and SQLite sidecars.
            // An existing new folder always takes precedence; never overwrite its data.
            if (!Directory.Exists(path) && Directory.Exists(legacyPath))
                Directory.Move(legacyPath, path);
            Directory.CreateDirectory(path);
            return path;
        }
    }
    public static string GetTempPath() => Path.Combine(Path.GetTempPath(), "VideoScrap2000", "scrape_media");
    public static string GetNameFromFile(string? path) => string.IsNullOrWhiteSpace(path) ? "" : Path.GetFileNameWithoutExtension(path);
    public static string GetExcMsg(Exception ex) => ex.InnerException is null ? ex.Message : $"{ex.Message}\n{GetExcMsg(ex.InnerException)}";
    public static string CreateTempFileFromData(byte[]? data)
    {
        if (data is not { Length: > 0 }) return "";
        Directory.CreateDirectory(GetTempPath());
        var path = Path.Combine(GetTempPath(), Path.GetRandomFileName());
        File.WriteAllBytes(path, data);
        return path;
    }
    public static void OpenFileWithDefaultApp(string path) => Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
}
