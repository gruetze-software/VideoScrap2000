using System;
using System.Collections.Generic;
using System.Formats.Asn1;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace MovieScrap2000.TMDbScraper
{
	public class TMDbTools
	{

		public class TmdbIntArrayAsObjectConverter : JsonConverter<List<int>>
		{
			// Die Basisklasse JsonConverter<T> macht CanConvert unnötig und besser
			// T ist List<int>, d.h. dieser Konverter wird nur für List<int> aufgerufen.

			public override List<int>? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
			{
				// Der Typ, den wir erwarten, ist List<int> (repräsentiert durch das T-Argument)

				switch (reader.TokenType)
				{
					// Fall 1: Korrektes Array ("genre_ids": [1, 2, ...])
					case JsonTokenType.StartArray:
						// Wir verwenden den Standard-Deserialisierer von System.Text.Json,
						// um das Array korrekt in List<int> zu konvertieren.
						// ACHTUNG: Hier muss 'options' übergeben werden, um Rekursion zu vermeiden
						// (wir können den Standard-Konverter nicht direkt verwenden, da wir uns in diesem befinden).
						// Der effizienteste Weg ist die Verwendung von JsonSerializer.Deserialize.

						// Wir erstellen eine Kopie der Optionen, damit wir unseren eigenen Konverter entfernen.
						var internalOptions = new JsonSerializerOptions(options);
						// Entferne diesen Konverter aus den Optionen, um eine Endlosschleife zu verhindern.
						internalOptions.Converters.Remove(this);

						// Nutze den standardmäßigen List-Deserialisierer.
						return JsonSerializer.Deserialize<List<int>>(ref reader, internalOptions);

					// Fall 2: Falsches Objekt ("genre_ids": {})
					case JsonTokenType.StartObject:
						// Wenn wir ein StartObject-Token sehen, überspringen wir den Inhalt
						// (der Inhalt ist leer, {}), um zum EndObject-Token zu gelangen.
						// reader.Skip() macht genau das.
						reader.Skip();

						// Gebe eine leere Liste zurück, wie von der Logik gewünscht.
						return new List<int>();

					// Fall 3: Null ("genre_ids": null)
					case JsonTokenType.Null:
						return null;

					// Unerwarteter Fall
					default:
						throw new JsonException($"Unerwartetes Token {reader.TokenType} beim Lesen von List<int> erwartet.");
				}
			}

			public override void Write(Utf8JsonWriter writer, List<int> value, JsonSerializerOptions options)
			{
				// Pass-through: Wir übergeben den Wert an den Standard-Serializer
				// (Da wir keine spezielle Formatierung für das Schreiben benötigen).
				// Wir müssen sicherstellen, dass wir nicht rekursiv in diesen Konverter geraten.
				var internalOptions = new JsonSerializerOptions(options);
				internalOptions.Converters.Remove(this);

				JsonSerializer.Serialize(writer, value, internalOptions);
			}
		}
	}
}
