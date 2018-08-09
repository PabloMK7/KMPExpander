using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommonFiles;
using System.IO;
using Tao.OpenGl;
using LibCTR.Collections;
using System.ComponentModel;
using System.Windows.Forms;
using System.Drawing.Imaging;

namespace KMPExpander.Class
{
    public class OBJWrapper
    {
        public bool Visible = true;
        public bool Wireframe = false;
        public class MaterialInfo
        {
            [Browsable(false)]
            public string Name = "dummy";
            [Description("Decide whether to render this material's texture or not."), Category("Render Settings")]
            public bool UseTexture { get; set; }
            [Description("Decide whether to render the polygons with this material assigned or not."), Category("Render Settings")]
            public bool ShowMaterial { get; set; }
            [Browsable(false)]
            public int OpenGLTexID = 0;
            [Description("Set the Diffuse Color for this material."), Category("Color")]
            public Color DiffuseColor { get; set; }
            [Description("Set the Ambient Color for this material."), Category("Color")]
            public Color AmbientColor { get; set; }
            [Description("Set the Opacity for this material"),Category("Color")]
            public float Opacity { get; set; }
            public Bitmap Texture = null;
            private bool repeatS = true, repeatT = true, flipS = false, flipT = false;

            //List of Default materials

            public struct MaterialColor
            {
                public String Name;
                public Color Color;

                public MaterialColor(string name,Color color)
                {
                    Name = name;
                    Color = color;
                }

            }
            private static MaterialColor[] DefaultColors = new MaterialColor[]
            {
                new MaterialColor("DEFAULT",Color.FromArgb(0x77,0x77,0x77,0x77)),
                new MaterialColor("WHITE",Color.White),

                new MaterialColor("OOB",Color.DarkGreen),
                new MaterialColor("SOLIDFALL",Color.DarkGreen),
                new MaterialColor("FALL",Color.FromArgb(10,0xFF,0xCC,0xFF)),
                new MaterialColor("WATER",Color.FromArgb(240,0,0x99,0xFF)),
                new MaterialColor("MIZU",Color.FromArgb(240,0,0x99,0xFF)),
                new MaterialColor("LAVA",Color.Red),
                new MaterialColor("YOGAN",Color.Red),

                new MaterialColor("SANDROAD",Color.FromArgb(255,0xCC,0xCC,0x99)),
                new MaterialColor("ROAD",Color.Gray),
                new MaterialColor("WOOD",Color.Brown),

                new MaterialColor("GRASS",Color.FromArgb(255,0x33,0xCC,0x33)),
                new MaterialColor("KUSA",Color.FromArgb(255,0x33,0xCC,0x33)),
                new MaterialColor("OFFROAD",Color.DarkGreen),
                new MaterialColor("SAND",Color.FromArgb(255,0xFF,0xFF,0x99)),
                new MaterialColor("SUNA",Color.FromArgb(255,0xFF,0xFF,0x99)),
                new MaterialColor("MUD",Color.FromArgb(255,0x66,0x33,0x33)),
                new MaterialColor("SWAMP",Color.FromArgb(255,0x66,0x33,0x33)),
                new MaterialColor("DORO",Color.FromArgb(255,0x66,0x33,0x33)),
                new MaterialColor("SNOW",Color.FromArgb(255,0x99,0xCC,0xFF)),
                new MaterialColor("HALATION",Color.FromArgb(255,0x99,0xCC,0xFF)),
                new MaterialColor("YUKI",Color.FromArgb(255,0x99,0xCC,0xFF)),
                new MaterialColor("ICE",Color.FromArgb(255,0x99,0xCC,0xFF)),
                new MaterialColor("GLASS",Color.FromArgb(255,0x99,0xCC,0xFF)),

                new MaterialColor("CLIFF",Color.FromArgb(255,0x99,0x66,0x33)),
                new MaterialColor("WALL",Color.FromArgb(255,0x99,0x66,0x33)),
                new MaterialColor("ROCK",Color.FromArgb(255,0x99,0x66,0x33)),
                new MaterialColor("IWA",Color.FromArgb(255,0x99,0x66,0x33)),
                new MaterialColor("YAMA",Color.FromArgb(255,0x99,0x66,0x33)),
                new MaterialColor("MOUNTAIN",Color.FromArgb(255,0x99,0x66,0x33)),
                new MaterialColor("KABE",Color.FromArgb(255,0x99,0x66,0x33)),
                new MaterialColor("FENCE",Color.FromArgb(255,0x99,0x66,0x33)),
                new MaterialColor("SAKU",Color.FromArgb(255,0x99,0x66,0x33)),

                new MaterialColor("BOOST",Color.Red),
                new MaterialColor("DASH",Color.Red),
                new MaterialColor("SPEED",Color.Red),
                new MaterialColor("GLIDE",Color.Azure),
                new MaterialColor("RAINBOW",Color.Red),
            };


            private void DetectColor()
            {
                AmbientColor = Color.Black;
                DiffuseColor = Color.Black;
                Opacity = 0.5f;
                foreach (var matColor in DefaultColors)
                {
                    if (Name.ToUpper().Contains(matColor.Name))
                    {
                        AmbientColor = matColor.Color;
                        Opacity = AmbientColor.A / 255f;
                        break;
                    }
                }
                
            }

