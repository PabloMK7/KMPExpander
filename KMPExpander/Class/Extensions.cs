using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Design;
using System.Drawing.Design;
using System.Globalization;
using LibCTR.Collections;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Tao.OpenGl;

namespace Extensions
{
    public static class pointInPoly
    {
        public struct Point
        {
            public float x;
            public float y;
            public Point(float x1, float y1)
            {
                x = x1;
                y = y1;
            }
        };
        public static bool onSegment(Point p, Point q, Point r)
        {
            if (q.x <= max(p.x, r.x) && q.x >= min(p.x, r.x) &&
                    q.y <= max(p.y, r.y) && q.y >= min(p.y, r.y))
                return true;
            return false;
        }

        private static float min(float y1, float y2)
        {
            if (y1 < y2) return y1;
            return y2;
        }

        private static float max(float y1, float y2)
        {
            if (y1 > y2) return y1;
            return y2;
        }

        // To find orientation of ordered triplet (p, q, r).
        // The function returns following values
        // 0 --> p, q and r are colinear
        // 1 --> Clockwise
        // 2 --> Counterclockwise
        public static int orientation(Point p, Point q, Point r)
        {
            float val = (q.y - p.y) * (r.x - q.x) -
                      (q.x - p.x) * (r.y - q.y);

            if (val == 0) return 0;  // colinear
            return (val > 0) ? 1 : 2; // clock or counterclock wise
        }

        // The function that returns true if line segment 'p1q1'
        // and 'p2q2' intersect.
        public static bool doIntersect(Point p1, Point q1, Point p2, Point q2)
        {
            // Find the four orientations needed for general and
            // special cases
            int o1 = orientation(p1, q1, p2);
            int o2 = orientation(p1, q1, q2);
            int o3 = orientation(p2, q2, p1);
            int o4 = orientation(p2, q2, q1);

            // General case
            if (o1 != o2 && o3 != o4)
                return true;

            // Special Cases
            // p1, q1 and p2 are colinear and p2 lies on segment p1q1
            if (o1 == 0 && onSegment(p1, p2, q1)) return true;

            // p1, q1 and p2 are colinear and q2 lies on segment p1q1
            if (o2 == 0 && onSegment(p1, q2, q1)) return true;

            // p2, q2 and p1 are colinear and p1 lies on segment p2q2
            if (o3 == 0 && onSegment(p2, p1, q2)) return true;

            // p2, q2 and q1 are colinear and q1 lies on segment p2q2
            if (o4 == 0 && onSegment(p2, q1, q2)) return true;

            return false; // Doesn't fall in any of the above cases
        }

        // Returns true if the point p lies inside the polygon[] with n vertices
        public static bool isInside(Point[] polygon, int n, Point p)
        {
            // There must be at least 3 vertices in polygon[]
            if (n < 3) return false;

            // Create a point for line segment from p to infinite
            Point extreme = new Point(float.MaxValue, p.y);

            // Count intersections of the above line with sides of polygon
            int count = 0, i = 0;
            do
            {
                int next = (i + 1) % n;

                // Check if the line segment from 'p' to 'extreme' intersects
                // with the line segment from 'polygon[i]' to 'polygon[next]'
                if (doIntersect(polygon[i], polygon[next], p, extreme))
                {
                    // If the point 'p' is colinear with line segment 'i-next',
                    // then check if it lies on segment. If it lies, return true,
                    // otherwise false
                    if (orientation(polygon[i], p, polygon[next]) == 0)
                        return onSegment(polygon[i], p, polygon[next]);

                    count++;
                }
                i = next;
            } while (i != 0);

            // Return true if count is odd, false otherwise
            return (count & 1) == 1;  // Same as (count%2 == 1)
        }
    }
    public static class RadianDegree
    {
        public static float ToRadians(this float val)
        {
            return (float)((Math.PI / 180.0) * val);
        }

