using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Design;
using System.Drawing.Design;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

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
}

