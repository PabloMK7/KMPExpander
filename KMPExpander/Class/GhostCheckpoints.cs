using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using KMPExpander.Class.SimpleKMPs;

namespace KMPExpander.Class
{
    // The ghost areas of the checkpoints (the research on ghost checkpoints, "Checking a course for ghost checkpoints"),
    // offline, as exact polygons. Outside every quad, what the search returns only depends on what each quad says of the
    // place: off its side lines, "before the start" or "past the end". Those answers change only across a side line, a
    // checkpoint line or the line where the ratio's denominator is 0 (the points as far from both checkpoint lines,
    // a = b), so the plane cut along all these lines gives convex pieces in which every search has one result. The
    // search runs once from every checkpoint at a point inside each piece; a piece where a start gets a checkpoint by gap
    // bridging is a ghost area of that start.
    // A pair (start, ghost) is a respawn glitch when the ghost is more than MaxSkip checkpoints away along the course,
    // the game accepts it and its respawn point is another one. A border is an edge between a ghost piece of a start and
    // the start's own quad: a kart that drives out of the quad there gets the ghost.
    public class GhostCheckpoints
    {
        public const int MaxSkip = 2;

        public class Face
        {
            public double[] X, Z;
            public Dictionary<int, int> Ghosts;    // start -> ghost
        }

        public class Border
        {
            public int Start, Ghost;
            public float X0, Z0, X1, Z1;
        }

        public List<Face> Faces = new List<Face>();
        public HashSet<long> Glitches = new HashSet<long>();
        public List<Border> Borders = new List<Border>();
        public CheckpointSearch Search;

        public bool IsGlitch(int start, int ghost)
        {
            return Glitches.Contains(((long)start << 32) | (uint)ghost);
        }

        // The checkpoint list as the search sees it, to know when the result is out of date.
        public static string Signature(SimpleKMP kmp)
        {
            StringBuilder sb = new StringBuilder();
            foreach (var g in kmp.CheckPoints.Entries)
            {
                sb.Append('|');
                foreach (var p in g.Previous ?? new sbyte[0]) sb.Append(p).Append(',');
                sb.Append('/');
                foreach (var n in g.Next ?? new sbyte[0]) sb.Append(n).Append(',');
                foreach (var e in g.Entries)
                    sb.Append(';').Append(e.LeftPointX).Append(',').Append(e.LeftPointZ).Append(',').Append(e.RightPointX).Append(',')
                        .Append(e.RightPointZ).Append(',').Append(e.RespawnId).Append(',').Append(e.Key);
            }
            foreach (var j in kmp.RespawnPoints.Entries)
                sb.Append('#').Append(j.PositionX).Append(',').Append(j.PositionZ).Append(',').Append(j.ForceCheckpointID);
            return sb.ToString();
        }

        // The ghost areas of every checkpoint. Runs on a worker thread; returns null when cancelled. The pieces are cut
        // inside the checkpoints' bounds with a quarter of their size more on every side.
        public static GhostCheckpoints Compute(CheckpointSearch cs, Func<bool> cancelled)
        {
            if (cs.Points.Count == 0) return null;
            return Build(cs, Cut(new List<double[]>() { Box(cs) }, cs, cancelled), -1, cancelled);
        }

        // Only the areas where some start gets checkpoint ghost: fast enough to follow a drag. A bridge returns a quad
        // between its own side lines, entered from a linked quad between its side lines too, so the pieces are cut only
        // where a strip of the ghost meets a strip of a linked quad (without what an earlier such region covers).
        public static GhostCheckpoints ComputeFor(CheckpointSearch cs, int ghost)
        {
            var c = cs.Points[ghost];
            List<double[][]> regions = new List<double[][]>();
            foreach (double[][] own in Strips(cs, ghost))
                foreach (int linked in c.Nexts.Concat(c.Prevs).Distinct())
                    foreach (double[][] other in Strips(cs, linked))
                        regions.Add(own.Concat(other).ToArray());
            List<double[]> faces = new List<double[]>();
            for (int r = 0; r < regions.Count; r++)
            {
                double[] f = Box(cs);
                foreach (double[] h in regions[r]) if (f != null) f = Clip(f, h[0], h[1], h[2], h[3]);
                if (f == null) continue;
                foreach (double[] piece in Cut(new List<double[]>() { f }, cs, () => false))
                {
                    double x = 0, z = 0;
                    int count = piece.Length / 2;
                    for (int i = 0; i < count; i++) { x += piece[2 * i]; z += piece[2 * i + 1]; }
                    x /= count; z /= count;
                    bool earlier = false;
                    for (int e = 0; e < r && !earlier; e++)
                        earlier = regions[e].All(h => h[0] * (x - h[2]) + h[1] * (z - h[3]) >= 0);
                    if (!earlier) faces.Add(piece);
                }
            }
            return Build(cs, faces, ghost, () => false);
        }

