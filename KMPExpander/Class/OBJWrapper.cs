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
using Extensions;

namespace KMPExpander.Class
{
    public class OBJWrapper
    {
        public bool Visible = true;
        public bool Wireframe = false;
        public bool CullFrontFace = false;
        public bool CullBackFace = true;
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
                    if (Name != null && Name.ToUpper().Contains(matColor.Name))
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

        public bool Failed = false;
        private static int next_tex_id = 3;     // ID 0 is Default, ID 1 is Local Map and ID 2 is Global Map
        private int object_list = 0;

        public OBJWrapper(string path) : this(path, false) { }

        // quiet: no message box when the file cannot be read (Failed is set), for the object models.
        public OBJWrapper(string path, bool quiet)
        {
            base_path = Path.GetDirectoryName(path);
            try
            {
                obj_data = new OBJ(File.ReadAllBytes(path));
                // An OBJ without mtllib gets a colour per material name.
                bool has_mtl = !String.IsNullOrEmpty(obj_data.MTLPath);
                if (has_mtl)
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
                Failed = true;
                if (!quiet) MessageBox.Show("OBJ Wrapper Exception: \n\r" + e.ToString());
            }
        }

        private bool bounds_cached = false;
        private Vector3 bounds_min, bounds_max;
        private float[] groups_grid = null;
        private int[] groups_faces;
        private List<float[]> groups_rects;

        public bool GetBounds(out Vector3 min, out Vector3 max)
        {
            if (bounds_cached)
            {
                min = bounds_min;
                max = bounds_max;
                return min.X <= max.X;
            }
            min = new Vector3(float.MaxValue);
            max = new Vector3(float.MinValue);
            if (obj_data == null) return false;
            foreach (var p in obj_data.Faces)
            {
                foreach (var i in p.VertexIndieces)
                {
                    Vector3 v = obj_data.Vertices[i];
                    if (min.X > v.X) min.X = v.X;
                    if (min.Y > v.Y) min.Y = v.Y;
                    if (min.Z > v.Z) min.Z = v.Z;
                    if (max.X < v.X) max.X = v.X;
                    if (max.Y < v.Y) max.Y = v.Y;
                    if (max.Z < v.Z) max.Z = v.Z;
                }
            }
            bounds_min = min;
            bounds_max = max;
            bounds_cached = true;
            return min.X <= max.X;
        }

        // Splits the faces as the retail div files split the course model: one group per material and grid cell, a face
        // going to the cell that holds the centre of its XZ bounding box. Each group gets the XZ bounding box of its faces.
        public void GetCullingGroups(float minX, float minZ, float cellW, float cellH, out int[] faceGroups, out List<float[]> groupRects)
        {
            if (groups_grid != null && groups_grid[0] == minX && groups_grid[1] == minZ && groups_grid[2] == cellW && groups_grid[3] == cellH)
            {
                faceGroups = groups_faces;
                groupRects = groups_rects;
                return;
            }
            faceGroups = new int[obj_data.Faces.Count];
            groupRects = new List<float[]>();
            Dictionary<string, int> groups = new Dictionary<string, int>();
            for (int f = 0; f < obj_data.Faces.Count; f++)
            {
                var p = obj_data.Faces[f];
                float x0 = float.MaxValue, z0 = float.MaxValue, x1 = float.MinValue, z1 = float.MinValue;
                foreach (var i in p.VertexIndieces)
                {
                    Vector3 v = obj_data.Vertices[i];
                    x0 = Math.Min(x0, v.X); z0 = Math.Min(z0, v.Z);
                    x1 = Math.Max(x1, v.X); z1 = Math.Max(z1, v.Z);
                }
                int ix = cellW > 0 ? (int)Math.Floor(((x0 + x1) / 2 - minX) / cellW) : 0;
                int iz = cellH > 0 ? (int)Math.Floor(((z0 + z1) / 2 - minZ) / cellH) : 0;
                ix = Math.Max(0, Math.Min(CullingPreview.GridCells - 1, ix));
                iz = Math.Max(0, Math.Min(CullingPreview.GridCells - 1, iz));
                string key = p.Material + "/" + ix + "/" + iz;
                int g;
                if (!groups.TryGetValue(key, out g))
                {
                    g = groupRects.Count;
                    groups.Add(key, g);
                    groupRects.Add(new float[] { x0, z0, x1, z1 });
                }
                float[] r = groupRects[g];
                r[0] = Math.Min(r[0], x0); r[1] = Math.Min(r[1], z0);
                r[2] = Math.Max(r[2], x1); r[3] = Math.Max(r[3], z1);
                faceGroups[f] = g;
            }
            groups_grid = new float[] { minX, minZ, cellW, cellH };
            groups_faces = faceGroups;
            groups_rects = groupRects;
        }

