using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using CommonFiles;
using System.IO;
using KMPExpander.Class;
using LibCTR.Collections;
using Tao.OpenGl;

namespace KMPExpander
{
    public partial class TestFormOBJ : Form
    {
        private bool opengl_initialized = false;
        private float maximum_viewport = 16000;
        private float minimum_viewport = 450;
        private float viewport;
        public float Viewport
        {
            get { return viewport; }
            set
            {
                if (value > maximum_viewport) viewport = maximum_viewport;
                else if (value < minimum_viewport) viewport = minimum_viewport;
                else viewport = value;
            }
        }
        public Vector2 ViewportOffset;
        public SectionPicking.PickingInfo PickingInfo;


        OBJWrapper obj;

        public TestFormOBJ()
        {
            InitializeComponent();
        }

        private void loadOBKToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (openFileDialog1.ShowDialog()==DialogResult.OK)
            {
                obj = new OBJWrapper(openFileDialog1.FileName);
                Render();
            }
        }

        private void simpleOpenGlControlTest_Load(object sender, EventArgs e)
        {
            simpleOpenGlControlTest.MouseWheel += new MouseEventHandler(simpleOpenGlControlTest_MouseWheel);
            viewport = maximum_viewport;
            ViewportOffset = new Vector2(0, 0);
            simpleOpenGlControlTest.InitializeContexts();
            Gl.glEnable(Gl.GL_COLOR_MATERIAL);
            Gl.glEnable(Gl.GL_DEPTH_TEST);
            Gl.glDepthFunc(Gl.GL_ALWAYS);
            Gl.glEnable(Gl.GL_LOGIC_OP);
            Gl.glDisable(Gl.GL_CULL_FACE);
            Gl.glEnable(Gl.GL_TEXTURE_2D);
            Gl.glEnable(Gl.GL_LINE_SMOOTH);
            Gl.glEnable(Gl.GL_BLEND);
            Gl.glBlendFunc(Gl.GL_SRC_ALPHA, Gl.GL_ONE_MINUS_SRC_ALPHA);
            opengl_initialized = true;
            Render();
        }

        public void simpleOpenGlControlTest_MouseWheel(object sender, MouseEventArgs e)
        {
            float Diff = (maximum_viewport - minimum_viewport) / 15f;
            if (e.Delta >= 0) Viewport -= Diff;
            else Viewport += Diff;

            RectangleF disp = getDisplayRectangle(false);
            if (disp.Width < maximum_viewport * 2)
            {
                /*hScrollBar1.Maximum = (int)(maximum_viewport - disp.Width / 2f);
                hScrollBar1.Minimum = -(int)(maximum_viewport - disp.Width / 2f);
                hScrollBar1.Enabled = true;*/
            }
            else
            {
                /*hScrollBar1.Enabled = false;
                hScrollBar1.Value = hScrollBar1.Maximum = hScrollBar1.Minimum = 0;*/
                ViewportOffset = new Vector2(0, ViewportOffset.Z);
            }
            if (disp.Height < maximum_viewport * 2)
            {
                /*vScrollBar1.Maximum = (int)(maximum_viewport - disp.Height / 2f);
                vScrollBar1.Minimum = -(int)(maximum_viewport - disp.Height / 2f);
                vScrollBar1.Enabled = true;*/
            }
            else
            {
                /*vScrollBar1.Enabled = false;
                vScrollBar1.Value = vScrollBar1.Maximum = vScrollBar1.Minimum = 0;*/
                ViewportOffset = new Vector2(ViewportOffset.X, 0);
            }
            Render();
        }

        private void Render()
        {
            if (!opengl_initialized) return;
            BaseRender();

            if (obj != null) obj.Render();

            simpleOpenGlControlTest.Refresh();
        }

        private void BaseRender(bool picking = false)
        {
            Gl.glMatrixMode(Gl.GL_PROJECTION);
            //Gl.glMatrixMode(Gl.GL_PERSPECTIVE_CORRECTION_HINT);
            Gl.glLoadIdentity();
            Gl.glViewport(0, 0, simpleOpenGlControlTest.Width, simpleOpenGlControlTest.Height);
            RectangleF r = getDisplayRectangle(true);
            Gl.glOrtho(
                r.Left, r.Right,
                r.Bottom, r.Top,
                -8192, 8192);
            Gl.glMatrixMode(Gl.GL_MODELVIEW);
            Gl.glLoadIdentity();
            Gl.glClearColor(1f, 1f, 1f, 1f);
            Gl.glClear(Gl.GL_COLOR_BUFFER_BIT | Gl.GL_DEPTH_BUFFER_BIT);
            Gl.glColor4f(1, 1, 1, 1);
            Gl.glEnable(Gl.GL_TEXTURE_2D);
            Gl.glBindTexture(Gl.GL_TEXTURE_2D, 0);
            Gl.glColor4f(1, 1, 1, 1);
            Gl.glDisable(Gl.GL_CULL_FACE);
            Gl.glEnable(Gl.GL_ALPHA_TEST);
            Gl.glEnable(Gl.GL_DEPTH_TEST);
            Gl.glEnable(Gl.GL_BLEND);
            Gl.glEnable(Gl.GL_POINT_SMOOTH);
            Gl.glBlendFunc(Gl.GL_SRC_ALPHA, Gl.GL_ONE_MINUS_SRC_ALPHA);
            Gl.glAlphaFunc(Gl.GL_ALWAYS, 0f);
            Gl.glLoadIdentity();
        }

        private RectangleF getDisplayRectangle(bool AddViewportOffset)
        {
            float x = Viewport / simpleOpenGlControlTest.Width;
            x *= 2;
            float y = Viewport / simpleOpenGlControlTest.Height;
            y *= 2;
            float m = (x > y) ? x : y;
            if (AddViewportOffset)
                return new RectangleF(-(m * simpleOpenGlControlTest.Width) / 2f + ViewportOffset.X, -(m * simpleOpenGlControlTest.Height) / 2f + ViewportOffset.Z, (m * simpleOpenGlControlTest.Width), (m * simpleOpenGlControlTest.Height));
            else
                return new RectangleF(-(m * simpleOpenGlControlTest.Width) / 2f, -(m * simpleOpenGlControlTest.Height) / 2f, (m * simpleOpenGlControlTest.Width), (m * simpleOpenGlControlTest.Height));
        }

        private void TestFormOBJ_SizeChanged(object sender, EventArgs e)
        {
            Render();
        }
    }
}
