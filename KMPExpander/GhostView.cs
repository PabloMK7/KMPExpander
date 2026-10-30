using KMPExpander.Class;
using KMPExpander.Class.SimpleKMPs;
using Extensions;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using Tao.OpenGl;

namespace KMPExpander
{
    // The ghost checkpoints in the 2D top view (GhostCheckpoints), for the selected checkpoint, with a legend in the
    // bottom left corner when it has any (checkpoints named as in the tree: group, then checkpoint in the group):
    // - in purple, where karts at other checkpoints get the selected one as a ghost checkpoint, and the edges of their
    //   quads where driving out gives it as a respawn glitch. This only needs the selected checkpoint's own quads
    //   (GhostCheckpoints.ComputeFor), so it follows the checkpoint while it is dragged;
    // - in orange (a respawn glitch) or yellow (no effect), where a kart whose current checkpoint is the selected one
    //   gets a ghost checkpoint, and in red the edges of its quad where driving out gives a respawn glitch. This needs
    //   the search over the whole course, which runs on a worker thread when the checkpoints or respawn points changed
    //   and no mouse button is held.
    // "Ghost checkpoints of every checkpoint" shows the ghost areas of all checkpoints instead. The lines are drawn
    // over the KMP.
    public partial class Form1
    {
        private ToolStripMenuItem ghostMenuItem, ghostAllMenuItem;
        private GhostCheckpoints ghosts, ghostsOf, ghostsShown;   // ghostsShown: ghosts when up to date, for the lines
        private string ghostSignature, ghostsSignature, ghostsOfRequested;
        private int ghostsOfIndex = -1;                       // the checkpoint ghostsOf is for
        private bool ghostsOfBusy;
        private Tuple<string, int, CheckpointSearch> ghostsOfNext;
        private volatile int ghostVersion;
        private Panel ghostLegend;
        private List<LegendItem> ghostLegendItems = new List<LegendItem>();

        private static readonly float[] GhostOfColor = { 0.75f, 0.1f, 0.85f }, GhostOfHarmlessColor = { 0.8f, 0.6f, 0.9f },
            GhostFromColor = { 1f, 0.45f, 0f }, GhostFromHarmlessColor = { 1f, 0.85f, 0f }, GlitchLineColor = { 0.9f, 0f, 0f };

        private class LegendItem
        {
            public float[] Colour;      // null: text only
            public bool Line, Bold;
            public string Text;
        }

        private void InitializeGhosts(ToolStripDropDownButton viewButton)
        {
            ghostMenuItem = new ToolStripMenuItem("Ghost checkpoints") { CheckOnClick = true };
            ghostMenuItem.ToolTipText = "Top view: the ghost checkpoints of the selected checkpoint";
            ghostMenuItem.CheckedChanged += (s, e) => Render();
            ghostAllMenuItem = new ToolStripMenuItem("Ghost checkpoints of all checkpoints") { CheckOnClick = true };
            ghostAllMenuItem.ToolTipText = "Top view: the ghost areas of every checkpoint, not only of the selected one";
            ghostAllMenuItem.CheckedChanged += (s, e) => Render();
            viewButton.DropDownItems.Add(new ToolStripSeparator());
            viewButton.DropDownItems.Add(ghostMenuItem);
            viewButton.DropDownItems.Add(ghostAllMenuItem);
            ghostLegend = new Panel() { BackColor = Color.FromArgb(40, 40, 40), Visible = false };
            ghostLegend.Paint += PaintGhostLegend;
            simpleOpenGlControl1.Controls.Add(ghostLegend);
        }

        // Starts the search over the whole course when the checkpoints changed.
        private void UpdateGhosts(string signature)
        {
            if (Control.MouseButtons != MouseButtons.None || signature == ghostSignature) return;
            ghostSignature = signature;
            int version = ++ghostVersion;
            CheckpointSearch search = new CheckpointSearch(Kayempee);
            Task.Run(() =>
            {
                GhostCheckpoints result = null;
                try { result = GhostCheckpoints.Compute(search, () => version != ghostVersion); }
                catch { }
                if (version != ghostVersion) return;
                BeginInvoke((Action)(() =>
                {
                    if (version != ghostVersion) return;
                    ghosts = result;
                    ghostsSignature = signature;
                    Render();
                }));
            });
        }

        // The index of the first selected checkpoint across the whole CKPT list, or -1.
        private int SelectedCheckpointIndex()
        {
            int index = 0;
            foreach (var g in Kayempee.CheckPoints.Entries)
                foreach (var e in g.Entries)
                {
                    if (SelectedDots.Contains(e)) return index;
                    index++;
                }
            return -1;
        }

