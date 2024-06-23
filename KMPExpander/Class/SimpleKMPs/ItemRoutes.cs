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
    public class ItemRoutes : SectionBase<ItemRoutes.ItemGroup>
    {
        //[XmlElement("Group")]
        //public List<ItemGroup> Groups = new List<ItemGroup>();
        public ItemRoutes() {
        }
        public ItemRoutes(ITPT data, ITPH paths)
        {
            foreach (var path in paths.Entries)
            {
                Entries.Add(new ItemGroup(path, data));
            }
        }

        public class ItemGroup : SectionBase<ItemGroup.ItemEntry>
        {
            [XmlAttribute, Category("Item Group"), Description("Specifies which previous groups this one is linked to."), Editor(typeof(CustomEditor), typeof(UITypeEditor))]
            public Int16[] Previous { get; set; }
            [XmlAttribute, Category("Item Group"), Description("Specifies which next groups this one is linked to."), Editor(typeof(CustomEditor), typeof(UITypeEditor))]
            public Int16[] Next { get; set; }
            //[XmlAttribute]
            //public List<ItemEntry> Entries = new List<ItemEntry>();

            public class ItemEntry
            {
                //public Vector3 Position { get; set; }
                [XmlIgnore, Browsable(false)]
                public Vector3 Pos { get; set; } = new Vector3(0, 0, 0);
                [XmlAttribute]
                public Single PositionX
                {
                    get
                    {
                        return Pos.X;
                    }
                    set
                    {
                        Pos = new Vector3(value, Pos.Y, Pos.Z);
                    }
                }
                [XmlAttribute]
                public Single PositionY
                {
                    get
                    {
                        return Pos.Y;
                    }
                    set
                    {
                        Pos = new Vector3(Pos.X, value, Pos.Z);
                    }
                }
                [XmlAttribute]
                public Single PositionZ
                {
                    get
                    {
                        return Pos.Z;
                    }
                    set
                    {
                        Pos = new Vector3(Pos.X, Pos.Y, value);
                    }
                }
                [XmlAttribute]
                public Single Scale { get; set; }
                [Browsable(false), XmlAttribute]
                public UInt16 FlyModeVal { get; set; }
                [XmlIgnore]
                public string GravityMode {
                    get {
                        switch (FlyModeVal)
                        {
                            case 0:
                                return "0 - Affected By Gravity";
                            case 1:
                                return "1 - Unaffected By Gravity";
                            case 2:
                                return "2 - Cannon Section";
                            default:
                                return FlyModeVal + " - Unknown";
                        }
                    }
                    set {
                        UInt16 val = UInt16.Parse(value);
                        FlyModeVal = val;
                    }
                }
                [Browsable(false), XmlAttribute]
                public UInt16 PlayerScanRadiusVal { get; set; }
                [XmlIgnore]
                public string PlayerScanRadius
                {
                    get
                    {
                        switch (PlayerScanRadiusVal)
                        {
                            case 0:
                                return "0 - Small";
                            case 1:
                                return "1 - Big";
                            default:
                                return PlayerScanRadiusVal + " - Unknown";
                        }
                    }
                    set
                    {
                        UInt16 val = UInt16.Parse(value);
                        PlayerScanRadiusVal = val;
                    }
                }

                public ItemEntry() {
                    Scale = 1;
                }
                public ItemEntry(ITPT.ITPTEntry entry)
                {
                    //Position = entry.Position;
                    PositionX = entry.Position.X;
                    PositionY = entry.Position.Y;
                    PositionZ = entry.Position.Z;
                    Scale = entry.Scale;
                    FlyModeVal = entry.FlyModeVal;
                    PlayerScanRadiusVal = entry.PlayerScanRadiusVal;
                }

                public ITPT.ITPTEntry ToITPTEntry()
                {
                    ITPT.ITPTEntry entry = new ITPT.ITPTEntry();
                    entry.Position = new Vector3(PositionX,PositionY, PositionZ);
                    entry.Scale = Scale;
                    entry.FlyModeVal = FlyModeVal;
                    entry.PlayerScanRadiusVal = PlayerScanRadiusVal;
                    return entry;
                }

                public void DrawStrippedCircle(float cx, float cy, float r, int num_segments, Color Border)
                {
                    if (num_segments % 2 != 0) num_segments++;
                    double[] vertexlist = new double[(num_segments + 1) * 2];
                    
                    for (int ii = 0; ii < (num_segments + 1) * 2; ii += 2)
                    {
                        double theta = 2.0f * 3.1415926f * (ii / 2) / num_segments;//get the current angle

                        double x = r * Math.Cos(theta);//calculate the x component
                        double y = r * Math.Sin(theta);//calculate the y component

                        vertexlist[ii] = x + cx;
                        vertexlist[ii + 1] = y + cy;//output vertex
                    }

                    Gl.glColor4f(Border.R / 255f, Border.G / 255f, Border.B / 255f, Border.A);

                    Gl.glBegin(Gl.GL_LINE_LOOP);
                    for (int ii = 0; ii < num_segments * 2; ii += 4)
                    {
                        Gl.glBegin(Gl.GL_LINE_LOOP);
                        Gl.glVertex2d(vertexlist[ii], vertexlist[ii + 1]);//output vertex
                        Gl.glVertex2d(vertexlist[ii + 2], vertexlist[ii + 3]);//output vertex
                        Gl.glEnd();
                    }
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
                    ViewPlaneHandler vph = (Application.OpenForms[0] as Form1).vph;

                    VisualSettings Settings = (Application.OpenForms[0] as Form1).Settings;
                    Color pickingColor = SectionPicking.GetColor(Sections.ItemRoutes, group_id, entry_id);
                    float PointScale = 50f * Scale;

                    Gl.glPointSize(Settings.PointSize + 2f);
                    Gl.glBegin(Gl.GL_POINTS);
                    Gl.glColor4f(pickingColor.R / 255f, pickingColor.G / 255f, pickingColor.B / 255f, 1f);
                    vph.draw2DVertice(Pos);
                    Gl.glEnd();
                }

                public void RenderPoint()
                {
                    ViewPlaneHandler vph = (Application.OpenForms[0] as Form1).vph;

                    VisualSettings Settings = (Application.OpenForms[0] as Form1).Settings;
                    List<object> SelectedDots = (Application.OpenForms[0] as Form1).SelectedDots;
                    float PointScale = 50f * Scale;

                    Gl.glPointSize(Settings.PointSize + 2f);
                    Gl.glBegin(Gl.GL_POINTS);
                    if (SelectedDots.Contains(this)) Gl.glColor4f(Settings.HighlightPointborderColor.R / 255f, Settings.HighlightPointborderColor.G / 255f, Settings.HighlightPointborderColor.B / 255f, Settings.HighlightPointborderColor.A);
                    else Gl.glColor4f(Settings.PointborderColor.R / 255f, Settings.PointborderColor.G / 255f, Settings.PointborderColor.B / 255f, Settings.PointborderColor.A);
                    vph.draw2DVertice(Pos);
                    Gl.glEnd();

                    Gl.glPointSize(Settings.PointSize);
                    Gl.glBegin(Gl.GL_POINTS);
                    if (SelectedDots.Contains(this)) Gl.glColor4f(Settings.HighlightPointColor.R / 255f, Settings.HighlightPointColor.G / 255f, Settings.HighlightPointColor.B / 255f, Settings.HighlightPointColor.A);
                    else Gl.glColor4f(Settings.ItemColor.R / 255f, Settings.ItemColor.G / 255f, Settings.ItemColor.B / 255f, Settings.ItemColor.A);
                    vph.draw2DVertice(Pos);
                    Gl.glEnd();

                    Gl.glLineWidth(Settings.LineWidth);
                    if (SelectedDots.Contains(this))
                    {
                        DrawFilledCircle(vph.getViewCoord(Pos, 0), vph.getViewCoord(Pos, 1), PointScale, 24, Settings.HighlightPointborderColor, Settings.HighlightPointColor);
                        float scanRad = 300;
                        if (PlayerScanRadiusVal != 0) scanRad *= 3;
                        DrawStrippedCircle(vph.getViewCoord(Pos, 0), vph.getViewCoord(Pos, 1), scanRad, 24, Settings.HighlightPointborderColor);
                    }
                    else DrawFilledCircle(vph.getViewCoord(Pos, 0), vph.getViewCoord(Pos, 1), PointScale, 24, Settings.ItemColor, Settings.ItemColor);
                }

                public void RenderLine()
                {
                    ViewPlaneHandler vph = (Application.OpenForms[0] as Form1).vph;
                    vph.draw2DVertice(Pos);
                }
            }

            public ItemGroup()
            {
                Next = new short[6];
                Previous = new short[6];
                for (int i = 0; i < Next.Length; i++)
                {
                    Next[i] = -1;
                    Previous[i] = -1;
                }
            }
            public ItemGroup(ITPH.ITPHEntry path, ITPT data)
            {
                Previous = path.Previous;
                Next = path.Next;
                for (int i = path.Start; i < path.Start + path.Length; i++)
                {
                    Entries.Add(new ItemEntry(data.Entries[i]));
                }
            }
            public ITPH.ITPHEntry ToITPHEntry(ushort start)
            {
                ITPH.ITPHEntry entry = new ITPH.ITPHEntry();
                entry.Start = start;
                entry.Length = (ushort)Entries.Count;
                entry.Previous = Previous;
                entry.Next = Next;
                return entry;
            }

            public void Render()
            {
                if (!Visible) return;

                VisualSettings Settings = (Application.OpenForms[0] as Form1).Settings;

                if (Settings.LinkPoints)
                {
                    Gl.glColor4f(Settings.ItemLinkColor.R / 255f, Settings.ItemLinkColor.G / 255f, Settings.ItemLinkColor.B / 255f, Settings.ItemLinkColor.A);
                    for (int i = 0; i < Entries.Count - 1; i++)
                    {
                        Gl.glPushAttrib(Gl.GL_ENABLE_BIT);
                        if (Entries[i+1].FlyModeVal == 1)
                        {
                            Gl.glLineStipple(3, 0xAAAA);
                            Gl.glEnable(Gl.GL_LINE_STIPPLE);
                        }
                        Gl.glBegin(Gl.GL_LINES);
                        Entries[i].RenderLine();
                        Entries[i + 1].RenderLine();
                        Gl.glEnd();
                        Gl.glPopAttrib();
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

        public ITPT ToITPT()
        {
            ITPT data = new ITPT();
            uint Count = 0;
            foreach (var group in Entries)
                foreach (var entry in group.Entries)
                {
                    data.Entries.Add(entry.ToITPTEntry());
                    Count++;
                }
            data.NrEntries = Count;
            return data;
        }

        public ITPH ToITPH()
        {
            ITPH path = new ITPH();
            path.NrEntries = (uint)Entries.Count;
            ushort start = 0;
            foreach (var group in Entries)
            {
                path.Entries.Add(group.ToITPHEntry(start));
                start += (ushort)group.Entries.Count;
            }
            return path;
        }

        public TreeNode GetTreeNode()
        {
            TreeNode node = new TreeNode("Item Routes");

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
                    Gl.glColor4f(Settings.ItemLinkColor.R / 255f, Settings.ItemLinkColor.G / 255f, Settings.ItemLinkColor.B / 255f, Settings.ItemLinkColor.A);
                    try
                    {
                        foreach (var next in group.Next)
                            if (next != -1 && Entries[next].Visible)
                            {
                                Gl.glPushAttrib(Gl.GL_ENABLE_BIT);
                                if (Entries[next].Entries[0].FlyModeVal == 1) {
                                    Gl.glLineStipple(3, 0xAAAA);
                                    Gl.glEnable(Gl.GL_LINE_STIPPLE);
                                }
                                Gl.glBegin(Gl.GL_LINES);
                                group.Entries[group.Entries.Count - 1].RenderLine();
                                Entries[next].Entries[0].RenderLine();
                                Gl.glEnd();
                                Gl.glPopAttrib();
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
        public void Transform(Vector3 translation, Vector3 scale)
        {
            foreach (var group in Entries)
            {
                foreach (var entry in group.Entries)
                {
                    entry.Pos *= scale;
                    entry.Pos += translation;
                }
            }
        }
    }
}
