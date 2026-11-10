using CommunityToolkit.Mvvm.ComponentModel;
namespace VideoScrap2000.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    public bool IsDemo { get; init; }
    public string DemoTitle => Localization.Locale.T("Demo-Modus");
    public string DemoDescription => Localization.Locale.T("Erkunde die Anwendung mit erfundenen Filmen, Serien und eigenen Bildern. Die Demo verwendet einen separaten Speicherbereich. Deine Sammlung und Einstellungen bleiben unverändert.");
    public string DemoAction => Localization.Locale.T(IsDemo ? "Zur eigenen Sammlung" : "Demo-Modus öffnen");
    public string DetectedLanguage => Localization.Locale.T("Erkannte Client-Sprache: {0}").Replace("{0}",
        System.Globalization.CultureInfo.GetCultureInfo(MovieScrap2000.LanguagePreferences.ClientCulture).NativeName + " (" + MovieScrap2000.LanguagePreferences.ClientCulture + ")");
    public IReadOnlyList<string> ContentLanguageOptions { get; } = MovieScrap2000.LanguagePreferences.ContentLanguages
        .Select(code => code == "System" ? Localization.Locale.T("Client-Sprache automatisch erkennen")
            : System.Globalization.CultureInfo.GetCultureInfo(code).NativeName + " (" + code + ")").ToArray();
    public string InterfaceLanguageInfo => Localization.Locale.T("Die Oberfläche unterstützt Deutsch und Englisch. Andere Client-Sprachen verwenden Englisch für die Oberfläche; Metadaten und Artworks folgen weiterhin der Client-Sprache.");
    public string MetadataLanguageInfo => Localization.Locale.T("TMDB unterstützt viele ISO-639-1-Sprachen. Übersetzungen sind je nach Titel und Feld unterschiedlich vollständig. Ohne Übersetzung können Originaltexte oder leere Felder erscheinen. Texte werden nicht automatisch übersetzt.");
    public string ArtworkLanguageInfo => Localization.Locale.T("TMDB und Fanart.tv bieten Bilder in den vorhandenen Sprachen. Die gewünschte Sprache wird zuerst angezeigt, danach sprachneutrale und andere Bilder. Bildtexte werden nicht übersetzt; Episodenbilder sind meist sprachneutral.");
    public string MetadataLanguagePreview => Localization.Locale.T("Aktuelle Abfragesprache: {0}").Replace("{0}",
        MovieScrap2000.LanguagePreferences.ResolveContent(MovieScrap2000.LanguagePreferences.ContentFromIndex(MetadataLanguageIndex)));
    public string ArtworkLanguagePreview => Localization.Locale.T("Bevorzugte Bildsprache: {0}").Replace("{0}",
        MovieScrap2000.LanguagePreferences.ResolveContent(MovieScrap2000.LanguagePreferences.ContentFromIndex(ArtworkLanguageIndex)).Split('-')[0]);
    public System.Collections.ObjectModel.ObservableCollection<string> MovieFolders { get; } = [];
    public System.Collections.ObjectModel.ObservableCollection<string> SeriesFolders { get; } = [];
    public string LibraryPath { get => MovieFolders.FirstOrDefault() ?? ""; set { MovieFolders.Clear(); if (!string.IsNullOrWhiteSpace(value)) MovieFolders.Add(value); } }
    public string SeriesPath { get => SeriesFolders.FirstOrDefault() ?? ""; set { SeriesFolders.Clear(); if (!string.IsNullOrWhiteSpace(value)) SeriesFolders.Add(value); } }
    public string AddFolderText => Localization.Locale.T("Ordner hinzufügen …");
    public string MovieSourcesTitle => Localization.Locale.T("Filmquellen");
    public string SeriesSourcesTitle => Localization.Locale.T("Serienquellen");
    public string RemoveFolderText => Localization.Locale.T("Ausgewählte entfernen");
    public string SourcesInfo => Localization.Locale.T("Jede Quelle enthält die Unterordner deiner Filme oder Serien. Das Entfernen einer Quelle löscht keine Dateien.");
    [ObservableProperty] private int themeIndex;
    [ObservableProperty] private int languageIndex;
    [ObservableProperty, NotifyPropertyChangedFor(nameof(MetadataLanguagePreview))] private int metadataLanguageIndex;
    [ObservableProperty, NotifyPropertyChangedFor(nameof(ArtworkLanguagePreview))] private int artworkLanguageIndex;
    [ObservableProperty] private bool useFanartBackground;
    [ObservableProperty] private int artworkOptimizationProfile;
    public string OptimizationTitle => Localization.Locale.T("Artwork-Optimierung");
    public string OptimizationAction => Localization.Locale.T("Vorhandene Artworks optimieren …");
    public string OptimizationInfo => Localization.Locale.T("Optional für Android und schwächere Geräte. Wird beim Speichern neuer Artworks angewendet. Seitenverhältnis bleibt erhalten; kleine Bilder werden nicht vergrößert. Originale bleiben im Unterordner .artwork-originals erhalten. Transparente Bilder und Logos werden nicht verändert.") + " " + Localization.Locale.T("Darstellerbilder: Kompakt bis 300 × 450, Ausgewogen bis 500 × 750 Pixel.");
    public IReadOnlyList<string> OptimizationProfiles => new[] { "Original (unverändert)", "Kompakt · Poster 600 × 900, Fanart 1280 × 720", "Ausgewogen · Poster 1000 × 1500, Fanart 1920 × 1080" }.Select(Localization.Locale.T).ToArray();
    [ObservableProperty] private string status = "";
    [ObservableProperty] private bool isSelectingFolder;
    public MovieScrap2000.ScraperSettings Scrapers { get; set; } = new();
    public string ScrapersTitle => Localization.Locale.T("Scraper …");
    partial void OnStatusChanged(string value)
    {
        var translated = Localization.Locale.T(value);
        if (translated != value) Status = translated;
    }
}
