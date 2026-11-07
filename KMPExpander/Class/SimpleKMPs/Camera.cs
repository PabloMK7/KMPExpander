using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using LibCTR.Collections;
using KMPSections;
using System.Windows.Forms;
using Tao.OpenGl;
using System.Drawing;
using System.ComponentModel;
using Extensions;

namespace KMPExpander.Class.SimpleKMPs
{
    public class Camera : SectionBase<Camera.CameraEntry>
    {
        //[XmlElement("Entry")]
        //public List<CameraEntry> Entries = new List<CameraEntry>();
        public Camera() { }

        public Camera(CAME data)
        {
            foreach (var entry in data.Entries)
                Entries.Add(new CameraEntry(entry));
        }

        public class CameraEntry
        {
            [XmlAttribute, Browsable(false)]
            public Byte TypeID { get; set; }
            // The names of the classes the game builds for each type (Demo::DemoCameraDirector::createCamera_).
            public static string TypeName(int type)
            {
                switch (type)
                {
                    case 0: return "Goal";
                    case 1: return "Fixed";
                    case 2: return "Path";
                    case 3: return "Follow";
                    case 4: return "Fixed, Moving Look-at";
                    case 5: return "Path, Moving Look-at";
                    case 6: return "Follow Path";
                    case 7: return "Follow Path (2nd place)";
                    case 8: return "Follow Path (3rd place)";
                    default: return "Unknown";
                }
            }
            [XmlIgnore, Description("How the camera moves and where it looks. 0: goal camera, built but never shown by the game, it orbits the kart like type 3. 1: fixed at Position, looks at the kart. 2: rides its route, looks at the kart. 3: orbits the kart at the turn, tilt and distance of Viewpoint Begin. 4: fixed at Position, looks at a point moving from Viewpoint Begin to Viewpoint End. 5: rides its route, looks at a point moving from Viewpoint Begin to Viewpoint End (course intro). 6: circles the kart, its route points being offsets from the kart. 7 and 8: as 6, following the second (7) or third (8) driver of the award ceremony. Other values do nothing.")]
            public string Type
            {
                get
                {
                    return TypeID + " - " + TypeName(TypeID);
                }
                set
                {
                    TypeID = Byte.Parse(value.Split(' ')[0]);
                }
            }
            [XmlAttribute, Description("The next camera of the course intro chain, -1 for the last one. The intro plays the chain from its first camera (the first one with a Duration above 0 and a Video Next of 0, followed back through the cameras whose Next points to it). On follow cameras (types 0, 3, 6, 7, 8), -2 keeps the camera level instead of tilting it with the kart.")]
            public sbyte Next { get; set; }
            [XmlAttribute, Description("Not 0 on the pair of cameras that most likely rendered the course preview video of the menu, each points to the other. The game leaves such cameras out of the replays and of the intro.")]
            public Byte VideoNext { get; set; }
            [XmlAttribute, Description("The route the camera rides, -1 for none. Its speed is the Speed of the route points / 100 units per frame, blended between points. For types 6, 7 and 8 the route points are offsets from the kart.")]
            public sbyte RouteID { get; set; }
            [XmlAttribute, Description("Not read by the game. The speed comes from the route points.")]
            public UInt16 PointSpeed { get; set; }
            [XmlAttribute, Description("How fast the field of view goes from FOV Begin to FOV End, in hundredths of a degree per frame. 0 keeps FOV Begin.")]
            public UInt16 FOVSpeed { get; set; }
            [XmlAttribute, Description("Types 4 and 5: how fast the look-at point moves from Viewpoint Begin to Viewpoint End, in hundredths of a unit per frame. 0 keeps it at Viewpoint Begin. The last camera of the intro ignores it and takes Duration frames instead.")]
            public UInt16 ViewpointSpeed { get; set; }
            [XmlAttribute, Description("Not read by the game.")]
            public Byte StartFlag { get; set; }
            [XmlAttribute, Description("Not read by the game.")]
            public Byte VideoFlag { get; set; }
            [XmlIgnore, Browsable(false)]
            public Vector3 Pos { get; set; } = new Vector3(0, 0, 0);
            [XmlAttribute, Description("Where the camera stands, for cameras without a route.")]
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
            [XmlAttribute, Description("Where the camera stands, for cameras without a route.")]
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
            [XmlAttribute, Description("Where the camera stands, for cameras without a route.")]
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
            //public Vector3 Rotation { get; set; }
            [XmlAttribute, Description("Not used by the game's cameras.")]
            public Single RotationX { get; set; }
            [XmlAttribute, Description("Not used by the game's cameras.")]
            public Single RotationY { get; set; }
            [XmlAttribute, Description("Not used by the game's cameras.")]
            public Single RotationZ { get; set; }
            [XmlAttribute, Description("The field of view when the camera starts, in degrees.")]
            public Single FOVBegin { get; set; }
            [XmlAttribute, Description("The field of view the camera ends at, in degrees (see FOV Speed).")]
            public Single FOVEnd { get; set; }
            [XmlIgnore, Browsable(false)]
            public Vector3 VPBegin { get; set; } = new Vector3(0, 0, 0);
            private const string ViewpointBeginHelp = "Types 4 and 5: the point the camera looks at when it starts, in course coordinates. Types 0 and 3: X turns the camera around the kart from behind it (degrees), Y tilts it (degrees, negative is above) and Z is its distance from the kart.";
            [XmlAttribute("Viewpoint1X"), Description(ViewpointBeginHelp)]
            public Single ViewpointBeginX {
                get {
                    return VPBegin.X;
                }
                set {
                    VPBegin = new Vector3(value, VPBegin.Y, VPBegin.Z);
                }
            }
            [XmlAttribute("Viewpoint1Y"), Description(ViewpointBeginHelp)]
            public Single ViewpointBeginY
            {
                get
                {
                    return VPBegin.Y;
                }
                set
                {
                    VPBegin = new Vector3(VPBegin.X, value, VPBegin.Z);
                }
            }
            [XmlAttribute("Viewpoint1Z"), Description(ViewpointBeginHelp)]
            public Single ViewpointBeginZ
            {
                get
                {
                    return VPBegin.Z;
                }
                set
                {
                    VPBegin = new Vector3(VPBegin.X, VPBegin.Y, value);
                }
            }
            [XmlIgnore, Browsable(false)]
            public Vector3 VPEnd { get; set; } = new Vector3(0, 0, 0);
            private const string ViewpointEndHelp = "Types 4 and 5: the point the look-at point moves to, in course coordinates. Follow cameras (types 0, 3, 6, 7, 8): X turns the view sideways, Y up or down (degrees) and Z rolls it.";
            [XmlAttribute("Viewpoint2X"), Description(ViewpointEndHelp)]
            public Single ViewpointEndX
            {
                get
                {
                    return VPEnd.X;
                }
                set
                {
                    VPEnd = new Vector3(value, VPEnd.Y, VPEnd.Z);
                }
            }
            [XmlAttribute("Viewpoint2Y"), Description(ViewpointEndHelp)]
            public Single ViewpointEndY
            {
                get
                {
                    return VPEnd.Y;
                }
                set
                {
                    VPEnd = new Vector3(VPEnd.X, value, VPEnd.Z);
                }
            }
            [XmlAttribute("Viewpoint2Z"), Description(ViewpointEndHelp)]
            public Single ViewpointEndZ
            {
                get
                {
                    return VPEnd.Z;
                }
                set
                {
                    VPEnd = new Vector3(VPEnd.X, VPEnd.Y, value);
                }
            }
            [XmlAttribute, Browsable(false)]
            public Single DurationRaw { get; set; }
            [XmlIgnore, Description("Frames, or seconds with an s at the end. Above 0 only on the cameras of the course intro: the first such camera starts the intro chain. The intro shots last 232, 204 and 240 frames whatever is written here, on the last camera of type 5 it is the time its look-at point takes to reach Viewpoint End. Only the award ceremony plays each camera for its own Duration.")]
            public string Duration
            {
                get
                {
                    return DurationRaw.ToString() + " - " + (DurationRaw / 60).ToString() + "s";
                }
                set
                {
                    if (value.Length == 0) return;
                    if (value.ElementAt(value.Length - 1) == 's')
                    {
                        DurationRaw = (float.Parse(value.Substring(0, value.Length - 1)) * 60);
                    }
                    else
                    {
                        DurationRaw = float.Parse(value);
                    }
                }
            }

