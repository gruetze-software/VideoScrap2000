
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace MovieScrap2000.TMDbScraper
{
	public class KeywordsContainer
	{
		[JsonPropertyName("id")]
		public int Id { get; set; }

		[JsonPropertyName("keywords")]
		public List<Keyword> Keywords { get; set; } = new List<Keyword>();
	}

	public class Keyword
	{
		[JsonPropertyName("id")]
		public int Id { get; set; }

		[JsonPropertyName("name")]
		public string? Name { get; set; }
	}
}
