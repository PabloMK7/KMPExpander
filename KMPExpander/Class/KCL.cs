using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using Extensions;
using Tao.OpenGl;

namespace KMPExpander.Class
{
    // A course collision file (.kcl), in the layout EveryFileExplorer reads for Mario Kart 7: offsets of the vertices,
    // normals, prisms (- 0x10) and octree; each prism is a length, a vertex, the face normal, the three edge normals and
    // the collision value. The low 5 bits of the value are the collision type, bits 5 to 7 its variant (EveryFileExplorer's
    // "basic effect"). The triangles are drawn coloured by type, and give the simulated karts the height of the ground.
    public class KCL
    {
        // What a collision type does to a kart, for drawing and for the kart simulator.
        public enum Kinds { Road, Offroad, Slippery, Boost, GliderPad, Moving, SolidOutOfBounds, Unknown, NoEffect, Wall, ItemWall, Fall, Trigger, GliderActivator }

        public class TypeInfo
        {
            public int ID;
            public string TemplateName, Name, Description;
            public string[] Variants;
            public Kinds Kind;
            public Color Color;

            public TypeInfo(int id, string templateName, string name, string description, string[] variants)
            {
                ID = id; TemplateName = templateName; Name = name; Description = description; Variants = variants;
                Kind = KindOf(id);
                Color = ColorOf(id, Kind);
            }

            // Karts drive on these; the others they fall through or are stopped by.
            public bool IsFloor
            {
                get { return Kind <= Kinds.Unknown; }
            }
        }

