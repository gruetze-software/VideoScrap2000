using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace MovieScrap2000.TMDbScraper
{
	[JsonConverter(typeof(JsonStringEnumConverter))]
	public enum ChangeAction
	{
		Unknown,
		added = 1,
		created = 2,
		updated = 3,
		deleted = 4,
		destroyed = 5
	}

	public class ChangesContainer
	{
		[JsonPropertyName("changes")]
		public List<Change> Changes { get; set; } = new List<Change>();
	}

	public class Change
	{
		[JsonPropertyName("items")]
		public List<ChangeItemBase> Items { get; set; } = new List<ChangeItemBase>();

		[JsonPropertyName("key")]
		public string? Key { get; set; }
	}

	public abstract class ChangeItemBase
	{
		[JsonPropertyName("action")]
		public ChangeAction Action { get; set; }

		[JsonPropertyName("id")]
		public string? Id { get; set; }

		/// <summary>
		/// A language code, e.g. en
		/// This field is not always set
		/// </summary>
		[JsonPropertyName("iso_639_1")]
		public string? Iso6391 { get; set; }

		[JsonPropertyName("time")]
		[JsonConverter(typeof(TmdbUtcTimeConverter))]
		public DateTime Time { get; set; }
	}
}
