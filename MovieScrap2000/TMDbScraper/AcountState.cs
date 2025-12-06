using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace MovieScrap2000.TMDbScraper
{
	public class AccountState
	{
		/// <summary>
		/// Represents the current favorite status of the related movie for the current user session.
		/// </summary>
		[JsonPropertyName("favorite")]
		public bool Favorite { get; set; }

		/// <summary>
		/// The TMDb id for the related movie
		/// </summary>
		[JsonPropertyName("id")]
		public int Id { get; set; }

		[JsonPropertyName("rating")]
		public double? Rating { get; set; }

		/// <summary>
		/// Represents the presence of the related movie on the current user's watchlist.
		/// </summary>
		[JsonPropertyName("watchlist")]
		public bool Watchlist { get; set; }
	}
}