        private bool GhostsShown()
        {
            return ghostMenuItem != null && ghostMenuItem.Checked && Kayempee != null && vph.mode == ViewPlaneHandler.PLANE_MODES.XZ &&
                Kayempee.CheckPoints.Visible && Kayempee.CheckPoints.Entries.Count > 0;
        }

        // The areas, under the KMP; also brings the results and the legend up to date.
        private void RenderGhostAreas()
        {
            ghostLegendItems.Clear();
            ghostsShown = null;
            if (GhostsShown())
            {
                bool all = ghostAllMenuItem.Checked;
                int selected = SelectedCheckpointIndex();
                if (all || selected >= 0)
                {
                    string signature = GhostCheckpoints.Signature(Kayempee);
                    UpdateGhosts(signature);
                    GhostCheckpoints full = ghostsShown = ghostsSignature == signature ? ghosts : null;
                    if (all) RenderAllGhosts(full);
                    else RenderSelectedGhosts(full, signature, selected);
                }
            }
            UpdateGhostLegend();
        }

        private void RenderAllGhosts(GhostCheckpoints full)
        {
            if (full == null || full.Faces.Count == 0) return;
            BeginGhostAreas();
            bool glitches = false, harmless = false;
            foreach (var face in full.Faces)
            {
                bool glitch = face.Ghosts.Any(p => full.IsGlitch(p.Key, p.Value));
                if (glitch) glitches = true; else harmless = true;
                DrawGhostFace(face, glitch ? GhostFromColor : GhostFromHarmlessColor, glitch);
            }
            EndGhostAreas();
            ghostLegendItems.Add(new LegendItem() { Bold = true, Text = "Ghost checkpoints of all checkpoints" });
            if (glitches) ghostLegendItems.Add(new LegendItem() { Colour = GhostFromColor, Text = "Area of harmful ghosts (wrong respawn point)" });
            if (harmless) ghostLegendItems.Add(new LegendItem() { Colour = GhostFromHarmlessColor, Text = "Area of unharmful ghosts (no effect)" });
            if (full.Borders.Any(b => full.IsGlitch(b.Start, b.Ghost)))
                ghostLegendItems.Add(new LegendItem() { Colour = GlitchLineColor, Line = true, Text = "Border where a harmful ghost checkpoint is accessible" });
        }

        // The legend only appears when the selected checkpoint has ghosts one way or the other.
        private void RenderSelectedGhosts(GhostCheckpoints full, string signature, int selected)
        {
            RequestGhostsOf(signature + "|" + selected, selected);
            List<LegendItem> items = new List<LegendItem>();
            BeginGhostAreas();

            // Karts coming from this checkpoint get ghosts (the search over the whole course).
            if (full != null)
            {
                SortedSet<int> glitches = new SortedSet<int>(), harmless = new SortedSet<int>();
                foreach (var face in full.Faces)
                {
                    int ghost;
                    if (!face.Ghosts.TryGetValue(selected, out ghost)) continue;
                    bool glitch = full.IsGlitch(selected, ghost);
                    (glitch ? glitches : harmless).Add(ghost);
                    DrawGhostFace(face, glitch ? GhostFromColor : GhostFromHarmlessColor, glitch);
                }
                if (glitches.Count > 0)
                    items.Add(new LegendItem() { Colour = GhostFromColor, Text = "Area of harmfum ghost " + CheckpointNames(glitches) + " seen by selected checkpoint (wrong respawn point)" });
                if (harmless.Count > 0)
                    items.Add(new LegendItem() { Colour = GhostFromHarmlessColor, Text = "Area of unharmful ghost " + CheckpointNames(harmless) + " seen by selected checkpoint (no effect)" });
                if (full.Borders.Any(b => b.Start == selected && full.IsGlitch(b.Start, b.Ghost)))
                    items.Add(new LegendItem() { Colour = GlitchLineColor, Line = true, Text = "Border where a harmful ghost checkpoint is accessible" });
            }

            // This checkpoint is the ghost of others (follows a drag).
            if (ghostsOfIndex == selected && ghostsOf != null && ghostsOf.Faces.Count > 0)
            {
                SortedSet<int> glitchStarts = new SortedSet<int>(), harmlessStarts = new SortedSet<int>();
                foreach (var face in ghostsOf.Faces)
                {
                    bool glitch = false;
                    foreach (int start in face.Ghosts.Keys)
                        if (ghostsOf.IsGlitch(start, selected)) { glitch = true; glitchStarts.Add(start); }
                        else harmlessStarts.Add(start);
                    DrawGhostFace(face, glitch ? GhostOfColor : GhostOfHarmlessColor, glitch);
                }
                if (glitchStarts.Count > 0)
                    items.Add(new LegendItem() { Colour = GhostOfColor, Text = Capitalized("Harmful ghost area produced by the current selected checkpoint (wrong respawn point)") });
                if (harmlessStarts.Count > 0)
                    items.Add(new LegendItem() { Colour = GhostOfHarmlessColor, Text = Capitalized("Unharmful ghost area produced by the current selected checkpoint (no effect)") });
                var drivenOut = ghostsOf.Borders.Where(b => ghostsOf.IsGlitch(b.Start, b.Ghost)).Select(b => b.Start).Distinct().ToList();
                if (drivenOut.Count > 0)
                    items.Add(new LegendItem() { Colour = GhostOfColor, Line = true, Text = "Border where the selected checkpoint harmful ghost area is accessible" });
                if (glitchStarts.Count > 0)
                    items.Add(new LegendItem() { Text = "Moving this checkpoint changes the purple areas" });
            }
            EndGhostAreas();
            if (items.Count == 0) return;
            ghostLegendItems.Add(new LegendItem() { Bold = true, Text = "Ghost checkpoints of " + CheckpointName(selected) });
            ghostLegendItems.AddRange(items);
            if (full == null)
                ghostLegendItems.Add(new LegendItem() { Text = Control.MouseButtons != MouseButtons.None ?
                    "The orange and yellow areas are updated when the drag ends" : "Searching for the orange and yellow areas..." });
        }

