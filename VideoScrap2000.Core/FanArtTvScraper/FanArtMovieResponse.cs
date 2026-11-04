using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace MovieScrap2000.FanArtTvScraper
{
  public class FanArtMovieResponse
  {
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("tmdb_id")]
    public string? TmDbID { get; set; }

    [JsonPropertyName("imdb_id")]
    public string? ImDbID { get; set; }

    [JsonPropertyName("image_count")]
    public int ImageCount { get; set; }

    [JsonPropertyName("hdmovieclearart")]
    public List<FanArtBase> ClearArt { get; set; } = new List<FanArtBase>();

    [JsonPropertyName("moviethumb")]
    public List<FanArtBase> Thumb { get; set; } = new List<FanArtBase>();

    [JsonPropertyName("moviebanner")]
    public List<FanArtBase> Banner { get; set; } = new List<FanArtBase>();

    [JsonPropertyName("moviedisc")]
    public List<FanArtBase> Disc { get; set; } = new List<FanArtBase>();

    [JsonPropertyName("movieposter")]
    public List<FanArtBase> Poster { get; set; } = new List<FanArtBase>();

    [JsonPropertyName("movielogo")]
    public List<FanArtBase> Logo { get; set; } = new List<FanArtBase>();
    [JsonPropertyName("hdmovielogo")]
    public List<FanArtBase> HdLogo { get; set; } = [];

    [JsonPropertyName("moviebackground")]
    public List<FanArtBase> Background { get; set; } = new List<FanArtBase>();

    public override string ToString()
    {
      return $"{Name} Images: {ImageCount}";
    }
  }
}
