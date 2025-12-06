using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using static MovieScrap2000.TMDbScraper.TMDbTools;

namespace MovieScrap2000.TMDbScraper
{
	public abstract class KnownForBase
	{
		[JsonPropertyName("backdrop_path")]
		public string? BackdropPath { get; set; }

		[JsonPropertyName("genre_ids")]
		[JsonConverter(typeof(TmdbIntArrayAsObjectConverter)) /*#307*/]
		public List<int> GenreIds { get; set; } = new List<int>();

		[JsonPropertyName("id")]
		public int Id { get; set; }

		[JsonPropertyName("media_type")]
		public MediaType MediaType { get; set; }

		[JsonPropertyName("original_language")]
		public string? OriginalLanguage { get; set; }

		[JsonPropertyName("overview")]
		public string? Overview { get; set; }

		[JsonPropertyName("popularity")]
		public double Popularity { get; set; }

		[JsonPropertyName("poster_path")]
		public string? PosterPath { get; set; }

		[JsonPropertyName("vote_average")]
		public double VoteAverage { get; set; }

		[JsonPropertyName("vote_count")]
		public int VoteCount { get; set; }
	}
}
