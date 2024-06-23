﻿using System;
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
using System.Drawing.Design;
using Tao.OpenGl;
using System.Drawing;

namespace KMPExpander.Class.SimpleKMPs
{
    public class CheckPoints : SectionBase<CheckPoints.CheckpointGroup>
    {
        //[XmlElement("Group")]
        //public List<CheckpointGroup> Groups = new List<CheckpointGroup>();
        public CheckPoints() { }
        public CheckPoints(CKPT data, CKPH paths)
        {
            foreach (var path in paths.Entries)
            {
                Entries.Add(new CheckpointGroup(path, data));
            }
        }

        public class CheckpointGroup : SectionBase<CheckpointGroup.CheckpointEntry>
        {
            [XmlAttribute, Category("Checkpoint Group"), Description("Specifies which previous groups this one is linked to."), Editor(typeof(CustomEditor), typeof(UITypeEditor))]
            public sbyte[] Previous { get; set; }
            [XmlAttribute, Category("Checkpoint Group"), Description("Specifies which next groups this one is linked to."), Editor(typeof(CustomEditor), typeof(UITypeEditor))]
            public sbyte[] Next { get; set; }
            [XmlAttribute, Category("Checkpoint Group"), TypeConverter(typeof(HexTypeConverter))]
            public UInt16 Unknown { get; set; }
            //[XmlElement("Entry")]
            //public List<CheckpointEntry> Entries = new List<CheckpointEntry>();

            public class CheckpointEntry
            {
                //public Vector2 LeftPoint { get; set; }
                [XmlAttribute]
                public Single LeftPointX { get; set; }
                [XmlAttribute]
                public Single LeftPointZ { get; set; }
                //public Vector2 RightPoint { get; set; }
                [XmlAttribute]
                public Single RightPointX { get; set; }
                [XmlAttribute]
                public Single RightPointZ { get; set; }
                [XmlAttribute("Respawn")]
                public Byte RespawnId { get; set; }
                [XmlAttribute]
                public SByte Key { get; set; }
                [XmlAttribute]
                public SByte ClipID { get; set; }
                [XmlAttribute]
                public SByte Section { get; set; }
                [XmlAttribute]
                public Byte Unknown3 { get; set; }
                [XmlAttribute]
                public Byte Unknown4 { get; set; }

