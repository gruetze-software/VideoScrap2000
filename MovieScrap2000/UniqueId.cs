using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace MovieScrap2000
{
	public class UniqueId
	{
		[XmlText]
		public string? IdValue { get; set; }

		[XmlAttribute("type")]
		public string? Type { get; set; }

		[XmlAttribute("default")]
		public bool Default { get; set; }
	}
}
