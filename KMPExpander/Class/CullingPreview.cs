using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using LibCTR.Collections;
using Tao.OpenGl;

namespace KMPExpander.Class
{
    // What the game would cull: the 8x8 leaf cells of its quadtree, hidden by the clip ID of a checkpoint and, with the
    // kart simulator, by the camera's view.
    public class CullingPreview
    {
        public const int GridCells = 8;

        public float MinX, MinZ, CellW, CellH;
        public bool[] HiddenCells;                   // null: nothing to preview
        public List<float[]> ViewEdges = null;       // the view region: normal x, normal z, point x, point z
        public int KartClipID = -1;
        public bool Simulating = false;              // culling for the simulated kart, not for a selected checkpoint
        public SimpleKMPs.KartSimulator.KartEntry CameraKart = null;
        public List<float[]> ViewPolygon = null;     // the view region as a polygon (x, z), for drawing
        private SimpleKMP kmp = null;
        private List<int> clipRoutes = null;
        public int[] ObjFaceGroups = null;           // OBJ face -> culling group, as the retail div rule splits it
        public List<float[]> ObjGroupRects = null;   // min x, min z, max x, max z

        // The routes of the clip objects with this clip ID: the clip ID picks a bit of a 32-bit mask (-1 and 32 or more
        // clip nothing), and the game only reads the first 32 clip objects.
        public static List<int> GetClipRoutes(SimpleKMP kmp, int clipID)
        {
            List<int> routes = new List<int>();
            if (kmp == null || clipID < 0 || clipID >= 32) return routes;
            int clipObjects = 0;
            foreach (var obj in kmp.Objects.Entries)
            {
                if (obj.ObjectID != 0x1E) continue;
                if (clipObjects++ == 32) break;
                if (obj.Settings1 != clipID || kmp.Routes.Entries.ElementAtOrDefault(obj.RouteID) == null) continue;
                if (kmp.Routes.Entries[obj.RouteID].Entries.Count == 4 && !routes.Contains(obj.RouteID)) routes.Add(obj.RouteID);
            }
            return routes;
        }

        // The checkpoint quad (a checkpoint and one of its next ones) that holds the point; null if none. The game
        // keeps the result of its own search, which can differ near overlapping or misplaced checkpoints.
        public static SimpleKMPs.CheckPoints.CheckpointGroup.CheckpointEntry FindCheckpoint(SimpleKMP kmp, float x, float z)
        {
            if (kmp == null || kmp.CheckPoints == null) return null;
            var groups = kmp.CheckPoints.Entries;
            for (int g = 0; g < groups.Count; g++)
            {
                var entries = groups[g].Entries;
                for (int i = 0; i < entries.Count; i++)
                {
                    var nexts = new List<SimpleKMPs.CheckPoints.CheckpointGroup.CheckpointEntry>();
                    if (i + 1 < entries.Count) nexts.Add(entries[i + 1]);
                    else if (groups[g].Next != null)
                    {
                        foreach (var next in groups[g].Next)
                            if (next >= 0 && next < groups.Count && groups[next].Entries.Count > 0) nexts.Add(groups[next].Entries[0]);
                    }
                    foreach (var n in nexts)
                    {
                        float[] qx = { entries[i].LeftPointX, n.LeftPointX, n.RightPointX, entries[i].RightPointX };
                        float[] qz = { entries[i].LeftPointZ, n.LeftPointZ, n.RightPointZ, entries[i].RightPointZ };
                        if (PointInPolygon(qx, qz, x, z)) return entries[i];
                    }
                }
            }
            return null;
        }

        private static bool PointInPolygon(float[] px, float[] pz, float x, float z)
        {
            bool inside = false;
            for (int i = 0, j = px.Length - 1; i < px.Length; j = i++)
            {
                if ((pz[i] > z) != (pz[j] > z) && x < (px[j] - px[i]) * (z - pz[i]) / (pz[j] - pz[i]) + px[i]) inside = !inside;
            }
            return inside;
        }

