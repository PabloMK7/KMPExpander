using Extensions;
using LibCTR.Collections;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using System.Windows.Input;
using Tao.OpenGl;

namespace KMPExpander.Class.SimpleKMPs
{
    public class CamePlayer
    {
        public Form1 Parent;
        // public readonly int INTERVAL = 16;
        public IntroCamePlayer incame;
        public bool stopCurr = false;
        public bool exitThread;
        public bool userStop;
        public CamePlayer(Form1 Parent) {
            this.Parent = Parent;
        }
        public Camera.CameraEntry GetStartingIntroCame()
        {
            for (int i = 0; i < Parent.Kayempee.Camera.Entries.Count; i++)
            {
                if (Parent.Kayempee.Camera.Entries[i].TypeID == 5) return Parent.Kayempee.Camera.Entries[i];
            }
            return null;
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
            exitThread = false;
            userStop = false;
            blockStuff();
            Thread thread = new Thread(new ThreadStart(playIntro));
            thread.Start();
            while (!exitThread && thread.IsAlive)
            {
                Application.DoEvents();
                if (Keyboard.IsKeyDown(Key.Escape))
                {
                    userStop = true;
                    incame.Stop();
                }
                if (incame != null)
                {
                    if (incame.needsRendering)
                    {
                        incame.Render();
                        incame.needsRendering = false;
                    }
                }
            }
            unblockStuff();
        }
        public void playIntro()
        {
            if (Parent.Kayempee == null) return;
            SimpleKMP kmp = Parent.Kayempee;
            Camera.CameraEntry curr = GetStartingIntroCame();
            if (curr == null) return;
            Camera.CameraEntry currtest = curr;
            int totcount = kmp.Camera.Entries.Count;
            int count = 0;
            while (true) //Check if cameras are linked properly
            {
                if (count >= 0) break;
                if (kmp.Routes.Entries.ElementAtOrDefault(currtest.RouteID) == null || kmp.Routes.Entries.ElementAtOrDefault(currtest.RouteID).Entries.Count == 0) return;
                currtest = kmp.Camera.Entries.ElementAtOrDefault(currtest.Next);
                if (currtest == null) break;
                else if (currtest.TypeID != 5) return;
                count++;
            }
            System.Diagnostics.Stopwatch s = new System.Diagnostics.Stopwatch();
            int currduration = (int)(curr.DurationRaw / 60 * 1000);
            incame = new IntroCamePlayer(Parent, kmp, curr);
            s.Start();
            while (!userStop)
            {
                System.Threading.Thread.Sleep(1);
                if (s.ElapsedMilliseconds >= currduration)
                {
                    if (userStop) break;
                    s.Reset();
                    incame.Stop();
                    curr = kmp.Camera.Entries.ElementAtOrDefault(curr.Next);
                    if (curr == null) break;
                    incame = new IntroCamePlayer(Parent, kmp, curr);
                    currduration = (int)(curr.DurationRaw / 60 * 1000);
                    s.Start();
                }
            }
            exitThread = true;
        }
        public class IntroCamePlayer
        {
            public Form1 Parent;
            public Vector3 pos;
            public Vector3 lookat;
            public Routes.RouteGroup route;
            public Camera.CameraEntry entry;
            public SimpleKMP kmp;
            public int currindex;
            public Vector3 currindexpos;
            public Vector3 nextindexpos;
            public Vector3 directionVector;
            public float betweenindexdis;
            public float currspd;
            public float nextspd;
            public bool stop;
            //
            public bool stopLookAt;
            public Vector3 lookatstart;
            public Vector3 lookatend;
            public Vector3 lookatdirvector;
            public Vector3 lookatpos;
            public float distbetweenviewport;
            public int lookattimes;
            public Vector3 lookatspeed;
            //
            public float FOV;
            public float FOVstart;
            public float FOVend;
            public float FOVincamount;
            public int FOVamountTimes;
            //
            public Vector3 posrend;
            public Vector3 lookatrend;
            public float FOVrend;
            //
            public bool needsRendering;
            public int running = 0;
            Thread UpdateThread;
            public bool runThread;

