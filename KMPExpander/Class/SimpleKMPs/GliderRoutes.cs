using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using LibCTR.Collections;
using KMPSections;
using System.Windows.Forms;
using System.ComponentModel;
using Extensions;
using System.Drawing.Design;
using Tao.OpenGl;
using System.Drawing;

namespace KMPExpander.Class.SimpleKMPs
{
    public class GliderRoutes : SectionBase<GliderRoutes.GliderGroup>
    {
        /*[XmlElement("Group")]
        public List<GliderGroup> Groups = new List<GliderGroup>();*/
        public GliderRoutes() { }
        public GliderRoutes(GLPT data, GLPH paths)
        {
            foreach (var path in paths.Entries)
            {
                Entries.Add(new GliderGroup(path, data));
            }
        }

        public class GliderGroup : SectionBase<GliderGroup.GliderEntry>
        {
            [XmlAttribute, Category("Glider Group"), Description("Specifies which previous groups this one is linked to."), Editor(typeof(CustomEditor), typeof(UITypeEditor))]
            public sbyte[] Previous { get; set; }
            [XmlAttribute, Category("Glider Group"), Description("Specifies which next groups this one is linked to."), Editor(typeof(CustomEditor), typeof(UITypeEditor))]
            public sbyte[] Next { get; set; }
            [XmlAttribute, Category("Glider Group"), Browsable(false)]
            public UInt32 RouteSettings { get; set; }
            //
            [XmlAttribute, Category("Glider Group Settings"), Description("Prevents karts from leaving the route and are forced to land after finishing it.")]
            public bool ForceToRoute { get
                {
                    return (RouteSettings & 0xFF) != 0;
                }
                set
                {
                    RouteSettings = (RouteSettings & ~0xFFu) | (value ? 1u : 0u);
                }
            }
            [XmlAttribute, Category("Glider Group Settings"), Description("Cannon route, karts are launched as shoot from a cannon.")]
            public bool CannonSection
            {
                get
                {
                    return (RouteSettings & 0xFF00) != 0;
                }
                set
                {
                    RouteSettings = (RouteSettings & ~0xFF00u) | (value ? 1u : 0u) << 8;
                }
            }
            [XmlAttribute, Category("Glider Group Settings"), Description("Karts won't be able to raise, only move downwards.")]
            public bool PreventRaising
            {
                get
                {
                    return (RouteSettings & 0xFF0000) != 0;
                }
                set
                {
                    RouteSettings = (RouteSettings & ~0xFF0000u) | (value ? 1u : 0u) << 16;
                }
            }
            //
            [XmlAttribute, Category("Glider Group"), TypeConverter(typeof(HexTypeConverter))]
            public UInt32 Unknown2 { get; set; }
            /*[XmlElement("Entry")]
            public List<GliderEntry> Entries = new List<GliderEntry>();*/

            public class GliderEntry
            {
                //public Vector3 Position { get; set; }
                [XmlAttribute]
                public Single PositionX { get; set; }
                [XmlAttribute]
                public Single PositionY { get; set; }
                [XmlAttribute]
                public Single PositionZ { get; set; }
                [XmlAttribute]
                public Single Scale { get; set; }
                [XmlAttribute, TypeConverter(typeof(HexTypeConverter))]
                public UInt32 Unknown1 { get; set; }
                [XmlAttribute, TypeConverter(typeof(HexTypeConverter))]
                public UInt32 Unknown2 { get; set; }

                public GliderEntry() {
                    Scale = 1;
                }
                public GliderEntry(GLPT.GLPTEntry entry)
                {
                    //Position = entry.Position;
                    PositionX = entry.Position.X;
                    PositionY = entry.Position.Y;
                    PositionZ = entry.Position.Z;
                    Scale = entry.Scale;
                    Unknown1 = entry.Unknown1;
                    Unknown2 = entry.Unknown2;
                }

                public GLPT.GLPTEntry ToGLPTEntry()
                {
                    GLPT.GLPTEntry entry = new GLPT.GLPTEntry();
                    //entry.Position = Position;
                    entry.Position = new Vector3(PositionX, PositionY, PositionZ);
                    entry.Scale = Scale;
                    entry.Unknown1 = Unknown1;
                    entry.Unknown2 = Unknown2;
                    return entry;
                }