        // The names of EveryFileExplorer (KCLInformations.xml) and of the template's KDGndCol::ECollisionTypes, which
        // are made up; where they disagree, both are shown.
        public static readonly TypeInfo[] Types =
        {
            new TypeInfo(0, "ROAD", "Road 1", "Solid road.", new[] { "Normal", "Sand/Gravel", "Sand/Gravel", "Smooth", "Wood", "Slot Based", "Metal Grid", "Metal" }),
            new TypeInfo(1, "ROAD2", "Road 2", "Solid road.", new[] { "Normal (Alternate Sound)", "Carpet", "Slot Based", "Bouncy (Non-trickable)", "Grass/Gravel", "Glass", "Normal (Bump Effect)", "Rainbow" }),
            new TypeInfo(2, "ROAD3", "Road 3", "Solid road.", new[] { "Normal", "Piano", "Xylophone", "Glockenspiel", "Smooth (Bump Effect)", "Slot Based", "Metal Grid", "Slot Based" }),
            new TypeInfo(3, "SAND", "Road 4", "Solid road.", new[] { "Dirt Particles", "Sand/Gravel", "Slot Based", "Slot Based", "Slot Based", "Snow", "Dirt", "Sand/Gravel" }),
            new TypeInfo(4, "LDIRT", "Slippery Road 1", "Slippery road, doesn't slow down karts.", new[] { "Dirt Particles", "Sand/Gravel", "Carpet", "Water", "Grass", "Wood", "Mud (Bump Effect)", "Dirt" }),
            new TypeInfo(5, "DIRT", "Off-road", "Terrain that slows down karts.", new[] { "Dirt", "Sand", "Honey", "Water", "Grass", "Snow", "Mud", "Wood" }),
            new TypeInfo(6, "HDIRT", "Heavy Off-road", "Terrain that slows down karts a lot.", new[] { "Dirt", "Mud", "Slot Based", "Slot Based", "Grass", "Snow", "Glass", "Mud" }),
            new TypeInfo(7, "ICE", "Slippery Road 2", "Slippery road, doesn't slow down karts.", new[] { "Ice", "Mud", "Wet Road", "Water", "Slot Based", "Normal (Alternate Sound)", "Normal (Alternate Sound)", "Normal (Alternate Sound)" }),
            new TypeInfo(8, "DASH", "Boost Pad", "Provides a quick speed boost.", new[] { "Normal", "Sand/Gravel", "Sand/Gravel", "Smooth", "Wood", "Slot Based", "Metal Grid", "Metal" }),
            new TypeInfo(9, "GLIDE", "Glider Pad", "Activates glider.", new[] { "Normal", "Instant Glider Activation", "Sand/Gravel", "Slot Based", "Sand/Gravel", "Rainbow", "Metal Grid", "Metal" }),
            new TypeInfo(10, "ITROAD", "Does Nothing", "Does nothing. [?]", new[] { "Unknown", "Unknown", "Unknown", "Unknown", "Unknown", "Unknown", "Unknown", "Unknown" }),
            new TypeInfo(11, "RESQ", "Solid Out Of Bounds", "Solid terrain that respawns the kart.", new[] { "Unknown", "Unknown", "Unknown", "Unknown", "Unknown", "Unknown", "Unknown", "Unknown" }),
            new TypeInfo(12, "PULL", "Moving Terrain", "Moves karts, settings specified in the KMP.", new[] { "Water", "Conveyor Belt", "Normal (Alternate Sound)", "Normal (Alternate Sound)", "Normal (Alternate Sound)", "Normal (Alternate Sound)", "Normal (Alternate Sound)", "Normal (Alternate Sound)" }),
            new TypeInfo(13, "LAROAD", "Road 5", "Solid Road.", new[] { "Slot Based", "Airship Fortress Metal", "Slot Based", "Metal", "Slot Based", "Slot Based", "Smooth", "Smooth" }),
            new TypeInfo(14, "BELT", "Unknown (Crashes)", "Crashes if a kart comes in contact.", new[] { "Unknown", "Unknown", "Unknown", "Unknown", "Unknown", "Unknown", "Unknown", "Unknown" }),
            new TypeInfo(15, "DUMMY0", "Does nothing", "Karts fall through it.", new[] { "Unknown", "Unknown", "Unknown", "Unknown", "Unknown", "Unknown", "Unknown", "Unknown" }),
            new TypeInfo(16, "WALL", "Wall 1", "Solid wall.", new[] { "Normal", "Rock", "Metal", "Wood", "Slot Based", "Slot Based", "Rubber", "Music" }),
            new TypeInfo(17, "WALL2", "Wall 2", "Solid wall.", new[] { "Rope", "Glass", "Glass", "Balloon", "Slot Based", "Slot Based", "Slot Based", "Slot Based" }),
            new TypeInfo(18, "OUTF", "Kart Barrier", "Blocks karts from moving but not items.", new[] { "Unknown", "Unknown", "Unknown", "Unknown", "Unknown", "Unknown", "Unknown", "Unknown" }),
            new TypeInfo(19, "ITWALL", "Item Barrier", "Blocks items from moving but not karts.", new[] { "Unknown", "Unknown", "Unknown", "Unknown", "Unknown", "Unknown", "Unknown", "Unknown" }),
            new TypeInfo(20, "LWALL", "Wall 3", "Solid wall.", new[] { "Normal", "Rock", "Metal", "Wood", "Slot Based", "Glass", "Rubber", "Music" }),
            new TypeInfo(21, "BWALL", "Wall 4", "Solid wall.", new[] { "Normal", "Rock", "Metal", "Wood", "Slot Based", "Glass", "Rubber", "Music" }),
            new TypeInfo(22, "OUTMH", "Does nothing", "Karts fall through it.", new[] { "Unknown", "Unknown", "Unknown", "Unknown", "Unknown", "Unknown", "Unknown", "Unknown" }),
            new TypeInfo(23, "DUMMY1", "Does nothing", "Karts fall through it.", new[] { "Unknown", "Unknown", "Unknown", "Unknown", "Unknown", "Unknown", "Unknown", "Unknown" }),
            new TypeInfo(24, "DUMMY2", "Does nothing", "Karts fall through it.", new[] { "Unknown", "Unknown", "Unknown", "Unknown", "Unknown", "Unknown", "Unknown", "Unknown" }),
            new TypeInfo(25, "VALLEY", "Out Of Bounds 1", "Causes the karts to fall and respawn.", new[] { "Air", "Water", "Lava", "Lava (No Splash)", "Air", "Unknown", "Unknown", "Unknown" }),
            new TypeInfo(26, "TRIGGER", "Effect Trigger 1", "Triggers a visual effect.", new[] { "Unknown", "Smoke/Fog", "Leaves", "Unknown", "Unknown", "Unknown", "Unknown", "Unknown" }),
            new TypeInfo(27, "SOUND", "Does nothing", "Karts fall through it.", new[] { "Unknown", "Unknown", "Unknown", "Unknown", "Unknown", "Unknown", "Unknown", "Unknown" }),
            new TypeInfo(28, "CANNON", "Glider Activator", "Not solid, activates glider automatically.", new[] { "Unknown", "Unknown", "Unknown", "Unknown", "Unknown", "Unknown", "Unknown", "Unknown" }),
            new TypeInfo(29, "VALLEY2", "Out Of Bounds 2", "Causes the karts to fall and respawn.", new[] { "Air", "Water", "Lava", "Lava (No Splash)", "Air", "Unknown", "Unknown", "Unknown" }),
            new TypeInfo(30, "DUMMY3", "Does nothing", "Karts fall through it.", new[] { "Unknown", "Unknown", "Unknown", "Unknown", "Unknown", "Unknown", "Unknown", "Unknown" }),
            new TypeInfo(31, "ZONE", "Effect Trigger 2", "Triggers a visual effect.", new[] { "Unknown", "Flowerbed", "Unknown", "Unknown", "Unknown", "Unknown", "Unknown", "Unknown" }),
        };

