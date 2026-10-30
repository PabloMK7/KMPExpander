using LibCTR.Collections;
using LibEndianBinaryIO;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Diagnostics;
using Tao.OpenGl;
using Extensions;

namespace KMPExpander.Class
{
	public class CDAB
	{
        public CDAB()
        {
            Header = new CDABHeader();
            Shape = new SHAP();
            Streams = new List<STRM>();
            isVisible = true;
        }
		public CDAB(byte[] Data)
		{
			EndianBinaryReaderEx er = new EndianBinaryReaderEx(new MemoryStream(Data), Endianness.LittleEndian);
			try
			{
				Header = new CDABHeader(er);
				Shape = new SHAP(er);
				Streams = new List<STRM>();
				for (int i = 0; i < Shape.NrStreams; i++)
				{
					Streams.Add(new STRM(er));
				}
			}
			finally
			{
				er.Close();
			}
            isVisible = true;
        }
		public CDABHeader Header;
		public class CDABHeader
		{
            public CDABHeader()
            {
                Signature = "BADC";
                FileSize = 0;
                HeaderSize = 16;
                Unknown = 1000;
            }
            public CDABHeader(EndianBinaryReaderEx er)
            {
                Signature = er.ReadString(Encoding.ASCII, 4);
                if (Signature != "BADC") throw new FormatException("Invalid signature: " + Signature);
                FileSize = er.ReadUInt32();
                HeaderSize = er.ReadUInt32();
                Unknown = er.ReadUInt32();
            }
            public void Write(EndianBinaryWriter er)
            {
                er.Write(Signature, Encoding.ASCII, false);
                er.Write(FileSize);
                er.Write(HeaderSize);
                er.Write(Unknown);
            }
			public String Signature;
			public UInt32 FileSize;
			public UInt32 HeaderSize;
			public UInt32 Unknown;
		}
		public SHAP Shape;
		public class SHAP
		{
            public SHAP()
            {
                Signature = "PAHS";
                NrStreams = 0;
            }
			public SHAP(EndianBinaryReaderEx er)
			{
                Signature = er.ReadString(Encoding.ASCII, 4);
                if (Signature != "PAHS") throw new FormatException("Invalid signature: " + Signature);
                NrStreams = er.ReadUInt32();
            }
            public void Write(EndianBinaryWriter er)
            {
                er.Write(Signature, Encoding.ASCII, false);
                er.Write(NrStreams);
            }
			public String Signature;
			public UInt32 NrStreams;
		}
		public List<STRM> Streams;
		public class STRM
		{
			public STRM(EndianBinaryReaderEx er)
			{
                Signature = er.ReadString(Encoding.ASCII, 4);
                if (Signature != "MRTS") throw new FormatException("Invalid signature: " + Signature);
                NrEntries = er.ReadUInt16();
                VertexCount = er.ReadUInt16();
                isVisible = true;
                Entries = new List<STRMEntry>();
				for (int i = 0; i < NrEntries; i++)
				{
                    Entries.Add(new STRMEntry(er));
				}
			}
            public STRM()
            {
                Signature = "MRTS";
                NrEntries = 0;
                isVisible = true;
                VertexCount = 0;
                Entries = new List<STRMEntry>();
            }
			public String Signature;
			public UInt16 NrEntries;
			public UInt16 VertexCount;
			public List<STRMEntry> Entries;
            private bool isVisible;
            public bool GetVisibility() {
                return isVisible;
            }
            public void SetVisibility(bool visible)
            {
                isVisible = visible;
            }
            public void addSTRMEntry(STRMEntry e)
            {
                NrEntries++;
                Entries.Add(e);
            }
            public void Write(EndianBinaryWriter er)
            {
                er.Write(Signature, Encoding.ASCII, false);
                er.Write(NrEntries);
                er.Write(VertexCount);
                foreach(var e in Entries)
                {
                    e.Write(er);
                }
            }
            public class STRMEntry
            {
                public float area;
                public STRMEntry(EndianBinaryReaderEx er)
                {
                    minX = er.ReadSingle();
                    minZ = er.ReadSingle();
                    maxX = er.ReadSingle();
                    maxZ = er.ReadSingle();
                    area = (maxX - minX) * (maxZ - minZ);
                    isVisible = true;
                }
                public STRMEntry(Single minx, Single minz, Single maxx, Single maxz)
                {
                    minX = minx;
                    minZ = minz;
                    maxX = maxx;
                    maxZ = maxz;
                }
                public Single minX;
                public Single minZ;
                public Single maxX;
                public Single maxZ;
                public void Render()
                {
                    if (!GetVisibility()) return;

                    VisualSettings Settings = (Application.OpenForms[0] as Form1).Settings;
                    Gl.glColor4f(Settings.DivColor.R / 255f, Settings.DivColor.G / 255f, Settings.DivColor.B / 255f, Settings.DivColor.A);
                    Gl.glLineWidth(Settings.LineWidth / 2);
                    Gl.glBegin(Gl.GL_LINES);
                    Gl.glVertex2f(minX, minZ);
                    Gl.glVertex2f(minX, maxZ);
                    Gl.glEnd();
                    Gl.glBegin(Gl.GL_LINES);
                    Gl.glVertex2f(minX, maxZ);
                    Gl.glVertex2f(maxX, maxZ);
                    Gl.glEnd();
                    Gl.glBegin(Gl.GL_LINES);
                    Gl.glVertex2f(maxX, maxZ);
                    Gl.glVertex2f(maxX, minZ);
                    Gl.glEnd();
                    Gl.glBegin(Gl.GL_LINES);
                    Gl.glVertex2f(maxX, minZ);
                    Gl.glVertex2f(minX, minZ);
                    Gl.glEnd();
                }
                public void Write(EndianBinaryWriter er)
                {
                    er.Write(minX);
                    er.Write(minZ);
                    er.Write(maxX);
                    er.Write(maxZ);
                }

