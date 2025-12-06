using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MovieScrap2000
{
	public enum LogLevel
	{
		Info,      // Allgemeine Informationen und Fortschritt (Default)
		Warning,   // Etwas Unerwartetes ist passiert, beeinträchtigt aber nicht den Vorgang
		Error,     // Ein Fehler, der behandelt werden kann (z.B. NFO Lesefehler)
		Critical   // Kritischer Fehler, der den Vorgang stoppt oder die Anwendung betrifft
	}

	public class LogMessageObject
	{
		public DateTime Timestamp { get; }
		public LogLevel Level { get; }
		public string Source { get; }      // Z.B. "Scanner", "TMDbScraper", "UI"
		public string Message { get; }

		public LogMessageObject(string message, LogLevel level = LogLevel.Info, string source = "Manager")
		{
			this.Timestamp = DateTime.Now;
			this.Message = message;
			this.Level = level;
			this.Source = source;
		}

		// Hilfsmethode für eine einfache String-Darstellung (für die Anzeige)
		public override string ToString()
		{
			return $"[{Timestamp:HH:mm:ss}] [{Level.ToString().ToUpper()}] ({Source}): {Message}";
		}
	}
}
