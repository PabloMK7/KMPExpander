using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using KMPExpander.Class;
using KMPExpander.Class.SimpleKMPs;

namespace KMPExpander
{
    public partial class ErrorCheck : Form
    {
        Form1 ParentF = null;
        SimpleKMP kmp = null;
        List<string> errors;
        List<string> warnings;
        List<string> notes;
        public ErrorCheck(Form1 Parent)
        {
            InitializeComponent();
            this.ParentF = Parent;
            errors = new List<string>();
            warnings = new List<string>();
            notes = new List<string>();
            ErrorRefresh();
        }
        public void ErrorRefresh()
        {
            kmp = ParentF.Kayempee;
            List<string> back = (List<string>)MsgList.DataSource;
            MsgList.DataSource = null;
            errors.Clear();
            warnings.Clear();
            notes.Clear();
            StartErrorCheck();
            errorButton.Text = "Errors: " + errors.Count;
            warningButton.Text = "Warnings: " + warnings.Count;
            notesButton.Text = "Notes: " + notes.Count;
            if (back == null)
            {
                back = errors;
                errorButton.Select();
            }
            MsgList.DataSource = back;
        }
        public bool isInList<T>(List<T> l, int id)
        {
            return l.ElementAtOrDefault(id) != null;
        }
        public void CheckStartPos()
        {
            var s = kmp.StartPositions;
            if (s.Entries.Count != 0) notes.Add("Start Positions should only be used in battle tracks.");
            int i = 0;
            foreach (var ent in s.Entries) {
                if (ent.PositionY <= 0)
                {
                    warnings.Add(String.Format("Start Position {0}: Position Y is 0 or less.", i));
                }
                i++;
            }
        }
        public void CheckEnemy()
        {
            var s = kmp.EnemyRoutes;
            bool battle = kmp.CheckPoints.Entries.Count == 0;
            short i = 0;
            foreach(var g in s.Entries)
            {
                CheckEnemyLinks(s, i, battle);
                if (!battle && g.Next.Max() < 0) errors.Add(String.Format("Enemy Group {0} has no next group.", i));
                foreach(var n in g.Next)
                {
                    if (n < 0) continue;
                    if (!isInList(s.Entries, n))
                    {
                        errors.Add(String.Format("Enemy Group {0}: Next group with ID {1} doesn't exist.", i, n));
                        continue;
                    }
                    if (!battle && !s.Entries[n].Previous.Contains(i)) warnings.Add(String.Format("Enemy Group {0} has Enemy Group {1} in the next list, but Enemy Group {1} doesn't have Enemy Group {0} in the previous list.", i, n));
                }
                foreach (var n in g.Previous)
                {
                    if (n < 0) continue;
                    if (!isInList(s.Entries, n))
                    {
                        errors.Add(String.Format("Enemy Group {0}: Previuos group with ID {1} doesn't exist.", i, n));
                        continue;
                    }
                    if (!battle && !s.Entries[n].Next.Contains(i)) warnings.Add(String.Format("Enemy Group {0} has Enemy Group {1} in the previous list, but Enemy Group {1} doesn't have Enemy Group {0} in the next list.", i, n));
                }
                int j = 0;
                foreach(var p in g.Entries)
                {
                    if (p.PositionY <= 0) errors.Add(String.Format("Enemy Group {0}: Entry {1}: Position Y is 0 or less.", i, j));
                    j++;
                }
                i++;
            }
        }
        // The links as MapdataEnemyPoint::setup reads them (the research on enemy AI behaviour, Finding 2): only the
        // valid entries packed at the start of each list are read; in a battle each link names one end of the other
        // group through the link end flags, and only exists in the direction of the group that lists it.
        private void CheckEnemyLinks(KMPExpander.Class.SimpleKMPs.EnemyRoutes s, int i, bool battle)
        {
            var g = s.Entries[i];
            foreach (var list in new[] { g.Previous, g.Next })
            {
                if (list == null) continue;
                int valid = list.Count(n => n >= 0);
                for (int slot = 0; slot < valid; slot++)
                    if (list[slot] < 0)
                    {
                        warnings.Add(String.Format("Enemy Group {0}: the {1} groups have an empty slot before a group; the game only reads the first {2} slots, so fill them from the first slot on.", i, list == g.Previous ? "previous" : "next", valid));
                        break;
                    }
            }
            if (!battle)
            {
                if (g.LinkEndFlags != 0) warnings.Add(String.Format("Enemy Group {0}: the link end flags are only used in battle; in a race a next group marked \"last\" is entered at its last point, which no original race course does.", i));
                return;
            }
            if (g.Entries.Count == 0) return;
            if (g.Entries.Count == 1 && KMPExpander.Class.SimpleKMPs.EnemyRoutes.EnemyGroup.LinkSlots(g.Previous).Count > 0)
                warnings.Add(String.Format("Enemy Group {0} has a single point, so the game ignores its previous groups; list all its neighbours as next groups.", i));
            for (int side = 0; side < 2; side++)
            {
                bool next = side == 1;
                if (!next && g.Entries.Count == 1) continue;
                string myEnd = next ? "last" : "first";
                foreach (int[] link in KMPExpander.Class.SimpleKMPs.EnemyRoutes.EnemyGroup.LinkSlots(next ? g.Next : g.Previous))
                {
                    int q = link[1];
                    if (q >= s.Entries.Count || s.Entries[q].Entries.Count == 0) continue;
                    var other = s.Entries[q];
                    bool flag = g.LinkEndFlag(link[0], next);
                    bool otherFirst = next ? !flag : flag;
                    string otherEnd = otherFirst ? "first" : "last";
                    bool? expected = s.ExpectedLinkEndFlag(i, q, next);
                    if (q != i && expected != null && flag != expected.Value)
                        warnings.Add(String.Format("Enemy Group {0}: its {1} point is linked to the {2} point of Enemy Group {3}, but the {4} point of that group is nearer (Tools, Set battle link ends from geometry).", i, myEnd, otherEnd, q, otherFirst ? "last" : "first"));
                    // The link back: group q must list group i at its end of the junction, naming i's end. A single-point
                    // group only has its next groups.
                    bool back = false;
                    bool backIsNext = !otherFirst || other.Entries.Count == 1;
                    foreach (int[] b in KMPExpander.Class.SimpleKMPs.EnemyRoutes.EnemyGroup.LinkSlots(backIsNext ? other.Next : other.Previous))
                    {
                        if (b[1] != i) continue;
                        bool bflag = other.LinkEndFlag(b[0], backIsNext);
                        bool backToFirst = backIsNext ? !bflag : bflag;
                        if (backToFirst == !next || g.Entries.Count == 1) back = true;
                    }
                    if (!back && q != i)
                        notes.Add(String.Format("Enemy Group {0} links its {1} point to the {2} point of Enemy Group {3}, but that group does not link back to it there, so CPUs can drive from {0} to {3} but not from {3} to {0}.", i, myEnd, otherEnd, q));
                }
            }
        }