        // The game's QuadTree::convertTo. When the centre of the view leaves the model's bounding box through its bottom
        // (or top) first, the far corners whose rays reach that height before their own distance are moved there. The
        // view region is then made of the edges of the view volume that leave all eight corners on one side, seen from
        // above, keeping only those that cut the model's bounding rectangle. More than 8 edges: no view culling.
        public static List<float[]> GetViewRegion(Vector3 pos, Vector3 dir, float far, Vector3[] corners, Vector3 modelMin, Vector3 modelMax, out List<float[]> polygon)
        {
            Vector3[] c = (Vector3[])corners.Clone();
            float[] p = { pos.X, pos.Y, pos.Z }, d = { dir.X, dir.Y, dir.Z };
            float[] mn = { modelMin.X, modelMin.Y, modelMin.Z }, mx = { modelMax.X, modelMax.Y, modelMax.Z };
            int side = 0;
            float best = float.MaxValue;
            for (int a = 0; a < 3; a++)
            {
                if (Math.Abs(d[a]) < 1.192093e-07f) continue;
                float t = (mn[a] - p[a]) / d[a];
                int s = -1;
                if (t < 0 || t >= far)
                {
                    t = (mx[a] - p[a]) / d[a];
                    s = 1;
                    if (t < 0 || t >= far) continue;
                }
                if (t < best)
                {
                    best = t;
                    side = a == 1 ? s : 0;
                }
            }
            if (side != 0)
            {
                float plane = side < 0 ? modelMin.Y : modelMax.Y;
                foreach (int i in new int[] { 0, 1, 4, 5 })
                {
                    float dx = c[i].X - pos.X, dy = c[i].Y - pos.Y, dz = c[i].Z - pos.Z;
                    float l = (float)Math.Sqrt(dx * dx + dy * dy + dz * dz);
                    dx /= l; dy /= l; dz /= l;
                    if (Math.Abs(dy) <= 1.192093e-07f) continue;
                    float t = (plane - pos.Y) / dy;
                    if (t >= 0 && t < l) c[i] = new Vector3(pos.X + dx * t, pos.Y + dy * t, pos.Z + dz * t);
                }
            }

            polygon = ConvexHull(c);
            int[,] edges = { { 0, 1 }, { 0, 2 }, { 0, 4 }, { 1, 3 }, { 1, 5 }, { 2, 3 }, { 2, 6 }, { 3, 7 }, { 4, 5 }, { 4, 6 }, { 5, 7 }, { 6, 7 } };
            float[] rx = { modelMin.X, modelMax.X, modelMin.X, modelMax.X }, rz = { modelMin.Z, modelMin.Z, modelMax.Z, modelMax.Z };
            List<float[]> region = new List<float[]>();
            for (int e = 0; e < 12; e++)
            {
                int a = edges[e, 0], b = edges[e, 1];
                float nx = c[b].Z - c[a].Z, nz = c[a].X - c[b].X;
                if (nx == 0 && nz == 0) continue;
                bool first = true, oneSide = true;
                float reference = 0;
                for (int k = 0; k < 8 && oneSide; k++)
                {
                    if (k == a || k == b) continue;
                    float dd = (c[k].X - c[a].X) * nx + (c[k].Z - c[a].Z) * nz;
                    if (first) { if (dd != 0) { first = false; reference = dd; } }
                    else if (reference * dd < 0) oneSide = false;
                }
                if (!oneSide) continue;
                if (region.Count > 7) return null;
                float nl = (float)Math.Sqrt(nx * nx + nz * nz);
                nx /= nl; nz /= nl;
                if (reference >= 0) { nx = -nx; nz = -nz; }
                bool duplicate = false;
                foreach (var r in region)
                    if (Math.Abs(nx - r[0]) <= 1.192093e-07f && Math.Abs(nz - r[1]) <= 1.192093e-07f) duplicate = true;
                if (duplicate) continue;
                Vector3 q = reference < 0 ? c[a] : c[b];
                for (int k = 0; k < 4; k++)
                {
                    if (nx * (rx[k] - q.X) + nz * (rz[k] - q.Z) > 0)
                    {
                        region.Add(new float[] { nx, nz, q.X, q.Z });
                        break;
                    }
                }
            }
            return region;
        }

        // Convex hull of the corners seen from above.
        private static List<float[]> ConvexHull(Vector3[] c)
        {
            List<float[]> pts = new List<float[]>();
            foreach (var v in c) pts.Add(new float[] { v.X, v.Z });
            pts.Sort((a, b) => a[0] != b[0] ? a[0].CompareTo(b[0]) : a[1].CompareTo(b[1]));
            Func<float[], float[], float[], float> cross = (o, a, b) => (a[0] - o[0]) * (b[1] - o[1]) - (a[1] - o[1]) * (b[0] - o[0]);
            List<float[]> hull = new List<float[]>();
            for (int pass = 0; pass < 2; pass++)
            {
                int start = hull.Count;
                for (int i = 0; i < pts.Count; i++)
                {
                    float[] p = pass == 0 ? pts[i] : pts[pts.Count - 1 - i];
                    while (hull.Count >= start + 2 && cross(hull[hull.Count - 2], hull[hull.Count - 1], p) <= 0) hull.RemoveAt(hull.Count - 1);
                    hull.Add(p);
                }
                hull.RemoveAt(hull.Count - 1);
            }
            return hull;
        }

