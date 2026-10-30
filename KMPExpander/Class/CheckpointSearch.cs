using System;
using System.Collections.Generic;
using KMPExpander.Class.SimpleKMPs;

namespace KMPExpander.Class
{
    // The game's checkpoint search and what it accepts as a kart's checkpoint, ported from ghost_checkpoints.py of the
    // research on ghost checkpoints: Field::FindSector and FindRecursiveSector (its Finding 2), and
    // RaceSys::LapRankChecker::calcLapPosition_ and onOutOfBounds (its Finding 4), offline (ghost checkpoints are not
    // disabled). The search starts from the quad found the frame before, so where quads overlap seen from above (a road
    // over another) the kart keeps the one it drove into. Checkpoints are numbered across the whole CKPT list.
    // Not modelled: the rules of Maka Wuhu (LapRankChecker::m_is_maka_wuhu), the test of CKPT byte 0x15 on 3-lap courses
    // (only courses with sections set that byte, and they are not 3-lap courses), and the finish.
    public class CheckpointSearch
    {
        public class Point
        {
            public int Index, Group, Jugem, Type, Key, Section;
            public float LX, LZ, RX, RZ, FX, FZ;
            public List<int> Nexts = new List<int>(), Prevs = new List<int>();
            public CheckPoints.CheckpointGroup.CheckpointEntry Entry;
        }

        // What the game keeps per kart (KartInfo): the start of the next search, the current checkpoint (its respawn point
        // is used), the key id, the checkpoint kept when the search finds nothing, and Force_No_Lap_Completion.
        public class State
        {
            public int Previous = -1, Current = -1, Key = -1, LastValid = -1;
            public bool ForceNoLap;
        }

        public List<Point> Points = new List<Point>();
        private List<int> groupStart = new List<int>(), groupCount = new List<int>();
        private List<sbyte[]> groupPrev = new List<sbyte[]>(), groupNext = new List<sbyte[]>();
        private int finish, maxKey;
        private int[] jugemCheckpoint;

        public CheckpointSearch(SimpleKMP kmp)
        {
            int index = 0;
            foreach (var g in kmp.CheckPoints.Entries)
            {
                groupStart.Add(index);
                groupCount.Add(g.Entries.Count);
                groupPrev.Add(g.Previous ?? new sbyte[0]);
                groupNext.Add(g.Next ?? new sbyte[0]);
                foreach (var e in g.Entries)
                {
                    Point c = new Point() { Index = index++, Group = groupStart.Count - 1, Jugem = e.RespawnId, Type = e.Key, Section = e.Section, Entry = e,
                        LX = e.LeftPointX, LZ = e.LeftPointZ, RX = e.RightPointX, RZ = e.RightPointZ };
                    // MapdataCheckPoint constructor: the normal of the checkpoint line.
                    float fx = c.RZ - c.LZ, fz = c.LX - c.RX, length = (float)Math.Sqrt(fx * fx + fz * fz);
                    if (length == 0) length = 1;
                    c.FX = fx / length;
                    c.FZ = fz / length;
                    Points.Add(c);
                }
            }
            // MapdataCheckPoint::setupLink: inside a group the neighbours, at its ends the linked groups.
            foreach (Point c in Points)
            {
                int start = groupStart[c.Group], end = start + groupCount[c.Group] - 1;
                if (c.Index > start) c.Prevs.Add(c.Index - 1);
                else
                    foreach (sbyte q in groupPrev[c.Group])
                        if (q >= 0 && q < groupStart.Count && groupCount[q] > 0) c.Prevs.Add(groupStart[q] + groupCount[q] - 1);
                if (c.Index < end) c.Nexts.Add(c.Index + 1);
                else
                    foreach (sbyte q in groupNext[c.Group])
                        if (q >= 0 && q < groupStart.Count && groupCount[q] > 0) c.Nexts.Add(groupStart[q]);
            }
            // The key id of the last key checkpoint before each checkpoint, walking from the finish line.
            finish = 0;
            maxKey = 0;
            foreach (Point c in Points)
            {
                if (c.Type == 0) finish = c.Index;
                maxKey = Math.Max(maxKey, c.Type);
            }
            foreach (Point c in Points) c.Key = int.MinValue;
            if (Points.Count > 0)
            {
                Stack<KeyValuePair<int, int>> todo = new Stack<KeyValuePair<int, int>>();
                todo.Push(new KeyValuePair<int, int>(finish, 0));
                while (todo.Count > 0)
                {
                    var item = todo.Pop();
                    Point c = Points[item.Key];
                    if (c.Key != int.MinValue) continue;
                    c.Key = c.Type != -1 ? c.Type : item.Value;
                    for (int i = c.Nexts.Count - 1; i >= 0; i--)
                        if (Points[c.Nexts[i]].Key == int.MinValue) todo.Push(new KeyValuePair<int, int>(c.Nexts[i], c.Key));
                }
            }
            foreach (Point c in Points) if (c.Key == int.MinValue) c.Key = 0;
            // FieldDirector::createBeforeStructure: the checkpoint of each respawn point, its own when set, else the last
            // checkpoint naming it whose quad holds it.
            var jugems = kmp.RespawnPoints.Entries;
            jugemCheckpoint = new int[jugems.Count];
            for (int j = 0; j < jugems.Count; j++)
            {
                short forced = (short)jugems[j].ForceCheckpointID;
                jugemCheckpoint[j] = -1;
                if (forced > 0) { jugemCheckpoint[j] = forced & 0xFF; continue; }
                for (int i = Points.Count - 1; i >= 0; i--)
                {
                    float ratio = 0;
                    if (Points[i].Jugem == j && Check(Points[i], jugems[j].PositionX, jugems[j].PositionZ, ref ratio) == 2) { jugemCheckpoint[j] = i; break; }
                }
            }
        }

