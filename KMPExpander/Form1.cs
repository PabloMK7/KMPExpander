using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using System.Reflection;
using KMPExpander.Class;
using KMPExpander.Class.SimpleKMPs;
using Tao.OpenGl;
using LibCTR.Collections;
using Extensions;
using System.Linq;
using System.Threading;

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
            InitializeComponent();
            try
            {
                Settings = VisualSettings.FromXML();
            }
            catch
            {
                Settings = new VisualSettings();
            }
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
            (sender as ToolStripMenuItem).Checked = (mode_3d ? false : true);
            mode_3d = (mode_3d ? false : true);
            Render();
        }

        private void importOBJToolStripMenuItem_Click(object sender, EventArgs e)
        {
            obj_manager.ShowDialog();
        }

        private void toggleViewToolStripMenuItem_Click(object sender, EventArgs e)
        {
            UIMapPos.LocalMap.Visible = (UIMapPos.LocalMap.Visible ? false : true);
            Render();
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
                UIMapPos.LocalMap.LoadImage(LocalMap);
                unloadToolStripMenuItem.Enabled = true;
                Render();
            }
        }

        private void toolStripMenuItemSaveUIMap_Click(object sender, EventArgs e)
        {
            File.WriteAllBytes(uimappos_path, UIMapPos.Write());
        }

        private void unloadToolStripMenuItem_Click(object sender, EventArgs e)
        {
            UIMapPos.LocalMap.UnloadImage();
            (sender as ToolStripMenuItem).Enabled = false;
            Render();
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

        public SimpleKMP getKayEmPee()
        {
            return Kayempee;
        }

        private void loadKMP()
        {
            Kayempee = new SimpleKMP(File.ReadAllBytes(kmp_path));
            Text = "KMP Expander (" + kmp_path + ")";
            populateTreeView();
            dataGridView1.DataSource = null;
            closeKMPStripMenuItem6.Enabled = true;
            saveAsToolStripMenuItem.Enabled = true;
            saveToolStripMenuItem.Enabled = true;
            exportXMLToolStripMenuItem.Enabled = true;
        }

        private void closeKMPStripMenuItem6_Click(object sender, EventArgs e)
        {
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
        }

        private void closeUIMapStripMenuItem7_Click(object sender, EventArgs e)
        {
            UIMapPos.LocalMap.UnloadImage();
            UIMapPos = null;
            loadToolStripMenuItem.Enabled = false;
            toolStripMenuItemSaveUIMap.Enabled = false;
            unloadToolStripMenuItem.Enabled = false;
            (sender as ToolStripMenuItem).Enabled = false;
            Render();
        }

        private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            MessageBox.Show("KMP Expander v4 Made by Ermelber & PabloMK7");
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
                    populateTreeView();
                    dataGridView1.DataSource = null;
                    saveAsToolStripMenuItem.Enabled = true;
                    exportXMLToolStripMenuItem.Enabled = true;
                    closeKMPStripMenuItem6.Enabled = true;
                    Render();
                }
                catch
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
                            treeView1.SelectedNode.Tag.GetType().Equals(typeof(StageInformation)))
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
                propertyGrid1.SelectedObject = null;
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
                            treeView1.SelectedNode.Tag.GetType().Equals(typeof(Camera)))
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
            string name = dataGridView1.Columns[e.ColumnIndex].Name;
            if (name == "WideTurn" || name == "NormalTurn" || name == "SharpTurn")
            {
                dataGridView1.EndEdit();
                Render();
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
        private float found_height = 0;

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
                doScroll(true);

            } else if (e.KeyChar == '-')
            {
                doScroll(false);
            }
        }

        public void simpleOpenGlControl1_MouseWheel(object sender, MouseEventArgs e)
        {
            bool mode = false;
            if (e.Delta >= 0) mode = true;
            doScroll(mode);
        }

        public void doScroll(bool zoomIn)
        {
            float Diff = (maximum_viewport - minimum_viewport) / 15f;
            if (zoomIn) Viewport -= Diff;
            else Viewport += Diff;

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
            if (!opengl_initialized) return;
            
            byte[] pic = new byte[4] { 0xFF, 0xFF, 0xFF, 0xFF };

            BaseRender(true);
            Gl.glLoadIdentity();
            Gl.glDisable(Gl.GL_POLYGON_SMOOTH);
            Gl.glDisable(Gl.GL_POINT_SMOOTH);

            if (getY && (OBJModel!=null))
            {
                OBJModel.Render(true);
                Gl.glReadPixels(mousepoint.X, simpleOpenGlControl1.Height - mousepoint.Y, 1, 1, Gl.GL_BGRA, Gl.GL_UNSIGNED_BYTE, pic);

                if (pic[0] == 0xFF && pic[1] == 0xFF && pic[2] == 0xFF && pic[3] == 0xFF)
                {
                    return;
                }
                int face_id = ModelPicking.FromRgb(pic[2], pic[1], pic[0]);
                found_height = OBJModel.GetHeightValue(face_id, GetPosition(mousepoint));
            }
            else
            {
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
                Gl.glReadPixels(mousepoint.X, simpleOpenGlControl1.Height - mousepoint.Y, 1, 1, Gl.GL_BGRA, Gl.GL_UNSIGNED_BYTE, pic);

                if (pic[0] == 0xFF && pic[1] == 0xFF && pic[2] == 0xFF && pic[3] == 0xFF)
                {
                    PickingInfo.Section = Sections.None;
                    PickingInfo.PointID = PointID.None;
                    PickingInfo.GroupID = 0;
                    PickingInfo.EntryID = 0;
                }
                else
                    PickingInfo = SectionPicking.FromRgb(pic[2], pic[1], pic[0]);
            }
            Gl.glClear(Gl.GL_COLOR_BUFFER_BIT | Gl.GL_DEPTH_BUFFER_BIT);
            Render();
            return;
        }

        private void simpleOpenGlControl1_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left && e.Button != MouseButtons.Right) return;

            if (pencil_mode && (lastSelectedGroup != null))
            {
                if (vph.mode != ViewPlaneHandler.PLANE_MODES.XZ && (lastSelectedGroup.GetType() == typeof(CheckPoints.CheckpointGroup) || lastSelectedGroup.GetType() == typeof(Area))) return;
                Vector3 Position = GetPosition(e.Location);
                AddingPencil = AddEntry();
                if ((OBJModel != null) && vph.mode == ViewPlaneHandler.PLANE_MODES.XZ && (e.Button == MouseButtons.Right))
                {
                    PickPoint(e.Location, true);
                    Position.Y = found_height;
                }
                else Position.Y = 0f;
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
            if (PickingInfo.Section!=Sections.None && PickingInfo.Section!=Sections.LocalMap)
                SelectedDots.Add(Kayempee.GetPoint(PickingInfo));
        }

        private void simpleOpenGlControl1_MouseMove(object sender, MouseEventArgs e)
        {
            if ((e.Button == MouseButtons.Left || (e.Button == MouseButtons.Right)))
            {
                Vector3 position = GetPosition(e.Location);
                if (!start_move) return;
                start_move = true;
                if (PickingInfo.Section == Sections.None) return;

                if (PickingInfo.Section == Sections.LocalMap && vph.mode == ViewPlaneHandler.PLANE_MODES.XZ) UIMapPos.MovePoint(PickingInfo, position);
                else Kayempee.MovePoint(PickingInfo, position);
                Render();
            }
        }

        private void simpleOpenGlControl1_MouseUp(object sender, MouseEventArgs e)
        {
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

            if (e.Button == MouseButtons.Right && vph.mode == ViewPlaneHandler.PLANE_MODES.XZ)
            {
                PickPoint(e.Location, true);
                if (OBJModel!=null) Kayempee.MovePointY(PickingInfo, found_height);
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

            if (mode_3d)
            {
                Base3DRender();
                Gl.glPushMatrix();
                if (OBJModel != null) OBJModel.Render();
                Gl.glPopMatrix();
            }
            else
            {
                BaseRender();
                checkCullingRoute();
                if (OBJModel != null) OBJModel.Render();
                if (div != null) div.Render();
                if (UIMapPos != null) UIMapPos.Render();
                if (Kayempee != null) Kayempee.Render();
            }
            simpleOpenGlControl1.Refresh();
        }

        private Vector3 cameraDestination = new Vector3();
        public Vector3 CameraDestination { get { return cameraDestination; } set { cameraDestination = value; } }
        private Vector3 cameraRotation = new Vector3();
        public Vector3 CameraRotation { get { return cameraRotation; } set { cameraRotation = value; UpdateCamRotSinCos(); } }
        private float camRotXSin;
        private float camRotXCos;
        private float camRotYSin;
        private float camRotYCos;

        private void UpdateCamRotSinCos()
        {
            camRotXSin = (float)Math.Sin(CameraRotation.X * Math.PI / 180f);
            camRotXCos = (float)Math.Cos(CameraRotation.X * Math.PI / 180f);
            camRotYSin = (float)Math.Sin(CameraRotation.Y * Math.PI / 180f);
            camRotYCos = (float)Math.Cos(CameraRotation.Y * Math.PI / 180f);
        }

        public void Base3DRender(bool picking=false)
        {
            Gl.glMatrixMode(Gl.GL_PROJECTION);
            Gl.glLoadIdentity();
            Gl.glViewport(0, 0, simpleOpenGlControl1.Width, simpleOpenGlControl1.Height);
            Glu.gluPerspective(30, simpleOpenGlControl1.Width / simpleOpenGlControl1.Height, 4f, 25600);

            Gl.glMatrixMode(Gl.GL_MODELVIEW);
            Gl.glLoadIdentity();
            Gl.glBindTexture(Gl.GL_TEXTURE_2D, 0);
            Gl.glColor3f(1.0f, 1.0f, 1.0f);
            if (picking) Gl.glClearColor(1f, 1f, 1f, 1f);
            else Gl.glClearColor(Settings.BackgroundColor.R / 255f, Settings.BackgroundColor.G / 255f, Settings.BackgroundColor.B / 255f, Settings.BackgroundColor.A);

            Gl.glClearStencil(0);
            Gl.glClear(Gl.GL_COLOR_BUFFER_BIT | Gl.GL_DEPTH_BUFFER_BIT | Gl.GL_STENCIL_BUFFER_BIT);

            /*float camx, camy, camz;

            camx = (250f * camRotXCos * camRotYCos) + CameraDestination.X;
            camy = (250f * camRotYSin) + CameraDestination.Y;
            camz = (250f * camRotXSin * camRotYCos) + CameraDestination.Z;*/

            Glu.gluLookAt(0, 1000 ,0, 0, 0, 0, 0, -1, 0);

            Gl.glLoadIdentity();

        }

        public void checkCullingRoute()
        {
            if (Kayempee == null) return;
            Kayempee.currentCullingRoutes.Clear();
            foreach (object sel in SelectedDots)
            {
                if (!(sel is Class.SimpleKMPs.CheckPoints.CheckpointGroup.CheckpointEntry)) continue;
                int clipID = (sel as Class.SimpleKMPs.CheckPoints.CheckpointGroup.CheckpointEntry).ClipID;
                List<Class.SimpleKMPs.Objects.ObjectEntry> objlist = Kayempee.Objects.Entries;
                foreach (var obj in objlist)
                {
                    if (obj.ObjectID != 0x1E || obj.Settings1 != clipID || Kayempee.Routes.Entries.ElementAtOrDefault(obj.RouteID) == null) continue;
                    if (Kayempee.Routes.Entries[obj.RouteID].Entries.Count == 4)
                    {
                        Kayempee.currentCullingRoutes.Add(obj.RouteID);
                    }
                }
                
            }
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

        private RectangleF getDisplayRectangle(bool AddViewportOffset)
        {
            float x = Viewport / simpleOpenGlControl1.Width;
            x *= 2;
            float y = Viewport / simpleOpenGlControl1.Height;
            y *= 2;
            float m = (x > y) ? x : y;
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

        public void populateTreeView()
        {
            treeView1.CheckBoxes = true;
            treeView1.Nodes.Clear();
            if (Kayempee != null) Kayempee.PopulateTreeView(treeView1);
            if (div != null) div.PopulateTreeView(treeView1);
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
            switch((sender as ToolStripComboBox).SelectedIndex)
            {
                case 1:
                    vph.mode = ViewPlaneHandler.PLANE_MODES.XY;
                    break;
                case 2:
                    vph.mode = ViewPlaneHandler.PLANE_MODES.ZY;
                    break;
                case 0:
                default:
                    vph.mode = ViewPlaneHandler.PLANE_MODES.XZ;
                    break;
            }
            Render();
        }
    }
}
