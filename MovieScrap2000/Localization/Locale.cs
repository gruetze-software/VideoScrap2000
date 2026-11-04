using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Avalonia;
using Avalonia.Data.Converters;
using MovieScrap2000;

namespace VideoScrap2000.Localization;

public static class Locale
{
    private static readonly Dictionary<string, string> English = ReadTranslations();
    private static readonly List<(Regex Pattern, string Target, bool English)> Patterns = CreatePatterns();
    public static bool IsEnglish { get; private set; } = LanguagePreferences.ClientLanguage != "de";
    public static string Key(string text) => "L" + Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(text)))[..12];
    private static Dictionary<string, string> ReadTranslations()
    {
        using var stream = typeof(Locale).Assembly.GetManifestResourceStream("VideoScrap2000.Localization.en.json")!;
        return JsonSerializer.Deserialize<Dictionary<string, string>>(stream)!;
    }
    public static void Apply(string preference)
    {
        var culture = CultureInfo.GetCultureInfo(LanguagePreferences.Resolve(preference));
        IsEnglish = culture.TwoLetterISOLanguageName == "en";
        CultureInfo.CurrentUICulture = culture; CultureInfo.CurrentCulture = culture;
        CultureInfo.DefaultThreadCurrentUICulture = culture; CultureInfo.DefaultThreadCurrentCulture = culture;
        if (Application.Current is { } app)
            foreach (var pair in English) app.Resources[Key(pair.Key)] = IsEnglish ? pair.Value : pair.Key;
    }
    public static string T(string? text)
    {
        if (text is null) return "";
        if (English.TryGetValue(text, out var translated)) return IsEnglish ? translated : text;
        var reverse = English.FirstOrDefault(p => p.Value == text);
        if (reverse.Key is not null) return IsEnglish ? text : reverse.Key;
        foreach (var item in Patterns.Where(p => p.English == IsEnglish))
        {
            var match = item.Pattern.Match(text);
            if (match.Success) return Regex.Replace(item.Target, @"\{(\d+)\}", m => match.Groups["p" + m.Groups[1].Value].Value);
        }
        return text.Contains('\n') ? string.Join('\n', text.Split('\n').Select(T)) : text;
    }
    private static List<(Regex, string, bool)> CreatePatterns()
    {
        var result = new List<(Regex, string, bool)>();
        foreach (var pair in English.Where(p => Regex.IsMatch(p.Key, @"\{\d+\}")))
        {
            result.Add((Pattern(pair.Key), pair.Value, true));
            result.Add((Pattern(pair.Value), pair.Key, false));
        }
        return result;
    }
    private static Regex Pattern(string template)
    {
        var builder = new StringBuilder("^"); var position = 0;
        foreach (Match match in Regex.Matches(template, @"\{(\d+)\}"))
        {
            builder.Append(Regex.Escape(template[position..match.Index]));
            builder.Append("(?<p" + match.Groups[1].Value + ">.*?)"); position = match.Index + match.Length;
        }
        builder.Append(Regex.Escape(template[position..])).Append('$');
        return new(builder.ToString(), RegexOptions.Singleline | RegexOptions.CultureInvariant);
    }
}

public sealed class TranslateConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) => Locale.T(value?.ToString());
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotSupportedException();
}
