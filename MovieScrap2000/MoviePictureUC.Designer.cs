namespace MovieScrap2000
{
  partial class MoviePictureUC
  {
    /// <summary> 
    /// Required designer variable.
    /// </summary>
    private System.ComponentModel.IContainer components = null;

    /// <summary> 
    /// Clean up any resources being used.
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

        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();
            groupBoxPicture = new GroupBox();
            labelClose = new Label();
            tableLayoutPanelPicture = new TableLayoutPanel();
            pictureBoxImage = new PictureBox();
            contextMenuStripUC = new ContextMenuStrip(components);
            toolStripTextBoxPicType = new ToolStripTextBox();
            toolStripSeparator1 = new ToolStripSeparator();
            viewToolStripMenuItem = new ToolStripMenuItem();
            newToolStripMenuItem = new ToolStripMenuItem();
            deleteToolStripMenuItem = new ToolStripMenuItem();
            toolTipPicture = new ToolTip(components);
            filterToolStripMenuItem = new ToolStripMenuItem();
            groupBoxPicture.SuspendLayout();
            tableLayoutPanelPicture.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)pictureBoxImage).BeginInit();
            contextMenuStripUC.SuspendLayout();
            SuspendLayout();
            // 
            // groupBoxPicture
            // 
            groupBoxPicture.Controls.Add(labelClose);
            groupBoxPicture.Controls.Add(tableLayoutPanelPicture);
            groupBoxPicture.Dock = DockStyle.Fill;
            groupBoxPicture.Location = new Point(0, 0);
            groupBoxPicture.Name = "groupBoxPicture";
            groupBoxPicture.Size = new Size(184, 200);
            groupBoxPicture.TabIndex = 0;
            groupBoxPicture.TabStop = false;
            groupBoxPicture.Text = "PictureType";
            groupBoxPicture.Resize += groupBoxPicture_Resize;
            // 
            // labelClose
            // 
            labelClose.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            labelClose.AutoSize = true;
            labelClose.Cursor = Cursors.Hand;
            labelClose.Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point, 0);
            labelClose.ForeColor = Color.Crimson;
            labelClose.Location = new Point(168, 0);
            labelClose.Name = "labelClose";
            labelClose.Padding = new Padding(0, 0, 2, 0);
            labelClose.Size = new Size(16, 15);
            labelClose.TabIndex = 1;
            labelClose.Text = "X";
            labelClose.TextAlign = ContentAlignment.MiddleCenter;
            labelClose.Click += labelClose_Click;
            // 
            // tableLayoutPanelPicture
            // 
            tableLayoutPanelPicture.ColumnCount = 1;
            tableLayoutPanelPicture.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tableLayoutPanelPicture.Controls.Add(pictureBoxImage, 0, 0);
            tableLayoutPanelPicture.Dock = DockStyle.Fill;
            tableLayoutPanelPicture.Location = new Point(3, 19);
            tableLayoutPanelPicture.Name = "tableLayoutPanelPicture";
            tableLayoutPanelPicture.RowCount = 1;
            tableLayoutPanelPicture.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            tableLayoutPanelPicture.Size = new Size(178, 178);
            tableLayoutPanelPicture.TabIndex = 0;
            // 
            // pictureBoxImage
            // 
            pictureBoxImage.BackColor = SystemColors.ControlDarkDark;
            pictureBoxImage.ContextMenuStrip = contextMenuStripUC;
            pictureBoxImage.Dock = DockStyle.Fill;
            pictureBoxImage.InitialImage = null;
            pictureBoxImage.Location = new Point(3, 3);
            pictureBoxImage.Name = "pictureBoxImage";
            pictureBoxImage.Size = new Size(172, 172);
            pictureBoxImage.SizeMode = PictureBoxSizeMode.Zoom;
            pictureBoxImage.TabIndex = 1;
            pictureBoxImage.TabStop = false;
            pictureBoxImage.MouseDoubleClick += pictureBoxImage_MouseDoubleClick;
            // 
            // contextMenuStripUC
            // 
            contextMenuStripUC.ImageScalingSize = new Size(20, 20);
            contextMenuStripUC.Items.AddRange(new ToolStripItem[] { toolStripTextBoxPicType, toolStripSeparator1, viewToolStripMenuItem, newToolStripMenuItem, deleteToolStripMenuItem, filterToolStripMenuItem });
            contextMenuStripUC.Name = "contextMenuStripUC";
            contextMenuStripUC.Size = new Size(181, 145);
            // 
            // toolStripTextBoxPicType
            // 
            toolStripTextBoxPicType.BackColor = SystemColors.ControlLightLight;
            toolStripTextBoxPicType.Font = new Font("Segoe UI", 9F, FontStyle.Bold | FontStyle.Underline, GraphicsUnit.Point, 0);
            toolStripTextBoxPicType.Name = "toolStripTextBoxPicType";
            toolStripTextBoxPicType.ReadOnly = true;
            toolStripTextBoxPicType.Size = new Size(100, 23);
            toolStripTextBoxPicType.Text = "Picture";
            toolStripTextBoxPicType.TextBoxTextAlign = HorizontalAlignment.Center;
            // 
            // toolStripSeparator1
            // 
            toolStripSeparator1.Name = "toolStripSeparator1";
            toolStripSeparator1.Size = new Size(177, 6);
            // 
            // viewToolStripMenuItem
            // 
            viewToolStripMenuItem.Name = "viewToolStripMenuItem";
            viewToolStripMenuItem.Size = new Size(180, 22);
            viewToolStripMenuItem.Text = "View...";
            viewToolStripMenuItem.Click += viewToolStripMenuItem_Click;
            // 
            // newToolStripMenuItem
            // 
            newToolStripMenuItem.Name = "newToolStripMenuItem";
            newToolStripMenuItem.Size = new Size(180, 22);
            newToolStripMenuItem.Text = "New...";
            newToolStripMenuItem.Click += newToolStripMenuItem_Click;
            // 
            // deleteToolStripMenuItem
            // 
            deleteToolStripMenuItem.Name = "deleteToolStripMenuItem";
            deleteToolStripMenuItem.Size = new Size(180, 22);
            deleteToolStripMenuItem.Text = "Delete";
            deleteToolStripMenuItem.Click += deleteToolStripMenuItem_Click;
            // 
            // toolTipPicture
            // 
            toolTipPicture.ToolTipTitle = "Rolle";
            // 
            // filterToolStripMenuItem
            // 
            filterToolStripMenuItem.Name = "filterToolStripMenuItem";
            filterToolStripMenuItem.Size = new Size(180, 22);
            filterToolStripMenuItem.Text = "Filter";
            filterToolStripMenuItem.Click += filterToolStripMenuItem_Click;
            // 
            // MoviePictureUC
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            Controls.Add(groupBoxPicture);
            Name = "MoviePictureUC";
            Size = new Size(184, 200);
            Load += MoviePictureUC_Load;
            groupBoxPicture.ResumeLayout(false);
            groupBoxPicture.PerformLayout();
            tableLayoutPanelPicture.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)pictureBoxImage).EndInit();
            contextMenuStripUC.ResumeLayout(false);
            contextMenuStripUC.PerformLayout();
            ResumeLayout(false);
        }



        #endregion

        private GroupBox groupBoxPicture;
    private TableLayoutPanel tableLayoutPanelPicture;
    private PictureBox pictureBoxImage;
    private ContextMenuStrip contextMenuStripUC;
    private ToolStripMenuItem viewToolStripMenuItem;
    private ToolStripMenuItem newToolStripMenuItem;
    private ToolStripMenuItem deleteToolStripMenuItem;
    private ToolStripTextBox toolStripTextBoxPicType;
    private ToolStripSeparator toolStripSeparator1;
    private ToolTip toolTipPicture;
        private Label labelClose;
        private ToolStripMenuItem filterToolStripMenuItem;
    }
}
