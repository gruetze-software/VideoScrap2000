using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace MovieScrap2000.TMDbScraper
{
	public class Crew : CrewBase
	{
		[JsonPropertyName("credit_id")]
		public string? CreditId { get; set; }

		[JsonPropertyName("job")]
		public string? Job { get; set; }
	}

	public class CrewBase
	{
		[JsonPropertyName("department")]
		public string? Department { get; set; }

		[JsonPropertyName("id")]
		public int Id { get; set; }

		[JsonPropertyName("name")]
		public string? Name { get; set; }

		[JsonPropertyName("profile_path")]
		public string? ProfilePath { get; set; }

		[JsonPropertyName("gender")]
		public PersonGender? Gender { get; set; }

		[JsonPropertyName("adult")]
		public bool Adult { get; set; }

		[JsonPropertyName("known_for_department")]
		public string? KnownForDepartment { get; set; }

		[JsonPropertyName("original_name")]
		public string? OriginalName { get; set; }

		[JsonPropertyName("popularity")]
		public float Popularity { get; set; }
	}
}