                public void DrawFilledCircle(float cx, float cy, float r, int num_segments, Color Border, Color Fill)
                {
                    double[] vertexlist = new double[(num_segments + 1) * 2];

                    Gl.glColor4f(Fill.R / 255f, Fill.G / 255f, Fill.B / 255f, Fill.A / (255f * 4f));

                    Gl.glBegin(Gl.GL_TRIANGLE_FAN);
                    Gl.glVertex2d(cx, cy);
                    for (int ii = 0; ii < (num_segments + 1) * 2; ii += 2)
                    {
                        double theta = 2.0f * 3.1415926f * (ii / 2) / num_segments;//get the current angle

                        double x = r * Math.Cos(theta);//calculate the x component
                        double y = r * Math.Sin(theta);//calculate the y component

                        vertexlist[ii] = x + cx;
                        vertexlist[ii + 1] = y + cy;//output vertex
                        Gl.glVertex2d(x + cx, y + cy);//output vertex
                    }
                    Gl.glEnd();

                    Gl.glColor4f(Border.R / 255f, Border.G / 255f, Border.B / 255f, Border.A);

                    Gl.glBegin(Gl.GL_LINE_LOOP);
                    for (int ii = 0; ii < num_segments * 2; ii += 2)
                    {
                        Gl.glVertex2d(vertexlist[ii], vertexlist[ii + 1]);//output vertex
                    }
                    Gl.glEnd();
                }

                public void RenderPicking(int group_id, int entry_id)
                {
                    VisualSettings Settings = (Application.OpenForms[0] as Form1).Settings;
                    Color pickingColor = SectionPicking.GetColor(Sections.GliderRoutes, group_id, entry_id);
                    float PointScale = 50f * Scale;

                    Gl.glPointSize(Settings.PointSize + 2f);
                    Gl.glBegin(Gl.GL_POINTS);
                    Gl.glColor4f(pickingColor.R / 255f, pickingColor.G / 255f, pickingColor.B / 255f, 1f);
                    Gl.glVertex2f(PositionX, PositionZ);
                    Gl.glEnd();

                    Gl.glLineWidth(Settings.LineWidth);
                    DrawFilledCircle(PositionX, PositionZ, PointScale, 24, pickingColor, pickingColor);
                }


                public void RenderPoint()
                {
                    VisualSettings Settings = (Application.OpenForms[0] as Form1).Settings;
                    List<object> SelectedDots = (Application.OpenForms[0] as Form1).SelectedDots;
                    float PointScale = 50f * Scale;

                    Gl.glPointSize(Settings.PointSize + 2f);
                    Gl.glBegin(Gl.GL_POINTS);
                    if (SelectedDots.Contains(this)) Gl.glColor4f(Settings.HighlightPointborderColor.R / 255f, Settings.HighlightPointborderColor.G / 255f, Settings.HighlightPointborderColor.B / 255f, Settings.HighlightPointborderColor.A);
                    else Gl.glColor4f(Settings.PointborderColor.R / 255f, Settings.PointborderColor.G / 255f, Settings.PointborderColor.B / 255f, Settings.PointborderColor.A);
                    Gl.glVertex2f(PositionX, PositionZ);
                    Gl.glEnd();

                    Gl.glPointSize(Settings.PointSize);
                    Gl.glBegin(Gl.GL_POINTS);
                    if (SelectedDots.Contains(this)) Gl.glColor4f(Settings.HighlightPointColor.R / 255f, Settings.HighlightPointColor.G / 255f, Settings.HighlightPointColor.B / 255f, Settings.HighlightPointColor.A);
                    else Gl.glColor4f(Settings.GliderColor.R / 255f, Settings.GliderColor.G / 255f, Settings.GliderColor.B / 255f, Settings.GliderColor.A);
                    Gl.glVertex2f(PositionX, PositionZ);
                    Gl.glEnd();
         
                    Gl.glLineWidth(Settings.LineWidth);
                    if (SelectedDots.Contains(this)) DrawFilledCircle(PositionX, PositionZ, PointScale, 24, Settings.HighlightPointborderColor, Settings.HighlightPointColor);
                    else DrawFilledCircle(PositionX, PositionZ, PointScale, 24, Settings.GliderColor, Settings.GliderColor);
                }

