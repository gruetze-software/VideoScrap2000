using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace MovieScrap2000.TMDbScraper
{
	public class SingleResultContainer<T>
	{
		[JsonPropertyName("id")]
		public int Id { get; set; }

		[JsonPropertyName("results")]
		public T? Results { get; set; }
	}
}
