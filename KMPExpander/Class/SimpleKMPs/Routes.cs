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
using Tao.OpenGl;
using System.Drawing;

namespace KMPExpander.Class.SimpleKMPs
{
    public class Routes : SectionBase<Routes.RouteGroup>
    {
        //[XmlElement("Group")]
        //public List<RouteGroup> Groups = new List<RouteGroup>();
        public Routes() { }
        public Routes(POTI data)
        {
            foreach (var path in data.Routes)
            {
                Entries.Add(new RouteGroup(path));
            }
        }

        public class RouteGroup : SectionBase<RouteGroup.RouteEntry>
        {
            // The two settings of the route (Field::PathBase): the first chooses straight lines (0) or a curve through the
            // points (not 0), the second a closed route (0), whose first point follows the last one, or an open route
            // (not 0), which cameras run back and forth.
            [XmlAttribute, Browsable(false)]
            public Byte Setting1 { get; set; }
            [XmlAttribute, Browsable(false)]
            public Byte Setting2 { get; set; }
            [XmlIgnore, Category("Route"), Description("Whether the route is a curve through its points instead of straight lines.")]
            public bool Smooth { get { return Setting1 != 0; } set { Setting1 = (byte)(value ? 1 : 0); } }
            [XmlIgnore, Category("Route"), Description("Whether the route is closed: whatever follows it goes from the last point back to the first one. An open route is run back and forth by cameras.")]
            public bool Loop { get { return Setting2 == 0; } set { Setting2 = (byte)(value ? 0 : 1); } }
            // Files saved by older versions, which stored the first setting as Loop and the second as Smooth.
            [XmlAttribute("Loop"), Browsable(false)]
            public bool LegacyLoop { get { return false; } set { Setting1 = (byte)(value ? 1 : 0); } }
            public bool ShouldSerializeLegacyLoop() { return false; }
            [XmlAttribute("Smooth"), Browsable(false)]
            public bool LegacySmooth { get { return false; } set { Setting2 = (byte)(value ? 1 : 0); } }
            public bool ShouldSerializeLegacySmooth() { return false; }
            //[XmlElement("Entry")]
            //public List<RouteEntry> Entries = new List<RouteEntry>();
            public class RouteEntry
            {
                //public Vector3 Position { get; set; }
                [XmlIgnore, Browsable(false)]
                public Vector3 Pos { get; set; } = new Vector3(0, 0, 0);
                [XmlAttribute, Description("Position of the route point.")]
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
                [XmlAttribute, Description("Position of the route point.")]
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
                [XmlAttribute, Description("Position of the route point.")]
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
                [XmlAttribute, Description("Speed along the route, blended between points. Some objects use one of their own settings instead. Set the last point to 0 if whatever follows the route should never reach its end.")]
                public UInt16 Speed { get; set; }
                [XmlAttribute, TypeConverter(typeof(HexTypeConverter)), Description("Used by some objects, with a meaning that depends on the object. 0 by default.")]
                public UInt16 Setting2 { get; set; }

                public RouteEntry() { }

                public RouteEntry(POTI.POTIRoute.POTIPoint entry)
                {
                    //Position = entry.Position;
                    PositionX = entry.Position.X;
                    PositionY = entry.Position.Y;
                    PositionZ = entry.Position.Z;
                    Speed = entry.RouteSpeed;
                    Setting2 = entry.Setting2;
                }
                public POTI.POTIRoute.POTIPoint ToPOTIPoint()
                {
                    POTI.POTIRoute.POTIPoint point = new POTI.POTIRoute.POTIPoint();
                    //point.Position = Position;
                    point.Position = new Vector3(PositionX, PositionY, PositionZ);

                    point.RouteSpeed = Speed;
                    point.Setting2 = Setting2;
                    return point;
                }

                public void RenderPicking(int group_id, int entry_id)
                {
                    ViewPlaneHandler vph = (Application.OpenForms[0] as Form1).vph;

                    VisualSettings Settings = (Application.OpenForms[0] as Form1).Settings;
                    Color pickingColor = SectionPicking.GetColor(Sections.Routes, group_id, entry_id);

                    Gl.glColor4f(pickingColor.R / 255f, pickingColor.G / 255f, pickingColor.B / 255f, 1f);
                    vph.drawPoint(Pos, Settings.PointSize + 2f);
                }