        private static Kinds KindOf(int id)
        {
            switch (id)
            {
                case 0: case 1: case 2: case 3: case 13: return Kinds.Road;
                case 5: case 6: return Kinds.Offroad;
                case 4: case 7: return Kinds.Slippery;
                case 8: return Kinds.Boost;
                case 9: return Kinds.GliderPad;
                case 12: return Kinds.Moving;
                case 11: return Kinds.SolidOutOfBounds;
                case 14: return Kinds.Unknown;
                case 16: case 17: case 18: case 20: case 21: return Kinds.Wall;
                case 19: return Kinds.ItemWall;
                case 25: case 29: return Kinds.Fall;
                case 26: case 31: return Kinds.Trigger;
                case 28: return Kinds.GliderActivator;
                default: return Kinds.NoEffect;
            }
        }

        private static Color ColorOf(int id, Kinds kind)
        {
            switch (kind)
            {
                case Kinds.Road: return id == 13 ? Color.FromArgb(150, 150, 165) : Color.FromArgb(175 - 15 * id, 175 - 15 * id, 175 - 15 * id);
                case Kinds.Offroad: return id == 5 ? Color.FromArgb(160, 120, 60) : Color.FromArgb(110, 75, 35);
                case Kinds.Slippery: return id == 4 ? Color.FromArgb(150, 200, 120) : Color.FromArgb(140, 210, 240);
                case Kinds.Boost: return Color.FromArgb(255, 150, 0);
                case Kinds.GliderPad: return Color.FromArgb(40, 120, 255);
                case Kinds.Moving: return Color.FromArgb(60, 200, 160);
                case Kinds.SolidOutOfBounds: return Color.FromArgb(200, 0, 120);
                case Kinds.Unknown: return Color.FromArgb(255, 255, 255);
                case Kinds.Wall: return id == 18 ? Color.FromArgb(255, 90, 90) : Color.FromArgb(200, 40, 40);
                case Kinds.ItemWall: return Color.FromArgb(240, 170, 170);
                case Kinds.Fall: return id == 25 ? Color.FromArgb(120, 0, 200) : Color.FromArgb(70, 0, 140);
                case Kinds.Trigger: return Color.FromArgb(230, 230, 60);
                case Kinds.GliderActivator: return Color.FromArgb(0, 220, 255);
                default: return Color.FromArgb(90, 90, 90);
            }
        }

        public struct Triangle
        {
            public float AX, AY, AZ, BX, BY, BZ, CX, CY, CZ;
            public float NX, NY, NZ;
            public ushort Value;
            public int Type { get { return Value & 0x1F; } }
            public int Variant { get { return (Value >> 5) & 7; } }
        }

        public List<Triangle> Triangles = new List<Triangle>();
        public string FileName;

        // The tree: one node per collision type present, one child per variant; the checkboxes hide them.
        public class TypeNode
        {
            [Browsable(false)]
            public TypeInfo Info;
            public int Type { get { return Info.ID; } }
            public string Name { get { return Info.Name; } }
            [Description("The name used for this type in the MK7-Memory template, which was made up.")]
            public string TemplateName { get { return Info.TemplateName; } }
            public string Description { get { return Info.Description; } }
            [Description("How the kart simulator treats this collision type.")]
            public Kinds Kind { get { return Info.Kind; } }
            public int Triangles { get; internal set; }
            [Description("How many triangles let karts do a trick, as EveryFileExplorer reads it.")]
            public int Trickable { get; internal set; }
            public string Variants { get; internal set; }
        }

        public class VariantNode
        {
            [Browsable(false)]
            public TypeNode Parent;
            public int Variant { get; internal set; }
            public string Name { get; internal set; }
            public int Triangles { get; internal set; }
        }

        // How the triangles are drawn; shown in the property grid when the Collision node is selected. A front face is
        // one whose face normal points toward the viewer of the current view plane.
        public class RenderOptions
        {
            internal KCL Owner;
            private bool cullFront = false, cullBack = true;
            [Description("Draw only the outlines of the triangles.")]
            public bool Wireframe { get; set; } = false;
            [Description("Hide the triangles that face the viewer.")]
            public bool CullFrontFaces
            {
                get { return cullFront; }
                set { cullFront = value; Owner.InvalidateList(); }
            }
            [Description("Hide the triangles that face away from the viewer.")]
            public bool CullBackFaces
            {
                get { return cullBack; }
                set { cullBack = value; Owner.InvalidateList(); }
            }
        }

        public RenderOptions Options;
        private bool isVisible = true;
        private bool[] hidden = new bool[256];       // by the low 8 bits of the value: type and variant
        private int displayList = 0;
        private int listKey;                           // the view plane the list was made for, -1 for the 3D view

        // Ground queries go through a grid over the XZ bounds, each cell listing the triangles whose bounds touch it.
        private float minX, minZ, cellSize;
        private int cellsX, cellsZ;
        private List<int>[] cells;

