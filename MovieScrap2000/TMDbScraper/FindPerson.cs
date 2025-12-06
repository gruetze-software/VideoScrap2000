using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace MovieScrap2000.TMDbScraper
{
	public class FindPerson : SearchPerson
	{
		[JsonPropertyName("gender")]
		public PersonGender Gender { get; set; }

		[JsonPropertyName("known_for_department")]
		public string? KnownForDepartment { get; set; }
	}
}