        public static float ToDegrees(this float val)
        {
            return (float)(val * (180.0 / Math.PI));
        }
        public static double ToRadiansD(this double val)
        {
            return (Math.PI / 180.0) * val;
        }

        public static double ToDegreesD(this double val)
        {
            return val * (180.0 / Math.PI);
        }
    }

    public class CustomEditor : UITypeEditor
    {
        public override UITypeEditorEditStyle GetEditStyle(ITypeDescriptorContext context)
        {
            return UITypeEditorEditStyle.None;
        }
    }

    public class HexTypeConverter : TypeConverter
    {
        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
        {
            if (sourceType == typeof(string)) return true;
            else return base.CanConvertFrom(context, sourceType);
        }

        public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
        {
            if (destinationType == typeof(string)) return true;
            else return base.CanConvertTo(context, destinationType);
        }

        public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
        {
            if (destinationType != typeof(string)) return base.ConvertTo(context, culture, value, destinationType);
            bool reverse = false;

            if (value.GetType() == typeof(byte))
            {
                byte val = (byte)value;
                string t = string.Format("{0:X2}", val);
                for (int i = 0; i < 2 - t.Length; i++)
                {
                    t += "0";
                }
                return t;
            }
            else if (value.GetType() == typeof(UInt16))
            {
                UInt16 val = (UInt16)value;
                if (reverse) val = (UInt16)((((val >> 0) & 0xFF) << 8) | (((val >> 8) & 0xFF) << 0));
                string t = string.Format("{0:X4}", val);
                for (int i = 0; i < 4 - t.Length; i++)
                {
                    t += "0";
                }
                return t;
            }
            else if (value.GetType() == typeof(UInt32))
            {
                UInt32 val = (UInt32)value;
                string t = string.Format("{0:X8}", val);
                for (int i = 0; i < 8 - t.Length; i++)
                {
                    t += "0";
                }
                return t;
            }
            else return base.ConvertTo(context, culture, value, destinationType);
        }
        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
        {
            if (value.GetType() == typeof(string))
            {
                string input = (string)value;
                if (input.StartsWith("0x", StringComparison.OrdinalIgnoreCase)) input = input.Substring(2);

                PropertyDescriptor p;

                if (context != null) p = context.PropertyDescriptor;

                if (context == null)
                {
                    return UInt32.Parse(input, NumberStyles.HexNumber, culture);
                }
                else if (context.PropertyDescriptor.PropertyType == typeof(UInt16))
                {
                    return UInt16.Parse(input, NumberStyles.HexNumber, culture);
                }
                else if (context.PropertyDescriptor.PropertyType == typeof(UInt32))
                {
                    return UInt32.Parse(input, NumberStyles.HexNumber, culture);
                }
                else return base.ConvertFrom(context, culture, value);
            }
            else return base.ConvertFrom(context, culture, value);
        }
    }
    public class BinTypeConverter : TypeConverter
    {
        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
        {
            if (sourceType == typeof(string)) return true;
            else return base.CanConvertFrom(context, sourceType);
        }

        public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
        {
            if (destinationType == typeof(string)) return true;
            else return base.CanConvertTo(context, destinationType);
        }

        public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
        {
            if (destinationType != typeof(string)) return base.ConvertTo(context, culture, value, destinationType);
                
            if (value.GetType() == typeof(byte))
            {
                byte val = (byte)value;
                string r = "";
                for (int i = 7; i >= 0; i--)
                {
                    r += (((val >> i) & 1) == 1) ? "1" : "0";
                }
                return r;
            }
            else return base.ConvertTo(context, culture, value, destinationType);
        }

        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
        {
            if (value.GetType() == typeof(string))
            {
                string input = (string)value;
                    
                PropertyDescriptor p;

                if (context != null) p = context.PropertyDescriptor;

                byte ret = 0;
                if (input.Length > 8) throw new FormatException();
                for(int i= 0; i < input.Length; i++)
                {
                    if (input[input.Length - (i + 1)] == '1' || input[input.Length - (i + 1)] == '0')
                    {
                        ret |= (byte)(((input[input.Length - (i + 1)] == '1') ? 1 : 0) << i);
                    } else
                    {
                        throw new FormatException();
                    }
                }
                return ret;
            }
            else return base.ConvertFrom(context, culture, value);
        }
    }
    public class ViewPlaneHandler
    {
        public enum PLANE_MODES
        {
            XZ,
            XY,
            ZY
        };