            public CameraEntry(CAME.CAMEEntry entry)
            {
                TypeID = entry.Type;
                Next = entry.Next;
                VideoNext = entry.VideoNext;
                RouteID = entry.RouteID;
                PointSpeed = entry.PointSpeed;
                FOVSpeed = entry.FOVSpeed;
                ViewpointSpeed = entry.ViewpointSpeed;
                StartFlag = entry.StartFlag;
                VideoFlag = entry.VideoFlag;
                PositionX = entry.Position.X;
                PositionY = entry.Position.Y;
                PositionZ = entry.Position.Z;
                RotationX = entry.Rotation.X;
                RotationY = entry.Rotation.Y;
                RotationZ = entry.Rotation.Z;
                FOVBegin = entry.FOVBegin;
                FOVEnd = entry.FOVEnd;
                VPBegin = entry.Viewpoint1;
                VPEnd = entry.Viewpoint2;
                DurationRaw = entry.Duration;
            }

            public CameraEntry() { }

            public CAME.CAMEEntry ToCAMEEntry()
            {
                CAME.CAMEEntry entry = new CAME.CAMEEntry();
                entry.Type = TypeID;
                entry.Next = Next;
                entry.VideoNext = VideoNext;
                entry.StartFlag = StartFlag;
                entry.VideoFlag = VideoFlag;
                entry.RouteID = RouteID;
                entry.PointSpeed = PointSpeed;
                entry.FOVSpeed = FOVSpeed;
                entry.ViewpointSpeed = ViewpointSpeed;
                entry.Position = new Vector3(PositionX, PositionY, PositionZ);
                entry.Rotation = new Vector3(RotationX, RotationY, RotationZ);
                entry.FOVBegin = FOVBegin;
                entry.FOVEnd = FOVEnd;
                entry.Viewpoint1 = VPBegin;
                entry.Viewpoint2 = VPEnd;
                entry.Duration = DurationRaw;
                return entry;
            }