            public MaterialInfo(string name,bool auto_color=false)
            {
                Name = name;
                UseTexture = false;
                ShowMaterial = true;
                Opacity = 1;
                if (auto_color) DetectColor();
            }

            [Category("UV Settings"), Description("Repeat the texture in the U direction.")]
            public bool RepeatU
            {
                get { return repeatS; }
                set
                {
                    repeatS = value;
                    UpdateTex();
                }
            }
            [Category("UV Settings"), Description("Repeat the texture in the V direction.")]
            public bool RepeatV
            {
                get { return repeatT; }
                set
                {
                    repeatT = value;
                    UpdateTex();
                }
            }
            [Category("UV Settings"), Description("Mirror the texture in the U direction.")]
            public bool MirrorU
            {
                get { return flipS; }
                set
                {
                    flipS = value;
                    UpdateTex();
                }
            }
            [Category("UV Settings"), Description("Mirror the texture in the V direction.")]
            public bool MirrorV
            {
                get { return flipT; }
                set
                {
                    flipT = value;
                    UpdateTex();
                }
            }

            public void UpdateTex()
            {
                if (Texture == null) return;
                Texture.RotateFlip(RotateFlipType.RotateNoneFlipY);
                UploadTex(Texture, OpenGLTexID, repeatS, repeatT, flipS, flipT);
                Texture.RotateFlip(RotateFlipType.RotateNoneFlipY);
            }

            public override string ToString()
            {
                return Name;
            }
        }

        private string base_path;
        private OBJ obj_data;
        private MTL mtl_data = null;
        public List<MaterialInfo> Materials = new List<MaterialInfo>();

        public OBJWrapper(string path)
        {
            base_path = Path.GetDirectoryName(path);
            try
            {
                bool has_mtl = true;
                obj_data = new OBJ(File.ReadAllBytes(path));
                if (obj_data.MTLPath.Length > 0)
                    try
                    {
                        obj_data.MTLPath.Replace("/", "\\");
                        mtl_data = new MTL(File.ReadAllBytes(base_path + "\\" + obj_data.MTLPath));
                    }
                    catch
                    {
                        mtl_data = null;
                        has_mtl = false;
                    }
                InitializeMaterials(has_mtl);
            }
            catch (Exception e)
            {
                MessageBox.Show("OBJ Wrapper Exception: \n\r" + e.ToString());
            }
        }

        public List<string> GetMaterialNames()
        {
            List<string> names = new List<string>();
            foreach (var p in obj_data.Faces)
                if (!names.Contains(p.Material)) names.Add(p.Material);
            return names;
        }

        public void InitializeMaterials(bool has_mtl)
        {
            if (!has_mtl)
            {
                List<string> names = GetMaterialNames();
                foreach (var name in names)
                {
                    Materials.Add(new MaterialInfo(name,true));
                }
                return;
            }
            int i = 3; // ID 0 is Default, ID 1 is Local Map and ID 2 is Global Map
            foreach (var mat in mtl_data.Materials)
            {
                MaterialInfo temp = new MaterialInfo(mat.Name);
                try
                {
                    Bitmap bitmap = new Bitmap(new MemoryStream(File.ReadAllBytes(base_path + "\\" + mat.DiffuseMapPath)));
                    temp.Texture = bitmap;
                    temp.OpenGLTexID = i;
                    temp.UpdateTex();
                    temp.UseTexture = true;
                    i++;
                }
                catch
                {
                    
                }
                temp.DiffuseColor = mat.DiffuseColor;
                temp.AmbientColor = mat.AmbientColor;
                temp.Opacity = mat.Alpha;
                Materials.Add(temp);
            }
        }

