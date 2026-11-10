using Microsoft.Data.Sqlite;
using MovieScrap2000;
using MovieScrap2000.FanArtTvScraper;
using MovieScrap2000.TMDbScraper;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;

using System.IO;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

public class MetadataService : IDisposable
{
	public Task RemoveCachedMovieAsync(int movieId, CancellationToken token) => _dbManager.ExecuteAsync(async connection =>
	{
		using var command = connection.CreateCommand();
		command.CommandText = "DELETE FROM Movie WHERE Id = @id;";
		command.Parameters.AddWithValue("@id", movieId);
		await command.ExecuteNonQueryAsync(token);
	});

	private readonly DatabaseManager _dbManager;
	private readonly HttpClient _httpClient;

	public MetadataService(DatabaseManager dbManager, HttpClient? httpClient = null)
	{
		_dbManager = dbManager;
		_httpClient = httpClient ?? new HttpClient { Timeout = TimeSpan.FromSeconds(15) };
		_httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/100.0.4896.60 Safari/537.36");
	}

    public void Dispose() => _httpClient.Dispose();
    public Task<byte[]?> GetActorPictureAsync(int actorId, CancellationToken token) => _dbManager.ExecuteAsync(async connection =>
    {
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT Thumb FROM Actor WHERE Id = @Id";
        command.Parameters.AddWithValue("@Id", actorId);
        return await command.ExecuteScalarAsync(token) as byte[];
    });

	// --------------------------------------------------------------------------------
	// Core Repository & Load Operations (Hauptdatenverwaltung)
	// --------------------------------------------------------------------------------

	#region Core Repos & Load Operations
	/// <summary>
	/// DB-Status-Check: Prüft, ob überhaupt Daten im Hauptspeicher (Movie-Tabelle) vorhanden sind.
	/// </summary>
	/// <returns></returns>
	public async Task<bool> IsCachePopulatedAsync()
	{
		// Die Methode fragt den DataBaseManager, den Count als long abzurufen
		long count = await _dbManager.ExecuteScalarAsync<long>(async connection =>
		{
			// Diese Logik läuft INNERHALB des AsyncLock und des using-Blocks des DbManager

			using (var command = connection.CreateCommand())
			{
				command.CommandText = "SELECT COUNT(Id) FROM Movie LIMIT 1;";
				DatabaseManager.TraceSql(command, true);
				// ExecuteScalarAsync wird ausgeführt
				// Der Rückgabewert muss hier explizit in T (long) konvertiert werden
				var ret = await command.ExecuteScalarAsync();
				return ret != null ? Convert.ToInt64(ret) : 0L;
			}
		});

		return count > 0;
	}

	/// <summary>
	/// Bulk-Read: Lädt die Liste der Filme für die Hauptansicht, aber in abgespeckter Form (MovieViewModel).
	/// </summary>
	/// <returns>List<MovieViewModel></returns>
	public async Task<List<MovieViewModel>> LoadMoviesAsync(List<int>? movieIds = null)
	{
		var movies = new List<MovieViewModel>();

		// Prüfen, ob eine Filterliste vorhanden ist und IDs enthält.
		bool filterByIds = movieIds != null && movieIds.Any();

		// Wenn eine leere, aber spezifische Filterung gewünscht ist, sofort leere Liste zurückgeben.
		if (filterByIds && movieIds!.Count == 0)
		{
			return movies;
		}

		// ----------------------------------------------------
		// 1. Erstellung der WHERE-Klausel und der Platzhalter
		// ----------------------------------------------------
		string whereClause = string.Empty;
		var parameterNames = new Dictionary<string, object>();

		if (filterByIds)
		{
			// Dynamische Generierung von Platzhaltern für die IN-Klausel: @Id_0, @Id_1, ...
			var placeholders = movieIds!.Select((id, index) => $"@Id_{index}").ToList();
			whereClause = $" WHERE M.Id IN ({string.Join(", ", placeholders)})";

			// Parameter mit den tatsächlichen IDs befüllen
			for (int i = 0; i < movieIds!.Count; i++)
			{
				parameterNames.Add($"@Id_{i}", movieIds[i]);
			}
		}

		// ----------------------------------------------------
		// 2. Erstellung der vollständigen SQL-Abfrage
		// ----------------------------------------------------
		var query = $@"SELECT M.Id, M.Title, M.Year, M.Rating, M.Runtime, M.FolderPath, M.Plot, M.VideoFilePath, ";
		query += "GROUP_CONCAT(G.Name, ', ') AS GenresString, M.ScrapStatus FROM Movie M LEFT JOIN MovieGenre MG ON M.Id = MG.MovieId ";
		query += $"LEFT JOIN Genre G ON MG.GenreId = G.Id {whereClause} GROUP BY ";
		query += "M.Id, M.Title, M.Year, M.Rating, M.Runtime, M.FolderPath, M.Plot, M.VideoFilePath ORDER BY M.Title ASC;";

		// ----------------------------------------------------
		// 3. Ausführung der Abfrage
		// ----------------------------------------------------
		movies = await _dbManager.ExecuteReaderAsync<List<MovieViewModel>>(async connection =>
		{
			using (var command = connection.CreateCommand())
			{
				command.CommandText = query;

				// Parameter binden, falls Filterung aktiv ist
				foreach (var kvp in parameterNames)
				{
					command.Parameters.AddWithValue(kvp.Key, kvp.Value);
				}

				// Logging des vollständigen SQL-Statements (mit substituierten Parametern, wenn möglich)
				// Dies nutzt die Hilfsmethode, die wir zuvor besprochen haben
				DatabaseManager.TraceSql(command);

				using (var reader = await command.ExecuteReaderAsync())
				{
					var localMovies = new List<MovieViewModel>();
					while (await reader.ReadAsync())
					{
						// [Leselogik bleibt unverändert]
						localMovies.Add(new MovieViewModel
						{
							Id = reader.GetInt32(0),
							Title = reader.GetString(1),
							Year = reader.IsDBNull(2) ? string.Empty : reader.GetString(2),

							Rating = reader.IsDBNull(3) ? 0.0f : reader.GetFloat(3),
							Runtime = reader.IsDBNull(4) ? string.Empty : reader.GetInt32(4).ToString(),

							FolderPath = reader.GetString(5),
							Plot = reader.IsDBNull(6) ? string.Empty : reader.GetString(6),

							VideoFilePath = reader.IsDBNull(7) ? string.Empty : reader.GetString(7),
							GenresString = reader.IsDBNull(8) ? string.Empty : reader.GetString(8),
                            IsPlaceholder = reader.GetInt32(9) == 1
						});
					}
					return localMovies;
				}
			}
		});

		return movies;
	}

	/// <summary>
	/// Einzel-Check: Prüft, ob ein Film über den FolderPath existiert und gibt die ID zurück.
	/// </summary>
	/// <param name="folderPath"></param>
	/// <param name="cancellationToken"></param>
	/// <returns>DB-Id des Films</returns>
	public async Task<int> CheckMovieExistsAsync(string folderPath, CancellationToken cancellationToken)
	{
		object? result = await _dbManager.ExecuteScalarAsync<object?>(async connection =>
		{
			var query = "SELECT Id FROM Movie WHERE FolderPath = @FolderPath LIMIT 1;";

			using (var command = connection.CreateCommand())
			{
				command.CommandText = query;
				command.Parameters.AddWithValue("@FolderPath", folderPath);
				DatabaseManager.TraceSql(command, true);
				// Führe die Skalarabfrage asynchron aus.
				// Beachten Sie, dass das CancellationToken hier übergeben wird.
				return await command.ExecuteScalarAsync(cancellationToken);
			}
		});

		// Nach dem Entsperren und der Rückgabe:
		// Wenn der Film existiert, wird die ID (als object) zurückgegeben, sonst null.
		// Convert.ToInt32 kann das Ergebnis sicher in int konvertieren (oder 0, falls null)
		return result != null ? Convert.ToInt32(result) : 0;
	}

	/// <summary>
	/// Detail-Read: Lädt alle Details eines Films, insbesondere die deserialisierten
	/// JSON-Metadaten und die verknüpften N:M-Daten.
	/// </summary>
	/// <param name="movieId"></param>
	/// <returns></returns>
	/// <exception cref="Exception"></exception>
	public async Task<(Movie Nfo, List<Actor> Actors)> LoadFullMovieDetailsAsync(int movieId)
	{
		return await _dbManager.ExecuteAsync<(Movie Nfo, List<Actor> Actors)>(async connection =>
		{
			// --- 1. JSON-Metadaten laden ---
			string? json = null;
			using (var command = connection.CreateCommand())
			{
				command.CommandText = "SELECT JsonMetadata FROM Movie WHERE Id = @Id";
				command.Parameters.AddWithValue("@Id", movieId);

				// ExecuteScalarAsync muss hier direkt ausgeführt und awaitet werden.
				DatabaseManager.TraceSql(command, true);
				var result = await command.ExecuteScalarAsync();
				json = result as string;
			}

			Movie? nfo = string.IsNullOrWhiteSpace(json) ? new Movie() : NfoSerializer.Deserialize<Movie>(json);

			if (nfo == null)
			{
				throw new Exception($"Konnte NFO-Metadaten für MovieId {movieId} nicht deserialisieren.");
			}

			// --- 2. Actors laden (Reader-Abfrage) ---
			var actorQuery = @"SELECT A.Id, A.Name, MA.Role, MA.SortOrder FROM MovieActor MA JOIN Actor A ON MA.ActorId = A.Id ";
			actorQuery += @"WHERE MA.MovieId = @Id ORDER BY MA.SortOrder ASC;";

			var localActors = new List<Actor>();
			using (var command = connection.CreateCommand())
			{
				command.CommandText = actorQuery;
				command.Parameters.AddWithValue("@Id", movieId);
				DatabaseManager.TraceSql(command, true);
				using (var reader = await command.ExecuteReaderAsync())
				{
					while (await reader.ReadAsync())
					{
						localActors.Add(new Actor
						{
							Id = reader.GetInt32(0),
							Name = reader.GetString(1),
							Role = reader.IsDBNull(2) ? null : reader.GetString(2),
							Order = reader.GetInt32(3),
                            TmdbId = nfo.Actors.FirstOrDefault(a => a.Name == reader.GetString(1))?.TmdbId,
                            Thumb = nfo.Actors.FirstOrDefault(a => a.Name == reader.GetString(1))?.Thumb
						});
					}
				}
			}

			// --- 3. Rückgabe der kombinierten Ergebnisse ---
			return (nfo, localActors);
		});
	}