        public KCL(string path)
        {
            FileName = Path.GetFileName(path);
            Options = new RenderOptions() { Owner = this };
            using (BinaryReader br = new BinaryReader(new MemoryStream(File.ReadAllBytes(path))))
            {
                uint verticesOffset = br.ReadUInt32(), normalsOffset = br.ReadUInt32();
                uint prismsOffset = br.ReadUInt32() + 0x10, octreeOffset = br.ReadUInt32();
                if (verticesOffset > normalsOffset || normalsOffset > prismsOffset || prismsOffset > octreeOffset || octreeOffset > br.BaseStream.Length)
                    throw new InvalidDataException("Not a Mario Kart 7 KCL file");
                float[] vertices = ReadFloats(br, verticesOffset, (normalsOffset - verticesOffset) / 12 * 3);
                float[] normals = ReadFloats(br, normalsOffset, (prismsOffset - normalsOffset) / 12 * 3);
                br.BaseStream.Position = prismsOffset;
                uint count = (octreeOffset - prismsOffset) / 0x10;
                for (uint i = 0; i < count; i++)
                {
                    float length = br.ReadSingle();
                    int v = br.ReadUInt16(), n = br.ReadUInt16(), na = br.ReadUInt16(), nb = br.ReadUInt16(), nc = br.ReadUInt16();
                    ushort value = br.ReadUInt16();
                    if ((v + 1) * 3 > vertices.Length || Math.Max(Math.Max(n, na), Math.Max(nb, nc)) * 3 + 3 > normals.Length) continue;
                    Triangle t = new Triangle() { Value = value };
                    // EveryFileExplorer's KCL.GetTriangle: B and C lie along the cross products of the edge normals with
                    // the face normal, at the prism's length from A measured along edge normal C.
                    float ax = vertices[v * 3], ay = vertices[v * 3 + 1], az = vertices[v * 3 + 2];
                    float fx = normals[n * 3], fy = normals[n * 3 + 1], fz = normals[n * 3 + 2];
                    float[] crossA = Cross(normals[na * 3], normals[na * 3 + 1], normals[na * 3 + 2], fx, fy, fz);
                    float[] crossB = Cross(normals[nb * 3], normals[nb * 3 + 1], normals[nb * 3 + 2], fx, fy, fz);
                    float dB = crossB[0] * normals[nc * 3] + crossB[1] * normals[nc * 3 + 1] + crossB[2] * normals[nc * 3 + 2];
                    float dA = crossA[0] * normals[nc * 3] + crossA[1] * normals[nc * 3 + 1] + crossA[2] * normals[nc * 3 + 2];
                    if (dA == 0 || dB == 0) continue;
                    float kB = length / dB, kA = length / dA;
                    t.AX = ax; t.AY = ay; t.AZ = az;
                    t.BX = ax + crossB[0] * kB; t.BY = ay + crossB[1] * kB; t.BZ = az + crossB[2] * kB;
                    t.CX = ax + crossA[0] * kA; t.CY = ay + crossA[1] * kA; t.CZ = az + crossA[2] * kA;
                    t.NX = fx; t.NY = fy; t.NZ = fz;
                    if (float.IsNaN(t.BX + t.BY + t.BZ + t.CX + t.CY + t.CZ) || float.IsInfinity(t.BX + t.BY + t.BZ + t.CX + t.CY + t.CZ)) continue;
                    Triangles.Add(t);
                }
            }
            BuildGrid();
        }

        private static float[] ReadFloats(BinaryReader br, uint offset, uint count)
        {
            br.BaseStream.Position = offset;
            float[] result = new float[count];
            for (uint i = 0; i < count; i++) result[i] = br.ReadSingle();
            return result;
        }

        private static float[] Cross(float ax, float ay, float az, float bx, float by, float bz)
        {
            return new float[] { ay * bz - az * by, az * bx - ax * bz, ax * by - ay * bx };
        }

        private void BuildGrid()
        {
            float maxX = float.MinValue, maxZ = float.MinValue;
            minX = float.MaxValue; minZ = float.MaxValue;
            foreach (var t in Triangles)
            {
                minX = Math.Min(minX, Math.Min(t.AX, Math.Min(t.BX, t.CX))); maxX = Math.Max(maxX, Math.Max(t.AX, Math.Max(t.BX, t.CX)));
                minZ = Math.Min(minZ, Math.Min(t.AZ, Math.Min(t.BZ, t.CZ))); maxZ = Math.Max(maxZ, Math.Max(t.AZ, Math.Max(t.BZ, t.CZ)));
            }
            if (Triangles.Count == 0) { cellsX = cellsZ = 0; return; }
            MinY = float.MaxValue;
            foreach (var t in Triangles) MinY = Math.Min(MinY, Math.Min(t.AY, Math.Min(t.BY, t.CY)));
            cellSize = Math.Max(Math.Max(maxX - minX, maxZ - minZ) / 128, 1);
            cellsX = (int)((maxX - minX) / cellSize) + 1;
            cellsZ = (int)((maxZ - minZ) / cellSize) + 1;
            cells = new List<int>[cellsX * cellsZ];
            for (int i = 0; i < Triangles.Count; i++)
            {
                var t = Triangles[i];
                int x0 = CellX(Math.Min(t.AX, Math.Min(t.BX, t.CX))), x1 = CellX(Math.Max(t.AX, Math.Max(t.BX, t.CX)));
                int z0 = CellZ(Math.Min(t.AZ, Math.Min(t.BZ, t.CZ))), z1 = CellZ(Math.Max(t.AZ, Math.Max(t.BZ, t.CZ)));
                for (int z = z0; z <= z1; z++)
                    for (int x = x0; x <= x1; x++)
                    {
                        int c = z * cellsX + x;
                        if (cells[c] == null) cells[c] = new List<int>();
                        cells[c].Add(i);
                    }
            }
        }

