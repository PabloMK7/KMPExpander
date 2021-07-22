using LibCTR.Collections;
using LibEndianBinaryIO;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Tao.OpenGl;

namespace KMPExpander.Class
{
    public class Map
    {
        public float BottomLeftX, BottomLeftZ, TopRightX, TopRightZ;
        public bool Visible = true;
        Bitmap Image;

        public Map()
        {
            BottomLeftX = -1000;
            BottomLeftZ = -1000;
            TopRightX = 1000;
            TopRightZ = 1000;
        }

        public Map(EndianBinaryReaderEx er)
        {
            BottomLeftX = er.ReadSingle();
            BottomLeftZ = er.ReadSingle();
            TopRightX = er.ReadSingle();
            TopRightZ = er.ReadSingle();
        }

        public void Write(EndianBinaryWriter er)
        {
            er.Write(BottomLeftX);
            er.Write(BottomLeftZ);
            er.Write(TopRightX);
            er.Write(TopRightZ);
        }

        public void LoadImage(Bitmap Image, bool isLocalMap)
        {
            UnloadImage();
            this.Image = Image;
            UploadTex(Image, isLocalMap ? 1 : 2);
        }

        public void UnloadImage()
        {
            if (Image != null)
            {
                Image.Dispose();
                Image = null;
            }
        }

        public void Render(bool isLocalMap)
        {
            if (!Visible) return;
            VisualSettings Settings = (Application.OpenForms[0] as Form1).Settings;
            Color renderColor;

            if (isLocalMap)
                renderColor = Color.FromArgb(0, 0, 0);
            else
                renderColor = Color.FromArgb(128, 128, 128);

            Gl.glBindTexture(Gl.GL_TEXTURE_2D, 0);
            Gl.glColor4f(renderColor.R / 255f, renderColor.G / 255f, renderColor.B / 255f, 1f);
            Gl.glBegin(Gl.GL_POINTS);
            Gl.glVertex2f(BottomLeftX, BottomLeftZ);
            Gl.glEnd();
            Gl.glBegin(Gl.GL_POINTS);
            Gl.glVertex2f(TopRightX, TopRightZ);
            Gl.glEnd();

            Gl.glLineWidth(Settings.LineWidth);
            Gl.glColor4f(renderColor.R / 255f, renderColor.G / 255f, renderColor.B / 255f, 1f);
            Gl.glBegin(Gl.GL_LINES);

            Gl.glVertex2f(BottomLeftX, BottomLeftZ);
            Gl.glVertex2f(TopRightX, BottomLeftZ);
            Gl.glVertex2f(TopRightX, BottomLeftZ);
            Gl.glVertex2f(TopRightX, TopRightZ);
            Gl.glVertex2f(TopRightX, TopRightZ);
            Gl.glVertex2f(BottomLeftX, TopRightZ);
            Gl.glVertex2f(BottomLeftX, TopRightZ);
            Gl.glVertex2f(BottomLeftX, BottomLeftZ);

            Gl.glEnd();

            if (Image != null)
            {
                float botCoord = isLocalMap ? 0 : 1f - (1 / 1.065f);
                float rightCoor = isLocalMap ? 1 : 1 / 1.295f;

                Gl.glBindTexture(Gl.GL_TEXTURE_2D, isLocalMap ? 1 : 2);
                Gl.glColor3f(1f, 1f, 1f);
                Gl.glBegin(Gl.GL_QUADS);
                Gl.glTexCoord2f(0, botCoord);
                Gl.glVertex2f(BottomLeftX, BottomLeftZ);
                Gl.glTexCoord2f(rightCoor, botCoord);
                Gl.glVertex2f(TopRightX, BottomLeftZ);
                Gl.glTexCoord2f(rightCoor, 1);
                Gl.glVertex2f(TopRightX, TopRightZ);
                Gl.glTexCoord2f(0, 1);
                Gl.glVertex2f(BottomLeftX, TopRightZ);
                Gl.glEnd();
            }
        }

