using MovieScrap2000.FanArtTvScraper;
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

    // Dieses Dictionary definiert, wie der interne Bildtyp auf das externe Dateiformat gemappt wird.
    private static readonly Dictionary<string, string> MovieImageNameMapping = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        // Interne Typen (aus dem Cache) -> Externe Suffixe (für Kodi/Jellyfin)
        { ImageTypes.Poster, "-poster.jpg" },
        { ImageTypes.Fanart, "-fanart.jpg" },
        { ImageTypes.ClearArt, "-clearart.png" },
        { ImageTypes.ClearLogo, "-logo.png" },
        { ImageTypes.Landscape, "-landscape.jpg" },
        { ImageTypes.Banner, "-banner.jpg" },
				{ ImageTypes.DiskArt, "-discart.jpg" },
        { ImageTypes.KeyArt, "-keyart.jpg" }
    };

    // --- Veraltete MovieFolderInfo-Felder entfernt ---
    // private List<MovieFolderInfo> _movieList...
    // public IReadOnlyList<MovieFolderInfo> Movies...

    public string? RootPath { get; private set; }
    private int scanErrors;
    public int ScanErrors => scanErrors;

		private void OnLogMessage(string message, LogLevel level = LogLevel.Info, string source = "Scanner")
		{
            if (level is LogLevel.Error or LogLevel.Critical) Interlocked.Increment(ref scanErrors);
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
            scanErrors = 0;
            if (!Directory.Exists(rootPath)) throw new DirectoryNotFoundException(rootPath);
            RootPath = rootPath;
            var folders = Directory.GetDirectories(rootPath);
            int completed = 0;
            await Parallel.ForEachAsync(folders, new ParallelOptions { CancellationToken = cancellationToken, MaxDegreeOfParallelism = 4 }, async (folder, token) =>
            {
                await AnalyzeAndSaveMovieFolderAsync(folder, token);
                OnProgressReport((int)(100d * Interlocked.Increment(ref completed) / Math.Max(1, folders.Length)));
            });
            cancellationToken.ThrowIfCancellationRequested();
            OnProgressReport(100);
        }

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
                        var inferred = MovieNameParser.Parse(videoFilePath);
						movieNfo = new Movie
						{
							Title = inferred.Title,
							Plot = "Metadaten müssen noch durch Scraping geladen werden.",
							Year = inferred.Year?.ToString(System.Globalization.CultureInfo.InvariantCulture)
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
														isPlaceholder: isPlaceholder, cancellationToken, writeFiles: false);

						status = isPlaceholder ? "Platzhalter gespeichert" : "Metadaten gespeichert";
					}
				}
			}
			catch (OperationCanceledException) { throw; }
			catch (Exception ex)
			{
				status = $"KRITISCHER Fehler beim Scannen";
				OnLogMessage($"KRITISCHER Fehler in {movieTitleFromFolder} ({folderPath}): {ex.Message}", LogLevel.Critical, "Scanner");
			}
			finally
			{

			}
		}

    /// <summary>
    /// Speichert die Hauptfilmbilder im Movie-Ordner unter den spezifischen Dateinamen
    /// (z.B. [video_name]-poster.jpg).
    /// </summary>
    /// <param name="movieFolderPath">Der Pfad zum Filmordner.</param>
    /// <param name="videoFileNameWithoutExtension">Der Name der Videodatei ohne Erweiterung (Basis für Dateinamen).</param>
    /// <param name="imageCacheItems">Die Liste der zu speichernden Bilder (muss Data enthalten).</param>
    /// <param name="cancellationToken">Das Abbruch-Token.</param>
    public static async Task SynchronizeMovieImagesAsync(string movieFolderPath,
        string videoFileNameWithoutExtension, List<ImageCacheItem> imageCacheItems, CancellationToken cancellationToken,
        Func<string, string, ImageCacheItem, CancellationToken, Task>? saveImage = null)
    {
        foreach (var image in imageCacheItems)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (saveImage is not null) await saveImage(movieFolderPath, videoFileNameWithoutExtension, image, cancellationToken);
            else await ArtworkFiles.SaveAsync(movieFolderPath, videoFileNameWithoutExtension, image, cancellationToken);
        }
    }
    /// <summary>
    /// Synchronisiert den .actors-Unterordner: Löscht vorhandene Dateien und speichert die
    /// aktuellen Actor-Bilder unter dem Muster "Vorname_Nachname.jpg".
    /// </summary>
    /// <param name="movieFolderPath">Der Pfad zum Filmordner.</param>
    /// <param name="credits">Das Credits-Modell, das die Actor-Daten enthält.</param>
    /// <param name="cancellationToken">Das Abbruch-Token.</param>
    public static async Task SynchronizeActorFolderAsync(
        string movieFolderPath,
        List<Actor> credits,
        CancellationToken cancellationToken)
    {
      // Der Pfad zum .actors-Unterordner
      string actorFolderPath = Path.Combine(movieFolderPath, ".actors");

      // --- 1. Aufräumen: Ordner erstellen und vorhandene Dateien löschen ---

      // Die Erstellung/Prüfung des Ordners kann synchron erfolgen.
      if (!Directory.Exists(actorFolderPath))
      {
        Directory.CreateDirectory(actorFolderPath);
      }

      // Asynchrones Löschen aller Dateien im Ordner (Task.Run für blockierende I/O)
      try
      {
        await Task.Run(() =>
        {
          cancellationToken.ThrowIfCancellationRequested();

          var files = Directory.GetFiles(actorFolderPath);
          foreach (var file in files)
          {
            cancellationToken.ThrowIfCancellationRequested();
            // ACHTUNG: File.Delete ist synchron und läuft daher innerhalb von Task.Run
            File.Delete(file);
          }
        }, cancellationToken);
      }
      catch (OperationCanceledException)
      {
        throw;
      }
      catch (Exception ex)
      {
        // Protokollierung bei Problemen (z.B. Zugriffsbeschränkungen)
        Trace.WriteLine($"Fehler beim Leeren des .actors Ordners: {Utils.GetExcMsg(ex)}");
        // Fahren Sie fort, auch wenn das Löschen fehlgeschlagen ist.
      }


      // --- 2. Speichern: Aktuelle Actor-Bilder speichern ---

      if (credits != null && credits.Any())
      {
        var saveTasks = new List<Task>();

        foreach (var actor in credits)
        {
          // Prüfen, ob Bilddaten vorhanden sind
          if (actor.ThumbData != null && actor.ThumbData.Length > 0 && !string.IsNullOrWhiteSpace(actor.Name))
          {
						Trace.WriteLine($": Starte Speichern von Actor-Bild für \"{actor.Name}\"");
            // Task zum Speichern der einzelnen Datei starten
            // Wir müssen Task.Run verwenden, da File.WriteAllBytesAsync I/O-Asynchronität
            // bietet, aber wir möchten die übergeordnete Iteration nicht blockieren.
            Task saveTask = Task.Run(async () =>
            {
              cancellationToken.ThrowIfCancellationRequested();

              try
              {
                // 2a. Dateiname generieren (z.B. "Douglas_M._Griffin.jpg")
                string safeName = GenerateSafeActorFileName(actor.Name);
                string fileName = $"{safeName}.jpg";
                string fullPath = Path.Combine(actorFolderPath, fileName);

                // 2b. Datei asynchron schreiben (non-blocking I/O)
                await File.WriteAllBytesAsync(fullPath, actor.ThumbData, cancellationToken);
              }
              catch (Exception ex)
              {
                // Fehler beim Speichern eines einzelnen Bildes protokollieren
                Trace.WriteLine($"Fehler beim Speichern von Actor-Bild {actor.Name}: {ex.Message}");
                // Die Exception wird durch Task.WhenAll unten aufgefangen
                throw;
              }
            }, cancellationToken);

            saveTasks.Add(saveTask);
          }
        }

        // Parallel auf den Abschluss aller Speicher-Tasks warten
        try
        {
          await Task.WhenAll(saveTasks);
          Trace.WriteLine($"Erfolgreich {saveTasks.Count} Actor-Bilder synchronisiert.");
        }
        catch (OperationCanceledException)
        {
          throw;
        }
        catch (Exception ex)
        {
          // Dieser Block fängt alle Aggregated Exceptions auf, die durch Task.WhenAll gesammelt wurden.
          Trace.WriteLine($"Fehler beim Speichern eines oder mehrerer Actor-Bilder: {ex.Message}");
        }
      }
    }

    // ----------------------------------------------------------------------
    // Hilfsmethode zur Generierung eines sicheren Dateinamens
    // ----------------------------------------------------------------------

    /// <summary>
    /// Konvertiert den vollen Actornamen in ein sicheres Dateiformat (z.B. "Douglas M. Griffin" -> "Douglas_M._Griffin").
    /// </summary>
    private static string GenerateSafeActorFileName(string name)
    {
      // 1. Ersetze Leerzeichen durch Unterstriche
      string safeName = name.Replace(' ', '_');

      // 2. Entferne ungültige Dateinamenzeichen und ersetze sie durch Unterstriche
      var invalidChars = Path.GetInvalidFileNameChars();
      foreach (var c in invalidChars)
      {
        safeName = safeName.Replace(c, '_');
      }

      // 3. Optional: Entferne doppelte Unterstriche und führende/nachlaufende Unterstriche
      safeName = System.Text.RegularExpressions.Regex.Replace(safeName, @"_+", "_").Trim('_');

      return safeName;
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

      string videoFileNameWithoutExtension = Path.GetFileNameWithoutExtension(videoFilePath);
      string nfoFileName = $"{videoFileNameWithoutExtension}.nfo";

      // Fallback auf den Standardnamen, falls der Video-Dateiname nicht ermittelt werden kann
      if (string.IsNullOrEmpty(videoFileNameWithoutExtension))
      {
        nfoFileName = "movie.nfo";
      }
      string nfoFilePath = Path.Combine(folderPath, nfoFileName);

      // 3. Schreiben der Datei asynchron (non-blocking I/O).
      // Preserve the NFO selected during import, including Kodi's movie.nfo naming.
      var existingNfo = GetNfoFile(folderPath, videoFilePath);
      nfoFilePath = existingNfo ?? nfoFilePath;
      var previous = File.Exists(nfoFilePath) ? LoadExistingNfoDocument(nfoFilePath) : null;
      var originalCreation = previous is null ? DateTime.UtcNow
          : MovieFileFacts.StoredCreationTime((string?)previous.Root?.Attribute(MovieFileFacts.CreationAttribute))
              ?? File.GetCreationTimeUtc(nfoFilePath);
      var storedDate = (string?)previous?.Root?.Element("dateadded") ?? nfo.DateAdded;
      if (!DateTime.TryParseExact(storedDate, "yyyy-MM-dd HH:mm:ss",
          System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out var dateAdded))
      {
        dateAdded = previous is null ? DateTime.Now : originalCreation.ToLocalTime();
        if (dateAdded.Year < 1900) dateAdded = DateTime.Now;
      }
      nfo.DateAdded = dateAdded.ToString("yyyy-MM-dd HH:mm:ss", System.Globalization.CultureInfo.InvariantCulture);
      var xmlContent = NfoSerializer.SerializeXml(nfo);
      DateTime? creationTimeUtc = null;
      if (previous is not null)
      {
        if (OperatingSystem.IsWindows() || OperatingSystem.IsMacOS()) creationTimeUtc = originalCreation;
        // Keep fields from other media managers that our model does not understand.
        var next = System.Xml.Linq.XDocument.Parse(xmlContent);
        var knownNames = typeof(Movie).GetProperties()
            .SelectMany(p => p.GetCustomAttributes(typeof(XmlElementAttribute), true).Cast<XmlElementAttribute>())
            .Select(a => a.ElementName).ToHashSet(StringComparer.Ordinal);
        foreach (var element in previous.Root!.Elements().Where(e => !knownNames.Contains(e.Name.LocalName)))
            next.Root!.Add(new System.Xml.Linq.XElement(element));
        foreach (var attribute in previous.Root.Attributes().Where(a => !a.IsNamespaceDeclaration && next.Root!.Attribute(a.Name) is null))
            next.Root!.Add(new System.Xml.Linq.XAttribute(attribute));
        next.Root!.SetAttributeValue(MovieFileFacts.CreationAttribute,
            originalCreation.ToString("O", System.Globalization.CultureInfo.InvariantCulture));
        xmlContent = next.ToString();
      }
      var temporary = nfoFilePath + ".tmp";
      try
      {
        await File.WriteAllTextAsync(temporary, xmlContent, new System.Text.UTF8Encoding(false), cancellationToken);
        if (creationTimeUtc is { } created)
        {
          try { File.SetCreationTimeUtc(temporary, created); }
          catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or PlatformNotSupportedException)
          {
            // Some mounted shares cannot set birth time. The NFO attribute
            // still preserves the capture date without blocking metadata edits.
            System.Diagnostics.Trace.WriteLine($"NFO creation time retained in metadata: {ex.Message}");
          }
        }
        cancellationToken.ThrowIfCancellationRequested();
        if (File.Exists(nfoFilePath)) File.Copy(nfoFilePath, nfoFilePath + ".bak", true);
        File.Move(temporary, nfoFilePath, true);
      }
      finally { if (File.Exists(temporary)) File.Delete(temporary); }

      System.Diagnostics.Trace.WriteLine($"NFO-Datei erfolgreich gespeichert unter: {nfoFilePath}");

      return nfoFilePath;
    }

    internal static System.Xml.Linq.XDocument LoadExistingNfoDocument(string path)
    {
        try { return System.Xml.Linq.XDocument.Load(path); }
        catch (System.Xml.XmlException)
        {
            // Some older NFOs declare UTF-16 even though the file was written as UTF-8.
            using var reader = new StreamReader(path, new UTF8Encoding(false, throwOnInvalidBytes: true), detectEncodingFromByteOrderMarks: false);
            return System.Xml.Linq.XDocument.Load(reader);
        }
    }

    private static string? GetNfoFile(string folder, string video) =>
        FindNfoFile(new DirectoryInfo(folder).EnumerateFiles(), video)?.FullName;

    internal static FileInfo? FindNfoFile(IEnumerable<FileInfo> files, string video)
    {
        var candidates = files.Where(file => file.Extension.Equals(".nfo", StringComparison.OrdinalIgnoreCase)).ToArray();
        var expected = Path.GetFileNameWithoutExtension(video) + ".nfo";
        return candidates.FirstOrDefault(file => file.Name.Equals(expected, StringComparison.OrdinalIgnoreCase))
            ?? candidates.FirstOrDefault(file => file.Name.Equals("movie.nfo", StringComparison.OrdinalIgnoreCase))
            ?? (candidates.Length == 1 ? candidates[0] : null);
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
