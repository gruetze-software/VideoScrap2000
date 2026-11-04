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
				ThumbData = this.ThumbData != null ? (byte[])this.ThumbData.Clone() : null
			};
			return ac;
    }
	}
}
