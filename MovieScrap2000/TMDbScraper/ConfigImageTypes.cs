using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace MovieScrap2000.TMDbScraper
{
	public enum ImageType : byte
	{
		Backdrop = 0,
		Logo,
		Poster,
		Profile,
		Still
	};

	public enum ImageSize : byte
	{
		Thumb = 0,
		Small,
		Medium,
		Large,
		Original,
	};

	public class ConfigImageTypes
	{
		[JsonPropertyName("backdrop_sizes")]
		public List<string> BackdropSizes { get; set; } = new List<string>();

		[JsonPropertyName("base_url")]
		public string? BaseUrl { get; set; }

		[JsonPropertyName("logo_sizes")]
		public List<string> LogoSizes { get; set; } = new List<string>();

		[JsonPropertyName("poster_sizes")]
		public List<string> PosterSizes { get; set; } = new List<string>();

		[JsonPropertyName("profile_sizes")]
		public List<string> ProfileSizes { get; set; } = new List<string>();

		[JsonPropertyName("secure_base_url")]
		public string? SecureBaseUrl { get; set; }

		[JsonPropertyName("still_sizes")]
		public List<string> StillSizes { get; set; } = new List<string>();
	}
}
