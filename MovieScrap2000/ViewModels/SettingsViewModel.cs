using CommunityToolkit.Mvvm.ComponentModel;
namespace VideoScrap2000.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    public bool IsDemo { get; init; }
    public string DemoTitle => Localization.Locale.T("Demo für Screenshots");
    public string DemoDescription => Localization.Locale.T("Erfundene Filme, Serien und eigene Bilder. Die Demo verwendet einen separaten Speicherbereich und startet auf Englisch. Deine Sammlung und Einstellungen bleiben unverändert.");
    public string DemoAction => Localization.Locale.T(IsDemo ? "Zur eigenen Sammlung" : "Demo-Modus öffnen");
    public string DetectedLanguage => Localization.Locale.T("Erkannte Client-Sprache: {0}").Replace("{0}", MovieScrap2000.LanguagePreferences.ClientLanguage == "de" ? "Deutsch" : "English");
    [ObservableProperty] private string libraryPath = "";
    [ObservableProperty] private string seriesPath = "";
    [ObservableProperty] private int themeIndex;
    [ObservableProperty] private int languageIndex;
    [ObservableProperty] private int metadataLanguageIndex;
    [ObservableProperty] private int artworkLanguageIndex;
    [ObservableProperty] private bool useFanartBackground;
    [ObservableProperty] private string status = "";
    partial void OnStatusChanged(string value)
    {
        var translated = Localization.Locale.T(value);
        if (translated != value) Status = translated;
    }
}
