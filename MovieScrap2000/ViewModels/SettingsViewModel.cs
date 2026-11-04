using CommunityToolkit.Mvvm.ComponentModel;
namespace VideoScrap2000.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
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
