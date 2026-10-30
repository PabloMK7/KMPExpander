using Extensions;
using KMPExpander.Class;
using KMPExpander.Class.SimpleKMPs;
using LibCTR.Collections;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using Tao.OpenGl;

namespace KMPExpander
{
    public partial class Form1 : Form
    {
        public VisualSettings Settings = new VisualSettings();
        private FormSettings settings_manager;

        public SimpleKMP Kayempee = null;
        public bool allowRender = true;
        public bool PreventClose = false;
        private string kmp_path = "";
        public UIMapPos UIMapPos = null;
        private string uimappos_path;
        public CDAB div = null;
        private string div_path = "";
        public OBJWrapper OBJModel = null;
        public KCL Collision = null;
        private ToolStripMenuItem closeKCLMenuItem;
        public CullingPreview Culling = null;
        private ToolStripButton toolStripButtonDrive;
        private ToolStripButton toolStripButtonRaceStart;
        private ToolStripMenuItem statsMenuItem, occludeMenuItem;
        private ToolStripMenuItem allRoutePointsMenuItem;
        private Label statsLabel;
        private System.Windows.Forms.Timer kart_drive_timer;
        // The karts drive at the game's 60 frames per second of real time: each tick steps the frames due since the last.
        private System.Diagnostics.Stopwatch kart_drive_clock = new System.Diagnostics.Stopwatch();
        private long kart_drive_frames;
        private bool karts_started = false;
        private System.Diagnostics.Stopwatch kart_grid_refresh = System.Diagnostics.Stopwatch.StartNew();
        public bool ShowAllRoutePoints = false;
        public ObjectModels ObjModels = null;
        private ToolStripMenuItem objectModelsMenuItem;
        private FormOBJ obj_manager;
        public ObjList objlist;
        public ErrorCheck er = null;
        public UInt16 lastObjectID = 5;
        public ViewPlaneHandler vph = null;

        public ISectionBase lastSelectedGroup;
        private ISectionBase lastSelectedSection;
        public List<object> SelectedDots = new List<object>();

        private void ApplyBlackTheme(Control control)
        {
            
            //menuStrip1.BackColor = Color.FromArgb(0xff,0x2d,0x2d,0x30);
            //menuStrip1.ForeColor = Color.White;
            foreach (Control c in control.Controls)
            {
                ApplyBlackTheme(c);
                c.BackColor = Color.FromArgb(0xff, 0x2d, 0x2d, 0x30);
            }
        }


        public Form1(string parameters)
        {
            InitializeKMPExpander();
            kmp_path = parameters;
        }

        public Form1()
        {
            InitializeKMPExpander();
        }

        private void InitializeKMPExpander()
        {
            // TODO: Fix this section as AI inserted the widgets here incorrectly.
            InitializeComponent();
            viewPlaneComboBox.SelectedIndex = 0;
            toolStripButtonDrive = new ToolStripButton("Drive Karts");
            toolStripButtonDrive.Alignment = ToolStripItemAlignment.Right;
            toolStripButtonDrive.Image = Properties.Resources.start_1;
            toolStripButtonDrive.ImageTransparentColor = Color.Magenta;
            toolStripButtonDrive.ToolTipText = "Drive or pause the enabled simulated karts";
            toolStripButtonDrive.Click += (s, e) => { if (karts_started) PauseDriving(kart_drive_timer.Enabled); else startDriving(); };
            toolStrip1.Items.Insert(toolStrip1.Items.IndexOf(toolStripSeparator11), toolStripButtonDrive);
            toolStripButtonRaceStart = new ToolStripButton("Reset Karts");
            toolStripButtonRaceStart.Alignment = ToolStripItemAlignment.Right;
            toolStripButtonRaceStart.ToolTipText = "Put the simulated karts back on the start grid and reset their state";
            toolStripButtonRaceStart.Click += (s, e) => RestartKarts();
            toolStrip1.Items.Insert(toolStrip1.Items.IndexOf(toolStripButtonDrive), toolStripButtonRaceStart);
            ToolStripDropDownButton viewButton = new ToolStripDropDownButton("View");
            viewButton.Alignment = ToolStripItemAlignment.Right;
            statsMenuItem = new ToolStripMenuItem("Triangle statistics") { CheckOnClick = true };
            statsMenuItem.CheckedChanged += (s, e) => Render();
            allRoutePointsMenuItem = new ToolStripMenuItem("Smoothed enemy points of every path") { CheckOnClick = true };
            allRoutePointsMenuItem.CheckedChanged += (s, e) => { ShowAllRoutePoints = allRoutePointsMenuItem.Checked; Render(); };
            viewButton.DropDownItems.Add(statsMenuItem);
            viewButton.DropDownItems.Add(allRoutePointsMenuItem);
            occludeMenuItem = new ToolStripMenuItem("Hide points behind the model") { CheckOnClick = true, Checked = true };
            occludeMenuItem.ToolTipText = "3D view: the model, the object models and the collision hide the KMP behind them, and those points cannot be picked";
            occludeMenuItem.CheckedChanged += (s, e) => Render();
            viewButton.DropDownItems.Add(occludeMenuItem);
            objectModelsMenuItem = new ToolStripMenuItem("Object models") { CheckOnClick = true, Checked = true };
            objectModelsMenuItem.ToolTipText = "Draw the objects with their models from the object models folder";
            objectModelsMenuItem.CheckedChanged += (s, e) => Render();
            ToolStripMenuItem objectModelsFolderMenuItem = new ToolStripMenuItem("Object models folder...");
            objectModelsFolderMenuItem.Click += (s, e) =>
            {
                FolderBrowserDialog dialog = new FolderBrowserDialog() { Description = "Folder of the extracted object models (<name>.obj, or OBJ files in a <name> folder)", SelectedPath = Settings.ObjectModelsFolder };
                if (dialog.ShowDialog() != DialogResult.OK) return;
                Settings.ObjectModelsFolder = dialog.SelectedPath;
                try { Settings.ToXML(); } catch { }
                LoadObjectModels();
                Render();
            };
            viewButton.DropDownItems.Add(new ToolStripSeparator());
            viewButton.DropDownItems.Add(objectModelsMenuItem);
            viewButton.DropDownItems.Add(objectModelsFolderMenuItem);
            InitializeGhosts(viewButton);
            toolStrip1.Items.Insert(toolStrip1.Items.IndexOf(viewPlaneComboBox), viewButton);
            statsLabel = new Label();
            statsLabel.AutoSize = true;
            statsLabel.BackColor = Color.FromArgb(40, 40, 40);
            statsLabel.ForeColor = Color.White;
            statsLabel.Font = new Font(FontFamily.GenericMonospace, 8.25f);
            statsLabel.Padding = new Padding(4);
            statsLabel.Visible = false;
            simpleOpenGlControl1.Controls.Add(statsLabel);
            ToolStripMenuItem openKCLMenuItem = new ToolStripMenuItem("Open KCL");
            openKCLMenuItem.Click += (s, e) =>
            {
                OpenFileDialog dialog = new OpenFileDialog() { Filter = "Mario Kart 7 Collision (*.kcl)|*.kcl" };
                if (dialog.ShowDialog() == DialogResult.OK) loadKCL(dialog.FileName);
            };
            closeKCLMenuItem = new ToolStripMenuItem("Close KCL") { Enabled = false };
            closeKCLMenuItem.Click += (s, e) =>
            {
                if (Collision != null) Collision.Dispose();
                Collision = null;
                closeKCLMenuItem.Enabled = false;
                populateTreeView();
                Render();
            };
            ToolStripMenuItem linkEndsMenuItem = new ToolStripMenuItem("Set battle link ends from geometry");
            linkEndsMenuItem.ToolTipText = "Set the link end flags of every enemy group: each link meets the end of the linked group nearer to this group's end, as on the original battle courses";
            linkEndsMenuItem.Click += (s, e) =>
            {
                if (Kayempee == null) return;
                int changed = Kayempee.EnemyRoutes.SetLinkEndsFromGeometry();
                toolStripStatusLabel1.Text = "Link end flags changed on " + changed + " enemy groups";
                propertyGrid1.Refresh();
                Render();
            };
            toolStripMenuItem5.DropDownItems.Add(linkEndsMenuItem);
            int kclMenuIndex = fileToolStripMenuItem.DropDownItems.IndexOf(toolStripMenuItemCloseDiv) + 1;
            fileToolStripMenuItem.DropDownItems.Insert(kclMenuIndex, closeKCLMenuItem);
            fileToolStripMenuItem.DropDownItems.Insert(kclMenuIndex, openKCLMenuItem);
            fileToolStripMenuItem.DropDownItems.Insert(kclMenuIndex, new ToolStripSeparator());
            kart_drive_timer = new System.Windows.Forms.Timer();
            kart_drive_timer.Interval = 16;
            kart_drive_timer.Tick += kart_drive_timer_Tick;
            // Hovering a column shows the Description of its property, on the header and on its cells, for 30 seconds (the
            // grid's own tooltips close after 5).
            ToolTip gridToolTip = new ToolTip() { AutoPopDelay = 30000, InitialDelay = 400, ReshowDelay = 100 };
            dataGridView1.ShowCellToolTips = false;
            dataGridView1.CellMouseEnter += (s, e) =>
            {
                string text = "";
                if (e.ColumnIndex >= 0 && dataGridView1.DataSource != null)
                {
                    string name = dataGridView1.Columns[e.ColumnIndex].DataPropertyName;
                    PropertyDescriptor property = ListBindingHelper.GetListItemProperties(dataGridView1.DataSource)[name];
                    if (property != null && !String.IsNullOrEmpty(property.Description)) text = WrapText(property.Description, 80);
                }
                if (text != gridToolTip.GetToolTip(dataGridView1)) gridToolTip.SetToolTip(dataGridView1, text);
            };
            dataGridView1.MouseLeave += (s, e) => gridToolTip.SetToolTip(dataGridView1, "");
            try
            {
                Settings = VisualSettings.FromXML();
            }
            catch
            {
                Settings = new VisualSettings();
            }
            LoadObjectModels();
            AllowDrop = true;
            this.DragEnter += new DragEventHandler(Form1_DragEnter);
            this.DragDrop += new DragEventHandler(Form1_DragDrop);
            obj_manager = new FormOBJ(this);
            settings_manager = new FormSettings(this);
            ImageList ImageList = new ImageList();
            ImageList.Images.Add(Properties.Resources.empty);
            ImageList.Images.Add(Properties.Resources.start_1);
            ImageList.Images.Add(Properties.Resources.enemy_2);
            ImageList.Images.Add(Properties.Resources.item_4);
            ImageList.Images.Add(Properties.Resources.glider_1);
            ImageList.Images.Add(Properties.Resources.checkpoint_1);
            ImageList.Images.Add(Properties.Resources.jugem_1);
            ImageList.Images.Add(Properties.Resources.objects_3);
            ImageList.Images.Add(Properties.Resources.routes_1);
            ImageList.Images.Add(Properties.Resources.area_1);
            ImageList.Images.Add(Properties.Resources.came_1);
            ImageList.Images.Add(Properties.Resources.stage_1);
            ImageList.Images.Add(Properties.Resources.genm); // 12
            ImageList.Images.Add(Properties.Resources.gitem);
            ImageList.Images.Add(Properties.Resources.gglider);
            ImageList.Images.Add(Properties.Resources.gcheckpoint);
            ImageList.Images.Add(Properties.Resources.groutes);
            ImageList.Images.Add(Properties.Resources.div_p);
            ImageList.Images.Add(Properties.Resources.div_c);
            treeView1.ImageList = ImageList;
            treeView1.HideSelection = false;
            objlist = new ObjList();
            vph = new ViewPlaneHandler();
            InitializeView3D();
            InitializeUndo();
            //ApplyBlackTheme(this);
        }