        // Faces of the model, and those drawn: shown material, and not culled by the preview.
        public void CountFaces(CullingPreview culling, out int total, out int drawn)
        {
            total = obj_data.Faces.Count;
            drawn = 0;
            for (int f = 0; f < obj_data.Faces.Count; f++)
            {
                if (culling != null && culling.FaceCulled(f)) continue;
                if (GetMaterialByName(obj_data.Faces[f].Material).ShowMaterial) drawn++;
            }
        }

        // The height of the faces at the point that is closest to near (the highest one without near), for the
        // simulated kart; null if no face is there.
        public float? GetGroundHeight(float x, float z, float? near = null)
        {
            float? best = null;
            foreach (var p in obj_data.Faces)
            {
                if (p.VertexIndieces.Count < 3) continue;
                Vector3 a = obj_data.Vertices[p.VertexIndieces[0]], b = obj_data.Vertices[p.VertexIndieces[1]], c = obj_data.Vertices[p.VertexIndieces[2]];
                float d = (b.Z - c.Z) * (a.X - c.X) + (c.X - b.X) * (a.Z - c.Z);
                if (Math.Abs(d) < 1e-6f) continue;
                float l1 = ((b.Z - c.Z) * (x - c.X) + (c.X - b.X) * (z - c.Z)) / d;
                float l2 = ((c.Z - a.Z) * (x - c.X) + (a.X - c.X) * (z - c.Z)) / d;
                float l3 = 1 - l1 - l2;
                if (l1 < 0 || l2 < 0 || l3 < 0) continue;
                float y = l1 * a.Y + l2 * b.Y + l3 * c.Y;
                if (best == null || (near == null ? y > best.Value : Math.Abs(y - near.Value) < Math.Abs(best.Value - near.Value))) best = y;
            }
            return best;
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
            foreach (var mat in mtl_data.Materials)
            {
                MaterialInfo temp = new MaterialInfo(mat.Name);
                try
                {
                    Bitmap bitmap = new Bitmap(new MemoryStream(File.ReadAllBytes(base_path + "\\" + mat.DiffuseMapPath)));
                    temp.Texture = bitmap;
                    temp.OpenGLTexID = next_tex_id++;
                    temp.UpdateTex();
                    temp.UseTexture = true;
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

            ViewPlaneHandler vph = (Application.OpenForms[0] as Form1).vph;

            if (CullFrontFace || CullBackFace)
            {
                Gl.glEnable(Gl.GL_CULL_FACE);
                Gl.glCullFace(CullFrontFace && CullBackFace ? Gl.GL_FRONT_AND_BACK : CullFrontFace ? Gl.GL_FRONT : Gl.GL_BACK);
            }
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
                        vph.draw3DVertice(obj_data.Vertices[p.VertexIndieces[0]]);
                        vph.draw3DVertice(obj_data.Vertices[p.VertexIndieces[1]]);
                        vph.draw3DVertice(obj_data.Vertices[p.VertexIndieces[2]]);
                        Gl.glEnd();
                    }
                    i++;
                }
                return;
            }
            
            CullingPreview culling = (Application.OpenForms[0] as Form1).Culling;
            int f = -1;
            foreach (var p in obj_data.Faces)
            {
                f++;
                if (culling != null && culling.FaceCulled(f)) continue;
                if (GetMaterialByName(p.Material).ShowMaterial)
                {
                    GlSetMaterial(GetMaterialByName(p.Material));
                    Gl.glBegin(Gl.GL_TRIANGLES);
                    if (p.TexCoordIndieces.Count > 0) Gl.glTexCoord2f(obj_data.TexCoords[p.TexCoordIndieces[0]].X, obj_data.TexCoords[p.TexCoordIndieces[0]].Z);
                    vph.draw3DVertice(obj_data.Vertices[p.VertexIndieces[0]]);
                    if (p.TexCoordIndieces.Count > 0) Gl.glTexCoord2f(obj_data.TexCoords[p.TexCoordIndieces[1]].X, obj_data.TexCoords[p.TexCoordIndieces[1]].Z);
                    vph.draw3DVertice(obj_data.Vertices[p.VertexIndieces[1]]);
                    if (p.TexCoordIndieces.Count > 0) Gl.glTexCoord2f(obj_data.TexCoords[p.TexCoordIndieces[2]].X, obj_data.TexCoords[p.TexCoordIndieces[2]].Z);
                    vph.draw3DVertice(obj_data.Vertices[p.VertexIndieces[2]]);
                    Gl.glEnd();
                }
            }
            Gl.glDisable(Gl.GL_ALPHA_TEST);
            Gl.glDisable(Gl.GL_CULL_FACE);
            Gl.glPolygonMode(Gl.GL_FRONT_AND_BACK, Gl.GL_FILL);
        }

