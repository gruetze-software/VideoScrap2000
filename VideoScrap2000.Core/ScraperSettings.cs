namespace MovieScrap2000;

public sealed class ScraperSettings
{
    public string SeriesProvider { get; set; } = "TMDB";
    public string TmdbReadToken { get; set; } = "";
    public string FanartPersonalKey { get; set; } = "";
    [System.Text.Json.Serialization.JsonIgnore]
    public string TvdbApiKey { get; set; } = ""; // In-memory override for development/tests; never saved in user settings.
    public string TvdbPin { get; set; } = "";
    public bool EnableTvdb { get; set; }
    public bool EnableTmdbArtwork { get; set; } = true;
    public bool EnableFanart { get; set; } = true;
    public bool EnableTvmaze { get; set; } = true;
    public ScraperSettings Copy() => (ScraperSettings)MemberwiseClone();
    public void Normalize()
    {
        TmdbReadToken = (TmdbReadToken ?? "").Trim(); FanartPersonalKey = (FanartPersonalKey ?? "").Trim();
        TvdbApiKey = (TvdbApiKey ?? "").Trim(); TvdbPin = (TvdbPin ?? "").Trim();
        if (!EnableTvdb || SeriesProvider != "TheTVDB") SeriesProvider = "TMDB";
        if (EnableTvdb && string.IsNullOrWhiteSpace(ProjectApiKeys.Tvdb(TvdbApiKey))) throw new InvalidOperationException("Der TheTVDB-Projektzugang fehlt in diesem Build. Bitte eine offizielle Version verwenden.");
        if (new[] { TmdbReadToken, FanartPersonalKey, TvdbApiKey, TvdbPin }.Any(value => value.Any(char.IsControl)))
            throw new InvalidOperationException("API-Zugangsdaten dürfen keine Zeilenumbrüche enthalten.");
    }
}
