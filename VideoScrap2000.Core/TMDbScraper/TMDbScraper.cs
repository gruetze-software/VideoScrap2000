
using MovieScrap2000.FanArtTvScraper;
using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;


namespace MovieScrap2000.TMDbScraper
{
	[JsonConverter(typeof(JsonStringEnumConverter))]
	public enum MediaType
	{
		Unknown,
		movie = 1,
		tv = 2,
		person = 3,
		episode = 4,
		tv_episode = 5,
		season = 6,
		tv_season = 7,
		collection = 8
	}

	public class TMDbScraperManager : IDisposable
	{
		private readonly string _apitoken = "eyJhbGciOiJIUzI1NiJ9.eyJhdWQiOiI2MzkyNmNlYWVhYTA2ZTVmYmJkY2ZmMDQ3OTQ3OTk4MCIsIm5iZiI6MTU5NDM4NDA1Ni43ODgsInN1YiI6IjVmMDg1ZWI4MWQxYmY0MDAzNjY0ZGY1NyIsInNjb3BlcyI6WyJhcGlfcmVhZCJdLCJ2ZXJzaW9uIjoxfQ.Uuxwt16g25bpFYu2UzSvQrtJaYpPQV8DH5U4RbBlJEU";
		private readonly HttpClient _httpClient;
		private TMDbConfig? _config;
		private readonly FanArtTvScraper.FanArtTvScraper _fanartScraper;
		public TMDbScraperManager(HttpClient? client = null, FanArtTvScraper.FanArtTvScraper? fanart = null)
		{
            _httpClient = client ?? new HttpClient();
            _fanartScraper = fanart ?? new();
			_httpClient.DefaultRequestHeaders.Authorization =
						new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _apitoken);
			_httpClient.BaseAddress = new Uri("https://api.themoviedb.org/3/");

		}

        public void Dispose()
        {
            _httpClient.Dispose();
            _fanartScraper.Client.Dispose();
        }

		public async Task SetConfigAsync()
		{
			// 1. URL kodieren und Basis-Adresse nutzen
			// external_source=imdb_id ist korrekt für /find/
			string url = $"configuration";

			try
			{
				// 2. HTTP-Anfrage senden
				HttpResponseMessage response = await _httpClient.GetAsync(url);

				// Fehler prüfen (4xx oder 5xx Statuscodes)
				response.EnsureSuccessStatusCode();

				// 3. JSON-String lesen
				string jsonString = await response.Content.ReadAsStringAsync();

				// 4. Deserialisierung
				TMDbConfig? config = JsonSerializer.Deserialize<TMDbConfig>(jsonString);
				_config = config;
			}
			catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
			{
				// Spezielle Behandlung für 404 (Nicht gefunden)
				_config = null;
			}
			catch (Exception ex)
			{
				// Loggen Sie den Fehler hier
				Trace.WriteLine($"Fehler beim Abrufen der config: {Utils.GetExcMsg(ex)}");
				throw; // Werfen Sie den Fehler weiter, um ihn im aufrufenden Code zu behandeln
			}
		}

        public async Task<List<SearchMovie>> SearchMoviesAsync(string query, CancellationToken cancellationToken = default, int? year = null, string language = "de-DE")
        {
            var yearFilter = year.HasValue ? $"&year={year.Value}" : "";
            using var response = await _httpClient.GetAsync($"search/movie?query={Uri.EscapeDataString(query)}&language={Uri.EscapeDataString(language)}&include_adult=false{yearFilter}", cancellationToken);
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            return JsonSerializer.Deserialize<SearchContainer<SearchMovie>>(json)?.Results ?? [];
        }

        public async Task<List<SearchTv>> SearchSeriesAsync(string query, string language, CancellationToken token)
        {
            using var response = await _httpClient.GetAsync($"search/tv?query={Uri.EscapeDataString(query)}&language={Uri.EscapeDataString(language)}&include_adult=false", token);
            response.EnsureSuccessStatusCode();
            return JsonSerializer.Deserialize<SearchContainer<SearchTv>>(await response.Content.ReadAsStringAsync(token))?.Results ?? [];
        }

