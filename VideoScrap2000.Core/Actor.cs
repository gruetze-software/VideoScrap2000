using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace MovieScrap2000
{
	public class Actor
	{
		[XmlIgnore]
		public int Id { get; set; }

		[XmlElement("name")]
		public string Name { get; set; } = "";

		[XmlElement("role")]
		public string? Role { get; set; } = "";

		[XmlElement("order")]
		public int Order { get; set; }

		[XmlElement("thumb")]
		public string? Thumb { get; set; }

		[XmlIgnore]
		public byte[]? ThumbData { get; set; }

		[XmlIgnore, System.Text.Json.Serialization.JsonIgnore]
		public bool HasPendingPortrait { get; set; }

		public const string PortraitFolder = ".actors";
		public const string ReplacementPrefix = "portrait-";
		public static bool IsReplacementPortrait(string? path) =>
			path?.Replace('\\', '/').StartsWith(PortraitFolder + "/" + ReplacementPrefix, StringComparison.Ordinal) == true;

		[XmlElement("tmdbid")]
		public int? TmdbId { get; set; } = 0;

		public override string ToString()
		{
			if (string.IsNullOrEmpty(Name)) return "[Empty Actor]";
			return Name;
		}

		internal Actor Copy()
		{
			Actor ac = new Actor()
			{
				Id = this.Id,
				Name = this.Name,
				Role = this.Role,
				Order = this.Order,
				Thumb = this.Thumb,
				TmdbId = this.TmdbId,
				HasPendingPortrait = this.HasPendingPortrait,
				ThumbData = this.ThumbData != null ? (byte[])this.ThumbData.Clone() : null
			};
			return ac;
    }
	}
}
