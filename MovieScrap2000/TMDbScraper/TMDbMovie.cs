using MovieScrap2000.TMDbScraper.TMDbScraper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace MovieScrap2000.TMDbScraper
{
	public class TMDbMovie
	{
		[JsonPropertyName("account_states")]
		public AccountState AccountStates { get; set; } = new AccountState();

		[JsonPropertyName("adult")]
		public bool Adult { get; set; }

		[JsonPropertyName("alternative_titles")]
		public AlternativeTitles AlternativeTitles { get; set; } = new AlternativeTitles();

		[JsonPropertyName("backdrop_path")]
		public string? BackdropPath { get; set; }

		[JsonPropertyName("belongs_to_collection")]
		public SearchCollection BelongsToCollection { get; set; } = new SearchCollection();

		[JsonPropertyName("budget")]
		public long Budget { get; set; }

		[JsonPropertyName("changes")]
		public ChangesContainer Changes { get; set; } = new ChangesContainer();

		[JsonPropertyName("credits")]
		public Credits Credits { get; set; } = new Credits();

		[JsonIgnore]
		public string DirectorsString
		{
			get
			{
				if (Credits != null && Credits.Crew != null)
				{
					var directors = Credits.Crew.Where(c => c.Job != null && c.Job.Equals("Director", StringComparison.OrdinalIgnoreCase)).Select(c => c.Name).ToList();
					if (directors.Count > 0)
					{
						return string.Join("; ", directors);
					}
				}
				return string.Empty;
			}
    }

    [JsonPropertyName("genres")]
		public List<Genre> Genres { get; set; } = new List<Genre>();
		[JsonIgnore]
		public string GenresString
		{
			get
			{
				if (Genres != null && Genres.Count > 0)
				{
					return string.Join("; ", Genres.Select(g => g.Name));
				}
				return string.Empty;
			}
    }

    [JsonPropertyName("homepage")]
		public string? Homepage { get; set; }

		[JsonPropertyName("id")]
		public int Id { get; set; }

		[JsonPropertyName("images")]
		public TMDbImages Images { get; set; } = new TMDbImages();

		[JsonPropertyName("imdb_id")]
		public string? ImdbId { get; set; }

		[JsonPropertyName("keywords")]
		public KeywordsContainer Keywords { get; set; } = new KeywordsContainer();

		[JsonPropertyName("lists")]
		public SearchContainer<ListResult> Lists { get; set; } = new SearchContainer<ListResult>();

		[JsonPropertyName("original_language")]
		public string? OriginalLanguage { get; set; }

		[JsonPropertyName("original_title")]
		public string? OriginalTitle { get; set; }

		[JsonPropertyName("overview")]
		public string? Overview { get; set; }

		[JsonPropertyName("popularity")]
		public double? Popularity { get; set; }

		[JsonPropertyName("poster_path")]
		public string? PosterPath { get; set; }

		[JsonPropertyName("production_companies")]
		public List<ProductionCompany> ProductionCompanies { get; set; } = new List<ProductionCompany>();
		
		[JsonIgnore]
		public string ProductionCompaniesString
		{
			get
			{
				if (ProductionCompanies != null && ProductionCompanies.Count > 0)
				{
					return string.Join("; ", ProductionCompanies.Select(pc => pc.Name));
				}
				return string.Empty;
			}
    }

    [JsonPropertyName("production_countries")]
		public List<ProductionCountry> ProductionCountries { get; set; } = new List<ProductionCountry>();
		
		[JsonIgnore]
		public string ProductionCountriesString
		{
			get
			{
				if (ProductionCountries != null && ProductionCountries.Count > 0)
				{
					return string.Join("; ", ProductionCountries.Select(pc => pc.Name));
				}
				return string.Empty;
			}
    }

    [JsonPropertyName("release_date")]
		public DateTime? ReleaseDate { get; set; }

		[JsonPropertyName("release_dates")]
		public ResultContainer<ReleaseDatesContainer> ReleaseDates { get; set; } = new ResultContainer<ReleaseDatesContainer>();

		[JsonPropertyName("external_ids")]
		public ExternalIdsMovie ExternalIds { get; set; } = new ExternalIdsMovie();

		[JsonPropertyName("releases")]
		public Releases Releases { get; set; } = new Releases();

		[JsonPropertyName("revenue")]
		public long Revenue { get; set; }

		[JsonPropertyName("reviews")]
		public SearchContainer<ReviewBase> Reviews { get; set; } = new SearchContainer<ReviewBase>();

		[JsonPropertyName("runtime")]
		public int? Runtime { get; set; }

		[JsonPropertyName("similar")]
		public SearchContainer<SearchMovie> Similar { get; set; } = new SearchContainer<SearchMovie>();

		[JsonPropertyName("recommendations")]
		public SearchContainer<SearchMovie> Recommendations { get; set; } = new SearchContainer<SearchMovie>();

		[JsonPropertyName("spoken_languages")]
		public List<SpokenLanguage> SpokenLanguages { get; set; } = new List<SpokenLanguage>();

		[JsonPropertyName("status")]
		public string? Status { get; set; }

		[JsonPropertyName("tagline")]
		public string? Tagline { get; set; }

		[JsonPropertyName("title")]
		public string? Title { get; set; }

		[JsonPropertyName("translations")]
		public TranslationsContainer Translations { get; set; } = new TranslationsContainer();

		[JsonPropertyName("video")]
		public bool Video { get; set; }

		[JsonPropertyName("videos")]
		public ResultContainer<Video> Videos { get; set; } = new ResultContainer<Video>();

		[JsonPropertyName("watch/providers")]
		public SingleResultContainer<Dictionary<string, WatchProviders>> WatchProviders { get; set; } = new SingleResultContainer<Dictionary<string, WatchProviders>>();

		[JsonPropertyName("vote_average")]
		public double VoteAverage { get; set; }

		[JsonPropertyName("vote_count")]
		public int VoteCount { get; set; }
	}
}