        public Task<JsonElement> GetSeriesDetailsAsync(int id, string language, CancellationToken token) =>
            GetSeriesJsonAsync($"tv/{id}?language={Uri.EscapeDataString(language)}&append_to_response=external_ids,aggregate_credits,content_ratings", token);

        public Task<JsonElement> GetEpisodeDetailsAsync(int id, int season, int episode, string language, CancellationToken token) =>
            GetSeriesJsonAsync($"tv/{id}/season/{season}/episode/{episode}?language={Uri.EscapeDataString(language)}", token);

        public Task<JsonElement> GetSeasonDetailsAsync(int id, int season, string language, CancellationToken token) =>
            GetSeriesJsonAsync($"tv/{id}/season/{season}?language={Uri.EscapeDataString(language)}", token);

        public Task<JsonElement> GetSeriesArtworkAsync(int tmdbId, int tvdbId, CancellationToken token) =>
            _fanartScraper.GetSeriesArtworkAsync(tmdbId, tvdbId, token);

        private async Task<JsonElement> GetSeriesJsonAsync(string path, CancellationToken token)
        {
            using var response = await _httpClient.GetAsync(path, token);
            response.EnsureSuccessStatusCode();
            return JsonSerializer.Deserialize<JsonElement>(await response.Content.ReadAsStringAsync(token));
        }

        public async Task<MovieScrap2000.TMDbScraper.TMDbScraper.TMDbImages> GetMovieImagesAsync(int tmdbId, CancellationToken token = default)
        {
            // This endpoint must have no language filter: language only controls ranking in the UI.
            using var response = await _httpClient.GetAsync($"movie/{tmdbId}/images", token);
            response.EnsureSuccessStatusCode();
            return JsonSerializer.Deserialize<MovieScrap2000.TMDbScraper.TMDbScraper.TMDbImages>(await response.Content.ReadAsStringAsync(token)) ?? new();
        }

		public async Task<List<SearchMovie>> FindMovieByImdbIdAsync(string? imdbId, string language = "de-DE")
		{
			if (string.IsNullOrEmpty(imdbId))
			{
				return new List<SearchMovie>();
			}

			// 1. URL kodieren und Basis-Adresse nutzen
			// external_source=imdb_id ist korrekt für /find/
			string url = $"find/{Uri.EscapeDataString(imdbId)}?external_source=imdb_id&language={Uri.EscapeDataString(language)}";

			try
			{
				// 2. HTTP-Anfrage senden
				HttpResponseMessage response = await _httpClient.GetAsync(url);

				// Fehler prüfen (4xx oder 5xx Statuscodes)
				response.EnsureSuccessStatusCode();

				// 3. JSON-String lesen
				string jsonString = await response.Content.ReadAsStringAsync();

				// 4. Deserialisierung

				// Zuerst in den Container-Typ deserialisieren, der die geschachtelten Ergebnisse enthält
				FindContainer? findResult = JsonSerializer.Deserialize<FindContainer>(jsonString);

				// Überprüfung und Rückgabe der Filmergebnisse
				return findResult?.MovieResults ?? new List<SearchMovie>();
			}
			catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
			{
				// Spezielle Behandlung für 404 (Nicht gefunden)
				return new List<SearchMovie>();
			}
			catch (Exception ex)
			{
				// Loggen Sie den Fehler hier
				Trace.WriteLine($"Fehler beim Abrufen der TMDb Daten für ID {imdbId}: {ex.Message}");
				throw; // Werfen Sie den Fehler weiter, um ihn im aufrufenden Code zu behandeln
			}
		}

		public async Task<TMDbMovie?> GetMovieFullDetailsAsync(int tmdbId, string language = "de")
		{
			if (tmdbId <= 0)
			{
				return null;
			}

			// 1. URL mit Append-Parametern erstellen
			// `language` steuert die Übersetzung von Titeln/Plots.
			// `append_to_response` holt Bilder und Credits (Cast/Crew) in einem Request.
			string url = $"movie/{tmdbId}?language={Uri.EscapeDataString(language)}&append_to_response=images,credits,release_dates,keywords&include_image_language={Uri.EscapeDataString(language.Split('-')[0])},null";

			try
			{
				// 2. HTTP-Anfrage senden und Erfolg prüfen
				HttpResponseMessage response = await _httpClient.GetAsync(url);
				response.EnsureSuccessStatusCode();

				// 3. JSON-String lesen und deserialisieren
				string jsonString = await response.Content.ReadAsStringAsync();

				// Deserialisierung in die MovieDetail-Klasse
				return JsonSerializer.Deserialize<TMDbMovie>(jsonString);
			}
			catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
			{
				// 404 bedeutet, der Film wurde mit dieser ID nicht gefunden.
				return null;
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Fehler beim Abrufen der Details für TMDb ID {tmdbId}: {ex.Message}");
				throw;
			}
		}