        // Whether the game would not draw a kart or object at this point: like QuadTree::getClipContext, it sits in
        // the smallest cell that holds the square of this radius around it, shrunk by the margin (the root when the
        // square leaves the grid by more than the margin), and is drawn while that cell is.
        public bool PointHidden(float x, float z, float radius)
        {
            if (!Simulating || HiddenCells == null || CellW <= 0 || CellH <= 0) return false;
            float mx = CellW * 0.025f, mz = CellH * 0.025f;
            float x1 = MinX + GridCells * CellW, z1 = MinZ + GridCells * CellH;
            float ax = x - radius, az = z - radius, bx = x + radius, bz = z + radius;
            int level = 0, cx = 0, cz = 0;
            if (!(ax < MinX - mx || az < MinZ - mz || bx > x1 + mx || bz > z1 + mz))
            {
                ax = Math.Max(ax, MinX); az = Math.Max(az, MinZ); bx = Math.Min(bx, x1); bz = Math.Min(bz, z1);
                float sax = ax + mx, sbx = bx - mx, saz = az + mz, sbz = bz - mz;
                if (sbx < sax) sax = sbx = (ax + bx) / 2;
                if (sbz < saz) saz = sbz = (az + bz) / 2;
                int ix0 = Leaf(sax, MinX, CellW), ix1 = Leaf(sbx, MinX, CellW), iz0 = Leaf(saz, MinZ, CellH), iz1 = Leaf(sbz, MinZ, CellH);
                level = 3;
                while (level > 0 && ((ix0 >> (3 - level)) != (ix1 >> (3 - level)) || (iz0 >> (3 - level)) != (iz1 >> (3 - level)))) level--;
                cx = ix0 >> (3 - level);
                cz = iz0 >> (3 - level);
            }
            float w = CellW * (1 << (3 - level)), h = CellH * (1 << (3 - level));
            float rx = MinX + cx * w, rz = MinZ + cz * h;
            if (clipRoutes != null)
                foreach (var r in clipRoutes)
                    if (CDAB.RectInsideQuad(rx, rz, rx + w, rz + h, kmp.Routes.Entries[r])) return true;
            return !CellInView(ViewEdges, rx, rz, rx + w, rz + h, mx, mz);
        }

        private static int Leaf(float v, float min, float size)
        {
            return Math.Max(0, Math.Min(GridCells - 1, (int)((v - min) / size)));
        }

        // A cell is out of view when its four corners, pushed out by 2.5% of a leaf cell, are all beyond one edge.
        public static bool CellInView(List<float[]> region, float x0, float z0, float x1, float z1, float marginX, float marginZ)
        {
            if (region == null) return true;
            float[] cx = { x0 - marginX, x1 + marginX, x0 - marginX, x1 + marginX };
            float[] cz = { z0 - marginZ, z0 - marginZ, z1 + marginZ, z1 + marginZ };
            foreach (var e in region)
            {
                bool allOut = true;
                for (int k = 0; k < 4 && allOut; k++)
                    if ((cx[k] - e[2]) * e[0] + (cz[k] - e[3]) * e[1] < 0) allOut = false;
                if (allOut) return false;
            }
            return true;
        }

        public bool RectHidden(float minX, float minZ, float maxX, float maxZ)
        {
            for (int iz = 0; iz < GridCells; iz++)
            {
                for (int ix = 0; ix < GridCells; ix++)
                {
                    float x = MinX + ix * CellW, z = MinZ + iz * CellH;
                    bool touches = minX <= x + CellW && x <= maxX && minZ <= z + CellH && z <= maxZ;
                    if (touches && !HiddenCells[iz * GridCells + ix]) return false;
                }
            }
            return true;
        }

        public bool FaceCulled(int face)
        {
            if (HiddenCells == null || ObjFaceGroups == null || face >= ObjFaceGroups.Length) return false;
            float[] r = ObjGroupRects[ObjFaceGroups[face]];
            return RectHidden(r[0], r[1], r[2], r[3]);
        }

