using System;
using System.Xml;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using LibCTR.Collections;
using g3;


namespace KMPExpander.Class
{
    public class CMDL
    {
        public XmlDocument xml;
        public List<CMDLShape> shapes;
        public CMDL(string fileName)
        {
            xml = new XmlDocument();
            xml.Load(fileName);
            XmlNode meta = xml.SelectSingleNode("NintendoWareIntermediateFile/GraphicsContentCtr");
            if (meta.Attributes["withkmpe"] != null)
            {
                throw new Exception("This file was already parsed, please use the original one!");
            } else
            {
                (meta as XmlElement).SetAttribute("withkmpe", "true");
            }
            shapes = new List<CMDLShape>();
            XmlNodeList models = xml.SelectNodes("NintendoWareIntermediateFile/GraphicsContentCtr/Models/Model");
            if (models.Count == 0) models = xml.SelectNodes("NintendoWareIntermediateFile/GraphicsContentCtr/Models/SkeletalModel");
            XmlNodeList shps = models[0].SelectNodes("Shapes/SeparateDataShapeCtr");
            for (int i = 0; i < shps.Count; i++)
            {
                shapes.Add(new CMDLShape(shps[i], xml));
            }
        }
        public void Save(string fileN)
        {
            File.WriteAllText(fileN, Beautify(xml).Replace("&gt;", ">").Replace("&lt;", "<")); //Sigh... 
        }
        static public string Beautify(XmlDocument doc)
        {
            MemoryStream memStream = new MemoryStream();
            XmlWriterSettings settings = new XmlWriterSettings
            {
                Indent = true,
                IndentChars = "\t",
                NewLineChars = "\r\n",
                NewLineHandling = NewLineHandling.Replace,
                Encoding = Encoding.UTF8
            };
            using (XmlWriter writer = XmlWriter.Create(memStream, settings))
            {
                doc.Save(writer);
            }
            return Encoding.UTF8.GetString(memStream.ToArray());
        }
        public void fixStreamCount()
        {
            int count = 0;
            foreach (var s in shapes)
            {
                count += s.indstrm.Count;
            }
            XmlNodeList notes = xml.SelectNodes("NintendoWareIntermediateFile/GraphicsContentCtr/EditData/ContentSummaryMetaData/Values/ContentSummary/ObjectSummaries/ObjectSummary/Notes/Note");
            for (int i = 0; i < notes.Count; i++)
            {
                XmlNode e = notes[i];
                if (e.Attributes["Name"] != null && e.Attributes["Name"].Value.Equals("TotalIndexStreamCount"))
                {
                    (e as XmlElement).SetAttribute("Value", count.ToString());
                }
            }
        }
        public class CMDLShape {
            public XmlNode shapeNode;
            public XmlDocument xml;
            public List<Vector3> Vertices;
            string VerticesString;
            public XmlNodeList indexstreamlist;
            public List<IndexStream> indstrm;
            public CMDLShape(XmlNode s, XmlDocument x) {
                shapeNode = s;
                xml = x;
                Vertices = new List<Vector3>();
                VerticesString = s.SelectSingleNode("VertexAttributes/Vector3VertexStreamCtr").InnerText;
                strToList();
                indexstreamlist = s.SelectNodes("PrimitiveSets/PrimitiveSetCtr/Primitives/PrimitiveCtr/IndexStreams/*");
                mergeIndexStreamList();
                indstrm = new List<IndexStream>();
                indstrm.Add(new IndexStream(indexstreamlist[0], Vertices));
                indexstreamlist[0].ParentNode.RemoveAll(); //Prepare for CDAB parsing
            }
            public void addNewIndexStream(List<IndexStream.Triangle> l)
            {
                IndexStream tri = new IndexStream(l);
                tri.fixOrientedBox();
                indstrm.Add(tri);
                XmlNode indexstreamparent = shapeNode.SelectSingleNode("PrimitiveSets/PrimitiveSetCtr/Primitives/PrimitiveCtr/IndexStreams");
                indexstreamparent.AppendChild(tri.generateNode(xml, Vertices.Count));
            }
            public void mergeIndexStreamList()
            {
                if (indexstreamlist.Count == 1)
                {
                    return;
                }
                else throw new Exception("Stream list has invalid number of elements!");
            }
            public void strToList()
            {
                Vertices.Clear();
                string[] array = VerticesString.Split("\r\n".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
                foreach (string line in array)
                {
                    string[] v = line.Split(" ".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
                    if (v.Count() != 3)
                    {
                        throw new Exception("Invalid vertex data.");
                    }
                    Vertices.Add(new Vector3(float.Parse(v[0].Replace('.', ',')), float.Parse(v[1].Replace('.', ',')), float.Parse(v[2].Replace('.', ','))));
                }
            }
            public void listToStr()
            {
                VerticesString = "";
                foreach (var v in Vertices)
                {
                    VerticesString += v.X.ToString().Replace(',', '.');
                    VerticesString += v.Y.ToString().Replace(',', '.');
                    VerticesString += v.Z.ToString().Replace(',', '.');
                    VerticesString += "\r\n";
                }
            }
            public class IndexStream {
                public List<Triangle> triangles;
                public g3.ContOrientedBox3 orbox;
                public IndexStream(XmlNode n, List<Vector3> vertices)
                {
                    triangles = new List<Triangle>();
                    orbox = null;
                    foreach(var s in SplitBy(n.InnerText, ' ', 3))
                    {
                        triangles.Add(new Triangle(s, vertices));
                    }
                }
                public IndexStream (List<Triangle> newtri)
                {
                    triangles = newtri;
                    orbox = null;
                }
                public void fixOrientedBox()
                {
                    List<g3.Vector3d> points = new List<Vector3d>();
                    foreach(var t in triangles)
                    {
                        foreach(var v in t.vtx)
                        {
                            points.Add(new Vector3d(v.X, v.Y, v.Z));
                        }
                    }
                    orbox = new ContOrientedBox3(points);
                }
                public IEnumerable<string> SplitBy(string input, char separator, int n)
                {
                    int lastindex = 0;
                    int curr = 0;

                    while (curr < input.Length)
                    {
                        int count = 0;
                        while (curr < input.Length && count < n)
                        {
                            if (input[curr++] == separator) count++;
                        }
                        yield return input.Substring(lastindex, curr - lastindex - (curr < input.Length ? 1 : 0));
                        lastindex = curr;
                    }
                }
                public XmlNode generateNode(XmlDocument xml, int vertexcount)
                {
                    string name;
                    if (vertexcount < 256) name = "UbyteIndexStreamCtr";
                    else name = "UshortIndexStreamCtr";
                    XmlNode ret = xml.CreateNode("element", name, "");
                    (ret as XmlElement).SetAttribute("PrimitiveMode", "Triangles");
                    (ret as XmlElement).SetAttribute("Size", (triangles.Count * 3).ToString());
                    string facedata = "";
                    foreach (var t in triangles)
                    {
                        facedata += t.getString();
                    }
                    facedata = facedata.Trim();
                    XmlElement orboundbox = xml.CreateElement("OrientedBoundingBox");
                    XmlElement centerpos = xml.CreateElement("CenterPosition");
                    XmlElement ormat = xml.CreateElement("OrientationMatrix");
                    XmlElement size = xml.CreateElement("Size");
                    orboundbox.AppendChild(centerpos);
                    orboundbox.AppendChild(ormat);
                    orboundbox.AppendChild(size);
                    centerpos.SetAttribute("X", orbox.Box.Center.x.ToString().Replace(',','.'));
                    centerpos.SetAttribute("Y", orbox.Box.Center.y.ToString().Replace(',', '.'));
                    centerpos.SetAttribute("Z", orbox.Box.Center.z.ToString().Replace(',', '.'));
                    ormat.SetAttribute("M00", orbox.Box.AxisX.x.ToString().Replace(',', '.'));
                    ormat.SetAttribute("M01", orbox.Box.AxisX.y.ToString().Replace(',', '.'));
                    ormat.SetAttribute("M02", orbox.Box.AxisX.z.ToString().Replace(',', '.'));
                    ormat.SetAttribute("M10", orbox.Box.AxisY.x.ToString().Replace(',', '.'));
                    ormat.SetAttribute("M11", orbox.Box.AxisY.y.ToString().Replace(',', '.'));
                    ormat.SetAttribute("M12", orbox.Box.AxisY.z.ToString().Replace(',', '.'));
                    ormat.SetAttribute("M20", orbox.Box.AxisZ.x.ToString().Replace(',', '.'));
                    ormat.SetAttribute("M21", orbox.Box.AxisZ.y.ToString().Replace(',', '.'));
                    ormat.SetAttribute("M22", orbox.Box.AxisZ.z.ToString().Replace(',', '.'));
                    size.SetAttribute("X", orbox.Box.Extent.x.ToString().Replace(',', '.'));
                    size.SetAttribute("Y", orbox.Box.Extent.y.ToString().Replace(',', '.'));
                    size.SetAttribute("Z", orbox.Box.Extent.z.ToString().Replace(',', '.'));
                    ret.InnerText = orboundbox.OuterXml + facedata;
                    return ret;
                }
                public class Triangle {
                    public List<Vector3> vtx;
                    public List<int> vtxids;
                    public Triangle(string s, List<Vector3> vertices)
                    {
                        vtx = new List<Vector3>();
                        vtxids = new List<int>();
                        string[] sp = s.Split(' ');
                        foreach(var spi in sp)
                        {
                            vtx.Add(vertices[int.Parse(spi)]);
                            vtxids.Add(int.Parse(spi));
                        }
                    }
                    public string getString()
                    {
                        return String.Format(" {0} {1} {2}", vtxids[0], vtxids[1], vtxids[2]);
                    }
                    public float getMinX()
                    {
                        float min = float.MaxValue;
                        foreach (var v in vtx)
                        {
                            if (min > v.X) min = v.X;
                        }
                        return min;
                    }
                    public float getMinZ()
                    {
                        float min = float.MaxValue;
                        foreach (var v in vtx)
                        {
                            if (min > v.Z) min = v.Z;
                        }
                        return min;
                    }
                    public float getMaxX()
                    {
                        float max = float.MinValue;
                        foreach (var v in vtx)
                        {
                            if (max < v.X) max = v.X;
                        }
                        return max;
                    }
                    public float getMaxZ()
                    {
                        float max = float.MinValue;
                        foreach (var v in vtx)
                        {
                            if (max < v.Z) max = v.Z;
                        }
                        return max;
                    }
                }
            }
        }
    }
}