	/// <summary>
	/// Ruft eine Liste von Movie-IDs ab, die dem angegebenen Actor zugeordnet sind.
	/// Dies ist die effizientere Methode, da sie direkt die ActorId verwendet.
	/// </summary>
	/// <param name="actorId">Die eindeutige ID des Actors.</param>
	/// <param name="cancellationToken">Token zur Abbruchsteuerung.</param>
	/// <returns>Eine Liste von Ganzzahlen (MovieIds).</returns>
	public async Task<List<int>> GetMovieIdsByActorIdAsync(
			int actorId,
			CancellationToken cancellationToken)
	{
		// Die Abfrage kann direkt auf der Junction-Tabelle MovieActor ausgeführt werden.
		var selectQuery = @"SELECT MovieId FROM MovieActor WHERE ActorId = @ActorId;";

		// Wir verwenden ExecuteReaderAsync für das Auslesen der Liste.
		var movieIds = await _dbManager.ExecuteReaderAsync<List<int>>(async connection =>
		{
			var localMovieIds = new List<int>();

			using (var command = connection.CreateCommand())
			{
				command.CommandText = selectQuery;
				// Parameterbindung schützt vor SQL Injection
				command.Parameters.AddWithValue("@ActorId", actorId);

				DatabaseManager.TraceSql(command, true);

				using (var reader = await command.ExecuteReaderAsync(cancellationToken))
				{
					while (await reader.ReadAsync())
					{
						localMovieIds.Add(reader.GetInt32(0));
					}
				}
			}
			return localMovieIds;
		});

		return movieIds;
	}

	#endregion

	// --------------------------------------------------------------------------------
	// Speichern und Aktualisieren des Films (Transaktionssicher)
	// --------------------------------------------------------------------------------

	#region Save und Update Movie
	/// <summary>
	/// Führt ein Upsert (Insert oder Update) der Hauptfilminformationen durch.
	/// </summary>
	/// <param name="movieNfo"></param>
	/// <param name="folderPath"></param>
	/// <param name="videoFilePath"></param>
	/// <param name="isPlaceholder"></param>
	/// <param name="cancellationToken"></param>
	/// <returns></returns>
	/// <exception cref="Exception"></exception>
    public async Task RemoveArtworkCacheAsync(int movieId, IEnumerable<string> types, CancellationToken token)
    {
        await _dbManager.ExecuteAsync(async connection =>
        {
            foreach (var type in types)
            {
                using var command = connection.CreateCommand();
                command.CommandText = "DELETE FROM ImageCache WHERE MovieId = @id AND Type = @type COLLATE NOCASE;";
                command.Parameters.AddWithValue("@id", movieId);
                command.Parameters.AddWithValue("@type", type);
                await command.ExecuteNonQueryAsync(token);
            }
        });
    }

	public async Task SaveMovieAsync(
			Movie movieNfo,
			string folderPath,
			string videoFilePath,
			bool isPlaceholder,
			CancellationToken cancellationToken, bool writeFiles = true,
            Func<string, string, ImageCacheItem, CancellationToken, Task>? saveImage = null)
	{

		if (writeFiles && !isPlaceholder)
        {
            await MovieManager.SaveNfoFileAsync(movieNfo, folderPath, videoFilePath, cancellationToken);
            await MovieManager.SynchronizeMovieImagesAsync(folderPath,
                Path.GetFileNameWithoutExtension(videoFilePath), movieNfo.FanArtPics
                .Where(p => p.ImageData is { Length: > 0 })
                .Select(p => new ImageCacheItem { Type = p.ImageType ?? "", Data = p.ImageData! }).ToList(), cancellationToken, saveImage);
        }

        // Die gesamte Speicherlogik läuft nun im thread-sicheren, asynchronen Block
    Trace.WriteLine($"Aktualisiere DB ({movieNfo.Title})");
    await _dbManager.ExecuteAsync(async connection =>
		{
			// Starte eine Transaktion innerhalb der gesicherten Verbindung
			using (var transaction = connection.BeginTransaction())
			{
				try
				{
					// 1. Haupttabelle Movie speichern (Upsert) und MovieId abrufen
					// ACHTUNG: UpsertMovieMainAsync muss nun die Transaktion akzeptieren.
					// ACHTUNG: Diese Methode muss ASYNCHRON sein.

					int movieId = await UpsertMovieMainAsync(connection, transaction, movieNfo, folderPath, videoFilePath, isPlaceholder);

					// 2. VERBINDUNGSTABELLEN NUR SPEICHERN, WENN KEIN PLATZHALTER
					if (!isPlaceholder)
					{
						// ------------------------------------------------------------------------
						// NEU: Alte Verknüpfungen löschen (Actors, Genres etc. für diese MovieId)
						// Dies ist essentiell, um Actors zu entfernen, die im NFO-Objekt fehlen!
						// ------------------------------------------------------------------------
						await DeleteMovieJunctionsAsync(connection, transaction, movieId, cancellationToken);

						// 3. Neue Verknüpfungen speichern (Insert oder Upsert)
						// Die Methode wird umbenannt, um klar zu stellen, dass sie die aktuellen Daten speichert.
						await InsertMovieJunctionsAsync(connection, transaction, movieId, movieNfo, cancellationToken);
					}

					// 4. Transaktion abschließen
					transaction.Commit();

          Trace.WriteLine($"Aktualisierung abgeschlossen ({movieNfo.Title})");
        }
				catch (Exception ex)
				{
					// Bei Fehlern die Transaktion zurückrollen
					transaction.Rollback();

					// Wir werfen die Exception HIER weiter. Die Fehlerbehandlung in der aufrufenden
					// Methode kann die Transaktionsdetails hinzufügen, wie es zuvor der Fall war.
					System.Diagnostics.Trace.WriteLine($"Fehler in SaveMovieAsync: {Utils.GetExcMsg(ex)}");
					throw;
				}
			}
		});
	}


	/// <summary>
	/// Aktualisiert die Datenbankeinträge für einen bestehenden Film anhand eines bereits
	/// geparsten Movie-Objekts (NFO-Daten).
	/// </summary>
	/// <param name="movieId">Die ID des Films, der aktualisiert werden soll.</param>
	/// <param name="movieNfo">Das bereits geparste Movie-Objekt mit den neuen NFO-Daten.</param>
	/// <param name="folderPath">Der Ordnerpfad des Films (für UpdateMovieMainOnlyAsync).</param>
	/// <param name="cancellationToken">Das Abbruch-Token.</param>
	/// <returns>True, wenn die Aktualisierung erfolgreich war, sonst False.</returns>
	public async Task<bool> UpdateMovieFromNfoAsync(
			int movieid,
			Movie movieNfo,
			string folderPath,
			string videoFilePath,
			CancellationToken cancellationToken)
	{
		if (movieNfo == null || movieid <= 0) return false;

		// Die gesamte Aktualisierungslogik läuft nun im thread-sicheren, asynchronen Block
		return await _dbManager.ExecuteAsync(async connection =>
		{
			// Starte eine Transaktion innerhalb der gesicherten Verbindung
			using (var transaction = connection.BeginTransaction())
			{
				try
				{
					// A) Aktualisiere die Haupttabelle (Movie)
					await UpdateMovieMainOnlyAsync(
							connection,
							transaction,
							movieNfo,
							folderPath,
							videoFilePath,
							cancellationToken);

					// B) Alte Verknüpfungen löschen
					// Muss die Connection und Transaktion verwenden
					await DeleteMovieJunctionsAsync(connection, transaction, movieid, cancellationToken);

					// C) Neue Verknüpfungen einfügen
					// Muss die Connection und Transaktion verwenden
					await InsertMovieJunctionsAsync(connection, transaction, movieid, movieNfo, cancellationToken);

					// 2. Transaktion abschließen
					transaction.Commit();
					return true; // Erfolg
				}
				catch (OperationCanceledException)
				{
					transaction.Rollback();
					throw;
				}
				catch (Exception ex)
				{
					// Bei Fehlern die Transaktion zurückrollen
					transaction.Rollback();

					// Logging bleibt hier, da die Information über den Fehler im Kontext des Locks nützlich ist
					Trace.WriteLine($"DB-Update fehlgeschlagen für Film {movieid}: {Utils.GetExcMsg(ex)}");

					// Wir werfen die Exception HIER nicht weiter, da die Methode false zurückgeben soll.
					// ACHTUNG: Die Methode wirft die Exception intern weiter, um den AsyncLock freizugeben.
					// Wenn ExecuteAsync die Exception abfängt und false zurückgibt, ist es ok.
					// Da unsere ExecuteAsync<T> aber throw zulässt, müssen wir hier einen Wert zurückgeben.
					return false;
				}
			}
		});
	}


