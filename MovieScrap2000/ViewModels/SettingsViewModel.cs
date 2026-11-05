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
    [ObservableProperty] private string libraryPath = "";
    [ObservableProperty] private string seriesPath = "";
    [ObservableProperty] private int themeIndex;
    [ObservableProperty] private int languageIndex;
    [ObservableProperty, NotifyPropertyChangedFor(nameof(MetadataLanguagePreview))] private int metadataLanguageIndex;
    [ObservableProperty, NotifyPropertyChangedFor(nameof(ArtworkLanguagePreview))] private int artworkLanguageIndex;
    [ObservableProperty] private bool useFanartBackground;
    [ObservableProperty] private string status = "";
    partial void OnStatusChanged(string value)
    {
        var translated = Localization.Locale.T(value);
        if (translated != value) Status = translated;
    }
}