        private void UploadTex(Bitmap b, int Id)
        {
            Gl.glBindTexture(Gl.GL_TEXTURE_2D, Id);
            Gl.glColor3f(1, 1, 1);
            BitmapData d = b.LockBits(new Rectangle(0, 0, b.Width, b.Height), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
            Gl.glTexImage2D(Gl.GL_TEXTURE_2D, 0, Gl.GL_RGBA8, b.Width, b.Height, 0, Gl.GL_BGRA, Gl.GL_UNSIGNED_BYTE, d.Scan0);
            b.UnlockBits(d);
            Gl.glTexParameteri(Gl.GL_TEXTURE_2D, Gl.GL_TEXTURE_MIN_FILTER, Gl.GL_NEAREST);
            Gl.glTexParameteri(Gl.GL_TEXTURE_2D, Gl.GL_TEXTURE_MAG_FILTER, Gl.GL_NEAREST);
            bool repeatS = true;
            bool repeatT = true;
            bool flipS = false;
            bool flipT = false;
            int S;
            if (repeatS && flipS)
            {
                S = Gl.GL_MIRRORED_REPEAT;
            }
            else if (repeatS)
            {
                S = Gl.GL_REPEAT;
            }
            else
            {
                S = Gl.GL_CLAMP;
            }
            int T;
            if (repeatT && flipT)
            {
                T = Gl.GL_MIRRORED_REPEAT;
            }
            else if (repeatT)
            {
                T = Gl.GL_REPEAT;
            }
            else
            {
                T = Gl.GL_CLAMP;
            }
            Gl.glTexParameterf(Gl.GL_TEXTURE_2D, Gl.GL_TEXTURE_WRAP_S, S);
            Gl.glTexParameterf(Gl.GL_TEXTURE_2D, Gl.GL_TEXTURE_WRAP_T, T);
        }

        public void RenderPicking(bool isLocalMap)
        {
            if (!Visible) return;

            VisualSettings Settings = (Application.OpenForms[0] as Form1).Settings;

            Gl.glPointSize(Settings.PointSize + 2f);

            Color pickingColor = SectionPicking.GetColor(isLocalMap ? Sections.LocalMap : Sections.GlobalMap, 0, 0, PointID.Left);
            Gl.glColor4f(pickingColor.R / 255f, pickingColor.G / 255f, pickingColor.B / 255f, 1f);
            Gl.glBegin(Gl.GL_POINTS);
            Gl.glVertex2f(BottomLeftX, BottomLeftZ);
            Gl.glEnd();

            pickingColor = SectionPicking.GetColor(isLocalMap ? Sections.LocalMap : Sections.GlobalMap, 0, 0, PointID.Right);
            Gl.glColor4f(pickingColor.R / 255f, pickingColor.G / 255f, pickingColor.B / 255f, 1f);
            Gl.glBegin(Gl.GL_POINTS);
            Gl.glVertex2f(TopRightX, TopRightZ);
            Gl.glEnd();
        }
    }

    public class UIMapPos
    {
        public Map GlobalMap;
        public Map LocalMap;
        public Byte Unknown;

        public UIMapPos(byte[] Data)
        {
            EndianBinaryReaderEx er = new EndianBinaryReaderEx(new MemoryStream(Data), Endianness.LittleEndian);
            try
            {
                GlobalMap = new Map(er);
                LocalMap = new Map(er);
                Unknown = er.ReadByte();
            }
            finally
            {
                er.Close();
            }
        }

        public byte[] Write()
        {
            MemoryStream m = new MemoryStream();
            EndianBinaryWriter er = new EndianBinaryWriter(m, Endianness.LittleEndian);
            GlobalMap.Write(er);
            LocalMap.Write(er);
            er.Write(Unknown);
            byte[] result = m.ToArray();
            er.Close();
            return result;
        }

        public void Render(bool picking = false)
        {
            if ((Application.OpenForms[0] as Form1).vph.mode != Extensions.ViewPlaneHandler.PLANE_MODES.XZ) return;
            Gl.glBindTexture(Gl.GL_TEXTURE_2D, 0);
            if (!picking)
            {
                Gl.glDepthFunc(Gl.GL_ALWAYS);
                Gl.glLoadIdentity();
                LocalMap.Render(true);
                GlobalMap.Render(false);
            }
            else
            {
                LocalMap.RenderPicking(true);
                GlobalMap.RenderPicking(false);
            }
                
        }

        public void MovePoint(SectionPicking.PickingInfo pick_info,Vector3 position)
        {
            switch (pick_info.Section)
            {
                case Sections.LocalMap:
                    if (pick_info.PointID==PointID.Left)
                    {
                        LocalMap.BottomLeftX = position.X;
                        LocalMap.BottomLeftZ = position.Z;
                    }
                    else
                    {
                        LocalMap.TopRightX = position.X;
                        LocalMap.TopRightZ = position.Z;
                    }
                    break;
                case Sections.GlobalMap:
                    if (pick_info.PointID == PointID.Left)
                    {
                        GlobalMap.BottomLeftX = position.X;
                        GlobalMap.BottomLeftZ = position.Z;
                    }
                    else
                    {
                        GlobalMap.TopRightX = position.X;
                        GlobalMap.TopRightZ = position.Z;
                    }
                    break;
            }
        }
    }
}
