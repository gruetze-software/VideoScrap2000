using MovieScrap2000.TMDbScraper;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.Window;

namespace MovieScrap2000
{
	public partial class FormScrap : Form
	{
		private bool isSearching = false;
		private TMDbScraperManager scraperManager = new TMDbScraperManager();
		public Movie NFO { get; set; }
		public TMDbMovie? SelectedMovie { get; set; }

    public List<MoviePictureUC> ActorListUc = new List<MoviePictureUC>();
    public List<MoviePictureUC> MovieListUc = new List<MoviePictureUC>();

    public FormScrap(Movie nfo)
		{
			InitializeComponent();
			NFO = nfo;
		}

		private void FormScrap_Load(object sender, EventArgs e)
		{
      textBoxSuchbegriff.Text = NFO.Title;
			textBoxSucheID.Text = NFO.ImdbIdFromUnique;
			linkLabelHomepage.LinkClicked += LinkLabelHomepage_LinkClicked;
		}

		private async void buttonSucheStartStop_Click(object sender, EventArgs e)
		{
			if (isSearching)
			{
				// TODO: Cancel search

				buttonSucheStartStop.Text = "Start";
			}
			else
			{
				isSearching = true;
				buttonSucheStartStop.Text = "Stop";
				listViewSearchResult.Items.Clear();
				if ( !string.IsNullOrWhiteSpace(textBoxSucheID.Text))
				{
					var results = await scraperManager.FindMovieByImdbIdAsync(textBoxSucheID.Text);
					foreach (var movie in results)
					{
						var item = new ListViewItem(new string[]
						{
							movie.Title ?? "N/A",
							movie.ReleaseDate?.ToString("yyyy-MM-dd") ?? "N/A",
						});
						item.Tag = movie;
						listViewSearchResult.Items.Add(item);
					}
				}
				
				isSearching = false;
				buttonSucheStartStop.Text = "Start";
			}
		}

		private async void listViewSearchResult_SelectedIndexChanged(object sender, EventArgs e)
		{
			SelectedMovie = null;

			if (listViewSearchResult.SelectedItems.Count == 1)
			{
				SearchMovie? smovie = listViewSearchResult.SelectedItems[0].Tag as SearchMovie;

				if (smovie != null)
				{
					// 1. ASYNCHRON AUF DAS ERGEBNIS WARTEN
					// Die UI wird freigegeben, während die HTTP-Anfrage läuft.
					// Der Code pausiert hier, bis das Ergebnis da ist.
					var movieDetails = await scraperManager.GetMovieFullDetailsAsync(smovie.Id);

					// 2. ERGEBNIS ZUWEISEN
					SelectedMovie = movieDetails;
				}
			}

			// 3. UI-Anpassung, NACHDEM SelectedMovie zugewiesen wurde
			AdaptDetails();
		}