        // MapdataCheckPoint::checkSectorAndDistanceRatio: 2 inside the quad, 1 between its side lines only, 0 outside;
        // ratio is written whenever the side tests pass.
        private int Check(Point c, float x, float z, ref float ratio)
        {
            float dx = x - c.RX, dz = z - c.RZ;
            int res = 0;
            foreach (int j in c.Nexts)
            {
                Point m = Points[j];
                float e1x = m.LX - c.LX, e1z = m.LZ - c.LZ, e2x = m.RX - c.RX, e2z = m.RZ - c.RZ;
                float vx = x - m.LX, vz = z - m.LZ;
                if (e1x * vz - e1z * vx < 0) continue;
                if (e2z * dx - e2x * dz < 0) continue;
                float a = c.FX * dx + c.FZ * dz, b = m.FX * vx + m.FZ * vz, den = a - b;
                ratio = den != 0 ? a / den : (a > 0 ? float.PositiveInfinity : a < 0 ? float.NegativeInfinity : float.NaN);
                if (ratio >= 0 && ratio <= 1) return 2;
                res = 1;
            }
            return res;
        }

        // One search (Field::FindSector with full set), as the model's Search class.
        internal class Search
        {
            public CheckpointSearch Owner;
            public float X, Z, Ratio;
            public int[] Seen;                             // Seen[i] == Stamp: quad i was tested by this search
            public int Stamp = 1;
            public bool Bridged, SkipGlobal;               // the result is a ghost checkpoint; leave out the last test of every quad
            public int[] TableRes;                         // Check of every quad at (X, Z), when known
            public float[] TableRatio;

            private int Test(Point c)
            {
                if (TableRes == null) return Owner.Check(c, X, Z, ref Ratio);
                int res = TableRes[c.Index];
                if (res > 0) Ratio = TableRatio[c.Index];
                return res;
            }

            public int Recursive(int depth, bool backward, Point c, int flags)
            {
                int limit = (flags & 4) != 0 ? 16 : 8;
                if (depth >= 0 && depth > limit) return -1;
                int res = Seen[c.Index] == Stamp ? 0 : Test(c);
                Seen[c.Index] = Stamp;
                if (res == 2) return c.Index;
                int nd = depth >= 0 ? depth + 1 : -1, f2;
                List<int> first, second;
                bool firstBack;
                if (backward)
                {
                    if ((flags & 1) != 0 && res == 1 && Ratio > 1) { Ratio = 1; Bridged = true; return c.Index; }
                    if ((flags & 2) == 0 && c.Type >= 0) return -1;
                    f2 = res == 1 && Ratio < 0 ? flags | 1 : flags & ~1;
                    first = c.Prevs; second = c.Nexts; firstBack = true;
                }
                else
                {
                    if ((flags & 1) != 0 && res == 1 && Ratio < 0) { Ratio = 0; Bridged = true; return c.Index; }
                    if ((flags & 2) == 0 && c.Type >= 0) return -1;
                    f2 = res == 1 && Ratio > 1 ? flags | 1 : flags & ~1;
                    first = c.Nexts; second = c.Prevs; firstBack = false;
                }
                foreach (int j in first)
                {
                    int r = Recursive(nd, firstBack, Owner.Points[j], f2);
                    if (r != -1) return r;
                }
                int last = -1;
                foreach (int j in second)
                {
                    if (Seen[j] == Stamp) continue;
                    last = Recursive(nd, !firstBack, Owner.Points[j], f2);
                    if (last != -1) return last;
                }
                return last;
            }

