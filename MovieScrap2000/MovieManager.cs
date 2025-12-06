using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace MovieScrap2000
{
	public class MovieManager
	{
		private readonly MetadataService _metadataService;

		// --- Events ---
		public event EventHandler<LogMessageObject>? LogMessage;
		public event EventHandler<int>? ProgressReport;

		public MovieManager(MetadataService metadataService)
		{
			_metadataService = metadataService;
		}

		// --- Konstanten ---
		public static readonly HashSet<string> VideoExtensions = new HashSet<string>
		{
				".mp4",
				".mkv",
				".mpg",
				".avi",
				".mov",
				".wmv",
				".flv",
				".ts",
				".webm"
		};

		// --- Veraltete MovieFolderInfo-Felder entfernt ---
		// private List<MovieFolderInfo> _movieList...
		// public IReadOnlyList<MovieFolderInfo> Movies...

		public string? RootPath { get; private set; }

		private void OnLogMessage(string message, LogLevel level = LogLevel.Info, string source = "Scanner")
		{
			LogMessageObject logEntry = new LogMessageObject(message, level, source);
			Trace.WriteLine(logEntry);
			LogMessage?.Invoke(this, logEntry);
		}

		private void OnProgressReport(int percent)
		{
			ProgressReport?.Invoke(this, percent);
		}

		// --- Haupt-Scan-Methode ---

		/// <summary>
		/// Scannt das übergeordnete Verzeichnis, analysiert Ordner parallel und speichert Metadaten sofort in die DB.
		/// </summary>
		/// <param name="rootPath">Der Pfad zum Haupt-Filmordner.</param>
		public async Task ScanDirectoryAsync(string rootPath, CancellationToken cancellationToken)
		{
			OnLogMessage($"Starte parallelen Scan des Verzeichnisses: {rootPath}", LogLevel.Info, "Scanner");

			if (!Directory.Exists(rootPath))
			{
				OnLogMessage($"FEHLER: Das Verzeichnis '{rootPath}' existiert nicht.", LogLevel.Error, "Scanner");
				return;
			}

			this.RootPath = rootPath;

			string[] movieFolders = Directory.GetDirectories(rootPath);
			int totalFolders = movieFolders.Length;
			int completedCount = 0;

			OnLogMessage($"-> {totalFolders} Unterordner gefunden. Starte Parallelverarbeitung und DB-Speicherung...", LogLevel.Info, "Scanner");

			// Liste der Tasks für Analyse und Speicherung
			var analysisTasks = movieFolders.Select(folderPath =>
			{
				// 🛑 Wrapper-Funktion, die den Task startet UND den Zähler verwaltet
				return AnalyzeAndSaveMovieFolderAsync(folderPath, cancellationToken)
						.ContinueWith(_ =>
						{
							// Dieser Teil wird ausgeführt, NACHDEM AnalyzeAndSaveMovieFolderAsync (inkl. await) abgeschlossen ist
							// (egal ob erfolgreich oder mit Fehler).

							// --- Fortschritts-Tracking (Wie ursprünglich in Parallel.ForEach) ---

							int currentCompleted = Interlocked.Increment(ref completedCount);
							int percent = (int)((double)currentCompleted / totalFolders * 100);

							// Wir verwenden eine einfache Logik, da wir uns jetzt in einem ContinueWith-Thread befinden.
							// ACHTUNG: ProgressReport muss ggf. auf dem UI-Thread ausgeführt werden!
							// Wenn ProgressReport ein Control/Window berührt, müssen Sie Dispatcher/SynchronizationContext verwenden.
							OnProgressReport(percent);

						}, TaskScheduler.Default); // Führen Sie ContinueWith im ThreadPool aus

			}).ToList();

			// Warten auf den Abschluss ALLER Analyse- und Speicheraufgaben
			try
			{
				await Task.WhenAll(analysisTasks);
			}
			catch (Exception ex)
			{
				OnLogMessage($"Fehler während des Parallel-Scans: {ex.Message}", LogLevel.Critical, "Scanner");
			}

			// Am Ende den Status auf 100% setzen
			OnProgressReport(100);

			OnLogMessage($"Scan und Datenbank-Speicherung abgeschlossen.", LogLevel.Info, "Scanner");
		}

		// --- Analyse und Speichern eines einzelnen Ordners (Asynchron) ---

		/// <summary>
		/// Analysiert einen einzelnen Ordner, liest NFO/Video-Pfade und speichert Metadaten/Platzhalter in die DB.
		/// </summary>
		private async Task AnalyzeAndSaveMovieFolderAsync(string folderPath, CancellationToken cancellationToken)
		{
			// Lokale Variablen
			string? movieTitleFromFolder = Path.GetFileName(folderPath);
			string? videoFilePath = null;
			string status;
			bool isNfoFound = false;
			Movie? movieNfo = null;

			try
			{
				// --- Logik für Dateisuche (Video und NFO) ---

				FileInfo[] filesInFolder = new DirectoryInfo(folderPath).GetFiles();

				// 1. Hauptfilm-Datei identifizieren (größte Videodatei)
				var videoFiles = filesInFolder
								.Where(f => VideoExtensions.Contains(f.Extension.ToLower()))
								.ToList();

				if (videoFiles.Any())
				{
					FileInfo mainVideoFile = videoFiles.OrderByDescending(f => f.Length).First();
					videoFilePath = mainVideoFile.FullName;
					OnLogMessage($"Videodatei '{mainVideoFile.Name}' im Ordner '{movieTitleFromFolder}' gefunden.", LogLevel.Info, "Scanner");

				}
				else
				{
					status = "Keine Videodatei gefunden";
					OnLogMessage($"WARNUNG: Keine Videodatei im Ordner '{movieTitleFromFolder}' gefunden.", LogLevel.Warning, "Scanner");
					return;
				}

				// 2. NFO-Datei identifizieren UND LESEN
				string? nfoFile = GetNfoFile(folderPath, videoFilePath);
				if (!string.IsNullOrEmpty(nfoFile))
				{
					OnLogMessage($"Lese Nfo-Datei '{Path.GetFileName(nfoFile)}' im Ordner '{movieTitleFromFolder}'.", LogLevel.Info, "Scanner");
					movieNfo = ReadNfo(nfoFile);

					if (movieNfo != null)
					{
						status = "NFO vorhanden (gelesen)";
						isNfoFound = true;
					}
					else
					{
						status = "NFO vorhanden (FEHLER beim Lesen)";
						OnLogMessage($"FEHLER beim Lesen der NFO '{Path.GetFileName(nfoFile)}'.", LogLevel.Error, "Scanner");
					}
				}

				// Existiert bereits ein Eintrag in der DB für diesen Filmordner?
				int existingMovieId = await _metadataService.CheckMovieExistsAsync(folderPath, cancellationToken);
				bool movieAlreadyExists = existingMovieId > 0;

				// 🛑 3. Platzhalter erstellen, wenn KEIN gültiges NFO-Objekt existiert
				if (movieNfo == null)
				{
					// Nur einen Platzhalter erstellen, wenn der Film NICHT bereits existiert.
					// Wenn er existiert, ändern wir die Daten nicht, es sei denn, die NFO wurde gelöscht.
					if (!movieAlreadyExists)
					{
						status = "Platzhalter (Scraping nötig)";
						OnLogMessage($"Erzeuge neue Nfo-Datei im Ordner '{movieTitleFromFolder}'.", LogLevel.Info, "Scanner");

						// Minimales NFO-Objekt für den Platzhalter erstellen
						movieNfo = new Movie
						{
							Title = Path.GetFileNameWithoutExtension(videoFilePath) ?? "Unbekannter Film",
							Plot = "Metadaten müssen noch durch Scraping geladen werden.",
							Year = null
						};
					}
				}

				// Wenn der Film bereits existiert und keine NFO gefunden wurde, behalten wir die alten DB-Daten.
				if (movieNfo != null && videoFilePath != null)
				{
					if (movieAlreadyExists)
					{
						// Fall A: Film existiert, NFO-Update erzwingen
						if (isNfoFound)
						{
							OnLogMessage($"Update bestehender Film (NFO) in Datenbank: '{movieTitleFromFolder}'.", LogLevel.Info, "Scanner");

							// 🛑 Neue DB-Methode, die nur UPDATET und die Junctions neu erstellt
							await _metadataService.UpdateMovieFromNfoAsync(
									existingMovieId, // Wir benötigen die ID für das Update
									movieNfo,
									folderPath,
									videoFilePath,
									cancellationToken);

							status = "Metadaten aktualisiert";
						}
						else
						{
							// Film existiert, aber keine NFO gefunden -> NICHTS tun (Daten bleiben erhalten)
							status = "Vorhanden (Ignoriert)";
						}
					}
					else
					{
						// Fall B: Film existiert NICHT -> Normaler Upsert (Insert)
						bool isPlaceholder = !isNfoFound;

						OnLogMessage($"Speichere NEUEN Film in Datenbank: '{movieTitleFromFolder}'.", LogLevel.Info, "Scanner");

						await _metadataService.SaveMovieAsync(
														movieNfo,
														folderPath,
														videoFilePath,
														isPlaceholder: isPlaceholder,
														cancellationToken);

						status = isPlaceholder ? "Platzhalter gespeichert" : "Metadaten gespeichert";
					}
				}
			}
			catch (Exception ex)
			{
				status = $"KRITISCHER Fehler beim Scannen";
				OnLogMessage($"KRITISCHER Fehler in {movieTitleFromFolder} ({folderPath}): {ex.Message}", LogLevel.Critical, "Scanner");
			}
			finally
			{

			}
		}

		public async Task<bool> UpdateMovieMetadataFromNfoAsync(int movieId, string folderPath, string videoFilePath, CancellationToken cancellationToken)
		{
			// Diese Methode führt ReadNfo intern aus und ruft dann MetadataService.UpdateMovieFromNfoAsync auf.

			string? nfoPath = GetNfoFile(folderPath, videoFilePath);
			if ( string.IsNullOrEmpty(nfoPath))
				return false; // Keine NFO gefunden

			// 1. Lesen der NFO 
			Movie? movieNfo = ReadNfo(nfoPath);

			if (movieNfo == null) return false;

			// 2. Datenbank-Update steuern (Nutzt die Methode Ihres MetadataService)
			return await _metadataService.UpdateMovieFromNfoAsync(
					movieId,
					movieNfo,
					folderPath,
					videoFilePath,
					cancellationToken);
		}

    // --- NFO-Leselogik ---

    /// <summary>
    /// Speichert das Movie-Objekt als XML-Datei (NFO) unter dem angegebenen Pfad.
    /// </summary>
    /// <param name="nfo">Das zu serialisierende Movie-Objekt.</param>
    /// <param name="folderPath">Der Ordner, in dem die NFO gespeichert werden soll.</param>
    /// <param name="cancellationToken">Token zur Abbruchsteuerung.</param>
    /// <returns>Der vollständige Pfad zur gespeicherten NFO-Datei.</returns>
    public static async Task<string> SaveNfoFileAsync(
        Movie nfo,
        string folderPath,
				string videoFilePath,
        CancellationToken cancellationToken)
    {
      cancellationToken.ThrowIfCancellationRequested();

      // 1. Serialisierung des Movie-Objekts in einen XML-String.
      string? xmlContent = NfoSerializer.SerializeXml(nfo);

      if (string.IsNullOrEmpty(xmlContent))
      {
        throw new InvalidOperationException("NFO-Serialisierung fehlgeschlagen.");
      }

      string videoFileNameWithoutExtension = Path.GetFileNameWithoutExtension(videoFilePath);
      string nfoFileName = $"{videoFileNameWithoutExtension}.nfo";

      // Fallback auf den Standardnamen, falls der Video-Dateiname nicht ermittelt werden kann
      if (string.IsNullOrEmpty(videoFileNameWithoutExtension))
      {
        nfoFileName = "movie.nfo";
      }
      string nfoFilePath = Path.Combine(folderPath, nfoFileName);

      // 3. Schreiben der Datei asynchron (non-blocking I/O).
      await File.WriteAllTextAsync(nfoFilePath, xmlContent, cancellationToken);

      System.Diagnostics.Trace.WriteLine($"NFO-Datei erfolgreich gespeichert unter: {nfoFilePath}");

      return nfoFilePath;
    }

    private string? GetNfoFile(string folderPath, string videoFilePath )
		{
			string expectedNfoFileName = Utils.GetNameFromFile(videoFilePath) + ".nfo";
			string expectedNfoPath = Path.Combine(folderPath, expectedNfoFileName);

			if (File.Exists(expectedNfoPath))
			{
				return expectedNfoPath;
			}
			else
			{
				DirectoryInfo dir = new DirectoryInfo(folderPath);
				FileInfo[] filesInFolder = new DirectoryInfo(folderPath).GetFiles("*.nfo", SearchOption.TopDirectoryOnly);
				if ( filesInFolder.Length > 0)
				{
					return filesInFolder[0].FullName;
				}
			}

			return null;
		}

		/// <summary>
		/// Liest eine Kodi-kompatible NFO-Datei ein und deserialisiert sie in ein MovieNFO-Objekt.
		/// </summary>
		private Movie? ReadNfo(string nfoFilePath) 
		{
			if (!File.Exists(nfoFilePath))
			{
				return null;
			}

			try
			{
				// Typ auf MovieNFO angepasst (angenommen)
				XmlSerializer serializer = new XmlSerializer(typeof(Movie));

				using (TextReader reader = new StreamReader(nfoFilePath))
				{
					object? deserializedObject = serializer.Deserialize(reader);
					if (deserializedObject is Movie movieData) 
					{
						return movieData;
					}
					else
					{
						return null;
					}
				}
			}
			catch (InvalidOperationException ex)
			{
				Trace.WriteLine($"Fehler beim Deserialisieren der NFO-Datei {nfoFilePath}: {ex.Message}");
				return null;
			}
			catch (Exception ex)
			{
				Trace.WriteLine($"Ein Lesefehler ist aufgetreten: {ex.Message}");
				return null;
			}
		}
	}
}
