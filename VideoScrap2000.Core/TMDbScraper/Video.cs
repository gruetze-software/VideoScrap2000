using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace MovieScrap2000.TMDbScraper
{
	public class Video
	{
		[JsonPropertyName("id")]
		public string? Id { get; set; }

		/// <summary>
		/// A country code, e.g. US
		/// </summary>
		[JsonPropertyName("iso_3166_1")]
		public string? Iso31661 { get; set; }

		/// <summary>
		/// A language code, e.g. en
		/// </summary>
		[JsonPropertyName("iso_639_1")]
		public string? Iso6391 { get; set; }

		[JsonPropertyName("key")]
		public string? Key { get; set; }

		[JsonPropertyName("name")]
		public string? Name { get; set; }

		[JsonPropertyName("official")]
		public bool Official { get; set; }

		[JsonPropertyName("published_at")]
		public DateTime PublishedAt { get; set; }

		[JsonPropertyName("site")]
		public string? Site { get; set; }

		[JsonPropertyName("size")]
		public int Size { get; set; }

		[JsonPropertyName("type")]
		public string? Type { get; set; }
	}
}
