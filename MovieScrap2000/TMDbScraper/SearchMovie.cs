using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace MovieScrap2000.TMDbScraper
{
	public class SearchMovie : SearchMovieTvBase
	{
		public SearchMovie()
		{
			MediaType = MediaType.movie;
		}

		[JsonPropertyName("adult")]
		public bool Adult { get; set; }

		[JsonPropertyName("original_title")]
		public string? OriginalTitle { get; set; }

		[JsonPropertyName("release_date")]
		public DateTime? ReleaseDate { get; set; }

		[JsonPropertyName("title")]
		public string? Title { get; set; }

		[JsonPropertyName("video")]
		public bool Video { get; set; }

		public override string ToString()
		{
			return $"{Title} ({ReleaseDate?.Year.ToString() ?? "n/a"})";
		}
	}
}
