using System.Text.Json;
namespace MovieScrap2000;

public sealed class AppSettings
{
    public string Theme { get; set; } = "System";
    public string LibraryPath { get; set; } = "";
    public string SeriesPath { get; set; } = "";
    public string Language { get; set; } = "System";
    public string MetadataLanguage { get; set; } = "System";
    public string ArtworkLanguage { get; set; } = "System";
    public bool UseFanartBackground { get; set; }
    public int ArtworkOptimizationProfile { get; set; }
    private static string FilePath => Path.Combine(Utils.GetOptionsPath(), "settings.json");
    public static AppSettings Load()
    {
        try { return JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(FilePath)) ?? new(); }
        catch (Exception ex) when (ex is IOException or JsonException or UnauthorizedAccessException) { return new(); }
    }
    public void Save()
    {
        var temporary = FilePath + ".tmp";
        File.WriteAllText(temporary, JsonSerializer.Serialize(this));
        File.Move(temporary, FilePath, true);
    }
}
