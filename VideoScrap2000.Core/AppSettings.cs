using System.Text.Json;
namespace MovieScrap2000;

public sealed class AppSettings
{
    public string Theme { get; set; } = "System";
    public string LibraryPath { get; set; } = "";
    public string SeriesPath { get; set; } = "";
    // Null means an older settings file; an empty list intentionally has no sources.
    public List<string>? MovieFolders { get; set; }
    public List<string>? SeriesFolders { get; set; }
    public List<string> GetMovieFolders() => LibrarySources.Normalize(MovieFolders ?? [LibraryPath]);
    public List<string> GetSeriesFolders() => LibrarySources.Normalize(SeriesFolders ?? [SeriesPath]);
    public string Language { get; set; } = "System";
    public string MetadataLanguage { get; set; } = "System";
    public string ArtworkLanguage { get; set; } = "System";
    public bool UseFanartBackground { get; set; }
    public bool ScanOnStartup { get; set; }
    public int ArtworkOptimizationProfile { get; set; }
    private ScraperSettings scrapers = new();
    public ScraperSettings Scrapers { get => scrapers; set => scrapers = value ?? new(); }
    private static string FilePath => Path.Combine(Utils.GetOptionsPath(), "settings.json");
    public static AppSettings Load()
    {
        try { return JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(FilePath)) ?? new(); }
        catch (Exception ex) when (ex is IOException or JsonException or UnauthorizedAccessException) { return new(); }
    }
    public void Save()
    {
        MovieFolders = GetMovieFolders(); SeriesFolders = GetSeriesFolders();
        LibraryPath = MovieFolders.FirstOrDefault() ?? "";
        SeriesPath = SeriesFolders.FirstOrDefault() ?? "";
        var temporary = FilePath + ".tmp";
        File.WriteAllText(temporary, JsonSerializer.Serialize(this));
        if (!OperatingSystem.IsWindows()) File.SetUnixFileMode(temporary, UnixFileMode.UserRead | UnixFileMode.UserWrite);
        File.Move(temporary, FilePath, true);
    }
}
