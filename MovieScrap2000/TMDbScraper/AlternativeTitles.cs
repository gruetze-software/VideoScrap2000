using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace MovieScrap2000.TMDbScraper
{
	public class AlternativeTitle
	{
		/// <summary>
		/// A country code, e.g. US
		/// </summary>
		[JsonPropertyName("iso_3166_1")]
		public string? Iso31661 { get; set; }

		[JsonPropertyName("title")]
		public string? Title { get; set; }

		/// <summary>
		/// The type of title (e.g. working title, DVD title, modern title)
		/// </summary>
		[JsonPropertyName("type")]
		public string? Type { get; set; }
	}

	public class AlternativeTitles
	{
		[JsonPropertyName("id")]
		public int Id { get; set; }

		[JsonPropertyName("titles")]
		public List<AlternativeTitle> Titles { get; set; } = new List<AlternativeTitle>();
	}
}