        // The strips of the quads of checkpoint i, where the side tests of the quad test pass: for each next m, the two
        // half-planes n·(p - q) >= 0, as (nx, nz, qx, qz).
        private static List<double[][]> Strips(CheckpointSearch cs, int i)
        {
            var c = cs.Points[i];
            List<double[][]> strips = new List<double[][]>();
            foreach (int j in c.Nexts)
            {
                var m = cs.Points[j];
                strips.Add(new double[][] {
                    new double[] { -(m.LZ - c.LZ), m.LX - c.LX, m.LX, m.LZ },
                    new double[] { m.RZ - c.RZ, -(m.RX - c.RX), c.RX, c.RZ } });
            }
            return strips;
        }

        // The part of the convex polygon f where n·p >= n·(px, pz), or null.
        private static double[] Clip(double[] f, double nx, double nz, double px, double pz)
        {
            foreach (double[] piece in Split(new List<double[]>() { f }, new double[] { nx, nz, nx * px + nz * pz }))
            {
                double x = 0, z = 0;
                int count = piece.Length / 2;
                for (int i = 0; i < count; i++) { x += piece[2 * i]; z += piece[2 * i + 1]; }
                if (nx * (x / count - px) + nz * (z / count - pz) >= 0) return piece;
            }
            return null;
        }

        private static List<double[]> Cut(List<double[]> faces, CheckpointSearch cs, Func<bool> cancelled)
        {
            foreach (double[] line in Lines(cs))
            {
                if (cancelled()) return null;
                faces = Split(faces, line);
            }
            return faces;
        }

        private static double[] Box(CheckpointSearch cs)
        {
            var cps = cs.Points;
            double minX = double.MaxValue, minZ = double.MaxValue, maxX = double.MinValue, maxZ = double.MinValue;
            foreach (var c in cps)
            {
                minX = Math.Min(minX, Math.Min(c.LX, c.RX)); maxX = Math.Max(maxX, Math.Max(c.LX, c.RX));
                minZ = Math.Min(minZ, Math.Min(c.LZ, c.RZ)); maxZ = Math.Max(maxZ, Math.Max(c.LZ, c.RZ));
            }
            double size = Math.Max(Math.Max(maxX - minX, maxZ - minZ), 1);
            minX -= size * 0.25; maxX += size * 0.25; minZ -= size * 0.25; maxZ += size * 0.25;
            return new double[] { minX, minZ, maxX, minZ, maxX, maxZ, minX, maxZ };
        }

        // The searches in every piece, then the respawn glitches and the borders. onlyGhost >= 0 keeps only that ghost.
        private static GhostCheckpoints Build(CheckpointSearch cs, List<double[]> faces, int onlyGhost, Func<bool> cancelled)
        {
            if (faces == null) return null;
            int n = cs.Points.Count;
            GhostCheckpoints g = new GhostCheckpoints() { Search = cs };
            bool[][] reach = Reach(cs);
            Face[] result = new Face[faces.Count];
            const int chunk = 256;
            Parallel.For(0, (faces.Count + chunk - 1) / chunk, (block, loop) =>
            {
                if (cancelled()) { loop.Stop(); return; }
                int[] res = new int[n];
                float[] ratio = new float[n];
                List<int> candidates = new List<int>();
                CheckpointSearch.GhostScratch scratch = cs.NewGhostScratch();
                for (int fi = block * chunk; fi < Math.Min(faces.Count, (block + 1) * chunk); fi++)
                    result[fi] = Ghosts(cs, faces[fi], reach, res, ratio, candidates, scratch, onlyGhost);
            });
            if (cancelled()) return null;
            foreach (Face face in result) if (face != null) g.Faces.Add(face);

            int[] prog = cs.Progress();
            int lap = prog.Max() + 1;
            foreach (Face face in g.Faces)
                foreach (var pair in face.Ghosts)
                {
                    long key = ((long)pair.Key << 32) | (uint)pair.Value;
                    if (!g.Glitches.Contains(key) && Glitch(cs, prog, lap, pair.Key, pair.Value)) g.Glitches.Add(key);
                }
            // The edges of a ghost piece of a start whose other side is in the start's quad: tested half a unit across the
            // middle of the edge.
            foreach (Face face in g.Faces)
            {
                int k = face.X.Length;
                double cx = face.X.Average(), cz = face.Z.Average();
                for (int i = 0; i < k; i++)
                {
                    int j = (i + 1) % k;
                    double mx = (face.X[i] + face.X[j]) / 2, mz = (face.Z[i] + face.Z[j]) / 2;
                    double nx = face.Z[j] - face.Z[i], nz = face.X[i] - face.X[j], length = Math.Sqrt(nx * nx + nz * nz);
                    if (length < 1e-6) continue;
                    if (nx * (mx - cx) + nz * (mz - cz) < 0) { nx = -nx; nz = -nz; }
                    float qx = (float)(mx + nx / length * 0.5), qz = (float)(mz + nz / length * 0.5);
                    foreach (var pair in face.Ghosts)
                        if (cs.Inside(pair.Key, qx, qz))
                            g.Borders.Add(new Border() { Start = pair.Key, Ghost = pair.Value,
                                X0 = (float)face.X[i], Z0 = (float)face.Z[i], X1 = (float)face.X[j], Z1 = (float)face.Z[j] });
                }
            }
            return g;
        }

