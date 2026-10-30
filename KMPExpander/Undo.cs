using KMPExpander.Class;
using KMPExpander.Class.SimpleKMPs;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;

namespace KMPExpander
{
    // Undo and redo (Ctrl+Z; Ctrl+Y or Ctrl+Shift+Z) of every change to the KMP. A few times per second the KMP is written
    // out and compared with the last state kept; when it changed, and no mouse button is held and no cell is being edited
    // (so that a drag is one step), the last state goes onto the undo list. Undoing loads a kept state back, with the
    // sections' and groups' visibility, the kart simulator and the selected tree node as they are.
    public partial class Form1
    {
        private const int UndoLimit = 100;
        private List<byte[]> undoStates = new List<byte[]>(), redoStates = new List<byte[]>();
        private byte[] undoCurrent;
        private Timer undoTimer;

        private void InitializeUndo()
        {
            undoTimer = new Timer() { Interval = 300 };
            undoTimer.Tick += (s, e) => CommitUndo();
            undoTimer.Start();
        }

        // A KMP was opened, imported or closed: both lists start over.
        private void ResetUndo()
        {
            undoStates.Clear();
            redoStates.Clear();
            undoCurrent = null;
            CommitUndo();
        }

        private void CommitUndo()
        {
            if (Kayempee == null || Control.MouseButtons != MouseButtons.None || dataGridView1.IsCurrentCellInEditMode) return;
            byte[] now;
            try { now = Kayempee.Write(); }
            catch { return; }
            if (undoCurrent != null && now.SequenceEqual(undoCurrent)) return;
            if (undoCurrent != null)
            {
                undoStates.Add(undoCurrent);
                if (undoStates.Count > UndoLimit) undoStates.RemoveAt(0);
                redoStates.Clear();
            }
            undoCurrent = now;
        }

        private void Undo(bool redo)
        {
            CommitUndo();
            List<byte[]> from = redo ? redoStates : undoStates, to = redo ? undoStates : redoStates;
            if (Kayempee == null || undoCurrent == null || from.Count == 0)
            {
                toolStripStatusLabel1.Text = redo ? "Nothing to redo" : "Nothing to undo";
                return;
            }
            to.Add(undoCurrent);
            undoCurrent = from[from.Count - 1];
            from.RemoveAt(from.Count - 1);
            RestoreKMP(undoCurrent);
            toolStripStatusLabel1.Text = String.Format("{0} ({1} undo and {2} redo steps left)", redo ? "Redo" : "Undo", undoStates.Count, redoStates.Count);
        }

        private void RestoreKMP(byte[] data)
        {
            SimpleKMP old = Kayempee;
            Kayempee = new SimpleKMP(data);
            Kayempee.KartSimulator = old.KartSimulator;
            Kayempee.currentCullingRoutes = old.currentCullingRoutes;
            foreach (FieldInfo field in typeof(SimpleKMP).GetFields())
            {
                if (!typeof(ISectionBase).IsAssignableFrom(field.FieldType) || field.FieldType == typeof(KartSimulator)) continue;
                ISectionBase was = (ISectionBase)field.GetValue(old), now = (ISectionBase)field.GetValue(Kayempee);
                if (was == null || now == null) continue;
                now.SetVisibility(was.GetVisibility());
                IList oldGroups = was.GetEntries() as IList, newGroups = now.GetEntries() as IList;
                if (oldGroups == null || newGroups == null) continue;
                for (int i = 0; i < Math.Min(oldGroups.Count, newGroups.Count); i++)
                    if (oldGroups[i] is ISectionBase && newGroups[i] is ISectionBase)
                        ((ISectionBase)newGroups[i]).SetVisibility(((ISectionBase)oldGroups[i]).GetVisibility());
            }

            // The same tree node is selected again, which refreshes the grid and the property grid.
            List<int> path = new List<int>();
            for (TreeNode n = treeView1.SelectedNode; n != null; n = n.Parent) path.Insert(0, n.Index);
            SelectedDots.Clear();
            lastSelectedGroup = null;
            lastSelectedSection = null;
            dataGridView1.DataSource = null;
            propertyGrid1.SelectedObject = null;
            populateTreeView();
            TreeNodeCollection nodes = treeView1.Nodes;
            TreeNode node = null;
            foreach (int i in path)
            {
                if (i >= nodes.Count) break;
                node = nodes[i];
                nodes = node.Nodes;
            }
            if (node != null) treeView1.SelectedNode = node;
            Render();
        }

        // Text being edited (a grid cell, the property grid) keeps its own Ctrl+Z. Escape pauses the driving karts,
        // wherever the focus is.
        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == Keys.Escape && kart_drive_timer != null && kart_drive_timer.Enabled)
            {
                PauseDriving(true);
                return true;
            }
            bool undo = keyData == (Keys.Control | Keys.Z);
            bool redo = keyData == (Keys.Control | Keys.Y) || keyData == (Keys.Control | Keys.Shift | Keys.Z);
            if (undo || redo)
            {
                Control focused = this;
                while (focused is ContainerControl && ((ContainerControl)focused).ActiveControl != null) focused = ((ContainerControl)focused).ActiveControl;
                if (!(focused is TextBoxBase) && !dataGridView1.IsCurrentCellInEditMode)
                {
                    Undo(redo);
                    return true;
                }
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }
    }
}
