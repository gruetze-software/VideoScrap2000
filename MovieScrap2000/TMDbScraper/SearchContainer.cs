using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace MovieScrap2000.TMDbScraper
{
	public class SearchContainer<T>
	{
		[JsonPropertyName("page")]
		public int Page { get; set; }

		[JsonPropertyName("results")]
		public List<T> Results { get; set; } = new List<T>();

		[JsonPropertyName("total_pages")]
		public int TotalPages { get; set; }

		[JsonPropertyName("total_results")]
		public int TotalResults { get; set; }
	}
}