            public void RenderPicking(int entry_id)
            {
                VisualSettings Settings = (Application.OpenForms[0] as Form1).Settings;
                Color pickingColor = SectionPicking.GetColor(Sections.Camera, 0, entry_id);
                ViewPlaneHandler vph = (Application.OpenForms[0] as Form1).vph;
                
                Gl.glColor4f(pickingColor.R / 255f, pickingColor.G / 255f, pickingColor.B / 255f, 1f);
                vph.drawPoint(Pos, Settings.PointSize + 2f);
            }

            public void RenderPoint()
            {
                VisualSettings Settings = (Application.OpenForms[0] as Form1).Settings;
                List<object> SelectedDots = (Application.OpenForms[0] as Form1).SelectedDots;
                ViewPlaneHandler vph = (Application.OpenForms[0] as Form1).vph;
                
                if (SelectedDots.Contains(this)) Gl.glColor4f(Settings.HighlightPointborderColor.R / 255f, Settings.HighlightPointborderColor.G / 255f, Settings.HighlightPointborderColor.B / 255f, Settings.HighlightPointborderColor.A);
                else Gl.glColor4f(Settings.PointborderColor.R / 255f, Settings.PointborderColor.G / 255f, Settings.PointborderColor.B / 255f, Settings.PointborderColor.A);
                vph.drawPoint(Pos, Settings.PointSize + 2f, true);

                if (SelectedDots.Contains(this)) Gl.glColor4f(Settings.HighlightPointColor.R / 255f, Settings.HighlightPointColor.G / 255f, Settings.HighlightPointColor.B / 255f, Settings.HighlightPointColor.A);
                else Gl.glColor4f(Settings.CameColor.R / 255f, Settings.CameColor.G / 255f, Settings.CameColor.B / 255f, Settings.CameColor.A);
                vph.drawPoint(Pos, Settings.PointSize);

                // Viewpoints, which are points of the course only for types 4 and 5
                if (SelectedDots.Contains(this) && (TypeID == 4 || TypeID == 5))
                {
                    Vector3 vp1 = VPBegin;
                    Vector3 vp2 = VPEnd;

                    Gl.glPointSize(Settings.PointSize / 2);
                    Gl.glBegin(Gl.GL_POINTS);
                    Gl.glColor4f((Settings.HighlightPointColor.R / 255f) + 0.1f, (Settings.HighlightPointColor.G / 255f) + 0.1f, (Settings.HighlightPointColor.B / 255f) + 0.1f, Settings.HighlightPointColor.A);
                    vph.draw2DVertice(vp1);
                    Gl.glColor4f((Settings.HighlightPointColor.R / 255f) - 0.1f, (Settings.HighlightPointColor.G / 255f) - 0.1f, (Settings.HighlightPointColor.B / 255f) - 0.1f, Settings.HighlightPointColor.A);
                    vph.draw2DVertice(vp2);
                    Gl.glEnd();
                }
            }
        }

        public CAME ToCAME()
        {
            CAME data = new CAME();
            data.NrEntries = (ushort)Entries.Count;
            foreach (var entry in Entries)
                data.Entries.Add(entry.ToCAMEEntry());
            return data;
        }

        public TreeNode GetTreeNode()
        {
            TreeNode node = new TreeNode("Camera Settings");
            node.Tag = this;
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

            foreach (var entry in Entries)
                entry.RenderPoint();
        }

        public void Transform(Vector3 translation, Vector3 scale)
        {
            foreach (var entry in Entries)
            {
                entry.Pos *= scale;
                entry.Pos += translation;

                // Only types 4 and 5 hold course coordinates in their viewpoints.
                if (entry.TypeID != 4 && entry.TypeID != 5) continue;
                entry.VPBegin = entry.VPBegin * scale + translation;
                entry.VPEnd = entry.VPEnd * scale + translation;
            }
        }
    }
}
