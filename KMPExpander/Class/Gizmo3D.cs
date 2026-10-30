using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Extensions;
using LibCTR.Collections;
using Tao.OpenGl;

namespace KMPExpander.Class
{
    // Rays of the 3D view against triangles, planes and axes.
    public static class Ray3D
    {
        // The distance along dir at which the ray hits the triangle (either side), null when it misses (Moller-Trumbore).
        public static float? Triangle(Vector3 origin, Vector3 dir, Vector3 a, Vector3 b, Vector3 c)
        {
            Vector3 e1 = b - a, e2 = c - a;
            Vector3 p = dir.Cross(e2);
            float det = e1.Dot(p);
            if (Math.Abs(det) < 1e-8f) return null;
            float inv = 1 / det;
            Vector3 s = origin - a;
            float u = s.Dot(p) * inv;
            if (u < 0 || u > 1) return null;
            Vector3 q = s.Cross(e1);
            float v = dir.Dot(q) * inv;
            if (v < 0 || u + v > 1) return null;
            float t = e2.Dot(q) * inv;
            return t > 0 ? t : (float?)null;
        }

        // Where the ray meets the plane through point with that normal, null when it is parallel or behind the ray.
        public static Vector3? Plane(Vector3 origin, Vector3 dir, Vector3 point, Vector3 normal)
        {
            float den = normal.Dot(dir);
            if (Math.Abs(den) < 1e-4f) return null;
            float t = normal.Dot(point - origin) / den;
            if (t <= 0) return null;
            return origin + dir * t;
        }

        // The position along the axis line through point that is closest to the ray, null when they are parallel.
        public static float? Axis(Vector3 origin, Vector3 dir, Vector3 point, Vector3 axis)
        {
            float b = axis.Dot(dir);
            float den = 1 - b * b;
            if (den < 1e-4f) return null;
            Vector3 w = point - origin;
            return (b * dir.Dot(w) - axis.Dot(w)) / den;
        }
    }

    // The transform gizmo of the 3D view, in three modes like Maya's: arrows per axis (X red, Y green, Z blue) and a square
    // per plane to move; a ball with a ring per axis to turn around it (the ball itself turns freely, as a trackball);
    // arrows ending in boxes to scale along an axis, and a centre box to scale all three. axes says which axes the entry has (checkpoints have no height, enemy points have one
    // scale for all).
    public static class TransformGizmo
    {
        public enum Modes { Move, Rotate, Scale }
        public const int None = 0, X = 1, Y = 2, Z = 3, XY = 4, XZ = 5, YZ = 6, Centre = 7, Ball = 8;

        public static bool IsAxis(int handle) { return handle >= X && handle <= Z; }

        public static Vector3 Axis(int handle)
        {
            switch (handle)
            {
                case X: return new Vector3(1, 0, 0);
                case Y: return new Vector3(0, 1, 0);
                default: return new Vector3(0, 0, 1);
            }
        }

        public static Vector3 PlaneNormal(int handle)
        {
            switch (handle)
            {
                case XY: return new Vector3(0, 0, 1);
                case YZ: return new Vector3(1, 0, 0);
                default: return new Vector3(0, 1, 0);
            }
        }

        // Two unit vectors perpendicular to an axis.
        private static void Perpendicular(int axis, out Vector3 u, out Vector3 v)
        {
            u = axis == X ? new Vector3(0, 1, 0) : new Vector3(1, 0, 0);
            v = axis == Z ? new Vector3(0, 1, 0) : new Vector3(0, 0, 1);
        }

        private static Color HandleColor(int handle)
        {
            switch (handle)
            {
                case X: case YZ: return Color.FromArgb(230, 50, 50);
                case Y: case XZ: return Color.FromArgb(60, 200, 60);
                case Centre: return Color.FromArgb(230, 230, 230);
                default: return Color.FromArgb(50, 110, 240);
            }
        }

