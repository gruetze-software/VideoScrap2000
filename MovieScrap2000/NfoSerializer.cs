using MovieScrap2000;
using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Xml;
using System.Xml.Serialization;

public static class NfoSerializer
{
  public static string SerializeXml(Movie nfo)
  {
    var serializer = new XmlSerializer(typeof(Movie));

    // 1. Definiere die Schreib-Einstellungen
    var settings = new XmlWriterSettings
    {
      // Setze die Einrückung auf true
      Indent = true,
      // Definiere das Zeichen für die Einrückung (optional, Tabs oder Leerzeichen)
      IndentChars = "  ",
      // Definiere den Zeilenumbruch (Windows/Linux/Mac)
      NewLineHandling = NewLineHandling.Replace
    };

    using (var stringWriter = new StringWriter())
    {
      // 2. Erstelle einen XmlWriter mit den gewünschten Einstellungen
      using (var xmlWriter = XmlWriter.Create(stringWriter, settings))
      {
        // 3. Serialisiere das Objekt mit dem Writer
        serializer.Serialize(xmlWriter, nfo);
        return stringWriter.ToString();
      }
    }
  }

  // Konfiguriert die JSON-Serialisierungsoptionen für die Datenbank.
  // - WriteIndented = false: Spart Platz in der Datenbank.
  // - WhenWritingNull: Lässt null-Werte im JSON weg (macht den String kleiner).
  private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
	{
		WriteIndented = false,
		DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
		PropertyNameCaseInsensitive = true // Kann die Robustheit bei kleinen Namenskonflikten erhöhen
	};

	/// <summary>
	/// Serialisiert ein beliebiges NFO-Objekt (MovieNFO, TvShowNFO, etc.) in einen JSON-String 
	/// zur Speicherung im Feld 'JsonMetadata' der Datenbank.
	/// </summary>
	/// <typeparam name="T">Der Typ des NFO-Objekts.</typeparam>
	/// <param name="nfoObject">Das zu speichernde C#-Objekt.</param>
	/// <returns>JSON-String oder null, falls das Objekt null ist.</returns>
	public static string? Serialize<T>(T nfoObject) where T : class
	{
		if (nfoObject == null)
		{
			return null;
		}

		try
		{
			return JsonSerializer.Serialize(nfoObject, JsonOptions);
		}
		catch (Exception ex)
		{
			// Optional: Hier können Sie spezifische Fehlerprotokollierung hinzufügen
			System.Diagnostics.Trace.WriteLine($"Fehler beim Serialisieren des Objekts vom Typ {typeof(T).Name}: {ex.Message}");
			return null;
		}
	}

	/// <summary>
	/// Deserialisiert den JSON-String aus der Datenbank zurück in das entsprechende C#-Objekt.
	/// </summary>
	/// <typeparam name="T">Der erwartete Typ des NFO-Objekts.</typeparam>
	/// <param name="jsonString">Der JSON-String aus der Datenbank.</param>
	/// <returns>Das rekonstruierte NFO-Objekt oder null.</returns>
	public static T? Deserialize<T>(string jsonString) where T : class
	{
		if (string.IsNullOrWhiteSpace(jsonString))
		{
			return null;
		}

		try
		{
			// Beachte das '?' für nullable T (abhängig von C# Version > 8)
			return JsonSerializer.Deserialize<T>(jsonString, JsonOptions);
		}
		catch (Exception ex)
		{
			// Optional: Hier können Sie spezifische Fehlerprotokollierung hinzufügen
			System.Diagnostics.Trace.WriteLine($"Fehler beim Deserialisieren zu Typ {typeof(T).Name}: {ex.Message}");
			return null;
		}
	}
}