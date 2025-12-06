namespace MovieScrap2000
{
  partial class FormMain
  {
    /// <summary>
    ///  Required designer variable.
    /// </summary>
    private System.ComponentModel.IContainer components = null;

    /// <summary>
    ///  Clean up any resources being used.
    /// </summary>
    /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
    protected override void Dispose(bool disposing)
    {
      if (disposing && (components != null))
      {
        components.Dispose();
      }
      base.Dispose(disposing);
    }

        #region Windows Form Designer generated code

        /// <summary>
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FormMain));
            splitContainerMain = new SplitContainer();
            tableLayoutPanelMainMovieList = new TableLayoutPanel();
            fluentListViewMovies = new Fluent.Lists.AdvancedListView();
            contextMenuStripMovieList = new ContextMenuStrip(components);
            toolStripMenuItemMovieListAusNFONeuEinlesen = new ToolStripMenuItem();
            toolStripMenuItemMovieListScrap = new ToolStripMenuItem();
            flowLayoutPanelMainMovieList = new FlowLayoutPanel();
            label13 = new Label();
            textBoxFilter = new TextBox();
            label14 = new Label();
            trackBarFilterOnOff = new TrackBar();
            label15 = new Label();
            splitContainerMovie = new SplitContainer();
            tableLayoutPanelTitel = new TableLayoutPanel();
            tableLayoutPanelTitelDetail = new TableLayoutPanel();
            textBoxMovieTitle = new TextBox();
            textBoxMovieYear = new TextBox();
            textBoxMovieTag = new TextBox();
            tableLayoutPanelMovie = new TableLayoutPanel();
            tableLayoutPanelActorsAndImages = new TableLayoutPanel();
            flowLayoutPanelMoviePictures = new FlowLayoutPanel();
            flowLayoutPanelActors = new FlowLayoutPanel();
            tableLayoutPanelDetails = new TableLayoutPanel();
            label10 = new Label();
            textBoxMovieCountry = new TextBox();
            label9 = new Label();
            textBoxMovieInhalt = new TextBox();
            label4 = new Label();
            textBoxMoviePlot = new TextBox();
            label1 = new Label();
            label2 = new Label();
            textBoxMovieTitleOri = new TextBox();
            label5 = new Label();
            label3 = new Label();
            textBoxMovieDirector = new TextBox();
            label6 = new Label();
            textBoxMovieRating = new TextBox();
            label7 = new Label();
            textBoxMovieGenre = new TextBox();
            label8 = new Label();
            textBoxMovieStudio = new TextBox();
            flowLayoutPanelLzId = new FlowLayoutPanel();
            textBoxMovieLaufzeit = new TextBox();
            label11 = new Label();
            textBoxMovieIMDb = new TextBox();
            flowLayoutPanelDates = new FlowLayoutPanel();
            dateTimePickerMovieRelease = new DateTimePicker();
            label12 = new Label();
            dateTimePickerMovieAdded = new DateTimePicker();
            statusStripMain = new StatusStrip();
            toolStripProgressBarMain = new ToolStripProgressBar();
            toolStripStatusLabelMain = new ToolStripStatusLabel();
            toolStripMain = new ToolStrip();
            toolStripButton1 = new ToolStripButton();
            toolStripButton2 = new ToolStripButton();
            toolStripButton3 = new ToolStripButton();
            ((System.ComponentModel.ISupportInitialize)splitContainerMain).BeginInit();
            splitContainerMain.Panel1.SuspendLayout();
            splitContainerMain.Panel2.SuspendLayout();
            splitContainerMain.SuspendLayout();
            tableLayoutPanelMainMovieList.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)fluentListViewMovies).BeginInit();
            contextMenuStripMovieList.SuspendLayout();
            flowLayoutPanelMainMovieList.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)trackBarFilterOnOff).BeginInit();
            ((System.ComponentModel.ISupportInitialize)splitContainerMovie).BeginInit();
            splitContainerMovie.Panel1.SuspendLayout();
            splitContainerMovie.Panel2.SuspendLayout();
            splitContainerMovie.SuspendLayout();
            tableLayoutPanelTitel.SuspendLayout();
            tableLayoutPanelTitelDetail.SuspendLayout();
            tableLayoutPanelMovie.SuspendLayout();
            tableLayoutPanelActorsAndImages.SuspendLayout();
            tableLayoutPanelDetails.SuspendLayout();
            flowLayoutPanelLzId.SuspendLayout();
            flowLayoutPanelDates.SuspendLayout();
            statusStripMain.SuspendLayout();
            toolStripMain.SuspendLayout();
            SuspendLayout();
            // 
            // splitContainerMain
            // 
            splitContainerMain.Dock = DockStyle.Fill;
            splitContainerMain.Location = new Point(0, 39);
            splitContainerMain.Name = "splitContainerMain";
            // 
            // splitContainerMain.Panel1
            // 
            splitContainerMain.Panel1.Controls.Add(tableLayoutPanelMainMovieList);
            // 
            // splitContainerMain.Panel2
            // 
            splitContainerMain.Panel2.Controls.Add(splitContainerMovie);
            splitContainerMain.Size = new Size(1008, 600);
            splitContainerMain.SplitterDistance = 349;
            splitContainerMain.TabIndex = 0;
            // 
            // tableLayoutPanelMainMovieList
            // 
            tableLayoutPanelMainMovieList.ColumnCount = 1;
            tableLayoutPanelMainMovieList.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tableLayoutPanelMainMovieList.Controls.Add(fluentListViewMovies, 0, 1);
            tableLayoutPanelMainMovieList.Controls.Add(flowLayoutPanelMainMovieList, 0, 0);
            tableLayoutPanelMainMovieList.Dock = DockStyle.Fill;
            tableLayoutPanelMainMovieList.Location = new Point(0, 0);
            tableLayoutPanelMainMovieList.Name = "tableLayoutPanelMainMovieList";
            tableLayoutPanelMainMovieList.RowCount = 2;
            tableLayoutPanelMainMovieList.RowStyles.Add(new RowStyle(SizeType.Absolute, 40F));
            tableLayoutPanelMainMovieList.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            tableLayoutPanelMainMovieList.Size = new Size(349, 600);
            tableLayoutPanelMainMovieList.TabIndex = 2;
            // 
            // fluentListViewMovies
            // 
            fluentListViewMovies.CellEditUseWholeCell = false;
            fluentListViewMovies.ContextMenuStrip = contextMenuStripMovieList;
            fluentListViewMovies.Dock = DockStyle.Fill;
            fluentListViewMovies.FullRowSelect = true;
            fluentListViewMovies.GridLines = true;
            fluentListViewMovies.Location = new Point(3, 43);
            fluentListViewMovies.Name = "fluentListViewMovies";
            fluentListViewMovies.ShowGroups = false;
            fluentListViewMovies.ShowImagesOnSubItems = true;
            fluentListViewMovies.ShowItemCountOnGroups = true;
            fluentListViewMovies.Size = new Size(343, 554);
            fluentListViewMovies.TabIndex = 1;
            fluentListViewMovies.View = View.Details;
            fluentListViewMovies.SelectionChanged += fluentListViewMovies_SelectionChanged;
            // 
            // contextMenuStripMovieList
            // 
            contextMenuStripMovieList.Items.AddRange(new ToolStripItem[] { toolStripMenuItemMovieListAusNFONeuEinlesen, toolStripMenuItemMovieListScrap });
            contextMenuStripMovieList.Name = "contextMenuStripMovieList";
            contextMenuStripMovieList.Size = new Size(191, 48);
            // 
            // toolStripMenuItemMovieListAusNFONeuEinlesen
            // 
            toolStripMenuItemMovieListAusNFONeuEinlesen.Name = "toolStripMenuItemMovieListAusNFONeuEinlesen";
            toolStripMenuItemMovieListAusNFONeuEinlesen.Size = new Size(190, 22);
            toolStripMenuItemMovieListAusNFONeuEinlesen.Text = "Aus NFO neu einlesen";
            toolStripMenuItemMovieListAusNFONeuEinlesen.Click += toolStripMenuItemMovieListAusNFONeuEinlesen_Click;
            // 
            // toolStripMenuItemMovieListScrap
            // 
            toolStripMenuItemMovieListScrap.Name = "toolStripMenuItemMovieListScrap";
            toolStripMenuItemMovieListScrap.Size = new Size(190, 22);
            toolStripMenuItemMovieListScrap.Text = "Scrap...";
            toolStripMenuItemMovieListScrap.Click += toolStripMenuItemMovieListScrap_Click;
            // 
            // flowLayoutPanelMainMovieList
            // 
            flowLayoutPanelMainMovieList.Controls.Add(label13);
            flowLayoutPanelMainMovieList.Controls.Add(textBoxFilter);
            flowLayoutPanelMainMovieList.Controls.Add(label14);
            flowLayoutPanelMainMovieList.Controls.Add(trackBarFilterOnOff);
            flowLayoutPanelMainMovieList.Controls.Add(label15);
            flowLayoutPanelMainMovieList.Dock = DockStyle.Fill;
            flowLayoutPanelMainMovieList.Location = new Point(3, 3);
            flowLayoutPanelMainMovieList.Name = "flowLayoutPanelMainMovieList";
            flowLayoutPanelMainMovieList.Size = new Size(343, 34);
            flowLayoutPanelMainMovieList.TabIndex = 2;
            // 
            // label13
            // 
            label13.Location = new Point(3, 0);
            label13.Name = "label13";
            label13.Size = new Size(43, 23);
            label13.TabIndex = 0;
            label13.Text = "Filter:";
            label13.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // textBoxFilter
            // 
            textBoxFilter.Location = new Point(52, 3);
            textBoxFilter.Name = "textBoxFilter";
            textBoxFilter.Size = new Size(127, 23);
            textBoxFilter.TabIndex = 1;
            // 
            // label14
            // 
            label14.Location = new Point(185, 0);
            label14.Name = "label14";
            label14.Size = new Size(43, 23);
            label14.TabIndex = 3;
            label14.Text = "Aus";
            label14.TextAlign = ContentAlignment.MiddleRight;
            // 
            // trackBarFilterOnOff
            // 
            trackBarFilterOnOff.Location = new Point(234, 3);
            trackBarFilterOnOff.Maximum = 1;
            trackBarFilterOnOff.Name = "trackBarFilterOnOff";
            trackBarFilterOnOff.Size = new Size(43, 45);
            trackBarFilterOnOff.TabIndex = 2;
            trackBarFilterOnOff.Scroll += trackBarFilterOnOff_Scroll;
            // 
            // label15
            // 
            label15.Location = new Point(283, 0);
            label15.Name = "label15";
            label15.Size = new Size(43, 23);
            label15.TabIndex = 4;
            label15.Text = "An";
            label15.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // splitContainerMovie
            // 
            splitContainerMovie.Dock = DockStyle.Fill;
            splitContainerMovie.FixedPanel = FixedPanel.Panel1;
            splitContainerMovie.IsSplitterFixed = true;
            splitContainerMovie.Location = new Point(0, 0);
            splitContainerMovie.Name = "splitContainerMovie";
            splitContainerMovie.Orientation = Orientation.Horizontal;
            // 
            // splitContainerMovie.Panel1
            // 
            splitContainerMovie.Panel1.Controls.Add(tableLayoutPanelTitel);
            // 
            // splitContainerMovie.Panel2
            // 
            splitContainerMovie.Panel2.Controls.Add(tableLayoutPanelMovie);
            splitContainerMovie.Size = new Size(655, 600);
            splitContainerMovie.SplitterDistance = 100;
            splitContainerMovie.TabIndex = 0;
            // 
            // tableLayoutPanelTitel
            // 
            tableLayoutPanelTitel.ColumnCount = 3;
            tableLayoutPanelTitel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 15F));
            tableLayoutPanelTitel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40F));
            tableLayoutPanelTitel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 45F));
            tableLayoutPanelTitel.Controls.Add(tableLayoutPanelTitelDetail, 1, 0);
            tableLayoutPanelTitel.Dock = DockStyle.Fill;
            tableLayoutPanelTitel.Location = new Point(0, 0);
            tableLayoutPanelTitel.Margin = new Padding(3, 2, 3, 2);
            tableLayoutPanelTitel.Name = "tableLayoutPanelTitel";
            tableLayoutPanelTitel.RowCount = 1;
            tableLayoutPanelTitel.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            tableLayoutPanelTitel.Size = new Size(655, 100);
            tableLayoutPanelTitel.TabIndex = 0;
            // 
            // tableLayoutPanelTitelDetail
            // 
            tableLayoutPanelTitelDetail.ColumnCount = 1;
            tableLayoutPanelTitelDetail.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tableLayoutPanelTitelDetail.Controls.Add(textBoxMovieTitle, 0, 0);
            tableLayoutPanelTitelDetail.Controls.Add(textBoxMovieYear, 0, 1);
            tableLayoutPanelTitelDetail.Controls.Add(textBoxMovieTag, 0, 2);
            tableLayoutPanelTitelDetail.Dock = DockStyle.Fill;
            tableLayoutPanelTitelDetail.Location = new Point(101, 2);
            tableLayoutPanelTitelDetail.Margin = new Padding(3, 2, 3, 2);
            tableLayoutPanelTitelDetail.Name = "tableLayoutPanelTitelDetail";
            tableLayoutPanelTitelDetail.RowCount = 3;
            tableLayoutPanelTitelDetail.RowStyles.Add(new RowStyle());
            tableLayoutPanelTitelDetail.RowStyles.Add(new RowStyle());
            tableLayoutPanelTitelDetail.RowStyles.Add(new RowStyle());
            tableLayoutPanelTitelDetail.Size = new Size(256, 96);
            tableLayoutPanelTitelDetail.TabIndex = 0;
            // 
            // textBoxMovieTitle
            // 
            textBoxMovieTitle.Dock = DockStyle.Fill;
            textBoxMovieTitle.Font = new Font("Segoe UI", 11.25F, FontStyle.Bold, GraphicsUnit.Point, 0);
            textBoxMovieTitle.Location = new Point(3, 3);
            textBoxMovieTitle.Name = "textBoxMovieTitle";
            textBoxMovieTitle.Size = new Size(250, 27);
            textBoxMovieTitle.TabIndex = 4;
            textBoxMovieTitle.TextAlign = HorizontalAlignment.Center;
            textBoxMovieTitle.WordWrap = false;
            // 
            // textBoxMovieYear
            // 
            textBoxMovieYear.Dock = DockStyle.Fill;
            textBoxMovieYear.Location = new Point(3, 36);
            textBoxMovieYear.Name = "textBoxMovieYear";
            textBoxMovieYear.Size = new Size(250, 23);
            textBoxMovieYear.TabIndex = 5;
            textBoxMovieYear.TextAlign = HorizontalAlignment.Center;
            // 
            // textBoxMovieTag
            // 
            textBoxMovieTag.Dock = DockStyle.Fill;
            textBoxMovieTag.Location = new Point(3, 64);
            textBoxMovieTag.Margin = new Padding(3, 2, 3, 2);
            textBoxMovieTag.Name = "textBoxMovieTag";
            textBoxMovieTag.Size = new Size(250, 23);
            textBoxMovieTag.TabIndex = 6;
            textBoxMovieTag.TextAlign = HorizontalAlignment.Center;
            // 
            // tableLayoutPanelMovie
            // 
            tableLayoutPanelMovie.ColumnCount = 1;
            tableLayoutPanelMovie.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tableLayoutPanelMovie.Controls.Add(tableLayoutPanelActorsAndImages, 0, 1);
            tableLayoutPanelMovie.Controls.Add(tableLayoutPanelDetails, 0, 0);
            tableLayoutPanelMovie.Dock = DockStyle.Fill;
            tableLayoutPanelMovie.Location = new Point(0, 0);
            tableLayoutPanelMovie.Name = "tableLayoutPanelMovie";
            tableLayoutPanelMovie.RowCount = 2;
            tableLayoutPanelMovie.RowStyles.Add(new RowStyle(SizeType.Absolute, 194F));
            tableLayoutPanelMovie.RowStyles.Add(new RowStyle(SizeType.Absolute, 26F));
            tableLayoutPanelMovie.Size = new Size(655, 496);
            tableLayoutPanelMovie.TabIndex = 4;
            // 
            // tableLayoutPanelActorsAndImages
            // 
            tableLayoutPanelActorsAndImages.ColumnCount = 1;
            tableLayoutPanelActorsAndImages.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tableLayoutPanelActorsAndImages.Controls.Add(flowLayoutPanelMoviePictures, 0, 1);
            tableLayoutPanelActorsAndImages.Controls.Add(flowLayoutPanelActors, 0, 0);
            tableLayoutPanelActorsAndImages.Dock = DockStyle.Fill;
            tableLayoutPanelActorsAndImages.Location = new Point(3, 197);
            tableLayoutPanelActorsAndImages.Name = "tableLayoutPanelActorsAndImages";
            tableLayoutPanelActorsAndImages.RowCount = 2;
            tableLayoutPanelActorsAndImages.RowStyles.Add(new RowStyle(SizeType.Percent, 40F));
            tableLayoutPanelActorsAndImages.RowStyles.Add(new RowStyle(SizeType.Percent, 60F));
            tableLayoutPanelActorsAndImages.Size = new Size(649, 296);
            tableLayoutPanelActorsAndImages.TabIndex = 0;
            // 
            // flowLayoutPanelMoviePictures
            // 
            flowLayoutPanelMoviePictures.AutoScroll = true;
            flowLayoutPanelMoviePictures.BackColor = Color.White;
            flowLayoutPanelMoviePictures.BorderStyle = BorderStyle.Fixed3D;
            flowLayoutPanelMoviePictures.Dock = DockStyle.Fill;
            flowLayoutPanelMoviePictures.Location = new Point(3, 120);
            flowLayoutPanelMoviePictures.Margin = new Padding(3, 2, 3, 2);
            flowLayoutPanelMoviePictures.Name = "flowLayoutPanelMoviePictures";
            flowLayoutPanelMoviePictures.Size = new Size(643, 174);
            flowLayoutPanelMoviePictures.TabIndex = 1;
            // 
            // flowLayoutPanelActors
            // 
            flowLayoutPanelActors.AutoScroll = true;
            flowLayoutPanelActors.AutoSize = true;
            flowLayoutPanelActors.BackColor = Color.White;
            flowLayoutPanelActors.BorderStyle = BorderStyle.Fixed3D;
            flowLayoutPanelActors.Dock = DockStyle.Fill;
            flowLayoutPanelActors.Location = new Point(3, 3);
            flowLayoutPanelActors.Name = "flowLayoutPanelActors";
            flowLayoutPanelActors.Size = new Size(643, 112);
            flowLayoutPanelActors.TabIndex = 2;
            // 
            // tableLayoutPanelDetails
            // 
            tableLayoutPanelDetails.ColumnCount = 4;
            tableLayoutPanelDetails.ColumnStyles.Add(new ColumnStyle());
            tableLayoutPanelDetails.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            tableLayoutPanelDetails.ColumnStyles.Add(new ColumnStyle());
            tableLayoutPanelDetails.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            tableLayoutPanelDetails.Controls.Add(label10, 2, 4);
            tableLayoutPanelDetails.Controls.Add(textBoxMovieCountry, 1, 4);
            tableLayoutPanelDetails.Controls.Add(label9, 0, 4);
            tableLayoutPanelDetails.Controls.Add(textBoxMovieInhalt, 3, 3);
            tableLayoutPanelDetails.Controls.Add(label4, 2, 3);
            tableLayoutPanelDetails.Controls.Add(textBoxMoviePlot, 1, 3);
            tableLayoutPanelDetails.Controls.Add(label1, 0, 3);
            tableLayoutPanelDetails.Controls.Add(label2, 0, 0);
            tableLayoutPanelDetails.Controls.Add(textBoxMovieTitleOri, 1, 0);
            tableLayoutPanelDetails.Controls.Add(label5, 2, 0);
            tableLayoutPanelDetails.Controls.Add(label3, 0, 1);
            tableLayoutPanelDetails.Controls.Add(textBoxMovieDirector, 1, 1);
            tableLayoutPanelDetails.Controls.Add(label6, 2, 1);
            tableLayoutPanelDetails.Controls.Add(textBoxMovieRating, 3, 1);
            tableLayoutPanelDetails.Controls.Add(label7, 0, 2);
            tableLayoutPanelDetails.Controls.Add(textBoxMovieGenre, 1, 2);
            tableLayoutPanelDetails.Controls.Add(label8, 2, 2);
            tableLayoutPanelDetails.Controls.Add(textBoxMovieStudio, 3, 2);
            tableLayoutPanelDetails.Controls.Add(flowLayoutPanelLzId, 3, 4);
            tableLayoutPanelDetails.Controls.Add(flowLayoutPanelDates, 3, 0);
            tableLayoutPanelDetails.Dock = DockStyle.Fill;
            tableLayoutPanelDetails.Location = new Point(3, 3);
            tableLayoutPanelDetails.Name = "tableLayoutPanelDetails";
            tableLayoutPanelDetails.RowCount = 6;
            tableLayoutPanelDetails.RowStyles.Add(new RowStyle(SizeType.Absolute, 25F));
            tableLayoutPanelDetails.RowStyles.Add(new RowStyle(SizeType.Absolute, 25F));
            tableLayoutPanelDetails.RowStyles.Add(new RowStyle(SizeType.Absolute, 25F));
            tableLayoutPanelDetails.RowStyles.Add(new RowStyle(SizeType.Absolute, 70F));
            tableLayoutPanelDetails.RowStyles.Add(new RowStyle(SizeType.Absolute, 25F));
            tableLayoutPanelDetails.RowStyles.Add(new RowStyle(SizeType.Absolute, 25F));
            tableLayoutPanelDetails.Size = new Size(649, 188);
            tableLayoutPanelDetails.TabIndex = 0;
            // 
            // label10
            // 
            label10.AutoSize = true;
            label10.Dock = DockStyle.Fill;
            label10.Location = new Point(328, 145);
            label10.Name = "label10";
            label10.Size = new Size(51, 25);
            label10.TabIndex = 21;
            label10.Text = "Laufzeit:";
            label10.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // textBoxMovieCountry
            // 
            textBoxMovieCountry.Dock = DockStyle.Fill;
            textBoxMovieCountry.Location = new Point(61, 148);
            textBoxMovieCountry.Name = "textBoxMovieCountry";
            textBoxMovieCountry.Size = new Size(261, 23);
            textBoxMovieCountry.TabIndex = 20;
            // 
            // label9
            // 
            label9.AutoSize = true;
            label9.Dock = DockStyle.Fill;
            label9.Location = new Point(3, 145);
            label9.Name = "label9";
            label9.Size = new Size(52, 25);
            label9.TabIndex = 19;
            label9.Text = "Land:";
            label9.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // textBoxMovieInhalt
            // 
            textBoxMovieInhalt.AcceptsReturn = true;
            textBoxMovieInhalt.Dock = DockStyle.Fill;
            textBoxMovieInhalt.Location = new Point(385, 78);
            textBoxMovieInhalt.Multiline = true;
            textBoxMovieInhalt.Name = "textBoxMovieInhalt";
            textBoxMovieInhalt.ScrollBars = ScrollBars.Vertical;
            textBoxMovieInhalt.Size = new Size(261, 64);
            textBoxMovieInhalt.TabIndex = 1;
            // 
            // label4
            // 
            label4.AutoSize = true;
            label4.Dock = DockStyle.Fill;
            label4.Location = new Point(328, 75);
            label4.Name = "label4";
            label4.Size = new Size(51, 70);
            label4.TabIndex = 18;
            label4.Text = "Inhalt:";
            label4.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // textBoxMoviePlot
            // 
            textBoxMoviePlot.AcceptsReturn = true;
            textBoxMoviePlot.Dock = DockStyle.Fill;
            textBoxMoviePlot.Location = new Point(61, 78);
            textBoxMoviePlot.Multiline = true;
            textBoxMoviePlot.Name = "textBoxMoviePlot";
            textBoxMoviePlot.ScrollBars = ScrollBars.Vertical;
            textBoxMoviePlot.Size = new Size(261, 64);
            textBoxMoviePlot.TabIndex = 17;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Dock = DockStyle.Fill;
            label1.Location = new Point(3, 75);
            label1.Name = "label1";
            label1.Size = new Size(52, 70);
            label1.TabIndex = 16;
            label1.Text = "Plot:";
            label1.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Dock = DockStyle.Fill;
            label2.Location = new Point(3, 0);
            label2.Name = "label2";
            label2.Size = new Size(52, 25);
            label2.TabIndex = 1;
            label2.Text = "Original:";
            label2.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // textBoxMovieTitleOri
            // 
            textBoxMovieTitleOri.Dock = DockStyle.Fill;
            textBoxMovieTitleOri.Location = new Point(61, 3);
            textBoxMovieTitleOri.Name = "textBoxMovieTitleOri";
            textBoxMovieTitleOri.Size = new Size(261, 23);
            textBoxMovieTitleOri.TabIndex = 6;
            // 
            // label5
            // 
            label5.AutoSize = true;
            label5.Dock = DockStyle.Fill;
            label5.Location = new Point(328, 0);
            label5.Name = "label5";
            label5.Size = new Size(51, 25);
            label5.TabIndex = 8;
            label5.Text = "Release:";
            label5.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Dock = DockStyle.Fill;
            label3.Location = new Point(3, 25);
            label3.Name = "label3";
            label3.Size = new Size(52, 25);
            label3.TabIndex = 2;
            label3.Text = "Regie:";
            label3.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // textBoxMovieDirector
            // 
            textBoxMovieDirector.Dock = DockStyle.Fill;
            textBoxMovieDirector.Location = new Point(61, 28);
            textBoxMovieDirector.Name = "textBoxMovieDirector";
            textBoxMovieDirector.Size = new Size(261, 23);
            textBoxMovieDirector.TabIndex = 7;
            // 
            // label6
            // 
            label6.AutoSize = true;
            label6.Dock = DockStyle.Fill;
            label6.Location = new Point(328, 25);
            label6.Name = "label6";
            label6.Size = new Size(51, 25);
            label6.TabIndex = 10;
            label6.Text = "Rating:";
            label6.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // textBoxMovieRating
            // 
            textBoxMovieRating.Dock = DockStyle.Fill;
            textBoxMovieRating.Location = new Point(385, 28);
            textBoxMovieRating.Name = "textBoxMovieRating";
            textBoxMovieRating.Size = new Size(261, 23);
            textBoxMovieRating.TabIndex = 11;
            // 
            // label7
            // 
            label7.AutoSize = true;
            label7.Dock = DockStyle.Fill;
            label7.Location = new Point(3, 50);
            label7.Name = "label7";
            label7.Size = new Size(52, 25);
            label7.TabIndex = 12;
            label7.Text = "Genre:";
            label7.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // textBoxMovieGenre
            // 
            textBoxMovieGenre.Dock = DockStyle.Fill;
            textBoxMovieGenre.Location = new Point(61, 53);
            textBoxMovieGenre.Name = "textBoxMovieGenre";
            textBoxMovieGenre.Size = new Size(261, 23);
            textBoxMovieGenre.TabIndex = 13;
            // 
            // label8
            // 
            label8.AutoSize = true;
            label8.Dock = DockStyle.Fill;
            label8.Location = new Point(328, 50);
            label8.Name = "label8";
            label8.Size = new Size(51, 25);
            label8.TabIndex = 14;
            label8.Text = "Studio:";
            label8.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // textBoxMovieStudio
            // 
            textBoxMovieStudio.Dock = DockStyle.Fill;
            textBoxMovieStudio.Location = new Point(385, 53);
            textBoxMovieStudio.Name = "textBoxMovieStudio";
            textBoxMovieStudio.Size = new Size(261, 23);
            textBoxMovieStudio.TabIndex = 15;
            // 
            // flowLayoutPanelLzId
            // 
            flowLayoutPanelLzId.Controls.Add(textBoxMovieLaufzeit);
            flowLayoutPanelLzId.Controls.Add(label11);
            flowLayoutPanelLzId.Controls.Add(textBoxMovieIMDb);
            flowLayoutPanelLzId.Dock = DockStyle.Fill;
            flowLayoutPanelLzId.Location = new Point(382, 145);
            flowLayoutPanelLzId.Margin = new Padding(0);
            flowLayoutPanelLzId.Name = "flowLayoutPanelLzId";
            flowLayoutPanelLzId.Size = new Size(267, 25);
            flowLayoutPanelLzId.TabIndex = 23;
            // 
            // textBoxMovieLaufzeit
            // 
            textBoxMovieLaufzeit.Location = new Point(3, 1);
            textBoxMovieLaufzeit.Margin = new Padding(3, 1, 3, 3);
            textBoxMovieLaufzeit.Name = "textBoxMovieLaufzeit";
            textBoxMovieLaufzeit.Size = new Size(52, 23);
            textBoxMovieLaufzeit.TabIndex = 22;
            // 
            // label11
            // 
            label11.AutoSize = true;
            label11.Location = new Point(61, 4);
            label11.Margin = new Padding(3, 4, 3, 0);
            label11.Name = "label11";
            label11.Size = new Size(39, 15);
            label11.TabIndex = 23;
            label11.Text = "IMDb:";
            // 
            // textBoxMovieIMDb
            // 
            textBoxMovieIMDb.Location = new Point(106, 1);
            textBoxMovieIMDb.Margin = new Padding(3, 1, 3, 3);
            textBoxMovieIMDb.Name = "textBoxMovieIMDb";
            textBoxMovieIMDb.Size = new Size(74, 23);
            textBoxMovieIMDb.TabIndex = 24;
            // 
            // flowLayoutPanelDates
            // 
            flowLayoutPanelDates.Controls.Add(dateTimePickerMovieRelease);
            flowLayoutPanelDates.Controls.Add(label12);
            flowLayoutPanelDates.Controls.Add(dateTimePickerMovieAdded);
            flowLayoutPanelDates.Dock = DockStyle.Fill;
            flowLayoutPanelDates.Location = new Point(382, 0);
            flowLayoutPanelDates.Margin = new Padding(0);
            flowLayoutPanelDates.Name = "flowLayoutPanelDates";
            flowLayoutPanelDates.Size = new Size(267, 25);
            flowLayoutPanelDates.TabIndex = 24;
            // 
            // dateTimePickerMovieRelease
            // 
            dateTimePickerMovieRelease.Format = DateTimePickerFormat.Short;
            dateTimePickerMovieRelease.Location = new Point(3, 1);
            dateTimePickerMovieRelease.Margin = new Padding(3, 1, 3, 3);
            dateTimePickerMovieRelease.Name = "dateTimePickerMovieRelease";
            dateTimePickerMovieRelease.Size = new Size(76, 23);
            dateTimePickerMovieRelease.TabIndex = 9;
            // 
            // label12
            // 
            label12.AutoSize = true;
            label12.Location = new Point(85, 4);
            label12.Margin = new Padding(3, 4, 3, 0);
            label12.Name = "label12";
            label12.Size = new Size(45, 15);
            label12.TabIndex = 10;
            label12.Text = "Added:";
            // 
            // dateTimePickerMovieAdded
            // 
            dateTimePickerMovieAdded.Format = DateTimePickerFormat.Short;
            dateTimePickerMovieAdded.Location = new Point(136, 1);
            dateTimePickerMovieAdded.Margin = new Padding(3, 1, 3, 3);
            dateTimePickerMovieAdded.Name = "dateTimePickerMovieAdded";
            dateTimePickerMovieAdded.Size = new Size(76, 23);
            dateTimePickerMovieAdded.TabIndex = 11;
            // 
            // statusStripMain
            // 
            statusStripMain.ImageScalingSize = new Size(20, 20);
            statusStripMain.Items.AddRange(new ToolStripItem[] { toolStripProgressBarMain, toolStripStatusLabelMain });
            statusStripMain.Location = new Point(0, 639);
            statusStripMain.Name = "statusStripMain";
            statusStripMain.Size = new Size(1008, 22);
            statusStripMain.TabIndex = 0;
            statusStripMain.Text = "statusStrip1";
            // 
            // toolStripProgressBarMain
            // 
            toolStripProgressBarMain.Name = "toolStripProgressBarMain";
            toolStripProgressBarMain.Size = new Size(100, 16);
            // 
            // toolStripStatusLabelMain
            // 
            toolStripStatusLabelMain.Name = "toolStripStatusLabelMain";
            toolStripStatusLabelMain.Size = new Size(40, 17);
            toolStripStatusLabelMain.Text = "Bereit.";
            // 
            // toolStripMain
            // 
            toolStripMain.ImageScalingSize = new Size(32, 32);
            toolStripMain.Items.AddRange(new ToolStripItem[] { toolStripButton1, toolStripButton2, toolStripButton3 });
            toolStripMain.Location = new Point(0, 0);
            toolStripMain.Name = "toolStripMain";
            toolStripMain.Size = new Size(1008, 39);
            toolStripMain.TabIndex = 1;
            toolStripMain.Text = "toolStrip1";
            // 
            // toolStripButton1
            // 
            toolStripButton1.DisplayStyle = ToolStripItemDisplayStyle.Image;
            toolStripButton1.Image = (Image)resources.GetObject("toolStripButton1.Image");
            toolStripButton1.ImageTransparentColor = Color.Magenta;
            toolStripButton1.Name = "toolStripButton1";
            toolStripButton1.Size = new Size(36, 36);
            toolStripButton1.Text = "toolStripButton1";
            // 
            // toolStripButton2
            // 
            toolStripButton2.DisplayStyle = ToolStripItemDisplayStyle.Image;
            toolStripButton2.Image = (Image)resources.GetObject("toolStripButton2.Image");
            toolStripButton2.ImageTransparentColor = Color.Magenta;
            toolStripButton2.Name = "toolStripButton2";
            toolStripButton2.Size = new Size(36, 36);
            toolStripButton2.Text = "toolStripButton2";
            // 
            // toolStripButton3
            // 
            toolStripButton3.DisplayStyle = ToolStripItemDisplayStyle.Image;
            toolStripButton3.Image = (Image)resources.GetObject("toolStripButton3.Image");
            toolStripButton3.ImageTransparentColor = Color.Magenta;
            toolStripButton3.Name = "toolStripButton3";
            toolStripButton3.Size = new Size(36, 36);
            toolStripButton3.Text = "toolStripButton3";
            // 
            // FormMain
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1008, 661);
            Controls.Add(splitContainerMain);
            Controls.Add(statusStripMain);
            Controls.Add(toolStripMain);
            Name = "FormMain";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "Form1";
            Load += FormMain_Load;
            Shown += FormMain_Shown;
            splitContainerMain.Panel1.ResumeLayout(false);
            splitContainerMain.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)splitContainerMain).EndInit();
            splitContainerMain.ResumeLayout(false);
            tableLayoutPanelMainMovieList.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)fluentListViewMovies).EndInit();
            contextMenuStripMovieList.ResumeLayout(false);
            flowLayoutPanelMainMovieList.ResumeLayout(false);
            flowLayoutPanelMainMovieList.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)trackBarFilterOnOff).EndInit();
            splitContainerMovie.Panel1.ResumeLayout(false);
            splitContainerMovie.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)splitContainerMovie).EndInit();
            splitContainerMovie.ResumeLayout(false);
            tableLayoutPanelTitel.ResumeLayout(false);
            tableLayoutPanelTitelDetail.ResumeLayout(false);
            tableLayoutPanelTitelDetail.PerformLayout();
            tableLayoutPanelMovie.ResumeLayout(false);
            tableLayoutPanelActorsAndImages.ResumeLayout(false);
            tableLayoutPanelActorsAndImages.PerformLayout();
            tableLayoutPanelDetails.ResumeLayout(false);
            tableLayoutPanelDetails.PerformLayout();
            flowLayoutPanelLzId.ResumeLayout(false);
            flowLayoutPanelLzId.PerformLayout();
            flowLayoutPanelDates.ResumeLayout(false);
            flowLayoutPanelDates.PerformLayout();
            statusStripMain.ResumeLayout(false);
            statusStripMain.PerformLayout();
            toolStripMain.ResumeLayout(false);
            toolStripMain.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private SplitContainer splitContainerMain;
    private StatusStrip statusStripMain;
    private ToolStrip toolStripMain;
    private ToolStripButton toolStripButton1;
    private ToolStripButton toolStripButton2;
    private ToolStripButton toolStripButton3;
    private Fluent.Lists.AdvancedListView fluentListViewMovies;
    private ToolStripProgressBar toolStripProgressBarMain;
    private SplitContainer splitContainerMovie;
    private TableLayoutPanel tableLayoutPanelDetails;
    private Label label2;
    private TextBox textBoxMovieTitle;
    private TextBox textBoxMovieYear;
    private TextBox textBoxMovieTitleOri;
    private Label label3;
    private TextBox textBoxMovieDirector;
    private Label label5;
    private DateTimePicker dateTimePickerMovieRelease;
    private Label label6;
    private TextBox textBoxMovieRating;
    private TableLayoutPanel tableLayoutPanelMovie;
    private TextBox textBoxMovieInhalt;
    private ToolStripStatusLabel toolStripStatusLabelMain;
    private TextBox textBoxMovieStudio;
    private Label label8;
    private TextBox textBoxMovieGenre;
    private Label label7;
    private TableLayoutPanel tableLayoutPanelTitel;
    private TableLayoutPanel tableLayoutPanelTitelDetail;
    private TextBox textBoxMovieTag;
    private Label label4;
    private TextBox textBoxMoviePlot;
    private Label label1;
    private FlowLayoutPanel flowLayoutPanelMoviePictures;
    private TextBox textBoxMovieCountry;
    private Label label9;
    private TextBox textBoxMovieLaufzeit;
    private Label label10;
    private FlowLayoutPanel flowLayoutPanelLzId;
    private Label label11;
    private TextBox textBoxMovieIMDb;
    private FlowLayoutPanel flowLayoutPanelDates;
    private Label label12;
    private DateTimePicker dateTimePickerMovieAdded;
    private TableLayoutPanel tableLayoutPanelActorsAndImages;
    private FlowLayoutPanel flowLayoutPanelActors;
    private ContextMenuStrip contextMenuStripMovieList;
    private ToolStripMenuItem toolStripMenuItemMovieListAusNFONeuEinlesen;
    private ToolStripMenuItem toolStripMenuItemMovieListScrap;
    private TableLayoutPanel tableLayoutPanelMainMovieList;
    private FlowLayoutPanel flowLayoutPanelMainMovieList;
    private Label label13;
    private TextBox textBoxFilter;
    private Label label14;
    private TrackBar trackBarFilterOnOff;
    private Label label15;
  }
}