        private static void SetColor(int handle, int active, float alpha, bool picking)
        {
            Color c = picking ? SectionPicking.GetColor(Sections.Gizmo, 0, handle) : handle == active ? Color.Yellow : HandleColor(handle);
            Gl.glColor4f(c.R / 255f, c.G / 255f, c.B / 255f, picking ? 1f : alpha);
        }

        // size is the length of the arrows (the radius of the rings) in world units. Drawn over everything; when picking,
        // the shafts start away from the point, so that the point itself can still be picked.
        public static void Render(Modes mode, Vector3 pos, float size, int active, int[] axes, bool picking)
        {
            ViewPlaneHandler vph = (System.Windows.Forms.Application.OpenForms[0] as Form1).vph;
            Gl.glBindTexture(Gl.GL_TEXTURE_2D, 0);
            Gl.glDepthFunc(Gl.GL_ALWAYS);
            Gl.glLoadIdentity();
            if (mode == Modes.Rotate) RenderRotate(vph, pos, size, active, axes, picking);
            else
            {
                if (mode == Modes.Move) RenderPlanes(vph, pos, size, active, axes, picking);
                foreach (int h in axes)
                {
                    Vector3 a = Axis(h);
                    SetColor(h, active, 1f, picking);
                    Gl.glLineWidth(picking ? 10 : 3);
                    Gl.glBegin(Gl.GL_LINES);
                    vph.draw3DVertice(pos + a * (picking ? size * 0.18f : 0));
                    vph.draw3DVertice(pos + a * (size * (mode == Modes.Move ? 0.8f : 0.9f)));
                    Gl.glEnd();
                    if (mode == Modes.Move) Cone(vph, h, pos + a * (size * 0.8f), pos + a * size, size * (picking ? 0.1f : 0.07f));
                    else Box(vph, pos + a * size, size * (picking ? 0.1f : 0.07f));
                }
                if (mode == Modes.Scale)
                {
                    SetColor(Centre, active, 1f, picking);
                    Box(vph, pos, size * 0.1f);
                }
            }
            Gl.glLineWidth(1);
        }

        private static void RenderPlanes(ViewPlaneHandler vph, Vector3 pos, float size, int active, int[] axes, bool picking)
        {
            List<int> planes = new List<int>();
            if (axes.Contains(X) && axes.Contains(Y)) planes.Add(XY);
            if (axes.Contains(X) && axes.Contains(Z)) planes.Add(XZ);
            if (axes.Contains(Y) && axes.Contains(Z)) planes.Add(YZ);
            foreach (int h in planes)
            {
                Vector3 n = PlaneNormal(h);
                Vector3 u = n.X != 0 ? new Vector3(0, 1, 0) : new Vector3(1, 0, 0);
                Vector3 v = n.Z != 0 ? new Vector3(0, 1, 0) : new Vector3(0, 0, 1);
                Vector3[] quad = {
                    pos + u * (size * 0.25f) + v * (size * 0.25f), pos + u * (size * 0.45f) + v * (size * 0.25f),
                    pos + u * (size * 0.45f) + v * (size * 0.45f), pos + u * (size * 0.25f) + v * (size * 0.45f) };
                SetColor(h, active, 0.35f, picking);
                Gl.glBegin(Gl.GL_QUADS);
                foreach (var q in quad) vph.draw3DVertice(q);
                Gl.glEnd();
                if (picking) continue;
                SetColor(h, active, 1f, false);
                Gl.glLineWidth(1);
                Gl.glBegin(Gl.GL_LINE_LOOP);
                foreach (var q in quad) vph.draw3DVertice(q);
                Gl.glEnd();
            }
        }

