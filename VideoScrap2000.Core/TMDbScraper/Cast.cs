using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace MovieScrap2000.TMDbScraper
{
	public enum PersonGender
	{
		Unknown = 0,
		Female = 1,
		Male = 2,
		NonBinary = 3
	}

	public class Cast
	{
		[JsonPropertyName("cast_id")]
		public int CastId { get; set; }

		[JsonPropertyName("character")]
		public string? Character { get; set; }

		[JsonPropertyName("credit_id")]
		public string? CreditId { get; set; }

		[JsonPropertyName("id")]
		public int Id { get; set; }

		[JsonPropertyName("name")]
		public string? Name { get; set; }

		[JsonPropertyName("order")]
		public int Order { get; set; }

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

    [JsonIgnore]
    public byte[]? PictureData { get; set; }

    public override string ToString()
		{
			return $"{Name} as {Character} ({Id})";
		}
	}

}
