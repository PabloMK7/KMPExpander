using Extensions;
using LibCTR.Collections;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using System.Windows.Input;
using Tao.OpenGl;

namespace KMPExpander.Class.SimpleKMPs
{
    // Plays the course intro as the game does (IntroCameraDirector), 60 frames per second, until its 700 frames are over
    // or Escape is pressed. The 3D view shows what the camera sees on a screen of the 3DS's size; the 2D views show the
    // camera, its route, its view and its look-at points on the map.
    public class CamePlayer
    {
        public Form1 Parent;
        private IntroCameraDirector director;
        private float fraction = 1;

        public CamePlayer(Form1 Parent) {
            this.Parent = Parent;
        }
        public void blockStuff()
        {
            Parent.dataGridView1.ReadOnly = true;
            Parent.menuStrip1.Enabled = false;
            Parent.toolStripButtonAdd.Enabled = false;
            Parent.toolStripButtonRemove.Enabled = false;
            Parent.toolStripButtonUp.Enabled = false;
            Parent.toolStripButtonDown.Enabled = false;
            Parent.toolStripButtonPencil.Enabled = false;
            Parent.allowRender = false;
            Parent.PreventClose = true;
        }
        public void unblockStuff()
        {
            Parent.dataGridView1.ReadOnly = false;
            Parent.menuStrip1.Enabled = true;
            Parent.toolStripButtonAdd.Enabled = true;
            Parent.toolStripButtonRemove.Enabled = true;
            Parent.toolStripButtonUp.Enabled = true;
            Parent.toolStripButtonDown.Enabled = true;
            Parent.toolStripButtonPencil.Enabled = true;
            Parent.allowRender = true;
            Parent.PreventClose = false;
        }
        public void playIntroSequence()
        {
            if (Parent.Kayempee == null) return;
            director = new IntroCameraDirector(Parent.Kayempee);
            if (director.Error != null || director.Chain.Count == 0)
            {
                MessageBox.Show(director.Error ?? "The course has no intro cameras.", "Play Intro Camera", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            KartSimulator sim = Parent.Kayempee.KartSimulator;
            sim.PlaceForIntro(Parent.Kayempee, Parent.OBJModel);
            Func<int, KartView> karts = Parent.DemoKarts();
            blockStuff();
            Parent.IntroPlaying = true;
            try
            {
                director.Start(karts);
                Render();
                Stopwatch clock = Stopwatch.StartNew();
                long frames = 0;
                double lastDrawn = -1;
                while (!director.Finished)
                {
                    Application.DoEvents();
                    if (Keyboard.IsKeyDown(Key.Escape) || Parent.IsDisposed) break;
                    // At most 6 frames at once, so that a slow frame does not make the camera jump.
                    double exact = clock.Elapsed.TotalSeconds * 60;
                    long due = (long)exact;
                    int steps = (int)Math.Max(0, Math.Min(6, due - frames));
                    frames = Math.Max(frames + steps, due - 6);
                    for (int i = 0; i < steps && !director.Finished; i++) director.Step(karts);
                    // Drawn about 60 times a second, between the last two frames, so that the camera moves smoothly.
                    if (exact - lastDrawn < 0.9)
                    {
                        Thread.Sleep(1);
                        continue;
                    }
                    lastDrawn = exact;
                    fraction = (float)Math.Max(0, Math.Min(1, exact - frames));
                    Render();
                }
            }
            finally
            {
                Parent.IntroPlaying = false;
                sim.RestoreAfterIntro();
                unblockStuff();
            }
        }

        private string Status()
        {
            DemoCamera c = director.Current;
            string shot = c == null ? "last view held" : String.Format("shot {0}: camera {1} ({2})", director.Shot + 1, c.Index, c.Entry.Type);
            return String.Format("Course intro: frame {0}/{1}, {2}{3}. Escape stops it.", director.Frame, IntroCameraDirector.IntroFrames, shot,
                director.Chain.Count != 3 ? String.Format(" (the chain has {0} cameras; the game plays shots of 232, 204 and 240 frames, a fourth one until the end)", director.Chain.Count) : "");
        }

        public void Render()
        {
            if (!Parent.opengl_initialized) return;
            Parent.SetStatusText(Status());
            DemoView v = DemoView.Lerp(director.PrevView, director.View, fraction);
            if (Parent.mode_3d)
            {
                Parent.RenderIntro3D(v, !director.KartsHidden);
                return;
            }
            Parent.BaseRender();
            if (Parent.OBJModel != null) Parent.OBJModel.Render();
            RenderThis(v);
            Parent.simpleOpenGlControl1.Refresh();
        }

        private void RenderThis(DemoView v)
        {
            DemoCameraDrawing.Draw(Parent.vph, Parent.Settings, Parent.Kayempee, v, Parent.DemoKarts());
        }
    }

    // The 2D drawing of a demo camera, for the course intro and the replay camera: its route (moved onto the kart for the
    // cameras whose route follows it), its view as a cone of its field of view, the camera, and for types 4 and 5 the
    // points its look-at moves between.
    public static class DemoCameraDrawing
    {
        public static void Draw(ViewPlaneHandler vph, VisualSettings Settings, SimpleKMP kmp, DemoView v, Func<int, KartView> karts)
        {
            Gl.glBindTexture(Gl.GL_TEXTURE_2D, 0);
            Gl.glDepthFunc(Gl.GL_ALWAYS);
            Gl.glLoadIdentity();
            Camera.CameraEntry entry = v.Camera != null ? v.Camera.Entry : null;
            Routes.RouteGroup route = entry != null && entry.RouteID >= 0 ? kmp.Routes.Entries.ElementAtOrDefault(entry.RouteID) : null;
            if (route != null && route.Entries.Count > 0)
            {
                // Follow path cameras ride their route around the kart's camera target, in course axes.
                Vector3 offset = new Vector3(0);
                if (entry.TypeID >= 6 && entry.TypeID <= 8) offset = karts(entry.TypeID == 7 ? 1 : entry.TypeID == 8 ? 2 : -1).Target;
                DrawRoute(vph, Settings, route, offset);
            }
            RenderView(vph, Settings, v.Position, v.LookAt, v.Fov);
            RenderCame(vph, Settings, v.Position);
            if (entry != null && (entry.TypeID == 4 || entry.TypeID == 5)) RenderVP(vph, Settings, entry, v.LookAt);
            Gl.glDepthFunc(Gl.GL_LEQUAL);
        }

        public static void DrawRoute(ViewPlaneHandler vph, VisualSettings Settings, Routes.RouteGroup route, Vector3 offset)
        {
            Gl.glLineWidth(Settings.LineWidth);
            Gl.glColor4f(Settings.RouteLinkColor.R / 255f, Settings.RouteLinkColor.G / 255f, Settings.RouteLinkColor.B / 255f, Settings.RouteLinkColor.A / 255f);
            Gl.glBegin(Gl.GL_LINE_STRIP);
            foreach (Vector3 p in CameraRail.Polyline(route)) vph.draw2DVertice(p + offset);
            Gl.glEnd();
            Gl.glColor4f(Settings.RouteColor.R / 255f, Settings.RouteColor.G / 255f, Settings.RouteColor.B / 255f, Settings.RouteColor.A / 255f);
            foreach (var p in route.Entries) vph.drawPoint(p.Pos + offset, Settings.PointSize * 0.6f);
        }

        private static void RenderCame(ViewPlaneHandler vph, VisualSettings Settings, Vector3 pos)
        {
            Gl.glColor4f(Settings.PointborderColor.R / 255f, Settings.PointborderColor.G / 255f, Settings.PointborderColor.B / 255f, Settings.PointborderColor.A);
            vph.drawPoint(pos, Settings.PointSize * (2f / 3f) + 2f, true);

            Gl.glColor4f(Settings.CameColor.R / 255f, Settings.CameColor.G / 255f, Settings.CameColor.B / 255f, Settings.CameColor.A);
            vph.drawPoint(pos, Settings.PointSize * (2f / 3f));
        }

        // The look-at points: viewpoint begin and end joined by a dotted line, and where the camera looks now.
        private static void RenderVP(ViewPlaneHandler vph, VisualSettings Settings, Camera.CameraEntry entry, Vector3 lookatpos)
        {
            Vector3 vp1 = entry.VPBegin;
            Vector3 vp2 = entry.VPEnd;
            Gl.glColor4f((Settings.HighlightPointborderColor.R / 255f), (Settings.HighlightPointborderColor.G / 255f), (Settings.HighlightPointborderColor.B / 255f), Settings.HighlightPointborderColor.A);
            Gl.glLineWidth(Settings.LineWidth / 2f);
            Gl.glPushAttrib(Gl.GL_ENABLE_BIT);
            Gl.glLineStipple(3, 0xAAAA);
            Gl.glEnable(Gl.GL_LINE_STIPPLE);
            Gl.glBegin(Gl.GL_LINES);
            vph.draw2DVertice(vp1);
            vph.draw2DVertice(vp2);
            Gl.glEnd();
            Gl.glPopAttrib();

            Gl.glPointSize(Settings.PointSize / 2);
            Gl.glBegin(Gl.GL_POINTS);
            vph.draw2DVertice(vp1);
            vph.draw2DVertice(vp2);
            Gl.glColor4f(Settings.HighlightPointColor.R / 255f, Settings.HighlightPointColor.G / 255f, Settings.HighlightPointColor.B / 255f, Settings.HighlightPointColor.A);
            vph.draw2DVertice(lookatpos);
            Gl.glEnd();
        }

        // The camera's view on the map: a cone of its field of view toward the look-at point. It reaches 50000 units,
        // the far clip of the game's cameras. (It used to reach 10^9 units, from the origin instead of the camera, which
        // the float precision of the drawing made shake.)
        private static void RenderView(ViewPlaneHandler vph, VisualSettings Settings, Vector3 pos, Vector3 lookatpos, float FOV)
        {
            double angle = Math.Atan2(vph.getViewCoord(lookatpos, 1) - vph.getViewCoord(pos, 1), vph.getViewCoord(lookatpos, 0) - vph.getViewCoord(pos, 0));
            double px = vph.getViewCoord(pos, 0), py = vph.getViewCoord(pos, 1), reach = 50000;
            double xcenter = px + Math.Cos(angle) * reach;
            double ycenter = py + Math.Sin(angle) * reach;
            double xside1 = px + Math.Cos(angle + RadianDegree.ToRadiansD((double)FOV / 2)) * reach;
            double yside1 = py + Math.Sin(angle + RadianDegree.ToRadiansD((double)FOV / 2)) * reach;
            double xside2 = px + Math.Cos(angle - RadianDegree.ToRadiansD((double)FOV / 2)) * reach;
            double yside2 = py + Math.Sin(angle - RadianDegree.ToRadiansD((double)FOV / 2)) * reach;
            Gl.glColor4f((Settings.HighlightPointColor.R / 255f), (Settings.HighlightPointColor.G / 255f), (Settings.HighlightPointColor.B / 255f), 0.25f);
            Gl.glBegin(Gl.GL_TRIANGLE_FAN);
            vph.draw2DVertice(pos);
            Gl.glVertex2d(xside1, yside1);
            Gl.glVertex2d(xside2, yside2);
            Gl.glEnd();
            Gl.glColor4f((Settings.CameColor.R / 255f), (Settings.CameColor.G / 255f), (Settings.CameColor.B / 255f), Settings.CameColor.A);
            Gl.glBegin(Gl.GL_LINE_LOOP);
            Gl.glVertex2d(xside1, yside1);
            vph.draw2DVertice(pos);
            Gl.glVertex2d(xside2, yside2);
            Gl.glEnd();
            Gl.glColor4f((Settings.HighlightPointborderColor.R / 255f), (Settings.HighlightPointborderColor.G / 255f), (Settings.HighlightPointborderColor.B / 255f), Settings.HighlightPointborderColor.A);
            Gl.glLineWidth(Settings.LineWidth / 2f);
            Gl.glPushAttrib(Gl.GL_ENABLE_BIT);
            Gl.glLineStipple(3, 0xAAAA);
            Gl.glEnable(Gl.GL_LINE_STIPPLE);
            Gl.glBegin(Gl.GL_LINES);
            vph.draw2DVertice(pos);
            Gl.glVertex2d(xcenter, ycenter);
            Gl.glEnd();
            Gl.glPopAttrib();
        }
    }
}
