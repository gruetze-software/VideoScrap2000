using System.Globalization;
namespace MovieScrap2000;

public static class LanguagePreferences
{
    // Capture the actual client culture before the UI changes CurrentUICulture.
    public static string ClientCulture { get; } = string.IsNullOrWhiteSpace(CultureInfo.CurrentUICulture.Name) ? "en-US" : CultureInfo.CurrentUICulture.Name;
    public static string ClientLanguage { get; } = CultureInfo.GetCultureInfo(ClientCulture).TwoLetterISOLanguageName == "de" ? "de" : "en";
    public static IReadOnlyList<string> ContentLanguages { get; } = new[] { "System", "de", "en" }
        .Concat(CultureInfo.GetCultures(CultureTypes.NeutralCultures)
            .Where(culture => culture.Name.Length > 0 && culture.TwoLetterISOLanguageName.Length == 2)
            .Select(culture => culture.TwoLetterISOLanguageName.ToLowerInvariant())
            .Where(code => code is not "de" and not "en").Distinct().OrderBy(code => CultureInfo.GetCultureInfo(code).EnglishName, StringComparer.Ordinal))
        .Concat(new[] { "pt-BR", "pt-PT", "zh-CN", "zh-TW", "en-GB", "fr-CA" }).ToArray();
    public static string Resolve(string? preference, string? follow = null) => preference switch
    {
        "de" => "de-DE", "en" => "en-US",
        _ => follow switch { "de" => "de-DE", "en" => "en-US", _ => ClientLanguage == "de" ? "de-DE" : "en-US" }
    };
    public static string FromIndex(int value) => value switch { 1 => "de", 2 => "en", _ => "System" };
    public static int ToIndex(string? value) => value switch { "de" => 1, "en" => 2, _ => 0 };

    public static string ResolveContent(string? preference, string? clientCulture = null)
    {
        var code = string.IsNullOrWhiteSpace(preference) || preference == "System" ? clientCulture ?? ClientCulture : preference;
        try
        {
            var culture = CultureInfo.GetCultureInfo(code);
            if (culture.TwoLetterISOLanguageName.Length != 2 || !ContentLanguages.Contains(culture.TwoLetterISOLanguageName)) return "en-US";
            var specific = culture.IsNeutralCulture ? CultureInfo.CreateSpecificCulture(culture.Name) : culture;
            var region = new RegionInfo(specific.Name).TwoLetterISORegionName;
            if (region.Length != 2) region = new RegionInfo(CultureInfo.CreateSpecificCulture(culture.TwoLetterISOLanguageName).Name).TwoLetterISORegionName;
            return culture.TwoLetterISOLanguageName + "-" + region;
        }
        catch (ArgumentException) { return "en-US"; }
    }

    public static string ContentFromIndex(int index) => index >= 0 && index < ContentLanguages.Count ? ContentLanguages[index] : "System";
    public static int ContentToIndex(string? code)
    {
        var exact = ContentLanguages.ToList().FindIndex(value => string.Equals(value, code, StringComparison.OrdinalIgnoreCase));
        if (exact >= 0) return exact;
        var language = code?.Split('-')[0];
        return Math.Max(0, ContentLanguages.ToList().FindIndex(value => value == language));
    }

    public static int ImagePriority(string? language, string preferred) =>
        string.Equals(language?.Split('-')[0], preferred.Split('-')[0], StringComparison.OrdinalIgnoreCase) ? 0
        : string.IsNullOrWhiteSpace(language) || language is "00" or "null" ? 1 : 2;
}
