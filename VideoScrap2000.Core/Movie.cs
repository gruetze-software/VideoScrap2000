using MovieScrap2000.FanArtTvScraper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;

namespace MovieScrap2000
{
	[XmlRoot("movie")]
	public class Movie
	{
		// --- Neue IDs ---
		[XmlElement("id")]
		public string? Id { get; set; } // IMDb ID des Scrapers

		[XmlElement("tmdb")]
		public string? TmdbId { get; set; } // TMDb ID (oft doppelt mit <uniqueid>)

		[XmlElement("tmdbcolid")]
		public string? TmdbCollectionId { get; set; } // ID der Filmsammlung/Reihe

		// Die Liste von <uniqueid> Objekten (bleibt unverändert)
		[XmlElement("uniqueid")]
		public List<UniqueId> UniqueIds { get; set; } = new List<UniqueId>();

		/// <summary>
		/// Ruft die TMDb ID aus der UniqueIds-Liste ab.
		/// </summary>
		[XmlIgnore]
		public int? TmdbIdFromUnique
		{
			get
			{
				var tmdbIdString = UniqueIds.FirstOrDefault(id => id.Type != null && id.Type.Equals("tmdb", StringComparison.OrdinalIgnoreCase))?.IdValue;
				if (tmdbIdString != null && int.TryParse(tmdbIdString, out int tmdbId))
				{
					return tmdbId;
				}
				return null;
			}
		}

		/// <summary>
		/// Ruft die IMDb ID aus der UniqueIds-Liste ab.
		/// </summary>
		[XmlIgnore]
		public string? ImdbIdFromUnique
		{
			get => UniqueIds.FirstOrDefault(id => id.Type != null && id.Type.Equals("imdb", StringComparison.OrdinalIgnoreCase))?.IdValue;
		}

		/// <summary>
		/// Ruft die Collection ID aus der UniqueIds-Liste ab.
		/// </summary>
		[XmlIgnore]
		public string? TmdbCollectionIdFromUnique
		{
			get => UniqueIds.FirstOrDefault(id => id.Type != null && id.Type.Equals("tmdbcollection", StringComparison.OrdinalIgnoreCase))?.IdValue;
		}

		// --- Allgemeine Metadaten ---
		[XmlElement("title")]
		public string? Title { get; set; }

		[XmlElement("originaltitle")]
		public string? OriginalTitle { get; set; }
        [XmlElement("sorttitle")]
        public string? SortTitle { get; set; }
        [XmlElement("premiered")]
        public string? Premiered { get; set; }
        [XmlElement("tag")]
        public List<string> Tags { get; set; } = [];
        [XmlElement("certification")]
        public string? Certification { get; set; }

		[XmlElement("language")]
		public string? Language { get; set; }

		[XmlElement("year")]
		public string? Year { get; set; }

		[XmlElement("releasedate")]
		public string? ReleaseDate { get; set; } // YYYY-MM-DD
		[XmlIgnore]
		[JsonIgnore]
		public DateTime? ReleaseDateTime
		{
			get
			{
				if (DateTime.TryParse(ReleaseDate, out DateTime dt))
					return dt;

				return null;
			}
		}

		[XmlElement("country")]
		public List<string> Country { get; set; } = new List<string>();
		[XmlIgnore]
		[JsonIgnore]
		public string? DisplayCountries
		{
			get
			{
				if (Country.Any())
					return string.Join("; ", Country);
				return null;
			}
		}

		[XmlElement("rating")]
		// Ändern Sie dies zu string
		public string? RatingString { get; set; }
		// ...

		// Fügen Sie eine Hilfseigenschaft hinzu, um den Wert sicher zu konvertieren
		[XmlIgnore]
		[JsonIgnore]
		public float Rating
		{
			get
			{
				if (!string.IsNullOrEmpty(RatingString) && float.TryParse(RatingString.Replace(',', '.'),
					System.Globalization.NumberStyles.Any,
					System.Globalization.CultureInfo.InvariantCulture, out float result))
					return result;
				else
					return 0f;
			}
      set
      {
        // Konvertiert den float-Wert zurück in einen String (mit Punkt als Dezimaltrennzeichen)
        // und speichert ihn im RatingString.
        RatingString = value.ToString(System.Globalization.CultureInfo.InvariantCulture);
      }
    }

		[XmlElement("votes")]
		public int Votes { get; set; }

		[XmlElement("mpaa")]
		public string? Mpaa { get; set; }

		[XmlElement("genre")]
		public List<string> Genres { get; set; } = new List<string>();

		[XmlIgnore]
		[JsonIgnore]
		public string? DisplayGenres
		{
			get
			{
				if (Genres.Any())
					return string.Join("; ", Genres);
				return null;
			}
		}

		[XmlElement("studio")]
		public List<string> Studios { get; set; } = new List<string>();

		[XmlIgnore]
		[JsonIgnore]
		public string? DisplayStudios
		{
			get
			{
				if (Studios.Any())
					return string.Join("; ", Studios);
				return null;
			}
		}

		[XmlElement("director")]
		public List<string> Directors { get; set; } = new List<string>();

		[XmlIgnore]
		[JsonIgnore]
		public string? DisplayDirectors
		{
			get
			{
				if (Directors.Any())
					return string.Join("; ", Directors);
				return null;
			}
		}

		[XmlElement("credits")]
		public List<string> Credits { get; set; } = new List<string>();

		[XmlIgnore]
		[JsonIgnore]
		public string? DisplayCredits
		{
			get
			{
				if (Credits.Any())
					return string.Join("; ", Credits);
				return null;
			}
		}

		[XmlElement("outline")]
		public string? Outline { get; set; } // Kurze Zusammenfassung

		[XmlElement("tagline")]
		public string? Tagline { get; set; } // Claim/Slogan

		[XmlElement("plot")]
		public string? Plot { get; set; } // Ausführliche Zusammenfassung

		[XmlElement("runtime")]
		public int Runtime { get; set; }

		[XmlElement("dateadded")]
		public string? DateAdded { get; set; } // Datum, wann Film hinzugefügt wurde
		[XmlIgnore]
		[JsonIgnore]
		public DateTime? DateAddedDateTime
		{
			get
			{
				if (DateTime.TryParse(DateAdded, out DateTime dt))
					return dt;

				return null;
			}
		}

		[XmlElement("datemodified")]
		public string? DateModified { get; set; } // Datum der letzten Bearbeitung

		// Die Liste von <actor> Objekten
		[XmlElement("actor")]
		public List<Actor> Actors { get; set; } = new List<Actor>();

		[XmlIgnore]
		[JsonIgnore]
		public List<FanArtBase> FanArtPics { get; set; } = new List<FanArtBase>();

    // Filmreihe/Set-Name
        [XmlElement("set")]
        public MovieSetInfo? Set { get; set; }
        [XmlIgnore]
        public string? MovieSet
        {
            get => Set?.Name ?? Set?.LegacyName;
            set
            {
                if (value is null && Set is null) return;
                Set ??= new(); Set.Name = value; Set.LegacyName = null;
            }
        }

		// Die komplexe Struktur der Dateiinformationen
		[XmlElement("fileinfo")]
		public MovieFileInfo? FileInfo { get; set; }

		// Sperr-Flag
		[XmlElement("locked")]
		public bool Locked { get; set; }

		public Movie()
		{

		}

		public override string ToString()
		{
			return $"{Title} ({Year})";
		}
	}
    public sealed class MovieSetInfo
    {
        [XmlText] public string? LegacyName { get; set; }
        [XmlElement("name")] public string? Name { get; set; }
        [XmlElement("overview")] public string? Overview { get; set; }
    }
}
