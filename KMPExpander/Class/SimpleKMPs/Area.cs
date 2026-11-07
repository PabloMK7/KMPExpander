using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using LibCTR.Collections;
using KMPSections;
using System.Windows.Forms;
using Extensions;
using System.ComponentModel;
using Tao.OpenGl;
using System.Drawing;

namespace KMPExpander.Class.SimpleKMPs
{
    public class Area : SectionBase<Area.AreaEntry>
    {

        //[XmlElement("Entry")]
        //public List<AreaEntry> Entries = new List<AreaEntry>();
        public Area() { }

        public Area(AREA data)
        {
            foreach (var entry in data.Entries)
                Entries.Add(new AreaEntry(entry));
        }

        public class AreaEntry
        {
            [XmlAttribute, Browsable(false)]
            public Byte ShapeMode { get; set; }
            [XmlIgnore, Description("0: a box, 1000 x ScaleX wide, 1000 x ScaleZ deep, from its position up to 1000 x ScaleY above it. 1: a cylinder (an ellipse when ScaleX and ScaleZ differ) of radius 500 x ScaleX and 500 x ScaleZ, up to 500 x ScaleY above its position, with no bottom (everything below its top is inside). The game skips areas of any other shape, which can crash it.")]
            public string Shape
            {
                get
                {
                    switch(ShapeMode)
                    {
                        case 0:
                            return "0 - Box";
                        case 1:
                            return "1 - Cylinder";
                        default:
                            return ShapeMode.ToString() + " - Invalid";
                    }
                }
                set
                {
                    ShapeMode = Byte.Parse(value.Split(' ')[0]);
                }
            }
            [XmlAttribute, Browsable(false)]
            public Byte TypeID { get; set; }
            [XmlIgnore, Description("What the area does. 0: while the watched kart is inside, replays (and the view 5 seconds after the finish) show the camera in CAMEIndex. 1: switches on one of the course slot's environment effects (falling leaves, snow, rain...) for the player inside, chosen by Settings1. 3: karts on moving road collision (KCL type 0x0C) inside it are pushed along the route in RouteID. 11: plays an ambient sound, or a sound placed along the route in RouteID.")]
            public string Type
            {
                get
                {
                    switch (TypeID)
                    {
                        case 0:
                            return "0 - Camera";
                        case 1:
                            return "1 - Environment Effect";
                        case 3:
                            return "3 - Moving Road";
                        case 11:
                            return "11 - Sound";
                        default:
                            return TypeID.ToString() + " - Unused";
                    }
                }
                set
                {
                    TypeID = Byte.Parse(value.Split(' ')[0]);
                }
            }
            [XmlAttribute("CameraId"), Description("Camera areas (type 0) only: the camera shown while the watched kart is inside. The area is ignored when the index is -1, or when the camera is of type 0 or has a Video Next other than 0. Without any camera area the course has no replay cameras and no intro.")]
            public SByte CAMEIndex { get; set; }
            [XmlAttribute, Description("Camera areas (type 0) only: where camera areas overlap, the one with the highest priority is used; the area the kart is already in is kept while it stays inside. Other area types ignore it and use the first area in the list.")]
            public Byte Priority { get; set; }
            //public Vector3 Position { get; set; }
            [XmlIgnore, Browsable(false)]
            public Vector3 Pos { get; set; } = new Vector3(0, 0, 0);
            [XmlAttribute, Description("Position of the centre of the area.")]
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
            [XmlAttribute, Description("Height of the bottom of the area: a box up to 1000 x ScaleY units, a cylinder up to 500 x ScaleY (and a cylinder also covers everything below).")]
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
            [XmlAttribute, Description("Position of the centre of the area.")]
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
            [XmlAttribute, Description("Rotation of the area.")]
            public Single RotationX { get; set; }
            [XmlAttribute, Description("Rotation of the area.")]
            public Single RotationY { get; set; }
            [XmlAttribute, Description("Rotation of the area.")]
            public Single RotationZ { get; set; }
            [XmlAttribute, Description("Width: a box is 1000 x ScaleX units wide, a cylinder has a radius of 500 x ScaleX.")]
            public Single ScaleX { get; set; }
            [XmlAttribute, Description("Height: a box is 1000 x ScaleY units tall, a cylinder reaches 500 x ScaleY above its position.")]
            public Single ScaleY { get; set; }
            [XmlAttribute, Description("Depth: a box is 1000 x ScaleZ units deep, a cylinder has a radius of 500 x ScaleZ.")]
            public Single ScaleZ { get; set; }
            //public Vector3 Scale { get; set; }
            [XmlAttribute, TypeConverter(typeof(HexTypeConverter)), Description("Environment effect areas (type 1): the effect slot switched on, 0 to 3. What each slot shows depends on the course slot the course replaces. Sound areas (type 11): with RouteID -1 or 0, the ambient sound played inside (0 to 3 stop the ambient sound), with a route, the sound placed along it. Not read by camera and moving road areas.")]
            public UInt16 Settings1 { get; set; }
            [XmlAttribute, TypeConverter(typeof(HexTypeConverter)), Description("Environment effect areas (type 1): a delay, in seconds after the countdown starts, during which the area switches nothing on. Sound areas (type 11) with a route: 0 places the sound at the nearest point of the route, 1 the same ignoring heights, 2 on a flat surface at the height of the first route point (such as the sea). Not read by camera and moving road areas.")]
            public UInt16 Settings2 { get; set; }
            [XmlAttribute, Description("Moving road areas (type 3): the route the karts are pushed along, from each point toward the next, as strongly as the Speed of the route points / 100. Sound areas (type 11): -1 or 0 for an ambient sound, 1 or more for a sound placed along that route. Not read by camera and environment effect areas.")]
            public sbyte RouteID { get; set; }
            [XmlAttribute, Description("Not read by the game. -1 in every original course.")]
            public sbyte EnemyID { get; set; }
            [XmlAttribute, TypeConverter(typeof(HexTypeConverter)), Description("Not read by the game, probably padding. 0 in every original course.")]
            public UInt16 Unknown { get; set; }