        // The grid comes from the div, as in the game, or from the OBJ, as if a div were made from it.
        // kart: the simulated kart, or null.
        // For the camera kart, clipID says whether the clip ID of its checkpoint hides cells and cameraView whether its view
        // does.
        public static CullingPreview Compute(CDAB div, OBJWrapper obj, SimpleKMP kmp, SimpleKMPs.KartSimulator.KartEntry kart, List<int> selectedClipRoutes, bool clipID = true, bool cameraView = true)
        {
            CullingPreview c = new CullingPreview();
            bool hasGrid = false;
            if (div != null) hasGrid = div.GetGrid(out c.MinX, out c.MinZ, out c.CellW, out c.CellH);
            Vector3 objMin = new Vector3(), objMax = new Vector3();
            bool hasObj = obj != null && obj.GetBounds(out objMin, out objMax);
            if (!hasGrid && hasObj)
            {
                c.MinX = objMin.X; c.MinZ = objMin.Z;
                c.CellW = (objMax.X - objMin.X) / GridCells; c.CellH = (objMax.Z - objMin.Z) / GridCells;
                hasGrid = true;
            }
            c.kmp = kmp;
            if (kart != null)
            {
                c.Simulating = true;
                c.CameraKart = kart;
                // While the karts drive, the checkpoint the game's search found for the kart; else the first quad it is in.
                var cp = kart.Checkpoint ?? FindCheckpoint(kmp, kart.Pos.X, kart.Pos.Z);
                c.KartClipID = cp == null ? -1 : cp.ClipID;
            }
            if (!hasGrid) return c;
            if (hasObj) obj.GetCullingGroups(c.MinX, c.MinZ, c.CellW, c.CellH, out c.ObjFaceGroups, out c.ObjGroupRects);

            List<int> routes = selectedClipRoutes;
            bool active = routes != null && routes.Count != 0;
            if (kart != null)
            {
                active = true;
                routes = clipID ? GetClipRoutes(kmp, c.KartClipID) : null;
                Vector3 modelMin = hasObj ? objMin : new Vector3(c.MinX, float.MinValue, c.MinZ);
                Vector3 modelMax = hasObj ? objMax : new Vector3(c.MinX + GridCells * c.CellW, float.MaxValue, c.MinZ + GridCells * c.CellH);
                if (cameraView) c.ViewEdges = GetViewRegion(kart.GetCameraPosition(), kart.GetCameraDirection(), kart.Far, kart.GetCorners(), modelMin, modelMax, out c.ViewPolygon);
                c.clipRoutes = routes;
            }
            if (!active) return c;

            c.HiddenCells = new bool[GridCells * GridCells];
            for (int iz = 0; iz < GridCells; iz++)
            {
                for (int ix = 0; ix < GridCells; ix++)
                {
                    float x = c.MinX + ix * c.CellW, z = c.MinZ + iz * c.CellH;
                    bool hidden = false;
                    if (routes != null)
                    {
                        foreach (var r in routes)
                            if (CDAB.RectInsideQuad(x, z, x + c.CellW, z + c.CellH, kmp.Routes.Entries[r])) hidden = true;
                    }
                    if (kart != null && !CellInView(c.ViewEdges, x, z, x + c.CellW, z + c.CellH, c.CellW * 0.025f, c.CellH * 0.025f)) hidden = true;
                    c.HiddenCells[iz * GridCells + ix] = hidden;
                }
            }
            return c;
        }

        public void RenderGrid(float r, float g, float b)
        {
            if (CellW <= 0 && CellH <= 0) return;
            if (HiddenCells != null)
            {
                Gl.glColor4f(r, g, b, 0.2f);
                Gl.glBegin(Gl.GL_QUADS);
                for (int iz = 0; iz < GridCells; iz++)
                {
                    for (int ix = 0; ix < GridCells; ix++)
                    {
                        if (!HiddenCells[iz * GridCells + ix]) continue;
                        float x = MinX + ix * CellW, z = MinZ + iz * CellH;
                        Gl.glVertex2f(x, z);
                        Gl.glVertex2f(x + CellW, z);
                        Gl.glVertex2f(x + CellW, z + CellH);
                        Gl.glVertex2f(x, z + CellH);
                    }
                }
                Gl.glEnd();
            }
            Gl.glColor4f(r, g, b, 0.35f);
            Gl.glLineWidth(1);
            Gl.glBegin(Gl.GL_LINES);
            for (int i = 0; i <= GridCells; i++)
            {
                Gl.glVertex2f(MinX + i * CellW, MinZ);
                Gl.glVertex2f(MinX + i * CellW, MinZ + GridCells * CellH);
                Gl.glVertex2f(MinX, MinZ + i * CellH);
                Gl.glVertex2f(MinX + GridCells * CellW, MinZ + i * CellH);
            }
            Gl.glEnd();
        }
    }
}