        private static string Capitalized(string text)
        {
            return text.Length == 0 ? text : Char.ToUpper(text[0]) + text.Substring(1);
        }

        // "group 3 checkpoint 5", from the index across the whole CKPT list.
        private string CheckpointName(int index)
        {
            int group = 0;
            foreach (var g in Kayempee.CheckPoints.Entries)
            {
                if (index < g.Entries.Count) return "group " + group + " checkpoint " + index;
                index -= g.Entries.Count;
                group++;
            }
            return "checkpoint " + index;
        }

        // "group 3 checkpoints 5-9 and 12, group 4 checkpoint 0".
        private string CheckpointNames(IEnumerable<int> indexes)
        {
            List<string> parts = new List<string>();
            int start = 0, group = 0;
            HashSet<int> set = new HashSet<int>(indexes);
            foreach (var g in Kayempee.CheckPoints.Entries)
            {
                List<int> local = new List<int>();
                for (int i = 0; i < g.Entries.Count; i++) if (set.Contains(start + i)) local.Add(i);
                if (local.Count > 0) parts.Add("group " + group + (local.Count == 1 ? " checkpoint " : " checkpoints ") + Ranges(local));
                start += g.Entries.Count;
                group++;
            }
            return "[" + String.Join(", ", parts) + "]";
        }

        // GhostCheckpoints.ComputeFor on a worker thread, for the latest request only: while one runs, newer requests
        // replace each other and the last one runs next, so that the area follows a drag without slowing it down.
        private void RequestGhostsOf(string key, int selected)
        {
            if (key == ghostsOfRequested) return;
            ghostsOfRequested = key;
            ghostsOfNext = Tuple.Create(key, selected, new CheckpointSearch(Kayempee));
            if (ghostsOfBusy) return;
            ghostsOfBusy = true;
            RunGhostsOf();
        }

        private void RunGhostsOf()
        {
            var request = ghostsOfNext;
            ghostsOfNext = null;
            Task.Run(() =>
            {
                GhostCheckpoints result = null;
                try { result = GhostCheckpoints.ComputeFor(request.Item3, request.Item2); }
                catch { }
                BeginInvoke((Action)(() =>
                {
                    ghostsOf = result;
                    ghostsOfIndex = request.Item2;
                    if (ghostsOfNext != null) RunGhostsOf();
                    else ghostsOfBusy = false;
                    Render();
                }));
            });
        }

        // The lines, over the KMP, with a white edge so that they stand out from the checkpoints.
        private void RenderGhostLines()
        {
            if (!GhostsShown()) return;
            bool all = ghostAllMenuItem.Checked;
            int selected = all ? -1 : SelectedCheckpointIndex();
            if (!all && selected < 0) return;
            GhostCheckpoints full = ghostsShown;
            List<GhostCheckpoints.Border> red = full == null ? new List<GhostCheckpoints.Border>() :
                full.Borders.Where(b => (all || b.Start == selected) && full.IsGlitch(b.Start, b.Ghost)).ToList();
            List<GhostCheckpoints.Border> purple = all || ghostsOf == null || ghostsOfIndex != selected ? new List<GhostCheckpoints.Border>() :
                ghostsOf.Borders.Where(b => ghostsOf.IsGlitch(b.Start, b.Ghost)).ToList();
            Gl.glBindTexture(Gl.GL_TEXTURE_2D, 0);
            Gl.glDisable(Gl.GL_TEXTURE_2D);
            Gl.glDisable(Gl.GL_LIGHTING);
            Gl.glLoadIdentity();
            Gl.glDepthFunc(Gl.GL_ALWAYS);
            float width = Math.Max(5f, Settings.LineWidth * 3);
            for (int pass = 0; pass < 2; pass++)
            {
                Gl.glLineWidth(pass == 0 ? width + 4 : width);
                foreach (var set in new[] { new { Lines = purple, Colour = GhostOfColor }, new { Lines = red, Colour = GlitchLineColor } })
                {
                    if (pass == 0) Gl.glColor4f(1f, 1f, 1f, 1f);
                    else Gl.glColor4f(set.Colour[0], set.Colour[1], set.Colour[2], 1f);
                    Gl.glBegin(Gl.GL_LINES);
                    foreach (var b in set.Lines)
                    {
                        vph.drawXZ(b.X0, b.Z0, 0);
                        vph.drawXZ(b.X1, b.Z1, 0);
                    }
                    Gl.glEnd();
                }
            }
            Gl.glLineWidth(Settings.LineWidth);
            Gl.glEnable(Gl.GL_TEXTURE_2D);
        }