                public CheckpointEntry() {
                    Key = -1;
                    ClipID = -1;
                    Section = -1;
                }
                public CheckpointEntry(CKPT.CKPTEntry entry)
                {
                    LeftPointX = entry.Point1.X;
                    RightPointX = entry.Point2.X;
                    LeftPointZ = entry.Point1.Z;
                    RightPointZ = entry.Point2.Z;
                    RespawnId = entry.RespawnId;
                    Key = entry.Key;
                    ClipID = entry.ClipID;
                    Section = entry.Section;
                    Unknown3 = entry.Unknown3;
                    Unknown4 = entry.Unknown4;
                }
                public CKPT.CKPTEntry ToCKPTEntry(Byte prev,Byte next)
                {
                    CKPT.CKPTEntry entry = new CKPT.CKPTEntry();
                    entry.Point1 = new Vector2(LeftPointX, LeftPointZ);
                    entry.Point2 = new Vector2(RightPointX, RightPointZ);
                    entry.RespawnId = RespawnId;
                    entry.Key = Key;
                    entry.Previous = prev;
                    entry.Next = next;
                    entry.ClipID = ClipID;
                    entry.Section = Section;
                    entry.Unknown3 = Unknown3;
                    entry.Unknown4 = Unknown4;
                    return entry;
                }
                public void judgeColor(VisualSettings Settings, bool isLeft)
                {
                    if (isLeft)
                    {
                        if (Section != -1 || Key == 0) Gl.glColor4f(Settings.CkptSectionLeftColor.R / 255f, Settings.CkptSectionLeftColor.G / 255f, Settings.CkptSectionLeftColor.B / 255f, Settings.CkptSectionLeftColor.A);
                        else if (Key != -1) Gl.glColor4f(Settings.CkptKeyLeftColor.R / 255f, Settings.CkptKeyLeftColor.G / 255f, Settings.CkptKeyLeftColor.B / 255f, Settings.CkptKeyLeftColor.A);
                        else Gl.glColor4f(Settings.CkptLeftColor.R / 255f, Settings.CkptLeftColor.G / 255f, Settings.CkptLeftColor.B / 255f, Settings.CkptLeftColor.A);
                    }
                    else
                    {
                        if (Section != -1 || Key == 0) Gl.glColor4f(Settings.CkptSectionRightColor.R / 255f, Settings.CkptSectionRightColor.G / 255f, Settings.CkptSectionRightColor.B / 255f, Settings.CkptSectionRightColor.A);
                        else if (Key != -1) Gl.glColor4f(Settings.CkptKeyRightColor.R / 255f, Settings.CkptKeyRightColor.G / 255f, Settings.CkptKeyRightColor.B / 255f, Settings.CkptKeyRightColor.A);
                        else Gl.glColor4f(Settings.CkptRightColor.R / 255f, Settings.CkptRightColor.G / 255f, Settings.CkptRightColor.B / 255f, Settings.CkptRightColor.A);
                    }
                }
                public void RenderPicking(int group_id,int entry_id)
                {
                    VisualSettings Settings = (Application.OpenForms[0] as Form1).Settings;

                    Color pickingColorL = SectionPicking.GetColor(Sections.CheckPoints, group_id, entry_id, PointID.Left);
                    Color pickingColorR = SectionPicking.GetColor(Sections.CheckPoints, group_id, entry_id, PointID.Right);

                    

                    Gl.glPointSize(Settings.PointSize + 2f);
                    Gl.glBegin(Gl.GL_POINTS);
                    Gl.glColor4f(pickingColorR.R / 255f, pickingColorR.G / 255f, pickingColorR.B / 255f, 1f);
                    Gl.glVertex2f(RightPointX, RightPointZ);
                    Gl.glEnd();

                    Gl.glPointSize(Settings.PointSize + 2f);
                    Gl.glBegin(Gl.GL_POINTS);
                    Gl.glColor4f(pickingColorL.R / 255f, pickingColorL.G / 255f, pickingColorL.B / 255f, 1f);
                    Gl.glVertex2f(LeftPointX, LeftPointZ);
                    Gl.glEnd();
                }

                public void RenderSegment()
                {
                    VisualSettings Settings = (Application.OpenForms[0] as Form1).Settings;
                    List<object> SelectedDots = (Application.OpenForms[0] as Form1).SelectedDots;

                    Gl.glBegin(Gl.GL_LINES);
                    judgeColor(Settings, false);
                    Gl.glVertex2f(RightPointX, RightPointZ);
                    judgeColor(Settings, true);
                    Gl.glVertex2f(LeftPointX, LeftPointZ);
                    Gl.glEnd();

                    Gl.glPointSize(Settings.PointSize + 2f);
                    Gl.glBegin(Gl.GL_POINTS);
                    if (SelectedDots.Contains(this)) Gl.glColor4f(Settings.HighlightPointborderColor.R / 255f, Settings.HighlightPointborderColor.G / 255f, Settings.HighlightPointborderColor.B / 255f, Settings.HighlightPointborderColor.A);
                    else Gl.glColor4f(Settings.PointborderColor.R / 255f, Settings.PointborderColor.G / 255f, Settings.PointborderColor.B / 255f, Settings.PointborderColor.A);
                    Gl.glVertex2f(RightPointX, RightPointZ);
                    Gl.glEnd();

                    Gl.glPointSize(Settings.PointSize);
                    Gl.glBegin(Gl.GL_POINTS);
                    if (SelectedDots.Contains(this)) Gl.glColor4f(Settings.HighlightPointColor.R / 255f, Settings.HighlightPointColor.G / 255f, Settings.HighlightPointColor.B / 255f, Settings.HighlightPointColor.A);
                    else
                    {
                        judgeColor(Settings, false);
                    }
                    Gl.glVertex2f(RightPointX, RightPointZ);
                    Gl.glEnd();

                    Gl.glPointSize(Settings.PointSize + 2f);
                    Gl.glBegin(Gl.GL_POINTS);
                    if (SelectedDots.Contains(this)) Gl.glColor4f(Settings.HighlightPointborderColor.R / 255f, Settings.HighlightPointborderColor.G / 255f, Settings.HighlightPointborderColor.B / 255f, Settings.HighlightPointborderColor.A);
                    else Gl.glColor4f(Settings.PointborderColor.R / 255f, Settings.PointborderColor.G / 255f, Settings.PointborderColor.B / 255f, Settings.PointborderColor.A);
                    Gl.glVertex2f(LeftPointX, LeftPointZ);
                    Gl.glEnd();

                    Gl.glPointSize(Settings.PointSize);
                    Gl.glBegin(Gl.GL_POINTS);
                    if (SelectedDots.Contains(this)) Gl.glColor4f(Settings.HighlightPointColor.R / 255f, Settings.HighlightPointColor.G / 255f, Settings.HighlightPointColor.B / 255f, Settings.HighlightPointColor.A);
                    else
                    {
                        judgeColor(Settings, true);
                    }
                    Gl.glVertex2f(LeftPointX, LeftPointZ);
                    Gl.glEnd();
                }

