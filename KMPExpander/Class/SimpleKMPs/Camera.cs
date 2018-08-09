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
            [XmlAttribute]
            public string Type
            {
                get
                {
                    switch (TypeID)
                    {
                        case 0:
                            return "0 - Online Preview";
                        case 1:
                            return "1 - Fix Searh";
                        case 2:
                            return "2 - Route Search";
                        case 3:
                            return "3 - Fix Kart Follow";
                        case 5:
                            return "5 - Opening";
                        case 6:
                            return "6 - Route Kart Follow";
                        default:
                            return TypeID + " - Unknown";
                    }
                }
                set
                {
                    TypeID = Byte.Parse(value);
                }
            }
            [XmlAttribute]
            public sbyte Next { get; set; }
            [XmlAttribute]
            public Byte VideoNext { get; set; }
            [XmlAttribute]
            public sbyte RouteID { get; set; }
            [XmlAttribute]
            public UInt16 PointSpeed { get; set; }
            [XmlAttribute]
            public UInt16 FOVSpeed { get; set; }
            [XmlAttribute]
            public UInt16 ViewpointSpeed { get; set; }
            [XmlAttribute]
            public Byte StartFlag { get; set; }
            [XmlAttribute]
            public Byte VideoFlag { get; set; }
            [XmlAttribute]
            public Single PositionX { get; set; }
            [XmlAttribute]
            public Single PositionY { get; set; }
            [XmlAttribute]
            public Single PositionZ { get; set; }
            //public Vector3 Rotation { get; set; }
            [XmlAttribute]
            public Single RotationX { get; set; }
            [XmlAttribute]
            public Single RotationY { get; set; }
            [XmlAttribute]
            public Single RotationZ { get; set; }
            [XmlAttribute]
            public Single FOVBegin { get; set; }
            [XmlAttribute]
            public Single FOVEnd { get; set; }
            [XmlAttribute]
            public Single Viewpoint1X { get; set; }
            [XmlAttribute]
            public Single Viewpoint1Y { get; set; }
            [XmlAttribute]
            public Single Viewpoint1Z { get; set; }
            //public Vector3 Viewpoint1 { get; set; }
            [XmlAttribute]
            public Single Viewpoint2X { get; set; }
            [XmlAttribute]
            public Single Viewpoint2Y { get; set; }
            [XmlAttribute]
            public Single Viewpoint2Z { get; set; }
            //public Vector3 Viewpoint2 { get; set; }
            [XmlAttribute, Browsable(false)]
            public Single DurationRaw { get; set; }
            [XmlAttribute]
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
                Viewpoint1X = entry.Viewpoint1.X;
                Viewpoint1Y = entry.Viewpoint1.Y;
                Viewpoint1Z = entry.Viewpoint1.Z;
                Viewpoint2X = entry.Viewpoint2.X;
                Viewpoint2Y = entry.Viewpoint2.Y;
                Viewpoint2Z = entry.Viewpoint2.Z;
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
                entry.Viewpoint1 = new Vector3(Viewpoint1X,Viewpoint1Y, Viewpoint1Z);
                entry.Viewpoint2 = new Vector3(Viewpoint2X,Viewpoint2Y, Viewpoint2Z);
                entry.Duration = DurationRaw;
                return entry;
            }

            public void RenderPicking(int entry_id)
            {
                VisualSettings Settings = (Application.OpenForms[0] as Form1).Settings;
                Color pickingColor = SectionPicking.GetColor(Sections.Camera, 0, entry_id);

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

                Gl.glPointSize(Settings.PointSize + 2f);
                Gl.glBegin(Gl.GL_POINTS);
                if (SelectedDots.Contains(this)) Gl.glColor4f(Settings.HighlightPointborderColor.R / 255f, Settings.HighlightPointborderColor.G / 255f, Settings.HighlightPointborderColor.B / 255f, Settings.HighlightPointborderColor.A);
                else Gl.glColor4f(Settings.PointborderColor.R / 255f, Settings.PointborderColor.G / 255f, Settings.PointborderColor.B / 255f, Settings.PointborderColor.A);
                Gl.glVertex2f(PositionX, PositionZ);
                Gl.glEnd();

                Gl.glPointSize(Settings.PointSize);
                Gl.glBegin(Gl.GL_POINTS);
                if (SelectedDots.Contains(this)) Gl.glColor4f(Settings.HighlightPointColor.R / 255f, Settings.HighlightPointColor.G / 255f, Settings.HighlightPointColor.B / 255f, Settings.HighlightPointColor.A);
                else Gl.glColor4f(Settings.CameColor.R / 255f, Settings.CameColor.G / 255f, Settings.CameColor.B / 255f, Settings.CameColor.A);
                Gl.glVertex2f(PositionX, PositionZ);
                Gl.glEnd();

                // Viewpoints
                if (SelectedDots.Contains(this))
                {
                    Gl.glPointSize(Settings.PointSize / 2);
                    Gl.glBegin(Gl.GL_POINTS);
                    Gl.glColor4f((Settings.HighlightPointColor.R / 255f) + 0.1f, (Settings.HighlightPointColor.G / 255f) + 0.1f, (Settings.HighlightPointColor.B / 255f) + 0.1f, Settings.HighlightPointColor.A);
                    Gl.glVertex2d(Viewpoint1X, Viewpoint1Z);
                    Gl.glColor4f((Settings.HighlightPointColor.R / 255f) - 0.1f, (Settings.HighlightPointColor.G / 255f) - 0.1f, (Settings.HighlightPointColor.B / 255f) - 0.1f, Settings.HighlightPointColor.A);
                    Gl.glVertex2d(Viewpoint2X, Viewpoint2Z);
                    Gl.glEnd();
                    Gl.glBegin(Gl.GL_LINES);
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
    }
}
