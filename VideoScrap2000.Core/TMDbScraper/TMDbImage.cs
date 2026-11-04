using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;


namespace MovieScrap2000.TMDbScraper.TMDbScraper
{

	public class TMDbImages
	{
		[JsonPropertyName("backdrops")]
		public List<TMDbImageData> Backdrops { get; set; } = new List<TMDbImageData>();

		[JsonPropertyName("posters")]
		public List<TMDbImageData> Posters { get; set; } = new List<TMDbImageData>();

		[JsonPropertyName("logos")]
		public List<TMDbImageData> Logos { get; set; } = new List<TMDbImageData>();
	}

	public class TMDbImageData
	{
		[JsonPropertyName("aspect_ratio")]
		public double AspectRatio { get; set; }

		[JsonPropertyName("file_path")]
		public string? FilePath { get; set; }

		[JsonPropertyName("height")]
		public int Height { get; set; }

		[JsonPropertyName("iso_639_1")]
		public string? Iso6391 { get; set; }

		[JsonPropertyName("iso_3166_1")]
		public string? Iso31661 { get; set; }

		[JsonPropertyName("vote_average")]
		public double VoteAverage { get; set; }

		[JsonPropertyName("vote_count")]
		public int VoteCount { get; set; }

		[JsonPropertyName("width")]
		public int Width { get; set; }
	}
}