                public void RenderPoint()
                {
                    ViewPlaneHandler vph = (Application.OpenForms[0] as Form1).vph;

                    VisualSettings Settings = (Application.OpenForms[0] as Form1).Settings;
                    List<object> SelectedDots = (Application.OpenForms[0] as Form1).SelectedDots;

                    if (SelectedDots.Contains(this)) Gl.glColor4f(Settings.HighlightPointborderColor.R / 255f, Settings.HighlightPointborderColor.G / 255f, Settings.HighlightPointborderColor.B / 255f, Settings.HighlightPointborderColor.A);
                    else Gl.glColor4f(Settings.PointborderColor.R / 255f, Settings.PointborderColor.G / 255f, Settings.PointborderColor.B / 255f, Settings.PointborderColor.A);
                    vph.drawPoint(Pos, Settings.PointSize + 2f, true);

                    if (SelectedDots.Contains(this)) Gl.glColor4f(Settings.HighlightPointColor.R / 255f, Settings.HighlightPointColor.G / 255f, Settings.HighlightPointColor.B / 255f, Settings.HighlightPointColor.A);
                    else Gl.glColor4f(Settings.RouteColor.R / 255f, Settings.RouteColor.G / 255f, Settings.RouteColor.B / 255f, Settings.RouteColor.A);
                    vph.drawPoint(Pos, Settings.PointSize);
                }

                public void RenderLine()
                {
                    ViewPlaneHandler vph = (Application.OpenForms[0] as Form1).vph;
                    vph.draw2DVertice(Pos);
                }
            }

            public RouteGroup() { }

            public RouteGroup(POTI.POTIRoute path)
            {
                Setting1 = path.Setting1;
                Setting2 = path.Setting2;
                foreach (var entry in path.Points)
                {
                    Entries.Add(new RouteEntry(entry));
                }
            }

            public POTI.POTIRoute ToPOTIRoute()
            {
                POTI.POTIRoute route = new POTI.POTIRoute();
                route.NrPoints = (ushort)Entries.Count;
                route.Setting1 = Setting1;
                route.Setting2 = Setting2;
                foreach (var entry in Entries)
                    route.Points.Add(entry.ToPOTIPoint());
                return route;
            }

            public void Render(bool isCulling = false)
            {
                ViewPlaneHandler vph = (Application.OpenForms[0] as Form1).vph;
                if (!Visible) return;

                VisualSettings Settings = (Application.OpenForms[0] as Form1).Settings;
                Gl.glLineWidth(Settings.LineWidth);

                if (Settings.LinkPoints)
                {
                    Gl.glColor4f(Settings.RouteLinkColor.R / 255f, Settings.RouteLinkColor.G / 255f, Settings.RouteLinkColor.B / 255f, Settings.RouteLinkColor.A);
                    // As cameras ride it: closed or not, straight or curved.
                    Gl.glBegin(Gl.GL_LINE_STRIP);
                    foreach (Vector3 p in CameraRail.Polyline(this)) vph.draw2DVertice(p);
                    Gl.glEnd();
                }
                if (isCulling && vph.mode == ViewPlaneHandler.PLANE_MODES.XZ)
                {
                    Gl.glColor4f(Settings.RouteLinkColor.R / 255f, Settings.RouteLinkColor.G / 255f, Settings.RouteLinkColor.B / 255f, Settings.RouteLinkColor.A / (255f * 4f));
                    Gl.glBegin(Gl.GL_TRIANGLE_FAN);
                    for (int i = 0; i < 4; i++) vph.drawXZ(Entries[i].PositionX, Entries[i].PositionZ, Entries[i].PositionY);
                    Gl.glEnd();
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
        public POTI ToPOTI()
        {
            POTI data = new POTI();
            ushort Count = 0;
            data.NrRoutes = (ushort)Entries.Count;
            foreach (var group in Entries)
            {
                data.Routes.Add(group.ToPOTIRoute());
                Count += (ushort)group.Entries.Count;
            }
            data.NrPoints = Count;            
            return data;
        }

        public TreeNode GetTreeNode()
        {
            TreeNode node = new TreeNode("Routes");
            int i = 0;
            foreach (var group in Entries)
            {
                node.Nodes.Add("Group " + i.ToString());
                node.Nodes[i].Tag = Entries[i];
                node.Nodes[i].ImageIndex = 16;
                node.Nodes[i].SelectedImageIndex = 16;
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
            SimpleKMP kmp = (Application.OpenForms[0] as Form1).getKayEmPee();
            int j = 0;
            foreach (var group in Entries)
            {
                group.Render(kmp.currentCullingRoutes.Contains(j));
                j++;
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
