using System.Globalization;
namespace MovieScrap2000;

public static class LanguagePreferences
{
    public static string ClientLanguage { get; } = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "de" ? "de" : "en";
    public static string Resolve(string? preference, string? follow = null) => preference switch
    {
        "de" => "de-DE", "en" => "en-US",
        _ => follow switch { "de" => "de-DE", "en" => "en-US", _ => ClientLanguage == "de" ? "de-DE" : "en-US" }
    };
    public static string FromIndex(int value) => value switch { 1 => "de", 2 => "en", _ => "System" };
    public static int ToIndex(string? value) => value switch { "de" => 1, "en" => 2, _ => 0 };
}
