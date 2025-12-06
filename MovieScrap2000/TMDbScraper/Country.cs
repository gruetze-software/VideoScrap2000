using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace MovieScrap2000.TMDbScraper
{
	public class Country
	{
		[JsonPropertyName("certification")]
		public string? Certification { get; set; }

		/// <summary>
		/// A country code, e.g. US
		/// </summary>
		[JsonPropertyName("iso_3166_1")]
		public string? Iso31661 { get; set; }

		[JsonPropertyName("primary")]
		public bool Primary { get; set; }

		[JsonPropertyName("release_date")]
		public DateTime? ReleaseDate { get; set; }
	}
}