                public void RenderRightLine(VisualSettings Settings)
                {
                    judgeColor(Settings, false);
                    Gl.glVertex2f(RightPointX, RightPointZ);
                }

                public void RenderLeftLine(VisualSettings Settings)
                {
                    judgeColor(Settings, true);
                    Gl.glVertex2f(LeftPointX, LeftPointZ);
                }
            }

            public CheckpointGroup()
            {
                Next = new sbyte[6];
                Previous = new sbyte[6];
                for (int i = 0; i < Next.Length; i++)
                {
                    Next[i] = -1;
                    Previous[i] = -1;
                }
            }
            public CheckpointGroup(CKPH.CKPHEntry path, CKPT data)
            {
                Previous = path.Previous;
                Next = path.Next;
                Unknown = path.Unknown;
                for (int i = path.Start; i < path.Start + path.Length; i++)
                {
                    Entries.Add(new CheckpointEntry(data.Entries[i]));
                }
            }
            public CKPH.CKPHEntry ToCKPHEntry(byte start)
            {
                CKPH.CKPHEntry path = new CKPH.CKPHEntry();
                path.Start = start;
                path.Length = (byte)Entries.Count;
                path.Next = Next;
                path.Previous = Previous;
                path.Unknown = Unknown;
                return path;
            }

            public void RenderPicking(int group_id)
            {
                if (!Visible) return;

                for (int i = 0; i < Entries.Count; i++)
                    Entries[i].RenderPicking(group_id,i);
            }

            private static bool IsBigger180(Vector2 vertex, Vector2 p1, Vector2 p2)
            {
                Vector2 V1 = p1 - vertex;
                Vector2 V2 = p2 - vertex;
                return V2.YCross(V1) <= 0f;
            }

            public static bool IsConvex(CheckpointEntry prev, CheckpointEntry next)
            {
                Vector2 prevLeft = new Vector2(prev.LeftPointX, prev.LeftPointZ);
                Vector2 prevRight = new Vector2(prev.RightPointX, prev.RightPointZ);
                Vector2 nextLeft = new Vector2(next.LeftPointX, next.LeftPointZ);
                Vector2 nextRight = new Vector2(next.RightPointX, next.RightPointZ);
                return IsBigger180(prevLeft, nextLeft, prevRight) &&
                    IsBigger180(prevRight, prevLeft, nextRight) &&
                    IsBigger180(nextRight, prevRight, nextLeft) &&
                    IsBigger180(nextLeft, nextRight, prevLeft);
            }

            public void Render()
            {
                if (!Visible) return;

                VisualSettings Settings = (Application.OpenForms[0] as Form1).Settings;

                if (Settings.LinkPoints)
                {
                    
                    for (int i = 0; i < Entries.Count - 1; i++)
                    {
                        Gl.glBegin(Gl.GL_LINES);
                        Entries[i].RenderLeftLine(Settings);
                        Entries[i + 1].RenderLeftLine(Settings);
                        Gl.glEnd();

                        Gl.glBegin(Gl.GL_LINES);
                        Entries[i].RenderRightLine(Settings);
                        Entries[i + 1].RenderRightLine(Settings);
                        Gl.glEnd();

                        if (!IsConvex(Entries[i], Entries[i+1]))
                        {
                            Gl.glBegin(Gl.GL_TRIANGLE_FAN);
                            Gl.glColor4f(1f, 0f, 0f, 0.75f);
                            Gl.glVertex2f(Entries[i].LeftPointX, Entries[i].LeftPointZ);
                            Gl.glVertex2f(Entries[i].RightPointX, Entries[i].RightPointZ);
                            Gl.glVertex2f(Entries[i+1].RightPointX, Entries[i+1].RightPointZ);
                            Gl.glVertex2f(Entries[i+1].LeftPointX, Entries[i+1].LeftPointZ);
                            Gl.glEnd();
                        }
                    }
                }
                foreach (var entry in Entries)
                    entry.RenderSegment();
            }
        }
        public CKPT ToCKPT()
        {
            CKPT data = new CKPT();
            uint Count = 0;
            uint Index;
            Byte Prev;
            Byte Next;
            foreach (var group in Entries)
            {
                Index = 0;
                foreach (var entry in group.Entries)
                {
                    if (Index + 1 == group.Entries.Count)
                        Next = 255;
                    else Next = (byte)(Count+1);

                    if (Index == 0)
                        Prev = 255;
                    else Prev = (byte)(Count-1);

                    data.Entries.Add(entry.ToCKPTEntry(Prev,Next));
                    Count++;
                    Index++;
                }
            }
                
            data.NrEntries = Count;
            return data;
        }

