namespace MovieScrap2000;

public sealed class ScraperSettings
{
    public string SeriesProvider { get; set; } = "TMDB";
    public string TmdbReadToken { get; set; } = "";
    public string FanartPersonalKey { get; set; } = "";
    public string TvdbApiKey { get; set; } = "";
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
        if (EnableTvdb && TvdbApiKey.Length == 0) throw new InvalidOperationException("Bitte einen TheTVDB-Projekt-API-Key eingeben oder TheTVDB deaktivieren.");
        if (new[] { TmdbReadToken, FanartPersonalKey, TvdbApiKey, TvdbPin }.Any(value => value.Any(char.IsControl)))
            throw new InvalidOperationException("API-Zugangsdaten dürfen keine Zeilenumbrüche enthalten.");
    }
}