            public AreaEntry(AREA.AREAEntry entry)
            {
                ShapeMode = entry.Mode;
                TypeID = entry.Type;
                CAMEIndex = entry.CAMEIndex;
                Priority = entry.Unknown1;
                PositionX = entry.Position.X;
                PositionY = entry.Position.Y;
                PositionZ = entry.Position.Z;
                RotationX = entry.Rotation.X;
                RotationY = entry.Rotation.Y;
                RotationZ = entry.Rotation.Z;
                ScaleX = entry.Scale.X;
                ScaleY = entry.Scale.Y;
                ScaleZ = entry.Scale.Z;
                Settings1 = entry.Settings1;
                Settings2 = entry.Settings2;
                RouteID = entry.RouteID;
                EnemyID = entry.EnemyID;
                Unknown = entry.Unknown5;
            }

            public AreaEntry() { }

            public AREA.AREAEntry ToAREAEntry()
            {
                AREA.AREAEntry entry = new AREA.AREAEntry();
                entry.Mode = ShapeMode;
                entry.Type = TypeID;
                entry.CAMEIndex = CAMEIndex;
                entry.Unknown1 = Priority;
                entry.Position = new Vector3(PositionX, PositionY, PositionZ);
                entry.Rotation = new Vector3(RotationX, RotationY, RotationZ);
                entry.Scale = new Vector3(ScaleX, ScaleY, ScaleZ);
                entry.Settings1 = Settings1;
                entry.Settings2 = Settings2;
                entry.RouteID = RouteID;
                entry.EnemyID = EnemyID;
                entry.Unknown5 = Unknown;

                return entry;
            }