        public void CheckItem()
        {
            var s = kmp.ItemRoutes;
            short i = 0;
            foreach (var g in s.Entries)
            {
                if (g.Next.Max() < 0) errors.Add(String.Format("Item Group {0} has no next group.", i));
                if (g.Previous.Max() < 0) errors.Add(String.Format("Item Group {0} has no previous group.", i));
                foreach (var n in g.Next)
                {
                    if (n < 0) continue;
                    if (!isInList(s.Entries, n))
                    {
                        errors.Add(String.Format("Item Group {0}: Item group with ID {1} doesn't exist.", i, n));
                        continue;
                    }
                    if (!s.Entries[n].Previous.Contains(i)) warnings.Add(String.Format("Item Group {0} has Item Group {1} in the next list, but Item Group {1} doesn't have Item Group {0} in the previous list.", i, n));
                }
                foreach (var n in g.Previous)
                {
                    if (n < 0) continue;
                    if (!isInList(s.Entries, n))
                    {
                        errors.Add(String.Format("Item Group {0}: Previuos group with ID {1} doesn't exist.", i, n));
                        continue;
                    }
                    if (!s.Entries[n].Next.Contains(i)) warnings.Add(String.Format("Item Group {0} has Item Group {1} in the previous list, but Item Group {1} doesn't have Item Group {0} in the next list.", i, n));
                }
                int j = 0;
                foreach (var p in g.Entries)
                {
                    if (p.PositionY <= 0) errors.Add(String.Format("Item Group {0}: Entry {1}: Position Y is 0 or less.", i, j));
                    j++;
                }
                i++;
            }
        }
        public void CheckGlider()
        {

            var s = kmp.GliderRoutes;
            sbyte i = 0;
            foreach (var g in s.Entries)
            {
                if (g.Next.Max() >= 0) notes.Add(String.Format("Glider Group {0}: Next field is normally not used.", i));
                if (g.Previous.Max() >= 0) notes.Add(String.Format("Glider Group {0}: Previous field is normally not used.", i));
                foreach (var n in g.Next)
                {
                    if (n < 0) continue;
                    if (!isInList(s.Entries, n))
                    {
                        errors.Add(String.Format("Glider Group {0}: Glider group with ID {1} doesn't exist.", i, n));
                        continue;
                    }
                    if (!s.Entries[n].Previous.Contains(i)) warnings.Add(String.Format("Glider Group {0} has Glider Group {1} in the next list, but Glider Group {1} doesn't have Glider Group {0} in the previous list.", i, n));
                }
                foreach (var n in g.Previous)
                {
                    if (n < 0) continue;
                    if (!isInList(s.Entries, n))
                    {
                        errors.Add(String.Format("Glider Group {0}: Previuos group with ID {1} doesn't exist.", i, n));
                        continue;
                    }
                    if (!s.Entries[n].Next.Contains(i)) warnings.Add(String.Format("Glider Group {0} has Glider Group {1} in the previous list, but Glider Group {1} doesn't have Glider Group {0} in the next list.", i, n));
                }
                if (g.CannonSection && !g.ForceToRoute) notes.Add(String.Format("Glider Group {0} has the CannonSection flag set to True but not the ForceToRoute flag.", i));
                int j = 0;
                foreach (var p in g.Entries)
                {
                    if (p.PositionY <= 0) errors.Add(String.Format("Glider Group {0}: Entry {1}: Position Y is 0 or less.", i, j));
                    j++;
                }
                i++;
            }
        }
        public bool CheckCheckPointKeySection(int id, int currentKey, int currentSection, int start, int itercount, List<int> path) { 
            path.Add(id);
            if (itercount > sbyte.MaxValue)
            {
                errors.Add(String.Format("Infinite Checkpoint Loop {0} detected that never reaches the finish line.", string.Join("->", path)));
                return false;
            }
            var grp = kmp.CheckPoints.Entries[id];
            int i = 0;
            foreach (var ent in grp.Entries)
            {
                if (ent.Key != -1)
                {
                    if (ent.Key != currentKey + 1)
                    {
                        errors.Add(String.Format("Invalid Key Checkpoint order in Checkpoint Path {0}, Checkpoint {1} (expected Key value {2}).", string.Join("->", path), i, currentKey + 1));
                        return false;
                    } else
                    {
                        currentKey++;
                    }
                }
                if (ent.Section != -1)
                {
                    if (ent.Section != currentSection + 1)
                    {
                        if (ent.Section > 1) errors.Add(String.Format("Invalid Section Checkpoint in Checkpoint Group {0}, Checkpoint {1}, Section values can only be either 0 or 1.", id, i));
                        errors.Add(String.Format("Invalid Section Checkpoint order in Checkpoint Path {0}, Checkpoint {1} (expected Section value {2}).", string.Join("->", path), i, currentSection + 1));
                        return false;
                    } else
                    {
                        currentSection++;
                    }
                }
                i++;
            }
            foreach(var n in grp.Next)
            {
                if (n == -1 || n == start) continue;
                if (!CheckCheckPointKeySection(n, currentKey, currentSection, start, itercount + 1, path)) return false;
            }
            path.RemoveAt(path.Count - 1);
            return true;
        }
        public void CheckCheckPoint()
        {
            var s = kmp.CheckPoints;
            sbyte i = 0;
            int startid = -1;
            int initialcount = errors.Count + warnings.Count;
            foreach (var g in s.Entries)
            {
                if (g.Next.Max() < 0) errors.Add(String.Format("Checkpoint Group {0} has no next group.", i));
                if (g.Previous.Max() < 0) errors.Add(String.Format("Checkpoint Group {0} has no previous group.", i));
                foreach (var n in g.Next)
                {
                    if (n < 0) continue;
                    if (!isInList(s.Entries, n))
                    {
                        errors.Add(String.Format("Checkpoint Group {0}: Checkpoint group with ID {1} doesn't exist.", i, n));
                        continue;
                    }
                    if (!s.Entries[n].Previous.Contains(i)) warnings.Add(String.Format("Checkpoint Group {0} has Checkpoint Group {1} in the next list, but Checkpoint Group {1} doesn't have Checkpoint Group {0} in the previous list.", i, n));
                    if (!CheckPoints.CheckpointGroup.IsConvex(g.Entries[g.Entries.Count - 1], s.Entries[n].Entries[0]))
                    {
                        errors.Add(String.Format("Checkpoint groups {0} and {1} form a concave quadrilateral.", i, n));
                    }
                }
                foreach (var n in g.Previous)
                {
                    if (n < 0) continue;
                    if (!isInList(s.Entries, n))
                    {
                        errors.Add(String.Format("Checkpoint Group {0}: Checkpoint group with ID {1} doesn't exist.", i, n));
                        continue;
                    }
                    if (!s.Entries[n].Next.Contains(i)) warnings.Add(String.Format("Checkpoint Group {0} has Checkpoint Group {1} in the previous list, but Checkpoint Group {1} doesn't have Checkpoint Group {0} in the next list.", i, n));
                }
                int j = 0;
                CheckPoints.CheckpointGroup.CheckpointEntry prevCheck = null;
                foreach (var ent in g.Entries)
                {
                    if (ent.Key == 0)
                    {
                        if (j == 0)
                        {
                            if (startid == -1) startid = i;
                            else errors.Add("Multiple Key Checkpoints with value 0 (finish line) detected.");
                        } else
                        {
                            warnings.Add(String.Format("Checkpoint Group {0}: Key Checkpoints with value 0 (finish line) can only be the first of their group.", i));
                        }
                    }
                    if (prevCheck != null)
                    {
                        if (!CheckPoints.CheckpointGroup.IsConvex(prevCheck, ent))
                        {
                            errors.Add(String.Format("Checkpoint Group {0}: Checkpoints {1} and {2} form a concave quadrilateral.", i, j - 1, j));
                        }
                    }
                    prevCheck = ent;
                    j++;
                }
                i++;
            }
            if (errors.Count + warnings.Count == initialcount)
            {
                if (startid == -1) errors.Add("No Key Checkpoint with value 0 (finish line) was detected.");
                else CheckCheckPointKeySection(startid, -1, -1, startid, 0, new List<int>());
            }
            i = 0;
            foreach (var g in s.Entries)
            {
                int j = 0;
                foreach (var ent in g.Entries)
                {
                    if (!isInList(kmp.RespawnPoints.Entries, ent.RespawnId)) errors.Add(String.Format("Checkpoint Group {0}, Checkpoint {1}: Respawn Point with ID {2} doesn't exist.", i, j, ent.RespawnId));
                    j++;
                }
                i++;
            }
        }
        public void CheckRespawnPoint()
        {
            var s = kmp.RespawnPoints;
            int i = 0;
            foreach(var p in s.Entries)
            {
                if (p.PositionY <= 0) warnings.Add(String.Format("Respawn Point {0}: Position Y is 0 or less.", i));
                if (p.RotationX != 0 || p.RotationZ != 0) notes.Add(String.Format("Respawn Point {0}: RotationXZ are rarely used, did you want to edit RotationY?", i));
                i++;
            }
        }
        public void CheckObject()
        {
            var s = kmp.Objects;
            int i = 0;
            bool skyboxfound = false;
            bool startfound = false;
            foreach (var p in s.Entries)
            {
                if (p.PositionY <= 0) notes.Add(String.Format("Object {0}: Position Y is 0 or less.", i));
                if (p.RouteID >= 0 && !isInList(kmp.Routes.Entries, p.RouteID)) errors.Add(String.Format("Object {0}: Route with ID {1} doesn't exist.", i, p.RouteID));
                i++;
                if (p.ObjectID == 0x012D)
                {
                    if (!startfound) startfound = true;
                    else warnings.Add("Multiple Start Grid Objects detected.");
                }
                if (p.ObjectID >= 0x0385 && p.ObjectID <= 0x0396)
                {
                    if (!skyboxfound) skyboxfound = true;
                    else warnings.Add("Multiple Skybox Objects detected.");
                }
            }
            if (!startfound) errors.Add("No Start Grid Object found.");
            if (!skyboxfound) warnings.Add("No Skybox Object found.");
            int clipObjects = s.Entries.Count(o => o.ObjectID == 0x1E);
            if (clipObjects > 32) warnings.Add(String.Format("There are {0} culling (clip) objects, but the game only reads the first 32: the ones after the 32nd never hide anything.", clipObjects));
        }
        public void CheckRoutes()
        {
            return;
        }
        public void CheckAreas()
        {
            var s = kmp.Area;
            int i = 0;
            foreach (var p in s.Entries)
            {
                if (p.PositionY > 0) notes.Add(String.Format("Area {0}: Position Y is above 0, remember that areas only extend upwards so it is recommended to place them below 0.", i));
                switch(p.TypeID)
                {
                    case 0:
                        if (!isInList(kmp.Camera.Entries, p.CAMEIndex)) errors.Add(String.Format("Area {0}: Camera with ID {1} doesn't exist.", i, p.CAMEIndex));
                        break;
                    case 3:
                        if (!isInList(kmp.Routes.Entries, p.RouteID)) errors.Add(String.Format("Area {0}: Route with ID {1} doesn't exist.", i, p.RouteID));
                        break;
                    case 11:
                        if (p.RouteID != -1 && !isInList(kmp.Routes.Entries, p.RouteID)) errors.Add(String.Format("Area {0}: Route with ID {1} doesn't exist.", i, p.RouteID));
                        break;
                    default:
                        break;
                }
                i++;
            }
        }
        public void CheckCameras()
        {
            notes.Add("Camera error checking is not implemented yet.");
            return;
        }
        public void CheckRendering()
        {
            notes.Add("Rendering error checking is not implemented yet.");
            return;
        }
        public void StartErrorCheck()
        {
            CheckStartPos();
            CheckEnemy();
            CheckItem();
            CheckGlider();
            CheckCheckPoint();
            CheckRespawnPoint();
            CheckObject();
            CheckRoutes();
            CheckAreas();
            CheckCameras();
            CheckRendering();
        }

        private void errorButton_Click(object sender, EventArgs e)
        {
            MsgList.DataSource = errors;
            (sender as Button).Select();
        }

        private void warningButton_Click(object sender, EventArgs e)
        {
            MsgList.DataSource = warnings;
            (sender as Button).Select();
        }

        private void notesButton_Click(object sender, EventArgs e)
        {
            MsgList.DataSource = notes;
            (sender as Button).Select();
        }

        private void refreshButton_Click(object sender, EventArgs e)
        {
            ErrorRefresh();
        }
    }
}
