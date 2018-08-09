using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using LibCTR.Collections;
using KMPSections;
using System.Windows.Forms;
using LibEndianBinaryIO.Serialization;
using System.ComponentModel;
using Extensions;
using System.Drawing.Design;
using Tao.OpenGl;
using System.Drawing;

namespace KMPExpander.Class.SimpleKMPs
{
    public class EnemyRoutes : SectionBase<EnemyRoutes.EnemyGroup>
    {
        //[XmlElement("Group")]
        //public List<EnemyGroup> Groups = new List<EnemyGroup>();
        public EnemyRoutes() { }
        public EnemyRoutes(ENPT data,ENPH paths)
        {
            foreach (var path in paths.Entries)
            {
                Entries.Add(new EnemyGroup(path,data));
            }
        }
        
        public class EnemyGroup : SectionBase<EnemyGroup.EnemyEntry>
        {
            [XmlAttribute,Category("Enemy Group"),Description("Specifies which previous groups this one is linked to."), Editor(typeof(CustomEditor), typeof(UITypeEditor))]
            public Int16[] Previous { get; set; }
            [XmlAttribute,Category("Enemy Group"), Description("Specifies which next groups this one is linked to."), Editor(typeof(CustomEditor), typeof(UITypeEditor))]
            public Int16[] Next { get; set; }
            [Category("Enemy Group")]
            [XmlAttribute, TypeConverter(typeof(HexTypeConverter))]
            public UInt32 Unknown { get; set; }
            
            //public List<EnemyEntry> Entries = new List<EnemyEntry>();

            public class EnemyEntry
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
                [Browsable(false), XmlAttribute]
                public UInt16 MushSettingsVal { get; set; }
                [XmlAttribute]
                public string MushSettings {
                    get {
                        switch(MushSettingsVal)
                        {
                            case 0:
                                return "0 - Can Use Mushroom";
                            case 1:
                                return "1 - Needs Mushroom";
                            case 2:
                                return "2 - Cannot Use Mushroom";
                            default:
                                return MushSettingsVal + " - Unknown";
                        }
                    }
                    set {
                        UInt16 val = UInt16.Parse(value);
                        MushSettingsVal = val;
                    }
                }
                [Browsable(false), XmlAttribute]
                public byte DriftSettingsVal { get; set; }
                [XmlAttribute]
                public string DriftSettings
                {
                    get
                    {
                        switch (DriftSettingsVal)
                        {
                            case 0:
                                return "0 - Allow Drift, Allow Miniturbo";
                            case 1:
                                return "1 - Disallow Drift, Allow Miniturbo";
                            case 2:
                                return "2 - Disallow Drift, Disallow Miniturbo";
                            default:
                                return DriftSettingsVal + " - Unknown";
                        }
                    }
                    set
                    {
                        byte val = byte.Parse(value);
                        DriftSettingsVal = val;
                    }
                }
                [XmlAttribute, Browsable(false)]
                public byte Flags { get; set; }
                //
                [XmlAttribute]
                public bool WideTurn
                {
                    get
                    {
                        return (Flags & 0x1) != 0;
                    }
                    set
                    {
                        Flags = (byte)((Flags & ~(1 << 0)) | ((value ? 1 : 0) << 0));
                    }
                }
                [XmlAttribute]
                public bool NormalTurn
                {
                    get
                    {
                        return (Flags & 0x4) != 0;
                    }
                    set
                    {
                        Flags = (byte)((Flags & ~(1 << 2)) | ((value ? 1 : 0) << 2));
                    }
                }
                [XmlAttribute]
                public bool SharpTurn
                {
                    get
                    {
                        return (Flags & 0x10) != 0;
                    }
                    set
                    {
                        Flags = (byte)((Flags & ~(1 << 4)) | ((value ? 1 : 0) << 4));
                    }
                }
                [XmlAttribute]
                public bool TricksForbidden
                {
                    get
                    {
                        return (Flags & 0x8) != 0;
                    }
                    set
                    {
                        Flags = (byte)((Flags & ~(1 << 3)) | ((value ? 1 : 0) << 3));
                    }
                }
                [XmlAttribute]
                public bool StickToRoute
                {
                    get
                    {
                        return (Flags & 0x40) != 0;
                    }
                    set
                    {
                        Flags = (byte)((Flags & ~(1 << 6)) | ((value ? 1 : 0) << 6));
                    }
                }
                [XmlAttribute]
                public bool BouncyMushSection
                {
                    get
                    {
                        return (Flags & 0x20) != 0;
                    }
                    set
                    {
                        Flags = (byte)((Flags & ~(1 << 5)) | ((value ? 1 : 0) << 5));
                    }
                }
                [XmlAttribute]
                public bool ForceDefaultSpeed
                {
                    get
                    {
                        return (Flags & 0x80) != 0;
                    }
                    set
                    {
                        Flags = (byte)((Flags & ~(1 << 7)) | ((value ? 1 : 0) << 7));
                    }
                }
                [XmlAttribute]
                public bool UnknownFlag
                {
                    get
                    {
                        return (Flags & 0x2) != 0;
                    }
                    set
                    {
                        Flags = (byte)((Flags & ~(1 << 1)) | ((value ? 1 : 0) << 1));
                    }
                }
                //
                [XmlAttribute]
                public Int16 Unknown1 { get; set; }
                [XmlAttribute]
                public Int16 Unknown2 { get; set; }

