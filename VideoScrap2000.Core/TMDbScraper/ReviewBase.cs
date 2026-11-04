using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace MovieScrap2000.TMDbScraper
{
	public class ReviewBase
	{
		[JsonPropertyName("author")]
		public string? Author { get; set; }

		[JsonPropertyName("author_details")]
		public AuthorDetails AuthorDetails { get; set; } = new AuthorDetails();

		[JsonPropertyName("content")]
		public string? Content { get; set; }

		[JsonPropertyName("id")]
		public string? Id { get; set; }

		[JsonPropertyName("url")]
		public string? Url { get; set; }

		[JsonPropertyName("created_at")]
		public DateTime CreatedAt { get; set; }

		[JsonPropertyName("updated_at")]
		public DateTime UpdatedAt { get; set; }
	}
}
