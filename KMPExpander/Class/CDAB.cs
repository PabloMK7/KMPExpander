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
                float area;
                public STRMEntry(EndianBinaryReaderEx er)
                {
                    minX = er.ReadSingle();
                    minZ = er.ReadSingle();
                    maxX = er.ReadSingle();
                    maxZ = er.ReadSingle();
                    area = (maxX - minX) * (minZ - maxZ);
                }
                public STRMEntry(Single minx, Single minz, Single maxx, Single maxz)
                {
                    minX = minx;
                    minZ = minz;
                    maxX = maxx;
                    maxZ = maxz;
                    if (Math.Abs(minX -maxX) < 1)
                    {
                        minX += 0.5f;
                        maxX += 0.5f;
                    }
                    if (Math.Abs(minZ - maxZ) < 1)
                    {
                        minZ += 0.5f;
                        maxZ += 0.5f;
                    }
                }
                public Single minX;
                public Single minZ;
                public Single maxX;
                public Single maxZ;
                public bool insideRoute(SimpleKMPs.Routes.RouteGroup grp)
                {
                    List<Extensions.pointInPoly.Point> poly = new List<Extensions.pointInPoly.Point>();
                    foreach(var en in grp.Entries)
                    {
                        poly.Add(new Extensions.pointInPoly.Point(en.PositionX, en.PositionZ));
                    }
                    Extensions.pointInPoly.Point[] arr = poly.ToArray();
                    if (Extensions.pointInPoly.isInside(arr, 4, new Extensions.pointInPoly.Point(minX, minZ)) ||
                        Extensions.pointInPoly.isInside(arr, 4, new Extensions.pointInPoly.Point(minX, maxZ)) ||
                        Extensions.pointInPoly.isInside(arr, 4, new Extensions.pointInPoly.Point(maxX, maxZ)) ||
                        Extensions.pointInPoly.isInside(arr, 4, new Extensions.pointInPoly.Point(maxX, minZ)))
                        return true;
                    return false;
                }
                public void Render(int count, float single)
                {

                    VisualSettings Settings = (Application.OpenForms[0] as Form1).Settings;
                    SimpleKMP kmp = (Application.OpenForms[0] as Form1).getKayEmPee();
                    if (kmp != null)
                    {
                        if (kmp.currentCullingRoutes.Count != 0)
                        {
                            bool isIniside = false;
                            foreach (var i in kmp.currentCullingRoutes)
                            {
                                if (!isIniside && insideRoute(kmp.Routes.Entries[i])) isIniside = true;
                            }
                            if (!isIniside) return;
                        }
                        else if ((Application.OpenForms[0] as Form1).lastSelectedGroup != null && (Application.OpenForms[0] as Form1).lastSelectedGroup.GetType() == typeof(SimpleKMPs.CheckPoints.CheckpointGroup))
                        {
                            return;
                        }
                    }
                    
                    //
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
        internal void Render(bool picking = false)
        {
            if (!isVisible || (Application.OpenForms[0] as Form1).vph.mode != ViewPlaneHandler.PLANE_MODES.XZ) return;
            Gl.glBindTexture(Gl.GL_TEXTURE_2D, 0);
            if (!picking)
            {
                Gl.glDepthFunc(Gl.GL_ALWAYS);
                Gl.glLoadIdentity();
            }
            for (int i = 0; i < Shape.NrStreams; i++) {
                float single = 1.0f / Streams[i].NrEntries;
                for (int j = 0; j < Streams[i].NrEntries; j++)
                {
                    if (!Streams[i].GetVisibility()) continue;
                    Streams[i].Entries[j].Render(j+1, single);
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
                node.Nodes.Add("Group " + i.ToString());
                node.Nodes[i].Tag = group;
                node.Nodes[i].ImageIndex = 18;
                node.Nodes[i].SelectedImageIndex = 18;
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
                        subnode.Checked = ((STRM)subnode.Tag).GetVisibility();
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
            CDAB ret = new CDAB();
            for (int i = 0; i < cmdl.shapes.Count; i++)
            {
                cmdl.shapes[i].indstrm[0].triangles = cmdl.shapes[i].indstrm[0].triangles.OrderBy(tri => tri.getMinZ()).ToList();
                STRM mat = new STRM();
                List<CMDL.CMDLShape.IndexStream.Triangle> zstrip = new List<CMDL.CMDLShape.IndexStream.Triangle>();
                CMDL.CMDLShape.IndexStream.Triangle zseltri = cmdl.shapes[i].indstrm[0].triangles[0];
                int zindex = 0;
                while (true)
                {
                    while (cmdl.shapes[i].indstrm[0].triangles[zindex].getMinZ() - zseltri.getMinZ() <= 1000)
                    {
                        zstrip.Add(cmdl.shapes[i].indstrm[0].triangles[zindex]);
                        zindex++;
                        if (zindex == cmdl.shapes[i].indstrm[0].triangles.Count) break;
                    }
                    zstrip = zstrip.OrderBy(face => face.getMinX()).ToList();
                    List<CMDL.CMDLShape.IndexStream.Triangle> xstrip = new List<CMDL.CMDLShape.IndexStream.Triangle>();
                    CMDL.CMDLShape.IndexStream.Triangle xseltri = zstrip[0];
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
                        cmdl.shapes[i].addNewIndexStream(xstrip);
                        if (xindex == zstrip.Count) break;
                        xseltri = zstrip[xindex];
                        xstrip.Clear();
                    }
                    if (zindex == cmdl.shapes[i].indstrm[0].triangles.Count) break;
                    zseltri = cmdl.shapes[i].indstrm[0].triangles[zindex];
                    zstrip.Clear();
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
            var ext = new List<string> { ".cmdlext", ".ctex", ".cmata", ".cmcla", ".cmtpa", ".cmtsa"};
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
