using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace MovieScrap2000.TMDbScraper
{
	public class WatchProviders
	{
		[JsonPropertyName("link")]
		public string? Link { get; set; }

		[JsonPropertyName("flatrate")]
		public List<WatchProviderItem> FlatRate { get; set; } = new List<WatchProviderItem>();

		[JsonPropertyName("rent")]
		public List<WatchProviderItem> Rent { get; set; } = new List<WatchProviderItem>();

		[JsonPropertyName("buy")]
		public List<WatchProviderItem> Buy { get; set; } = new List<WatchProviderItem>();

		[JsonPropertyName("free")]
		public List<WatchProviderItem> Free { get; set; } = new List<WatchProviderItem>();

		[JsonPropertyName("ads")]
		public List<WatchProviderItem> Ads { get; set; } = new List<WatchProviderItem>();
	}

	public class WatchProviderItem
	{
		[JsonPropertyName("display_priority")]
		public int? DisplayPriority { get; set; }

		[JsonPropertyName("logo_path")]
		public string? LogoPath { get; set; }

		[JsonPropertyName("provider_id")]
		public int? ProviderId { get; set; }

		[JsonPropertyName("provider_name")]
		public string? ProviderName { get; set; }
	}
}
