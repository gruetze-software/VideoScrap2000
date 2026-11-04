using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace VideoScrap2000.Views;

public sealed class RatingStarsConverter : IValueConverter
{
    private static readonly Geometry FullStar = StreamGeometry.Parse("M8,0 L10.4,5.1 L16,6 L12,10 L13,16 L8,13 L3,16 L4,10 L0,6 L5.6,5.1 Z");
    private static readonly Geometry HalfStar = StreamGeometry.Parse("M8,0 L8,13 L3,16 L4,10 L0,6 L5.6,5.1 Z");

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var text = value is string input ? input.Replace(',', '.') : System.Convert.ToString(value, CultureInfo.InvariantCulture);
        if (!decimal.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out var score) || score is < 0 or > 10)
            return Array.Empty<Geometry>();

        var halfStars = (int)Math.Round(score, MidpointRounding.AwayFromZero);
        var stars = new Geometry[(halfStars + 1) / 2];
        for (var i = 0; i < stars.Length; i++) stars[i] = i * 2 + 1 == halfStars ? HalfStar : FullStar;
        return stars;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotSupportedException();
}