        public enum AXIS
        {
            X,
            Y,
            Z
        };

        public PLANE_MODES mode;
        // The 3D view keeps the XZ layout of the 2D one (X, Z, and the height Y on the depth axis) and its camera matrix
        // turns that back into the world, so the 2D drawing code only needs a height for what it draws in XZ.
        public bool Is3D = false;
        // Set by the 3D camera every frame: what drawPoint needs to size its spheres, and whether this is the picking
        // pass (flat colours, no lighting).
        public bool Picking = false;
        public Vector3 CameraPos, CameraDir;
        public float PixelScale = 1;          // world units per pixel at distance 1 along CameraDir
        public float PointRadius = 25;        // world radius of a sphere of the base point size
        public float BasePointSize = 7;
        private int sphereList = 0;

        public ViewPlaneHandler()
        {
            mode = PLANE_MODES.XZ;
        }

        // A point given by its X and Z, at height y in the 3D view.
        public void drawXZ(float x, float z, float y)
        {
            if (Is3D) Gl.glVertex3f(x, z, y);
            else Gl.glVertex2f(x, z);
        }

        // A point of size pixels: a square in 2D; in 3D a lit sphere of PointRadius (never smaller than size pixels on
        // screen). The shading outlines the spheres, so the border passes of the 2D points are left out.
        public void drawPoint(Vector3 v, float size, bool border = false)
        {
            if (!Is3D)
            {
                Gl.glPointSize(size);
                Gl.glBegin(Gl.GL_POINTS);
                draw2DVertice(v);
                Gl.glEnd();
                return;
            }
            if (border) return;
            float depth = Math.Max(1f, (v - CameraPos).Dot(CameraDir));
            float r = Math.Max(PointRadius * size / Math.Max(1f, BasePointSize), size / 2 * depth * PixelScale);
            Gl.glPushMatrix();
            Gl.glTranslatef(v.X, v.Z, v.Y);
            Gl.glScalef(r, r, r);
            if (!Picking) Gl.glEnable(Gl.GL_LIGHTING);
            if (sphereList == 0) CompileSphere();
            else Gl.glCallList(sphereList);
            Gl.glDisable(Gl.GL_LIGHTING);
            Gl.glPopMatrix();
        }

        // In 3D, the volume of a point of radius r (the scale of enemy, item and glider points): a see-through sphere in
        // the fill colour (a quarter of its alpha, as the 2D circles) with its equator in the border colour. False in 2D,
        // where the caller draws its circle.
        public bool drawVolume(Vector3 v, float r, System.Drawing.Color border, System.Drawing.Color fill)
        {
            if (!Is3D) return false;
            Gl.glPushMatrix();
            Gl.glTranslatef(v.X, v.Z, v.Y);
            Gl.glScalef(r, r, r);
            Gl.glDepthMask(Gl.GL_FALSE);
            Gl.glColor4f(fill.R / 255f, fill.G / 255f, fill.B / 255f, fill.A / (255f * 4f));
            if (!Picking) Gl.glEnable(Gl.GL_LIGHTING);
            if (sphereList == 0) CompileSphere();
            else Gl.glCallList(sphereList);
            Gl.glDisable(Gl.GL_LIGHTING);
            Gl.glColor4f(border.R / 255f, border.G / 255f, border.B / 255f, border.A / 255f);
            Gl.glBegin(Gl.GL_LINE_LOOP);
            for (int i = 0; i < 32; i++)
            {
                double a = 2 * Math.PI * i / 32;
                Gl.glVertex3f((float)Math.Cos(a), (float)Math.Sin(a), 0);
            }
            Gl.glEnd();
            Gl.glDepthMask(Gl.GL_TRUE);
            Gl.glPopMatrix();
            return true;
        }

