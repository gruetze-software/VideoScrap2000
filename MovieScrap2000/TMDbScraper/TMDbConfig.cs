using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace MovieScrap2000.TMDbScraper
{
	public class TMDbConfig
	{
		[JsonPropertyName("change_keys")]
		public List<string> ChangeKeys { get; set; } = new List<string>();

		[JsonPropertyName("images")]
		public ConfigImageTypes Images { get; set; } = new ConfigImageTypes();
	}
}