            // Whether p is inside the area, as MapdataAreaCalcBox / MapdataAreaCalcCylinder test it: in the area's own
            // axes, a box from 0 to 1000 x ScaleY up and 500 x Scale X and Z either side; a cylinder below 500 x ScaleY
            // and within the ellipse of radii 500 x ScaleX and 500 x ScaleZ. Other shapes contain nothing.
            public bool Contains(Vector3 p)
            {
                if (ShapeMode > 1) return false;
                double rx = RotationX * Math.PI / 180, ry = RotationY * Math.PI / 180, rz = RotationZ * Math.PI / 180;
                double cx = Math.Cos(rx), sx = Math.Sin(rx), cy = Math.Cos(ry), sy = Math.Sin(ry), cz = Math.Cos(rz), sz = Math.Sin(rz);
                double[,] r = {
                    { cy * cz, sx * sy * cz - cx * sz, cx * sy * cz + sx * sz },
                    { cy * sz, sx * sy * sz + cx * cz, cx * sy * sz - sx * cz },
                    { -sy, sx * cy, cx * cy } };
                double dx = p.X - PositionX, dy = p.Y - PositionY, dz = p.Z - PositionZ;
                double lx = r[0, 0] * dx + r[1, 0] * dy + r[2, 0] * dz;
                double ly = r[0, 1] * dx + r[1, 1] * dy + r[2, 1] * dz;
                double lz = r[0, 2] * dx + r[1, 2] * dy + r[2, 2] * dz;
                if (ShapeMode == 0)
                    return ly >= 0 && ly <= 1000.0 * ScaleY && Math.Abs(lx) <= 500.0 * ScaleX && Math.Abs(lz) <= 500.0 * ScaleZ;
                if (ly > 500.0 * ScaleY) return false;
                double ratio = ScaleX / (double)ScaleZ, radius = 500.0 * ScaleX;
                return lx * lx + (lz * ratio) * (lz * ratio) <= radius * radius;
            }

