using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace MovieScrap2000.TMDbScraper
{
	public class SpokenLanguage
	{
		/// <summary>
		/// A language code, e.g. en
		/// </summary>
		[JsonPropertyName("iso_639_1")]
		public string? Iso6391 { get; set; }
		
		[JsonPropertyName("name")]
		public string? Name { get; set; }
	}
}