		private string? getUrlImageSize(ImageSize size, ImageType type)
		{
			if (_config == null)
				throw new ApplicationException("Zuerst die Config setzen.");

			if (type == ImageType.Backdrop)
			{
				if (_config.Images.BackdropSizes.Count < 4)
					throw new ApplicationException("config.Images.BackdropSizes.Count < 4?");
				switch (size)
				{
					case ImageSize.Thumb: return _config.Images.BackdropSizes[0]; // w300
					case ImageSize.Small:
					case ImageSize.Medium: return _config.Images.BackdropSizes[1]; // w780
					case ImageSize.Large: return _config.Images.BackdropSizes[2]; // w1280
					case ImageSize.Original: return "original";
					default: Debug.Assert(false); return "original";
				}
			}
			else if (type == ImageType.Poster)
			{
				if (_config.Images.PosterSizes.Count < 6)
					throw new ApplicationException("config.Images.PosterSizes.Count < 6?");
				switch (size)
				{
					case ImageSize.Thumb: return _config.Images.PosterSizes[0]; // w92
					case ImageSize.Small: return _config.Images.PosterSizes[2]; // w185
					case ImageSize.Medium: return _config.Images.PosterSizes[3]; // w342
					case ImageSize.Large: return _config.Images.PosterSizes[5]; // w780
					case ImageSize.Original: return "original";
					default: Debug.Assert(false); return "original";
				}
			}
			else if (type == ImageType.Logo)
			{
				if (_config.Images.LogoSizes.Count < 6)
					throw new ApplicationException("config.Images.LogoSizes.Count < 6?");
				switch (size)
				{
					case ImageSize.Thumb: return _config.Images.LogoSizes[0]; // w45
					case ImageSize.Small: return _config.Images.LogoSizes[2]; // w154
					case ImageSize.Medium: return _config.Images.LogoSizes[4]; // w300
					case ImageSize.Large: return _config.Images.LogoSizes[5]; // w500
					case ImageSize.Original: return "original";
					default: Debug.Assert(false); return "original";
				}
			}
			else if (type == ImageType.Profile)
			{
				if (_config.Images.ProfileSizes.Count < 4)
					throw new ApplicationException("config.Images.ProfileSizes.Count < 4?");
				switch (size)
				{
					case ImageSize.Thumb: return _config.Images.ProfileSizes[0]; // w45
					case ImageSize.Small: return _config.Images.ProfileSizes[1]; // w185
					case ImageSize.Medium:
					case ImageSize.Large: return _config.Images.ProfileSizes[2]; // h632
					case ImageSize.Original: return "original";
					default: Debug.Assert(false); return "original";
				}
			}
			else if (type == ImageType.Still)
			{
				if (_config.Images.StillSizes.Count < 4)
					throw new ApplicationException("config.Images.StillSizes.Count < 4?");
				switch (size)
				{
					case ImageSize.Thumb: return _config.Images.StillSizes[0]; // w92
					case ImageSize.Small: return _config.Images.StillSizes[1]; // w185
					case ImageSize.Medium:
					case ImageSize.Large: return _config.Images.StillSizes[2]; // w300
					case ImageSize.Original: return "original";
					default: Debug.Assert(false); return "original";
				}
			}

			Debug.Assert(false, "Unbekannter Image-Typ");
			return null;
		}

