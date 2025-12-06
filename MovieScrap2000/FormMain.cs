using Fluent;
using Fluent.Lists;
using System.Collections.Generic;
using System.Diagnostics;
using System.Security.Cryptography.Xml;
using System.Threading;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace MovieScrap2000
{
  public partial class FormMain : Form
  {
    public MovieManager MovieMan { get; set; }
    public DatabaseManager DBMan { get; set; } = new DatabaseManager();
    public MetadataService MetaService { get; set; }

    private MovieViewModel? _selectedMovieModel = null;
    private CancellationTokenSource? _loadingCts; // Zum Abbrechen von Ladevorgängen
    public MoviePictureUC MoviePictureUCClearLogo { get; set; }
    public MoviePictureUC MoviePictureUCBanner { get; set; }
    public MoviePictureUC MoviePictureUCPoster { get; set; }
    public MoviePictureUC MoviePictureUCFanart { get; set; }
    public MoviePictureUC MoviePictureUCLandscape { get; set; }
    public MoviePictureUC MoviePictureUCClearart { get; set; }
    public MoviePictureUC MoviePictureUCDiskart { get; set; }
    public MoviePictureUC MoviePictureUCKeyart { get; set; }

    private List<MovieViewModel> _movieList = new List<MovieViewModel>();
    public FormMain()
    {
      InitializeComponent();
      MoviePictureUCClearLogo = new MoviePictureUC() { PictureTyp = "ClearLogo", IsWidthAutoAdjusted = false, ShowGroupBox = false };
      MoviePictureUCClearLogo.Dock = DockStyle.Fill;
      MoviePictureUCBanner = new MoviePictureUC() { PictureTyp = "Banner", IsWidthAutoAdjusted = false, ShowGroupBox = false };
      MoviePictureUCBanner.Dock = DockStyle.Fill;
      tableLayoutPanelTitel.Controls.Add(MoviePictureUCClearLogo, 0, 0);
      tableLayoutPanelTitel.Controls.Add(MoviePictureUCBanner, 2, 0);

      MoviePictureUCPoster = new MoviePictureUC() { PictureTyp = "Poster", IsWidthAutoAdjusted = true };
      MoviePictureUCFanart = new MoviePictureUC() { PictureTyp = "Fanart", IsWidthAutoAdjusted = true };
      MoviePictureUCLandscape = new MoviePictureUC() { PictureTyp = "Landscape", IsWidthAutoAdjusted = true };
      MoviePictureUCClearart = new MoviePictureUC() { PictureTyp = "ClearArt", IsWidthAutoAdjusted = true };
      MoviePictureUCDiskart = new MoviePictureUC() { PictureTyp = "DiskArt", IsWidthAutoAdjusted = true };
      MoviePictureUCKeyart = new MoviePictureUC() { PictureTyp = "KeyArt", IsWidthAutoAdjusted = true };
      flowLayoutPanelMoviePictures.Controls.Add(MoviePictureUCPoster);
      flowLayoutPanelMoviePictures.Controls.Add(MoviePictureUCFanart);
      flowLayoutPanelMoviePictures.Controls.Add(MoviePictureUCLandscape);
      flowLayoutPanelMoviePictures.Controls.Add(MoviePictureUCClearart);
      flowLayoutPanelMoviePictures.Controls.Add(MoviePictureUCDiskart);
      flowLayoutPanelMoviePictures.Controls.Add(MoviePictureUCKeyart);
      flowLayoutPanelMoviePictures.Refresh();

      MetaService = new MetadataService(DBMan);
      MovieMan = new MovieManager(MetaService);
      MovieMan.ProgressReport += MovieMan_ProgressReport;
    }

    private void MovieMan_ProgressReport(object? sender, int percent)
    {
      // Wir prüfen InvokeRequired auf der MainForm (oder dem StatusStrip-Control), 
      // da ToolStripItem dies nicht direkt unterstützt.
      if (this.InvokeRequired)
      {
        // Führen Sie die Aktualisierung im UI-Thread durch
        this.Invoke(new Action<int>(UpdateProgressBar), percent);
      }
      else
      {
        // Wir sind bereits im UI-Thread
        UpdateProgressBar(percent);
        if (percent == 100)
          UpdateProgressBar(0);
      }
    }

    private void UpdateProgressBar(int percent)
    {
      // Sicherstellen, dass der Wert im gültigen Bereich liegt
      percent = Math.Max(0, Math.Min(100, percent));

      // Die Zuweisung des Wertes erfolgt direkt auf dem ToolStripProgressBar-Element
      toolStripProgressBarMain.Value = percent;

      if (percent == 100)
        toolStripProgressBarMain.Value = 0;
      // Optional: Den Fortschritt als Text in einem ToolStripStatusLabel anzeigen
      // statusLabelProgress.Text = $"Scanne: {percent}%"; 
    }

    private void FormMain_Load(object sender, EventArgs e)
    {
      SetupFluentListView();
    }

    private async void FormMain_Shown(object sender, EventArgs e)
    {
      // Starten Sie die langlaufende asynchrone Initialisierung, 
      // nachdem das Formular bereits sichtbar ist.
      await InitializeAppAsync();
    }

    private async Task InitializeAppAsync()
    {
      fluentListViewMovies.Enabled = false;
      try
      {
        toolStripStatusLabelMain.Text = "Datenbank wird initialisiert...";
        // Datenbank-Initialisierung (Erstellt DB und Tabellen, falls nicht existent)
        await DBMan.InitializeDatabaseAsync();

        bool isPopulated = await MetaService.IsCachePopulatedAsync();

        if (isPopulated)
        {
          // 3a. Datenbank ist gefüllt -> Schnelles Laden der Metadaten
          // Annahme: LoadAllMoviesAsync gibt eine Liste von Objekten für die Listview zurück
          _movieList = await MetaService.LoadMoviesAsync();
          fluentListViewMovies.SetObjects(_movieList);
          toolStripStatusLabelMain.Text = "Bereit.";
        }
        else
        {
          // 3b. Datenbank ist leer -> Vollständiger Scan des Dateisystems und Speichern in der DB
          // DAUERT LANGE!
          toolStripStatusLabelMain.Text = "Datenbank ist leer. Scanne Dateisystem...";
          Application.DoEvents();

          // HINWEIS: Wir müssen den MovieManager für den Scan noch neu instanziieren 
          // und ihm den MetadataService übergeben.
          MovieMan = new MovieManager(MetaService);
          MovieMan.ProgressReport += MovieMan_ProgressReport;

          // Erzwingen der Ausführung des gesamten Scan-Vorgangs im Hintergrundthread
          await Task.Run(async () =>
          {
            // Dieser Block läuft nun garantiert in einem Hintergrundthread.
            // Ruft die ursprüngliche Async-Methode auf, aber im Kontext des Task.Run
            await MovieMan.ScanDirectoryAsync("Z:\\1_42_6_25556\\Movies", CancellationToken.None);
          });

          toolStripStatusLabelMain.Text = "Scan abgeschlossen.";
          // Daten aus der DB neu laden, nachdem sie gespeichert wurden
          _movieList = await MetaService.LoadMoviesAsync();
          fluentListViewMovies.SetObjects(_movieList);
        }
      }
      catch (Exception ex)
      {
        Trace.WriteLine(ex.Message);
      }
      finally
      {
        fluentListViewMovies.Enabled = true;
      }
    }
    private void SetupFluentListView()
    {
      // Sicherstellen, dass die Liste leer und korrekt konfiguriert ist
      fluentListViewMovies.View = View.Details;
      fluentListViewMovies.Columns.Clear();
      fluentListViewMovies.FullRowSelect = true; // Wichtig für eine bessere Benutzererfahrung

      // --- Spalten-Definitionen ---

      // 1. ANZEIGE-TITEL (Hauptspalte)
      fluentListViewMovies.Columns.Add(new OLVColumn("Filmtitel", nameof(MovieViewModel.Title))
      {
        Width = 250,
      });

      // 2. JAHR
      fluentListViewMovies.Columns.Add(new OLVColumn("Jahr", nameof(MovieViewModel.Year))
      {
        Width = 70,
        TextAlign = HorizontalAlignment.Center
      });


      // 5. Ordnerpfad (wichtig für Debugging, kann versteckt werden)
      fluentListViewMovies.Columns.Add(new OLVColumn("Genre", nameof(MovieViewModel.GenresString))
      {
        Width = 150,
        IsVisible = false // Versteckt die Spalte standardmäßig
      });
    }

    private async void fluentListViewMovies_SelectionChanged(object sender, EventArgs e)
    {
      _selectedMovieModel = null;
      if (fluentListViewMovies.SelectedObject is MovieViewModel movieInfo)
        _selectedMovieModel = movieInfo;

      await UpdateDetailPanelAsync();
    }

    private async Task UpdateDetailPanelAsync()
    {
      // Klonen _selectedMovieInfo, um Konsistenz während der asynchronen Operation zu gewährleisten
      MovieViewModel? currentMovieInfo = _selectedMovieModel ?? null;

      if (currentMovieInfo == null)
      {
        textBoxMovieTag.Text = string.Empty;
        textBoxMovieDirector.Text = string.Empty;
        textBoxMovieTitle.Text = string.Empty;
        textBoxMovieTitleOri.Text = string.Empty;
        textBoxMovieYear.Text = string.Empty;
        textBoxMoviePlot.Text = string.Empty;
        textBoxMovieInhalt.Text = string.Empty;
        dateTimePickerMovieRelease.Text = string.Empty;
        dateTimePickerMovieAdded.Text = string.Empty;
        textBoxMovieRating.Text = string.Empty;
        textBoxMovieGenre.Text = string.Empty;
        textBoxMovieStudio.Text = string.Empty;
        textBoxMovieCountry.Text = string.Empty;
        textBoxMovieLaufzeit.Text = string.Empty;
        textBoxMovieIMDb.Text = string.Empty;
        MoviePictureUCBanner.SetPlaceHolderEmpty();
        MoviePictureUCClearLogo.SetPlaceHolderEmpty();
        MoviePictureUCPoster.SetPlaceHolderEmpty();
        MoviePictureUCFanart.SetPlaceHolderEmpty();
        MoviePictureUCLandscape.SetPlaceHolderEmpty();
        MoviePictureUCClearart.SetPlaceHolderEmpty();
        MoviePictureUCDiskart.SetPlaceHolderEmpty();
        MoviePictureUCKeyart.SetPlaceHolderEmpty();
        foreach (Control control in flowLayoutPanelActors.Controls)
        {
          if (control is MoviePictureUC actorUC)
          {
            actorUC.PictureUpdateRequested -= MainForm_HandlePictureUpdate;
            actorUC.PictureDeleteRequested -= MainForm_HandlePictureDelete;
            actorUC.PictureFilterRequested -= MainForm_HandlePictureFilter;
          }
        }
        flowLayoutPanelActors.Controls.Clear();
        return;
      }

      // 1. Volle Details asynchron laden
      try
      {
        // Wir übergeben den MetadataService, um die DB-Abfrage auszulösen
        Trace.WriteLine($"Lade Volldetails für Film: {currentMovieInfo.Title}");
        await currentMovieInfo.LoadFullDetailsAsync(MetaService);
      }
      catch (Exception ex)
      {
        Trace.WriteLine($"Fehler beim Vollladen der Movie-Details: {ex.Message}");
        return;
      }

      // Sicherheitsprüfung (für den Fall, dass die Auswahl gewechselt wurde)
      if (currentMovieInfo.Id != _selectedMovieModel?.Id) return;

      // 2. Detail-Panel mit den geladenen Daten füllen
      // Die Daten stammen nun aus dem deserialisierten NFO - Objekt(FullMovieNFO)
      var nfo = currentMovieInfo.FullMovieNFO;

      textBoxMovieDirector.Text = nfo?.DisplayDirectors ?? string.Empty;
      textBoxMovieTitle.Text = currentMovieInfo.Title ?? currentMovieInfo.Title;
      textBoxMovieTag.Text = nfo?.Tagline ?? string.Empty;
      textBoxMovieTitleOri.Text = nfo?.OriginalTitle ?? string.Empty;
      textBoxMovieYear.Text = currentMovieInfo.Year ?? string.Empty;
      textBoxMovieInhalt.Text = currentMovieInfo.Plot ?? string.Empty;
      textBoxMoviePlot.Text = nfo?.Outline ?? string.Empty;
      dateTimePickerMovieRelease.Value = nfo?.ReleaseDateTime ?? DateTime.Now;
      dateTimePickerMovieAdded.Value = nfo?.DateAddedDateTime ?? DateTime.Now;
      textBoxMovieRating.Text = nfo?.RatingString ?? string.Empty;
      textBoxMovieGenre.Text = nfo?.DisplayGenres ?? string.Empty;
      textBoxMovieStudio.Text = nfo?.DisplayStudios ?? string.Empty;
      textBoxMovieCountry.Text = nfo?.DisplayCountries ?? string.Empty;
      textBoxMovieLaufzeit.Text = nfo?.Runtime.ToString() ?? string.Empty;
      textBoxMovieIMDb.Text = nfo?.Id ?? string.Empty;

      // 3. Bild-Cache-Prüfung und Laden

      // Vorherige Bilder zurücksetzen
      MoviePictureUCBanner.SetPlaceHolderEmpty(); MoviePictureUCBanner.MovieId = currentMovieInfo.Id;
      MoviePictureUCClearLogo.SetPlaceHolderEmpty(); MoviePictureUCClearLogo.MovieId = currentMovieInfo.Id;
      MoviePictureUCPoster.SetPlaceHolderEmpty(); MoviePictureUCPoster.MovieId = currentMovieInfo.Id;
      MoviePictureUCFanart.SetPlaceHolderEmpty(); MoviePictureUCFanart.MovieId = currentMovieInfo.Id;
      MoviePictureUCLandscape.SetPlaceHolderEmpty(); MoviePictureUCLandscape.MovieId = currentMovieInfo.Id;
      MoviePictureUCClearart.SetPlaceHolderEmpty(); MoviePictureUCClearart.MovieId = currentMovieInfo.Id;
      MoviePictureUCDiskart.SetPlaceHolderEmpty(); MoviePictureUCDiskart.MovieId = currentMovieInfo.Id;
      MoviePictureUCKeyart.SetPlaceHolderEmpty(); MoviePictureUCKeyart.MovieId = currentMovieInfo.Id;
      flowLayoutPanelActors.Controls.Clear();

      // Abbrechen der vorherigen Bildlade-Operation, falls eine läuft
      _loadingCts?.Cancel();
      _loadingCts?.Dispose(); // Wichtig: alte CTS entsorgen
      _loadingCts = new CancellationTokenSource();
      CancellationToken cancellationToken = _loadingCts.Token;
      // 2. Asynchrone Bildsuche starten und warten
      try
      {
        // Zunächst ohne Daten laden, um die Controls anzulegen
        // Das Laden der Bilder kommt danach
        Trace.WriteLine($"Lade Bild-Platzhalter für Film: {currentMovieInfo.Title}");
        var picPathsEmpty = await MetaService.GetImageCacheItemsWithOutDataAsync(currentMovieInfo.Id,
            currentMovieInfo.FolderPath,
            currentMovieInfo.VideoFilePath,
                cancellationToken);

        if (currentMovieInfo.Id != _selectedMovieModel?.Id)
          return;

        foreach (var p in picPathsEmpty)
        {
          switch (p.Type)
          {
            case ImageTypes.Banner: MoviePictureUCBanner.SetPlaceHolderLoading(); break;
            case ImageTypes.ClearLogo: MoviePictureUCClearLogo.SetPlaceHolderLoading(); break;
            case ImageTypes.Poster: MoviePictureUCPoster.SetPlaceHolderLoading(); break;
            case ImageTypes.Fanart: MoviePictureUCFanart.SetPlaceHolderLoading(); break;
            case ImageTypes.Landscape: MoviePictureUCLandscape.SetPlaceHolderLoading(); break;
            case ImageTypes.ClearArt: MoviePictureUCClearart.SetPlaceHolderLoading(); break;
            case ImageTypes.DiskArt: MoviePictureUCDiskart.SetPlaceHolderLoading(); break;
            case ImageTypes.KeyArt: MoviePictureUCKeyart.SetPlaceHolderLoading(); break;
            default: break;
          }
        }

        List<MoviePictureUC> actorListUc = new List<MoviePictureUC>();
        if (currentMovieInfo.Actors != null)
        {
          foreach (var actor in currentMovieInfo.Actors)
          {
            MoviePictureUC act = new MoviePictureUC()
            {
              PictureTyp = actor.Name,
              ToolTipText = actor.Role,
              IsWidthAutoAdjusted = false,
              Width = 126
            };
            act.SetPlaceHolderEmpty();
            act.Tag = actor;
            act.MovieId = currentMovieInfo.Id;
            actorListUc.Add(act);
            flowLayoutPanelActors.Controls.Add(act);
          }
        }
        // --- Paralleles Laden aller Bilder starten ---

        // a) Im ImageCache prüfen, ob Bilder (als BLOB) vorhanden sind.
        // b) Falls nicht, Bilder von der Festplatte/Web laden.
        // c) Geladene Bilder als BLOBs in die ImageCache-Tabelle speichern.
        // d) Die Bilder als Pfade/Speicher (MemoryStream) für das UI zurückgeben.
        Trace.WriteLine($"Lade Bild-Daten für Film: {currentMovieInfo.Title}");
        var picPaths = await MetaService.GetOrCacheMoviePicturesAsync(currentMovieInfo.Id,
            currentMovieInfo.FolderPath,
            currentMovieInfo.VideoFilePath,
                cancellationToken);

        // Wir erstellen eine Liste von Tasks, die alle gleichzeitig starten.
        var pictureLoadingTasks = new List<Task>();
        foreach (var pic in picPaths)
        {
          Trace.WriteLine($"Bild gefunden: Typ={pic.Type}, Datenlänge={(pic.Data != null ? pic.Data.Length : 0)} Bytes");
          switch (pic.Type)
          {
            case ImageTypes.Banner:
              pictureLoadingTasks.Add(MoviePictureUCBanner.LoadPictureAsync(pic, cancellationToken));
              break;
            case ImageTypes.ClearLogo:
              pictureLoadingTasks.Add(MoviePictureUCClearLogo.LoadPictureAsync(pic, cancellationToken));
              break;
            case ImageTypes.Poster:
              pictureLoadingTasks.Add(MoviePictureUCPoster.LoadPictureAsync(pic, cancellationToken));
              break;
            case ImageTypes.Fanart:
              pictureLoadingTasks.Add(MoviePictureUCFanart.LoadPictureAsync(pic, cancellationToken));
              break;
            case ImageTypes.Landscape:
              pictureLoadingTasks.Add(MoviePictureUCLandscape.LoadPictureAsync(pic, cancellationToken));
              break;
            case ImageTypes.ClearArt:
              pictureLoadingTasks.Add(MoviePictureUCClearart.LoadPictureAsync(pic, cancellationToken));
              break;
            case ImageTypes.DiskArt:
              pictureLoadingTasks.Add(MoviePictureUCDiskart.LoadPictureAsync(pic, cancellationToken));
              break;
            case ImageTypes.KeyArt:
              pictureLoadingTasks.Add(MoviePictureUCKeyart.LoadPictureAsync(pic, cancellationToken));
              break;
            default: Debug.Assert(false); break;
          }
        }

        if (currentMovieInfo.Actors != null)
        {
          foreach (var actor in currentMovieInfo.Actors)
          {
            var data = await MetaService.GetOrCreateActorThumbAsync(actor.Name, actor.TmdbId, actor.Thumb, actor.ThumbData, cancellationToken);
            if (data.ActorId >= 0 && data.Data != null)
            {
              var uc = actorListUc.FirstOrDefault(x => x.Tag is Actor && ((Actor)x.Tag).Name == actor.Name);
              if (uc != null)
              {
                uc.ActorId = data.ActorId;
                uc.PictureUpdateRequested += MainForm_HandlePictureUpdate;
                uc.PictureDeleteRequested += MainForm_HandlePictureDelete;
                uc.PictureFilterRequested += MainForm_HandlePictureFilter;
                uc.SetPlaceHolderLoading();
                pictureLoadingTasks.Add(uc.LoadPictureAsync(data.Data, cancellationToken));
              }
            }
          }
        }

        // Auf den Abschluss ALLER Ladevorgänge warten
        // Der UI-Thread ist währenddessen NICHT blockiert.
        try
        {
          await Task.WhenAll(pictureLoadingTasks);
        }
        catch (OperationCanceledException)
        {
          // Dies fängt Abbrüche ab, wenn der Benutzer einen neuen Eintrag auswählt, 
          // während Bilder geladen werden.
          // Das ist das normale, erwartete Verhalten bei Abbruch.
          return;
        }
        catch (Exception ex)
        {
          // Behandeln Sie alle anderen Fehler, die beim Laden aufgetreten sind.
          Trace.WriteLine($"Fehler beim Laden der Bilder für '{currentMovieInfo.Title}': {ex.Message}");
        }

      }
      catch (OperationCanceledException)
      {
        // Dies ist normal, wenn der Benutzer einen anderen Film ausgewählt hat.
        // Nichts tun, oder optional eine Log-Meldung ausgeben.
        Trace.WriteLine($"Bildladen für '{currentMovieInfo.Title}' abgebrochen.");
      }
      catch (Exception ex)
      {
        // Andere Fehler (z.B. Datei nicht gefunden, korruptes Bild)
        Trace.WriteLine($"Fehler beim Laden der Bilder für '{currentMovieInfo.Title}': {ex.Message}");
      }
      finally
      {
        flowLayoutPanelMoviePictures.PerformLayout();
      }
    }

    private async void MainForm_HandlePictureUpdate(object? sender, PictureUpdateEventArgs e)
    {
      if (sender is MoviePictureUC)
      {
        MoviePictureUC uc = (MoviePictureUC)sender;
        if (e.ActorId != null && e.ActorId >= 0)
        {
          try
          {
            await MetaService.UpdateActorThumbExternalAsync(
                    (int)e.ActorId,
                    e.NewImageData,
                    CancellationToken.None);

            // Optional: Bestätigung oder Log-Eintrag
            Trace.WriteLine($"Actor ID {e.ActorId} Bild erfolgreich aktualisiert.");
            // Form aktualisieren
            await UpdateDetailPanelAsync();
          }
          catch (Exception ex)
          {
            MessageBox.Show($"Datenbankfehler beim Speichern des Actor-Bildes: {Utils.GetExcMsg(ex)}", "Fehler", MessageBoxButtons.OK, MessageBoxIcon.Error);
          }
        }
        else
        {
          // TODO
        }
      }
    }

    private async void MainForm_HandlePictureDelete(object? sender, PictureEventArgs e)
    {
      if (sender is MoviePictureUC && _selectedMovieModel != null)
      {
        MoviePictureUC uc = (MoviePictureUC)sender;
        if (e.ActorId != null && e.ActorId >= 0 && e.MovieId != null && e.MovieId >= 0)
        {
          int movieId = (int)e.MovieId;
          int actorId = (int)e.ActorId;

          // Wir benötigen das aktuell geladene Modell für das Update
          var currentModel = _selectedMovieModel;
          if (currentModel == null || currentModel.Id != movieId)
            return;

          try
          {
            // Bestehende Verknüpfung löschen
            await MetaService.DeleteMovieActorLinkAsync(
                movieId,
                actorId,
                CancellationToken.None);
            Trace.WriteLine($"Actor ID {actorId} erfolgreich aus Movie ID {movieId} entfernt.");
            // Die Actor-Liste im aktuellen Modell neu aus der DB laden
            await currentModel.LoadLinkedDataAsync(MetaService, CancellationToken.None);
            Trace.WriteLine($"Actor-Liste erfolgreich aus Movie ID {movieId} neu geladen.");
            // Form aktualisieren
            await UpdateDetailPanelAsync();
          }
          catch (Exception ex)
          {
            MessageBox.Show($"Datenbankfehler beim Entfernen des Actor: {Utils.GetExcMsg(ex)}", "Fehler", MessageBoxButtons.OK, MessageBoxIcon.Error);
          }
        }
        else
        {
          // TODO
        }
      }
    }

    private async void MainForm_HandlePictureFilter(object? sender, PictureEventArgs e)
    {
      if (sender is MoviePictureUC && _selectedMovieModel != null)
      {
        MoviePictureUC uc = (MoviePictureUC)sender;
        if (e.ActorId != null && e.ActorId >= 0 && e.MovieId != null && e.MovieId >= 0)
        {
          int movieId = (int)e.MovieId;
          int actorId = (int)e.ActorId;

          // Wir benötigen das aktuell geladene Modell für die spätere Selektion
          var currentModel = _selectedMovieModel;
          if (currentModel == null || currentModel.Id != movieId)
            return;

          try
          {
            // Filter anwenden
            var moviesfiltered = await MetaService.GetMovieIdsByActorIdAsync(actorId, CancellationToken.None);
            if (moviesfiltered != null && moviesfiltered.Count > 0)
            {
              var filteredMovieList = _movieList.Where(m => moviesfiltered.Contains(m.Id)).ToList();
              fluentListViewMovies.SetObjects(filteredMovieList);
              // Selektiere den alten Film in der gefilterten Liste
              var myMovie = filteredMovieList.FirstOrDefault(x => x.Id == currentModel.Id);
              if (myMovie != null)
              {
                fluentListViewMovies.SelectedObject = myMovie;
              }
              trackBarFilterOnOff.Value = 1;
            }
          }
          catch (Exception ex)
          {
            MessageBox.Show($"Fehler beim Filtern der Liste nach Actor: {Utils.GetExcMsg(ex)}", "Fehler", MessageBoxButtons.OK, MessageBoxIcon.Error);
          }
        }
        else
        {
          // TODO
        }
      }
    }

    private async void toolStripMenuItemMovieListAusNFONeuEinlesen_Click(object sender, EventArgs e)
    {
      var selMovies = fluentListViewMovies.SelectedObjects.Cast<MovieViewModel>();
      if (selMovies == null || !selMovies.Any())
      {
        MessageBox.Show("Es sind keine Filme zum Aktualisieren selektiert.", "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
        return;
      }

      MovieViewModel firstmm = selMovies.First();

      // 1. Initialisiere Abbruch-Token und Starte den UI-Status
      _loadingCts = new CancellationTokenSource();
      var cancellationToken = _loadingCts.Token;

      int successCount = 0;
      int errorCount = 0;

      try
      {
        // 1. Parallelisiertes Update über den Manager
        var updateTasks = selMovies.Select(movieVm =>
                MovieMan.UpdateMovieMetadataFromNfoAsync(
                        movieVm.Id,
                        movieVm.FolderPath,
                        movieVm.VideoFilePath,
                        cancellationToken)
        ).ToList();

        // 2. Auf alle Updates warten
        var results = await Task.WhenAll(updateTasks);

        successCount = results.Count(r => r);
        errorCount = results.Count(r => !r);

        // 2. Datenbank: Komplette Liste neu laden (Reload)
        // Die alte _movieList wird durch die frischen Daten aus der DB ersetzt
        Trace.WriteLine("Lade gesamte Filmliste aus Datenbank neu...");
        _movieList = await MetaService.LoadMoviesAsync();
        fluentListViewMovies.Objects = null;
        fluentListViewMovies.SetObjects(_movieList);

        _selectedMovieModel = _movieList.FirstOrDefault(x => x.FolderPath == firstmm.FolderPath);
        await UpdateDetailPanelAsync();

        MessageBox.Show($"Aktualisierung abgeschlossen.\n{successCount} Filme erfolgreich aktualisiert.\n{errorCount} Fehler.",
                                        "Erfolg", MessageBoxButtons.OK, MessageBoxIcon.Information);
      }
      catch (OperationCanceledException)
      {
        MessageBox.Show("Aktualisierung vom Benutzer abgebrochen.", "Abgebrochen", MessageBoxButtons.OK, MessageBoxIcon.Warning);
      }
      catch (Exception ex)
      {
        MessageBox.Show($"Ein unerwarteter Fehler ist aufgetreten: {ex.Message}", "Fehler", MessageBoxButtons.OK, MessageBoxIcon.Error);
      }
      finally
      {
        // SetUiState(false, "Bereit");
        _loadingCts?.Dispose();
        _loadingCts = null;
      }
    }

    private async void toolStripMenuItemMovieListScrap_Click(object sender, EventArgs e)
    {
      if (_selectedMovieModel != null)
      {
        FormScrap frm = new FormScrap(_selectedMovieModel.FullMovieNFO ?? new Movie());
        if (frm.ShowDialog() == DialogResult.OK)
        {
          if (frm.ActorListUc.Count > 0)
          {
            var newlist = new List<Actor>();
            // Es sind bereits Actors vorhanden, dann neue dazupacken, Änderungen überschreiben
            if (_selectedMovieModel.Actors != null)
            {
              // 1. Iteriere über die NEUEN Scrap-Actors
              foreach (var scrapactor in frm.ActorListUc)
              {
                TMDbScraper.Cast? c = scrapactor.Tag as TMDbScraper.Cast;
                if (c == null) continue;

                // Versuche, den Actor über TMDbId oder Name zu finden
                Actor? ac = _selectedMovieModel.Actors.FirstOrDefault(a =>
                        (c.Id != 0 && a.TmdbId == c.Id) || (a.Name == c.Name));

                if (ac != null)
                {
                  // A) Actor existiert bereits -> AKTUALISIEREN & MARKIEREN
                  ac.Role = c.Character ?? ac.Role;
                  ac.TmdbId = c.Id != 0 ? c.Id : ac.TmdbId;
                  ac.ThumbData = c.PictureData ?? ac.ThumbData;

                  // Markiere den vorhandenen Actor als BEHALTEN
                  if (ac.Id > 0)
                    newlist.Add(ac.Copy());
                }
                else
                {
                  // B) Neuer Actor -> HINZUFÜGEN
                  ac = new Actor()
                  {
                    Name = c.Name ?? string.Empty,
                    Role = c.Character ?? string.Empty,
                    TmdbId = c.Id,
                    Thumb = c.ProfilePath ?? string.Empty,
                    ThumbData = c.PictureData ?? null
                  };
                  newlist.Add(ac);
                  // NEUE Actors brauchen nicht markiert zu werden, da sie keine alten IDs haben.
                }
              }
            }
            // Es sind gar keine Actors vorhanden (ActorListUc.Count > 0)
            else
            {
              // Ersetze die Actors-Liste vollständig durch die Scrap-Daten
              foreach (var scrapactor in frm.ActorListUc)
              {
                TMDbScraper.Cast? c = scrapactor.Tag as TMDbScraper.Cast;
                if (c != null)
                {
                  newlist.Add(new Actor
                  {
                    Name = c.Name ?? string.Empty,
                    Role = c.Character ?? string.Empty,
                    TmdbId = c.Id,
                    Thumb = c.ProfilePath ?? string.Empty,
                    ThumbData = c.PictureData ?? null
                  });
                }
              }
            }

            _selectedMovieModel.Actors = newlist;
          }
          // ----------------------------------------------------
          // Wenn KEINE Actors gescrapt wurden (frm.ActorListUc.Count == 0), 
          // aber das Modell Actors hat, müssen diese gelöscht werden.
          // ----------------------------------------------------
          else if (_selectedMovieModel.Actors != null && _selectedMovieModel.Actors.Any())
          {
            _selectedMovieModel.Actors = new List<Actor>();
          }

          var scrapmovie = frm.SelectedMovie;
          if (scrapmovie != null)
          {
            if (!string.IsNullOrEmpty(scrapmovie.Title))
              _selectedMovieModel.Title = scrapmovie.Title;
            if (!string.IsNullOrEmpty(scrapmovie.OriginalTitle) && _selectedMovieModel.FullMovieNFO != null)
              _selectedMovieModel.FullMovieNFO.OriginalTitle = scrapmovie.OriginalTitle;
            if (!string.IsNullOrEmpty(scrapmovie.DirectorsString) && _selectedMovieModel.FullMovieNFO != null)
              _selectedMovieModel.FullMovieNFO.Directors = scrapmovie.Credits.Crew
                  .Where(s => s.Job == "Director" && !string.IsNullOrEmpty(s.Name))
                  .Select(s => s.Name!).ToList();
            if (scrapmovie.Genres.Count > 0)
              _selectedMovieModel.GenresString = string.Join("; ", scrapmovie.Genres
                  .Where(s => !string.IsNullOrEmpty(s.Name)) // 1. Filtern: Entferne alle Einträge, deren Name null oder leer ist.
                  .Select(s => s.Name!)); // 2. Projizieren: Wähle nur den Namen aus den gefilterten Ergebnissen.
            if (!string.IsNullOrWhiteSpace(scrapmovie.Overview))
              _selectedMovieModel.Plot = scrapmovie.Overview;
            if (scrapmovie.ProductionCompanies.Count > 0 && _selectedMovieModel.FullMovieNFO != null)
              _selectedMovieModel.FullMovieNFO.Studios = scrapmovie.ProductionCompanies
                  .Where(s => !string.IsNullOrEmpty(s.Name)) // 1. Filtern: Entferne alle Einträge, deren Name null oder leer ist.
                  .Select(s => s.Name!) // 2. Projizieren: Wähle nur den Namen aus den gefilterten Ergebnissen.
                  .ToList();
            if (scrapmovie.ProductionCountries.Count > 0 && _selectedMovieModel.FullMovieNFO != null)
              _selectedMovieModel.FullMovieNFO.Country = scrapmovie.ProductionCountries
                  .Where(s => !string.IsNullOrEmpty(s.Name)) // 1. Filtern: Entferne alle Einträge, deren Name null oder leer ist.
                  .Select(s => s.Name!) // 2. Projizieren: Wähle nur den Namen aus den gefilterten Ergebnissen.
                  .ToList();
            if (scrapmovie.ReleaseDate.HasValue)
            {
              if (_selectedMovieModel.FullMovieNFO != null)
                _selectedMovieModel.FullMovieNFO.ReleaseDate = scrapmovie.ReleaseDate.Value.ToShortDateString();
              _selectedMovieModel.Year = scrapmovie.ReleaseDate.Value.Year.ToString();
            }
            if (scrapmovie.Runtime.HasValue && _selectedMovieModel.FullMovieNFO != null)
              _selectedMovieModel.FullMovieNFO.Runtime = scrapmovie.Runtime.Value;
            if (!string.IsNullOrWhiteSpace(scrapmovie.Tagline) && _selectedMovieModel.FullMovieNFO != null)
              _selectedMovieModel.FullMovieNFO.Tagline = scrapmovie.Tagline;
            if (scrapmovie.VoteAverage > 0.0 && _selectedMovieModel.FullMovieNFO != null)
              _selectedMovieModel.FullMovieNFO.Rating = (float)scrapmovie.VoteAverage;


          }

          // Film speichern (MetaService.SaveMovieAsync muss die Junction-Tabellen löschen/neu erstellen!)
          await MetaService.SaveMovieAsync(_selectedMovieModel.FullMovieNFO!,
                  _selectedMovieModel.FolderPath, _selectedMovieModel.VideoFilePath, false, CancellationToken.None);
          await UpdateDetailPanelAsync();
        } // Scrap-Dialog wurde abgebrochen
      }
    }

    private void trackBarFilterOnOff_Scroll(object sender, EventArgs e)
    {
      if (trackBarFilterOnOff.Value == 0)
      {
        // Filter aus - komplette Liste anzeigen
        fluentListViewMovies.SetObjects(_movieList);
      }
    }
  }
}
		
	