        // The searches from every start at a point inside the piece f, which is in no quad: the piece with what they
        // find by gap bridging, or null.
        private static Face Ghosts(CheckpointSearch cs, double[] f, bool[][] reach, int[] res, float[] ratio, List<int> candidates,
            CheckpointSearch.GhostScratch scratch, int onlyGhost)
        {
            var cps = cs.Points;
            int n = cps.Count, k = f.Length / 2;
            double cx = 0, cz = 0;
            for (int i = 0; i < k; i++) { cx += f[2 * i]; cz += f[2 * i + 1]; }
            float x = (float)(cx / k), z = (float)(cz / k);
            cs.CheckAll(x, z, res, ratio);
            for (int i = 0; i < n; i++) if (res[i] == 2) return null;
            if (onlyGhost >= 0 && res[onlyGhost] != 1) return null;
            // A bridge returns a quad between whose side lines the place is, entered from a linked one that is too.
            candidates.Clear();
            for (int i = 0; i < n; i++)
            {
                if (res[i] != 1) continue;
                bool linked = false;
                foreach (int j in cps[i].Nexts) if (res[j] == 1) linked = true;
                foreach (int j in cps[i].Prevs) if (res[j] == 1) linked = true;
                if (linked) candidates.Add(i);
            }
            if (candidates.Count == 0) return null;
            Dictionary<int, int> found = null;
            for (int s = 0; s < n; s++)
            {
                bool reached = false;
                if (onlyGhost >= 0) reached = reach[s][onlyGhost];
                else foreach (int i in candidates) if (reach[s][i]) { reached = true; break; }
                if (!reached) continue;
                int ghost = cs.FindGhost(scratch, s, x, z, res, ratio);
                if (ghost < 0 || (onlyGhost >= 0 && ghost != onlyGhost)) continue;
                if (found == null) found = new Dictionary<int, int>();
                found[s] = ghost;
            }
            if (found == null) return null;
            Face face = new Face() { X = new double[k], Z = new double[k], Ghosts = found };
            for (int i = 0; i < k; i++) { face.X[i] = f[2 * i]; face.Z[i] = f[2 * i + 1]; }
            return face;
        }

        // The lines where an answer of the quad test (MapdataCheckPoint::checkSectorAndDistanceRatio) can change, as
        // (nx, nz, d) with n·p = d: every checkpoint line (a = 0, b = 0), and for every quad c -> m its side lines and
        // the line a = b, where a = F_c·(p - R_c) and b = F_m·(p - L_m).
        private static List<double[]> Lines(CheckpointSearch cs)
        {
            List<double[]> lines = new List<double[]>();
            Action<double, double, double, double> through = (ax, az, bx, bz) =>
            {
                double nx = bz - az, nz = ax - bx;
                if (nx * nx + nz * nz > 1e-9) lines.Add(new double[] { nx, nz, nx * ax + nz * az });
            };
            foreach (var c in cs.Points)
            {
                through(c.LX, c.LZ, c.RX, c.RZ);
                foreach (int j in c.Nexts)
                {
                    var m = cs.Points[j];
                    through(c.LX, c.LZ, m.LX, m.LZ);
                    through(c.RX, c.RZ, m.RX, m.RZ);
                    double nx = c.FX - m.FX, nz = c.FZ - m.FZ;
                    if (nx * nx + nz * nz > 1e-9) lines.Add(new double[] { nx, nz, c.FX * c.RX + c.FZ * c.RZ - (m.FX * m.LX + m.FZ * m.LZ) });
                }
            }
            return lines;
        }