        // The ball (a disc facing the camera) and, of each ring, the half on the camera's side. The rings are drawn after the
        // ball, so where they cross it they are picked.
        private static void RenderRotate(ViewPlaneHandler vph, Vector3 pos, float size, int active, int[] axes, bool picking)
        {
            Vector3 toCamera = (vph.CameraPos - pos).Normalize();
            Vector3 r = toCamera.Cross(new Vector3(0, 1, 0));
            r = r.Length < 1e-4f ? new Vector3(1, 0, 0) : r.Normalize();
            Vector3 up = r.Cross(toCamera);
            if (picking) SetColor(Ball, active, 1f, true);
            else if (active == Ball) Gl.glColor4f(1f, 1f, 0f, 0.3f);
            else Gl.glColor4f(0.8f, 0.8f, 0.8f, 0.2f);
            // When picking, the middle is left out so that the point itself can still be picked.
            float inner = picking ? 0.18f : 0;
            Gl.glBegin(Gl.GL_QUAD_STRIP);
            for (int i = 0; i <= 48; i++)
            {
                double t = i * Math.PI / 24;
                Vector3 dir = r * (float)Math.Cos(t) + up * (float)Math.Sin(t);
                vph.draw3DVertice(pos + dir * (size * inner));
                vph.draw3DVertice(pos + dir * (size * 0.95f));
            }
            Gl.glEnd();
            if (!picking)
            {
                Gl.glColor4f(0.6f, 0.6f, 0.6f, 1f);
                Gl.glLineWidth(1);
                Gl.glBegin(Gl.GL_LINE_LOOP);
                for (int i = 0; i < 48; i++)
                {
                    double t = i * Math.PI / 24;
                    vph.draw3DVertice(pos + r * (size * 1.05f * (float)Math.Cos(t)) + up * (size * 1.05f * (float)Math.Sin(t)));
                }
                Gl.glEnd();
            }
            foreach (int h in axes)
            {
                Vector3 u, v;
                Perpendicular(h, out u, out v);
                SetColor(h, active, 1f, picking);
                Gl.glLineWidth(picking ? 10 : 3);
                Gl.glBegin(Gl.GL_LINES);
                for (int i = 0; i < 64; i++)
                {
                    Vector3 p0 = u * (float)Math.Cos(i * Math.PI / 32) + v * (float)Math.Sin(i * Math.PI / 32);
                    Vector3 p1 = u * (float)Math.Cos((i + 1) * Math.PI / 32) + v * (float)Math.Sin((i + 1) * Math.PI / 32);
                    if ((p0 + p1).Dot(toCamera) < -0.1f) continue;
                    vph.draw3DVertice(pos + p0 * size);
                    vph.draw3DVertice(pos + p1 * size);
                }
                Gl.glEnd();
            }
        }

        private static void Cone(ViewPlaneHandler vph, int axis, Vector3 baseCentre, Vector3 tip, float radius)
        {
            Vector3 u, v;
            Perpendicular(axis, out u, out v);
            foreach (Vector3 apex in new Vector3[] { tip, baseCentre })
            {
                Gl.glBegin(Gl.GL_TRIANGLE_FAN);
                vph.draw3DVertice(apex);
                for (int i = 0; i <= 12; i++)
                {
                    double angle = i * Math.PI / 6;
                    vph.draw3DVertice(baseCentre + u * (radius * (float)Math.Cos(angle)) + v * (radius * (float)Math.Sin(angle)));
                }
                Gl.glEnd();
            }
        }

        private static void Box(ViewPlaneHandler vph, Vector3 c, float h)
        {
            int[][] faces = { new[] { 0, 1, 3, 2 }, new[] { 4, 6, 7, 5 }, new[] { 0, 4, 5, 1 }, new[] { 2, 3, 7, 6 }, new[] { 0, 2, 6, 4 }, new[] { 1, 5, 7, 3 } };
            Gl.glBegin(Gl.GL_QUADS);
            foreach (var f in faces)
                foreach (int i in f)
                    vph.draw3DVertice(c + new Vector3((i & 4) != 0 ? h : -h, (i & 2) != 0 ? h : -h, (i & 1) != 0 ? h : -h));
            Gl.glEnd();
        }
    }
}
