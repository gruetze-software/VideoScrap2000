using System.Globalization;
using System.Text.RegularExpressions;

namespace MovieScrap2000;

public sealed record ParsedMovieName(string Title, int? Year);

public static class MovieNameParser
{
    private static readonly Regex Years = new(@"(?<![\p{L}\p{N}])(?:18|19|20)\d{2}(?![\p{L}\p{N}])", RegexOptions.CultureInvariant);
    private static readonly Regex Technical = new(@"(?<!\w)(?:\d{3,4}p|web[ ._-]?(?:dl|rip)|blu[ ._-]?ray|bd[ ._-]?rip|brrip|dvd[ ._-]?rip|hdtv|[xh][ ._-]?26[45]|hevc|avc|remux)(?!\w)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
    private static readonly Regex TrailingTags = new(@"(?:\s+(?:german|english|deutsch|dl|dual|multi|dubbed|subbed|proper|repack|aac|dts|ac3))+$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
    private static readonly Regex Parenthetical = new(@"\((?>[^()]|\((?<nested>)|\)(?<-nested>))*(?(nested)(?!))\)");

    public static ParsedMovieName ParseSearchTitle(string title)
    {
        int? year = null;
        var cleaned = Parenthetical.Replace(title, match =>
        {
            var content = match.Value.AsSpan()[1..^1].Trim();
            if (content.Length == 4 && int.TryParse(content, NumberStyles.None, CultureInfo.InvariantCulture, out var candidate)
                && candidate >= 1888 && candidate <= DateTime.UtcNow.Year + 1)
                year = candidate;
            return " ";
        });
        return new(Regex.Replace(cleaned, @"\s+", " ").Trim(), year);
    }

    public static ParsedMovieName Parse(string fileName)
    {
        // Only remove real video extensions; a release name may end in .x264-GROUP.
        var name = Path.GetFileName(fileName);
        if (MovieManager.VideoExtensions.Contains(Path.GetExtension(name).ToLowerInvariant())) name = Path.GetFileNameWithoutExtension(name);
        name = Regex.Replace(name, @"[._]+", " ");
        name = Regex.Replace(name, @"\s+", " ").Trim();
        var technical = Technical.Match(name);
        var titlePart = technical.Success ? name[..technical.Index] : name;
        int? year = null;
        // The last plausible year lets titles such as "2001 A Space Odyssey 1968" survive.
        var match = Years.Matches(titlePart).Cast<Match>().LastOrDefault(m =>
            int.Parse(m.Value, CultureInfo.InvariantCulture) is var value && value >= 1888 && value <= DateTime.UtcNow.Year + 1
            && titlePart[..m.Index].Any(char.IsLetterOrDigit));
        if (match is not null)
        {
            year = int.Parse(match.Value, CultureInfo.InvariantCulture);
            titlePart = titlePart[..match.Index];
        }
        if (technical.Success) titlePart = TrailingTags.Replace(titlePart.Trim(), "");
        titlePart = titlePart.Trim(' ', '-', '[', ']', '(', ')');
        titlePart = Regex.Replace(titlePart, @"\s+", " ");
        if (string.IsNullOrWhiteSpace(titlePart)) titlePart = name;
        if (titlePart == titlePart.ToLowerInvariant() || titlePart == titlePart.ToUpperInvariant())
            titlePart = CultureInfo.InvariantCulture.TextInfo.ToTitleCase(titlePart.ToLowerInvariant());
        return new(titlePart, year);
    }
}
