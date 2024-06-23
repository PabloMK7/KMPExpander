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
    public class Objects : SectionBase<Objects.ObjectEntry>
    {
        //[XmlElement("Entry")]
        //public List<ObjectEntry> Entries = new List<ObjectEntry>();
        public Objects() { }

        public Objects(GOBJ data)
        {
            foreach (var entry in data.Entries)
                Entries.Add(new ObjectEntry(entry));
        }

        public class ObjectEntry
        {
            [XmlIgnore, Browsable(false)]
            private UInt16 ObjectIDRaw;
            [XmlAttribute, Browsable(false)]
            public UInt16 ObjectID
            {
                get
                {
                    return ObjectIDRaw; 
                }
                set
                {
                    ObjectIDRaw = (Application.OpenForms[0] as Form1).lastObjectID = value;
                }
            }
            [XmlIgnore, TypeConverter(typeof(HexTypeConverter))]
            public string ObjectType
            {
                get
                {
                    ObjList objlist = (Application.OpenForms[0] as Form1).objlist;
                    if (!objlist.displayName.ContainsKey(ObjectID)) return "? [Unknown] (" + ObjectID.ToString("X4")+")";
                    return objlist.GetDisplayName(ObjectID) + " (" + ObjectID.ToString("X4") + ")" ;
                }
                set { ObjectID = UInt16.Parse(value); }
            }
            [XmlAttribute, TypeConverter(typeof(HexTypeConverter))]
            public UInt16 Unknown1 { get; set; }
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
            //public Vector3 Rotation { get; set; }
            [XmlAttribute]
            public Single RotationX { get; set; }
            [XmlAttribute]
            public Single RotationY { get; set; }
            [XmlAttribute]
            public Single RotationZ { get; set; }
            [XmlAttribute]
            public Single ScaleX { get; set; }
            [XmlAttribute]
            public Single ScaleY { get; set; }
            [XmlAttribute]
            public Single ScaleZ { get; set; }
            //public Vector3 Scale { get; set; }
            [XmlAttribute]
            public Int16 RouteID { get; set; }
            [XmlAttribute]
            public UInt16 Settings1 { get; set; }
            [XmlAttribute]
            public UInt16 Settings2 { get; set; }
            [XmlAttribute]
            public UInt16 Settings3 { get; set; }
            [XmlAttribute]
            public UInt16 Settings4 { get; set; }
            [XmlAttribute]
            public UInt16 Settings5 { get; set; }
            [XmlAttribute]
            public UInt16 Settings6 { get; set; }
            [XmlAttribute]
            public UInt16 Settings7 { get; set; }
            [XmlAttribute]
            public UInt16 Settings8 { get; set; }
            [XmlAttribute]
            public UInt16 Visibility { get; set; }
            [XmlAttribute]
            public Int16 EnemyRoute { get; set; }
            [XmlAttribute, TypeConverter(typeof(HexTypeConverter))]
            public UInt16 Unknown3 { get; set; }

            public ObjectEntry(GOBJ.GOBJEntry entry)
            {
                ObjectID = entry.ObjectID;
                Unknown1 = entry.Unknown1;
                PositionX = entry.Position.X;
                PositionY = entry.Position.Y;
                PositionZ = entry.Position.Z;
                RotationX = entry.Rotation.X;
                RotationY = entry.Rotation.Y;
                RotationZ = entry.Rotation.Z;
                ScaleX = entry.Scale.X;
                ScaleY = entry.Scale.Y;
                ScaleZ = entry.Scale.Z;
                RouteID = entry.RouteID;
                Settings1 = entry.Settings1;
                Settings2 = entry.Settings2;
                Settings3 = entry.Settings3;
                Settings4 = entry.Settings4;
                Settings5 = entry.Settings5;
                Settings6 = entry.Settings6;
                Settings7 = entry.Settings7;
                Settings8 = entry.Settings8;
                Visibility = entry.Visibility;
                EnemyRoute = entry.Unknown2;
                Unknown3 = entry.Unknown3;
            }

            public ObjectEntry()
            {
                ObjectID = (Application.OpenForms[0] as Form1).lastObjectID;
                Unknown1 = 0;
                PositionX = 0;
                PositionZ = 0;
                PositionY = 0;
                RotationX = 0;
                RotationY = 0;
                RotationZ = 0;
                ScaleX = 1;
                ScaleY = 1;
                ScaleZ = 1;
                RouteID = -1;
                Settings1 = 0;
                Settings2 = 0;
                Settings3 = 0;
                Settings4 = 0;
                Settings5 = 0;
                Settings6 = 0;
                Settings7 = 0;
                Settings8 = 0;
                Visibility = 7;
                EnemyRoute = -1;
                Unknown3 = 0;
            }

            public GOBJ.GOBJEntry ToGOBJEntry()
            {
                GOBJ.GOBJEntry entry = new GOBJ.GOBJEntry();
                entry.ObjectID = ObjectID;
                entry.Unknown1 = Unknown1;
                entry.Position = new Vector3(PositionX, PositionY, PositionZ);
                entry.Rotation = new Vector3(RotationX, RotationY, RotationZ);
                entry.Scale = new Vector3(ScaleX, ScaleY, ScaleZ);
                entry.RouteID = RouteID;
                entry.Settings1 = Settings1;
                entry.Settings2 = Settings2;
                entry.Settings3 = Settings3;
                entry.Settings4 = Settings4;
                entry.Settings5 = Settings5;
                entry.Settings6 = Settings6;
                entry.Settings7 = Settings7;
                entry.Settings8 = Settings8;
                entry.Visibility = Visibility;
                entry.Unknown2 = EnemyRoute;
                entry.Unknown3 = Unknown3;
                return entry;
            }

            public void RenderPicking(int entry_id)
            {
                ViewPlaneHandler vph = (Application.OpenForms[0] as Form1).vph;

                VisualSettings Settings = (Application.OpenForms[0] as Form1).Settings;
                Color pickingColor = SectionPicking.GetColor(Sections.Objects, 0, entry_id);

                Gl.glPointSize(Settings.PointSize + 2f);
                Gl.glBegin(Gl.GL_POINTS);
                Gl.glColor4f(pickingColor.R / 255f, pickingColor.G / 255f, pickingColor.B / 255f, 1f);
                vph.draw2DVertice(Pos);
                Gl.glEnd();
            }

            public void RenderPoint(bool picking)
            {
                ViewPlaneHandler vph = (Application.OpenForms[0] as Form1).vph;

                VisualSettings Settings = (Application.OpenForms[0] as Form1).Settings;
                List<object> SelectedDots = (Application.OpenForms[0] as Form1).SelectedDots;

                if ((ObjectID == 0x160 || ObjectID == 0x1AC) && SelectedDots.Contains(this))
                {
                    Gl.glColor4f(Settings.ObjectsColor.R / 255f, Settings.ObjectsColor.G / 255f, Settings.ObjectsColor.B / 255f, 0.25f);
                    Gl.glPushMatrix();
                    Gl.glTranslatef(PositionX, PositionZ, 0);
                    if (ObjectID == 0x160) Gl.glRotatef(-RotationY, 0, 0, 1);
                    Gl.glBegin(Gl.GL_QUADS);
                    Gl.glVertex2f(-ScaleX / 2, -ScaleZ / 2);
                    Gl.glVertex2f(ScaleX / 2, -ScaleZ / 2);
                    Gl.glVertex2f(ScaleX / 2, ScaleZ / 2);
                    Gl.glVertex2f(-ScaleX / 2, ScaleZ / 2);
                    Gl.glEnd();
                    Gl.glPopMatrix();
                }

                Gl.glPointSize(Settings.PointSize + 2f);
                Gl.glBegin(Gl.GL_POINTS);
                if (SelectedDots.Contains(this)) Gl.glColor4f(Settings.HighlightPointborderColor.R / 255f, Settings.HighlightPointborderColor.G / 255f, Settings.HighlightPointborderColor.B / 255f, Settings.HighlightPointborderColor.A);
                else Gl.glColor4f(Settings.PointborderColor.R / 255f, Settings.PointborderColor.G / 255f, Settings.PointborderColor.B / 255f, Settings.PointborderColor.A);
                vph.draw2DVertice(Pos);
                Gl.glEnd();

                Gl.glPointSize(Settings.PointSize);
                Gl.glBegin(Gl.GL_POINTS);
                if (SelectedDots.Contains(this)) Gl.glColor4f(Settings.HighlightPointColor.R / 255f, Settings.HighlightPointColor.G / 255f, Settings.HighlightPointColor.B / 255f, Settings.HighlightPointColor.A);
                else Gl.glColor4f(Settings.ObjectsColor.R / 255f, Settings.ObjectsColor.G / 255f, Settings.ObjectsColor.B / 255f, Settings.ObjectsColor.A);
                vph.draw2DVertice(Pos);
                Gl.glEnd();
            }
        }

        public GOBJ ToGOBJ()
        {
            GOBJ data = new GOBJ();
            data.NrEntries = (uint)Entries.Count;
            foreach (var entry in Entries)
                data.Entries.Add(entry.ToGOBJEntry());
            return data;
        }

        public TreeNode GetTreeNode()
        {
            TreeNode node = new TreeNode("Objects");
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
                entry.RenderPoint(picking);
        }

        public void Transform(Vector3 translation, Vector3 scale)
        {
            foreach (var entry in Entries)
            {
                entry.Pos *= scale;
                entry.Pos += translation;
            }
        }
    }
}
