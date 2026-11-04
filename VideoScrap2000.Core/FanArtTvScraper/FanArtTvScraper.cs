using MovieScrap2000.FanArtTvScraper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace MovieScrap2000.FanArtTvScraper
{
  public class FanArtTvScraper
  {
    /*
     * Important for Project Developers: When a personal API key is provided, it bypasses any rate limits applied to
     * the project key and provides better access to newly added images. We strongly recommend that projects allow
     * users to optionally enter their personal API key. This gives users control over their API access and improves
     * their experience with faster access to new content. Direct users to get their personal key in your application's settings.
     * https://api.fanart.tv/#section/Fanart.tv-API-Documentation/Authentication
     *
     */
    private readonly static string _baseurl = "https://webservice.fanart.tv/v3.2/";
    private static readonly string? BundledProjectKey = typeof(FanArtTvScraper).Assembly
      .GetCustomAttributes<AssemblyMetadataAttribute>()
      .FirstOrDefault(attribute => attribute.Key == "FanartTvProjectApiKey")?.Value;
    public HttpClient Client { get; private set; }

    public FanArtTvScraper(HttpClient? client = null)
    {
      Client = client ?? new HttpClient();
    }

    /// <summary>
    /// Ruft asynchron alle verfügbaren Fanart-Artworks für einen Film von Fanart.tv ab.
    /// </summary>
    /// <param name="tmdbId">Die TMDb-ID des Films, der abgefragt werden soll.</param>
    /// <param name="cancellationToken">Das Abbruch-Token.</param>
    /// <returns>Ein FanArtMovieResponse-Objekt oder null bei einem Fehler/keinem Ergebnis.</returns>
    public async Task<FanArtMovieResponse?> GetFanArtMovieAsync(
        int tmdbId,
        CancellationToken cancellationToken = default)
    {
      // 1. URL zusammenbauen
      // Endpunkt: /movies/{tmdb_id}?api_key={api_key}
      string url = $"{_baseurl}movies/{tmdbId}";

      try
      {
        cancellationToken.ThrowIfCancellationRequested();

        // 2. Asynchronen HTTP-Aufruf durchführen
        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        var projectKey = Environment.GetEnvironmentVariable("FANART_TV_PROJECT_API_KEY")?.Trim();
        if (string.IsNullOrWhiteSpace(projectKey)) projectKey = BundledProjectKey;
        if (!string.IsNullOrWhiteSpace(projectKey)) request.Headers.Add("api-key", projectKey);

        var personalKey = Environment.GetEnvironmentVariable("FANART_TV_API_KEY")?.Trim();
        if (!string.IsNullOrWhiteSpace(personalKey)) request.Headers.Add("client-key", personalKey);
        using var response = await Client.SendAsync(request, cancellationToken);

        // 3. Statuscode prüfen
        if (!response.IsSuccessStatusCode)
        {
          // Protokollieren Sie den Fehlercode (z.B. 404 Not Found, 500 Server Error)
          System.Diagnostics.Trace.WriteLine($"Fanart.tv API-Fehler (TMDb ID: {tmdbId}): {response.StatusCode}");
          return null;
        }

        // 4. Inhalt asynchron als String lesen
        string jsonContent = await response.Content.ReadAsStringAsync(cancellationToken);

        // 5. JSON deserialisieren
        var options = new JsonSerializerOptions
        {
          PropertyNameCaseInsensitive = true // Fanart.tv nutzt oft Kleinbuchstaben/snake_case
        };

        FanArtMovieResponse? fanArtResponse = JsonSerializer.Deserialize<FanArtMovieResponse>(jsonContent, options);

        // Nicht vorhandene Felder zuordnen
        if (fanArtResponse != null)
        {
          if (fanArtResponse.ClearArt != null)
            foreach (var item in fanArtResponse.ClearArt) item.ImageType = ImageTypes.ClearArt;
          if (fanArtResponse.Background != null)
            foreach (var item in fanArtResponse.Background) item.ImageType = ImageTypes.Fanart;
          if (fanArtResponse.Banner != null)
            foreach (var item in fanArtResponse.Banner) item.ImageType = ImageTypes.Banner;
          if (fanArtResponse.Disc != null)
            foreach (var item in fanArtResponse.Disc) item.ImageType = ImageTypes.DiskArt;
          if (fanArtResponse.Logo != null)
            foreach (var item in fanArtResponse.Logo) item.ImageType = ImageTypes.ClearLogo;
          foreach (var item in fanArtResponse.HdLogo) item.ImageType = ImageTypes.ClearLogo;
          if (fanArtResponse.Poster != null)
            foreach (var item in fanArtResponse.Poster) item.ImageType = ImageTypes.Poster;
          if (fanArtResponse.Thumb != null)
            foreach (var item in fanArtResponse.Thumb) item.ImageType = ImageTypes.KeyArt;
        }
        return fanArtResponse;
      }
      catch (HttpRequestException ex)
      {
        // Netzwerk- oder DNS-Fehler behandeln
        System.Diagnostics.Trace.WriteLine($"HttpRequestException bei Fanart.tv: {ex.Message}");
        return null;
      }
      catch (JsonException ex)
      {
        // Fehler beim Parsen der JSON-Antwort behandeln
        System.Diagnostics.Trace.WriteLine($"JsonException bei Fanart.tv: {ex.Message}");
        return null;
      }
      catch (OperationCanceledException)
      {
        // Abbruch durch CancellationToken
        System.Diagnostics.Trace.WriteLine($"Fanart.tv Abruf abgebrochen.");
        throw; // Optional: Exception weiterwerfen
      }
    }

  }
}
