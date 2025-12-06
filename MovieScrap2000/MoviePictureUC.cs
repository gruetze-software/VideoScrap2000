using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.Button;

namespace MovieScrap2000
{
  public partial class MoviePictureUC : UserControl
  {
    // Platzhalterbild (MUSS im Konstruktor geladen werden!)
    private Image? _loadingPlaceholder;
    private Image? _emptyPlaceholder;

    private string? _picType = "Picture";
    public string? PictureTyp
    {
      get { return _picType; }
      set
      {
        _picType = value;
        groupBoxPicture.Text = _picType; 
        AutoScaleGroupBoxTitle(); 
        toolStripTextBoxPicType.Text = _picType;
        filterToolStripMenuItem.Visible = string.IsNullOrEmpty(_picType) || !_imageNonActors.Contains(_picType);
      }
    }
    private bool _showGroupBox = true;
    public bool ShowGroupBox { get { return _showGroupBox; } set { _showGroupBox = value; AdjustGroupBox(); } }
    public ImageCacheItem? ImageItem { get; set; }
    public int? ActorId { get; set; }
    public int? MovieId { get; set; }

    private bool _closebutton = false;
    public bool CloseButton { get { return _closebutton; } set { _closebutton = value; labelClose.Visible = _closebutton; } }

    public bool HasImageData { get { return ImageItem != null && ImageItem.Data != null && ImageItem.Data != Array.Empty<byte>(); } }

    private string? _nottext = null;
    public string? ToolTipText
    {
      get { return _nottext; }
      set { _nottext = value; if (string.IsNullOrEmpty(_nottext)) toolTipPicture.RemoveAll(); else toolTipPicture.SetToolTip(pictureBoxImage, _nottext); }
    }
    /// <summary>
    /// Steuert, ob die Breite des UserControls automatisch proportional zur Höhe des geladenen Bildes gesetzt wird.
    /// </summary>
    public bool IsWidthAutoAdjusted { get; set; } = false;

    public event PictureUpdateEventHandler? PictureUpdateRequested;
    public event PictureEventHandler? PictureDeleteRequested;
    public event PictureEventHandler? PictureCloseRequested;
    public event PictureEventHandler? PictureFilterRequested;

    private List<string> _imageNonActors = new List<string> {
        ImageTypes.Poster, ImageTypes.Fanart, ImageTypes.ClearLogo,
        ImageTypes.Banner, ImageTypes.Landscape, ImageTypes.ClearArt,
        ImageTypes.KeyArt, ImageTypes.DiskArt };

    public MoviePictureUC()
    {
      InitializeComponent();

      // Laden Sie hier Ihr Platzhalterbild (z.B. ein 'Loading'-Icon).
      // Dies muss nur einmal erfolgen. Verwenden Sie einen Pfad zu Ihrer Ressource.
      // Beispiel: Laden aus Ressourcen oder einem festen Pfad
      try
      {
        _loadingPlaceholder = Properties.Resources.loading;
        _emptyPlaceholder = Properties.Resources.Empty;
        SetPlaceHolderEmpty();
      }
      catch { /* Optional: Fehler bei fehlender Lade-Grafik ignorieren */ }
    }

    private void MoviePictureUC_Load(object sender, EventArgs e)
    {
      labelClose.Visible = CloseButton;
      labelClose.BringToFront();
    }

    public void SetPlaceHolderEmpty()
    {
      if (pictureBoxImage.Image != _emptyPlaceholder)
      {
        DisposeCurrentPicture();
        pictureBoxImage.Image = _emptyPlaceholder;
        ImageItem = null;
      }

      if (IsWidthAutoAdjusted)
      {
        CalculateAndApplyNewSize();
      }
    }

    public void SetPlaceHolderLoading()
    {
      if (pictureBoxImage.Image != _loadingPlaceholder)
      {
        DisposeCurrentPicture();
        pictureBoxImage.Image = _loadingPlaceholder;
      }
    }

