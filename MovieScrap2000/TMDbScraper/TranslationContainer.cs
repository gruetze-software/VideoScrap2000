using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Transactions;

namespace MovieScrap2000.TMDbScraper
{
	public class TranslationsContainer
	{
		[JsonPropertyName("id")]
		public int Id { get; set; }

		[JsonPropertyName("translations")]
		public List<Translation> Translations { get; set; } = new List<Translation>();
	}

	public class Translation
	{
		[JsonPropertyName("english_name")]
		public string? EnglishName { get; set; }

		/// <summary>
		/// A language code, e.g. en
		/// </summary>
		[JsonPropertyName("iso_639_1")]
		public string? Iso6391 { get; set; }

		[JsonPropertyName("iso_3166_1")]
		public string? Iso31661 { get; set; }

		[JsonPropertyName("name")]
		public string? Name { get; set; }

		[JsonPropertyName("data")]
		public TranslationData Data { get; set; } = new TranslationData();
	}

	public class TranslationData
	{
		[JsonPropertyName("name")]
		public string? Name { get; set; }

		// Private hack to ensure two properties (name, title) are deserialized into Name.
		// Tv Shows and Movies will use different names for their translation data.
		[JsonPropertyName("title")]
		private string? Title
		{
			set => Name = value;
		}

		[JsonPropertyName("overview")]
		public string? Overview { get; set; }

		// Private hack to ensure two properties (overview, biography) are deserialized into Overview.
		// Most of the entities have an overview, but people have a biography.
		[JsonPropertyName("biography")]
		private string? Biography
		{
			set => Overview = value;
		}

		[JsonPropertyName("homepage")]
		public string? HomePage { get; set; }

		[JsonPropertyName("tagline")]
		public string? Tagline { get; set; }

		[JsonPropertyName("runtime")]
		public int Runtime { get; set; }
	}
}
