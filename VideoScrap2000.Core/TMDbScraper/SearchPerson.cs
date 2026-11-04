using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace MovieScrap2000.TMDbScraper
{
	public class SearchPerson : SearchBase
	{
		public SearchPerson()
		{
			MediaType = MediaType.person;
		}

		[JsonPropertyName("adult")]
		public bool Adult { get; set; }

		[JsonPropertyName("known_for")]
		public List<KnownForBase> KnownFor { get; set; } = new List<KnownForBase>();

		[JsonPropertyName("name")]
		public string? Name { get; set; }

		[JsonPropertyName("profile_path")]
		public string? ProfilePath { get; set; }
	}
}