    private void AdjustGroupBox()
    {
      if (ShowGroupBox)
      {
        if (!this.Controls.Contains(groupBoxPicture))
        {
          this.Controls.Remove(tableLayoutPanelPicture);
          this.Controls.Add(groupBoxPicture);
          groupBoxPicture.Controls.Add(tableLayoutPanelPicture);
          tableLayoutPanelPicture.Dock = DockStyle.Fill;
          groupBoxPicture.Dock = DockStyle.Fill;
        }
      }
      else
      {
        if (this.Controls.Contains(groupBoxPicture))
        {
          groupBoxPicture.Controls.Remove(tableLayoutPanelPicture);
          this.Controls.Remove(groupBoxPicture);
          this.Controls.Add(tableLayoutPanelPicture);
          tableLayoutPanelPicture.Dock = DockStyle.Fill;
        }
      }
    }

    public async Task LoadPictureAsync(byte[]? data, CancellationToken externalToken)
    {
      if (data == null || data.Length == 0)
      {
        SetPlaceHolderEmpty();
        return;
      }

      ImageCacheItem imageCacheItem = new ImageCacheItem() { Data = data };
      await LoadPictureAsync(imageCacheItem, externalToken);
    }

    /// <summary>
    /// Passt die Schriftgröße der GroupBox so an, dass der Text in die verfügbare Breite passt.
    /// </summary>
    /// <param name="groupBox">Die GroupBox, deren Schriftgröße angepasst werden soll.</param>
    /// <param name="targetText">Der vollständige Text, der zugewiesen wurde.</param>
    /// <param name="maxSize">Die maximale erlaubte Schriftgröße (z.B. 9.0f).</param>
    /// <param name="minSize">Die minimale erlaubte Schriftgröße (z.B. 6.0f).</param>
    private void AutoScaleGroupBoxTitle(float maxSize = 9.0f, float minSize = 6.0f)
    {
      if (string.IsNullOrEmpty(PictureTyp))
      {
        // Setze auf die Standardgröße zurück, wenn kein Text vorhanden ist.
        groupBoxPicture.Font = new Font(groupBoxPicture.Font.FontFamily, maxSize, groupBoxPicture.Font.Style);
        return;
      }

      // Prüfen, ob die Breite realistisch ist. Wenn 0, frühzeitig beenden oder Standardbreite verwenden.
      if (groupBoxPicture.ClientSize.Width < 50)
      {
        groupBoxPicture.Font = new Font(groupBoxPicture.Font.FontFamily, maxSize, groupBoxPicture.Font.Style);
        return;
      }

      int closelabelWidth = labelClose.Visible ? labelClose.Width + 4 : 0;

      // Die verfügbare Breite ist etwas geringer als die Gesamtbreite der GroupBox
      // aufgrund von Rahmen und Padding. Ein Puffer von ca. 15-20 Pixeln ist ratsam.
      int availableWidth = groupBoxPicture.ClientSize.Width - 20 - closelabelWidth;
      if (availableWidth < 30)
        availableWidth = 30; // Min. Breite, um Endlosschleife oder Fehler zu vermeiden

      // Starte mit der maximalen Größe
      float currentSize = maxSize;

      // Die Schriftfamilie und der Stil bleiben unverändert
      FontFamily family = groupBoxPicture.Font.FontFamily;
      FontStyle style = groupBoxPicture.Font.Style;

      while (currentSize >= minSize)
      {
        using (Font testFont = new Font(family, currentSize, style))
        {
          // Messt die Breite des Textes mit der aktuellen Schriftgröße
          Size textSize = TextRenderer.MeasureText(PictureTyp, testFont);

          if (textSize.Width <= availableWidth)
          {
            // Passt: Setze die neue, kleinere Schriftgröße und beende die Schleife
            groupBoxPicture.Font = new Font(family, currentSize, style);
            return;
          }
        }

        // Text ist zu breit, reduziere die Größe schrittweise
        currentSize -= 0.5f; // Reduziere in 0.5er Schritten
      }

      // Wenn die minimale Größe erreicht ist und der Text immer noch nicht passt,
      // verwenden wir die minimale Größe und akzeptieren eventuelles Abschneiden.
      groupBoxPicture.Font = new Font(family, minSize, style);
    }

