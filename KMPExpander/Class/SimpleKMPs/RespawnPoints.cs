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
    public class RespawnPoints : SectionBase<RespawnPoints.RespawnEntry>
    {
        //[XmlElement("Entry")]
        //public List<RespawnEntry> Entries = new List<RespawnEntry>();
        public RespawnPoints() { }

        public RespawnPoints(JGPT data)
        {
            foreach (var entry in data.Entries)
                Entries.Add(new RespawnEntry(entry));
        }

        public class RespawnEntry
        {
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
            [XmlAttribute, TypeConverter(typeof(HexTypeConverter))]
            public UInt16 Unknown { get; set; }

            public RespawnEntry(JGPT.JGPTEntry entry)
            {
                PositionX = entry.Position.X;
                PositionY = entry.Position.Y;
                PositionZ = entry.Position.Z;
                RotationX = entry.Rotation.X;
                RotationY = entry.Rotation.Y;
                RotationZ = entry.Rotation.Z;
                Unknown = entry.Unknown;
            }

            public RespawnEntry() { }

            public JGPT.JGPTEntry ToJGPTEntry(ushort i)
            {
                JGPT.JGPTEntry entry = new JGPT.JGPTEntry();
                entry.Index = i;
                entry.Position = new Vector3(PositionX,PositionY,PositionZ);
                entry.Rotation = new Vector3(RotationX,RotationY, RotationZ);
                entry.Unknown = Unknown;
                return entry;
            }

            public void RenderPicking(int entry_id)
            {
                ViewPlaneHandler vph = (Application.OpenForms[0] as Form1).vph;

                VisualSettings Settings = (Application.OpenForms[0] as Form1).Settings;
                Color pickingColor = SectionPicking.GetColor(Sections.RespawnPoints, 0, entry_id);

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

                Gl.glPointSize(Settings.PointSize + 2f);
                Gl.glBegin(Gl.GL_POINTS);
                if (SelectedDots.Contains(this)) Gl.glColor4f(Settings.HighlightPointborderColor.R / 255f, Settings.HighlightPointborderColor.G / 255f, Settings.HighlightPointborderColor.B / 255f, Settings.HighlightPointborderColor.A);
                else Gl.glColor4f(Settings.PointborderColor.R / 255f, Settings.PointborderColor.G / 255f, Settings.PointborderColor.B / 255f, Settings.PointborderColor.A);
                vph.draw2DVertice(Pos);
                Gl.glEnd();
                if (vph.mode == ViewPlaneHandler.PLANE_MODES.XZ)
                {
                    Gl.glPushMatrix();
                    Gl.glTranslatef(PositionX, PositionZ, 0);
                    Gl.glRotatef(-RotationY, 0, 0, 1);

                    Gl.glBegin(Gl.GL_LINES);
                    Gl.glVertex2f(0, 0);
                    Gl.glVertex2f(0, 150);
                    Gl.glEnd();
                    Gl.glPopMatrix();
                }
                Gl.glPointSize(Settings.PointSize);
                Gl.glBegin(Gl.GL_POINTS);
                if (SelectedDots.Contains(this)) Gl.glColor4f(Settings.HighlightPointColor.R / 255f, Settings.HighlightPointColor.G / 255f, Settings.HighlightPointColor.B / 255f, Settings.HighlightPointColor.A);
                else Gl.glColor4f(Settings.JugemColor.R / 255f, Settings.JugemColor.G / 255f, Settings.JugemColor.B / 255f, Settings.JugemColor.A);
                vph.draw2DVertice(Pos);
                Gl.glEnd();
            }

        }

        public JGPT ToJGPT()
        {
            JGPT data = new JGPT();
            data.NrEntries = (uint)Entries.Count;
            ushort i = 0;
            foreach (var entry in Entries)
            {
                data.Entries.Add(entry.ToJGPTEntry(i));
                i++;
            }

            return data;
        }

        public TreeNode GetTreeNode()
        {
            TreeNode node = new TreeNode("Respawn Points");
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
    }
}