        // Cuts the convex polygons (x, z pairs) that the line crosses in two.
        private static List<double[]> Split(List<double[]> faces, double[] line)
        {
            double length = Math.Sqrt(line[0] * line[0] + line[1] * line[1]);
            double nx = line[0] / length, nz = line[1] / length, d = line[2] / length;
            const double eps = 1e-4;
            List<double[]> result = new List<double[]>(faces.Count + 64);
            List<double> a = new List<double>(), b = new List<double>();
            foreach (double[] f in faces)
            {
                int k = f.Length / 2;
                double lo = double.MaxValue, hi = double.MinValue;
                for (int i = 0; i < k; i++)
                {
                    double s = nx * f[2 * i] + nz * f[2 * i + 1] - d;
                    lo = Math.Min(lo, s); hi = Math.Max(hi, s);
                }
                if (lo >= -eps || hi <= eps)
                {
                    result.Add(f);
                    continue;
                }
                a.Clear(); b.Clear();
                for (int i = 0; i < k; i++)
                {
                    int j = (i + 1) % k;
                    double x0 = f[2 * i], z0 = f[2 * i + 1], x1 = f[2 * j], z1 = f[2 * j + 1];
                    double s0 = nx * x0 + nz * z0 - d, s1 = nx * x1 + nz * z1 - d;
                    if (s0 <= eps) { a.Add(x0); a.Add(z0); }
                    if (s0 >= -eps) { b.Add(x0); b.Add(z0); }
                    if ((s0 < -eps && s1 > eps) || (s0 > eps && s1 < -eps))
                    {
                        double t = s0 / (s0 - s1), x = x0 + (x1 - x0) * t, z = z0 + (z1 - z0) * t;
                        a.Add(x); a.Add(z);
                        b.Add(x); b.Add(z);
                    }
                }
                if (a.Count >= 6) result.Add(a.ToArray());
                if (b.Count >= 6) result.Add(b.ToArray());
            }
            return result;
        }

        // ghost_checkpoints.py is_glitch, offline: far along the course, accepted, and another respawn point.
        private static bool Glitch(CheckpointSearch cs, int[] prog, int lap, int start, int ghost)
        {
            if (prog[start] >= 0 && prog[ghost] >= 0)
            {
                int d = ((prog[ghost] - prog[start]) % lap + lap) % lap;
                if (d > lap / 2) d -= lap;
                if (Math.Abs(d) <= MaxSkip) return false;
            }
            return cs.Accepts(start, ghost) && cs.Points[ghost].Jugem != cs.Points[start].Jugem;
        }

        // ghost_checkpoints.py search_reach: for each start, the quads its search can test, those up to 17 links away and
        // those the fallback walk reaches without going through a key checkpoint.
        private static bool[][] Reach(CheckpointSearch cs)
        {
            var cps = cs.Points;
            bool[][] result = new bool[cps.Count][];
            for (int s = 0; s < cps.Count; s++)
            {
                HashSet<int> seen = new HashSet<int>() { s };
                List<int> ring = new List<int>() { s };
                for (int k = 0; k < 17; k++)
                {
                    List<int> nextRing = new List<int>();
                    foreach (int i in ring)
                        foreach (int j in cps[i].Nexts.Concat(cps[i].Prevs))
                            if (seen.Add(j)) nextRing.Add(j);
                    ring = nextRing;
                }
                Stack<int> todo = new Stack<int>(cps[s].Nexts.Concat(cps[s].Prevs));
                HashSet<int> walk = new HashSet<int>(todo);
                while (todo.Count > 0)
                {
                    var c = cps[todo.Pop()];
                    if (c.Type >= 0) continue;
                    foreach (int j in c.Nexts.Concat(c.Prevs))
                        if (walk.Add(j)) todo.Push(j);
                }
                seen.UnionWith(walk);
                result[s] = new bool[cps.Count];
                foreach (int i in seen) result[s][i] = true;
            }
            return result;
        }
    }
}