        private int CellX(float x) { return Math.Max(0, Math.Min(cellsX - 1, (int)((x - minX) / cellSize))); }
        private int CellZ(float z) { return Math.Max(0, Math.Min(cellsZ - 1, (int)((z - minZ) / cellSize))); }

        // The triangles whose bounds touch the cell of (x, z).
        public IEnumerable<int> TrianglesAt(float x, float z)
        {
            if (cellsX == 0 || x < minX || z < minZ || x > minX + cellsX * cellSize || z > minZ + cellsZ * cellSize) yield break;
            List<int> list = cells[CellZ(z) * cellsX + CellX(x)];
            if (list == null) yield break;
            foreach (int i in list) yield return i;
        }

        // The height of the floor under (x, z) (a floor type, facing up) nearest to near, or the highest one without it;
        // null when there is none. type gets the triangle's collision value.
        public float? GetGroundHeight(float x, float z, float? near, out ushort value)
        {
            float? best = null;
            value = 0;
            foreach (int i in TrianglesAt(x, z))
            {
                var t = Triangles[i];
                if (t.NY <= 0.01f || !Types[t.Type].IsFloor) continue;
                float d = (t.BZ - t.CZ) * (t.AX - t.CX) + (t.CX - t.BX) * (t.AZ - t.CZ);
                if (Math.Abs(d) < 1e-6f) continue;
                float l1 = ((t.BZ - t.CZ) * (x - t.CX) + (t.CX - t.BX) * (z - t.CZ)) / d;
                float l2 = ((t.CZ - t.AZ) * (x - t.CX) + (t.AX - t.CX) * (z - t.CZ)) / d;
                float l3 = 1 - l1 - l2;
                if (l1 < 0 || l2 < 0 || l3 < 0) continue;
                float y = l1 * t.AY + l2 * t.BY + l3 * t.CY;
                if (best == null || (near == null ? y > best.Value : Math.Abs(y - near.Value) < Math.Abs(best.Value - near.Value)))
                {
                    best = y;
                    value = t.Value;
                }
            }
            return best;
        }

        public float? GetGroundHeight(float x, float z, float? near = null)
        {
            ushort value;
            return GetGroundHeight(x, z, near, out value);
        }

        // The lowest height of the file, for the kart simulator's fall limit.
        public float MinY = 0;

        // The highest floor under (x, z) that is not above maxY, or null. value gets its collision value.
        public float? FloorBelow(float x, float z, float maxY, out ushort value)
        {
            Triangle floor;
            return FloorBelow(x, z, maxY, out value, out floor);
        }

        // The same, with the floor triangle (its normal gives the slope).
        public float? FloorBelow(float x, float z, float maxY, out ushort value, out Triangle floor)
        {
            float? best = null;
            value = 0;
            floor = default(Triangle);
            foreach (int i in TrianglesAt(x, z))
            {
                var t = Triangles[i];
                if (t.NY <= 0.01f || !Types[t.Type].IsFloor) continue;
                float d = (t.BZ - t.CZ) * (t.AX - t.CX) + (t.CX - t.BX) * (t.AZ - t.CZ);
                if (Math.Abs(d) < 1e-6f) continue;
                float l1 = ((t.BZ - t.CZ) * (x - t.CX) + (t.CX - t.BX) * (z - t.CZ)) / d;
                float l2 = ((t.CZ - t.AZ) * (x - t.CX) + (t.AX - t.CX) * (z - t.CZ)) / d;
                float l3 = 1 - l1 - l2;
                if (l1 < 0 || l2 < 0 || l3 < 0) continue;
                float y = l1 * t.AY + l2 * t.BY + l3 * t.CY;
                if (y > maxY || (best != null && y <= best.Value)) continue;
                best = y;
                value = t.Value;
                floor = t;
            }
            return best;
        }

        private int[] stamp;
        private int stampCounter = 0;

        // Every triangle whose bounds touch the square of half-size r around (x, z), once each.
        private List<int> TrianglesNear(float x, float z, float r)
        {
            List<int> result = new List<int>();
            if (cellsX == 0) return result;
            if (stamp == null) stamp = new int[Triangles.Count];
            stampCounter++;
            int x0 = CellX(x - r), x1 = CellX(x + r), z0 = CellZ(z - r), z1 = CellZ(z + r);
            if (x + r < minX || z + r < minZ || x - r > minX + cellsX * cellSize || z - r > minZ + cellsZ * cellSize) return result;
            for (int cz = z0; cz <= z1; cz++)
                for (int cx = x0; cx <= x1; cx++)
                {
                    List<int> list = cells[cz * cellsX + cx];
                    if (list == null) continue;
                    foreach (int i in list)
                    {
                        if (stamp[i] == stampCounter) continue;
                        stamp[i] = stampCounter;
                        result.Add(i);
                    }
                }
            return result;
        }

