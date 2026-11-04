using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace MovieScrap2000.TMDbScraper
{
	public class SearchTv : SearchMovieTvBase
	{
		public SearchTv()
		{
			MediaType = MediaType.tv;
		}

		[JsonPropertyName("first_air_date")]
		[JsonConverter(typeof(TmdbOptionalDateConverter))]
		public DateTime? FirstAirDate { get; set; }

		[JsonPropertyName("name")]
		public string? Name { get; set; }

		[JsonPropertyName("original_name")]
		public string? OriginalName { get; set; }

		/// <summary>
		/// Country ISO code ex. US
		/// </summary>
		[JsonPropertyName("origin_country")]
		public List<string> OriginCountry { get; set; } = new List<string>();
	}
}
