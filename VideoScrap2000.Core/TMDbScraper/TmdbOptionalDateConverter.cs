using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MovieScrap2000.TMDbScraper;

public sealed class TmdbOptionalDateConverter : JsonConverter<DateTime?>
{
    public override bool HandleNull => true;
    public override DateTime? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null) return null;
        if (reader.TokenType != JsonTokenType.String) throw new JsonException("Expected a date string or null.");
        return DateTime.TryParse(reader.GetString(), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var date) ? date : null;
    }
    public override void Write(Utf8JsonWriter writer, DateTime? value, JsonSerializerOptions options)
    {
        if (value.HasValue) writer.WriteStringValue(value.Value); else writer.WriteNullValue();
    }
}