        // The point of triangle t nearest to p (Ericson, Real-Time Collision Detection, 5.1.5).
        private static void ClosestPoint(Triangle t, float px, float py, float pz, out float qx, out float qy, out float qz)
        {
            float abx = t.BX - t.AX, aby = t.BY - t.AY, abz = t.BZ - t.AZ;
            float acx = t.CX - t.AX, acy = t.CY - t.AY, acz = t.CZ - t.AZ;
            float apx = px - t.AX, apy = py - t.AY, apz = pz - t.AZ;
            float d1 = abx * apx + aby * apy + abz * apz, d2 = acx * apx + acy * apy + acz * apz;
            if (d1 <= 0 && d2 <= 0) { qx = t.AX; qy = t.AY; qz = t.AZ; return; }
            float bpx = px - t.BX, bpy = py - t.BY, bpz = pz - t.BZ;
            float d3 = abx * bpx + aby * bpy + abz * bpz, d4 = acx * bpx + acy * bpy + acz * bpz;
            if (d3 >= 0 && d4 <= d3) { qx = t.BX; qy = t.BY; qz = t.BZ; return; }
            float vc = d1 * d4 - d3 * d2;
            if (vc <= 0 && d1 >= 0 && d3 <= 0)
            {
                float v = d1 / (d1 - d3);
                qx = t.AX + abx * v; qy = t.AY + aby * v; qz = t.AZ + abz * v; return;
            }
            float cpx = px - t.CX, cpy = py - t.CY, cpz = pz - t.CZ;
            float d5 = abx * cpx + aby * cpy + abz * cpz, d6 = acx * cpx + acy * cpy + acz * cpz;
            if (d6 >= 0 && d5 <= d6) { qx = t.CX; qy = t.CY; qz = t.CZ; return; }
            float vb = d5 * d2 - d1 * d6;
            if (vb <= 0 && d2 >= 0 && d6 <= 0)
            {
                float w = d2 / (d2 - d6);
                qx = t.AX + acx * w; qy = t.AY + acy * w; qz = t.AZ + acz * w; return;
            }
            float va = d3 * d6 - d5 * d4;
            if (va <= 0 && (d4 - d3) >= 0 && (d5 - d6) >= 0)
            {
                float w = (d4 - d3) / ((d4 - d3) + (d5 - d6));
                qx = t.BX + (t.CX - t.BX) * w; qy = t.BY + (t.CY - t.BY) * w; qz = t.BZ + (t.CZ - t.BZ) * w; return;
            }
            float denom = 1 / (va + vb + vc);
            float vv = vb * denom, ww = vc * denom;
            qx = t.AX + abx * vv + acx * ww; qy = t.AY + aby * vv + acy * ww; qz = t.AZ + abz * vv + acz * ww;
        }

        // Whether a sphere touches a triangle of one of these kinds; value gets the first one's collision value.
        public bool Touches(float x, float y, float z, float r, Func<TypeInfo, bool> kinds, out ushort value)
        {
            return Touches(x, y, z, r, kinds, null, out value);
        }

        // values, when given, also filters the triangles by their whole collision value (type and variant).
        public bool Touches(float x, float y, float z, float r, Func<TypeInfo, bool> kinds, Func<ushort, bool> values, out ushort value)
        {
            value = 0;
            foreach (int i in TrianglesNear(x, z, r))
            {
                var t = Triangles[i];
                if (!kinds(Types[t.Type]) || (values != null && !values(t.Value))) continue;
                float qx, qy, qz;
                ClosestPoint(t, x, y, z, out qx, out qy, out qz);
                if ((qx - x) * (qx - x) + (qy - y) * (qy - y) + (qz - z) * (qz - z) < r * r)
                {
                    value = t.Value;
                    return true;
                }
            }
            return false;
        }