            public void RenderPicking(int entry_id)
            {
                float sizeX = 500f * ScaleX;
                float sizeY = 500f * ScaleZ;
                VisualSettings Settings = (Application.OpenForms[0] as Form1).Settings;
                Color pickingColor = SectionPicking.GetColor(Sections.Area, 0, entry_id);
                ViewPlaneHandler vph = (Application.OpenForms[0] as Form1).vph;

                Gl.glColor4f(pickingColor.R / 255f, pickingColor.G / 255f, pickingColor.B / 255f, 1f);
                vph.drawPoint(new Vector3(PositionX, PositionY, PositionZ), Settings.PointSize + 2f);
            }
            public void DrawFilledCircle(float cx, float cy, float rx, float ry, int num_segments, Color Border, Color Fill)
            {
                double[] vertexlist = new double[(num_segments + 1) * 2];

                Gl.glColor4f(Fill.R / 255f, Fill.G / 255f, Fill.B / 255f, Fill.A / (255f * 4f));

                Gl.glBegin(Gl.GL_TRIANGLE_FAN);
                Gl.glVertex2d(cx, cy);
                for (int ii = 0; ii < (num_segments + 1) * 2; ii += 2)
                {
                    double theta = 2.0f * 3.1415926f * (ii / 2) / num_segments;//get the current angle

                    double x = rx * Math.Cos(theta);//calculate the x component
                    double y = ry * Math.Sin(theta);//calculate the y component

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
            // The area as a volume in the 3D view: a box (shape 0) 1000 * ScaleY high above its position, or an elliptic
            // cylinder up to 500 * ScaleY above it. A cylinder has no bottom, so its sides go on downward, fading out.
            private void RenderVolume(List<object> SelectedDots, VisualSettings Settings, int type)
            {
                float sizeX = 500f * ScaleX, sizeZ = 500f * ScaleZ, height = (type == 0 ? 1000f : 500f) * ScaleY;
                Color c = SelectedDots.Contains(this) ? Settings.HighlightPointColor : Settings.AreaColor;
                Color edge = SelectedDots.Contains(this) ? Settings.HighlightPointborderColor : Settings.AreaColor;
                int sides = type == 0 ? 4 : 24;
                float[] x = new float[sides + 1], z = new float[sides + 1];
                for (int i = 0; i <= sides; i++)
                {
                    double a = type == 0 ? Math.PI / 4 + i * Math.PI / 2 : i * 2 * Math.PI / sides;
                    float f = type == 0 ? (float)Math.Sqrt(2) : 1f;
                    x[i] = sizeX * f * (float)Math.Cos(a);
                    z[i] = sizeZ * f * (float)Math.Sin(a);
                }
                Gl.glPushMatrix();
                Gl.glLoadIdentity();
                Gl.glMultMatrixf((Application.OpenForms[0] as Form1).vph.modelMatrix(Pos, new Vector3(RotationX, RotationY, RotationZ), new Vector3(1, 1, 1)));
                // See-through sides, top and bottom, which do not hide what is drawn behind them later.
                Gl.glDepthMask(Gl.GL_FALSE);
                Gl.glColor4f(c.R / 255f, c.G / 255f, c.B / 255f, 0.15f);
                Gl.glBegin(Gl.GL_QUAD_STRIP);
                for (int i = 0; i <= sides; i++)
                {
                    Gl.glVertex3f(x[i], 0, z[i]);
                    Gl.glVertex3f(x[i], height, z[i]);
                }
                Gl.glEnd();
                foreach (float h in type == 0 ? new float[] { 0, height } : new float[] { height })
                {
                    Gl.glBegin(Gl.GL_POLYGON);
                    for (int i = 0; i < sides; i++) Gl.glVertex3f(x[i], h, z[i]);
                    Gl.glEnd();
                }
                if (type != 0)
                {
                    float depth = Math.Max(1000f, 2 * Math.Max(Math.Abs(sizeX), Math.Abs(sizeZ)));
                    Gl.glBegin(Gl.GL_QUAD_STRIP);
                    for (int i = 0; i <= sides; i++)
                    {
                        Gl.glColor4f(c.R / 255f, c.G / 255f, c.B / 255f, 0.15f);
                        Gl.glVertex3f(x[i], 0, z[i]);
                        Gl.glColor4f(c.R / 255f, c.G / 255f, c.B / 255f, 0f);
                        Gl.glVertex3f(x[i], -depth, z[i]);
                    }
                    Gl.glEnd();
                }
                Gl.glDepthMask(Gl.GL_TRUE);
                Gl.glColor4f(edge.R / 255f, edge.G / 255f, edge.B / 255f, 1f);
                Gl.glLineWidth(Settings.LineWidth);
                foreach (float h in type == 0 ? new float[] { 0, height } : new float[] { height })
                {
                    Gl.glBegin(Gl.GL_LINE_LOOP);
                    for (int i = 0; i < sides; i++) Gl.glVertex3f(x[i], h, z[i]);
                    Gl.glEnd();
                }
                Gl.glBegin(Gl.GL_LINES);
                for (int i = 0; i < sides; i += type == 0 ? 1 : sides / 4)
                {
                    Gl.glColor4f(edge.R / 255f, edge.G / 255f, edge.B / 255f, 1f);
                    Gl.glVertex3f(x[i], height, z[i]);
                    if (type != 0) Gl.glColor4f(edge.R / 255f, edge.G / 255f, edge.B / 255f, 0f);
                    Gl.glVertex3f(x[i], type == 0 ? 0 : -Math.Max(1000f, 2 * Math.Max(Math.Abs(sizeX), Math.Abs(sizeZ))), z[i]);
                }
                Gl.glEnd();
                Gl.glPopMatrix();
            }

            public void RenderZone(List<object> SelectedDots, VisualSettings Settings, int type)
            {
                if ((Application.OpenForms[0] as Form1).vph.Is3D)
                {
                    RenderVolume(SelectedDots, Settings, type);
                    return;
                }
                float sizeX = 500f * ScaleX;
                float sizeY = 500f * ScaleZ;
                if (type == 0)
                {
                    if (SelectedDots.Contains(this)) Gl.glColor4f(Settings.HighlightPointColor.R / 255f, Settings.HighlightPointColor.G / 255f, Settings.HighlightPointColor.B / 255f, 0.25f);
                    else Gl.glColor4f(Settings.AreaColor.R / 255f, Settings.AreaColor.G / 255f, Settings.AreaColor.B / 255f, 0.25f);
                    Gl.glPushMatrix();
                    (Application.OpenForms[0] as Form1).vph.translateXZ(PositionX, PositionZ, PositionY);
                    Gl.glRotatef(-RotationY, 0, 0, 1);
                    Gl.glBegin(Gl.GL_QUADS);
                    Gl.glVertex2f(-sizeX, -sizeY);
                    Gl.glVertex2f(sizeX, -sizeY);
                    Gl.glVertex2f(sizeX, sizeY);
                    Gl.glVertex2f(-sizeX, sizeY);
                    Gl.glEnd();

                    if (SelectedDots.Contains(this)) Gl.glColor4f(Settings.HighlightPointborderColor.R / 255f, Settings.HighlightPointborderColor.G / 255f, Settings.HighlightPointborderColor.B / 255f, 1f);
                    else Gl.glColor4f(Settings.AreaColor.R / 255f, Settings.AreaColor.G / 255f, Settings.AreaColor.B / 255f, 1f);
                    Gl.glLineWidth(Settings.LineWidth);
                    Gl.glBegin(Gl.GL_LINES);
                    Gl.glVertex2f(-sizeX, sizeY);
                    Gl.glVertex2f(sizeX, sizeY);
                    Gl.glVertex2f(sizeX, sizeY);
                    Gl.glVertex2f(sizeX, -sizeY);
                    Gl.glVertex2f(sizeX, -sizeY);
                    Gl.glVertex2f(-sizeX, -sizeY);
                    Gl.glVertex2f(-sizeX, -sizeY);
                    Gl.glVertex2f(-sizeX, sizeY);
                    Gl.glEnd();
                    Gl.glPopMatrix();
                } else if (type == 1)
                {
                    Gl.glPushMatrix();
                    (Application.OpenForms[0] as Form1).vph.translateXZ(PositionX, PositionZ, PositionY);
                    Gl.glRotatef(-RotationY, 0, 0, 1);
                    Gl.glLineWidth(Settings.LineWidth);

                    if (SelectedDots.Contains(this)) DrawFilledCircle(0, 0, sizeX, sizeY, 24, Settings.HighlightPointborderColor, Settings.HighlightPointColor);
                    else DrawFilledCircle(0, 0, sizeX, sizeY, 24, Settings.AreaColor, Settings.AreaColor);

                    Gl.glPopMatrix();
                }
            }
            public void RenderPoint()
            {
                VisualSettings Settings = (Application.OpenForms[0] as Form1).Settings;
                ViewPlaneHandler vph = (Application.OpenForms[0] as Form1).vph;
                List<object> SelectedDots = (Application.OpenForms[0] as Form1).SelectedDots;

                if (SelectedDots.Contains(this)) Gl.glColor4f(Settings.HighlightPointborderColor.R / 255f, Settings.HighlightPointborderColor.G / 255f, Settings.HighlightPointborderColor.B / 255f, Settings.HighlightPointborderColor.A);
                else Gl.glColor4f(Settings.PointborderColor.R / 255f, Settings.PointborderColor.G / 255f, Settings.PointborderColor.B / 255f, Settings.PointborderColor.A);
                vph.drawPoint(new Vector3(PositionX, PositionY, PositionZ), Settings.PointSize + 2f, true);

                if (SelectedDots.Contains(this)) Gl.glColor4f(Settings.HighlightPointColor.R / 255f, Settings.HighlightPointColor.G / 255f, Settings.HighlightPointColor.B / 255f, Settings.HighlightPointColor.A);
                else Gl.glColor4f(Settings.AreaColor.R / 255f, Settings.AreaColor.G / 255f, Settings.AreaColor.B / 255f, Settings.AreaColor.A);
                vph.drawPoint(new Vector3(PositionX, PositionY, PositionZ), Settings.PointSize);

                RenderZone(SelectedDots, Settings, ShapeMode);
            }
        }
        public AREA ToAREA()
        {
            AREA data = new AREA();
            data.NrEntries = (uint)Entries.Count;
            foreach (var entry in Entries)
                data.Entries.Add(entry.ToAREAEntry());
            return data;
        }

        public TreeNode GetTreeNode()
        {
            TreeNode node = new TreeNode("Area");
            node.Tag = this;
            return node;
        }

        public void Render(bool picking)
        {
            ViewPlaneHandler vph = (Application.OpenForms[0] as Form1).vph;
            if (!Visible || vph.mode != ViewPlaneHandler.PLANE_MODES.XZ) return;

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

                entry.ScaleX *= scale.X;
                entry.ScaleY *= scale.Y;
                entry.ScaleZ *= scale.Z;
            }
        }
    }
}
