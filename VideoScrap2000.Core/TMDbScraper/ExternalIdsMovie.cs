using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace MovieScrap2000.TMDbScraper
{
	public class ExternalIdsMovie : ExternalIds
	{
		[JsonPropertyName("imdb_id")]
		public string? ImdbId { get; set; }

		[JsonPropertyName("facebook_id")]
		public string? FacebookId { get; set; }

		[JsonPropertyName("twitter_id")]
		public string? TwitterId { get; set; }

		[JsonPropertyName("instagram_id")]
		public string? InstagramId { get; set; }
	}

	public class ExternalIds
	{
		[JsonPropertyName("freebase_id")]
		public string? FreebaseId { get; set; }

		[JsonPropertyName("freebase_mid")]
		public string? FreebaseMid { get; set; }

		[JsonPropertyName("id")]
		public int Id { get; set; }

		[JsonPropertyName("tvrage_id")]
		public string? TvrageId { get; set; }

		[JsonPropertyName("wikidata_id")]
		public string? WikidataId { get; set; }
	}
}