        // The model drawn before may leave its last texture bound and lighting on, which would tint the areas grey.
        private void BeginGhostAreas()
        {
            Gl.glBindTexture(Gl.GL_TEXTURE_2D, 0);
            Gl.glDisable(Gl.GL_TEXTURE_2D);
            Gl.glDisable(Gl.GL_LIGHTING);
            Gl.glLoadIdentity();
            Gl.glDepthFunc(Gl.GL_ALWAYS);
            Gl.glEnable(Gl.GL_BLEND);
            Gl.glBlendFunc(Gl.GL_SRC_ALPHA, Gl.GL_ONE_MINUS_SRC_ALPHA);
        }

        private void EndGhostAreas()
        {
            Gl.glEnable(Gl.GL_TEXTURE_2D);
        }

        private void DrawGhostFace(GhostCheckpoints.Face face, float[] colour, bool glitch)
        {
            Gl.glColor4f(colour[0], colour[1], colour[2], glitch ? 0.6f : 0.45f);
            Gl.glBegin(Gl.GL_POLYGON);
            for (int i = 0; i < face.X.Length; i++) vph.drawXZ((float)face.X[i], (float)face.Z[i], 0);
            Gl.glEnd();
        }

        // "3, 79-89, 107".
        private static string Ranges(IEnumerable<int> values)
        {
            List<string> parts = new List<string>();
            int[] v = values.OrderBy(x => x).ToArray();
            for (int i = 0; i < v.Length; i++)
            {
                int j = i;
                while (j + 1 < v.Length && v[j + 1] == v[j] + 1) j++;
                parts.Add(j > i ? v[i] + "-" + v[j] : v[i].ToString());
                i = j;
            }
            return String.Join(", ", parts);
        }

        private void UpdateGhostLegend()
        {
            if (ghostLegend == null) return;
            ghostLegend.Visible = ghostLegendItems.Count > 0;
            if (!ghostLegend.Visible) return;
            using (Graphics gr = ghostLegend.CreateGraphics())
            {
                float w = 0;
                foreach (var item in ghostLegendItems)
                    using (Font f = LegendFont(item)) w = Math.Max(w, gr.MeasureString(item.Text, f).Width);
                ghostLegend.Size = new Size((int)w + 34, ghostLegendItems.Count * 18 + 8);
            }
            ghostLegend.Left = 8;
            ghostLegend.Top = simpleOpenGlControl1.Height - ghostLegend.Height - 8;
            ghostLegend.Invalidate();
        }

        private Font LegendFont(LegendItem item)
        {
            return new Font(Font.FontFamily, 8.25f, item.Bold ? FontStyle.Bold : FontStyle.Regular);
        }

        private void PaintGhostLegend(object sender, PaintEventArgs e)
        {
            for (int i = 0; i < ghostLegendItems.Count; i++)
            {
                var item = ghostLegendItems[i];
                int y = 4 + i * 18;
                if (item.Colour != null)
                {
                    Color c = Color.FromArgb((int)(item.Colour[0] * 255), (int)(item.Colour[1] * 255), (int)(item.Colour[2] * 255));
                    using (Brush b = new SolidBrush(c))
                    {
                        if (item.Line)
                        {
                            e.Graphics.FillRectangle(Brushes.White, 4, y + 5, 20, 7);
                            e.Graphics.FillRectangle(b, 4, y + 6, 20, 5);
                        }
                        else e.Graphics.FillRectangle(b, 6, y + 2, 16, 13);
                    }
                }
                using (Font f = LegendFont(item))
                    e.Graphics.DrawString(item.Text, f, Brushes.White, item.Colour != null || !item.Bold ? 28 : 4, y + 1);
            }
        }
    }
}
