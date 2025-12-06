using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace MovieScrap2000.TMDbScraper
{
	public class FindContainer
	{
		[JsonPropertyName("movie_results")]
		public List<SearchMovie> MovieResults { get; set; } = new List<SearchMovie>();

		[JsonPropertyName("person_results")]
		public List<FindPerson> PersonResults { get; set; } = new List<FindPerson>();

		[JsonPropertyName("tv_episode_results")]
		public List<SearchTvEpisode> TvEpisode { get; set; } = new List<SearchTvEpisode>();

		[JsonPropertyName("tv_results")]
		public List<SearchTv> TvResults { get; set; } = new List<SearchTv>();

		[JsonPropertyName("tv_season_results")]
		public List<FindTvSeason> TvSeason { get; set; } = new List<FindTvSeason>();
	}
}
