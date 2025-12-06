using System;
using System.Collections.Generic;
using System.Formats.Asn1;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace MovieScrap2000.TMDbScraper
{
	public class TmdbUtcTimeConverter : JsonConverter<DateTime>
	{
		private const string Format = "yyyy-MM-dd HH:mm:ss 'UTC'";

		public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
		{
			// 1. Lesen des String-Wertes aus dem JSON
			if (reader.TokenType != JsonTokenType.String)
			{
				throw new JsonException($"Unerwarteter Token-Typ {reader.TokenType}. String erwartet.");
			}

			string? dateString = reader.GetString();

			if (string.IsNullOrEmpty(dateString))
			{
				// Behandeln von Null- oder leeren Werten
				return default;
			}

			// 2. Parsen des Strings mit dem spezifischen Format
			// Das 'UTC' im Formatstring ist ein Literal, daher verwenden wir DateTime.ParseExact.
			if (DateTime.TryParseExact(dateString, Format, CultureInfo.InvariantCulture,
																 DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
																 out DateTime result))
			{
				return result;
			}

			throw new JsonException($"Datum '{dateString}' konnte nicht in das Format '{Format}' geparst werden.");
		}

		public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
		{
			// Schreiben des DateTime-Objekts im gewünschten Format zurück in den JSON-String.
			writer.WriteStringValue(value.ToString(Format, CultureInfo.InvariantCulture));
		}
	}
}