	/// <summary>
	/// Die Kernlogik für INSERT OR REPLACE/UPDATE der Movie-Tabelle.
	/// </summary>
	/// <param name="connection"></param>
	/// <param name="transaction"></param>
	/// <param name="nfo"></param>
	/// <param name="folderPath"></param>
	/// <param name="videoFilePath"></param>
	/// <param name="isPlaceholder"></param>
	/// <returns></returns>
	/// <exception cref="InvalidOperationException"></exception>
	private async Task<int> UpsertMovieMainAsync(
		SqliteConnection connection,
		SqliteTransaction transaction,
		Movie nfo,
		string folderPath,
		string videoFilePath,
		bool isPlaceholder)
	{
		Trace.WriteLine($"UpsertMovieMainAsync() ({nfo.Title})");

		// 1. Vorbereiten der variablen Daten
		string now = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"); // Datum/Zeit für DB

		// JSON muss immer (auch für Platzhalter) versucht werden zu serialisieren,
		// aber nur für vollwertige Filme gespeichert werden.
		string? nfoJsonSerialized = NfoSerializer.Serialize(nfo);

    // Annahme: Die NFO ist immer movie.nfo oder der Name des Videos.
    string videoFileNameWithoutExtension = Path.GetFileNameWithoutExtension(videoFilePath);
    string nfoFileName = $"{videoFileNameWithoutExtension}.nfo";

    // Fallback auf den Standardnamen, falls der Video-Dateiname nicht ermittelt werden kann
    if (string.IsNullOrEmpty(videoFileNameWithoutExtension) )
    {
      nfoFileName = "movie.nfo";
    }
    string nfoFilePath = Path.Combine(folderPath, nfoFileName);
    long nfoModifiedTimestamp = new FileInfo(nfoFilePath).LastWriteTimeUtc.Ticks;

		// IDs
		var imdbUniqueId = nfo.UniqueIds?
				.FirstOrDefault(id => id.Type != null && id.Type.Equals("imdb", StringComparison.OrdinalIgnoreCase));
		var tmdbUniqueId = nfo.UniqueIds?
				.FirstOrDefault(id => id.Type != null && id.Type.Equals("tmdb", StringComparison.OrdinalIgnoreCase));

		// Status
		int scrapStatus = isPlaceholder ? 1 : 0;

		// Platzhalter-Werte auf NULL oder Standard setzen
		string title = nfo.Title ?? Path.GetFileName(folderPath) ?? "Unbekannter Film"; // Titel ist Pflicht
		string? originalTitle = isPlaceholder ? null : nfo.OriginalTitle;
		string? year = nfo.Year;
		string? plot = nfo.Plot;
		string? outline = isPlaceholder ? null : nfo.Outline;
		string? tagline = isPlaceholder ? null : nfo.Tagline;
		string? mpaa = isPlaceholder ? null : nfo.Mpaa;
		float? rating = isPlaceholder ? null : nfo.Rating;
		int? runtime = isPlaceholder ? null : nfo.Runtime;
		string? jsonToSave = nfoJsonSerialized;

		// 2. SQL-Befehl (ALLE SPALTEN MÜSSEN HIER UND UNTEN ENTHALTEN SEIN)
		using (var command = connection.CreateCommand())
		{
			command.Transaction = transaction;

			var upsertQuery = $@"INSERT INTO Movie (Title, OriginalTitle, Year, FolderPath, VideoFilePath, Plot, Outline, Tagline, Rating, Runtime, Mpaa, ImdbId, TMDbId, JsonMetadata, NfoModified, DateAdded, DateModified, ScrapStatus) ";
			upsertQuery += $@"VALUES (@Title, @OriginalTitle, @Year, @FolderPath, @VideoFilePath, @Plot, @Outline, @Tagline, @Rating, @Runtime, @Mpaa, @ImdbId, @TMDbId, @JsonMetadata, @NfoModified, @NowAdded, @NowModified, @ScrapStatus) ";
			upsertQuery += $@"ON CONFLICT(FolderPath) DO UPDATE SET Title = excluded.Title, OriginalTitle = excluded.OriginalTitle, Year = excluded.Year, ";
			upsertQuery += $@"VideoFilePath = excluded.VideoFilePath, Plot = excluded.Plot, Outline = excluded.Outline, Tagline = excluded.Tagline, Rating = excluded.Rating, ";
			upsertQuery += $@"Runtime = excluded.Runtime, Mpaa = excluded.Mpaa, ImdbId = excluded.ImdbId, TMDbId = excluded.TMDbId, JsonMetadata = excluded.JsonMetadata, ";
			upsertQuery += $@"NfoModified = excluded.NfoModified, DateModified = excluded.DateModified, ";
			upsertQuery += "DateAdded = CASE WHEN Movie.ScrapStatus = 1 AND excluded.ScrapStatus = 0 THEN excluded.DateAdded ELSE Movie.DateAdded END, ";
			upsertQuery += $@"ScrapStatus = CASE ";
			upsertQuery += $@"WHEN Movie.ScrapStatus = 1 AND excluded.ScrapStatus = 0 THEN 0 ";
			upsertQuery += $@"ELSE excluded.ScrapStatus END; ";
			upsertQuery += $@"SELECT Id FROM Movie WHERE FolderPath = @FolderPath;";

			command.CommandText = upsertQuery;

			// 3. Parameterbindung (mit korrekter Zuweisung der Platzhalter-Werte)

			// IDs
			command.Parameters.AddWithValue("@ImdbId", (object?)imdbUniqueId?.IdValue ?? DBNull.Value);
			// TMDb-ID muss als INTEGER gespeichert werden, falls die Spalte so definiert ist.
			object tmdbIdParam = DBNull.Value;
			if (tmdbUniqueId != null && int.TryParse(tmdbUniqueId.IdValue, out int tmdbId))
			{
				tmdbIdParam = tmdbId;
			}
			command.Parameters.AddWithValue("@TMDbId", tmdbIdParam);

			// String-Felder verwenden die Platzhalter-Variablen
			command.Parameters.AddWithValue("@Title", title);
			command.Parameters.AddWithValue("@OriginalTitle", (object?)originalTitle ?? DBNull.Value);
			command.Parameters.AddWithValue("@Year", (object?)year ?? DBNull.Value);
			command.Parameters.AddWithValue("@Plot", (object?)plot ?? DBNull.Value);
			command.Parameters.AddWithValue("@Outline", (object?)outline ?? DBNull.Value);
			command.Parameters.AddWithValue("@Tagline", (object?)tagline ?? DBNull.Value);
			command.Parameters.AddWithValue("@Mpaa", (object?)mpaa ?? DBNull.Value);

			// Numerische Felder
			command.Parameters.AddWithValue("@Rating", (object?)rating ?? DBNull.Value);
			command.Parameters.AddWithValue("@Runtime", (object?)runtime ?? DBNull.Value);

			// System-Parameter
			command.Parameters.AddWithValue("@FolderPath", folderPath);
			command.Parameters.AddWithValue("@VideoFilePath", videoFilePath);
			command.Parameters.AddWithValue("@NfoModified", nfoModifiedTimestamp);
			command.Parameters.AddWithValue("@JsonMetadata", (object?)jsonToSave ?? DBNull.Value);
			command.Parameters.AddWithValue("@ScrapStatus", scrapStatus);

			// Zeitstempel
			command.Parameters.AddWithValue("@NowAdded", DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture));
			command.Parameters.AddWithValue("@NowModified", now);

			DatabaseManager.TraceSql(command, true);
			// 4. Ausführen und Id abrufen
			var result = await command.ExecuteScalarAsync();

			if (result == null)
			{
				throw new InvalidOperationException($"Konnten MovieId für {nfo.Title} nicht abrufen.");
			}
			return Convert.ToInt32(result);
		}
	}


	/// <summary>
	/// Führt ein reines UPDATE der Haupttabelle durch, wenn die ID bekannt ist.
	/// </summary>
	/// <param name="connection"></param>
	/// <param name="transaction"></param>
	/// <param name="nfo"></param>
	/// <param name="folderPath"></param>
	/// <param name="videoFilePath"></param>
	/// <param name="cancellationToken"></param>
	/// <returns></returns>
	private async Task UpdateMovieMainOnlyAsync(
		SqliteConnection connection,
		SqliteTransaction transaction,
		Movie nfo,
		string folderPath,
		string videoFilePath,
		CancellationToken cancellationToken)
	{
		Trace.WriteLine($"UpdateMovieMainOnlyAsync() ({nfo.Title})");
		// 1. Vorbereiten der variablen Daten (Übernahme aus UpsertMovieMainAsync)
		string now = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");

		// JSON muss serialisiert werden
		string? nfoJsonSerialized = NfoSerializer.Serialize(nfo);

		// NFO-Modifiziert-Zeitstempel (Falls die NFO-Datei existiert, sonst DB-Wert beibehalten)
		long nfoModifiedTimestamp = 0;
		// Annahme: Die NFO ist immer movie.nfo oder der Name des Videos.
		string videoFileNameWithoutExtension = Path.GetFileNameWithoutExtension(videoFilePath);
		string nfoFileName = $"{videoFileNameWithoutExtension}.nfo";

		// Fallback auf den Standardnamen, falls der Video-Dateiname nicht ermittelt werden kann
		if (string.IsNullOrEmpty(videoFileNameWithoutExtension))
		{
			nfoFileName = "movie.nfo";
		}
		string nfoFilePath = Path.Combine(folderPath, nfoFileName);
		try
		{
			nfoModifiedTimestamp = new FileInfo(nfoFilePath).LastWriteTimeUtc.Ticks;
		}
		catch
		{ /* Datei nicht gefunden / Fehler, setze auf 0 oder lade alten Wert.
             * Hier ignorieren wir den Fehler und verwenden den NFO-Wert aus der Transaktion. */
		}

		// IDs
		var imdbUniqueId = nfo.UniqueIds?
				.FirstOrDefault(id => id.Type != null && id.Type.Equals("imdb", StringComparison.OrdinalIgnoreCase));
		var tmdbUniqueId = nfo.UniqueIds?
				.FirstOrDefault(id => id.Type != null && id.Type.Equals("tmdb", StringComparison.OrdinalIgnoreCase));

		// Die geparsten NFO-Werte verwenden (keine Platzhalter-Logik nötig, da wir aktualisieren)
		string title = nfo.Title ?? Path.GetFileName(folderPath) ?? "Unbekannter Film";
		string? originalTitle = nfo.OriginalTitle;
		string? year = nfo.Year;
		string? plot = nfo.Plot;
		string? outline = nfo.Outline;
		string? tagline = nfo.Tagline;
		string? mpaa = nfo.Mpaa;
		float? rating = nfo.Rating;
		int? runtime = nfo.Runtime;
		string? jsonToSave = nfoJsonSerialized;
		// ScrapStatus wird auf 0 (Complete) gesetzt, da die NFO erfolgreich gelesen wurde
		int scrapStatus = 0;

		// 2. SQL-Befehl (UPDATE)
		using (var command = connection.CreateCommand())
		{
			command.Transaction = transaction;

			var updateQuery = $@"UPDATE Movie SET Title = @Title, OriginalTitle = @OriginalTitle, Year = @Year, VideoFilePath = @VideoFilePath, ";
			updateQuery += $@"Plot = @Plot, Outline = @Outline, Tagline = @Tagline, Rating = @Rating, Runtime = @Runtime, Mpaa = @Mpaa, ImdbId = @ImdbId, ";
			updateQuery += $@"TMDbId = @TMDbId, JsonMetadata = @JsonMetadata, NfoModified = @NfoModified, DateModified = @NowModified, ScrapStatus = @ScrapStatus, ";
			updateQuery += "DateAdded = CASE WHEN ScrapStatus = 1 THEN @NowModified ELSE DateAdded END ";
			updateQuery += @"WHERE FolderPath = @FolderPath;";

			command.CommandText = updateQuery;

			// 3. Parameterbindung

			// IDs
			command.Parameters.AddWithValue("@ImdbId", (object?)imdbUniqueId?.IdValue ?? DBNull.Value);
			object tmdbIdParam = DBNull.Value;
			if (tmdbUniqueId != null && int.TryParse(tmdbUniqueId.IdValue, out int tmdbId))
			{
				tmdbIdParam = tmdbId;
			}
			command.Parameters.AddWithValue("@TMDbId", tmdbIdParam);

			// String-Felder
			command.Parameters.AddWithValue("@Title", title);
			command.Parameters.AddWithValue("@OriginalTitle", (object?)originalTitle ?? DBNull.Value);
			command.Parameters.AddWithValue("@Year", (object?)year ?? DBNull.Value);
			command.Parameters.AddWithValue("@Plot", (object?)plot ?? DBNull.Value);
			command.Parameters.AddWithValue("@Outline", (object?)outline ?? DBNull.Value);
			command.Parameters.AddWithValue("@Tagline", (object?)tagline ?? DBNull.Value);
			command.Parameters.AddWithValue("@Mpaa", (object?)mpaa ?? DBNull.Value);

			// Numerische Felder
			command.Parameters.AddWithValue("@Rating", (object?)rating ?? DBNull.Value);
			command.Parameters.AddWithValue("@Runtime", (object?)runtime ?? DBNull.Value);

			// System-Parameter
			command.Parameters.AddWithValue("@FolderPath", folderPath);
			// Da wir den Film aktualisieren, verwenden wir den VideoFilePath aus dem NFO-Objekt oder lassen ihn unverändert
			command.Parameters.AddWithValue("@VideoFilePath", (object?)videoFilePath ?? DBNull.Value);
			command.Parameters.AddWithValue("@NfoModified", nfoModifiedTimestamp);
			command.Parameters.AddWithValue("@JsonMetadata", (object?)jsonToSave ?? DBNull.Value);
			command.Parameters.AddWithValue("@ScrapStatus", scrapStatus);

			// Zeitstempel
			command.Parameters.AddWithValue("@NowModified", now);
			DatabaseManager.TraceSql(command, true);
			// 4. Ausführen
			await command.ExecuteNonQueryAsync(cancellationToken);
		}
	}

	#endregion

	// --------------------------------------------------------------------------------
	// Normalisierung und Verknüpfung (Junctions)
	// --------------------------------------------------------------------------------

	#region Normalisierung und Verknüpfung
	/// <summary>
	/// Steuert die Normalisierung der Stammdaten (Genre, Studio, Actor etc.) und das
	/// anschließende Einfügen in die Verknüpfungstabellen.
	/// </summary>
	/// <param name="connection"></param>
	/// <param name="transaction"></param>
	/// <param name="movieId"></param>
	/// <param name="movieNfo"></param>
	/// <param name="cancellationToken"></param>
	/// <returns></returns>
	private async Task InsertMovieJunctionsAsync(
		SqliteConnection connection,
		SqliteTransaction transaction,
		int movieId,
		Movie movieNfo,
		CancellationToken cancellationToken)
	{
    Trace.WriteLine($"InsertMovieJunctionsAsync() ({movieNfo.Title})");
    // =========================================================================
    // 1. 🎭 SPEZIELLE BEHANDLUNG: Actors (wegen Role und SortOrder)
    // =========================================================================
    if (movieNfo.Actors != null && movieNfo.Actors.Any())
		{
			// Wir iterieren durch die Actors im NFO-Objekt
			foreach (var actor in movieNfo.Actors)
			{
				// 1. Upsert des Actors in die Actor-Tabelle (Name, TMDbId, Thumb)
				// Annahme: Diese Methode ist spezialisiert auf Actor-Daten und gibt die ID zurück.
				int actorId = await UpsertActorWithThumbAsync(
						connection, transaction, actor.Name, actor.TmdbId, actor.Thumb, actor.ThumbData, cancellationToken);

				// 2. Einfügen des Links in die MovieActor-Tabelle (inkl. Role und Order)
				var insertActorQuery = "INSERT OR IGNORE INTO MovieActor (MovieId, ActorId, Role, SortOrder) VALUES (@MovieId, @ActorId, @Role, @Order);";

				using (var cmd = connection.CreateCommand())
				{
					cmd.Transaction = transaction;
					cmd.CommandText = insertActorQuery;
					cmd.Parameters.AddWithValue("@MovieId", movieId);
					cmd.Parameters.AddWithValue("@ActorId", actorId);
					cmd.Parameters.AddWithValue("@Role", (object?)actor.Role ?? DBNull.Value);
					cmd.Parameters.AddWithValue("@Order", actor.Order);
					DatabaseManager.TraceSql(cmd, true);
					await cmd.ExecuteNonQueryAsync(cancellationToken);
				}

			}
		}

		// =========================================================================
		// 2. 🎬 GENERISCHE BEHANDLUNG: Directors, Studios, Genres, Countries, Credits
		// =========================================================================

		// Alle anderen Verknüpfungen, die nur eine Name-Spalte in der Stammdaten-Tabelle haben
		// und keine zusätzlichen Felder in der Junction-Tabelle (Role, Order etc.).

		// Hier verwenden wir eine generische Upsert-Methode, die Name-Listen verarbeitet
		// und eine Liste von IDs zurückgibt, die dann an Ihre generische Einfüge-Methode weitergegeben wird.

		// --- Directors ---
		var directorNames = movieNfo.Directors?.Where(d => !string.IsNullOrEmpty(d)).ToList() ?? new List<string>();
		if (directorNames.Any())
		{
			var directorIds = await UpsertAndGetIdsAsync(connection, transaction, "Director", directorNames);
			await InsertGenericJunctionsAsync(connection, transaction, "MovieDirector", "DirectorId", movieId, directorIds);
		}

		// --- Studios ---
		var studioNames = movieNfo.Studios?.Where(n => !string.IsNullOrEmpty(n)).ToList() ?? new List<string>();
		if (studioNames.Any())
		{
			var studioIds = await UpsertAndGetIdsAsync(connection, transaction, "Studio", studioNames);
			await InsertGenericJunctionsAsync(connection, transaction, "MovieStudio", "StudioId", movieId, studioIds);
		}

		// --- Genres ---
		var genreNames = movieNfo.Genres?.Where(n => !string.IsNullOrEmpty(n)).ToList() ?? new List<string>();
		if (genreNames.Any())
		{
			var genreIds = await UpsertAndGetIdsAsync(connection, transaction, "Genre", genreNames);
			await InsertGenericJunctionsAsync(connection, transaction, "MovieGenre", "GenreId", movieId, genreIds);
		}

		// --- Countries ---
		var countryNames = movieNfo.Country?.Where(n => !string.IsNullOrEmpty(n)).ToList() ?? new List<string>();
		if (countryNames.Any())
		{
			var countryIds = await UpsertAndGetIdsAsync(connection, transaction, "Country", countryNames);
			await InsertGenericJunctionsAsync(connection, transaction, "MovieCountry", "CountryId", movieId, countryIds);
		}

		// --- Credits ---
		var creditNames = movieNfo.Credits?.Where(n => !string.IsNullOrEmpty(n)).ToList() ?? new List<string>();
		if (creditNames.Any())
		{
			var creditsIds = await UpsertAndGetIdsAsync(connection, transaction, "Credit", creditNames);
			await InsertGenericJunctionsAsync(connection, transaction, "MovieCredit", "CreditId", movieId, creditsIds);
		}
	}


	/// <summary>
	/// Löscht alle N:M-Links für einen Film (Atomare Vorbereitung für ein Update).
	/// </summary>
	/// <param name="connection">Die offene Sqlite-Verbindung.</param>
	/// <param name="transaction">Die aktive Sqlite-Transaktion.</param>
	/// <param name="movieId">Die ID des Films, dessen Verknüpfungen gelöscht werden sollen.</param>
	/// <param name="cancellationToken">Das Abbruch-Token.</param>
	private async Task DeleteMovieJunctionsAsync(
			SqliteConnection connection,
			SqliteTransaction transaction,
			int movieId,
			CancellationToken cancellationToken)
	{
    Trace.WriteLine($"DeleteMovieJunctionsAsync() (Movie-ID: {movieId})");

    // Liste aller Verknüpfungstabellen (Junctions), die gelöscht werden müssen
    var junctionTables = new[]
		{
				"MovieActor",
				"MovieDirector",
				"MovieStudio",
				"MovieGenre",
				"MovieCountry",
				"MovieCredit"
        // Fügen Sie hier alle weiteren N:M-Tabellen hinzu, falls vorhanden
    };

		// Führen Sie für jede Tabelle einen DELETE-Befehl aus
		foreach (var tableName in junctionTables)
		{
			// 🛑 Sicherheitsprüfung (optional, aber empfohlen):
			// Sicherstellen, dass der Tabellenname nur erlaubte Werte enthält
			if (!IsJunctionTableNameValid(tableName))
			{
				Trace.WriteLine($"WARNUNG: Ungültiger Tabellenname {tableName} bei Löschversuch ignoriert.");
				continue;
			}

			var deleteQuery = $"DELETE FROM {tableName} WHERE MovieId = @MovieId;";

			using (var command = connection.CreateCommand())
			{
				command.Transaction = transaction;
				command.CommandText = deleteQuery;
				command.Parameters.AddWithValue("@MovieId", movieId);
				DatabaseManager.TraceSql(command, true);
				await command.ExecuteNonQueryAsync(cancellationToken);
			}
		}
	}

	/// <summary>
	/// Führt ein Batch-Upsert von Namen in einer Stammdatentabelle durch und gibt die IDs zurück.
	/// </summary>
	/// <param name="connection">Die offene Sqlite-Verbindung.</param>
	/// <param name="transaction">Die aktive Sqlite-Transaktion.</param>
	/// <param name="tableName">Name der Stammdatentabelle (z.B. "Director", "Studio").</param>
	/// <param name="names">Liste der einzufügenden/abzurufenden Namen.</param>
	/// <returns>Eine Liste von IDs, die den übergebenen Namen entsprechen.</returns>
	private async Task<List<int>> UpsertAndGetIdsAsync(
			SqliteConnection connection,
			SqliteTransaction transaction,
			string tableName,
			List<string> names)
	{
		Trace.WriteLine($"UpsertAndGetIdsAsync() (Table: {tableName})");

		var ids = new List<int>();

		if (names == null || !names.Any())
		{
			return ids;
		}

		// 🛑 Sicherheitsprüfung (SQL Injection vermeiden)
		// Stellen Sie sicher, dass der tableName nur gültige Werte enthält (z.B. durch eine Whitelist)
		if (!IsStammdatenTableNameValid(tableName))
		{
			// Diese Hilfsmethode müssen Sie im MetaService implementieren (siehe unten)
			throw new ArgumentException($"Ungültiger Tabellenname: {tableName}");
		}

		// 1. **Upsert-Schritt:** Fügt neue Namen ein, falls sie noch nicht existieren.
		// Wir nutzen ON CONFLICT DO NOTHING, da wir nur die IDs benötigen.
		var upsertQuery = $@"INSERT INTO {tableName} (Name) VALUES (@Name) ON CONFLICT(Name) DO NOTHING;";

		// 2. **Select-Schritt:** Ruft die IDs für alle Namen ab.
		// Wir nutzen IN-Klausel für eine effiziente Abfrage.
		// Die Platzhalter werden dynamisch generiert.
		var selectPlaceholders = names.Select((_, i) => $"@Name_{i}").ToList();
		var selectQuery = $@"SELECT Id FROM {tableName} WHERE Name IN ({string.Join(", ", selectPlaceholders)});";


		// --- UPSERT AUSFÜHREN ---
		foreach (var name in names.Distinct()) // Distinct, falls Duplikate in der Liste sind
		{
			if (string.IsNullOrEmpty(name)) continue;

			using (var command = connection.CreateCommand())
			{
				command.Transaction = transaction;
				command.CommandText = upsertQuery;
				command.Parameters.AddWithValue("@Name", name);
				DatabaseManager.TraceSql(command, true);
				await command.ExecuteNonQueryAsync();
			}
		}

		// --- SELECT AUSFÜHREN ---
		using (var command = connection.CreateCommand())
		{
			command.Transaction = transaction;
			command.CommandText = selectQuery;

			// Parameter für die SELECT IN-Klausel binden
			for (int i = 0; i < names.Count; i++)
			{
				command.Parameters.AddWithValue($"@Name_{i}", names[i]);
			}

			using (var reader = await command.ExecuteReaderAsync())
			{
				while (await reader.ReadAsync())
				{
					ids.Add(reader.GetInt32(0));
				}
			}
		}

		// Stellen Sie sicher, dass die Anzahl der abgerufenen IDs der Anzahl der Namen entspricht (optional)
		// if (ids.Count != names.Count) { ... Fehlerbehandlung ... }

		return ids;
	}

	/// <summary>
	/// Fügt einfache N:M-Verknüpfungen in die angegebene Junction-Tabelle ein.
	/// Diese Methode funktioniert für Junction-Tabellen, die nur zwei Spalten (MovieId und ForeignId) enthalten.
	/// </summary>
	/// <param name="connection">Die offene Sqlite-Verbindung.</param>
	/// <param name="transaction">Die aktive Sqlite-Transaktion.</param>
	/// <param name="junctionTableName">Name der Junction-Tabelle (z.B. "MovieDirector").</param>
	/// <param name="foreignIdColumnName">Name der Fremdschlüsselspalte (z.B. "DirectorId").</param>
	/// <param name="movieId">Die ID des aktuellen Films.</param>
	/// <param name="foreignIds">Eine Liste der Fremdschlüssel-IDs (z.B. DirectorIds).</param>
	/// <returns>Ein Task, der die asynchrone Operation darstellt.</returns>
	private async Task InsertGenericJunctionsAsync(
			SqliteConnection connection,
			SqliteTransaction transaction,
			string junctionTableName,
			string foreignIdColumnName,
			int movieId,
			List<int> foreignIds)
	{
    Trace.WriteLine($"InsertGenericJunctionsAsync() (Table: {junctionTableName})");

    if (foreignIds == null || !foreignIds.Any())
		{
			return;
		}

		// 🛑 Sicherheitsprüfung (SQL Injection vermeiden)
		// Wir lassen NUR die Spalten- und Tabellennamen zu, die hartcodiert sind oder validiert wurden.
		// Der Code geht davon aus, dass die übergebenen Namen sicher sind und die Tabellen existieren.
		if (!IsJunctionTableNameValid(junctionTableName) || !IsColumnNameValid(foreignIdColumnName))
		{
			throw new ArgumentException("Ungültiger Tabellen- oder Spaltenname übergeben.");
		}

		// Erstelle Platzhalter für alle IDs
		var valueClauses = new List<string>();

		// Wir erzeugen einen Satz von Parametern: (@MovieId, @ForeignId_0), (@MovieId, @ForeignId_1), ...
		for (int i = 0; i < foreignIds.Count; i++)
		{
			valueClauses.Add($"(@MovieId, @ForeignId_{i})");
		}

		// Die finale SQL-Query
		string insertQuery = $@"INSERT OR IGNORE INTO {junctionTableName} (MovieId, {foreignIdColumnName}) VALUES {string.Join(", ", valueClauses)};";

		using (var command = connection.CreateCommand())
		{
			command.Transaction = transaction;
			command.CommandText = insertQuery;

			// Binde den konstanten MovieId Parameter
			command.Parameters.AddWithValue("@MovieId", movieId);

			// Binde alle dynamischen ForeignId Parameter
			for (int i = 0; i < foreignIds.Count; i++)
				command.Parameters.AddWithValue($"@ForeignId_{i}", foreignIds[i]);

			DatabaseManager.TraceSql(command, true);
			await command.ExecuteNonQueryAsync();
		}
	}

	#endregion

	// --------------------------------------------------------------------------------
	// Normalization & Junction Helpers (Hilfen für N:M-Beziehungen)
	// --------------------------------------------------------------------------------

	#region Normalization & Junction Helpers
	private bool IsJunctionTableNameValid(string name)
	{
		// Hier eine Liste aller gültigen Junction-Tabellennamen pflegen
		var validNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
		{
				"MovieActor", "MovieDirector", "MovieStudio", "MovieGenre", "MovieCountry", "MovieCredit"
		};
		return validNames.Contains(name);
	}

	private bool IsColumnNameValid(string name)
	{
		var validNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
		{
				"ActorId", "DirectorId", "StudioId", "GenreId", "CountryId", "CreditId"
		};
		return validNames.Contains(name);
	}

	private bool IsStammdatenTableNameValid(string name)
	{
		// Dies ist eine Whitelist aller gültigen Stammdaten-Tabellen, die diese Methode verwendet.
		var validNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
		{
				"Director", "Studio", "Genre", "Country", "Credit"
		};
		return validNames.Contains(name);
	}

	/// <summary>
	/// Spezifischer Junction-Write: Löscht nur eine einzelne Verknüpfung eines Actors
	/// (z.B. wenn ein Nutzer einen Actor entfernt).
	/// </summary>
	/// <param name="movieId"></param>
	/// <param name="actorId"></param>
	/// <param name="cancellationToken"></param>
	/// <returns></returns>
	public async Task DeleteMovieActorLinkAsync(
		int movieId,
		int actorId,
		CancellationToken cancellationToken)
	{
    Trace.WriteLine($"DeleteMovieActorLinkAsync() (Movie: {movieId}, Actor: {actorId})");

    await _dbManager.ExecuteAsync(async connection =>
    {
      // Starte eine Transaktion innerhalb der gesicherten Verbindung
      using (var transaction = connection.BeginTransaction())
      {
        try
        {
          // SQL-Befehl: Lösche den Link in der MovieActor-Tabelle
          var deleteQuery = "DELETE FROM MovieActor WHERE MovieId = @MovieId AND ActorId = @ActorId;";

          using (var command = connection.CreateCommand())
          {
            command.Transaction = transaction;
            command.CommandText = deleteQuery;
            command.Parameters.AddWithValue("@MovieId", movieId);
            command.Parameters.AddWithValue("@ActorId", actorId);

            // Führe den Befehl asynchron aus
            DatabaseManager.TraceSql(command, true);
            await command.ExecuteNonQueryAsync(cancellationToken);
          }

          transaction.Commit();
        }
        catch (Exception ex)
        {
          transaction.Rollback();

          // Logging und Fehlerweiterleitung
          System.Diagnostics.Trace.WriteLine($"Fehler beim Löschen des Actor-Links: {Utils.GetExcMsg(ex)}");
          throw;
        }
      }
    });
  }

	#endregion

	// --------------------------------------------------------------------------------
	// Image & Actor Cache Management (Bilder und Thumbnails)
	// --------------------------------------------------------------------------------

	#region Image & Actor Management
	/// <summary>
	/// Speichert Darsteller und bereits vorhandene Bilddaten ohne Netzwerkzugriff.
	/// </summary>
	/// <param name="connection"></param>
	/// <param name="transaction"></param>
	/// <param name="name"></param>
	/// <param name="tmdbId"></param>
	/// <param name="thumbUrl"></param>
	/// <param name="cancellationToken"></param>
	/// <returns></returns>
	public async Task<int> UpsertActorWithThumbAsync(
        SqliteConnection connection, SqliteTransaction transaction,
        string name, int? tmdbId, string? thumbUrl, byte[]? thumbData,
        CancellationToken cancellationToken)
    {
        // Network downloads while holding the writer lock stall all scan workers.
        // Portraits are loaded on demand by the editor instead.
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = "SELECT Id FROM Actor WHERE Name=@Name OR (@TMDbId>0 AND TMDbId=@TMDbId) ORDER BY (Name=@Name) DESC LIMIT 1;";
        command.Parameters.AddWithValue("@Name", name);
        command.Parameters.AddWithValue("@TMDbId", (object?)tmdbId ?? DBNull.Value);
        var existing = await command.ExecuteScalarAsync(cancellationToken);
        command.Parameters.AddWithValue("@Thumb", thumbData is { Length: > 0 } ? thumbData : DBNull.Value);
        if (existing is not null && existing != DBNull.Value)
        {
            command.CommandText = "UPDATE Actor SET TMDbId=COALESCE(@TMDbId,TMDbId), Thumb=COALESCE(@Thumb,Thumb) WHERE Id=@Id;";
            command.Parameters.AddWithValue("@Id", existing);
            await command.ExecuteNonQueryAsync(cancellationToken);
            return Convert.ToInt32(existing);
        }
        command.CommandText = "INSERT INTO Actor(Name,TMDbId,Thumb) VALUES(@Name,@TMDbId,@Thumb); SELECT last_insert_rowid();";
        return Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken));
    }

    /// <summary>
	/// Lädt Bilder zu dem Film, wie z. B. Poster oder Fanart aus der DB, sucht nach fehlenden Bildern
	/// lokal und speichert sie in der DB als BLOB.
	/// </summary>
	/// <param name="movieId"></param>
	/// <param name="movieFolderPath"></param>
	/// <param name="videoFilePath"></param>
	/// <param name="cancellationToken"></param>
	/// <returns></returns>
	public async Task<List<ImageCacheItem>> GetOrCacheMoviePicturesAsync(
		int movieId,
		string movieFolderPath,
		string? videoFilePath,
		CancellationToken cancellationToken)
	{
		// Die gesamte Logik (Lesen, Cachen, Schreiben) läuft nun in einem einzigen,
    // thread-sicheren, asynchronen Block.
    var cachedImages = await _dbManager.ExecuteAsync<List<ImageCacheItem>>(async connection =>
    {
      var currentCachedImages = new List<ImageCacheItem>();

      // 1. Laden der vorhandenen Cache-Einträge (Reader-Abfrage)
      var selectQuery = "SELECT Id, Type, Data FROM ImageCache WHERE MovieId = @MovieId;";

      using (var command = connection.CreateCommand())
      {
        command.CommandText = selectQuery;
        command.Parameters.AddWithValue("@MovieId", movieId);
        DatabaseManager.TraceSql(command, true);
        // ExecuteReaderAsync wird ausgeführt
        using (var reader = await command.ExecuteReaderAsync(cancellationToken))
        {
          while (await reader.ReadAsync())
          {
            currentCachedImages.Add(new ImageCacheItem
            {
              Id = reader.GetInt32(0),
              Type = reader.GetString(1),
              Data = (byte[])reader.GetValue(2)
            });
          }
        }
      }

      // 2. Prüfen, welche Bilder fehlen
      var missingTypes = ImageTypes.AllTypes
              .Except(currentCachedImages.Select(c => c.Type), StringComparer.OrdinalIgnoreCase)
              .ToList();

      if (missingTypes.Any())
      {
        // 3. Nachladen, Speichern und Cachen in einer Transaktion
        // Die Transaktion bleibt HIER, um das Lesen und Schreiben atomar zu halten.
        using (var transaction = connection.BeginTransaction())
        {
          try
          {
            foreach (var type in missingTypes)
            {
              cancellationToken.ThrowIfCancellationRequested();

              // Die Dateisuche (FindLocalPicturePath) muss außerhalb der DB-I/O-Sperre bleiben,
              // aber da wir hier schon im Lock sind, ist es in Ordnung.
              // Wichtig ist, dass die blockierenden File-Operationen (File.ReadAllBytesAsync)
              // weiterhin asynchron ausgeführt werden.

              string? fullPath = await Task.Run(() =>
                      FindLocalPicturePath(movieFolderPath, videoFilePath, type), cancellationToken);

              if (!string.IsNullOrEmpty(fullPath))
              {
                // Bild von der Festplatte laden (ASYNCHRON, aber nicht blockierend für andere Threads)
                byte[] imageData = await File.ReadAllBytesAsync(fullPath, cancellationToken);

                // Speichern in die ImageCache-Tabelle
                var insertQuery = "INSERT INTO ImageCache (MovieId, Type, Data) VALUES (@MovieId, @Type, @Data);";

                using (var insertCommand = connection.CreateCommand())
                {
                  insertCommand.Transaction = transaction;
                  insertCommand.CommandText = insertQuery;
                  insertCommand.Parameters.AddWithValue("@MovieId", movieId);
                  insertCommand.Parameters.AddWithValue("@Type", type);
                  insertCommand.Parameters.AddWithValue("@Data", imageData);

                  // ExecuteNonQueryAsync wird ausgeführt
                  DatabaseManager.TraceSql(insertCommand, true);
                  await insertCommand.ExecuteNonQueryAsync(cancellationToken);
                }

                // Füge das neu geladene Bild zur Rückgabeliste hinzu
                currentCachedImages.Add(new ImageCacheItem { Type = type, Data = imageData });
              }
            }

            transaction.Commit();
          }
          catch (Exception)
          {
            transaction.Rollback();
            throw;
          }
        }
      }

      // Rückgabe der Ergebnisse
      return currentCachedImages;
    });

    return cachedImages;
  }

  /// <summary>
  /// Aktualisiert (UPDATE) oder fügt (INSERT) die gegebenen Bilder in die ImageCache-Tabelle ein.
  /// </summary>
  /// <param name="movieId">Die ID des zugehörigen Films.</param>
  /// <param name="newCacheItems">Die Liste der zu speichernden/aktualisierenden Bilder.</param>
  /// <param name="cancellationToken">Das Abbruch-Token.</param>
  /// <returns>Die Anzahl der betroffenen Zeilen.</returns>
  public async Task<int> UpsertImageCacheItemsAsync(
      int movieId,
      List<FanArtBase> newCacheItems,
      CancellationToken cancellationToken)
  {
    if (newCacheItems == null || !newCacheItems.Any())
    {
      return 0;
    }

    int totalAffectedRows = 0;

    // Die gesamte Logik läuft in einem einzigen, thread-sicheren Block, um Deadlocks zu vermeiden.
    // Wir verwenden ExecuteAsync, da wir schreibende Operationen (INSERT/UPDATE) durchführen.
    await _dbManager.ExecuteAsync(async connection =>
    {
      // Starte eine Transaktion, um die gesamte Batch-Operation atomar zu machen.
      using (var transaction = connection.BeginTransaction())
      {
        try
        {
          // SQLite 3.35.0+ unterstützt native UPSERT-Syntax (ON CONFLICT DO UPDATE).
          // Wir verwenden die kompatible Methode: SELECT -> UPDATE/INSERT.

          foreach (var newItem in newCacheItems)
          {
            cancellationToken.ThrowIfCancellationRequested();

            // 1. Suche nach vorhandenem Eintrag (Basierend auf MovieId und Type)
            var selectQuery = "SELECT Id FROM ImageCache WHERE MovieId = @MovieId AND Type = @Type;";
            int? existingId = null;

            using (var selectCommand = connection.CreateCommand())
            {
              selectCommand.Transaction = transaction;
              selectCommand.CommandText = selectQuery;
              selectCommand.Parameters.AddWithValue("@MovieId", movieId);
              selectCommand.Parameters.AddWithValue("@Type", newItem.ImageType);

              // ExecuteScalarAsync ist effizienter, um nur eine ID abzurufen
              var result = await selectCommand.ExecuteScalarAsync(cancellationToken);
              if (result != null && result != DBNull.Value)
              {
                existingId = Convert.ToInt32(result);
              }
            }

            if (existingId.HasValue)
            {
              // FALL 1: UPDATE (Aktualisieren des vorhandenen Eintrags)
              var updateQuery = "UPDATE ImageCache SET Data = @Data WHERE Id = @Id;";

              using (var updateCommand = connection.CreateCommand())
              {
                updateCommand.Transaction = transaction;
                updateCommand.CommandText = updateQuery;
                updateCommand.Parameters.AddWithValue("@Data", newItem.ImageData);
                updateCommand.Parameters.AddWithValue("@Id", existingId.Value);

                totalAffectedRows += await updateCommand.ExecuteNonQueryAsync(cancellationToken);
              }
            }
            else
            {
              // FALL 2: INSERT (Neuen Eintrag hinzufügen)
              var insertQuery = "INSERT INTO ImageCache (MovieId, Type, Data) VALUES (@MovieId, @Type, @Data);";

              using (var insertCommand = connection.CreateCommand())
              {
                insertCommand.Transaction = transaction;
                insertCommand.CommandText = insertQuery;
                insertCommand.Parameters.AddWithValue("@MovieId", movieId);
                insertCommand.Parameters.AddWithValue("@Type", newItem.ImageType);
                insertCommand.Parameters.AddWithValue("@Data", newItem.ImageData);

                totalAffectedRows += await insertCommand.ExecuteNonQueryAsync(cancellationToken);
              }
            }
          } // Ende foreach

          transaction.Commit();
        }
        catch (Exception)
        {
          transaction.Rollback();
          throw;
        }
      }
    });

    return totalAffectedRows;
  }

  /// <summary>
  /// Liest Actors mit allen Verknüpfungsdaten (Role, SortOrder) aus der DB aus.
  /// </summary>
  /// <param name="movieId"></param>
  /// <param name="cancellationToken"></param>
  /// <returns></returns>
  public async Task<List<Actor>> GetActorsForMovieAsync(int movieId, CancellationToken cancellationToken)
	{
		var actors = new List<Actor>();

		// SQL-Query:
		// 1. SELECT * von Actor
		// 2. JOIN mit MovieActor, um die Verknüpfung für die gegebene MovieId zu finden
		// 3. Sortiere nach SortOrder, um die korrekte Reihenfolge zu gewährleisten
		var selectQuery = @"SELECT A.Id, A.Name, A.TMDbId, A.Thumb, MA.Role, MA.SortOrder FROM Actor A INNER JOIN ";
    selectQuery += @"MovieActor MA ON A.Id = MA.ActorId WHERE MA.MovieId = @MovieId ORDER BY  MA.SortOrder ASC;";

    // Die gesamte Lese- und Fülllogik läuft nun im thread-sicheren, asynchronen Block.
    actors = await _dbManager.ExecuteReaderAsync<List<Actor>>(async connection =>
    {
      var localActors = new List<Actor>();

      using (var command = connection.CreateCommand())
      {
        command.CommandText = selectQuery;
        command.Parameters.AddWithValue("@MovieId", movieId);
        DatabaseManager.TraceSql(command, true);
        using (var reader = await command.ExecuteReaderAsync(cancellationToken))
        {
          while (await reader.ReadAsync(cancellationToken))
          {
            // Erstellen des Actor-Objekts und Füllen der Eigenschaften
            localActors.Add(new Actor
            {
              Id = reader.GetInt32(reader.GetOrdinal("Id")),
              Name = reader.GetString(reader.GetOrdinal("Name")),

              // TMDbId ist nullable
              TmdbId = reader.IsDBNull(reader.GetOrdinal("TMDbId"))
                                     ? (int?)null
                                     : reader.GetInt32(reader.GetOrdinal("TMDbId")),

              // Thumb (BLOB) ist nullable
              ThumbData = reader.IsDBNull(reader.GetOrdinal("Thumb"))
                                     ? null
                                     : (byte[])reader["Thumb"],

              // Film-spezifische Verknüpfungsdaten:
              Role = reader.IsDBNull(reader.GetOrdinal("Role"))
                                 ? null
                                 : reader.GetString(reader.GetOrdinal("Role")),

              Order = reader.GetInt32(reader.GetOrdinal("SortOrder"))
            });
          }
        }
      }
			return localActors;
    });

    return actors;
  }

	public async Task<List<ImageCacheItem>> GetImageCacheItemsWithOutDataAsync(int movieId,
		string movieFolderPath,
		string? videoFilePath,
		CancellationToken cancellationToken)
	{
		// Die Datenbank-Lese-Logik (Punkt 1) läuft im thread-sicheren, asynchronen Block.
    var cachedImages = await _dbManager.ExecuteReaderAsync<List<ImageCacheItem>>(async connection =>
    {
      var localCachedImages = new List<ImageCacheItem>();
      var selectQuery = "SELECT Id, Type FROM ImageCache WHERE MovieId = @MovieId;";

      using (var command = connection.CreateCommand())
      {
        command.CommandText = selectQuery;
        command.Parameters.AddWithValue("@MovieId", movieId);
        DatabaseManager.TraceSql(command, true);
        using (var reader = await command.ExecuteReaderAsync(cancellationToken))
        {
          while (await reader.ReadAsync())
          {
            localCachedImages.Add(new ImageCacheItem
            {
              Id = reader.GetInt32(0),
              Type = reader.GetString(1),
              Data = Array.Empty<byte>() // Daten werden hier nicht geladen
            });
          }
        }
      }
      return localCachedImages;
    });

    // Der DB-Lock ist HIER FREIGEGEBEN!

    // 2. Prüfen, welche Bilder fehlen (Dateisystem-Logik)
    var missingTypes = ImageTypes.AllTypes
        .Except(cachedImages.Select(c => c.Type), StringComparer.OrdinalIgnoreCase)
        .ToList();

    if (missingTypes.Any())
    {
      foreach (var type in missingTypes)
      {
        cancellationToken.ThrowIfCancellationRequested();

        // Die Dateisuche muss in Task.Run erfolgen, da File.Exists blockiert.
        string? fullPath = await Task.Run(() =>
          FindLocalPicturePath(movieFolderPath, videoFilePath, type), cancellationToken);

        if (!string.IsNullOrEmpty(fullPath))
        {
          // Füge das neu geladene Bild zur Rückgabeliste hinzu (ohne Daten)
          cachedImages.Add(new ImageCacheItem { Type = type, Data = Array.Empty<byte>() });
        }
      }
    }

    return cachedImages;
  }

  public async Task<(byte[] Data, int ActorId)> GetOrCreateActorThumbAsync(
    string actorName,
    int? tmdbId,
    string? thumbUrl,
    byte[]? picturedata,
    CancellationToken cancellationToken)
  {
    byte[] defaultImageBytes = GetEmptyImageBytes()!;
    byte[]? preDownloadedBytes = null;

    // Die DB-Logik verwendet nun picturedata ODER preDownloadedBytes.
    byte[] dataToSave = picturedata ?? preDownloadedBytes ?? defaultImageBytes;

    // Der gesamte DB-Teil läuft im Lock:
    return await _dbManager.ExecuteAsync(async connection =>
    {
      // ----------------------------------------------------
      // 2. Prüfen, ob der Actor existiert (SELECT)
      // ----------------------------------------------------

      var selectQuery = @"SELECT Id, Thumb FROM Actor WHERE Name = @Name OR (@TMDbId > 0 AND TMDbId = @TMDbId) LIMIT 1;";

      using (var command = connection.CreateCommand())
      {
        command.CommandText = selectQuery;
        command.Parameters.AddWithValue("@Name", actorName);
        command.Parameters.AddWithValue("@TMDbId", tmdbId);
        DatabaseManager.TraceSql(command, true);
        using (var reader = await command.ExecuteReaderAsync(cancellationToken))
        {
          if (await reader.ReadAsync(cancellationToken))
          {
            int actorId = reader.GetInt32(0);

            // 2a/2b: Thumb ist bereits in DB ODER wir haben neue Daten
            if (!reader.IsDBNull(1))
            {
              // Übergabe oder vorgeladene Bytes haben Vorrang
              if (dataToSave.Length > 0 && dataToSave != defaultImageBytes)
              {
                // A1. Aktualisiere den Actor-Eintrag mit neuem Thumb
                await UpdateActorThumbAsync(connection, actorId, dataToSave, cancellationToken);
                return (Data: dataToSave, ActorId: actorId);
              }
              else
              {
                // A2. Thumb ist bereits in der DB (gecached)
                return (Data: (byte[])reader.GetValue(1), ActorId: actorId);
              }
            }

            // 2c/2d: Thumb fehlt, aber Actor existiert
            // Wir haben entweder vorgeladen (dataToSave) oder verwenden Default
            await UpdateActorThumbAsync(connection, actorId, dataToSave, cancellationToken);
            return (Data: dataToSave, ActorId: actorId);
          }
          else
          {
            // ----------------------------------------------------
            // 3. Actor existiert noch nicht -> Einfügen
            // ----------------------------------------------------

            int newActorId = -1;

            // 3b. Einfügen des neuen Actors in einer Transaktion
            using (var transaction = connection.BeginTransaction())
            {
              try
              {
                var insertQuery = "INSERT INTO Actor (Name, TMDbId, Thumb) VALUES (@Name, @TMDbId, @Thumb); SELECT last_insert_rowid();";

                using (var insertCommand = connection.CreateCommand())
                {
                  insertCommand.Transaction = transaction;
                  insertCommand.CommandText = insertQuery;
                  insertCommand.Parameters.AddWithValue("@Name", actorName);
                  insertCommand.Parameters.AddWithValue("@TMDbId", tmdbId);
                  insertCommand.Parameters.AddWithValue("@Thumb", dataToSave);
                  DatabaseManager.TraceSql(insertCommand, true);
                  object? result = await insertCommand.ExecuteScalarAsync(cancellationToken);
                  newActorId = Convert.ToInt32(result);
                  transaction.Commit();
                }
              }
              catch (SqliteException ex) when (ex.SqliteErrorCode == 19) // Race Condition
              {
                transaction.Rollback();
                Trace.WriteLine($"Race Condition beim Einfügen von Actor '{actorName}'. Eintrag existiert bereits.");
                // Da wir nicht wissen, welche ID der andere Thread vergeben hat,
                // geben wir die default Bytes zurück und lassen den nächsten Aufruf die ID finden.
                return (Data: dataToSave, ActorId: -1);
              }
              catch
              {
                transaction.Rollback();
                throw;
              }
            }

            // 3c. Gib die gefundenen (oder Default-) Bytes an den Aufrufer zurück
            return (Data: dataToSave, ActorId: newActorId);
          }
        }
      }
    });
  }

  // Die neue öffentliche Methode für den externen Aufruf
  public async Task UpdateActorThumbExternalAsync(
      int actorId,
      byte[] thumbData,
      CancellationToken cancellationToken)
  {
    // Nutzt die ExecuteAsync-Methode des DataBaseManager, um die Operation zu serialisieren
    await _dbManager.ExecuteAsync(async connection =>
    {
      // Ruft die ursprüngliche, interne Methode mit der bereitgestellten Verbindung auf
      await UpdateActorThumbAsync(connection, actorId, thumbData, cancellationToken);
    });
  }

  private async Task UpdateActorThumbAsync(
			SqliteConnection connection,
			int actorId,
			byte[] thumbData,
			CancellationToken cancellationToken)
	{
		// Hier Logik zum einfachen UPDATE des Thumbs, ohne Transaktion, da es schon im Haupt-Lock läuft.
		using (var command = connection.CreateCommand())
		{
			command.CommandText = "UPDATE Actor SET Thumb = @Thumb WHERE Id = @Id;";
			command.Parameters.AddWithValue("@Thumb", thumbData);
			command.Parameters.AddWithValue("@Id", actorId);
			await command.ExecuteNonQueryAsync(cancellationToken);
		}
	}

  #endregion


  // --------------------------------------------------------------------------------
  // Binary & File Helpers (Externe/Synchrone Helfer)
  // --------------------------------------------------------------------------------

  #region Binary & File Helpers
  /// <summary>
  /// Liefert ein leeres Bild als Byte-Array aus den eingebetteten Ressourcen.
  /// </summary>
  /// <returns></returns>
  public byte[]? GetEmptyImageBytes()
  {
    using var stream = typeof(MetadataService).Assembly.GetManifestResourceStream("VideoScrap2000.Empty.png");
    if (stream == null) return null;
    using var buffer = new MemoryStream();
    stream.CopyTo(buffer);
    return buffer.ToArray();
  }

  private string? FindLocalPicturePath(string folderPath, string? videoFilePath, string picNameEnd)
	{
		if (string.IsNullOrEmpty(folderPath))
			return null;

		// Diese Logik muss synchon ausgeführt werden, da File.Exists keine Async-Version hat.
		// Innerhalb von GetOrCacheMoviePicturesAsync rufen Sie dies in Task.Run auf (wie im Original).

		// 1. Variante: Im Ordner mit Bildtyp-Endung (z.B. D:\Film\poster.png)
		string file = Path.Combine(folderPath, picNameEnd + ".png");

		// 2. Variante: Dateiname + Bildtyp (z.B. D:\Film\video-poster.png)
		if (!string.IsNullOrEmpty(videoFilePath))
		{
			// Wir müssen den Dateinamen ohne Endung abrufen (Utils.GetNameFromFile)
			string baseFileName = Path.GetFileNameWithoutExtension(videoFilePath);
			file = Path.Combine(folderPath, baseFileName + "-" + picNameEnd + ".png");
		}

		if (File.Exists(file))
			return file;

		// 3. Variante: Endung auf .jpg ändern
		// Da wir schon geprüft haben, ob die PNG-Version existiert, ändern wir jetzt auf JPG.
		if (file.LastIndexOf(".") >= 0)
		{
			file = file.Substring(0, file.LastIndexOf(".")) + ".jpg";
		}
		else
		{
			file = file + ".jpg";
		}

		if (File.Exists(file))
			return file;

		return null;
	}

	/// <summary>
	/// Lädt ein Bild von der angegebenen URL herunter und gibt es als Byte-Array zurück.
	/// </summary>
	/// <param name="url">Die vollständige URL zum Bild.</param>
	/// <param name="cancellationToken">Das Abbruch-Token.</param>
	/// <returns>Das Bild als Byte-Array.</returns>
	/// <exception cref="HttpRequestException">Wird bei einem HTTP-Fehler ausgelöst.</exception>
	public async Task<byte[]> DownloadImageAsync(string url, CancellationToken cancellationToken)
	{
		Trace.WriteLine("DownloadImageAsync() " + url);
		if (string.IsNullOrEmpty(url))
		{
			throw new ArgumentException("Die URL darf nicht leer sein.", nameof(url));
		}

		// Konvertiere die URL, falls sie HTTP anstelle von HTTPS verwendet (gut für TMDB-Links)
		if (url.StartsWith("http://", StringComparison.OrdinalIgnoreCase))
		{
			url = url.Replace("http://", "https://");
		}

		// Setzt das Timeout für den Download, falls der HttpClient nicht konfiguriert ist (optional)
		// _httpClient.Timeout = TimeSpan.FromSeconds(15);

		try
		{
			Trace.WriteLine($"Starting image download from: {_httpClient.BaseAddress}{url}");
      // Lädt den gesamten Inhalt der Antwort asynchron als Byte-Array herunter
      byte[] imageBytes = await _httpClient.GetByteArrayAsync(url, cancellationToken);

			// Optional: Hier könnten Sie eine einfache Größen- und Formatprüfung einfügen,
			// um sicherzustellen, dass es sich um ein gültiges Bild handelt.
			if (imageBytes.Length == 0)
			{
				throw new InvalidDataException("Die heruntergeladene Datei war leer.");
			}

			return imageBytes;
		}
		catch (TaskCanceledException)
		{
			// Dies fängt den Abbruch durch das CancellationToken ab
			throw;
		}
		catch (HttpRequestException ex)
		{
			// Fängt Netzwerk- oder HTTP-Fehler (z.B. 404 Not Found, DNS-Fehler)
			System.Diagnostics.Trace.WriteLine($"HTTP-Fehler beim Download von {url}: {ex.Message}");
			throw;
		}
		catch (Exception ex)
		{
			System.Diagnostics.Trace.WriteLine($"Unerwarteter Fehler beim Download: {ex.Message}");
			throw;
		}
	}

	#endregion
}

