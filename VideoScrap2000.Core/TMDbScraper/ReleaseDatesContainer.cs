using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace MovieScrap2000.TMDbScraper
{
	public enum ReleaseDateType
	{
		premiere = 1,
		theatricallimited = 2,
		theatrical = 3,
		digital = 4,
		physical = 5,
		tv = 6
	}

	public class ReleaseDatesContainer
	{
		/// <summary>
		/// A country code, e.g. US
		/// </summary>
		[JsonPropertyName("iso_3166_1")]
		public string? Iso31661 { get; set; }

		[JsonPropertyName("release_dates")]
		public List<ReleaseDateItem> ReleaseDates { get; set; } = new List<ReleaseDateItem>();
	}

	public class ReleaseDateItem
	{
		[JsonPropertyName("certification")]
		public string? Certification { get; set; }

		/// <summary>
		/// A language code, e.g. en
		/// </summary>
		[JsonPropertyName("iso_639_1")]
		public string? Iso6391 { get; set; }

		[JsonPropertyName("note")]
		public string? Note { get; set; }

		[JsonPropertyName("release_date")]
		public DateTime ReleaseDate { get; set; }

		[JsonPropertyName("type")]
		public ReleaseDateType Type { get; set; }
	}
}
