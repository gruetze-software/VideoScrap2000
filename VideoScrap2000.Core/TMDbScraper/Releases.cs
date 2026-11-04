using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace MovieScrap2000.TMDbScraper
{
	public class Releases
	{
		[JsonPropertyName("countries")]
		public List<Country> Countries { get; set; } = new List<Country>();

		[JsonPropertyName("id")]
		public int Id { get; set; }
	}
}