// minimale Klasse, die in der Listview angezeigt werden könnte
public class MovieViewModel : System.ComponentModel.INotifyPropertyChanged
{
    public event System.ComponentModel.PropertyChangedEventHandler? PropertyChanged;
    public MovieFileFacts? FileFacts { get; private set; }
    public bool MissingMetadata => FileFacts is { IsAvailable: true, HasMetadata: false };
    public bool IsFolderUnavailable => FileFacts is { IsAvailable: false };
    public string FileDateText => FileFacts?.Modified?.ToString("dd.MM.yyyy HH:mm") ?? "–";
    public DateTime? MetadataCapturedAtUtc => FileFacts?.NfoCreatedAtUtc;
    public string MetadataCapturedDateText => MetadataCapturedAtUtc?.ToLocalTime().ToString("dd.MM.yyyy HH:mm") ?? "–";
    public bool IsPlaceholder { get; set; }
    public string VersionLabel { get; set; } = "";
    public bool HasVersionLabel => VersionLabel.Length > 0;
    public string MetadataStatus => IsFolderUnavailable ? "Ordner nicht erreichbar" : MissingMetadata ? "Neu" : "";
    public bool HasMetadataStatus => MetadataStatus.Length > 0;
    public void UpdateFileFacts(MovieFileFacts facts)
    {
        FileFacts = facts;
        foreach (var property in new[] { nameof(FileFacts), nameof(MissingMetadata), nameof(IsFolderUnavailable), nameof(FileDateText), nameof(MetadataCapturedAtUtc), nameof(MetadataCapturedDateText), nameof(MetadataStatus), nameof(HasMetadataStatus) })
            PropertyChanged?.Invoke(this, new(property));
    }
	// Die DB-Id ist wichtig, um später Details oder Bilder nachzuladen
	public int Id { get; set; }