    public async Task LoadPictureAsync(ImageCacheItem? item, CancellationToken externalToken)
    {
      ImageItem = item;
      // --- Phase 1: Vorbereitung und Abbruch ---

      // Wir nutzen das externe Token und das implizite Abbruch-Handling
      // durch OperationCanceledException, wenn der Benutzer einen neuen Film auswählt.

      // 1. Wenn die Daten leer sind, alles leeren und beenden
      if (!HasImageData || ImageItem == null)
      {
        SetPlaceHolderEmpty();
        return;
      }

      // 2. Platzhalter anzeigen und Aufräumen des alten Bildes
      // Da wir das externe Token verwenden, müssen wir hier nicht das interne _loadingCts verwalten.
      DisposeCurrentPicture();

      // (Optionaler Guard: Hier könnte man die Hash-ID der Bytes speichern, wenn man wirklich
      // ein Stale-Data-Guard benötigt, aber CancellationToken ist die Hauptlösung.)

      // --- Phase 2: Asynchrones Laden ---
      try
      {
        // Laden des Bildes in einem Hintergrund-Task
        Image loadedImage = await Task.Run(() =>
        {
          // Sicherstellen, dass die Operation abgebrochen werden kann
          externalToken.ThrowIfCancellationRequested();

          // Verwenden des MemoryStreams anstelle von FileStream
          using (var stream = new MemoryStream(ImageItem.Data))
          {
            // Rückgabe einer Kopie des Bildes, um den Stream sofort schließen zu können
            // .Clone() ist wichtig, da FromStream das Image-Objekt an den Stream koppeln kann.
            return (Image)Image.FromStream(stream).Clone();
          }
        }, externalToken); // Übergeben Sie das Token an Task.Run

        // --- Phase 3: Abschluss und Zuweisung ---

        // Prüfen, ob in der Zwischenzeit ein Abbruch angefordert wurde
        externalToken.ThrowIfCancellationRequested();

        // Altes Platzhalter- oder Bild entsorgen, bevor das neue zugewiesen wird
        DisposeCurrentPicture();
        pictureBoxImage.Image = loadedImage;

        if (IsWidthAutoAdjusted)
        {
          // Führt die proportionale Breitenberechnung durch
          CalculateAndApplyNewSize();
        }
      }
      catch (OperationCanceledException)
      {
        // Dies ist der erwartete Pfad bei Abbruch (z.B. Benutzer wählt neuen Film).
        // Das UserControl bleibt im Ladezustand oder wird geleert (je nach Logik in DisposeCurrentPicture).
      }
      catch (Exception ex)
      {
        // Fehlerbehandlung (z.B. beschädigtes Bild)
        Trace.WriteLine($"Fehler beim Laden des Bildes aus Bytes: {ex.Message}");
        DisposeCurrentPicture();
        // Optional: Hier Platzhalter für Fehlerbild anzeigen
      }
    }

    public void CalculateAndApplyNewSize()
    {
      // Wenn kein Bild zugewiesen oder Empty ist (z.B. nach DisposeCurrentPicture ohne neues Bild)
      if (this.pictureBoxImage.Image == null
      || this.pictureBoxImage.Image == _emptyPlaceholder)
      {
        // Setze eine Standardbreite, wenn kein Bild vorhanden ist (z.B. für Empty-State)
        this.Width = 210;
        return;
      }

      // --- Logik für geladene Bilder (wie zuvor) ---

      // Die feste Höhe des UserControls (z.B. 240px)
      int fixedHeight = this.Height;

      // Berechne den Skalierungsfaktor
      double scaleFactor = (double)fixedHeight / this.pictureBoxImage.Image.Height;

      // Berechne die neue Breite
      int newWidth = (int)(this.pictureBoxImage.Image.Width * scaleFactor);

      // Zuweisung der Breite
      this.Width = newWidth + 4; // 4px Puffer für Ränder (oder wie nötig)
    }

