using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace MovieScrap2000.FanArtTvScraper
{
  public class FanArtBase
  {
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonIgnore]
    public string? ImageType { get; set; }

    [JsonPropertyName("url")]
    public string? Url { get; set; }

    /// <summary>
    /// Language code (ISO 639-1 or '00' for language-neutral)
    /// </summary>
    [JsonPropertyName("lang")]
    public string? Language { get; set; }

    [JsonPropertyName("likes")]
    public string? Likes { get; set; }

    /// <summary>
    /// Disc number (for moviedisc and cdart only)
    /// </summary>
    [JsonPropertyName("disc")]
    public string? Disk { get; set; }

    /// <summary>
    /// Disc type (for moviedisc only - 'bluray', 'dvd', '3d')
    /// </summary>
    [JsonPropertyName("disc_type")]
    public string? DiskType { get; set; }

    /// <summary>
    /// Size indicator for cdart ('450' or '1000')
    /// </summary>
    [JsonPropertyName("size")]
    public string? Size { get; set; }

    [JsonPropertyName("season")]
    public string? Season { get; set; }

    [JsonPropertyName("added")]
    public string? Added { get; set; }

    [JsonPropertyName("width")]
    public string? Width { get; set; }

    [JsonPropertyName("height")]
    public string? Height { get; set; }

    [JsonIgnore]
    public byte[]? ImageData { get; set; }
    public override string ToString()
    {
      return $"Language={Language}, Size={Size}, Season={Season}, Added={Added}, Width={Width}, Height={Height}";
    }
  }
}