        private static void UploadTex(Bitmap b, int Id, bool repeatS = true, bool repeatT = true, bool flipS = false, bool flipT = false)
        {
            Gl.glBindTexture(Gl.GL_TEXTURE_2D, Id);
            Gl.glColor3f(1, 1, 1);
            BitmapData d = b.LockBits(new Rectangle(0, 0, b.Width, b.Height), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
            Gl.glTexImage2D(Gl.GL_TEXTURE_2D, 0, Gl.GL_RGBA8, b.Width, b.Height, 0, Gl.GL_BGRA, Gl.GL_UNSIGNED_BYTE, d.Scan0);
            b.UnlockBits(d);
            Gl.glTexParameteri(Gl.GL_TEXTURE_2D, Gl.GL_TEXTURE_MIN_FILTER, Gl.GL_NEAREST);
            Gl.glTexParameteri(Gl.GL_TEXTURE_2D, Gl.GL_TEXTURE_MAG_FILTER, Gl.GL_NEAREST);
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

        public void Render(bool picking = false)
        {
            if (!Visible) return;

            Gl.glDepthFunc(Gl.GL_LESS);
            Gl.glEnable(Gl.GL_ALPHA_TEST);
            Gl.glAlphaFunc(Gl.GL_GREATER, 0f);
            Gl.glLoadIdentity();
            Gl.glBindTexture(Gl.GL_TEXTURE_2D, 0);
            if (Wireframe) Gl.glPolygonMode(Gl.GL_FRONT_AND_BACK, Gl.GL_LINE);
            if (picking)
            {
                int i = 0;
                foreach (var p in obj_data.Faces)
                {
                    if (GetMaterialByName(p.Material).ShowMaterial)
                    {
                        Color pickingColor = ModelPicking.GetColor(i);
                        Gl.glColor4f(pickingColor.R / 255f, pickingColor.G / 255f, pickingColor.B / 255f, pickingColor.A);
                        Gl.glBegin(Gl.GL_TRIANGLES);
                        Gl.glVertex3f(obj_data.Vertices[p.VertexIndieces[0]].X, obj_data.Vertices[p.VertexIndieces[0]].Z, obj_data.Vertices[p.VertexIndieces[0]].Y);
                        Gl.glVertex3f(obj_data.Vertices[p.VertexIndieces[1]].X, obj_data.Vertices[p.VertexIndieces[1]].Z, obj_data.Vertices[p.VertexIndieces[1]].Y);
                        Gl.glVertex3f(obj_data.Vertices[p.VertexIndieces[2]].X, obj_data.Vertices[p.VertexIndieces[2]].Z, obj_data.Vertices[p.VertexIndieces[2]].Y);
                        Gl.glEnd();
                    }
                    i++;
                }
                return;
            }
            
            foreach (var p in obj_data.Faces)
            {
                if (GetMaterialByName(p.Material).ShowMaterial)
                {
                    GlSetMaterial(GetMaterialByName(p.Material));
                    Gl.glBegin(Gl.GL_TRIANGLES);
                    if (p.TexCoordIndieces.Count > 0) Gl.glTexCoord2f(obj_data.TexCoords[p.TexCoordIndieces[0]].X, obj_data.TexCoords[p.TexCoordIndieces[0]].Z);
                    Gl.glVertex3f(obj_data.Vertices[p.VertexIndieces[0]].X, obj_data.Vertices[p.VertexIndieces[0]].Z, obj_data.Vertices[p.VertexIndieces[0]].Y);
                    if (p.TexCoordIndieces.Count > 0) Gl.glTexCoord2f(obj_data.TexCoords[p.TexCoordIndieces[1]].X, obj_data.TexCoords[p.TexCoordIndieces[1]].Z);
                    Gl.glVertex3f(obj_data.Vertices[p.VertexIndieces[1]].X, obj_data.Vertices[p.VertexIndieces[1]].Z, obj_data.Vertices[p.VertexIndieces[1]].Y);
                    if (p.TexCoordIndieces.Count > 0) Gl.glTexCoord2f(obj_data.TexCoords[p.TexCoordIndieces[2]].X, obj_data.TexCoords[p.TexCoordIndieces[2]].Z);
                    Gl.glVertex3f(obj_data.Vertices[p.VertexIndieces[2]].X, obj_data.Vertices[p.VertexIndieces[2]].Z, obj_data.Vertices[p.VertexIndieces[2]].Y);
                    Gl.glEnd();
                }
            }
            Gl.glDisable(Gl.GL_ALPHA_TEST);
            Gl.glPolygonMode(Gl.GL_FRONT_AND_BACK, Gl.GL_FILL);
        }

        private static void GlSetMaterial(MaterialInfo mat_info)
        {
            if (!mat_info.UseTexture)
            {
                Gl.glBindTexture(Gl.GL_TEXTURE_2D, 0);
                Gl.glColor4f(mat_info.AmbientColor.R / 255f, mat_info.AmbientColor.G / 255f, mat_info.AmbientColor.B / 255f, mat_info.Opacity);
            }
            else
            {
                Gl.glColor4f(mat_info.DiffuseColor.R / 255f, mat_info.DiffuseColor.G / 255f, mat_info.DiffuseColor.B / 255f, mat_info.Opacity);
                Gl.glBindTexture(Gl.GL_TEXTURE_2D, mat_info.OpenGLTexID);
            }
        }

        private MaterialInfo GetMaterialByName(string matname)
        {
            foreach (var material in Materials)
                if (material.Name == matname) return material;
            return null;
        }

        public float GetHeightValue(int face_id, Vector3 Position)
        {
            Vector3 Vertex1 = obj_data.Vertices[obj_data.Faces[face_id].VertexIndieces[0]];
            Vector3 Vertex2 = obj_data.Vertices[obj_data.Faces[face_id].VertexIndieces[1]];
            Vector3 Vertex3 = obj_data.Vertices[obj_data.Faces[face_id].VertexIndieces[2]];
            Vector3 Normals = (Vertex2 - Vertex1).Cross(Vertex3 - Vertex1);
            Normals /= (float)Math.Sqrt(Normals.X * Normals.X + Normals.Y * Normals.Y + Normals.Z * Normals.Z);
            return (Normals.X * (Position.X - Vertex1.X) + Normals.Z * (Position.Z - Vertex1.Z)) / -Normals.Y + Vertex1.Y;
        }


    }
}