    public void DisposeCurrentPicture()
    {
      if (pictureBoxImage.Image != null
      && pictureBoxImage.Image != _emptyPlaceholder
      && pictureBoxImage.Image != _loadingPlaceholder)
      {
        try { pictureBoxImage.Image.Dispose(); } catch { }
      }

      pictureBoxImage.Image = null;
    }

    private void viewToolStripMenuItem_Click(object sender, EventArgs e)
    {
      if (HasImageData)
      {
        string tempfile = Utils.CreateTempFileFromData(ImageItem!.Data);
        Utils.OpenFileWithDefaultApp(tempfile);
      }
    }

    private async void newToolStripMenuItem_Click(object sender, EventArgs e)
    {
      OpenFileDialog dlg = new OpenFileDialog();
      // Setze den Filter für die Bildtypen
      dlg.Filter = "Image Files|*.jpg;*.jpeg;*.png|All Files (*.*)|*.*";
      dlg.Title = $"Neues Bild auswählen";

      // Nur eine Datei kann ausgewählt werden
      dlg.Multiselect = false;

      // Prüfe, ob der Benutzer 'OK' geklickt hat
      if (dlg.ShowDialog() == DialogResult.OK)
      {
        try
        {
          byte[] newImageBytes = await Task.Run(() => File.ReadAllBytes(dlg.FileName));
          // Delegiere die Speicherung an die Hauptform.
          PictureUpdateRequested?.Invoke(this, new PictureUpdateEventArgs(ActorId, newImageBytes));

          // 3. UI sofort aktualisieren (Laden der Bytes ins UserControl)
          await LoadPictureAsync(newImageBytes, CancellationToken.None);
        }
        catch (Exception ex)
        {
          MessageBox.Show($"Fehler beim Laden oder Speichern des Bildes: {Utils.GetExcMsg(ex)}", "Fehler",
              MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
      }
    }

    private void deleteToolStripMenuItem_Click(object sender, EventArgs e)
    {
      if (ActorId == null && MovieId == null)
      {
        // Nichts zu löschen
        return;
      }

      if (MessageBox.Show("Soll das Bild entfernt werden?", "Frage", MessageBoxButtons.YesNoCancel,
          MessageBoxIcon.Question) == DialogResult.Yes)
      {
        PictureDeleteRequested?.Invoke(this, new PictureEventArgs(ActorId, MovieId));
      }
    }

    private void pictureBoxImage_MouseDoubleClick(object sender, MouseEventArgs e)
    {
      if (HasImageData)
      {
        string tempfile = Utils.CreateTempFileFromData(ImageItem!.Data);
        Utils.OpenFileWithDefaultApp(tempfile);
      }
    }

    private void groupBoxPicture_Resize(object sender, EventArgs e)
    {
      AutoScaleGroupBoxTitle();
    }

    private void labelClose_Click(object sender, EventArgs e)
    {
      PictureCloseRequested?.Invoke(this, new PictureEventArgs(ActorId, MovieId));
    }

    private void filterToolStripMenuItem_Click(object sender, EventArgs e)
    {
      PictureFilterRequested?.Invoke(this, new PictureEventArgs(ActorId, MovieId));
    }
  }

  public class PictureEventArgs : EventArgs
  {
    public int? ActorId { get; }
    public int? MovieId { get; }

    public PictureEventArgs(int? actorId, int? movieId)
    {
      ActorId = actorId;
      MovieId = movieId;
    }
  }

  public class PictureUpdateEventArgs : EventArgs
  {
    public int? ActorId { get; }
    public byte[] NewImageData { get; }

    public PictureUpdateEventArgs(int? actorId, byte[] newImageData)
    {
      ActorId = actorId;
      NewImageData = newImageData;
    }
  }

  // Delegat für das Event
  public delegate void PictureUpdateEventHandler(object sender, PictureUpdateEventArgs e);
  public delegate void PictureEventHandler(object sender, PictureEventArgs e);
}
