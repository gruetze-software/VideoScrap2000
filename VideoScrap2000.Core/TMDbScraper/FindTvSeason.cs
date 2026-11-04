using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace MovieScrap2000.TMDbScraper
{
	public class FindTvSeason
	{
		[JsonPropertyName("air_date")]
		[JsonConverter(typeof(TmdbOptionalDateConverter))]
		public DateTime? AirDate { get; set; }

		[JsonPropertyName("episode_count")]
		public int EpisodeCount { get; set; }

		[JsonPropertyName("id")]
		public int Id { get; set; }

		[JsonPropertyName("name")]
		public string? Name { get; set; }

		[JsonPropertyName("overview")]
		public string? Overview { get; set; }

		[JsonPropertyName("poster_path")]
		public string? PosterPath { get; set; }

		[JsonPropertyName("season_number")]
		public int SeasonNumber { get; set; }

		[JsonPropertyName("show_id")]
		public int ShowId { get; set; }
	}
}
