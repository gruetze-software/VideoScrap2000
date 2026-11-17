using System.Globalization;
using Avalonia.Data.Converters;
using VideoScrap2000.Localization;

namespace VideoScrap2000.Views;

public sealed class MovieRatingLabelConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not float score || !float.IsFinite(score) || score is <= 0 or > 10)
            return string.Empty;

        var displayCulture = CultureInfo.GetCultureInfo(Locale.IsEnglish ? "en-US" : "de-DE");
        return $"★ {score.ToString("F1", displayCulture)}";
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotSupportedException();
}