            // The other prevs of each next, the other nexts of each prev (in these the inner loop breaks after a hit, the
            // outer one does not), the nexts, the prevs. Methods rather than lambdas, so that a search allocates nothing.
            private const int MainFlags = 6;
            private int found;

            private bool NextsPrevs(Point c)
            {
                var cps = Owner.Points;
                foreach (int j in c.Nexts)
                    foreach (int k in cps[j].Prevs)
                        if (k != c.Index) { found = Recursive(1, true, cps[k], MainFlags); if (found != -1) break; }
                return found != -1;
            }

            private bool PrevsNexts(Point c)
            {
                var cps = Owner.Points;
                foreach (int j in c.Prevs)
                    foreach (int k in cps[j].Nexts)
                        if (k != c.Index) { found = Recursive(1, false, cps[k], MainFlags); if (found != -1) break; }
                return found != -1;
            }

            private bool Nexts(Point c)
            {
                foreach (int j in c.Nexts) { found = Recursive(1, false, Owner.Points[j], MainFlags); if (found != -1) return true; }
                return false;
            }

            private bool Prevs(Point c)
            {
                foreach (int j in c.Prevs) { found = Recursive(1, true, Owner.Points[j], MainFlags); if (found != -1) return true; }
                return false;
            }

            public int Find(int start)
            {
                var cps = Owner.Points;
                Point c = cps[start];
                int res = Test(c);
                Seen[c.Index] = Stamp;
                if (res == 2) return c.Index;
                found = -1;
                bool done;
                if (res == 0) done = NextsPrevs(c) || PrevsNexts(c) || Nexts(c) || Prevs(c);
                else if (Ratio <= 0.5f) done = Prevs(c) || PrevsNexts(c) || NextsPrevs(c) || Nexts(c);
                else done = Nexts(c) || NextsPrevs(c) || PrevsNexts(c) || Prevs(c);
                // The fallback walks, without a depth limit but stopping at key checkpoints.
                if (!done)
                    foreach (int j in c.Nexts) { found = Recursive(-1, false, cps[j], 0); if (found != -1) { done = true; break; } }
                if (!done)
                    foreach (int j in c.Prevs) { found = Recursive(-1, true, cps[j], 0); if (found != -1) { done = true; break; } }
                // Last, every other quad in index order.
                if (found == -1 && !SkipGlobal)
                    foreach (Point d in cps)
                    {
                        if (Seen[d.Index] == Stamp) continue;
                        Seen[d.Index] = Stamp;
                        float ratio = 0;
                        if (Owner.Check(d, X, Z, ref ratio) == 2) return d.Index;
                    }
                return found;
            }
        }

        public int Find(int start, float x, float z)
        {
            if (Points.Count == 0) return -1;
            if (start < 0 || start >= Points.Count) start = finish;
            return new Search() { Owner = this, X = x, Z = z, Seen = new int[Points.Count] }.Find(start);
        }

        // Check of every quad at (x, z), for FindGhost: res[i] as Check returns it, ratio[i] when res[i] > 0.
        public void CheckAll(float x, float z, int[] res, float[] ratio)
        {
            for (int i = 0; i < Points.Count; i++)
            {
                float r = 0;
                res[i] = Check(Points[i], x, z, ref r);
                ratio[i] = r;
            }
        }

        // Whether (x, z) is inside the quad of checkpoint i.
        public bool Inside(int i, float x, float z)
        {
            float r = 0;
            return Check(Points[i], x, z, ref r) == 2;
        }

        // What FindGhost keeps from one search to the next on a thread: the search, its marks numbered by search.
        public class GhostScratch
        {
            internal Search Search;
        }

        public GhostScratch NewGhostScratch()
        {
            return new GhostScratch() { Search = new Search() { Owner = this, SkipGlobal = true, Seen = new int[Points.Count], Stamp = 0 } };
        }

        // The search from start at a place outside every quad (with CheckAll's results there): the ghost checkpoint it
        // returns by gap bridging, or -1.
        public int FindGhost(GhostScratch scratch, int start, float x, float z, int[] res, float[] ratio)
        {
            Search search = scratch.Search;
            search.X = x;
            search.Z = z;
            search.Ratio = 0;
            search.Bridged = false;
            search.TableRes = res;
            search.TableRatio = ratio;
            search.Stamp++;
            int r = search.Find(start);
            return search.Bridged ? r : -1;
        }

