namespace MovieScrap2000;

public static class SeriesArtworkCatalog
{
    public static IReadOnlyList<string> Types(int? season) => season.HasValue
        ? [ImageTypes.Poster, ImageTypes.Fanart, ImageTypes.Banner, ImageTypes.Landscape]
        : [ImageTypes.Poster, ImageTypes.Fanart, ImageTypes.ClearLogo, ImageTypes.ClearArt,
            ImageTypes.Banner, ImageTypes.Landscape, ImageTypes.CharacterArt];

    public static IReadOnlyList<string> FanartFields(string type, int? season) => type switch
    {
        ImageTypes.Poster => season.HasValue ? ["seasonposter"] : ["tvposter"],
        ImageTypes.Fanart => ["showbackground", "show4kbackground"],
        ImageTypes.ClearLogo => ["hdtvlogo", "clearlogo"],
        ImageTypes.ClearArt => ["hdclearart", "clearart"],
        ImageTypes.Banner => season.HasValue ? ["seasonbanner"] : ["tvbanner", "seasonbanner"],
        ImageTypes.Landscape => season.HasValue ? ["seasonthumb"] : ["tvthumb"],
        ImageTypes.CharacterArt => ["characterart"],
        _ => []
    };
}
