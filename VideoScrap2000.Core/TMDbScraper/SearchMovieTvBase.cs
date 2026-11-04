using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace MovieScrap2000.TMDbScraper
{
	public class SearchMovieTvBase : SearchBase
	{
		[JsonPropertyName("backdrop_path")]
		public string? BackdropPath { get; set; }

		[JsonPropertyName("genre_ids")]
		[JsonConverter(typeof(TMDbTools.TmdbIntArrayAsObjectConverter))]
		public List<int> GenreIds { get; set; } = new List<int>();

		[JsonPropertyName("original_language")]
		public string? OriginalLanguage { get; set; }

		[JsonPropertyName("overview")]
		public string? Overview { get; set; }

		[JsonPropertyName("poster_path")]
		public string? PosterPath { get; set; }

		[JsonPropertyName("vote_average")]
		public double VoteAverage { get; set; }

		[JsonPropertyName("vote_count")]
		public int VoteCount { get; set; }
	}
}