                private bool isVisible;
                public bool GetVisibility()
                {
                    return isVisible;
                }
                public void SetVisibility(bool visible)
                {
                    isVisible = visible;
                }
            }
		}
        public void addSTRM(STRM s)
        {
            Shape.NrStreams++;
            Streams.Add(s);
        }
        public void fixFileSize()
        {
            UInt32 newfilesize = 0;
            newfilesize += 16u; //Header
            newfilesize += 8u; //SHAP header
            newfilesize += 8u * Shape.NrStreams; //STRM headers
            foreach(var s in Streams)
            {
                newfilesize += 16u * s.NrEntries; //STRM entries
            }
            Header.FileSize = newfilesize;
        }

        // How the game culls the course model: it builds a quadtree of depth 3 over the XZ bounding box of all the div
        // entries, and draws an entry while one of the 8x8 cells of the last level that its rectangle touches is
        // visible (see CullingPreview). A clip object (0x1E) hides, while the current checkpoint has its clip ID, the
        // cells that lie wholly inside the quad of its route.
        public const int GridCells = CullingPreview.GridCells;

        public bool GetGrid(out float minX, out float minZ, out float cellW, out float cellH)
        {
            minX = float.MaxValue; minZ = float.MaxValue;
            float maxX = float.MinValue, maxZ = float.MinValue;
            foreach (var s in Streams)
            {
                foreach (var e in s.Entries)
                {
                    if (minX > e.minX) minX = e.minX;
                    if (minZ > e.minZ) minZ = e.minZ;
                    if (maxX < e.maxX) maxX = e.maxX;
                    if (maxZ < e.maxZ) maxZ = e.maxZ;
                }
            }
            cellW = (maxX - minX) / GridCells;
            cellH = (maxZ - minZ) / GridCells;
            return minX <= maxX;
        }