            public IntroCamePlayer(Form1 Parent, SimpleKMP kmp, Camera.CameraEntry entry)
            {
                this.Parent = Parent;
                this.entry = entry;
                this.kmp = kmp;
                needsRendering = false;
                route = kmp.Routes.Entries[entry.RouteID];
                currindex = 0;
                UpdateLookAtValues();
                UpdateFOVValues();
                try
                {
                    UpdateValues();
                } catch
                {
                    stop = true;
                    pos = routetopos(route.Entries[currindex]);
                }
                Start();
            }
            public Vector3 normaldirection(Vector3 vec1, Vector3 vec2)
            {
                return new Vector3(vec2.X - vec1.X, vec2.Y - vec1.Y, vec2.Z - vec1.Z).Normalize(); 
            }
            public Vector3 routetopos(Routes.RouteGroup.RouteEntry r)
            {
                return new Vector3(r.PositionX, r.PositionY, r.PositionZ);
            }
            public float distance(Vector3 pos1, Vector3 pos2)
            {
                return (float)Math.Sqrt(Math.Pow(pos1.X - pos2.X, 2) + Math.Pow(pos1.Y - pos2.Y, 2) + Math.Pow(pos1.Z - pos2.Z, 2));
            }
            public void Start()
            {
                runThread = true;
                UpdateThread = new Thread(new ThreadStart(Update));
                UpdateThread.Start();
            }
            public void Update()
            {
                System.Diagnostics.Stopwatch s = new System.Diagnostics.Stopwatch();
                s.Start();
                running = 1;
                while (runThread)
                {
                    if (s.ElapsedMilliseconds >= 16)
                    {
                        s.Reset();
                        s.Start();
                        UpdatePos();
                        UpdateLookAt();
                        UpdateFOV();
                        needsRendering = true;
                    }
                }
                running = 0;
            }
            public void Stop()
            {
                runThread = false;
                while (running != 0) Thread.Sleep(1);
            }
            public void UpdateValues()
            {
                currindexpos = pos = routetopos(route.Entries[currindex]);
                nextindexpos = routetopos(route.Entries[currindex + 1]);
                betweenindexdis = distance(currindexpos, nextindexpos);
                directionVector = normaldirection(currindexpos, nextindexpos);
                currspd = route.Entries[currindex].Speed;
                nextspd = route.Entries[currindex + 1].Speed;
            }
            public void UpdateLookAtValues()
            {
                lookatstart = lookatpos = new Vector3(entry.Viewpoint1X, entry.Viewpoint1Y, entry.Viewpoint1Z);
                lookatend = new Vector3(entry.Viewpoint2X, entry.Viewpoint2Y, entry.Viewpoint2Z);
                lookatdirvector = normaldirection(lookatstart, lookatend);
                distbetweenviewport = distance(lookatstart, lookatend);
                lookatspeed = entry.ViewpointSpeed * 0.01f * lookatdirvector;
                lookattimes = (int)(distbetweenviewport / lookatspeed.Length);
            }
            public void UpdateFOVValues()
            {
                FOVstart = FOV = entry.FOVBegin;
                FOVend = entry.FOVEnd;
                float FOVdist = FOVend - FOVstart;
                FOVincamount = entry.FOVSpeed * 0.01f;
                FOVamountTimes = (int)(FOVdist / FOVincamount);
            }
            public void UpdatePos()
            {
                if (!stop)
                {
                    float advancedRatio = distance(pos, currindexpos) / betweenindexdis;
                    if (advancedRatio >= 1)
                    {
                        currindex++;
                        if (route.Entries.ElementAtOrDefault(currindex + 1) == null)
                        {
                            stop = true;
                        } else
                        {
                            UpdateValues();
                        }
                    }
                    else
                    {
                        float newspeed = (currspd + ((nextspd - currspd) * advancedRatio)) / 93.75f;
                        pos += directionVector * newspeed;
                        if ((distance(pos, currindexpos) / betweenindexdis) >= 1) pos = nextindexpos;
                    }
                }
            }
            public void UpdateLookAt()
            {
                if (lookattimes > 0)
                {
                    lookatpos += lookatspeed;
                    lookattimes--;
                } else
                {
                    lookatpos = lookatend;
                }
            }
            public void UpdateFOV()
            {
                if (FOVamountTimes > 0)
                {
                    FOV += FOVincamount;
                    FOVamountTimes--;
                }
                else
                {
                    FOV = FOVend;
                }
            }
            public void RenderCame(VisualSettings Settings, Vector3 pos)
            {
                Gl.glPointSize(Settings.PointSize * (2f / 3f) + 2f);
                Gl.glBegin(Gl.GL_POINTS);
                Gl.glColor4f(Settings.PointborderColor.R / 255f, Settings.PointborderColor.G / 255f, Settings.PointborderColor.B / 255f, Settings.PointborderColor.A);
                Gl.glVertex2f(pos.X, pos.Z);
                Gl.glEnd();

                Gl.glPointSize(Settings.PointSize * (2f / 3f));
                Gl.glBegin(Gl.GL_POINTS);
                Gl.glColor4f(Settings.CameColor.R / 255f, Settings.CameColor.G / 255f, Settings.CameColor.B / 255f, Settings.CameColor.A);
                Gl.glVertex2f(pos.X, pos.Z);
                Gl.glEnd();
            }
            public void RenderVP(VisualSettings Settings, Vector3 pos, Vector3 lookatpos)
            {

                Gl.glColor4f((Settings.HighlightPointborderColor.R / 255f), (Settings.HighlightPointborderColor.G / 255f), (Settings.HighlightPointborderColor.B / 255f), Settings.HighlightPointborderColor.A);
                Gl.glLineWidth(Settings.LineWidth / 2f);
                Gl.glPushAttrib(Gl.GL_ENABLE_BIT);
                Gl.glLineStipple(3, 0xAAAA);
                Gl.glEnable(Gl.GL_LINE_STIPPLE);
                Gl.glBegin(Gl.GL_LINES);
                Gl.glVertex2d(entry.Viewpoint1X, entry.Viewpoint1Z);
                Gl.glVertex2d(entry.Viewpoint2X, entry.Viewpoint2Z);
                Gl.glEnd();
                Gl.glPopAttrib();

                Gl.glPointSize(Settings.PointSize / 2);
                Gl.glBegin(Gl.GL_POINTS);
                Gl.glVertex2d(entry.Viewpoint1X, entry.Viewpoint1Z);
                Gl.glVertex2d(entry.Viewpoint2X, entry.Viewpoint2Z);
                Gl.glColor4f(Settings.HighlightPointColor.R / 255f, Settings.HighlightPointColor.G / 255f, Settings.HighlightPointColor.B / 255f, Settings.HighlightPointColor.A);
                Gl.glVertex2d(lookatpos.X, lookatpos.Z);
                Gl.glEnd();
            }
            public void RenderView(VisualSettings Settings, Vector3 pos, Vector3 lookatpos, float FOV)
            {
                double angle = Math.Atan2(lookatpos.Z - pos.Z, lookatpos.X - pos.X);
                double xcenter = Math.Cos(angle) * 1000000000d;
                double ycenter = Math.Sin(angle) * 1000000000d;
                double xside1 = Math.Cos(angle + RadianDegree.ToRadiansD((double)FOV / 2)) * 1000000000d;
                double yside1 = Math.Sin(angle + RadianDegree.ToRadiansD((double)FOV / 2)) * 1000000000d;
                double xside2 = Math.Cos(angle - RadianDegree.ToRadiansD((double)FOV / 2)) * 1000000000d;
                double yside2 = Math.Sin(angle - RadianDegree.ToRadiansD((double)FOV / 2)) * 1000000000d;
                Gl.glColor4f((Settings.HighlightPointColor.R / 255f), (Settings.HighlightPointColor.G / 255f), (Settings.HighlightPointColor.B / 255f), 0.25f);
                Gl.glBegin(Gl.GL_TRIANGLE_FAN);
                Gl.glVertex2d(pos.X, pos.Z);
                Gl.glVertex2d(xside1, yside1);
                Gl.glVertex2d(xside2, yside2);
                Gl.glEnd();
                Gl.glColor4f((Settings.CameColor.R / 255f), (Settings.CameColor.G / 255f), (Settings.CameColor.B / 255f), Settings.CameColor.A);
                Gl.glBegin(Gl.GL_LINE_LOOP);
                Gl.glVertex2d(xside1, yside1);
                Gl.glVertex2d(pos.X, pos.Z);
                Gl.glVertex2d(xside2, yside2);
                Gl.glEnd();
                Gl.glColor4f((Settings.HighlightPointborderColor.R / 255f), (Settings.HighlightPointborderColor.G / 255f), (Settings.HighlightPointborderColor.B / 255f), Settings.HighlightPointborderColor.A);
                Gl.glLineWidth(Settings.LineWidth / 2f);
                Gl.glPushAttrib(Gl.GL_ENABLE_BIT);
                Gl.glLineStipple(3, 0xAAAA);
                Gl.glEnable(Gl.GL_LINE_STIPPLE);
                Gl.glBegin(Gl.GL_LINES);
                Gl.glVertex2d(pos.X, pos.Z);
                Gl.glVertex2d(xcenter, ycenter);
                Gl.glEnd();
                Gl.glPopAttrib();

            }
            public void RenderThis()
            {
                Gl.glBindTexture(Gl.GL_TEXTURE_2D, 0);
                Gl.glDepthFunc(Gl.GL_ALWAYS);
                Gl.glLoadIdentity();
                route.Render();
                VisualSettings Settings = (Application.OpenForms[0] as Form1).Settings;
                lookatrend.CopyFrom(lookatpos);
                posrend.CopyFrom(pos);
                FOVrend = FOV;
                RenderView(Settings, posrend, lookatrend, FOVrend);
                RenderCame(Settings, posrend);
                RenderVP(Settings, posrend, lookatrend);
            }
            public void Render() {
                if (!Parent.opengl_initialized) return;
                if (Parent.mode_3d)
                {
                    Parent.Base3DRender();
                    Gl.glPushMatrix();
                    if (Parent.OBJModel != null) Parent.OBJModel.Render();
                    Gl.glPopMatrix();
                }
                else
                {
                    Parent.BaseRender();
                    if (Parent.OBJModel != null) Parent.OBJModel.Render();
                    RenderThis();
                }
                Parent.simpleOpenGlControl1.Refresh();
            }
        }        
    }
}