		private async Task<string?> BuildTmdbImageUrlAsync(string? imagePath, ImageSize size, ImageType type)
		{
			if ( _config == null )
        await SetConfigAsync();

      // 1. Basis-Validierung
      string? imagesizeurl = getUrlImageSize(size, type);
			if (string.IsNullOrWhiteSpace(imagesizeurl) || string.IsNullOrWhiteSpace(imagePath))
				return null;

			// 2. Segmente bereinigen (Trimmen von Schrägstrichen an den Rändern)
			string baseUrl = _config!.Images.SecureBaseUrl!.Trim('/');
			string sizeUrl = imagesizeurl.Trim('/');
			string path = imagePath?.Trim('/') ?? string.Empty;

			// 3. URLs mit String-Interpolation zusammensetzen
			// Fügt automatisch die notwendigen Schrägstriche zwischen den bereinigten Segmenten ein.
			string cleanPath = $"{baseUrl}/{sizeUrl}/{path}";

			return cleanPath;
		}

		/// <summary>
		/// Lädt eine Bilddatei von TMDb als Byte-Array herunter.
		/// </summary>
		/// <param name="imagePath">Der relative Pfad des Bildes (z.B. /w9Vl58zI52w2zN3.jpg).</param>
		/// <returns>Ein Byte-Array des Bildes oder null, falls der Download fehlschlägt.</returns>
		public async Task<byte[]?> GetImageFromTMDbByteArrayAsync(string? imagePath, ImageSize size, ImageType type, CancellationToken cancellationToken = default)
		{
			// 1. Validierung des Pfades
			if (string.IsNullOrWhiteSpace(imagePath) || imagePath == "null")
				return null;

			if (_config == null)
				await SetConfigAsync();

			if (_config == null || string.IsNullOrEmpty(_config.Images.SecureBaseUrl))
				return null;


			// 2. Vollständige URL erstellen
			// Wenn der Pfad nicht mit '/' beginnt, fügen wir ihn hinzu, um eine korrekte URL zu erhalten.
			// TMDb-Pfade beginnen normalerweise mit einem Slash.
			string? urlimage = await BuildTmdbImageUrlAsync(imagePath, size, type);
			if (string.IsNullOrWhiteSpace(urlimage))
				return null;

			return await GetImageFromUrlByteArrayAsync(urlimage, _httpClient, cancellationToken);
    }

		public async Task<byte[]?> GetImageFromFanArtTvByteArrayAsync(string? url, CancellationToken cancellationToken = default)
		{
      // 1. Validierung des Pfades
      if (string.IsNullOrWhiteSpace(url) || url == "null")
        return null;

			return await GetImageFromUrlByteArrayAsync(url, _fanartScraper.Client, cancellationToken);
    }

    /// <summary>
    /// Lädt eine Bilddatei von einer Url als Byte-Array herunter.
    /// </summary>
    /// <returns>Ein Byte-Array des Bildes oder null, falls der Download fehlschlägt.</returns>
    private async Task<byte[]?> GetImageFromUrlByteArrayAsync(string? url, HttpClient client, CancellationToken cancellationToken)
    {
      // 1. Validierung des Pfades
      if (string.IsNullOrWhiteSpace(url) || url == "null")
        return null;

      try
      {
        // 3. Bilddaten asynchron als Byte-Array herunterladen
        // HttpClient.GetByteArrayAsync ist ideal für Binärdaten wie Bilder.
        byte[] imageBytes = await client.GetByteArrayAsync(url, cancellationToken);
        return imageBytes;
      }
      catch (HttpRequestException ex)
      {
        // Fehler bei der HTTP-Anfrage (z.B. 404 Not Found, Netzwerkfehler)
        // Protokollierung des Fehlers
        Trace.WriteLine($"Fehler beim Herunterladen des Bildes: {Utils.GetExcMsg(ex)}");
        return null;
      }
      catch (Exception ex) when (ex is not OperationCanceledException)
      {
        // Allgemeine Fehlerbehandlung
        Trace.WriteLine($"Unerwarteter Fehler beim Abrufen des Bild-Arrays: {Utils.GetExcMsg(ex)}");
        return null;
      }
    }

    public async Task<FanArtMovieResponse?> GetFanartTvMovieAsync(int tmdbid, CancellationToken cancellationToken = default)
		{
			return await _fanartScraper.GetFanArtMovieAsync(tmdbid, cancellationToken);
    }
	}
}