        // A unit sphere, its normals being its vertices (as the sphere is symmetric, the (X, Z, Y) layout changes nothing).
        private void CompileSphere()
        {
            const int slices = 16, stacks = 10;
            sphereList = Gl.glGenLists(1);
            Gl.glNewList(sphereList, Gl.GL_COMPILE_AND_EXECUTE);
            for (int i = 0; i < stacks; i++)
            {
                double a0 = Math.PI * i / stacks - Math.PI / 2, a1 = Math.PI * (i + 1) / stacks - Math.PI / 2;
                Gl.glBegin(Gl.GL_QUAD_STRIP);
                for (int j = 0; j <= slices; j++)
                {
                    double b = 2 * Math.PI * j / slices;
                    foreach (double a in new double[] { a1, a0 })
                    {
                        float x = (float)(Math.Cos(a) * Math.Cos(b)), y = (float)(Math.Cos(a) * Math.Sin(b)), z = (float)Math.Sin(a);
                        Gl.glNormal3f(x, y, z);
                        Gl.glVertex3f(x, y, z);
                    }
                }
                Gl.glEnd();
            }
            Gl.glEndList();
        }

        // The OpenGL matrix that draws a model given in world axes at pos, rotated (degrees; X, then Y, then Z, as sead's
        // makeR) and scaled, in the layout of this view (that of draw3DVertice).
        public float[] modelMatrix(Vector3 pos, Vector3 rotation, Vector3 scale)
        {
            double rx = rotation.X * Math.PI / 180, ry = rotation.Y * Math.PI / 180, rz = rotation.Z * Math.PI / 180;
            double cx = Math.Cos(rx), sx = Math.Sin(rx), cy = Math.Cos(ry), sy = Math.Sin(ry), cz = Math.Cos(rz), sz = Math.Sin(rz);
            double[,] r = {
                { cy * cz, sx * sy * cz - cx * sz, cx * sy * cz + sx * sz },
                { cy * sz, sx * sy * sz + cx * cz, cx * sy * sz - sx * cz },
                { -sy, sx * cy, cx * cy } };
            double[,] view;
            switch (Is3D ? PLANE_MODES.XZ : mode)
            {
                case PLANE_MODES.XY: view = new double[,] { { 1, 0, 0 }, { 0, -1, 0 }, { 0, 0, 1 } }; break;
                case PLANE_MODES.ZY: view = new double[,] { { 0, 0, 1 }, { 0, -1, 0 }, { -1, 0, 0 } }; break;
                default: view = new double[,] { { 1, 0, 0 }, { 0, 0, 1 }, { 0, 1, 0 } }; break;
            }
            double[] s = { scale.X, scale.Y, scale.Z }, p = { pos.X, pos.Y, pos.Z };
            float[] m = new float[16];
            for (int row = 0; row < 3; row++)
            {
                for (int col = 0; col < 3; col++)
                {
                    double v = 0;
                    for (int k = 0; k < 3; k++) v += view[row, k] * r[k, col];
                    m[col * 4 + row] = (float)(v * s[col]);
                }
                double t = 0;
                for (int k = 0; k < 3; k++) t += view[row, k] * p[k];
                m[12 + row] = (float)t;
            }
            m[15] = 1;
            return m;
        }

        public void translateXZ(float x, float z, float y)
        {
            Gl.glTranslatef(x, z, Is3D ? y : 0);
        }

        // What is drawn in XZ until popHeight (the circles around the points) goes to height y in the 3D view.
        // The circles are see-through, so they do not hide what is drawn behind them later.
        public void pushHeight(float y)
        {
            if (!Is3D) return;
            Gl.glPushMatrix();
            Gl.glTranslatef(0, 0, y);
            Gl.glDepthMask(Gl.GL_FALSE);
        }

