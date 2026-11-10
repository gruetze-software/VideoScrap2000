using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using System.Globalization;
using System.Xml.Linq;

namespace VideoScrap2000.Views;

internal static class MediaIconAssets
{
    private static readonly Dictionary<string, Geometry> Shapes = [];
    private static readonly Dictionary<string, Bitmap> Flags = [];
    internal static Control Icon(string name, Control owner)
    {
        if (!Shapes.TryGetValue(name, out var geometry))
        {
            using var stream = AssetLoader.Open(new Uri($"avares://VideoScrap2000/Assets/MediaIcons/{name}.svg"));
            var document = XDocument.Load(stream);
            geometry = Geometry.Parse(string.Join(" ", document.Descendants().Where(node => node.Name.LocalName == "path").Select(node => (string?)node.Attribute("d"))));
            Shapes[name] = geometry;
        }
        var icon = new Avalonia.Controls.Shapes.Path { Data = geometry, Width = 32, Height = 25, Stretch = Stretch.Uniform };
        icon.Bind(Avalonia.Controls.Shapes.Shape.FillProperty, new Binding("Foreground") { Source = owner });
        return icon;
    }
    internal static Control Flag(string? country, Control owner)
    {
        var uri = new Uri($"avares://VideoScrap2000/Assets/Flags/{country}.png");
        if (country is null || !AssetLoader.Exists(uri)) return Icon("web", owner);
        if (!Flags.TryGetValue(country, out var image))
        {
            using var stream = AssetLoader.Open(uri); Flags[country] = image = new Bitmap(stream);
        }
        return new Image { Source = image, Width = 24, Height = 18, Stretch = Stretch.Uniform };
    }
    internal static string? Country(string? language)
    {
        if (string.IsNullOrWhiteSpace(language)) return null;
        var code = language.Trim().Replace('_', '-').ToLowerInvariant();
        var aliases = new Dictionary<string, string> { ["ger"] = "deu", ["fre"] = "fra", ["dut"] = "nld", ["chi"] = "zho", ["cze"] = "ces", ["gre"] = "ell", ["rum"] = "ron", ["slo"] = "slk", ["wel"] = "cym", ["alb"] = "sqi", ["arm"] = "hye", ["baq"] = "eus", ["per"] = "fas", ["bur"] = "mya", ["ice"] = "isl", ["mac"] = "mkd", ["may"] = "msa", ["tib"] = "bod", ["geo"] = "kat" };
        code = aliases.GetValueOrDefault(code, code);
        try
        {
            var culture = CultureInfo.GetCultures(CultureTypes.AllCultures).FirstOrDefault(item => item.Name.Equals(code, StringComparison.OrdinalIgnoreCase))
                ?? CultureInfo.GetCultures(CultureTypes.NeutralCultures).FirstOrDefault(item => item.ThreeLetterISOLanguageName == code || item.EnglishName.Equals(language, StringComparison.OrdinalIgnoreCase) || item.NativeName.Equals(language, StringComparison.OrdinalIgnoreCase));
            if (culture is null || culture.Name.Length == 0 || code is "und" or "mul" or "zxx") return null;
            if (culture.IsNeutralCulture && culture.TwoLetterISOLanguageName == "en") return "gb";
            var region = new RegionInfo(culture.IsNeutralCulture ? CultureInfo.CreateSpecificCulture(culture.Name).Name : culture.Name);
            return region.TwoLetterISORegionName.ToLowerInvariant();
        }
        catch (ArgumentException) { return null; }
    }
}
