using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace MovieScrap2000.TMDbScraper
{
	public class SearchBase
	{
		[JsonPropertyName("id")]
		public int Id { get; set; }

		[JsonPropertyName("media_type")]
		public MediaType MediaType { get; set; }

		[JsonPropertyName("popularity")]
		public double Popularity { get; set; }
	}
}