        // Pushes a sphere out of the walls (the wall types, and floors steeper than about 70 degrees) sideways only, so
        // that a kart slides along them. Returns whether it touched one; pushX and pushZ get the direction it was pushed in.
        public bool PushOutOfWalls(ref float x, float y, ref float z, float r, out float pushX, out float pushZ)
        {
            bool hit = false;
            float startX = x, startZ = z;
            for (int pass = 0; pass < 2; pass++)
            {
                foreach (int i in TrianglesNear(x, z, r))
                {
                    var t = Triangles[i];
                    Kinds kind = Types[t.Type].Kind;
                    if (kind != Kinds.Wall) continue;
                    float qx, qy, qz;
                    ClosestPoint(t, x, y, z, out qx, out qy, out qz);
                    float dx = x - qx, dz = z - qz, dy = y - qy;
                    float d2 = dx * dx + dy * dy + dz * dz;
                    if (d2 >= r * r) continue;
                    // Collision is one-sided: a kart behind a wall (as on top of a ramp whose back face is a wall) is
                    // not pushed.
                    if (dx * t.NX + dy * t.NY + dz * t.NZ < 0) continue;
                    float h = (float)Math.Sqrt(dx * dx + dz * dz);
                    if (h < 1e-4f)
                    {
                        // The centre is on the wall: push along its normal.
                        float nh = (float)Math.Sqrt(t.NX * t.NX + t.NZ * t.NZ);
                        if (nh < 1e-4f) continue;
                        dx = t.NX / nh; dz = t.NZ / nh; h = 1;
                        d2 = 0;
                    }
                    float push = (float)Math.Sqrt(Math.Max(0, r * r - dy * dy)) - (float)Math.Sqrt(Math.Max(0, d2 - dy * dy));
                    if (push <= 0) continue;
                    x += dx / h * push;
                    z += dz / h * push;
                    hit = true;
                }
            }
            pushX = x - startX;
            pushZ = z - startZ;
            return hit;
        }

        public bool GetVisibility() { return isVisible; }
        public void SetVisibility(bool visible) { isVisible = visible; }

        public void SetHidden(int type, int variant, bool hide)
        {
            hidden[type | (variant << 5)] = hide;
            InvalidateList();
        }

        public bool IsHidden(int type, int variant) { return hidden[type | (variant << 5)]; }

        private void InvalidateList()
        {
            if (displayList != 0) Gl.glDeleteLists(displayList, 1);
            displayList = 0;
        }

        public void Dispose()
        {
            InvalidateList();
        }

        // The triangles in the colour of their type, darker the steeper they are, from a display list (rebuilt when a
        // type is hidden, the culling options or the view plane change), so that drawing them every frame while karts
        // drive stays cheap.
        public void Render()
        {
            if (!isVisible) return;
            ViewPlaneHandler vph = (Application.OpenForms[0] as Form1).vph;
            if (displayList != 0 && listKey != ListKey(vph)) InvalidateList();
            Gl.glLoadIdentity();
            Gl.glBindTexture(Gl.GL_TEXTURE_2D, 0);
            Gl.glDepthFunc(Gl.GL_LESS);
            Gl.glDisable(Gl.GL_TEXTURE_2D);
            if (Options.Wireframe) Gl.glPolygonMode(Gl.GL_FRONT_AND_BACK, Gl.GL_LINE);
            // In 3D the side facing the viewer changes with the camera, so OpenGL culls the faces (Compile winds them
            // counterclockwise around their normal).
            if (vph.Is3D && (Options.CullFrontFaces || Options.CullBackFaces))
            {
                Gl.glEnable(Gl.GL_CULL_FACE);
                Gl.glCullFace(Options.CullFrontFaces && Options.CullBackFaces ? Gl.GL_FRONT_AND_BACK : Options.CullFrontFaces ? Gl.GL_FRONT : Gl.GL_BACK);
            }
            if (displayList != 0) Gl.glCallList(displayList);
            else Compile(vph);
            Gl.glDisable(Gl.GL_CULL_FACE);
            Gl.glPolygonMode(Gl.GL_FRONT_AND_BACK, Gl.GL_FILL);
            Gl.glEnable(Gl.GL_TEXTURE_2D);
        }

        private static int ListKey(ViewPlaneHandler vph) { return vph.Is3D ? -1 : (int)vph.mode; }

        // The distance along dir to the nearest shown triangle the ray hits, null when it hits none (the 3D view's
        // snapping). solid leaves out what karts neither stand on nor are stopped by (falls, triggers, item walls...).
        public float? RayCast(LibCTR.Collections.Vector3 origin, LibCTR.Collections.Vector3 dir, bool solid = false)
        {
            if (!isVisible || (Options.CullFrontFaces && Options.CullBackFaces))
                return null;

            float? best = null;

            foreach (var t in Triangles)
            {
                if (hidden[t.Value & 0xFF]) continue;
                if (solid && !Types[t.Type].IsFloor && Types[t.Type].Kind != Kinds.Wall) continue;

                var a = new LibCTR.Collections.Vector3(t.AX, t.AY, t.AZ);
                var b = new LibCTR.Collections.Vector3(t.BX, t.BY, t.BZ);
                var c = new LibCTR.Collections.Vector3(t.CX, t.CY, t.CZ);

                // Calculate which side of the triangle the ray approaches.
                var normal = (b - a).Cross(c - a);
                float facing = normal.Dot(dir);

                if (Options.CullFrontFaces && facing < 0f) continue;
                if (Options.CullBackFaces && facing > 0f) continue;

                float? d = Ray3D.Triangle(origin, dir, a, b, c);

                if (d != null && (best == null || d.Value < best.Value))
                    best = d;
            }

            return best;
        }