        // The game's test: each edge of the quad (route points 0-1, 1-2, 2-3, 3-0) is a line, and the inside is the
        // side of the first route point that is not on that edge. The quad should be convex.
        public static bool RectInsideQuad(float minX, float minZ, float maxX, float maxZ, SimpleKMPs.Routes.RouteGroup grp)
        {
            float[] px = new float[4], pz = new float[4];
            for (int i = 0; i < 4; i++)
            {
                px[i] = grp.Entries[i].PositionX;
                pz[i] = grp.Entries[i].PositionZ;
            }
            float[] cx = { minX, maxX, minX, maxX };
            float[] cz = { minZ, minZ, maxZ, maxZ };
            for (int i = 0; i < 4; i++)
            {
                int j = (i + 1) % 4;
                int k = 0;
                while (k == i || k == j) k++;
                float nx = pz[j] - pz[i], nz = px[i] - px[j];
                if (nx * (px[k] - px[i]) + nz * (pz[k] - pz[i]) > 0)
                {
                    nx = -nx;
                    nz = -nz;
                }
                for (int c = 0; c < 4; c++)
                {
                    if ((cx[c] - px[i]) * nx + (cz[c] - pz[i]) * nz > 0) return false;
                }
            }
            return true;
        }

        internal void Render(bool picking = false)
        {
            if (!isVisible || (Application.OpenForms[0] as Form1).vph.mode != ViewPlaneHandler.PLANE_MODES.XZ || (Application.OpenForms[0] as Form1).vph.Is3D) return;
            Gl.glBindTexture(Gl.GL_TEXTURE_2D, 0);
            if (!picking)
            {
                Gl.glDepthFunc(Gl.GL_ALWAYS);
                Gl.glLoadIdentity();
            }
            // With a checkpoint selected, only the entries its clip ID hides are drawn, over the cells it hides. With the
            // kart simulator, only the entries the game would draw for that kart.
            Form1 form = Application.OpenForms[0] as Form1;
            CullingPreview culling = form.Culling;
            bool simulating = culling != null && culling.Simulating;
            if (culling == null || culling.HiddenCells == null)
            {
                if (form.getKayEmPee() != null && form.lastSelectedGroup != null && form.lastSelectedGroup.GetType() == typeof(SimpleKMPs.CheckPoints.CheckpointGroup)) return;
                culling = null;
            }
            VisualSettings Settings = form.Settings;
            if (!picking && form.Culling != null) form.Culling.RenderGrid(Settings.DivColor.R / 255f, Settings.DivColor.G / 255f, Settings.DivColor.B / 255f);
            for (int i = 0; i < Shape.NrStreams; i++) {
                for (int j = 0; j < Streams[i].NrEntries; j++)
                {
                    if (!Streams[i].GetVisibility()) continue;
                    var e = Streams[i].Entries[j];
                    if (culling != null && culling.RectHidden(e.minX, e.minZ, e.maxX, e.maxZ) != !simulating) continue;
                    e.Render();
                }
            }
        }
        public byte[] Write()
        {
            MemoryStream m = new MemoryStream();
            EndianBinaryWriter er = new EndianBinaryWriter(m, Endianness.LittleEndian);
            Header.Write(er);
            Shape.Write(er);
            foreach(var s in Streams)
            {
                s.Write(er);
            }
            byte[] result = m.ToArray();
            er.Close();
            return result;
        }
        public TreeNode GetTreeNode()
        {
            TreeNode node = new TreeNode("Div Sections");
            node.ImageIndex = 17;
            node.SelectedImageIndex = 17;
            node.Tag = this;
            int i = 0;
            foreach (var group in Streams)
            {
                node.Nodes.Add("Group " + i.ToString() + " (" + group.VertexCount.ToString() + "v)");
                node.Nodes[i].Tag = group;
                node.Nodes[i].ImageIndex = 18;
                node.Nodes[i].SelectedImageIndex = 18;
                int j = 0;
                foreach (var entry in group.Entries)
                {
                    node.Nodes[i].Nodes.Add("Entry " + j.ToString());
                    node.Nodes[i].Nodes[j].Tag = entry;
                    j++;
                }
                i++;
            }
            return node;
        }
        private bool isVisible;
        public bool GetVisibility()
        {
            return isVisible;
        }
        public void SetVisibility(bool visible)
        {
            isVisible = visible;
        }
        public void PopulateTreeView(TreeView tv)
        {
            tv.Nodes.Add(GetTreeNode());
            foreach (TreeNode node in tv.Nodes)
            {
                if (node.Tag is CDAB)
                {
                    node.Checked = ((CDAB)node.Tag).GetVisibility();
                    foreach (TreeNode subnode in node.Nodes)
                    {
                        subnode.Checked = ((STRM)subnode.Tag).GetVisibility();
                        foreach (TreeNode subsubnode in subnode.Nodes)
                        {
                            subsubnode.Checked = ((STRM.STRMEntry)subsubnode.Tag).GetVisibility();
                        }
                    }
                }
            }
        }
        public static STRM.STRMEntry calculateSTRMEntry(List<CMDL.CMDLShape.IndexStream.Triangle> l)
        {
            float minX = float.MaxValue, maxX = float.MinValue, minZ = float.MaxValue, maxZ = float.MinValue;
            foreach(var tri in l)
            {
                float triminX = tri.getMinX(), trimaxX = tri.getMaxX(), triminZ = tri.getMinZ(), trimaxZ = tri.getMaxZ();
                if (minX > triminX) minX = triminX;
                if (minZ > triminZ) minZ = triminZ;
                if (maxX < trimaxX) maxX = trimaxX;
                if (maxZ < trimaxZ) maxZ = trimaxZ;
            }
            return new STRM.STRMEntry(minX, minZ, maxX, maxZ);
        }
        private static int gridIndex(float v, float min, float cellSize)
        {
            if (cellSize <= 0) return 0;
            int i = (int)Math.Floor((v - min) / cellSize);
            return Math.Max(0, Math.Min(GridCells - 1, i));
        }
        public static CDAB createFromCmdl(string Filename, string saveFilename)
        {
            CMDL cmdl = null;
            try
            {
                cmdl = new CMDL(Filename);
            } catch(Exception e)
            {
                MessageBox.Show(e.Message);
                return null;
            }
            // Like the retail div files: an 8x8 grid over the XZ bounding box of the whole model, the same grid the game
            // culls with (GridCells), and one entry per mesh and cell. A triangle goes to the cell that holds the centre
            // of its XZ bounding box; the entry is the bounding box of its triangles.
            float minX = float.MaxValue, minZ = float.MaxValue, maxX = float.MinValue, maxZ = float.MinValue;
            foreach (var shape in cmdl.shapes)
            {
                foreach (var tri in shape.indstrm[0].triangles)
                {
                    if (minX > tri.getMinX()) minX = tri.getMinX();
                    if (minZ > tri.getMinZ()) minZ = tri.getMinZ();
                    if (maxX < tri.getMaxX()) maxX = tri.getMaxX();
                    if (maxZ < tri.getMaxZ()) maxZ = tri.getMaxZ();
                }
            }
            float cellW = (maxX - minX) / GridCells, cellH = (maxZ - minZ) / GridCells;
            CDAB ret = new CDAB();
            for (int i = 0; i < cmdl.shapes.Count; i++)
            {
                STRM mat = new STRM();
                List<CMDL.CMDLShape.IndexStream.Triangle>[] cells = new List<CMDL.CMDLShape.IndexStream.Triangle>[GridCells * GridCells];
                foreach (var tri in cmdl.shapes[i].indstrm[0].triangles)
                {
                    int ix = gridIndex((tri.getMinX() + tri.getMaxX()) / 2, minX, cellW);
                    int iz = gridIndex((tri.getMinZ() + tri.getMaxZ()) / 2, minZ, cellH);
                    int c = iz * GridCells + ix;
                    if (cells[c] == null) cells[c] = new List<CMDL.CMDLShape.IndexStream.Triangle>();
                    cells[c].Add(tri);
                }
                foreach (var cell in cells)
                {
                    if (cell == null) continue;
                    mat.addSTRMEntry(calculateSTRMEntry(cell));
                    cmdl.shapes[i].addNewIndexStream(cell);
                }
                int vtxcnt = cmdl.shapes[i].Vertices.Count;
                if (vtxcnt > UInt16.MaxValue) throw new Exception("Some material has more than 65535 vertices.");
                mat.VertexCount = (UInt16)vtxcnt;
                ret.addSTRM(mat);
            }
            ret.fixFileSize();
            cmdl.fixStreamCount();
            cmdl.Save(Filename + "ext");
            SaveBcmdl(Filename, saveFilename);
            return ret;
        }
        public static void SaveBcmdl(string Filename, string saveFileName)
        {
            string parfolder = Directory.GetParent(Filename).FullName;
            var ext = new List<string> { ".cmdlext", ".ctex", ".cmata", ".cmcla", ".cmtpa", ".cmtsa", ".cenv"};
            List<string> myFiles = Directory.GetFiles(parfolder, "*.*", SearchOption.AllDirectories).Where(s => ext.Contains(Path.GetExtension(s))).ToList();
            string nw4cRoot = Environment.GetEnvironmentVariable("NW4C_ROOT");
            if (nw4cRoot == null) { MessageBox.Show("Cannot export to .bcmdl (NW4C not found)."); return; }
            string toolExe = Path.Combine(nw4cRoot, "h3d\\tool\\bin\\CreativeStudio\\NW4C_CreativeStudioConsole.exe");
            if (!File.Exists(toolExe)) { MessageBox.Show("Cannot export to .bcmdl (\"" + toolExe + "\" doesn't exist)."); return; }
            for (int i = 0; i < myFiles.Count; i++)
            {
                myFiles[i] = "\"" + myFiles[i] + "\"";
            }

            Process process = new Process();
            ProcessStartInfo startInfo = new ProcessStartInfo();
            //startInfo.WindowStyle = ProcessWindowStyle.Hidden;
            startInfo.FileName = "cmd.exe";
            startInfo.Arguments = String.Format("/C {0} --auto_create_shader=off --index_stream_bounding_type=obb --output=\"{1}\" {2}", toolExe, saveFileName, string.Join(" ", myFiles));
            process.StartInfo = startInfo;
            process.Start();
        }
        /*public static CDAB createFromObj(byte[] Data)
        {
            Wavefront.ExGeom obj = new Wavefront.ExGeom(new CommonFiles.OBJ(Data));
            Dictionary<string, List<Wavefront.ExGeom.LinkedTriangle>> backup = new Dictionary<string, List<Wavefront.ExGeom.LinkedTriangle>>();
            foreach(var faces in obj.faces)
            {
                backup.Add(faces.Key, null);
                backup[faces.Key] = obj.faces[faces.Key].OrderBy(f => f.getMinZ()).ToList();
            }
            obj.faces = backup;
            CDAB ret = new CDAB();
            foreach(var f in obj.faces.Values)
            {
                STRM mat = new STRM();
                List<Wavefront.ExGeom.LinkedTriangle> zstrip = new List<Wavefront.ExGeom.LinkedTriangle>();
                Wavefront.ExGeom.LinkedTriangle zseltri = f[0];
                int zindex = 0;
                while (true)
                {
                    while (f[zindex].getMinZ() - zseltri.getMinZ() <= 1000)
                    {
                        zstrip.Add(f[zindex]);
                        zindex++;
                        if (zindex == f.Count) break;
                    }
                    zstrip = zstrip.OrderBy(face => face.getMinX()).ToList();
                    List<Wavefront.ExGeom.LinkedTriangle> xstrip = new List<Wavefront.ExGeom.LinkedTriangle>();
                    Wavefront.ExGeom.LinkedTriangle xseltri = zstrip[0];
                    int xindex = 0;
                    while (true)
                    {
                        while (zstrip[xindex].getMinX() - xseltri.getMinX() <= 1000)
                        {
                            xstrip.Add(zstrip[xindex]);
                            xindex++;
                            if (xindex == zstrip.Count) break;
                        }
                        mat.addSTRMEntry(calculateSTRMEntry(xstrip));
                        if (xindex == zstrip.Count) break;
                        xseltri = zstrip[xindex];
                        xstrip.Clear();
                    }
                    if (zindex == f.Count) break;
                    zseltri = f[zindex];
                    zstrip.Clear();
                }
                int vtxcnt = Wavefront.ExGeom.getUniqueVertexCount(f);
                if (vtxcnt > UInt16.MaxValue) throw new Exception("Some material has more than 65535 vertices.");
                mat.VertexCount = (UInt16)vtxcnt;
                ret.addSTRM(mat);
            }
            ret.fixFileSize();
            return ret;
        }*/
    }
}
