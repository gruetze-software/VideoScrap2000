using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace MovieScrap2000.TMDbScraper
{
	public class SearchCollection : SearchBase
	{
		// Fängt alle unbekannten Properties als JsonElement auf.
		[JsonExtensionData]
    // Feld MUSS initialisiert werden, der Compiler vertraut aber der externen Logik nicht.
    // Wir setzen es auf den Null-Vergessens-Operator, da die Runtime es setzt.
    private IDictionary<string, JsonElement> _additionalData = null!;

    [JsonPropertyName("adult")]
		public bool Adult { get; set; }

		[JsonPropertyName("backdrop_path")]
		public string? BackdropPath { get; set; }

		private string? _name;
		private string? _originalName;

		[JsonPropertyName("name")]
		public string? Name
		{
			get
			{
				// Fallback-Logik:
				if (_name == null && _additionalData != null &&
						_additionalData.TryGetValue("title", out var nameElement) &&
						nameElement.ValueKind != JsonValueKind.Null) // Sicherstellen, dass der Wert nicht null ist
				{
					// JsonElement.GetString() liest den Wert als string aus.
					return nameElement.GetString();
				}

				return _name;
			}
			set => _name = value;
		}

		[JsonPropertyName("original_language")]
		public string? OriginalLanguage { get; set; }

		[JsonPropertyName("original_name")]
		public string? OriginalName
		{
			get
			{
				// Fallback-Logik:
				if (_originalName == null && _additionalData != null &&
						_additionalData.TryGetValue("original_title", out var originalNameElement) &&
						originalNameElement.ValueKind != JsonValueKind.Null)
				{
					// JsonElement.GetString() liest den Wert als string aus.
					return originalNameElement.GetString();
				}

				return _originalName;
			}
			set => _originalName = value;
		}

		[JsonPropertyName("overview")]
		public string? Overview { get; set; }

		[JsonPropertyName("poster_path")]
		public string? PosterPath { get; set; }
	}
}
