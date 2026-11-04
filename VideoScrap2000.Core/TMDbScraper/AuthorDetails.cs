using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace MovieScrap2000.TMDbScraper
{
	public class AuthorDetails
	{
		[JsonPropertyName("name")]
		public string? Name { get; set; }

		[JsonPropertyName("username")]
		public string? Username { get; set; }

		[JsonPropertyName("avatar_path")]
		public string? AvatarPath { get; set; }

		[JsonPropertyName("rating")]
		public double? Rating { get; set; }
	}
}
