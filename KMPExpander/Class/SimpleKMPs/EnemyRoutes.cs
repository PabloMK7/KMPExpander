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
            if (entry.Cornering)
            {
                rmask += 0xEF;
                gmask += 0x95;
            }
            if (entry.HoldDrift)
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
            [XmlAttribute, Category("Enemy Group"), Description("The groups that lead into this one. The row shows the links the game reads, each with the point of that group it meets. Each slot takes a group number (-1 for none), and its drop-down picks which end of that group meets the first point of this group (usually its last point). Only battle courses use the first point, races ignore this choice."), Editor(typeof(CustomEditor), typeof(UITypeEditor)), TypeConverter(typeof(EnemyLinksConverter))]
            public Int16[] Previous { get; set; }
            [XmlAttribute, Category("Enemy Group"), Description("The groups this one leads to. The row shows the links the game reads, each with the point of that group it meets. Each slot takes a group number (-1 for none), and its drop-down picks which end of that group the last point of this group meets (usually its first point). In a race, the last point makes CPUs enter that group at its last point, which no original race course does."), Editor(typeof(CustomEditor), typeof(UITypeEditor)), TypeConverter(typeof(EnemyLinksConverter))]
            public Int16[] Next { get; set; }
            // The link end flags: bit n says which end of the path in Previous slot n the junction at this
            // path's first point is at, bit 16 + n the same for Next slot n.
            // Chosen in the slot rows of Previous and Next (EnemyLinksConverter).
            [XmlAttribute("LinkEndFlags"), Browsable(false)]
            public UInt32 LinkEndFlags { get; set; }

            // The valid entries the game reads: as many slots, from the first one, as there are valid entries.
            public static List<int[]> LinkSlots(Int16[] links)
            {
                List<int[]> list = new List<int[]>();
                if (links == null) return list;
                int count = 0;
                foreach (var n in links) if (n >= 0) count++;
                for (int slot = 0; slot < count && slot < links.Length; slot++)
                    if (links[slot] >= 0) list.Add(new int[] { slot, links[slot] });
                return list;
            }

            // Whether the group in this slot meets this group with its other end than usual (its first point for a
            // previous group, its last point for a next group).
            public bool LinkEndFlag(int slot, bool next)
            {
                return (LinkEndFlags & (1u << (slot + (next ? 16 : 0)))) != 0;
            }

            public void SetLinkEndFlag(int slot, bool next, bool value)
            {
                uint bit = 1u << (slot + (next ? 16 : 0));
                LinkEndFlags = value ? LinkEndFlags | bit : LinkEndFlags & ~bit;
            }

            // Whether the link in this slot meets the linked group's first point (else its last point).
            public bool LinksToFirst(int slot, bool next)
            {
                return LinkEndFlag(slot, next) != next;
            }

            // The links the game reads, as "group: first" or "group: last" for the end of the linked group they meet.
            public string LinksText(bool next)
            {
                List<string> parts = new List<string>();
                foreach (int[] link in LinkSlots(next ? Next : Previous))
                    parts.Add(link[1] + ": " + (LinksToFirst(link[0], next) ? "start" : "end"));
                return parts.Count == 0 ? "none" : String.Join(", ", parts);
            }

            //public List<EnemyEntry> Entries = new List<EnemyEntry>();

            public class EnemyEntry
            {
                //public Vector3 Position { get; set; }
                [XmlIgnore, Browsable(false)]
                public Vector3 Pos { get; set; } = new Vector3(0, 0, 0);
                [XmlAttribute, Description("Position of the enemy point.")]
                public Single PositionX {
                    get {
                        return Pos.X;
                    }
                    set {
                        Pos = new Vector3(value, Pos.Y, Pos.Z);
                    }
                }
                [XmlAttribute, Description("Height of the enemy point. Keep enemy points above Y = 0.")]
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
                [XmlAttribute, Description("Position of the enemy point.")]
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
                [XmlAttribute, Description("How wide the road is for the CPUs, in steps of 50 units to each side. CPUs drive in their own lanes, up to 85 percent of that width away from the point. Keep the whole width on the road.")]
                public Single Scale { get; set; }
                [Browsable(false), XmlAttribute]
                public UInt16 MushSettingsVal { get; set; }
                [XmlIgnore, Description("Whether CPUs may use a Mushroom here. 0 means they may. 1 marks the first point of a mushroom shortcut, which CPUs holding a Mushroom may take depending on their level, and no Mushroom is used here. 2 or any other value means no Mushroom.")]
                public string MushSettings {
                    get {
                        switch(MushSettingsVal)
                        {
                            case 0:
                                return "0 - Mushroom allowed";
                            case 1:
                                return "1 - Mushroom shortcut start";
                            default:
                                return MushSettingsVal + " - No Mushroom allowed";
                        }
                    }
                    set {
                        UInt16 val = UInt16.Parse(value);
                        MushSettingsVal = val;
                    }
                }
                [Browsable(false), XmlAttribute]
                public byte DriftSettingsVal { get; set; }
                [XmlIgnore, Description("Where CPUs drift, in races only. 0 allows drifting. 1 means no drift starts toward this point and a drift ends when the CPU reaches it, keeping the mini-turbo if a random check passes. 2 is like 1 but the mini-turbo is lost. 3 or more lets the corner decide, so a drift ends where the road straightens or turns the other way.")]
                public string DriftSettings
                {
                    get
                    {
                        switch (DriftSettingsVal)
                        {
                            case 0:
                                return "0 - Drift allowed";
                            case 1:
                                return "1 - Drift ends";
                            case 2:
                                return "2 - Drift ends, mini-turbo lost";
                            default:
                                return DriftSettingsVal + " - Corner decides";
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
                [XmlAttribute, Description("Flag 0x01, races only. A drifting CPU heading to this point \"cuts the corner\" depends on its level. At 150cc it moves 70 percent of the road width to the inside of the turn, at 50% it widens instead.")]
                public bool Cornering
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
                [XmlAttribute, Description("Flag 0x10, races only. Drifts toward this point are long and gentle and are not cut short.")]
                public bool HoldDrift
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
                
                [XmlAttribute, Description("Flag 0x04. The CPU only moves on to the next point when it is within 140 units of this one, instead of 280 in a race or 100 in a battle. It also works like Cornering.")]
                public bool Precise
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
                [XmlAttribute, Description("Flag 0x40, races only. A Bullet Bill cannot end at this point.")]
                public bool BulletBillCannotEnd
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
                [XmlAttribute, Description("Flag 0x02. A CPU whose next point is far above or below may be sent to a route at its own height.")]
                public bool HeightReroute
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
                [XmlAttribute, Description("Flag 0x20, races only. A Bullet Bill flies at the height of the route instead of falling.")]
                public bool BulletBillFollowsHeight
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
                [XmlAttribute, Description("Flag 0x80. CPUs drive at a fixed share of their top speed here, without catching up or slowing down for the player. In battle they also drive through the exact centre of the point and do a trick at every ramp.")]
                public bool BaseSpeed
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
                [XmlAttribute, Description("Flag 0x08, races only. CPUs heading to this point do not hop for tricks on ramps.")]
                public bool NoTricks
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
                [XmlIgnore, Description("Special uses of the point, in races. -1 means CPUs that finished the race are never sent here. -2 means Bullet Bills never start here or get sent here. -3 marks a path that Boost Jumper and Aviator Mii CPUs prefer. -4 marks a path that Dolphin Mii CPUs prefer. 1 to 3 are for the award ceremony course. Any value other than 0 also stops the point from being used to find where CPUs start. Keep 0 on battle courses and around the start line.")]
                public string PathFindOpts
                {
                    get
                    {
                        switch (PathFindOptsVal)
                        {
                            case -4:
                                return "-4 - Dolphin Mii path";
                            case -3:
                                return "-3 - Boost Jumper and Aviator Mii path";
                            case -2:
                                return "-2 - No Bullet Bill";
                            case -1:
                                return "-1 - No finished CPUs";
                            case 0:
                                return "0 - Normal";
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
                [XmlIgnore, Description("Limits how far above or below this point may be found when the game looks for the nearest point, for example after a respawn or for a Bullet Bill. 0 means no limit, a negative value means 75 units, and a positive value is the limit in units.")]
                public string MaxSearchYOffset
                {
                    get
                    {
                        if (MaxSearchYOffsetVal < 0) return MaxSearchYOffsetVal + " - 75 units";
                        else if (MaxSearchYOffsetVal > 0) return MaxSearchYOffsetVal + " - " + MaxSearchYOffsetVal + " units";
                        else return "0 - No limit";
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

                    Gl.glColor4f(pickingColor.R / 255f, pickingColor.G / 255f, pickingColor.B / 255f, 1f);
                    vph.drawPoint(Pos, Settings.PointSize + 2f);
                }
                public void RenderPoint()
                {
                    VisualSettings Settings = (Application.OpenForms[0] as Form1).Settings;
                    List<object> SelectedDots = (Application.OpenForms[0] as Form1).SelectedDots;
                    float PointScale = 50f * Scale;
                    Color newcolor = decideColor(this);

                    ViewPlaneHandler vph = (Application.OpenForms[0] as Form1).vph;


                    if (SelectedDots.Contains(this)) Gl.glColor4f(Settings.HighlightPointborderColor.R / 255f, Settings.HighlightPointborderColor.G / 255f, Settings.HighlightPointborderColor.B / 255f, Settings.HighlightPointborderColor.A);
                    else Gl.glColor4f(1 - Settings.PointborderColor.R / 255f, 1 - Settings.PointborderColor.G / 255f, 1 - Settings.PointborderColor.B / 255f, Settings.PointborderColor.A);
                    vph.drawPoint(Pos, Settings.PointSize + 2f, true);

                    if (SelectedDots.Contains(this)) Gl.glColor4f(Settings.HighlightPointColor.R / 255f, Settings.HighlightPointColor.G / 255f, Settings.HighlightPointColor.B / 255f, Settings.HighlightPointColor.A);
                    else Gl.glColor4f(newcolor.R / 255f, newcolor.G / 255f, newcolor.B / 255f, newcolor.A);
                    vph.drawPoint(Pos, Settings.PointSize);

                    Gl.glLineWidth(Settings.LineWidth);
                    if (SelectedDots.Contains(this)) { if (!vph.drawVolume(Pos, PointScale, Settings.HighlightPointborderColor, Settings.HighlightPointColor)) DrawFilledCircle(vph.getViewCoord(Pos, 0), vph.getViewCoord(Pos, 1), PointScale, 24, Settings.HighlightPointborderColor, Settings.HighlightPointColor); }
                    else if (!vph.drawVolume(Pos, PointScale, newcolor, newcolor)) DrawFilledCircle(vph.getViewCoord(Pos, 0), vph.getViewCoord(Pos, 1), PointScale, 24, newcolor, newcolor);

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
                LinkEndFlags = path.Unknown;
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
                entry.Unknown = LinkEndFlags;
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

        // The point of group q that a link reaches: for a link from a group's first point (a previous group), q's last
        // point, or its first one when the flag is set; for a link from the last point (a next group), q's first point,
        // or its last one when the flag is set.
        public static EnemyGroup.EnemyEntry LinkedEntry(EnemyGroup q, bool next, bool flag)
        {
            bool first = next ? !flag : flag;
            return first ? q.Entries[0] : q.Entries[q.Entries.Count - 1];
        }

        private static float Distance2(Vector3 a, Vector3 b)
        {
            return (a.X - b.X) * (a.X - b.X) + (a.Y - b.Y) * (a.Y - b.Y) + (a.Z - b.Z) * (a.Z - b.Z);
        }

        // The flag a link should have when the junction is where the paths come closest: set when the linked group's
        // unusual end (its first point for a previous group, its last point for a next group) is nearer to this group's
        // end. A group linked to itself (a loop) adds nothing. The original battle courses are built this way, apart
        // from links whose two ends are about as far (one less than twice as far as the other); for those this gives
        // null, and the flag is left as it is.
        public bool? ExpectedLinkEndFlag(int g, int q, bool next)
        {
            EnemyGroup grp = Entries[g], other = Entries[q];
            if (g == q || grp.Entries.Count == 0 || other.Entries.Count == 0) return false;
            Vector3 end = next ? grp.Entries[grp.Entries.Count - 1].Pos : grp.Entries[0].Pos;
            float toFirst = Distance2(end, other.Entries[0].Pos), toLast = Distance2(end, other.Entries[other.Entries.Count - 1].Pos);
            if (Math.Max(toFirst, toLast) < 4 * Math.Min(toFirst, toLast)) return null;
            return next ? toLast < toFirst : toFirst < toLast;
        }

        // Sets the link end flags of every group from where the paths meet (ExpectedLinkEndFlag). Returns how many
        // groups changed.
        public int SetLinkEndsFromGeometry()
        {
            int changed = 0;
            for (int g = 0; g < Entries.Count; g++)
            {
                EnemyGroup grp = Entries[g];
                uint before = grp.LinkEndFlags;
                for (int side = 0; side < 2; side++)
                    foreach (int[] link in EnemyGroup.LinkSlots(side == 1 ? grp.Next : grp.Previous))
                    {
                        if (link[1] >= Entries.Count) continue;
                        bool? flag = ExpectedLinkEndFlag(g, link[1], side == 1);
                        if (flag != null) grp.SetLinkEndFlag(link[0], side == 1, flag.Value);
                    }
                if (grp.LinkEndFlags != before) changed++;
            }
            return changed;
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
                        // The links as the game makes them: from the last point to the end of each next group that the
                        // link end flags name, and in a battle also from the first point to each previous group.
                        SimpleKMP kmp = (Application.OpenForms[0] as Form1).getKayEmPee();
                        bool battle = kmp != null && kmp.CheckPoints.Entries.Count == 0;
                        for (int side = 0; side < (battle && group.Entries.Count > 1 ? 2 : 1); side++)
                        {
                            bool next = side == 0;
                            foreach (int[] link in EnemyGroup.LinkSlots(next ? group.Next : group.Previous))
                            {
                                if (link[1] >= Entries.Count || !Entries[link[1]].Visible || Entries[link[1]].Entries.Count == 0) continue;
                                var target = LinkedEntry(Entries[link[1]], next, group.LinkEndFlag(link[0], next));
                                Gl.glColor4f(Settings.EnemyLinkColor.R / 255f, Settings.EnemyLinkColor.G / 255f, Settings.EnemyLinkColor.B / 255f, Settings.EnemyLinkColor.A);
                                Gl.glPushAttrib(Gl.GL_ENABLE_BIT);
                                decideDrawLineMode(target);
                                Gl.glBegin(Gl.GL_LINES);
                                (next ? group.Entries[group.Entries.Count - 1] : group.Entries[0]).RenderLine();
                                target.RenderLine();
                                Gl.glEnd();
                                Gl.glPopAttrib();
                            }
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

    // Previous and Next of an enemy group in the property grid. The array's row is information only: the links the game
    // reads with the end of each linked group (EnemyGroup.LinksText). Each slot's row takes a group number, and its
    // drop-down chooses which end of that group the link meets (the link end flag of the slot).
    public class EnemyLinksConverter : ArrayConverter
    {
        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
        {
            return false;
        }

        public override object ConvertTo(ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value, Type destinationType)
        {
            bool next;
            EnemyRoutes.EnemyGroup group = Owner(context, value, out next);
            if (destinationType == typeof(string) && group != null) return group.LinksText(next);
            return base.ConvertTo(context, culture, value, destinationType);
        }

        public override PropertyDescriptorCollection GetProperties(ITypeDescriptorContext context, object value, Attribute[] attributes)
        {
            bool next;
            EnemyRoutes.EnemyGroup group = Owner(context, value, out next);
            Int16[] links = value as Int16[];
            if (group == null || links == null) return base.GetProperties(context, value, attributes);
            PropertyDescriptor[] slots = new PropertyDescriptor[links.Length];
            for (int i = 0; i < links.Length; i++) slots[i] = new SlotDescriptor(group, next, i);
            return new PropertyDescriptorCollection(slots);
        }

        // The group whose Previous or Next array this is: the grid's instance, else found by the array itself.
        private static EnemyRoutes.EnemyGroup Owner(ITypeDescriptorContext context, object value, out bool next)
        {
            next = context != null && context.PropertyDescriptor != null && context.PropertyDescriptor.Name == "Next";
            EnemyRoutes.EnemyGroup group = context != null ? context.Instance as EnemyRoutes.EnemyGroup : null;
            if (group != null && (group.Next == value || group.Previous == value))
            {
                next = group.Next == value;
                return group;
            }
            SimpleKMP kmp = (Application.OpenForms[0] as Form1).getKayEmPee();
            if (kmp == null || value == null) return null;
            foreach (EnemyRoutes.EnemyGroup g in kmp.EnemyRoutes.Entries)
                if (g.Next == value || g.Previous == value)
                {
                    next = g.Next == value;
                    return g;
                }
            return null;
        }

        private class SlotDescriptor : PropertyDescriptor
        {
            private readonly EnemyRoutes.EnemyGroup group;
            private readonly bool next;
            private readonly int slot;

            public SlotDescriptor(EnemyRoutes.EnemyGroup group, bool next, int slot)
                : base("[" + slot + "]", new Attribute[] { new DescriptionAttribute("A group number, -1 for none. The drop-down chooses which end of that group the link meets: " + (next ? "usually its first point." : "usually its last point. Races ignore this choice.") + " A slot after the first -1 is not read when fewer groups are linked than its number.") })
            {
                this.group = group;
                this.next = next;
                this.slot = slot;
            }

            private Int16[] Links { get { return next ? group.Next : group.Previous; } }

            public override Type ComponentType { get { return typeof(Int16[]); } }
            public override bool IsReadOnly { get { return false; } }
            public override Type PropertyType { get { return typeof(string); } }
            public override TypeConverter Converter { get { return new SlotConverter(); } }
            public override bool CanResetValue(object component) { return false; }
            public override void ResetValue(object component) { }
            public override bool ShouldSerializeValue(object component) { return false; }

            public string Text(bool first)
            {
                return Links[slot] + ": " + (first ? "start" : "end") + " point";
            }

            public string[] Choices()
            {
                return Links[slot] < 0 ? new string[0] : new string[] { Text(true), Text(false) };
            }

            public override object GetValue(object component)
            {
                if (Links[slot] < 0) return "-1";
                bool read = EnemyRoutes.EnemyGroup.LinkSlots(Links).Any(l => l[0] == slot);
                return Text(group.LinksToFirst(slot, next)) + (read ? "" : " (not read)");
            }

            // A new group number starts with the usual end; "first" or "last" in the text chooses the end.
            public override void SetValue(object component, object value)
            {
                string text = (value as string ?? "").Trim();
                var number = System.Text.RegularExpressions.Regex.Match(text, "^-?[0-9]+");
                Int16 linked;
                if (!number.Success || !Int16.TryParse(number.Value, out linked)) return;
                linked = Math.Max((Int16)(-1), linked);
                if (linked != Links[slot]) group.SetLinkEndFlag(slot, next, false);
                Links[slot] = linked;
                if (text.IndexOf("first", StringComparison.OrdinalIgnoreCase) >= 0) group.SetLinkEndFlag(slot, next, !next);
                else if (text.IndexOf("last", StringComparison.OrdinalIgnoreCase) >= 0) group.SetLinkEndFlag(slot, next, next);
                if (linked < 0) group.SetLinkEndFlag(slot, next, false);
                OnValueChanged(component, EventArgs.Empty);
            }
        }

        private class SlotConverter : StringConverter
        {
            public override bool GetStandardValuesSupported(ITypeDescriptorContext context) { return true; }
            public override bool GetStandardValuesExclusive(ITypeDescriptorContext context) { return false; }

            public override StandardValuesCollection GetStandardValues(ITypeDescriptorContext context)
            {
                SlotDescriptor slot = context != null ? context.PropertyDescriptor as SlotDescriptor : null;
                return new StandardValuesCollection(slot != null ? slot.Choices() : new string[0]);
            }
        }
    }
}