	// Für die Hauptansicht
	private string _title = string.Empty;
	public string Title
	{
		get { return _title; }
		set
		{
			_title = value;
			if (FullMovieNFO != null)
				FullMovieNFO.Title = _title;
		}
	}

	private string _year = string.Empty;
  public string Year
	{
		get { return _year; }
		set
		{
      _year = value;
      if (FullMovieNFO != null)
        FullMovieNFO.Year = _year;
    }
	}
	private string _runtime = string.Empty;

	public string Runtime
	{
		get { return _runtime; }
		set
    {
      _runtime = value;
      if (FullMovieNFO != null && int.TryParse(_runtime, out int runtime) )
        FullMovieNFO.Runtime = runtime;
    }
  }
	private string _plot = string.Empty;

	public string Plot
	{
		get {  return _plot; }
		set
		{
			_plot = value;
			if (FullMovieNFO != null)
				FullMovieNFO.Plot = _plot;
    }
  }

	private float _rating = 0f;
  public float Rating
	{
		get { return _rating; }
    set
    {
      _rating = value;
			if (FullMovieNFO != null)
				FullMovieNFO.Rating = value;
    }
  }

	// Für die Filterung/Details
	public string FolderPath { get; set; } = string.Empty;

	// Der vollständige Pfad zur Haupt-Videodatei (aus der DB-Spalte VideoFilePath)
	public string VideoFilePath { get; set; } = string.Empty;