        private void Compile(ViewPlaneHandler vph)
        {
            // Toward the viewer of each view plane (draw3DVertice puts this axis on the depth that is drawn in front).
            float vx = 0, vy = 0, vz = 0;
            switch (vph.mode)
            {
                case ViewPlaneHandler.PLANE_MODES.XY: vz = 1; break;
                case ViewPlaneHandler.PLANE_MODES.ZY: vx = -1; break;
                default: vy = 1; break;
            }
            if (vph.Is3D) vy = 0;
            listKey = ListKey(vph);
            displayList = Gl.glGenLists(1);
            Gl.glNewList(displayList, Gl.GL_COMPILE_AND_EXECUTE);
            // What karts stand on or are stopped by first; then the rest (falls, triggers, item walls...) without writing
            // depth, so that it hides nothing behind it.
            for (int pass = 0; pass < 2; pass++)
            {
                if (pass == 1) Gl.glDepthMask(Gl.GL_FALSE);
                Gl.glBegin(Gl.GL_TRIANGLES);
                foreach (var t in Triangles)
                {
                    if (hidden[t.Value & 0xFF]) continue;
                    if ((Types[t.Type].IsFloor || Types[t.Type].Kind == Kinds.Wall) != (pass == 0)) continue;
                    float facing = t.NX * vx + t.NY * vy + t.NZ * vz;
                    if ((Options.CullFrontFaces && facing > 0) || (Options.CullBackFaces && facing < 0)) continue;
                    Color c = Types[t.Type].Color;
                    float shade = 0.55f + 0.45f * Math.Abs(t.NY);
                    float alpha = Types[t.Type].Kind == Kinds.NoEffect || Types[t.Type].Kind == Kinds.Trigger || Types[t.Type].Kind == Kinds.ItemWall || Types[t.Type].Kind == Kinds.Fall || Types[t.Type].Kind == Kinds.GliderActivator ? 0.35f : 1.0f;
                    Gl.glColor4f(c.R / 255f * shade, c.G / 255f * shade, c.B / 255f * shade, alpha);
                    var a = new LibCTR.Collections.Vector3(t.AX, t.AY, t.AZ);
                    var b = new LibCTR.Collections.Vector3(t.BX, t.BY, t.BZ);
                    var cc = new LibCTR.Collections.Vector3(t.CX, t.CY, t.CZ);
                    vph.draw3DVertice(a);
                    bool ccw = (b - a).Cross(cc - a).Dot(new LibCTR.Collections.Vector3(t.NX, t.NY, t.NZ)) >= 0;
                    vph.draw3DVertice(ccw ? b : cc);
                    vph.draw3DVertice(ccw ? cc : b);
                }
                Gl.glEnd();
            }
            Gl.glDepthMask(Gl.GL_TRUE);
            Gl.glEndList();
        }

        public void PopulateTreeView(TreeView tree)
        {
            TreeNode root = new TreeNode("Collision (" + FileName + ")") { Tag = this, Checked = isVisible };
            int[] counts = new int[256];
            int[] trickable = new int[32];
            foreach (var t in Triangles)
            {
                counts[t.Value & 0xFF]++;
                if ((t.Value & 0x1000) != 0) trickable[t.Type]++;
            }
            for (int type = 0; type < 32; type++)
            {
                TypeNode info = new TypeNode() { Info = Types[type], Trickable = trickable[type] };
                List<string> variants = new List<string>();
                TreeNode typeNode = new TreeNode() { Tag = info };
                bool anyShown = false;
                for (int variant = 0; variant < 8; variant++)
                {
                    int n = counts[type | (variant << 5)];
                    if (n == 0) continue;
                    info.Triangles += n;
                    VariantNode vn = new VariantNode() { Parent = info, Variant = variant, Name = Types[type].Variants[variant], Triangles = n };
                    variants.Add(variant + " " + vn.Name);
                    bool shown = !IsHidden(type, variant);
                    anyShown |= shown;
                    typeNode.Nodes.Add(new TreeNode(String.Format("Variant {0}: {1} ({2})", variant, vn.Name, n)) { Tag = vn, Checked = shown });
                }
                if (info.Triangles == 0) continue;
                info.Variants = String.Join(", ", variants);
                string name = Types[type].Name;
                if (!name.ToUpperInvariant().Replace(" ", "").StartsWith(Types[type].TemplateName)) name += " / " + Types[type].TemplateName;
                typeNode.Text = String.Format("{0:D2} {1} ({2})", type, name, info.Triangles);
                typeNode.Checked = anyShown;
                Color tint = Types[type].Color;
                typeNode.BackColor = Color.FromArgb(255 - (255 - tint.R) / 3, 255 - (255 - tint.G) / 3, 255 - (255 - tint.B) / 3);
                root.Nodes.Add(typeNode);
            }
            tree.Nodes.Add(root);
        }
    }
}