        public CKPH ToCKPH()
        {
            CKPH path = new CKPH();
            path.NrEntries = (uint)Entries.Count;
            byte start = 0;
            foreach (var group in Entries)
            {
                path.Entries.Add(group.ToCKPHEntry(start));
                start += (byte)group.Entries.Count;
            }
            return path;
        }

        public TreeNode GetTreeNode()
        {
            TreeNode node = new TreeNode("Checkpoints");

            int i = 0;
            foreach (var group in Entries)
            {
                node.Nodes.Add("Group " + i.ToString());
                node.Nodes[i].Tag = group;
                node.Nodes[i].ImageIndex = 15;
                node.Nodes[i].SelectedImageIndex = 15;
                i++;
            }
            return node;
        }

        public void Render(bool picking)
        {
            if (!Visible || (Application.OpenForms[0] as Form1).vph.mode != ViewPlaneHandler.PLANE_MODES.XZ) return;

            if (picking)
            {
                for (int i = 0; i < Entries.Count; i++)
                    Entries[i].RenderPicking(i);
                return;
            }

            VisualSettings Settings = (Application.OpenForms[0] as Form1).Settings;
            Gl.glLineWidth(Settings.LineWidth);
            foreach (var group in Entries)
            {
                if (Settings.LinkPoints && Settings.LinkPaths && group.Visible)
                {
                    Gl.glColor4f(Settings.RouteLinkColor.R / 255f, Settings.RouteLinkColor.G / 255f, Settings.RouteLinkColor.B / 255f, Settings.RouteLinkColor.A);
                    try
                    {
                        foreach (var next in group.Next)
                            if (next != -1 && Entries[next].Visible)
                            {
                                CheckpointGroup.CheckpointEntry prevC = group.Entries[group.Entries.Count - 1];
                                CheckpointGroup.CheckpointEntry nextC = Entries[next].Entries[0];

                                Gl.glBegin(Gl.GL_LINES);
                                prevC.RenderLeftLine(Settings);
                                nextC.RenderLeftLine(Settings);
                                Gl.glEnd();

                                Gl.glBegin(Gl.GL_LINES);
                                prevC.RenderRightLine(Settings);
                                nextC.RenderRightLine(Settings);
                                Gl.glEnd();

                                if (!CheckpointGroup.IsConvex(prevC, nextC))
                                {
                                    Gl.glBegin(Gl.GL_TRIANGLE_FAN);
                                    Gl.glColor4f(1f, 0f, 0f, 0.75f);
                                    Gl.glVertex2f(prevC.LeftPointX, prevC.LeftPointZ);
                                    Gl.glVertex2f(prevC.RightPointX, prevC.RightPointZ);
                                    Gl.glVertex2f(nextC.RightPointX, nextC.RightPointZ);
                                    Gl.glVertex2f(nextC.LeftPointX, nextC.LeftPointZ);
                                    Gl.glEnd();
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
                    entry.LeftPointX *= scale.X;
                    entry.LeftPointZ *= scale.Z;
                    entry.LeftPointX += translation.X;
                    entry.LeftPointZ += translation.Z;

                    entry.RightPointX *= scale.X;
                    entry.RightPointZ *= scale.Z;
                    entry.RightPointX += translation.X;
                    entry.RightPointZ += translation.Z;
                }
            }
        }
    }
}