        // The model as a course object: its shown faces in model space (the caller sets the object's matrix), without the
        // culling preview, from a display list.
        public void RenderAsObject()
        {
            if (!Visible || obj_data == null) return;
            if (object_list != 0)
            {
                Gl.glCallList(object_list);
                return;
            }
            object_list = Gl.glGenLists(1);
            Gl.glNewList(object_list, Gl.GL_COMPILE_AND_EXECUTE);
            foreach (var p in obj_data.Faces)
            {
                if (p.VertexIndieces.Count < 3) continue;
                MaterialInfo mat = GetMaterialByName(p.Material);
                if (mat != null && !mat.ShowMaterial) continue;
                if (mat != null) GlSetMaterial(mat);
                else
                {
                    Gl.glBindTexture(Gl.GL_TEXTURE_2D, 0);
                    Gl.glColor4f(0.8f, 0.8f, 0.8f, 1f);
                }
                Gl.glBegin(Gl.GL_TRIANGLES);
                for (int k = 0; k < 3; k++)
                {
                    if (p.TexCoordIndieces.Count > k) Gl.glTexCoord2f(obj_data.TexCoords[p.TexCoordIndieces[k]].X, obj_data.TexCoords[p.TexCoordIndieces[k]].Z);
                    Vector3 v = obj_data.Vertices[p.VertexIndieces[k]];
                    Gl.glVertex3f(v.X, v.Y, v.Z);
                }
                Gl.glEnd();
            }
            Gl.glEndList();
        }

        private static void GlSetMaterial(MaterialInfo mat_info)
        {
            if (!mat_info.UseTexture)
            {
                Gl.glBindTexture(Gl.GL_TEXTURE_2D, 0);
                Gl.glColor4f(mat_info.DiffuseColor.R / 255f, mat_info.DiffuseColor.G / 255f, mat_info.DiffuseColor.B / 255f, mat_info.Opacity);
            }
            else
            {
                Gl.glColor4f(1f, 1f, 1f, mat_info.Opacity);
                Gl.glBindTexture(Gl.GL_TEXTURE_2D, mat_info.OpenGLTexID);
            }
        }

        private MaterialInfo GetMaterialByName(string matname)
        {
            foreach (var material in Materials)
                if (material.Name == matname) return material;
            return null;
        }

        // The distance along dir to the nearest shown face the ray hits, null when it hits none (the 3D view's snapping).
        // The distance along dir to the nearest shown face the ray hits,
        // null when it hits none (the 3D view's snapping).
        public float? RayCast(Vector3 origin, Vector3 dir)
        {
            if (!Visible || (CullFrontFace && CullBackFace))
                return null;

            float? best = null;

            foreach (var p in obj_data.Faces)
            {
                if (p.VertexIndieces.Count < 3 ||
                    !GetMaterialByName(p.Material).ShowMaterial)
                    continue;

                Vector3 a = obj_data.Vertices[p.VertexIndieces[0]];
                Vector3 b = obj_data.Vertices[p.VertexIndieces[1]];
                Vector3 c = obj_data.Vertices[p.VertexIndieces[2]];

                // Determine which side of the triangle the ray approaches.
                if (CullFrontFace || CullBackFace)
                {
                    Vector3 normal = (b - a).Cross(c - a);
                    float facing = normal.Dot(dir);

                    // Front-facing: ray approaches against the normal.
                    if (CullFrontFace && facing < 0f)
                        continue;

                    // Back-facing: ray approaches along the normal.
                    if (CullBackFace && facing > 0f)
                        continue;
                }

                float? d = Ray3D.Triangle(origin, dir, a, b, c);

                if (d != null && (best == null || d.Value < best.Value))
                    best = d;
            }

            return best;
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