                public void RenderLine()
                {
                    Gl.glVertex2f(PositionX, PositionZ);
                }
            }

            public GliderGroup()
            {
                Next = new sbyte[6];
                Previous = new sbyte[6];
                for (int i = 0; i < Next.Length; i++)
                {
                    Next[i] = -1;
                    Previous[i] = -1;
                }
            }
            public GliderGroup(GLPH.GLPHEntry path, GLPT data)
            {
                Previous = path.Previous;
                Next = path.Next;
                RouteSettings = path.Unknown1;
                Unknown2 = path.Unknown2;
                for (int i = path.Start; i < path.Start + path.Length; i++)
                {
                    Entries.Add(new GliderEntry(data.Entries[i]));
                }
            }

            public GLPH.GLPHEntry ToGLPHEntry(byte start)
            {
                GLPH.GLPHEntry path = new GLPH.GLPHEntry();
                path.Start = start;
                path.Previous = Previous;
                path.Next = Next;
                path.Length = (byte)Entries.Count;
                path.Unknown1 = RouteSettings;
                path.Unknown2 = Unknown2;
                return path;
            }

            public void Render()
            {
                if (!Visible) return;

                VisualSettings Settings = (Application.OpenForms[0] as Form1).Settings;

                if (Settings.LinkPoints)
                {
                    Gl.glColor4f(Settings.GliderLinkColor.R / 255f, Settings.GliderLinkColor.G / 255f, Settings.GliderLinkColor.B / 255f, Settings.GliderLinkColor.A);
                    for (int i = 0; i < Entries.Count - 1; i++)
                    {
                        Gl.glBegin(Gl.GL_LINES);
                        Entries[i].RenderLine();
                        Entries[i + 1].RenderLine();
                        Gl.glEnd();
                    }
                }
                foreach (var entry in Entries)
                    entry.RenderPoint();
            }

            public void RenderPicking(int group_id)
            {
                if (!Visible) return;

                for (int i = 0; i < Entries.Count; i++)
                    Entries[i].RenderPicking(group_id, i);
            }
        }

        public GLPT ToGLPT()
        {
            GLPT data = new GLPT();
            uint Count = 0;
            foreach (var group in Entries)
                foreach (var entry in group.Entries)
                {
                    data.Entries.Add(entry.ToGLPTEntry());
                    Count++;
                }
            data.NrEntries = Count;
            return data;
        }

        public GLPH ToGLPH()
        {
            GLPH path = new GLPH();
            path.NrEntries = (uint)Entries.Count;
            byte start = 0;
            foreach (var group in Entries)
            {
                path.Entries.Add(group.ToGLPHEntry(start));
                start += (byte)group.Entries.Count;
            }
            return path;
        }

        public TreeNode GetTreeNode()
        {
            TreeNode node = new TreeNode("Glider Routes");

            int i = 0;
            foreach (var group in Entries)
            {
                node.Nodes.Add("Group " + i.ToString());
                node.Nodes[i].Tag = group;
                node.Nodes[i].ImageIndex = 13;
                node.Nodes[i].SelectedImageIndex = 13;
                i++;
            }
            return node;
        }

        public void Render(bool picking)
        {
            if (!Visible) return;

            if (picking)
            {
                for (int i = 0; i < Entries.Count; i++)
                    Entries[i].RenderPicking(i);
                return;
            }
            VisualSettings Settings = (Application.OpenForms[0] as Form1).Settings;
            Gl.glLineWidth(Settings.LineWidth);
            foreach (var group in Entries)
            {
                if (Settings.LinkPoints && Settings.LinkPaths && group.Visible)
                {
                    Gl.glColor4f(Settings.RouteLinkColor.R / 255f, Settings.RouteLinkColor.G / 255f, Settings.RouteLinkColor.B / 255f, Settings.RouteLinkColor.A);
                    try
                    {
                        foreach (var next in group.Next)
                            if (next != -1 && Entries[next].Visible)
                            {
                                Gl.glBegin(Gl.GL_LINES);
                                group.Entries[group.Entries.Count - 1].RenderLine();
                                Entries[next].Entries[0].RenderLine();
                                Gl.glEnd();
                            }
                    }
                    catch
                    {
                        Gl.glEnd();
                    }
                }
                group.Render();
            }
        }
    }
}