                public EnemyEntry() {
                    Scale = 1;
                }
                public EnemyEntry(ENPT.ENPTEntry entry)
                {
                    //Position = entry.Position;
                    PositionX = entry.Position.X;
                    PositionY = entry.Position.Y;
                    PositionZ = entry.Position.Z;
                    Scale = entry.Scale;
                    MushSettingsVal = entry.MushSettingsVal;
                    DriftSettingsVal = entry.DriftSettingsVal;
                    Flags = entry.Flags;
                    Unknown1 = entry.Unknown2;
                    Unknown2 = entry.Unknown3;
                }
                public ENPT.ENPTEntry ToENPTEntry()
                {
                    ENPT.ENPTEntry entry = new ENPT.ENPTEntry();
                    entry.Position = new Vector3(PositionX,PositionY,PositionZ);
                    entry.Scale = Scale;
                    entry.MushSettingsVal = MushSettingsVal;
                    entry.DriftSettingsVal = DriftSettingsVal;
                    entry.Flags = Flags;
                    entry.Unknown2 = Unknown1;
                    entry.Unknown3 = Unknown2;
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

                public void RenderPicking(int group_id,int entry_id)
                {
                    VisualSettings Settings = (Application.OpenForms[0] as Form1).Settings;
                    Color pickingColor = SectionPicking.GetColor(Sections.EnemyRoutes, group_id, entry_id);
                    float PointScale = 50f * Scale;

                    Gl.glPointSize(Settings.PointSize + 2f);
                    Gl.glBegin(Gl.GL_POINTS);
                    Gl.glColor4f(pickingColor.R / 255f, pickingColor.G / 255f, pickingColor.B / 255f, 1f);
                    Gl.glVertex2f(PositionX, PositionZ);
                    Gl.glEnd();
                }
                public void RenderPoint()
                {
                    VisualSettings Settings = (Application.OpenForms[0] as Form1).Settings;
                    List<object> SelectedDots = (Application.OpenForms[0] as Form1).SelectedDots;
                    float PointScale = 50f * Scale;

                    Gl.glPointSize(Settings.PointSize+2f);
                    Gl.glBegin(Gl.GL_POINTS);

                    if (SelectedDots.Contains(this)) Gl.glColor4f(Settings.HighlightPointborderColor.R / 255f, Settings.HighlightPointborderColor.G / 255f, Settings.HighlightPointborderColor.B / 255f, Settings.HighlightPointborderColor.A);
                    else Gl.glColor4f(Settings.PointborderColor.R / 255f, Settings.PointborderColor.G / 255f, Settings.PointborderColor.B / 255f, Settings.PointborderColor.A);
                    Gl.glVertex2f(PositionX, PositionZ);
                    Gl.glEnd();

                    Gl.glPointSize(Settings.PointSize);
                    Gl.glBegin(Gl.GL_POINTS);
                    if (SelectedDots.Contains(this)) Gl.glColor4f(Settings.HighlightPointColor.R / 255f, Settings.HighlightPointColor.G / 255f, Settings.HighlightPointColor.B / 255f, Settings.HighlightPointColor.A);
                    else Gl.glColor4f(Settings.EnemyColor.R / 255f, Settings.EnemyColor.G / 255f, Settings.EnemyColor.B / 255f, Settings.EnemyColor.A);
                    Gl.glVertex2f(PositionX,PositionZ);
                    Gl.glEnd();

                    Gl.glLineWidth(Settings.LineWidth);
                    if (SelectedDots.Contains(this)) DrawFilledCircle(PositionX, PositionZ, PointScale, 24, Settings.HighlightPointborderColor, Settings.HighlightPointColor);
                    else DrawFilledCircle(PositionX, PositionZ, PointScale, 24, Settings.EnemyColor, Settings.EnemyColor);
                }

                public void RenderLine()
                {
                    Gl.glVertex2f(PositionX, PositionZ);
                }
            }