        void Form1_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop)) e.Effect = DragDropEffects.Copy;
        }

        void Form1_DragDrop(object sender, DragEventArgs e)
        {
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
            bool divLoaded = false, kmpLoaded = false, uimpLoaded = false;
            foreach (string file in files)
            {
                switch(Path.GetExtension(file))
                {
                    case ".kmp":
                        if (!kmpLoaded) kmpLoaded = true;
                        else break;
                        kmp_path = file;
                        loadKMP();
                        break;
                    case ".div":
                        if (!divLoaded) divLoaded = true;
                        else break;
                        div_path = file;
                        loadDiv();
                        break;
                    case ".kcl":
                        loadKCL(file);
                        break;
                    case ".bin":
                        long length = new System.IO.FileInfo(file).Length;
                        if (length != 0x21) break;
                        if (!uimpLoaded) uimpLoaded = true;
                        else break;
                        uimappos_path = file;
                        loadUIMapPos();
                        break;
                    default:
                        break;
                }
            }
            Render();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            viewPlaneComboBox.SelectedIndex = 0;

            if (kmp_path.Length == 0) return;

            try
            {
                loadKMP();
            }
            catch
            {
                MessageBox.Show("Couldn't load the KMP!");
            }
        }

        private int clamp(int value, int min, int max)
        {
            return Math.Min(Math.Max(value, min), max);
        }

        #region menu

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (PreventClose)
            {
                e.Cancel = true;
                return;
            }
            if (Kayempee == null) return;

            DialogResult dialogResult = MessageBox.Show("Do you want to save changes?", "KMP Expander", MessageBoxButtons.YesNoCancel);
            if (dialogResult == DialogResult.Cancel || dialogResult == DialogResult.Abort) e.Cancel = true;

            if (dialogResult == DialogResult.Yes) saveKMP();

        }

        private void toggle3DViewToolStripMenuItem_Click(object sender, EventArgs e)
        {
            viewPlaneComboBox.SelectedIndex = mode_3d ? 0 : 3;
        }

        private void importOBJToolStripMenuItem_Click(object sender, EventArgs e)
        {
            obj_manager.ShowDialog();
        }

        private void setMapVisibleStripMenuItem_Click(object sender, EventArgs e)
        {
            if (UIMapPos == null) return;
            setMapVisibility(UIMapPos.Visibility != UIMapPos.MapVisibility.Visible ? UIMapPos.MapVisibility.Visible : UIMapPos.MapVisibility.Hidden);
            Render();
        }

        private void setMapTransparentStripMenuItem_Click(object sender, EventArgs e)
        {
            if (UIMapPos == null) return;
            setMapVisibility(UIMapPos.Visibility != UIMapPos.MapVisibility.Transparent ? UIMapPos.MapVisibility.Transparent : UIMapPos.MapVisibility.Hidden);
            Render();
        }

        private void setMapHiddenStripMenuItem_Click(object sender, EventArgs e)
        {
            if (UIMapPos == null) return;
            setMapVisibility(UIMapPos.Visibility != UIMapPos.MapVisibility.Hidden ? UIMapPos.MapVisibility.Hidden : UIMapPos.MapVisibility.Visible);
            Render();
        }

        private void setMapVisibility(UIMapPos.MapVisibility visibility)
        {
            UIMapPos.Visibility = visibility;

            setMapVisibleStripMenuItem.Checked = visibility == UIMapPos.MapVisibility.Visible;
            setMapTransparentStripMenuItem.Checked = visibility == UIMapPos.MapVisibility.Transparent;
            setMapHiddenStripMenuItem.Checked = visibility == UIMapPos.MapVisibility.Hidden;
        }

        private void optionsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            settings_manager.ShowDialog();
        }

        private void loadToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (openFileDialogImage.ShowDialog() == DialogResult.OK)
            {
                Bitmap LocalMap = new Bitmap(new MemoryStream(File.ReadAllBytes(openFileDialogImage.FileName)));
                LocalMap.RotateFlip(RotateFlipType.RotateNoneFlipY);
                UIMapPos.LocalMap.LoadImage(LocalMap, true);
                unloadToolStripMenuItem.Enabled = true;
                Render();
            }
        }

        private void unloadToolStripMenuItem_Click(object sender, EventArgs e)
        {
            UIMapPos.LocalMap.UnloadImage();
            (sender as ToolStripMenuItem).Enabled = false;
            Render();
        }

        private void loadGlobalMapToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (openFileDialogImage.ShowDialog() == DialogResult.OK)
            {
                Bitmap GlobalMap = new Bitmap(new MemoryStream(File.ReadAllBytes(openFileDialogImage.FileName)));
                GlobalMap.RotateFlip(RotateFlipType.RotateNoneFlipY);
                UIMapPos.GlobalMap.LoadImage(GlobalMap, false);
                unloadGlobalMapToolStripMenuItem.Enabled = true;
                Render();
            }
        }

        private void unloadGlobalMapToolStripMenuItem_Click(object sender, EventArgs e)
        {
            UIMapPos.GlobalMap.UnloadImage();
            (sender as ToolStripMenuItem).Enabled = false;
            Render();
        }

        private void toolStripMenuItemSaveUIMap_Click(object sender, EventArgs e)
        {
            File.WriteAllBytes(uimappos_path, UIMapPos.Write());
        }

        private void importImageToolStripMenuItem_Click(object sender, EventArgs e)
        {
            
        }

        private void toolStripMenuItemOpenUIMap_Click(object sender, EventArgs e)
        {
            if (openFileDialogUIMap.ShowDialog() == DialogResult.OK)
            {
                uimappos_path = openFileDialogUIMap.FileName;
                loadUIMapPos();
                Render();
            }
        }

        private void loadUIMapPos()
        {
            UIMapPos = new UIMapPos(File.ReadAllBytes(uimappos_path));
            toolStripMenuItemSaveUIMap.Enabled = true;
            importImageToolStripMenuItem.Enabled = true;
            closeUIMapStripMenuItem7.Enabled = true;
            setMapVisibility(UIMapPos.MapVisibility.Visible);
        }

        private void toolStripMenuItemOpenDiv_Click(object sender, EventArgs e)
        {
            if (openFileDialogDiv.ShowDialog() == DialogResult.OK)
            {
                div_path = openFileDialogDiv.FileName;
                loadDiv();
                Render();
            }
        }

        private void loadDiv()
        {
            div = new CDAB(File.ReadAllBytes(div_path));
            toolStripMenuItemCloseDiv.Enabled = true;
            saveDivToolStripMenuItem.Enabled = true;
            populateTreeView();
        }

        private void loadKCL(string path)
        {
            try
            {
                KCL kcl = new KCL(path);
                if (Collision != null) Collision.Dispose();
                Collision = kcl;
                closeKCLMenuItem.Enabled = true;
                // Karts that are not driving go back to the start, on the floor of the collision.
                if (Kayempee != null && !karts_started) Kayempee.KartSimulator.PlaceOnGrid(Kayempee, OBJModel);
                populateTreeView();
                Render();
                toolStripStatusLabel1.Text = "KCL loaded: " + kcl.Triangles.Count + " triangles";
            }
            catch (Exception ex)
            {
                MessageBox.Show("Couldn't load the KCL: " + ex.Message);
            }
        }

        private void toolStripMenuItemCreateDiv_Click(object sender, EventArgs e)
        {
            if (openFileDialogDivObj.ShowDialog() == DialogResult.OK)
            {
                if (saveFileDialogCmdl.ShowDialog() == DialogResult.OK)
                {
                    CDAB tempdiv = CDAB.createFromCmdl(openFileDialogDivObj.FileName, saveFileDialogCmdl.FileName);
                    if (tempdiv == null) return;
                    div = tempdiv;
                    toolStripMenuItemCloseDiv.Enabled = true;
                    saveDivToolStripMenuItem.Enabled = true;
                    populateTreeView();
                    Render();
                    toolStripStatusLabel1.Text = "Div created successfully";
                }
            }
        }

        private void saveFileDialogDiv_FileOk(object sender, CancelEventArgs e)
        {
            div_path = (sender as SaveFileDialog).FileName;
            File.WriteAllBytes(div_path, div.Write());
            toolStripStatusLabel1.Text = "Div saved successfully";
        }

        private void saveDivToolStripMenuItem_Click(object sender, EventArgs e)
        {
            saveFileDialogDiv.ShowDialog();
        }

        private void toolStripMenuItemCloseDiv_Click(object sender, EventArgs e)
        {
            div = null;
            populateTreeView();
            (sender as ToolStripMenuItem).Enabled = false;
            saveDivToolStripMenuItem.Enabled = false;
            Render();
        }

        private void openKMPToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (openFileDialogKMP.ShowDialog()==DialogResult.OK)
            {
                kmp_path = openFileDialogKMP.FileName;
                loadKMP();
                Render();
            }
        }

        // Text broken into lines of at most width characters, at spaces.
        private static string WrapText(string text, int width)
        {
            System.Text.StringBuilder result = new System.Text.StringBuilder();
            int line = 0;
            foreach (string word in text.Split(' '))
            {
                if (line > 0 && line + 1 + word.Length > width)
                {
                    result.Append('\n');
                    line = 0;
                }
                else if (line > 0)
                {
                    result.Append(' ');
                    line++;
                }
                result.Append(word);
                line += word.Length;
            }
            return result.ToString();
        }

        public SimpleKMP getKayEmPee()
        {
            return Kayempee;
        }

        private void loadKMP()
        {
            if (karts_started) StopDriving();
            Kayempee = new SimpleKMP(File.ReadAllBytes(kmp_path));
            Kayempee.KartSimulator.PlaceOnGrid(Kayempee, OBJModel);
            Text = "KMP Expander (" + kmp_path + ")";
            populateTreeView();
            dataGridView1.DataSource = null;
            closeKMPStripMenuItem6.Enabled = true;
            saveAsToolStripMenuItem.Enabled = true;
            saveToolStripMenuItem.Enabled = true;
            exportXMLToolStripMenuItem.Enabled = true;
            ResetUndo();
        }

        private void closeKMPStripMenuItem6_Click(object sender, EventArgs e)
        {
            if (karts_started) StopDriving();
            Kayempee = null;
            saveToolStripMenuItem.Enabled = false;
            saveAsToolStripMenuItem.Enabled = false;
            exportXMLToolStripMenuItem.Enabled = false;
            (sender as ToolStripMenuItem).Enabled = false;
            Render();
            populateTreeView();
            lastSelectedSection = null;
            lastSelectedGroup = null;
            dataGridView1.DataSource = null;
            propertyGrid1.SelectedObject = null;
            kmp_path = null;
            Text = "KMP Expander";
            ResetUndo();
        }

        private void closeUIMapStripMenuItem7_Click(object sender, EventArgs e)
        {
            UIMapPos.LocalMap.UnloadImage();
            UIMapPos = null;
            importImageToolStripMenuItem.Enabled = false;
            toolStripMenuItemSaveUIMap.Enabled = false;
            unloadToolStripMenuItem.Enabled = false;
            unloadGlobalMapToolStripMenuItem.Enabled = false;
            (sender as ToolStripMenuItem).Enabled = false;
            Render();
        }

        private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            MessageBox.Show("KMP Expander v2.0 Made by Ermelber & PabloMK7.\nLicensed under the MIT License.");
        }

        private void saveKMP()
        {
            File.WriteAllBytes(kmp_path, Kayempee.Write());
            Text = "KMP Expander (" + kmp_path + ")";
            toolStripStatusLabel1.Text = "Saved successfully";
        }

        private void openFileDialogKMP_FileOk(object sender, CancelEventArgs e)
        {

        }

        private void saveFileDialogKMP_FileOk(object sender, CancelEventArgs e)
        {
            kmp_path = (sender as SaveFileDialog).FileName;
            saveKMP();
            saveToolStripMenuItem.Enabled = true;
        }

        private void saveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            saveKMP();
        }

        private void saveAsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            saveFileDialogKMP.ShowDialog();
        }

        private void exportXMLToolStripMenuItem_Click(object sender, EventArgs e)
        {
            saveFileDialogXML.ShowDialog();
        }

        private void importXMLToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (openFileDialogXML.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    Kayempee = SimpleKMP.FromXML(File.ReadAllBytes(openFileDialogXML.FileName));
                    Kayempee.KartSimulator.PlaceOnGrid(Kayempee, OBJModel);
                    populateTreeView();
                    dataGridView1.DataSource = null;
                    saveAsToolStripMenuItem.Enabled = true;
                    exportXMLToolStripMenuItem.Enabled = true;
                    closeKMPStripMenuItem6.Enabled = true;
                    ResetUndo();
                    Render();
                }
                catch (Exception)
                {
                    MessageBox.Show("Cannot import XML.");
                }
            }
        }

        private void saveFileDialogXML_FileOk(object sender, CancelEventArgs e)
        {
            File.WriteAllBytes(saveFileDialogXML.FileName, Kayempee.WriteXML());
            toolStripStatusLabel1.Text = "Exported successfully";
        }

        #endregion

        #region treeview

        private void treeView1_AfterCheck(object sender, TreeViewEventArgs e)
        {
            if (e.Action == TreeViewAction.ByMouse)
            {
                if (e.Node.Tag is ISectionBase)
                    ((ISectionBase)e.Node.Tag).SetVisibility(((ISectionBase)e.Node.Tag).GetVisibility() ? false : true);
                else if (e.Node.Tag is CDAB)
                    ((CDAB)e.Node.Tag).SetVisibility(((CDAB)e.Node.Tag).GetVisibility() ? false : true);
                else if (e.Node.Tag is CDAB.STRM)
                    ((CDAB.STRM)e.Node.Tag).SetVisibility(((CDAB.STRM)e.Node.Tag).GetVisibility() ? false : true);
                else if (e.Node.Tag is CDAB.STRM.STRMEntry)
                    ((CDAB.STRM.STRMEntry)e.Node.Tag).SetVisibility(((CDAB.STRM.STRMEntry)e.Node.Tag).GetVisibility() ? false : true);
                else if (e.Node.Tag is KCL)
                    ((KCL)e.Node.Tag).SetVisibility(e.Node.Checked);
                else if (e.Node.Tag is KCL.TypeNode && Collision != null)
                {
                    // A collision type shows or hides all its variants.
                    foreach (TreeNode child in e.Node.Nodes)
                    {
                        KCL.VariantNode v = (KCL.VariantNode)child.Tag;
                        Collision.SetHidden(v.Parent.Type, v.Variant, !e.Node.Checked);
                        child.Checked = e.Node.Checked;
                    }
                }
                else if (e.Node.Tag is KCL.VariantNode && Collision != null)
                {
                    KCL.VariantNode v = (KCL.VariantNode)e.Node.Tag;
                    Collision.SetHidden(v.Parent.Type, v.Variant, !e.Node.Checked);
                    bool any = false;
                    foreach (TreeNode sibling in e.Node.Parent.Nodes) any |= sibling.Checked;
                    e.Node.Parent.Checked = any;
                }
                Render();
            }
        }

        private void treeView1_BeforeCheck(object sender, TreeViewCancelEventArgs e)
        {
            
              
        }

        private void treeView1_AfterSelect(object sender, TreeViewEventArgs e)
        {
            if (e.Node.Tag is ISectionBase)
            {
                toolStripStatusLabel1.Text = e.Node.Text;
                propertyGrid1.SelectedObject = e.Node.Tag;

                if (e.Node.Tag == null)
                {
                    dataGridView1.DataSource = null;
                    return;
                }

                toolStripStatusLabel1.Text = e.Node.Text;

                if (e.Node.Parent != null)
                {
                    lastSelectedGroup = (ISectionBase)e.Node.Tag;
                    toolStripStatusLabel1.Text = e.Node.Parent.Text + ": " + e.Node.Text;
                    dataGridView1.DataSource = lastSelectedGroup.GetEntries();
                }else if (treeView1.SelectedNode.Tag.GetType().Equals(typeof(StartPositions)) ||
                            treeView1.SelectedNode.Tag.GetType().Equals(typeof(RespawnPoints)) ||
                            treeView1.SelectedNode.Tag.GetType().Equals(typeof(Objects)) ||
                            treeView1.SelectedNode.Tag.GetType().Equals(typeof(Area)) ||
                            treeView1.SelectedNode.Tag.GetType().Equals(typeof(Camera)) ||
                            treeView1.SelectedNode.Tag.GetType().Equals(typeof(StageInformation)) ||
                            treeView1.SelectedNode.Tag.GetType().Equals(typeof(KartSimulator)))
                {
                    lastSelectedGroup = (ISectionBase)e.Node.Tag;
                    lastSelectedSection = (ISectionBase)e.Node.Tag;
                    dataGridView1.DataSource = lastSelectedGroup.GetEntries();
                }
                else
                {
                    lastSelectedGroup = null;
                    lastSelectedSection = (ISectionBase)e.Node.Tag;
                    dataGridView1.DataSource = null;
                }
            } else
            {
                lastSelectedGroup = null;
                // A collision type or variant shows what it is in the property grid, the Collision node its render options.
                if (e.Node.Tag is KCL) propertyGrid1.SelectedObject = (e.Node.Tag as KCL).Options;
                else propertyGrid1.SelectedObject = (e.Node.Tag is KCL.TypeNode || e.Node.Tag is KCL.VariantNode) ? e.Node.Tag : null;
                dataGridView1.DataSource = null;
                lastSelectedSection = null;
                if (e.Node.Parent != null)
                {
                    toolStripStatusLabel1.Text = e.Node.Parent.Text + ": " + e.Node.Text;
                } else
                {
                    toolStripStatusLabel1.Text = e.Node.Text;
                }
            }
        }

        private void propertyGrid1_Click(object sender, EventArgs e)
        {

        }

        private void addToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (lastSelectedSection == null) return;
            object Adding = lastSelectedSection.Add();
            populateTreeView();
            foreach (TreeNode node in treeView1.Nodes)
                foreach (TreeNode subnode in node.Nodes)
                    if (subnode.Tag == Adding)
                    {
                        treeView1.SelectedNode = subnode;
                        break;
                    }
        }

        private void deleteToolStripMenuItem_Click(object sender, EventArgs e)
        {
            
        }

        private void treeView1_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                // Select the clicked node
                treeView1.SelectedNode = treeView1.GetNodeAt(e.X, e.Y);
                if (treeView1.SelectedNode != null && treeView1.SelectedNode.Tag is ISectionBase)
                {
                    if (treeView1.SelectedNode.Parent == null)
                    {
                        if (treeView1.SelectedNode.Tag.GetType().Equals(typeof(StartPositions)) ||
                            treeView1.SelectedNode.Tag.GetType().Equals(typeof(RespawnPoints)) ||
                            treeView1.SelectedNode.Tag.GetType().Equals(typeof(Objects)) ||
                            treeView1.SelectedNode.Tag.GetType().Equals(typeof(Area)) ||
                            treeView1.SelectedNode.Tag.GetType().Equals(typeof(Camera)) ||
                            treeView1.SelectedNode.Tag.GetType().Equals(typeof(KartSimulator)))
                        {
                            contextMenuStripSection.Items[0].Visible = false;
                            contextMenuStripSection.Items[1].Visible = false;
                        }
                        else
                        {
                            contextMenuStripSection.Items[0].Visible = true;
                            contextMenuStripSection.Items[1].Visible = true;
                            lastSelectedSection = (ISectionBase)treeView1.SelectedNode.Tag;
                        }
                        contextMenuStripSection.Show(treeView1, e.Location);
                    }
                    else
                    {
                        contextMenuStripGroup.Show(treeView1, e.Location);
                        lastSelectedSection = (ISectionBase)treeView1.SelectedNode.Parent.Tag;
                    }

                }
            }
        }

        private void insertToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (lastSelectedSection == null) return;
            object Adding = lastSelectedSection.Insert(lastSelectedSection.IndexOf(lastSelectedGroup)+1);
            populateTreeView();
            foreach (TreeNode node in treeView1.Nodes)
                foreach (TreeNode subnode in node.Nodes)
                    if (subnode.Tag == Adding)
                    {
                        treeView1.SelectedNode = subnode;
                        break;
                    }
        }

        private void removetoolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (lastSelectedSection == null) return;
            lastSelectedSection.Remove(lastSelectedGroup);
            populateTreeView();
            foreach (TreeNode node in treeView1.Nodes)
                if (node.Tag == lastSelectedSection)
                {
                    node.Expand();
                    treeView1.SelectedNode = node;
                }
        }

        private void shiftToolStripMenuItem_Click(object sender, EventArgs e, bool moveUp = false)
        {
            if (lastSelectedSection == null) return;
            object Shifting = lastSelectedGroup;
            int row = lastSelectedSection.IndexOf(Shifting);
            if (moveUp) row--;
            else row++;

            if (row < 0) row = 0;
            if (row > lastSelectedSection.GetCount() - 1) row = lastSelectedSection.GetCount() - 1;

            lastSelectedSection.Remove(Shifting);
            lastSelectedSection.Insert(row, Shifting);
            populateTreeView();
            foreach (TreeNode node in treeView1.Nodes)
                foreach (TreeNode subnode in node.Nodes)
                    if (subnode.Tag == Shifting)
                    {
                        treeView1.SelectedNode = subnode;
                        break;
                    }
            Render();
        }

        private void shiftUptoolStripMenuItem_Click(object sender, EventArgs e)
        {
            shiftToolStripMenuItem_Click(sender, e, true);
        }

        private void shiftDownToolStripMenuItem_Click(object sender, EventArgs e)
        {
            shiftToolStripMenuItem_Click(sender, e);
        }


        #endregion

        #region datagridview

        //Maybe I will change this into something else, for now it's fine
        private void dataGridView1_CellBeginEdit(object sender, DataGridViewCellCancelEventArgs e)
        {
            if(SelectedDots.Count == 0) return;
            if (SelectedDots[0].GetType() == typeof(Objects.ObjectEntry))
            {
                if (e.ColumnIndex == 0)
                {
                    if (objlist.displayName.Count != 0)
                    {
                        new FormObjectList(SelectedDots[0] as Objects.ObjectEntry, e.RowIndex).ShowDialog();
                        e.Cancel = true;
                    }
                }
            } else if (SelectedDots[0].GetType() == typeof(StageInformation.StageInformationEntry))
            {
                if (e.ColumnIndex == 4)
                {
                    colorDialog1.Color = (SelectedDots[0] as StageInformation.StageInformationEntry).FlareColor;
                    e.Cancel = true;
                    if (colorDialog1.ShowDialog() == DialogResult.OK)
                    {
                        (SelectedDots[0] as StageInformation.StageInformationEntry).FlareColor = colorDialog1.Color;
                        
                    }
                }
            }
            
        }

        private void toolStripButtonPencil_Click(object sender, EventArgs e)
        {
            (sender as ToolStripButton).Checked = ((sender as ToolStripButton).Checked ? false : true);
            pencil_mode = (sender as ToolStripButton).Checked;
        }

        private void dataGridView1_SelectionChanged(object sender, EventArgs e)
        {
            if (!start_move)
            {
                SelectedDots.Clear();
                foreach (DataGridViewRow row in (sender as DataGridView).SelectedRows)
                    SelectedDots.Add(row.DataBoundItem);
                Render();
            }
        }

        private void dataGridView1_CellMouseUp(object sender, DataGridViewCellMouseEventArgs e)
        {
            
        }

        private void dataGridView1_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.ColumnIndex >= 0 && e.ColumnIndex < dataGridView1.Columns.Count)
            {
                string name = dataGridView1.Columns[e.ColumnIndex].Name;
                if (name == "Cornering" || name == "HoldDrift" || name == "Precise" || name == "Enabled" || name == "Respawn" || name == "Reroute")
                {
                    dataGridView1.EndEdit();
                    Render();
                }
            }
            
        }

        private void toolStripButtonRemove_Click(object sender, EventArgs e)
        {
            if (lastSelectedGroup == null) return;

            List<object> Removing = new List<object>();

            foreach (DataGridViewRow row in dataGridView1.SelectedRows)
                Removing.Add(row.DataBoundItem);
            dataGridView1.DataSource = null;
            foreach (var entry in Removing)
                lastSelectedGroup.Remove(entry);
            dataGridView1.DataSource = lastSelectedGroup.GetEntries();
        }

        private void treeView1_NodeMouseClick(object sender, TreeNodeMouseClickEventArgs e)
        {
            
        }

        private void toolStripButtonMove_Click(object sender, EventArgs e, bool moveUp = false)
        {
            if (lastSelectedGroup == null || dataGridView1.AreAllCellsSelected(false)) return;

            List<object> Shifting = new List<object>();
            int row_idx = -1;
            bool toReverse = false;

            if (lastSelectedGroup == null) return;

            if (dataGridView1.SelectedRows.Count > 1)
            {
                int i = 0;
                int prev_row = -1;
                foreach (DataGridViewRow row in dataGridView1.SelectedRows)
                {
                    int temp_row = int.Parse(row.HeaderCell.Value.ToString());
                    if (temp_row < (uint)row_idx)
                    {
                        row_idx = temp_row;
                        toReverse = (row.Index > prev_row);
                    }
                    i++;
                    prev_row = row.Index;
                }
            }
            else row_idx = int.Parse(dataGridView1.SelectedRows[0].HeaderCell.Value.ToString());

            if (moveUp) row_idx--;
            else row_idx++;

            if ((moveUp && row_idx == -1) || (!moveUp && row_idx+dataGridView1.SelectedRows.Count>dataGridView1.Rows.Count)) return;

            foreach (DataGridViewRow row in dataGridView1.SelectedRows)
                Shifting.Add(row.DataBoundItem);
            if (toReverse) Shifting.Reverse();

            dataGridView1.DataSource = null;
            foreach (var entry in Shifting)
                lastSelectedGroup.Remove(entry);
            foreach (var entry in Shifting)
                lastSelectedGroup.Insert(row_idx, entry);
            dataGridView1.DataSource = lastSelectedGroup.GetEntries();
            dataGridView1.ClearSelection();
            foreach (DataGridViewRow row in dataGridView1.Rows)
                if (Shifting.Contains(row.DataBoundItem)) row.Selected = true;
            
        }

        private object AddEntry()
        {
            int row_idx = -1;

            if (lastSelectedGroup == null) return null;

            bool empty = dataGridView1.Rows.Count == 0;

            if (dataGridView1.SelectedRows.Count > 0)
            {
                foreach (DataGridViewRow row in dataGridView1.SelectedRows)
                    row_idx = row.Index < (uint)row_idx ? row.Index : row_idx;
            }
            row_idx++;

            dataGridView1.DataSource = null;
            object Adding = null;

            if (empty) lastSelectedGroup.New();
            Adding = lastSelectedGroup.Insert(row_idx);
            
            return Adding;
        }

        private void toolStripButtonAdd_Click(object sender, EventArgs e)
        {
            if (lastSelectedGroup == null) return;
            object Adding = AddEntry();

            dataGridView1.DataSource = lastSelectedGroup.GetEntries();

            dataGridView1.ClearSelection();
            foreach (DataGridViewRow row in dataGridView1.Rows)
                if (Adding == row.DataBoundItem)
                {
                    try
                    {
                        row.Selected = true;
                        dataGridView1.FirstDisplayedScrollingRowIndex = row.Index;
                    }
                    catch
                    {

                    }
                    break;
                }
        }

        private void toolStripButtonUp_Click(object sender, EventArgs e)
        {
            toolStripButtonMove_Click(sender, e, true);
        }

        private void toolStripButtonDown_Click(object sender, EventArgs e)
        {
            toolStripButtonMove_Click(sender, e);
        }

        private void dataGridView1_DataSourceChanged(object sender, EventArgs e)
        {
            //Shows indicies of the rows
            (sender as DataGridView).TopLeftHeaderCell.Value = "ID";
            foreach (DataGridViewRow row in (sender as DataGridView).Rows)
                row.HeaderCell.Value = row.Index.ToString();
            (sender as DataGridView).AutoResizeRowHeadersWidth(DataGridViewRowHeadersWidthSizeMode.AutoSizeToFirstHeader);
            Render();
        }

        private void dataGridView1_MouseDown(object sender, MouseEventArgs e)
        {
            if ((ModifierKeys & Keys.Control) == Keys.Control)
            {
                (sender as DataGridView).ClearSelection();
            }
            else
            {
                base.OnMouseDown(e);
            }
        }

        private void dataGridView1_KeyDown(object sender, KeyEventArgs e)
        {
            switch (e.KeyCode)
            {
                case Keys.Delete:
                    toolStripButtonRemove.PerformClick();
                    break;
                /*
                case Keys.Insert:
                    toolStripButtonRemove.PerformClick();
                    break;
                case Keys.:
                    toolStripButtonUp.PerformClick();
                    break;
                case Keys.PageDown:
                    toolStripButtonDown.PerformClick();
                    break;
                case (Keys.Control & Keys.Shift & Keys.Down):
                        toolStripButtonDown.PerformClick();
                        break;
                        */
            }
             
        }

        #endregion

        #region opengl

        public bool opengl_initialized = false;
        private bool start_move = false;
        private bool pencil_mode = false;
        private bool adding_checkpoint = false;
        public bool mode_3d = false;

        private float maximum_viewport = 16000;
        private float minimum_viewport = 300;
        private float viewport;
        public float Viewport
        {
            get { return viewport; }
            set
            {
                if (value > maximum_viewport) viewport = maximum_viewport;
                else if (value < minimum_viewport) viewport = minimum_viewport;
                else viewport = value;
            }
        }
        public Vector2 ViewportOffset;
        public SectionPicking.PickingInfo PickingInfo;
        private object AddingPencil = null;
        private Nullable<float> found_height = 0;

        private Point move_viewport_start_location = new Point();
        private bool panning2D = false;               // a right drag, or a left one from empty space, pans the 2D view
        private Point move_viewport_start_scroll = new Point();

        private void simpleOpenGlControl1_Load(object sender, EventArgs e)
        {
            simpleOpenGlControl1.MouseWheel += new MouseEventHandler(simpleOpenGlControl1_MouseWheel);
            viewport = maximum_viewport;
            ViewportOffset = new Vector2(0, 0);
            simpleOpenGlControl1.InitializeContexts();
            Gl.glEnable(Gl.GL_COLOR_MATERIAL);
            Gl.glEnable(Gl.GL_DEPTH_TEST);
            Gl.glDepthFunc(Gl.GL_ALWAYS);
            Gl.glEnable(Gl.GL_LOGIC_OP);
            Gl.glDisable(Gl.GL_CULL_FACE);
            Gl.glEnable(Gl.GL_TEXTURE_2D);
            Gl.glEnable(Gl.GL_LINE_SMOOTH);
            Gl.glEnable(Gl.GL_BLEND);
            Gl.glBlendFunc(Gl.GL_SRC_ALPHA, Gl.GL_ONE_MINUS_SRC_ALPHA);

            /*Gl.ReloadFunctions();
            Gl.glEnable(Gl.GL_RESCALE_NORMAL);
            Gl.glEnable(Gl.GL_COLOR_MATERIAL);
            Gl.glEnable(Gl.GL_DEPTH_TEST);
            Gl.glEnable(Gl.GL_NORMALIZE);
            Gl.glDisable(Gl.GL_CULL_FACE);
            Gl.glFrontFace(Gl.GL_CCW);
            Gl.glEnable(Gl.GL_TEXTURE_2D);
            Gl.glClearDepth(1);
            Gl.glEnable(Gl.GL_ALPHA_TEST);
            Gl.glAlphaFunc(Gl.GL_GREATER, 0f);
            Gl.glEnable(Gl.GL_BLEND);
            Gl.glBlendFunc(Gl.GL_SRC_ALPHA, Gl.GL_ONE_MINUS_SRC_ALPHA);
            Gl.glShadeModel(Gl.GL_SMOOTH);
            Gl.glDepthFunc(Gl.GL_LEQUAL);*/

            opengl_initialized = true;
            Render();
        }

        private void simpleOpenGlControl1_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == '+')
            {
                if (mode_3d) ChangeSpeed3D(true);
                else doScroll(true);

            } else if (e.KeyChar == '-')
            {
                if (mode_3d) ChangeSpeed3D(false);
                else doScroll(false);
            }
        }

        public void simpleOpenGlControl1_MouseWheel(object sender, MouseEventArgs e)
        {
            bool positive = e.Delta >= 0;
            if (mode_3d)
            {
                Wheel3D(positive);
                return;
            }

            if ((ModifierKeys & (Keys.Shift | Keys.Control)) == 0)
            {
                doScroll(positive);
            }
            else
            {
                int speed = positive ? 100 : -100;

                if ((ModifierKeys & Keys.Shift) > 0)
                    hScrollBar1.Value = clamp((int)(hScrollBar1.Value - getViewportLevel() * speed), hScrollBar1.Minimum, hScrollBar1.Maximum);
                else if ((ModifierKeys & Keys.Control) > 0)
                    vScrollBar1.Value = clamp((int)(vScrollBar1.Value - getViewportLevel() * speed), vScrollBar1.Minimum, vScrollBar1.Maximum);

                ViewportOffset = new Vector2(hScrollBar1.Value, vScrollBar1.Value);
                Render();
            }
        }

        public void doScroll(bool zoomIn)
        {
            if (zoomIn) Viewport /= 1.3f;
            else Viewport *= 1.3f;

            RectangleF disp = getDisplayRectangle(false);
            if (disp.Width < maximum_viewport * 2)
            {
                hScrollBar1.Maximum = (int)(maximum_viewport - disp.Width / 2f);
                hScrollBar1.Minimum = -(int)(maximum_viewport - disp.Width / 2f);
                hScrollBar1.Enabled = true;
            }
            else
            {
                hScrollBar1.Enabled = false;
                hScrollBar1.Value = hScrollBar1.Maximum = hScrollBar1.Minimum = 0;
                ViewportOffset = new Vector2(0, ViewportOffset.Z);
            }
            if (disp.Height < maximum_viewport * 2)
            {
                vScrollBar1.Maximum = (int)(maximum_viewport - disp.Height / 2f);
                vScrollBar1.Minimum = -(int)(maximum_viewport - disp.Height / 2f);
                vScrollBar1.Enabled = true;
            }
            else
            {
                vScrollBar1.Enabled = false;
                vScrollBar1.Value = vScrollBar1.Maximum = vScrollBar1.Minimum = 0;
                ViewportOffset = new Vector2(ViewportOffset.X, 0);
            }
            Render();
        }

        private void vScrollBar1_Scroll(object sender, ScrollEventArgs e)
        {
            ViewportOffset = new Vector2(hScrollBar1.Value, vScrollBar1.Value);
            Render();
        }

        private void hScrollBar1_Scroll(object sender, ScrollEventArgs e)
        {
            ViewportOffset = new Vector2(hScrollBar1.Value, vScrollBar1.Value);
            Render();
        }

        private void Form1_SizeChanged(object sender, EventArgs e)
        {
            Render();
        }

        private void dataGridView1_CellValidated(object sender, DataGridViewCellEventArgs e)
        {
            Render();
        }

        private void splitContainer1_SizeChanged(object sender, EventArgs e)
        {
            Render();
        }

        private void splitContainer3_SizeChanged(object sender, EventArgs e)
        {
            Render();
        }

        private void splitContainer3_SplitterMoved(object sender, SplitterEventArgs e)
        {
            Render();
        }

        private void propertyGrid1_PropertyValueChanged(object s, PropertyValueChangedEventArgs e)
        {
            Render();
        }

        private void simpleOpenGlControl1_Click(object sender, EventArgs e)
        {

        }

        private void simpleOpenGlControl1_MouseClick(object sender, MouseEventArgs e)
        {

        }

        private void PickPoint(Point mousepoint = new Point(), bool getY = false)
        {
            found_height = null;
            if (!opengl_initialized) return;
            
            byte[] pic = new byte[4] { 0xFF, 0xFF, 0xFF, 0xFF };

            if (mode_3d) Base3DRender(true);
            else BaseRender(true);
            if (mode_3d && occludeMenuItem.Checked) RenderGeometry3D(true);
            Gl.glLoadIdentity();
            Gl.glDisable(Gl.GL_POLYGON_SMOOTH);
            Gl.glDisable(Gl.GL_POINT_SMOOTH);
            Gl.glDisable(Gl.GL_LINE_SMOOTH);

            if (UIMapPos != null) UIMapPos.Render(true);
            if (Kayempee != null)
            {
                if (adding_checkpoint)
                {
                    Gl.glBindTexture(Gl.GL_TEXTURE_2D, 0);
                    Kayempee.CheckPoints.Render(true);
                }
                else Kayempee.Render(true);
            }
            RenderGizmo(true);
            Gl.glReadPixels(mousepoint.X, simpleOpenGlControl1.Height - mousepoint.Y, 1, 1, Gl.GL_BGRA, Gl.GL_UNSIGNED_BYTE, pic);
            Gl.glEnable(Gl.GL_LINE_SMOOTH);

            if (pic[0] == 0xFF && pic[1] == 0xFF && pic[2] == 0xFF && pic[3] == 0xFF)
            {
                PickingInfo.Section = Sections.None;
                PickingInfo.PointID = PointID.None;
                PickingInfo.GroupID = 0;
                PickingInfo.EntryID = 0;
            }
            else
                PickingInfo = SectionPicking.FromRgb(pic[2], pic[1], pic[0]);

            if (getY && Collision != null)
            {
                // With a KCL loaded, points are dropped onto its highest floor; onto the OBJ only where it has none.
                var picked_pos = Kayempee.GetPointPosition(PickingInfo);
                if (picked_pos != null) found_height = Collision.GetGroundHeight(picked_pos.Value.X, picked_pos.Value.Z);
            }
            if (getY && found_height == null && (OBJModel!=null))
            {
                var picked_pos = Kayempee.GetPointPosition(PickingInfo);
                if (picked_pos != null)
                {
                    Gl.glClear(Gl.GL_COLOR_BUFFER_BIT | Gl.GL_DEPTH_BUFFER_BIT);
                    Point screen_point = GetScreenPoint(picked_pos.Value);
                    OBJModel.Render(true);
                    Gl.glReadPixels(screen_point.X, simpleOpenGlControl1.Height - screen_point.Y, 1, 1, Gl.GL_BGRA, Gl.GL_UNSIGNED_BYTE, pic);

                    if (pic[0] == 0xFF && pic[1] == 0xFF && pic[2] == 0xFF && pic[3] == 0xFF)
                    {
                        return;
                    }
                    int face_id = ModelPicking.FromRgb(pic[2], pic[1], pic[0]);
                    found_height = OBJModel.GetHeightValue(face_id, picked_pos.Value);
                }
            }
            
            Gl.glClear(Gl.GL_COLOR_BUFFER_BIT | Gl.GL_DEPTH_BUFFER_BIT);
            Render();
            return;
        }

        private void simpleOpenGlControl1_MouseDown(object sender, MouseEventArgs e)
        {
            if (mode_3d)
            {
                MouseDown3D(e);
                return;
            }
            // As in the 3D view: the right button only moves the view, the left one picks and drags (on empty space it
            // moves the view too), and Shift with it snaps to the surface (in the top view, the height under the mouse).
            if (e.Button == MouseButtons.Right)
            {
                panning2D = true;
                move_viewport_start_location = e.Location;
                move_viewport_start_scroll = new Point(hScrollBar1.Value, vScrollBar1.Value);
                return;
            }
            if (e.Button != MouseButtons.Left) return;
            bool snap = (ModifierKeys & Keys.Shift) != 0;

            if (Kayempee != null && !(pencil_mode && lastSelectedGroup != null && !(lastSelectedGroup is KartSimulator)))
            {
                SectionPicking.PickingInfo previous = PickingInfo;
                PickPoint(e.Location);
                bool gizmo = PickingInfo.Section == Sections.Gizmo;
                int handle = PickingInfo.EntryID;
                PickingInfo = previous;
                if (gizmo)
                {
                    mouse3DDown = mouse3DLast = e.Location;
                    StartDrag(handle, true, e.Location);
                    if (snap && gizmoMode == TransformGizmo.Modes.Move && TransformGizmo.IsAxis(handle)) dragAxisSnap = true;
                    if (drag3D != Drag3D.None) return;
                }
            }

            if (pencil_mode && (lastSelectedGroup != null) && !(lastSelectedGroup is KartSimulator))
            {
                if (vph.mode != ViewPlaneHandler.PLANE_MODES.XZ && (lastSelectedGroup.GetType() == typeof(CheckPoints.CheckpointGroup) || lastSelectedGroup.GetType() == typeof(Area))) return;
                Vector3 Position = GetPosition(e.Location);
                AddingPencil = AddEntry();
                Position.Y = 0f;
                if (lastSelectedGroup.GetType() == typeof(CheckPoints.CheckpointGroup)) Kayempee.MoveAnyPoint(AddingPencil, Position, true);
                Kayempee.MoveAnyPoint(AddingPencil, Position);
                dataGridView1.DataSource = lastSelectedGroup.GetEntries();

                dataGridView1.ClearSelection();
                foreach (DataGridViewRow row in dataGridView1.Rows)
                    if (AddingPencil == row.DataBoundItem)
                    {
                        row.Selected = true;
                        dataGridView1.FirstDisplayedScrollingRowIndex = row.Index;
                        break;
                    }
                if (lastSelectedGroup.GetType() == typeof(CheckPoints.CheckpointGroup))
                {
                    adding_checkpoint = true;
                    start_move = true;
                    PickPoint(e.Location);
                    PickingInfo.PointID = PointID.Right;
                    return;
                }
                AddingPencil = null;
                return;
            }
            
            start_move = true;
            PickPoint(e.Location);
            SelectedDots.Clear();
            if (PickingInfo.Section!=Sections.None && PickingInfo.Section != Sections.LocalMap && PickingInfo.Section != Sections.GlobalMap)
            {
                SelectedDots.Add(Kayempee.GetPoint(PickingInfo));
                // The gizmo goes on the clicked side of a checkpoint.
                gizmoLeft = PickingInfo.PointID != PointID.Right;
                if (snap)
                {
                    dragKeepsTree = false;
                    StartSnap();
                    DragTo(e.Location);
                    Render();
                }
            }
            else if (PickingInfo.Section == Sections.None)
            {
                panning2D = true;
                move_viewport_start_location = e.Location;
                move_viewport_start_scroll = new Point(hScrollBar1.Value, vScrollBar1.Value);
            }
        }

        private void simpleOpenGlControl1_MouseMove(object sender, MouseEventArgs e)
        {
            if (mode_3d)
            {
                MouseMove3D(e);
                return;
            }
            if (drag3D != Drag3D.None)
            {
                DragTo(e.Location);
                Render();
                return;
            }
            if (panning2D)
            {
                if (e.Button == MouseButtons.None) return;
                float m = getViewportLevel();
                int deltaX = e.X - move_viewport_start_location.X;
                int deltaY = e.Y - move_viewport_start_location.Y;

                hScrollBar1.Value = clamp((int)(move_viewport_start_scroll.X - m * deltaX), hScrollBar1.Minimum, hScrollBar1.Maximum);
                vScrollBar1.Value = clamp((int)(move_viewport_start_scroll.Y - m * deltaY), vScrollBar1.Minimum, vScrollBar1.Maximum);

                ViewportOffset = new Vector2(hScrollBar1.Value, vScrollBar1.Value);
                Render();
            }
            else if (e.Button == MouseButtons.Left)
            {
                Vector3 position = GetPosition(e.Location);
                if (!start_move) return;
                start_move = true;
                if (PickingInfo.Section == Sections.None) return;

                if ((PickingInfo.Section == Sections.LocalMap || PickingInfo.Section == Sections.GlobalMap) && vph.mode == ViewPlaneHandler.PLANE_MODES.XZ) UIMapPos.MovePoint(PickingInfo, position);
                else Kayempee.MovePoint(PickingInfo, position);
                Render();
            }
        }

        private void simpleOpenGlControl1_MouseUp(object sender, MouseEventArgs e)
        {
            if (mode_3d)
            {
                MouseUp3D(e);
                return;
            }
            if (drag3D != Drag3D.None)
            {
                drag3D = Drag3D.None;
                gizmoActive = TransformGizmo.None;
                start_move = false;
                if (dragKeepsTree) dataGridView1.Refresh();
                else ExpandTreeView();
                Render();
                return;
            }
            panning2D = false;
            if (e.Button != MouseButtons.Left) return;

            Render();
            if (pencil_mode)
            {
                if (adding_checkpoint)
                {
                    ExpandTreeView();
                    start_move = false;
                    adding_checkpoint = false;
                }
                return;
            }

            ExpandTreeView();
            if (PickingInfo.Section == Sections.None) dataGridView1.ClearSelection();
            start_move = false;
        }

        private void SelectInDataGridView()
        {
            dataGridView1.ClearSelection();
            foreach (DataGridViewRow row in dataGridView1.Rows)
                if (SelectedDots.Contains(row.DataBoundItem))
                {
                    row.Selected = true;
                    dataGridView1.FirstDisplayedScrollingRowIndex = row.Index;
                }
        }

        private void ExpandTreeView()
        {
            if (Kayempee == null) return;

            ISectionBase section = Kayempee.GetSection(PickingInfo);
            ISectionBase group = Kayempee.GetGroup(PickingInfo);

            foreach (TreeNode node in treeView1.Nodes)
                if (node.Tag==section)
                {
                    foreach (TreeNode subnode in node.Nodes)
                        if (subnode.Tag == group)
                        {
                            treeView1.SelectedNode = subnode;
                            dataGridView1.DataSource = group.GetEntries();
                            SelectInDataGridView();
                            return;
                        }
                    treeView1.SelectedNode = node;
                    dataGridView1.DataSource = section.GetEntries();
                    SelectInDataGridView();
                    return;
                }
        }

        internal void Render()
        {
            if (!opengl_initialized || !allowRender) return;

            if (mode_3d) Render3D();
            else
            {
                BaseRender();
                checkCullingRoute();
                UpdateCulling();
                if (OBJModel != null) OBJModel.Render();
                RenderObjectModels();
                if (Collision != null) Collision.Render();
                if (div != null) div.Render();
                if (UIMapPos != null) UIMapPos.Render();
                RenderGhostAreas();
                if (Kayempee != null) Kayempee.Render();
                RenderGhostLines();
                RenderGizmo(false);
            }
            simpleOpenGlControl1.Refresh();
        }

        public void checkCullingRoute()
        {
            if (Kayempee == null) return;
            Kayempee.currentCullingRoutes.Clear();
            foreach (object sel in SelectedDots)
            {
                if (!(sel is Class.SimpleKMPs.CheckPoints.CheckpointGroup.CheckpointEntry)) continue;
                int clipID = (sel as Class.SimpleKMPs.CheckPoints.CheckpointGroup.CheckpointEntry).ClipID;
                foreach (int route in CullingPreview.GetClipRoutes(Kayempee, clipID))
                {
                    if (!Kayempee.currentCullingRoutes.Contains(route)) Kayempee.currentCullingRoutes.Add(route);
                }
            }
        }

        private void LoadObjectModels()
        {
            ObjModels = null;
            if (String.IsNullOrEmpty(Settings.ObjectModelsFolder) || !Directory.Exists(Settings.ObjectModelsFolder)) return;
            try
            {
                ObjModels = new ObjectModels(Settings.ObjectModelsFolder);
                toolStripStatusLabel1.Text = ObjModels.ModelCount + " OBJ models in " + Settings.ObjectModelsFolder +
                    (objlist.internalName.Count == 0 ? " (no ObjFlow.xml next to KMPExpander: the object names are unknown)" : "");
            }
            catch (Exception e)
            {
                toolStripStatusLabel1.Text = "Cannot read the object models folder: " + e.Message;
            }
        }

        // The objects that have a model in the object models folder, placed by their position, rotation and scale.
        private void RenderObjectModels()
        {
            if (ObjModels == null || Kayempee == null || !Kayempee.Objects.Visible || !objectModelsMenuItem.Checked) return;
            Gl.glDepthFunc(Gl.GL_LESS);
            Gl.glEnable(Gl.GL_ALPHA_TEST);
            Gl.glAlphaFunc(Gl.GL_GREATER, 0f);
            foreach (var o in Kayempee.Objects.Entries)
            {
                string name;
                if (!objlist.internalName.TryGetValue(o.ObjectID, out name)) continue;
                OBJWrapper model = ObjModels.Get(name);
                if (model == null) continue;
                Gl.glPushMatrix();
                Gl.glLoadIdentity();
                Gl.glMultMatrixf(vph.modelMatrix(o.Pos, new Vector3(o.RotationX, o.RotationY, o.RotationZ), new Vector3(o.ScaleX, o.ScaleY, o.ScaleZ)));
                model.RenderAsObject();
                Gl.glPopMatrix();
            }
            Gl.glBindTexture(Gl.GL_TEXTURE_2D, 0);
            Gl.glDisable(Gl.GL_ALPHA_TEST);
        }

        // What the game would cull now: for the selected checkpoints' clip IDs when any are selected, else for the
        // selected simulated kart (or the camera kart while the Kart Camera is on), else nothing.
        public void UpdateCulling()
        {
            List<int> routes = Kayempee != null ? Kayempee.currentCullingRoutes : null;
            KartSimulator.KartEntry kart = null;
            bool checkpointSelected = SelectedDots.Any(d => d is CheckPoints.CheckpointGroup.CheckpointEntry);
            if (Kayempee != null && Kayempee.KartSimulator.Visible && !checkpointSelected)
            {
                kart = Kayempee.KartSimulator.Entries.FirstOrDefault(k => k.Enabled && SelectedDots.Contains(k));
                if (kart == null && kartCameraButton != null && kartCameraButton.Checked) kart = Kayempee.KartSimulator.CameraKart;
            }
            Culling = (div != null || OBJModel != null || kart != null) ? CullingPreview.Compute(div, OBJModel, Kayempee, kart, routes,
                kart == null || Kayempee.KartSimulator.ClipIDCulling, kart == null || Kayempee.KartSimulator.CameraCulling) : null;
            UpdateStats();
        }

        // Top right of the map: triangles of the OBJ and areas of the div, in total and drawn, and the simulated karts.
        private void UpdateStats()
        {
            if (statsLabel == null) return;
            statsLabel.Visible = statsMenuItem.Checked;
            if (!statsLabel.Visible) return;
            List<string> lines = new List<string>();
            if (OBJModel != null)
            {
                int total, drawn;
                OBJModel.CountFaces(Culling, out total, out drawn);
                lines.Add(String.Format("Triangles: {0} drawn of {1} ({2:0.0}%)", drawn, total, total > 0 ? 100.0 * drawn / total : 0));
            }
            else lines.Add("Triangles: no OBJ loaded");
            if (div != null)
            {
                int total = 0, drawn = 0;
                foreach (var st in div.Streams)
                    foreach (var e in st.Entries)
                    {
                        total++;
                        if (Culling == null || Culling.HiddenCells == null || !Culling.RectHidden(e.minX, e.minZ, e.maxX, e.maxZ)) drawn++;
                    }
                lines.Add(String.Format("Div areas: {0} drawn of {1}", drawn, total));
            }
            if (Culling != null && Culling.Simulating && Kayempee != null)
            {
                int karts = 0, shown = 0;
                foreach (var k in Kayempee.KartSimulator.Entries)
                {
                    if (!k.Enabled || k == Culling.CameraKart) continue;
                    karts++;
                    if (!Culling.PointHidden(k.Pos.X, k.Pos.Z, 20)) shown++;
                }
                if (karts > 0) lines.Add(String.Format("Other karts: {0} drawn of {1}", shown, karts));
            }
            statsLabel.Text = String.Join("\n", lines);
            statsLabel.Left = simpleOpenGlControl1.Width - statsLabel.Width - 8;
            statsLabel.Top = 8;
        }

        public float GetViewportLevel()
        {
            return getViewportLevel();
        }

        // Drive Karts starts the karts from where they are, and then pauses and resumes them, keeping their state. Restart
        // puts them back on the start grid with a fresh state, standing, and leaves them driving or paused as they were.
        private void startDriving()
        {
            if (!PrepareDriving(false)) return;
            PauseDriving(false);
        }

        private void RestartKarts()
        {
            bool driving = kart_drive_timer.Enabled;
            if (!PrepareDriving(true)) return;
            PauseDriving(!driving);
        }

        private bool PrepareDriving(bool fromGrid)
        {
            if (Kayempee == null) return false;
            if (fromGrid && !Kayempee.KartSimulator.PlaceOnGrid(Kayempee, OBJModel))
            {
                toolStripStatusLabel1.Text = "No start grid object or start point";
                return false;
            }
            if (!Kayempee.KartSimulator.StartDriving(Kayempee, fromGrid))
            {
                toolStripStatusLabel1.Text = "No enemy points to drive along, or no kart enabled";
                return false;
            }
            Kayempee.KartSimulator.Visible = true;
            foreach (TreeNode node in treeView1.Nodes)
                if (node.Tag == Kayempee.KartSimulator) node.Checked = true;
            karts_started = true;
            return true;
        }

        private void PauseDriving(bool pause)
        {
            if (pause) kart_drive_timer.Stop();
            else
            {
                kart_drive_clock.Restart();
                kart_drive_frames = 0;
                kart_drive_timer.Start();
            }
            toolStripButtonDrive.Text = pause ? "Drive Karts" : "Pause Karts";
            Render();
        }

        private void StopDriving()
        {
            kart_drive_timer.Stop();
            karts_started = false;
            toolStripButtonDrive.Text = "Drive Karts";
            Render();
        }

        private void kart_drive_timer_Tick(object sender, EventArgs e)
        {
            if (Kayempee == null)
            {
                StopDriving();
                return;
            }
            if (!allowRender)
            {
                PauseDriving(true);
                return;
            }
            // At most 6 frames per tick, so that a slow frame does not make the karts jump.
            long due = kart_drive_clock.ElapsedMilliseconds * 60 / 1000;
            int steps = (int)Math.Min(6, due - kart_drive_frames);
            kart_drive_frames = Math.Max(kart_drive_frames + steps, due - 6);
            for (int i = 0; i < steps; i++)
            {
                if (Kayempee.KartSimulator.Step(Kayempee, OBJModel)) continue;
                StopDriving();
                toolStripStatusLabel1.Text = "Every kart reached the end of a route";
                break;
            }
            // Repainting the grid view every frame made driving lag, so the kart rows are repainted a few times per
            // second, and not while a cell is being edited.
            if (dataGridView1.DataSource == Kayempee.KartSimulator.GetEntries() && !dataGridView1.IsCurrentCellInEditMode &&
                kart_grid_refresh.ElapsedMilliseconds >= 250)
            {
                dataGridView1.Invalidate();
                kart_grid_refresh.Restart();
            }
            Render();
        }

        public void BaseRender(bool picking=false)
        {
            Gl.glMatrixMode(Gl.GL_PROJECTION);
            Gl.glLoadIdentity();
            Gl.glViewport(0, 0, simpleOpenGlControl1.Width, simpleOpenGlControl1.Height);
            RectangleF r = getDisplayRectangle(true);
            Gl.glOrtho(
            r.Left, r.Right,
            r.Bottom, r.Top,
            -8192 * 2, 8192 * 2);
            Gl.glMatrixMode(Gl.GL_MODELVIEW);
            Gl.glLoadIdentity();
            
            //Background Color
            if (picking) Gl.glClearColor(1f, 1f, 1f, 1f);
            else Gl.glClearColor(Settings.BackgroundColor.R / 255f, Settings.BackgroundColor.G / 255f, Settings.BackgroundColor.B / 255f, Settings.BackgroundColor.A);
            Gl.glClear(Gl.GL_COLOR_BUFFER_BIT | Gl.GL_DEPTH_BUFFER_BIT);

            Gl.glColor4f(1, 1, 1, 1);
            Gl.glEnable(Gl.GL_TEXTURE_2D);
            Gl.glBindTexture(Gl.GL_TEXTURE_2D, 0);
            Gl.glColor4f(1, 1, 1, 1);
            Gl.glDisable(Gl.GL_CULL_FACE);
            Gl.glEnable(Gl.GL_ALPHA_TEST);
            Gl.glEnable(Gl.GL_DEPTH_TEST);
            Gl.glEnable(Gl.GL_BLEND);
            Gl.glEnable(Gl.GL_POINT_SMOOTH);
            Gl.glBlendFunc(Gl.GL_SRC_ALPHA, Gl.GL_ONE_MINUS_SRC_ALPHA);
            Gl.glAlphaFunc(Gl.GL_ALWAYS, 0f);
            Gl.glLoadIdentity();            
        }

        private float getViewportLevel()
        {
            return 2 * Viewport / Math.Min(simpleOpenGlControl1.Width, simpleOpenGlControl1.Height);
        }

        private RectangleF getDisplayRectangle(bool AddViewportOffset)
        {
            float m = getViewportLevel();
            if (AddViewportOffset)
                return new RectangleF(-(m * simpleOpenGlControl1.Width) / 2f + ViewportOffset.X, -(m * simpleOpenGlControl1.Height) / 2f + ViewportOffset.Z, (m * simpleOpenGlControl1.Width), (m * simpleOpenGlControl1.Height));
            else
                return new RectangleF(-(m * simpleOpenGlControl1.Width) / 2f, -(m * simpleOpenGlControl1.Height) / 2f, (m * simpleOpenGlControl1.Width), (m * simpleOpenGlControl1.Height));
        }

        public Vector3 GetPosition(Point mouse_point)
        {
            RectangleF r = getDisplayRectangle(true);
            float dX = r.Width / simpleOpenGlControl1.Width;
            float dY = r.Height / simpleOpenGlControl1.Height;
            Vector3 Pos = new Vector3();
            Pos.X = (float)mouse_point.X * dX + r.Left;
            Pos.Z = (float)mouse_point.Y * dY + r.Top;
            return Pos;
        }

        public Point GetScreenPoint(Vector3 worldPos)
        {
            RectangleF r = getDisplayRectangle(true);
            float dX = r.Width / simpleOpenGlControl1.Width;
            float dY = r.Height / simpleOpenGlControl1.Height;
            Point p = new Point();
            p.X = (int)((worldPos.X - r.Left) / dX);
            p.Y = (int)((worldPos.Z - r.Top) / dY);
            return p;
        }

        public void populateTreeView()
        {
            treeView1.CheckBoxes = true;
            treeView1.Nodes.Clear();
            if (Kayempee != null) Kayempee.PopulateTreeView(treeView1);
            if (div != null) div.PopulateTreeView(treeView1);
            if (Collision != null) Collision.PopulateTreeView(treeView1);
        }

        #endregion

        private void toolStripButton1_Click(object sender, EventArgs e)
        {
            if (Kayempee == null) return;
            (sender as ToolStripButton).Text = "Press \"Esc\" to stop";
            (sender as ToolStripButton).Enabled = false;
            (sender as ToolStripButton).Invalidate();
            Application.DoEvents();
            CamePlayer cp = new CamePlayer(this);
            cp.playIntroSequence();
            (sender as ToolStripButton).Enabled = true;
            (sender as ToolStripButton).Text = "Play Intro Camera";
            Render();
        }
        public void runErr()
        {
            er.ShowDialog();
        }
        private void errorCheckerToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (Kayempee == null) return;
            er = new ErrorCheck(this);
            Thread t = new Thread(new ThreadStart(runErr));
            t.Start();
        }

        private void helpToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Help.ShowHelp(this, Application.StartupPath + @"\KMPExpander.chm");
        }

        private void viewPlaneComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (vph == null) return;
            SetMode3D((sender as ToolStripComboBox).SelectedIndex == 3);
            switch((sender as ToolStripComboBox).SelectedIndex)
            {
                case 1:
                    vph.mode = ViewPlaneHandler.PLANE_MODES.XY;
                    break;
                case 2:
                    vph.mode = ViewPlaneHandler.PLANE_MODES.ZY;
                    break;
                case 0:
                case 3:
                default:
                    vph.mode = ViewPlaneHandler.PLANE_MODES.XZ;
                    break;
            }
            Render();
        }

        private void transformKMPToolStripMenuItem_Click(object sender, EventArgs e)
        {
            FormTransform transform = new FormTransform();
            transform.ShowDialog();
            if (transform.Confirmed)
            {
                if (Kayempee != null) Kayempee.Transform(transform.KMPTranslation, transform.KMPScale);
                if (UIMapPos != null) UIMapPos.Transform(transform.KMPTranslation, transform.KMPScale);
                Render();
            }
        }
    }
}