	// Nur der Dateiname zur einfachen Anzeige
	public string VideoFileName => Path.GetFileName(VideoFilePath);

	// Die Genres als String, um sie direkt anzeigen zu können
	private string _genresString = string.Empty;
  public string GenresString
	{
		get {  return _genresString; }
    set
		{
			_genresString = value;
			if (FullMovieNFO != null)
			{
				FullMovieNFO.Genres.Clear();
				FullMovieNFO.Genres.AddRange(_genresString.Split([", ", "; "], StringSplitOptions.RemoveEmptyEntries));
      }
    }
	}

	// Speichert das vollständig deserialisierte NFO-Objekt
	public Movie? FullMovieNFO { get; set; }

	// Speichert die vollständige Liste der Actor-Objekte (mit Rolle, Order)
	private List<Actor>? _actors;
  public List<Actor>? Actors
	{
		get { return _actors; }
		set
		{
			_actors = value;
			if (FullMovieNFO != null)
			{
				FullMovieNFO.Actors.Clear();
				if (_actors != null)
          FullMovieNFO.Actors.AddRange(_actors);
      }
    }
	}

	// NEU: Gibt an, ob die vollen Details geladen wurden
	public bool IsFullDetailsLoaded { get; private set; } = false;

	// Methode zum Laden der vollen Details (Muss von MetadataService injiziert werden)
	public async Task LoadFullDetailsAsync(MetadataService service)
	{
		if (IsFullDetailsLoaded) return; // Doppeltes Laden verhindern

		// Diese neue Methode im Service lädt JSON und Junctions
		var details = await service.LoadFullMovieDetailsAsync(this.Id);

		this.FullMovieNFO = details.Nfo;
		this.Actors = details.Actors; // Oder wie auch immer die Actors zurückgegeben werden

		IsFullDetailsLoaded = true;
	}

	public async Task LoadLinkedDataAsync(MetadataService metaService, CancellationToken cancellationToken)
	{
		// Die Actors-Liste des Modells komplett neu laden
		this.Actors = await metaService.GetActorsForMovieAsync(this.Id, cancellationToken);

		// Optional: Auch andere verknüpfte Listen neu laden, falls sie sich ändern könnten
		// this.Genres = await metaService.GetGenresForMovieAsync(this.Id, cancellationToken);
		// ...
	}
}
