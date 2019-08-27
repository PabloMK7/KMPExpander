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
            [XmlAttribute, Category("Route")]
            public bool Loop { get; set; }
            [XmlAttribute, Category("Route")]
            public bool Smooth { get; set; }
            //[XmlElement("Entry")]
            //public List<RouteEntry> Entries = new List<RouteEntry>();
            public class RouteEntry
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
                public UInt16 Speed { get; set; }
                [XmlAttribute, TypeConverter(typeof(HexTypeConverter))]
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

                    Gl.glPointSize(Settings.PointSize + 2f);
                    Gl.glBegin(Gl.GL_POINTS);
                    if (SelectedDots.Contains(this)) Gl.glColor4f(Settings.HighlightPointborderColor.R / 255f, Settings.HighlightPointborderColor.G / 255f, Settings.HighlightPointborderColor.B / 255f, Settings.HighlightPointborderColor.A);
                    else Gl.glColor4f(Settings.PointborderColor.R / 255f, Settings.PointborderColor.G / 255f, Settings.PointborderColor.B / 255f, Settings.PointborderColor.A);
                    vph.draw2DVertice(Pos);
                    Gl.glEnd();

                    Gl.glPointSize(Settings.PointSize);
                    Gl.glBegin(Gl.GL_POINTS);
                    if (SelectedDots.Contains(this)) Gl.glColor4f(Settings.HighlightPointColor.R / 255f, Settings.HighlightPointColor.G / 255f, Settings.HighlightPointColor.B / 255f, Settings.HighlightPointColor.A);
                    else Gl.glColor4f(Settings.RouteColor.R / 255f, Settings.RouteColor.G / 255f, Settings.RouteColor.B / 255f, Settings.RouteColor.A);
                    vph.draw2DVertice(Pos);
                    Gl.glEnd();
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
                Loop = (path.Setting1 & 1) == 1;
                Smooth = (path.Setting2 & 1) == 1;
                foreach (var entry in path.Points)
                {
                    Entries.Add(new RouteEntry(entry));
                }
            }

            public POTI.POTIRoute ToPOTIRoute()
            {
                POTI.POTIRoute route = new POTI.POTIRoute();
                route.NrPoints = (ushort)Entries.Count;
                route.Setting1 = (byte)(Loop ? 1 : 0);
                route.Setting2 = (byte)(Smooth ? 1 : 0);
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
                    for (int i = 0; i < Entries.Count - 1; i++)
                    {
                        Gl.glBegin(Gl.GL_LINES);
                        Entries[i].RenderLine();
                        Entries[i + 1].RenderLine();
                        Gl.glEnd();
                    }
                    if (Loop)
                    {
                        Gl.glBegin(Gl.GL_LINES);
                        Entries[Entries.Count-1].RenderLine();
                        Entries[0].RenderLine();
                        Gl.glEnd();
                    }
                }
                if (isCulling && vph.mode == ViewPlaneHandler.PLANE_MODES.XZ)
                {
                    Gl.glColor4f(Settings.RouteLinkColor.R / 255f, Settings.RouteLinkColor.G / 255f, Settings.RouteLinkColor.B / 255f, Settings.RouteLinkColor.A / (255f * 4f));
                    Gl.glBegin(Gl.GL_TRIANGLE_FAN);
                    Gl.glVertex2d(Entries[0].PositionX, Entries[0].PositionZ);
                    Gl.glVertex2d(Entries[1].PositionX, Entries[1].PositionZ);
                    Gl.glVertex2d(Entries[2].PositionX, Entries[2].PositionZ);
                    Gl.glVertex2d(Entries[3].PositionX, Entries[3].PositionZ);
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

    }
}