        // The last check of calcLapPosition_ offline, for a kart whose current checkpoint is before and whose search
        // returns after: whether after becomes its current checkpoint (ghost_checkpoints.py accept_jump; CKPT byte 0x15
        // is not modelled, as above).
        public bool Accepts(int before, int after)
        {
            int d = Points[after].Key - Points[before].Key;
            if (d == 1 || d == -1) return true;
            if ((d == -maxKey && after == finish) || (d == maxKey && after == (Points.Count - 1 + finish) % Points.Count)) return true;
            return Neighbours(before, after);
        }

        // The checkpoints from the finish line along the course, the shortest way, so that parallel routes at the same
        // place get the same value; -1 where the finish line does not lead.
        public int[] Progress()
        {
            int[] prog = new int[Points.Count];
            for (int i = 0; i < prog.Length; i++) prog[i] = -1;
            if (Points.Count == 0) return prog;
            Queue<int> todo = new Queue<int>();
            prog[finish] = 0;
            todo.Enqueue(finish);
            while (todo.Count > 0)
            {
                int i = todo.Dequeue();
                foreach (int j in Points[i].Nexts)
                    if (prog[j] < 0)
                    {
                        prog[j] = prog[i] + 1;
                        todo.Enqueue(j);
                    }
            }
            return prog;
        }

        // A kart placed somewhere: the quad the search finds from the finish line becomes its checkpoint.
        public void Init(State s, float x, float z)
        {
            int r = Find(finish, x, z);
            s.Previous = s.Current = r;
            s.Key = r >= 0 ? Points[r].Key : -1;
            s.LastValid = -1;
            s.ForceNoLap = false;
        }

        // calcLapPosition_ for one frame. False when the search finds no quad: the kart has left the course, and the game
        // starts a respawn on the next frame.
        public bool Update(State s, float x, float z)
        {
            int r = Find(s.Previous, x, z);
            if (r < 0)
            {
                if (s.LastValid < 0) s.LastValid = s.Current;
                return false;
            }
            s.Previous = r;
            Point a = Points[r];
            if (s.ForceNoLap)
            {
                if (a.Key == s.Key) s.ForceNoLap = false;
            }
            else if (s.Current >= 0)
            {
                int d = a.Key - s.Key;
                if (d == 1 || d == -1) { s.Key += d; s.Current = r; }
                else if ((d == -maxKey && r == finish) || (d == maxKey && r == (Points.Count - 1 + finish) % Points.Count)) { s.Key = a.Key; s.Current = r; }
                else if (d != 0) s.ForceNoLap = true;
            }
            if (s.Current < 0) { s.Current = r; s.Key = a.Key; }
            else if (s.Key >= 0 && Neighbours(s.Current, r)) s.Current = r;
            return true;
        }

        // The last tests of calcLapPosition_: the result is next to the current checkpoint in its group, or the current
        // one ends a previous group or starts a next group of the result's group.
        private bool Neighbours(int before, int after)
        {
            Point b = Points[before], a = Points[after];
            int start = groupStart[a.Group], end = start + groupCount[a.Group] - 1;
            if (before >= start && before <= end)
            {
                if (Math.Abs(after - before) <= 1) return true;
                int beforeFinish = (Points.Count - 1 + finish) % Points.Count;
                return (after == 0 && before == beforeFinish) || (after == beforeFinish && before == 0);
            }
            foreach (sbyte q in groupPrev[a.Group])
            {
                if (q < 0) break;
                if (q == b.Group) return before == groupStart[q] + groupCount[q] - 1;
            }
            foreach (sbyte q in groupNext[a.Group])
            {
                if (q < 0) break;
                if (q == b.Group) return before == groupStart[q];
            }
            return false;
        }

        // GetKartJugemRecoverSectorIndex: the checkpoint whose respawn point a respawn uses.
        public Point RespawnCheckpoint(State s)
        {
            int i = s.LastValid >= 0 ? s.LastValid : s.Current;
            return i >= 0 && i < Points.Count ? Points[i] : null;
        }

        // onOutOfBounds, when a respawn starts at respawn point jugem: the kart takes that point's checkpoint when it is
        // in the same key checkpoint section.
        public void OnRespawn(State s, int jugem)
        {
            s.LastValid = -1;
            if (jugem < 0 || jugem >= jugemCheckpoint.Length) return;
            int c = jugemCheckpoint[jugem];
            if (c >= 0 && c < Points.Count && Points[c].Key == s.Key) s.Current = s.Previous = c;
        }
    }
}
