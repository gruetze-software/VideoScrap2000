using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace MovieScrap2000.TMDbScraper
{
	public class SearchTvEpisode : SearchBase
	{
		public SearchTvEpisode()
		{
			MediaType = MediaType.episode;
		}

		[JsonPropertyName("air_date")]
		public DateTime? AirDate { get; set; }

		[JsonPropertyName("episode_number")]
		public int EpisodeNumber { get; set; }

		[JsonPropertyName("name")]
		public string? Name { get; set; }

		[JsonPropertyName("overview")]
		public string? Overview { get; set; }

		[JsonPropertyName("production_code")]
		public string? ProductionCode { get; set; }

		[JsonPropertyName("season_number")]
		public int SeasonNumber { get; set; }

		[JsonPropertyName("show_id")]
		public int ShowId { get; set; }

		[JsonPropertyName("still_path")]
		public string? StillPath { get; set; }

		[JsonPropertyName("vote_average")]
		public double VoteAverage { get; set; }

		[JsonPropertyName("vote_count")]
		public int VoteCount { get; set; }
	}
}
