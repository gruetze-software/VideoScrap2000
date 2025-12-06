using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace MovieScrap2000.TMDbScraper
{
	public class ProductionCountry
	{
		/// <summary>
		/// A country code, e.g. US
		/// </summary>
		[JsonPropertyName("iso_3166_1")]
		public string? Iso31661 { get; set; }

		[JsonPropertyName("name")]
		public string? Name { get; set; }
	}
}