            public EnemyGroup()
            {
                Next = new short[16];
                Previous = new short[16];
                for (int i = 0; i < Next.Length; i++)
                {
                    Next[i] = -1;
                    Previous[i] = -1;
                }
            }
            public EnemyGroup(ENPH.ENPHEntry path,ENPT data)
            {
                Previous = path.Previous;
                Next = path.Next;
                Unknown = path.Unknown;
                for (int i=path.Start;i< path.Start + path.Length;i++)
                {
                    Entries.Add(new EnemyEntry(data.Entries[i]));
                }
            }
            public ENPH.ENPHEntry ToENPHEntry(ushort start)
            {
                ENPH.ENPHEntry entry = new ENPH.ENPHEntry();
                entry.Start = start;
                entry.Length = (ushort)Entries.Count;
                entry.Previous = Previous;
                entry.Next = Next;
                entry.Unknown = Unknown;
                return entry;
            }

            public void RenderPicking(int group_id)
            {
                if (!Visible) return;

                for (int i = 0; i < Entries.Count; i++)
                    Entries[i].RenderPicking(group_id, i);
            }

            public void Render()
            {
                if (!Visible) return;

                VisualSettings Settings = (Application.OpenForms[0] as Form1).Settings;

                if (Settings.LinkPoints)
                {
                    Gl.glColor4f(Settings.EnemyLinkColor.R / 255f, Settings.EnemyLinkColor.G / 255f, Settings.EnemyLinkColor.B / 255f, Settings.EnemyLinkColor.A);
                    for (int i = 0; i < Entries.Count - 1; i++)
                    {
                        Gl.glBegin(Gl.GL_LINES);
                        Entries[i].RenderLine();
                        Entries[i + 1].RenderLine();
                        Gl.glEnd();
                    }
                }
                foreach (EnemyEntry entry in Entries)
                    entry.RenderPoint();
            }
        }

        public ENPT ToENPT()
        {
            ENPT data = new ENPT();
            uint Count = 0;
            foreach (var group in Entries)
                foreach (var entry in group.Entries)
                {
                    data.Entries.Add(entry.ToENPTEntry());
                    Count++;
                }
            data.NrEntries = Count; 
            return data;
        }

        public ENPH ToENPH()
        {
            ENPH path = new ENPH();
            path.NrEntries = (uint)Entries.Count;
            ushort start = 0;
            foreach (var group in Entries)
            {
                path.Entries.Add(group.ToENPHEntry(start));
                start += (ushort)group.Entries.Count;
            }
            return path;
        }

        public TreeNode GetTreeNode()
        {
            TreeNode node = new TreeNode("Enemy Routes");

            int i = 0;
            foreach (var group in Entries)
            {
                node.Nodes.Add("Group " + i.ToString());
                node.Nodes[i].Tag = group;
                node.Nodes[i].ImageIndex = 11;
                node.Nodes[i].SelectedImageIndex = 11;
                i++;
            }
            return node;
        }

        public void Render(bool picking)
        {
            if (!Visible) return;

            if (picking)
            {
                for (int i=0;i<Entries.Count;i++)
                    Entries[i].RenderPicking(i);
                return;
            }
            VisualSettings Settings = (Application.OpenForms[0] as Form1).Settings;

            Gl.glLineWidth(Settings.LineWidth);
            foreach (EnemyGroup group in Entries)
            {
                if (Settings.LinkPoints && Settings.LinkPaths && group.Visible)
                {
                    Gl.glColor4f(Settings.EnemyLinkColor.R / 255f, Settings.EnemyLinkColor.G / 255f, Settings.EnemyLinkColor.B / 255f, Settings.EnemyLinkColor.A);
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