		private async void AdaptDetails()
		{
			if (SelectedMovie != null)
			{
				var posterarr = await scraperManager.GetImageByteArrayAsync(SelectedMovie.PosterPath, ImageSize.Small, ImageType.Poster);
				if ( posterarr != null )
					pictureBoxPoster.Image = Utils.ConvertByteArrayToImage(posterarr);

				textBoxTitle.Text = SelectedMovie.Title;
				textBoxTitleOriginal.Text = SelectedMovie.OriginalTitle;
				textBoxTagLine.Text = SelectedMovie.Tagline;
				dateTimePickerRelease.Value = SelectedMovie.ReleaseDate ?? DateTime.Now;
				textBoxRegie.Text = SelectedMovie.DirectorsString;
				textBoxID.Text = SelectedMovie.Id.ToString();
				textBoxIMDbId.Text = SelectedMovie.ImdbId ?? "N/A";
				textBoxBudget.Text = SelectedMovie.Budget.ToString("N0") + "$";
				textBoxLaufzeit.Text = SelectedMovie.Runtime.HasValue ? $"{SelectedMovie.Runtime.Value}" : "N/A";
				textBoxRating.Text = SelectedMovie.VoteAverage.ToString();
				textBoxInhalt.Text = SelectedMovie.Overview;
				linkLabelHomepage.Text = SelectedMovie.Homepage ?? "";
				if (linkLabelHomepage.Text.Length > 0)
				{
					linkLabelHomepage.Links.Clear();
					linkLabelHomepage.Links.Add(0, linkLabelHomepage.Text.Length, linkLabelHomepage.Text);
				}

				textBoxCompanies.Text = SelectedMovie.ProductionCompaniesString;
        textBoxGenre.Text = SelectedMovie.GenresString;
				textBoxCountries.Text = SelectedMovie.ProductionCountriesString;

        ClearFlowLayoutPanels();
        // Actors
        ActorListUc = new List<MoviePictureUC>();
				foreach (var actor in SelectedMovie.Credits.Cast)
				{
					if (!string.IsNullOrEmpty(actor.ProfilePath))
					{ 
						MoviePictureUC act = new MoviePictureUC()
						{
							PictureTyp = actor.Name,
							ToolTipText = actor.Character,
							IsWidthAutoAdjusted = false,
							Width = 126
						};
						act.SetPlaceHolderEmpty();
						act.CloseButton = true;
						act.PictureCloseRequested += (sender, e) =>
            {
							flowLayoutPanelActors.Controls.Remove(act);
							ActorListUc.Remove(act);
							act.DisposeCurrentPicture();
						};
            act.Tag = actor;
						act.MovieId = NFO.TmdbIdFromUnique;
						ActorListUc.Add(act);
						flowLayoutPanelActors.Controls.Add(act);
						actor.PictureData = await scraperManager.GetImageByteArrayAsync(actor.ProfilePath, ImageSize.Medium, ImageType.Profile);
						await act.LoadPictureAsync(actor.PictureData, CancellationToken.None);
					}
        }

        // Pictures
        MovieListUc = new List<MoviePictureUC>();

				// Poster
        foreach (var moviepic in SelectedMovie.Images.Posters)
        {
          if (!string.IsNullOrEmpty(moviepic.FilePath))
          {
            MoviePictureUC act = new MoviePictureUC()
            {
              PictureTyp = "Poster",
              IsWidthAutoAdjusted = true
            };
            act.SetPlaceHolderEmpty();
            act.CloseButton = true;
            act.PictureCloseRequested += (sender, e) =>
            {
              flowLayoutPanelPicsPoster.Controls.Remove(act);
              MovieListUc.Remove(act);
              act.DisposeCurrentPicture();
            };

            act.MovieId = NFO.TmdbIdFromUnique;
            MovieListUc.Add(act);
            flowLayoutPanelPicsPoster.Controls.Add(act);
            var imgbytes = await scraperManager.GetImageByteArrayAsync(moviepic.FilePath, ImageSize.Medium, ImageType.Poster);
            await act.LoadPictureAsync(imgbytes, CancellationToken.None);
          }
        }

        // Backdrops
        foreach (var moviepic in SelectedMovie.Images.Backdrops)
        {
          if (!string.IsNullOrEmpty(moviepic.FilePath))
          {
            MoviePictureUC act = new MoviePictureUC()
            {
              PictureTyp = "Backdrop",
              IsWidthAutoAdjusted = true
            };
            act.SetPlaceHolderEmpty();
            act.CloseButton = true;
            act.PictureCloseRequested += (sender, e) =>
            {
              flowLayoutPanelPicsBackdrops.Controls.Remove(act);
              MovieListUc.Remove(act);
              act.DisposeCurrentPicture();
            };

            act.MovieId = NFO.TmdbIdFromUnique;
            MovieListUc.Add(act);
            flowLayoutPanelPicsBackdrops.Controls.Add(act);
            var imgbytes = await scraperManager.GetImageByteArrayAsync(moviepic.FilePath, ImageSize.Medium, ImageType.Poster);
            await act.LoadPictureAsync(imgbytes, CancellationToken.None);
          }
        }

        // Backdrops
        foreach (var moviepic in SelectedMovie.Images.Logos)
        {
          if (!string.IsNullOrEmpty(moviepic.FilePath))
          {
            MoviePictureUC act = new MoviePictureUC()
            {
              PictureTyp = "Logo",
              IsWidthAutoAdjusted = true
            };
            act.SetPlaceHolderEmpty();
            act.CloseButton = true;
            act.PictureCloseRequested += (sender, e) =>
            {
              flowLayoutPanelPicsLogos.Controls.Remove(act);
              MovieListUc.Remove(act);
              act.DisposeCurrentPicture();
            };

            act.MovieId = NFO.TmdbIdFromUnique;
            MovieListUc.Add(act);
            flowLayoutPanelPicsLogos.Controls.Add(act);
            var imgbytes = await scraperManager.GetImageByteArrayAsync(moviepic.FilePath, ImageSize.Medium, ImageType.Poster);
            await act.LoadPictureAsync(imgbytes, CancellationToken.None);
          }
        }
      }
			else
			{
        ClearFlowLayoutPanels();
        if (pictureBoxPoster.Image != null)
					pictureBoxPoster.Dispose();
				pictureBoxPoster = null;
				textBoxTitle.Text = string.Empty;
				textBoxTitleOriginal.Text = string.Empty;
				textBoxTagLine.Text = string.Empty;
				dateTimePickerRelease.Value = DateTime.Now;
				textBoxRegie.Text = string.Empty;
        textBoxID.Text = string.Empty;
				textBoxIMDbId.Text = string.Empty;
				textBoxLaufzeit.Text = string.Empty;
				textBoxBudget.Text = string.Empty;
				linkLabelHomepage.Links.Clear();
				linkLabelHomepage.Text = string.Empty;
				textBoxRating.Text = string.Empty;
				textBoxCompanies.Text = string.Empty;
				textBoxGenre.Text = string.Empty;
				textBoxCountries.Text = string.Empty;
        textBoxInhalt.Text = string.Empty;
			}
		}
    private void ClearFlowLayoutPanels()
		{
      foreach (Control control in flowLayoutPanelActors.Controls)
      {
        if (control is MoviePictureUC actorUC)
        {
          actorUC.PictureCloseRequested -= null;
          actorUC.DisposeCurrentPicture();
        }
      }
      flowLayoutPanelActors.Controls.Clear();

      foreach (Control control in flowLayoutPanelPicsPoster.Controls)
      {
        if (control is MoviePictureUC pic)
        {
          pic.PictureCloseRequested -= null;
          pic.DisposeCurrentPicture();
        }
      }
      flowLayoutPanelPicsPoster.Controls.Clear();

      foreach (Control control in flowLayoutPanelPicsBackdrops.Controls)
      {
        if (control is MoviePictureUC pic)
        {
          pic.PictureCloseRequested -= null;
          pic.DisposeCurrentPicture();
        }
      }
      flowLayoutPanelPicsBackdrops.Controls.Clear();

      foreach (Control control in flowLayoutPanelPicsLogos.Controls)
      {
        if (control is MoviePictureUC pic)
        {
          pic.PictureCloseRequested -= null;
          pic.DisposeCurrentPicture();
        }
      }
      flowLayoutPanelPicsLogos.Controls.Clear();
    }

