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
        
        public static Color decideColor(EnemyGroup.EnemyEntry entry)
        {
            VisualSettings Settings = (Application.OpenForms[0] as Form1).Settings;
            byte rmask = 0, gmask = 0, bmask = 0;
            if (entry.KeepMiniturbo)
            {
                rmask += 0xEF;
                gmask += 0x95;
            }
            if (entry.EncourageMiniturbo)
            {
                bmask = 0xEF;
            }
            return Color.FromArgb(Settings.EnemyColor.A, (Settings.EnemyColor.R + rmask), (Settings.EnemyColor.G + gmask), (Settings.EnemyColor.B + bmask));
        }

        public static void decideDrawLineMode(EnemyGroup.EnemyEntry entry)
        {
            VisualSettings Settings = (Application.OpenForms[0] as Form1).Settings;
            if (entry.DriftSettingsVal == 0)
            {
                Gl.glLineStipple(3, 0xAAAA);
                Gl.glEnable(Gl.GL_LINE_STIPPLE);
            } else if (entry.DriftSettingsVal == 1)
            {
                Gl.glLineStipple(3, 0xEEEE);
                Gl.glEnable(Gl.GL_LINE_STIPPLE);
            }
            Color newcolor = decideColor(entry);
            Gl.glColor4f(newcolor.R / 255f, newcolor.G / 255f, newcolor.B / 255f, newcolor.A / 255f);
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
                [XmlIgnore, Browsable(false)]
                public Vector3 Pos { get; set; } = new Vector3(0, 0, 0);
                [XmlAttribute]
                public Single PositionX {
                    get {
                        return Pos.X;
                    }
                    set {
                        Pos = new Vector3(value, Pos.Y, Pos.Z);
                    }
                }
                [XmlAttribute]
                public Single PositionY {
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
                public Single PositionZ {
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
                public UInt16 MushSettingsVal { get; set; }
                [XmlIgnore]
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
                [XmlIgnore]
                public string DriftSettings
                {
                    get
                    {
                        switch (DriftSettingsVal)
                        {
                            case 0:
                                return "0 - Allow Drift Start";
                            case 1:
                                return "1 - Allow Drift In Progress";
                            case 2:
                                return "2 - Disallow Drift";
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
                [XmlIgnore, Browsable(false)]
                public byte Flags { get; set; } = 0;
                //
                [XmlAttribute]
                public bool KeepMiniturbo
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
                public bool EncourageMiniturbo
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
                public bool IncreasePathPrecision
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
                public bool PreventRouteSwitch
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
                [XmlAttribute]
                public bool KeepBulletBill
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
                
                //
                [XmlAttribute, Browsable(false)]
                public Int16 PathFindOptsVal { get; set; }
                [XmlIgnore]
                public string PathFindOpts
                {
                    get
                    {
                        switch (PathFindOptsVal)
                        {
                            case -4:
                                return "-4 - Taken under unknown flag";
                            case -3:
                                return "-3 - Taken under unknown flag";
                            case -2:
                                return "-2 - Bullet cannot find";
                            case -1:
                                return "-1 - CPU Racer cannot find";
                            case 0:
                                return "0 - No restrictions";
                            default:
                                return PathFindOptsVal + " - Unknown";
                        }
                    }
                    set
                    {
                        Int16 val = Int16.Parse(value);
                        PathFindOptsVal = val;
                    }
                }
                [XmlAttribute, Browsable(false)] //Limit height find -1 = 75
                public Int16 MaxSearchYOffsetVal { get; set; }
                [XmlIgnore]
                public string MaxSearchYOffset
                {
                    get
                    {
                        if (MaxSearchYOffsetVal < 0) return "-1 (75) - Limited offset";
                        else if (MaxSearchYOffsetVal > 0) return MaxSearchYOffsetVal + " - Limited offset";
                        else return "0 - No limited offset";
                    }
                    set
                    {
                        Int16 val = Int16.Parse(value);
                        MaxSearchYOffsetVal = val;
                    }
                }
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
                    PathFindOptsVal = entry.Unknown2;
                    MaxSearchYOffsetVal = entry.Unknown3;
                }
                public ENPT.ENPTEntry ToENPTEntry()
                {
                    ENPT.ENPTEntry entry = new ENPT.ENPTEntry();
                    entry.Position = new Vector3(PositionX,PositionY,PositionZ);
                    entry.Scale = Scale;
                    entry.MushSettingsVal = MushSettingsVal;
                    entry.DriftSettingsVal = DriftSettingsVal;
                    entry.Flags = Flags;
                    entry.Unknown2 = PathFindOptsVal;
                    entry.Unknown3 = MaxSearchYOffsetVal;
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
                    ViewPlaneHandler vph = (Application.OpenForms[0] as Form1).vph;
                    float PointScale = 50f * Scale;

                    Gl.glPointSize(Settings.PointSize + 2f);
                    Gl.glBegin(Gl.GL_POINTS);
                    Gl.glColor4f(pickingColor.R / 255f, pickingColor.G / 255f, pickingColor.B / 255f, 1f);
                    vph.draw2DVertice(Pos);
                    Gl.glEnd();
                }
                public void RenderPoint()
                {
                    VisualSettings Settings = (Application.OpenForms[0] as Form1).Settings;
                    List<object> SelectedDots = (Application.OpenForms[0] as Form1).SelectedDots;
                    float PointScale = 50f * Scale;
                    Color newcolor = decideColor(this);

                    ViewPlaneHandler vph = (Application.OpenForms[0] as Form1).vph;

                    Gl.glPointSize(Settings.PointSize+2f);
                    Gl.glBegin(Gl.GL_POINTS);

                    if (SelectedDots.Contains(this)) Gl.glColor4f(Settings.HighlightPointborderColor.R / 255f, Settings.HighlightPointborderColor.G / 255f, Settings.HighlightPointborderColor.B / 255f, Settings.HighlightPointborderColor.A);
                    else Gl.glColor4f(1 - Settings.PointborderColor.R / 255f, 1 - Settings.PointborderColor.G / 255f, 1 - Settings.PointborderColor.B / 255f, Settings.PointborderColor.A);
                    vph.draw2DVertice(Pos);
                    Gl.glEnd();

                    Gl.glPointSize(Settings.PointSize);
                    Gl.glBegin(Gl.GL_POINTS);
                    if (SelectedDots.Contains(this)) Gl.glColor4f(Settings.HighlightPointColor.R / 255f, Settings.HighlightPointColor.G / 255f, Settings.HighlightPointColor.B / 255f, Settings.HighlightPointColor.A);
                    else Gl.glColor4f(newcolor.R / 255f, newcolor.G / 255f, newcolor.B / 255f, newcolor.A);
                    vph.draw2DVertice(Pos);
                    Gl.glEnd();

                    Gl.glLineWidth(Settings.LineWidth);
                    if (SelectedDots.Contains(this)) DrawFilledCircle(vph.getViewCoord(Pos, 0), vph.getViewCoord(Pos, 1), PointScale, 24, Settings.HighlightPointborderColor, Settings.HighlightPointColor);
                    else DrawFilledCircle(vph.getViewCoord(Pos, 0), vph.getViewCoord(Pos, 1), PointScale, 24, newcolor, newcolor);

                    if (SelectedDots.Contains(this) && MaxSearchYOffsetVal != 0 && (vph.mode == ViewPlaneHandler.PLANE_MODES.XY || vph.mode == ViewPlaneHandler.PLANE_MODES.ZY))
                    {
                        int toDraw = MaxSearchYOffsetVal;
                        if (toDraw == -1) toDraw = 75;
                        Gl.glColor4f(Settings.HighlightPointColor.R / 255f, Settings.HighlightPointColor.G / 255f, Settings.HighlightPointColor.B / 255f, Settings.HighlightPointColor.A);
                        Gl.glPushAttrib(Gl.GL_ENABLE_BIT);
                        Gl.glLineStipple(3, 0xAAAA);
                        Gl.glEnable(Gl.GL_LINE_STIPPLE);
                        Gl.glBegin(Gl.GL_LINES);

                        vph.draw2DVertice(new Vector3(100000, Pos.Y + toDraw, 100000));
                        vph.draw2DVertice(new Vector3(-100000, Pos.Y + toDraw, -100000));

                        Gl.glEnd();
                        Gl.glBegin(Gl.GL_LINES);

                        vph.draw2DVertice(new Vector3(100000, Pos.Y - toDraw, 100000));
                        vph.draw2DVertice(new Vector3(-100000, Pos.Y - toDraw, -100000));

                        Gl.glEnd();
                        Gl.glPopAttrib();
                    }
                }

                public void RenderLine()
                {
                    ViewPlaneHandler vph = (Application.OpenForms[0] as Form1).vph;
                    vph.draw2DVertice(Pos);
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
                   for (int i = 0; i < Entries.Count - 1; i++)
                    {
                        Gl.glColor4f(Settings.EnemyLinkColor.R / 255f, Settings.EnemyLinkColor.G / 255f, Settings.EnemyLinkColor.B / 255f, Settings.EnemyLinkColor.A);
                        Gl.glPushAttrib(Gl.GL_ENABLE_BIT);
                        decideDrawLineMode(Entries[i + 1]);
                        Gl.glBegin(Gl.GL_LINES);
                        Entries[i].RenderLine();
                        Entries[i + 1].RenderLine();
                        Gl.glEnd();
                        Gl.glPopAttrib();
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
                node.Nodes[i].ImageIndex = 12;
                node.Nodes[i].SelectedImageIndex = 12;
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
                    try
                    {
                        foreach (var next in group.Next)
                            if (next != -1 && Entries[next].Visible)
                            {
                                Gl.glColor4f(Settings.EnemyLinkColor.R / 255f, Settings.EnemyLinkColor.G / 255f, Settings.EnemyLinkColor.B / 255f, Settings.EnemyLinkColor.A);
                                Gl.glPushAttrib(Gl.GL_ENABLE_BIT);
                                decideDrawLineMode(Entries[next].Entries[0]);
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
    }
}
