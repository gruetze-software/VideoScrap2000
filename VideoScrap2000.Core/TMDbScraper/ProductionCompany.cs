using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace MovieScrap2000.TMDbScraper
{
	public class ProductionCompany
	{
		[JsonPropertyName("id")]
		public int Id { get; set; }

		[JsonPropertyName("name")]
		public string? Name { get; set; }

		[JsonPropertyName("logo_path")]
		public string? LogoPath { get; set; }

		[JsonPropertyName("origin_country")]
		public string? OriginCountry { get; set; }
	}
}