    private void LinkLabelHomepage_LinkClicked(object? sender, LinkLabelLinkClickedEventArgs e)
		{
			// Der Sender ist das LinkLabel-Steuerelement, das geklickt wurde.
			if (sender is LinkLabel linkLabel)
			{
				// 1. URL abrufen
				// Die URL wird üblicherweise in der Text- oder Tag-Eigenschaft des LinkLabels gespeichert,
				// oder sie wird direkt aus dem LinkLabel.Text-Inhalt verwendet. 
				// Wir nehmen an, die vollständige URL ist im LinkLabel.Text gespeichert.
				string? url = linkLabel.Text;

				if (!string.IsNullOrEmpty(url))
				{
					// 2. Browser öffnen (Plattformübergreifende Methode in .NET Core/.NET 8)
					try
					{
						// StartInfo verwenden, um den Browser plattformunabhängig zu öffnen (empfohlen).
						Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
					}
					catch (Exception ex)
					{
						// Fehlerbehandlung, falls der Browser nicht geöffnet werden kann (z.B. ungültige URL oder Berechtigungsprobleme)
						MessageBox.Show($"Die Homepage konnte nicht geöffnet werden. Überprüfen Sie die URL. Fehler: {Utils.GetExcMsg(ex)}", "Fehler beim Öffnen", MessageBoxButtons.OK, MessageBoxIcon.Error);
						// Optional: Loggen Sie den Fehler
					}
				}
			}
		}
	}
}