        public void popHeight()
        {
            if (!Is3D) return;
            Gl.glDepthMask(Gl.GL_TRUE);
            Gl.glPopMatrix();
        }

        public float getViewCoord(Vector3 v, int a)
        {
            switch (mode)
            {
                case PLANE_MODES.XY:
                    if (a == 0) return v.X;
                    else return -v.Y; 
                case PLANE_MODES.ZY:
                    if (a == 0) return v.Z;
                    else return -v.Y;
                case PLANE_MODES.XZ:
                default:
                    if (a == 0) return v.X;
                    else return v.Z;
            }
        }

        public void draw2DVertice(Vector3 v)
        {
            if (Is3D)
            {
                Gl.glVertex3f(v.X, v.Z, v.Y);
                return;
            }
            switch (mode)
            {
                case PLANE_MODES.XY:
                    Gl.glVertex2f(v.X, -v.Y);
                    return;
                case PLANE_MODES.ZY:
                    Gl.glVertex2f(v.Z, -v.Y);
                    return;
                case PLANE_MODES.XZ:
                default:
                    Gl.glVertex2f(v.X, v.Z);
                    return;
            }
        }

        /*
         * NOTE: CODE IS WRONG
         * public void storeToVector(ref Vector3 dst, Vector3 src, AXIS a)
        {
            switch (mode)
            {
                case PLANE_MODES.XY:
                    switch(a)
                    {
                        case AXIS.X:
                            dst.X = src.X;
                            break;
                        case AXIS.Y:
                            dst.Y = src.Z;
                            break;
                        case AXIS.Z:
                            dst.Z = -src.Y;
                            break;
                    }
                    return;
                case PLANE_MODES.ZY:
                    switch (a)
                    {
                        case AXIS.X:
                            dst.X = src.Z;
                            break;
                        case AXIS.Y:
                            dst.Y = -src.X;
                            break;
                        case AXIS.Z:
                            dst.Z = -src.Y;
                            break;
                    }
                    return;
                case PLANE_MODES.XZ:
                default:
                    switch (a)
                    {
                        case AXIS.X:
                            dst.X = src.X;
                            break;
                        case AXIS.Y:
                            dst.Y = src.Y;
                            break;
                        case AXIS.Z:
                            dst.Z = src.Z;
                            break;
                    }
                    return;
            }
        }*/

        public void storeToVector(ref Vector3 dst, Vector3 src)
        {
            switch (mode)
            {
                case PLANE_MODES.XY:
                    dst.X = src.X;
                    dst.Y = -src.Z;
                    dst.Z = src.Y;
                    return;
                case PLANE_MODES.ZY:
                    dst.Y = -src.Z;
                    dst.Z = src.X;
                    dst.X = -src.Y;
                    return;
                case PLANE_MODES.XZ:
                default:
                    dst.X = src.X;
                    dst.Y = src.Y;
                    dst.Z = src.Z;
                    return;
            }
        }

        public void storePlane(ref Vector3 dst, Vector3 src)
        {
            switch (mode)
            {
                case PLANE_MODES.XY:
                    dst.X = src.X;
                    dst.Y = -src.Z;
                    return;
                case PLANE_MODES.ZY:
                    dst.Y = -src.Z;
                    dst.Z = src.X;
                    return;
                case PLANE_MODES.XZ:
                default:
                    dst.X = src.X;
                    dst.Z = src.Z;
                    return;
            }
        }

        public void draw3DVertice(Vector3 v)
        {
            switch (mode)
            {
                case PLANE_MODES.XY:
                    Gl.glVertex3f(v.X, -v.Y, v.Z);
                    return;
                case PLANE_MODES.ZY:
                    Gl.glVertex3f(v.Z, -v.Y, -v.X);
                    return;
                case PLANE_MODES.XZ:
                default:
                    Gl.glVertex3f(v.X, v.Z, v.Y);
                    return;
            }
        }
    }
}

