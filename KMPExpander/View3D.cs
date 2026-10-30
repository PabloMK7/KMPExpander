using Extensions;
using KMPExpander.Class;
using KMPExpander.Class.SimpleKMPs;
using LibCTR.Collections;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using Tao.OpenGl;

namespace KMPExpander
{
    // The 3D view: the KMP drawn as in the XZ map, over the model, seen by a free camera (WASD to move, Q and E down and
    // up, Shift faster, right or middle drag, or left drag on empty space, to look). Left drag on a point moves it on its
    // horizontal plane, the gizmo moves, turns or scales the selection (Move, Rotate and Scale buttons, or 1, 2 and 3),
    // Shift-drag on a move arrow moves along it and snaps onto the model's surface near it, and Shift-drag on a point
    // puts it where the mouse is on the model. The right button never picks.
    public partial class Form1
    {
        private const float near3D = 10f, far3D = 200000f;
        private float fov3D = 45f;                        // vertical, degrees; the camera kart's while in its camera
        private ToolStripButton kartCameraButton;
        private Vector3 cam3DPos = new Vector3(0, 5000, -5000);
        private float cam3DYaw = 0, cam3DPitch = -45;     // degrees; yaw 0 looks toward +Z
        private bool cam3DPlaced = false;
        private float cam3DSpeed = 3000;                  // units per second
        private HashSet<Keys> keys3D = new HashSet<Keys>();
        private System.Windows.Forms.Timer cam3DTimer;
        private Stopwatch cam3DClock = new Stopwatch();

        private enum Drag3D { None, Look, Select, Axis, Plane, Snap, Rotate, Trackball, ScaleAxis, ScaleAll }
        private Drag3D drag3D = Drag3D.None;
        private bool dragKeepsTree;                       // on release, only the grid is refreshed (it shows the entries)
        private Point mouse3DDown, mouse3DLast;
        private bool mouse3DMoved;
        private int gizmoActive = TransformGizmo.None;
        private bool gizmoLeft = true;                    // the side of a selected checkpoint that carries the gizmo
        private Vector3 dragAnchor, dragAxis, dragNormal, dragStartHit;
        private float dragStartS;
        private bool dragAxisSnap;                        // a Shift-drag on a move arrow: snaps to the surface on the axis
        private List<object> dragObjects = new List<object>();
        private List<Vector3[]> dragStarts = new List<Vector3[]>();   // per object; both sides for a checkpoint
        private TransformGizmo.Modes gizmoMode = TransformGizmo.Modes.Move;
        private ToolStripButton[] gizmoModeButtons;
        private List<Vector3> dragStartTransforms = new List<Vector3>();   // rotation or scale of each object
        private Vector3 dragLastVector;
        private float dragAngle, dragSize;
        private double[,] dragTrackball;                  // the rotation of a trackball drag so far, in world axes
        private Point dragTrackballLast;

        private void InitializeView3D()
        {
            toggle3DViewToolStripMenuItem.Enabled = true;
            toggle3DViewToolStripMenuItem.ToolTipText = "WASD to move, Q/E down/up, Shift faster, drag on empty space to look, F to go to the selection";
            gizmoModeButtons = new ToolStripButton[3];
            string[] names = { "Move", "Rotate", "Scale" };
            for (int i = 0; i < 3; i++)
            {
                TransformGizmo.Modes mode = (TransformGizmo.Modes)i;
                gizmoModeButtons[i] = new ToolStripButton(names[i]) { Checked = i == 0, ToolTipText = names[i] + " the selection with the gizmo (" + (i + 1) + ")" };
                gizmoModeButtons[i].Click += (s, e) => SetGizmoMode(mode);
                toolStrip1.Items.Insert(toolStrip1.Items.IndexOf(viewPlaneComboBox) + 1 + i, gizmoModeButtons[i]);
            }
            kartCameraButton = new ToolStripButton("Kart Camera") { CheckOnClick = true, Alignment = ToolStripItemAlignment.Right, ToolTipText = "Look through the race camera of the camera kart (the selected enabled kart, else the first enabled one) in the 3D view; Escape leaves it" };
            kartCameraButton.CheckedChanged += (s, e) =>
            {
                if (kartCameraButton.Checked && !mode_3d) viewPlaneComboBox.SelectedIndex = 3;
                if (!kartCameraButton.Checked) fov3D = 45f;
                Render();
            };
            // Next to the other kart buttons, Restart and Drive Karts.
            toolStrip1.Items.Insert(toolStrip1.Items.IndexOf(toolStripButtonRaceStart), kartCameraButton);
            cam3DTimer = new System.Windows.Forms.Timer();
            cam3DTimer.Interval = 16;
            cam3DTimer.Tick += cam3DTimer_Tick;
            simpleOpenGlControl1.KeyDown += simpleOpenGlControl1_KeyDown;
            simpleOpenGlControl1.KeyUp += (s, e) => keys3D.Remove(e.KeyCode);
            simpleOpenGlControl1.Leave += (s, e) => keys3D.Clear();
        }

        private void SetMode3D(bool on)
        {
            if (on == mode_3d) return;
            mode_3d = on;
            vph.Is3D = on;
            toggle3DViewToolStripMenuItem.Checked = on;
            if (!on) kartCameraButton.Checked = false;
            keys3D.Clear();
            cam3DTimer.Stop();
            drag3D = Drag3D.None;
            gizmoActive = TransformGizmo.None;
            if (on)
            {
                // The 3D view draws everything as in the XZ map.
                vph.mode = ViewPlaneHandler.PLANE_MODES.XZ;
                if (!cam3DPlaced) PlaceCamera3D();
                simpleOpenGlControl1.Focus();
            }
        }

        // Looking down at 45 degrees on the model, or on the KMP's points without one.
        private void PlaceCamera3D()
        {
            Vector3 min, max;
            if (OBJModel == null || !OBJModel.GetBounds(out min, out max))
            {
                min = new Vector3(float.MaxValue);
                max = new Vector3(float.MinValue);
                if (Kayempee != null)
                    foreach (var p in Kayempee.StartPositions.Entries.Select(e => e.Pos)
                        .Concat(Kayempee.EnemyRoutes.Entries.SelectMany(g => g.Entries.Select(e => e.Pos))))
                    {
                        min = new Vector3(Math.Min(min.X, p.X), Math.Min(min.Y, p.Y), Math.Min(min.Z, p.Z));
                        max = new Vector3(Math.Max(max.X, p.X), Math.Max(max.Y, p.Y), Math.Max(max.Z, p.Z));
                    }
                if (min.X > max.X) min = max = new Vector3(0);
            }
            Vector3 centre = (min + max) / 2;
            float size = Math.Max(2000, Math.Max(max.X - min.X, max.Z - min.Z));
            cam3DYaw = 0;
            cam3DPitch = -45;
            cam3DPos = centre - Camera3DForward() * (size * 0.7f);
            cam3DPlaced = true;
        }

        private Vector3 Camera3DForward()
        {
            double yaw = cam3DYaw * Math.PI / 180, pitch = cam3DPitch * Math.PI / 180;
            return new Vector3((float)(Math.Cos(pitch) * Math.Sin(yaw)), (float)Math.Sin(pitch), (float)(Math.Cos(pitch) * Math.Cos(yaw)));
        }

        private Vector3 Camera3DRight()
        {
            return Camera3DForward().Cross(new Vector3(0, 1, 0)).Normalize();
        }

        public void Base3DRender(bool picking = false)
        {
            Base3DRender(picking, cam3DPos, cam3DPos + Camera3DForward(), fov3D);
        }

        // The camera goes into the projection matrix, as the drawing code resets the modelview matrix all the time.
        public void Base3DRender(bool picking, Vector3 eye, Vector3 target, float fovy)
        {
            Gl.glMatrixMode(Gl.GL_PROJECTION);
            Gl.glLoadIdentity();
            Gl.glViewport(0, 0, simpleOpenGlControl1.Width, simpleOpenGlControl1.Height);
            Glu.gluPerspective(fovy, (double)simpleOpenGlControl1.Width / Math.Max(1, simpleOpenGlControl1.Height), near3D, far3D);
            Glu.gluLookAt(eye.X, eye.Y, eye.Z, target.X, target.Y, target.Z, 0, 1, 0);
            // What is drawn comes as (X, Z, Y), the layout of the XZ map (see ViewPlaneHandler.Is3D).
            Gl.glMultMatrixf(new float[] { 1, 0, 0, 0, 0, 0, 1, 0, 0, 1, 0, 0, 0, 0, 0, 1 });
            Gl.glMatrixMode(Gl.GL_MODELVIEW);
            Gl.glLoadIdentity();

            if (picking) Gl.glClearColor(1f, 1f, 1f, 1f);
            else Gl.glClearColor(Settings.BackgroundColor.R / 255f, Settings.BackgroundColor.G / 255f, Settings.BackgroundColor.B / 255f, Settings.BackgroundColor.A);
            Gl.glClear(Gl.GL_COLOR_BUFFER_BIT | Gl.GL_DEPTH_BUFFER_BIT);

            Gl.glColor4f(1, 1, 1, 1);
            Gl.glEnable(Gl.GL_TEXTURE_2D);
            Gl.glBindTexture(Gl.GL_TEXTURE_2D, 0);
            Gl.glDisable(Gl.GL_CULL_FACE);
            Gl.glEnable(Gl.GL_ALPHA_TEST);
            Gl.glEnable(Gl.GL_DEPTH_TEST);
            Gl.glDepthFunc(Gl.GL_LEQUAL);
            Gl.glEnable(Gl.GL_BLEND);
            Gl.glEnable(Gl.GL_POINT_SMOOTH);
            Gl.glBlendFunc(Gl.GL_SRC_ALPHA, Gl.GL_ONE_MINUS_SRC_ALPHA);
            Gl.glAlphaFunc(Gl.GL_ALWAYS, 0f);

            // What vph.drawPoint needs for its spheres, and their light: from above (world (0.3, 1, 0.5), given in the
            // (X, Z, Y) layout), half ambient so that their lower side keeps its colour.
            vph.Picking = picking;
            vph.CameraPos = eye;
            vph.CameraDir = (target - eye).Normalize();
            vph.PixelScale = 2 * (float)Math.Tan(fovy * Math.PI / 360) / Math.Max(1, simpleOpenGlControl1.Height);
            vph.PointRadius = Settings.PointRadius3D;
            vph.BasePointSize = Settings.PointSize;
            Gl.glEnable(Gl.GL_NORMALIZE);
            Gl.glColorMaterial(Gl.GL_FRONT_AND_BACK, Gl.GL_AMBIENT_AND_DIFFUSE);
            Gl.glLightModelfv(Gl.GL_LIGHT_MODEL_AMBIENT, new float[] { 0.5f, 0.5f, 0.5f, 1f });
            Gl.glLightfv(Gl.GL_LIGHT0, Gl.GL_POSITION, new float[] { 0.3f, 0.5f, 1f, 0f });
            Gl.glLightfv(Gl.GL_LIGHT0, Gl.GL_AMBIENT, new float[] { 0f, 0f, 0f, 1f });
            Gl.glLightfv(Gl.GL_LIGHT0, Gl.GL_DIFFUSE, new float[] { 0.6f, 0.6f, 0.6f, 1f });
            Gl.glLightfv(Gl.GL_LIGHT0, Gl.GL_SPECULAR, new float[] { 0f, 0f, 0f, 1f });
            Gl.glEnable(Gl.GL_LIGHT0);
        }

        // World units per pixel at pos: the zoom of the map, or in 3D the size of a pixel at pos's distance.
        public float GetPixelSize(Vector3 pos)
        {
            if (!mode_3d) return getViewportLevel();
            float depth = Math.Max(near3D, (pos - cam3DPos).Dot(Camera3DForward()));
            return depth * 2 * (float)Math.Tan(fov3D * Math.PI / 360) / Math.Max(1, simpleOpenGlControl1.Height);
        }

        // The axes of the view, in world axes: screen right, screen up and toward the viewer. The 2D views draw their
        // vertical coordinate downward (draw2DVertice).
        private void ViewAxes(out Vector3 right, out Vector3 up, out Vector3 toViewer)
        {
            if (mode_3d)
            {
                Vector3 f = Camera3DForward();
                right = Camera3DRight();
                up = right.Cross(f);
                toViewer = f * -1;
                return;
            }
            switch (vph.mode)
            {
                case ViewPlaneHandler.PLANE_MODES.XY: right = new Vector3(1, 0, 0); up = new Vector3(0, 1, 0); break;
                case ViewPlaneHandler.PLANE_MODES.ZY: right = new Vector3(0, 0, 1); up = new Vector3(0, 1, 0); break;
                default: right = new Vector3(1, 0, 0); up = new Vector3(0, 0, -1); break;
            }
            toViewer = right.Cross(up);
        }

        // The ray under the mouse; in a 2D view it goes straight into the view plane from far in front of it.
        private void MouseRay(Point mouse, out Vector3 origin, out Vector3 dir)
        {
            if (!mode_3d)
            {
                Vector3 view = GetPosition(mouse), right, up, toViewer;
                ViewAxes(out right, out up, out toViewer);
                origin = right * view.X - up * view.Z + toViewer * far3D;
                dir = toViewer * -1;
                return;
            }
            float w = Math.Max(1, simpleOpenGlControl1.Width), h = Math.Max(1, simpleOpenGlControl1.Height);
            float tan = (float)Math.Tan(fov3D * Math.PI / 360);
            float x = (2 * mouse.X / w - 1) * tan * w / h, y = (1 - 2 * mouse.Y / h) * tan;
            Vector3 f = Camera3DForward(), r = Camera3DRight(), u = r.Cross(f);
            origin = cam3DPos;
            dir = (f + r * x + u * y).Normalize();
        }

        // The nearest point of the shown collision or model under the mouse.
        private Vector3? SurfaceHit(Point mouse)
        {
            Vector3 o, d;
            MouseRay(mouse, out o, out d);
            float? t = Collision != null ? Collision.RayCast(o, d) : null;
            float? m = OBJModel != null ? OBJModel.RayCast(o, d) : null;
            if (m != null && (t == null || m.Value < t.Value)) t = m;
            if (t == null) return null;
            return o + d * t.Value;
        }

        // With the kart camera on, the free camera is put where the camera kart's race camera is, with its field of view
        // (the free camera's controls are overridden on the next frame).
        private void FollowKartCamera()
        {
            if (!kartCameraButton.Checked || Kayempee == null) return;
            KartSimulator.KartEntry kart = Kayempee.KartSimulator.CameraKart;
            if (kart == null) return;
            Vector3 d = kart.GetCameraDirection().Normalize();
            cam3DPos = kart.GetCameraPosition();
            cam3DYaw = (float)(Math.Atan2(d.X, d.Z) * 180 / Math.PI);
            cam3DPitch = (float)(Math.Asin(Math.Max(-1, Math.Min(1, d.Y))) * 180 / Math.PI);
            fov3D = (float)(2 * Math.Atan(kart.ViewHalfHeight) * 180 / Math.PI);
        }

        // The model, the object models and the collision. Polygon offset pushes them back a little, so that what the KMP
        // draws on the ground is not hidden by it. With depthOnly (the picking pass), only into the depth buffer, so that
        // the points behind them cannot be picked.
        private void RenderGeometry3D(bool depthOnly)
        {
            if (depthOnly) Gl.glColorMask(Gl.GL_FALSE, Gl.GL_FALSE, Gl.GL_FALSE, Gl.GL_FALSE);
            Gl.glEnable(Gl.GL_POLYGON_OFFSET_FILL);
            Gl.glPolygonOffset(1, 4);
            if (OBJModel != null) OBJModel.Render();
            RenderObjectModels();
            if (Collision != null) Collision.Render();
            Gl.glDisable(Gl.GL_POLYGON_OFFSET_FILL);
            if (depthOnly) Gl.glColorMask(Gl.GL_TRUE, Gl.GL_TRUE, Gl.GL_TRUE, Gl.GL_TRUE);
        }

        private void Render3D()
        {
            FollowKartCamera();
            Base3DRender();
            checkCullingRoute();
            UpdateCulling();
            RenderGeometry3D(false);
            // Without "Hide points behind the model" the KMP is drawn over the model, as in the map; its spheres and
            // volumes always hide each other.
            if (!occludeMenuItem.Checked) Gl.glClear(Gl.GL_DEPTH_BUFFER_BIT);
            if (Kayempee != null) Kayempee.Render();
            RenderGizmo(false);
        }

        private void SetGizmoMode(TransformGizmo.Modes mode)
        {
            gizmoMode = mode;
            for (int i = 0; i < 3; i++) gizmoModeButtons[i].Checked = (int)mode == i;
            Render();
        }

        private static PropertyInfo FloatProperty(object o, string name)
        {
            PropertyInfo p = o == null ? null : o.GetType().GetProperty(name);
            return p != null && p.PropertyType == typeof(float) ? p : null;
        }

        private static Vector3 GetVector(object o, string prefix)
        {
            return new Vector3((float)FloatProperty(o, prefix + "X").GetValue(o, null), (float)FloatProperty(o, prefix + "Y").GetValue(o, null), (float)FloatProperty(o, prefix + "Z").GetValue(o, null));
        }

        private static void SetVector(object o, string prefix, Vector3 v)
        {
            FloatProperty(o, prefix + "X").SetValue(o, v.X, null);
            FloatProperty(o, prefix + "Y").SetValue(o, v.Y, null);
            FloatProperty(o, prefix + "Z").SetValue(o, v.Z, null);
        }

        // The gizmo's axes for the first selected entry in this mode, null when the mode does not apply to it: entries
        // with RotationX/Y/Z turn, those with ScaleX/Y/Z scale per axis, enemy, item and glider points (one Scale) only
        // with the centre box.
        private int[] GizmoAxes()
        {
            int[] axes = GizmoAxes3D();
            if (axes == null || mode_3d) return axes;
            // In a 2D view only what lies in the view plane: the arrows along it, and the ring around the axis across it.
            Vector3 right, up, toViewer;
            ViewAxes(out right, out up, out toViewer);
            bool rotate = gizmoMode == TransformGizmo.Modes.Rotate;
            return axes.Where(a => (Math.Abs(TransformGizmo.Axis(a).Dot(toViewer)) > 0.5f) == rotate).ToArray();
        }

        private int[] GizmoAxes3D()
        {
            object first = SelectedDots.Count > 0 ? SelectedDots[0] : null;
            int[] all = { TransformGizmo.X, TransformGizmo.Y, TransformGizmo.Z };
            switch (gizmoMode)
            {
                case TransformGizmo.Modes.Rotate:
                    return FloatProperty(first, "RotationY") != null ? all : null;
                case TransformGizmo.Modes.Scale:
                    if (FloatProperty(first, "ScaleX") != null) return all;
                    return FloatProperty(first, "Scale") != null ? new int[0] : null;
                default:
                    return SelectedDots.All(d => d is CheckPoints.CheckpointGroup.CheckpointEntry) ? new int[] { TransformGizmo.X, TransformGizmo.Z } : all;
            }
        }

        private Vector3? GizmoPosition()
        {
            if (Kayempee == null || SelectedDots.Count == 0) return null;
            return Kayempee.GetAnyPointPosition(SelectedDots[0], gizmoLeft);
        }

        private void RenderGizmo(bool picking)
        {
            Vector3? pos = GizmoPosition();
            if (pos == null) return;
            int[] axes = GizmoAxes();
            if (axes == null) return;
            if (!mode_3d)
            {
                // The rotate gizmo shows the side of its rings toward the viewer.
                Vector3 right, up, toViewer;
                ViewAxes(out right, out up, out toViewer);
                vph.CameraPos = pos.Value + toViewer * far3D;
            }
            TransformGizmo.Render(gizmoMode, pos.Value, 100 * GetPixelSize(pos.Value), gizmoActive, axes, picking);
        }

        private void MouseDown3D(MouseEventArgs e)
        {
            simpleOpenGlControl1.Focus();
            mouse3DDown = mouse3DLast = e.Location;
            mouse3DMoved = false;
            drag3D = Drag3D.None;
            // The middle and right buttons only turn the camera; Shift with the left button snaps to the surface.
            if (e.Button == MouseButtons.Middle || e.Button == MouseButtons.Right || Kayempee == null)
            {
                drag3D = Drag3D.Look;
                return;
            }
            if (e.Button != MouseButtons.Left) return;
            bool snap = (ModifierKeys & Keys.Shift) != 0;

            if (pencil_mode && lastSelectedGroup != null && !(lastSelectedGroup is KartSimulator))
            {
                AddPoint3D(e.Location);
                return;
            }

            SectionPicking.PickingInfo previous = PickingInfo;
            PickPoint(e.Location);
            if (PickingInfo.Section == Sections.Gizmo)
            {
                int handle = PickingInfo.EntryID;
                PickingInfo = previous;
                StartDrag(handle, true, e.Location);
                if (snap && gizmoMode == TransformGizmo.Modes.Move && TransformGizmo.IsAxis(handle)) dragAxisSnap = true;
                return;
            }
            if (PickingInfo.Section == Sections.None || PickingInfo.Section == Sections.LocalMap || PickingInfo.Section == Sections.GlobalMap)
            {
                drag3D = Drag3D.Look;
                return;
            }

            start_move = true;
            drag3D = Drag3D.Select;
            dragKeepsTree = false;
            SelectedDots.Clear();
            SelectedDots.Add(Kayempee.GetPoint(PickingInfo));
            gizmoLeft = PickingInfo.PointID != PointID.Right;
            if (snap) StartSnap();
            else StartDrag(TransformGizmo.XZ, false, e.Location);
            Render();
        }

        // Pencil mode: the new point goes onto the model under the mouse (or onto Y = 0, or in front of the camera). A
        // new checkpoint's right side then follows the mouse until the button is released.
        private void AddPoint3D(Point mouse)
        {
            Vector3 o, d;
            MouseRay(mouse, out o, out d);
            Vector3? hit = SurfaceHit(mouse) ?? Ray3D.Plane(o, d, new Vector3(0), new Vector3(0, 1, 0));
            Vector3 position = hit != null && (hit.Value - o).Length < far3D ? hit.Value : o + d * 2000;

            AddingPencil = AddEntry();
            bool checkpoint = lastSelectedGroup is CheckPoints.CheckpointGroup;
            if (checkpoint) Kayempee.MoveAnyPoint(AddingPencil, position, true);
            Kayempee.MoveAnyPoint(AddingPencil, position);
            dataGridView1.DataSource = lastSelectedGroup.GetEntries();
            dataGridView1.ClearSelection();
            foreach (DataGridViewRow row in dataGridView1.Rows)
                if (AddingPencil == row.DataBoundItem)
                {
                    row.Selected = true;
                    dataGridView1.FirstDisplayedScrollingRowIndex = row.Index;
                    break;
                }
            if (checkpoint)
            {
                adding_checkpoint = true;
                start_move = true;
                SelectedDots.Clear();
                SelectedDots.Add(AddingPencil);
                gizmoLeft = false;
                StartSnap();
                dragKeepsTree = true;
                mouse3DMoved = true;
            }
            AddingPencil = null;
            Render();
        }

        private void StoreDragStarts()
        {
            dragObjects.Clear();
            dragStarts.Clear();
            foreach (object o in SelectedDots)
            {
                Vector3? left = Kayempee.GetAnyPointPosition(o, true);
                if (left == null) continue;
                dragObjects.Add(o);
                dragStarts.Add(new Vector3[] { left.Value, Kayempee.GetAnyPointPosition(o, false).Value });
            }
        }

        private void StartDrag(int handle, bool fromGizmo, Point mouse)
        {
            Vector3? anchor = GizmoPosition();
            if (anchor == null) return;
            dragAnchor = anchor.Value;
            dragAxisSnap = false;
            StoreDragStarts();
            Vector3 o, d;
            MouseRay(mouse, out o, out d);
            if (fromGizmo && gizmoMode != TransformGizmo.Modes.Move)
            {
                if (!StartTransformDrag(handle, o, d)) return;
            }
            else if (TransformGizmo.IsAxis(handle))
            {
                dragAxis = TransformGizmo.Axis(handle);
                float? s = Ray3D.Axis(o, d, dragAnchor, dragAxis);
                if (s == null) return;
                dragStartS = s.Value;
                drag3D = Drag3D.Axis;
            }
            else
            {
                dragNormal = TransformGizmo.PlaneNormal(handle);
                Vector3? hit = Ray3D.Plane(o, d, dragAnchor, dragNormal);
                if (hit == null) return;
                dragStartHit = hit.Value;
                drag3D = Drag3D.Plane;
            }
            start_move = true;
            dragKeepsTree = fromGizmo;
            gizmoActive = handle;
        }

        // Rotate: the angle around the ring's axis, from where the ray meets the ring's plane. Scale: along the arrow, by
        // the gizmo's length per doubling, or for the centre box by the mouse moving right or up.
        private bool StartTransformDrag(int handle, Vector3 o, Vector3 d)
        {
            bool rotate = gizmoMode == TransformGizmo.Modes.Rotate;
            dragStartTransforms.Clear();
            foreach (object obj in dragObjects)
            {
                if (rotate) dragStartTransforms.Add(FloatProperty(obj, "RotationY") != null ? GetVector(obj, "Rotation") : new Vector3(0));
                else if (FloatProperty(obj, "ScaleX") != null) dragStartTransforms.Add(GetVector(obj, "Scale"));
                else dragStartTransforms.Add(new Vector3(FloatProperty(obj, "Scale") != null ? (float)FloatProperty(obj, "Scale").GetValue(obj, null) : 1));
            }
            dragAxis = TransformGizmo.Axis(handle);
            dragSize = 100 * GetPixelSize(dragAnchor);
            if (rotate && handle == TransformGizmo.Ball)
            {
                dragTrackball = new double[,] { { 1, 0, 0 }, { 0, 1, 0 }, { 0, 0, 1 } };
                dragTrackballLast = mouse3DDown;
                drag3D = Drag3D.Trackball;
            }
            else if (rotate)
            {
                Vector3? hit = Ray3D.Plane(o, d, dragAnchor, dragAxis);
                if (hit == null) return false;
                dragLastVector = hit.Value - dragAnchor;
                dragAngle = 0;
                drag3D = Drag3D.Rotate;
            }
            else if (TransformGizmo.IsAxis(handle))
            {
                float? s = Ray3D.Axis(o, d, dragAnchor, dragAxis);
                if (s == null) return false;
                dragStartS = s.Value;
                drag3D = Drag3D.ScaleAxis;
            }
            else drag3D = Drag3D.ScaleAll;
            return true;
        }

        // Shift turns in steps of 15 degrees and scales in steps of 0.1.
        private void TransformDragged(int handle, float angle, float factor)
        {
            bool snap = (ModifierKeys & Keys.Shift) != 0;
            for (int i = 0; i < dragObjects.Count; i++)
            {
                object obj = dragObjects[i];
                Vector3 start = dragStartTransforms[i];
                if (drag3D == Drag3D.Rotate)
                {
                    if (FloatProperty(obj, "RotationY") == null) continue;
                    float[] r = { start.X, start.Y, start.Z };
                    float value = r[handle - 1] + angle;
                    if (snap) value = (float)Math.Round(value / 15) * 15;
                    r[handle - 1] = (float)(((value + 180) % 360 + 360) % 360 - 180);
                    SetVector(obj, "Rotation", new Vector3(r[0], r[1], r[2]));
                }
                else
                {
                    Func<float, float> scaled = v => snap ? Math.Max(0.1f, (float)Math.Round(v * factor * 10) / 10) : v * factor;
                    if (FloatProperty(obj, "ScaleX") != null)
                    {
                        float[] sc = { start.X, start.Y, start.Z };
                        for (int k = 0; k < 3; k++)
                            if (drag3D == Drag3D.ScaleAll || k == handle - 1) sc[k] = scaled(sc[k]);
                        SetVector(obj, "Scale", new Vector3(sc[0], sc[1], sc[2]));
                    }
                    else if (FloatProperty(obj, "Scale") != null) FloatProperty(obj, "Scale").SetValue(obj, scaled(start.X), null);
                }
            }
        }

        private void StartSnap()
        {
            Vector3? anchor = GizmoPosition();
            if (anchor == null) return;
            dragAnchor = anchor.Value;
            StoreDragStarts();
            drag3D = Drag3D.Snap;
        }

        // Moves what is dragged by delta from where it was: one side of a single checkpoint, both sides when several
        // entries are selected.
        private void MoveDragged(Vector3 delta)
        {
            for (int i = 0; i < dragObjects.Count; i++)
            {
                if (dragObjects[i] is CheckPoints.CheckpointGroup.CheckpointEntry)
                {
                    if (dragObjects.Count > 1 || gizmoLeft) Kayempee.MoveAnyPoint(dragObjects[i], dragStarts[i][0] + delta, true);
                    if (dragObjects.Count > 1 || !gizmoLeft) Kayempee.MoveAnyPoint(dragObjects[i], dragStarts[i][1] + delta, false);
                }
                else Kayempee.MoveAnyPoint(dragObjects[i], dragStarts[i][0] + delta);
            }
        }

        private void DragTo(Point mouse)
        {
            Vector3 o, d;
            MouseRay(mouse, out o, out d);
            switch (drag3D)
            {
                case Drag3D.Axis:
                    float? s = Ray3D.Axis(o, d, dragAnchor, dragAxis);
                    if (s == null) break;
                    Vector3 to = dragAnchor + dragAxis * (s.Value - dragStartS);
                    if (dragAxisSnap) to = SnapAlongAxis(to, dragAxis);
                    MoveDragged(to - dragAnchor);
                    break;
                case Drag3D.Plane:
                    Vector3? hit = Ray3D.Plane(o, d, dragAnchor, dragNormal);
                    if (hit != null && (hit.Value - o).Length < far3D) MoveDragged(hit.Value - dragStartHit);
                    break;
                case Drag3D.Rotate:
                    Vector3? ring = Ray3D.Plane(o, d, dragAnchor, dragAxis);
                    if (ring == null) break;
                    Vector3 now = ring.Value - dragAnchor;
                    dragAngle += (float)(Math.Atan2(dragAxis.Dot(dragLastVector.Cross(now)), dragLastVector.Dot(now)) * 180 / Math.PI);
                    dragLastVector = now;
                    TransformDragged(gizmoActive, dragAngle, 1);
                    break;
                case Drag3D.Trackball:
                    // The ball turns as if dragged by the mouse: around the screen axis across the motion, half a degree
                    // per pixel.
                    int tx = mouse.X - dragTrackballLast.X, ty = mouse.Y - dragTrackballLast.Y;
                    dragTrackballLast = mouse;
                    if (tx == 0 && ty == 0) break;
                    Vector3 r, u, toViewer;
                    ViewAxes(out r, out u, out toViewer);
                    Vector3 axis = (u * tx + r * ty).Normalize();
                    dragTrackball = Multiply(AxisRotation(axis, Math.Sqrt(tx * tx + ty * ty) * 0.5 * Math.PI / 180), dragTrackball);
                    for (int i = 0; i < dragObjects.Count; i++)
                        if (FloatProperty(dragObjects[i], "RotationY") != null)
                            SetVector(dragObjects[i], "Rotation", MatrixToEuler(Multiply(dragTrackball, EulerToMatrix(dragStartTransforms[i]))));
                    break;
                case Drag3D.ScaleAxis:
                    float? along = Ray3D.Axis(o, d, dragAnchor, dragAxis);
                    if (along != null) TransformDragged(gizmoActive, 0, (float)Math.Pow(2, (along.Value - dragStartS) / dragSize));
                    break;
                case Drag3D.ScaleAll:
                    TransformDragged(gizmoActive, 0, (float)Math.Pow(2, ((mouse.X - mouse3DDown.X) - (mouse.Y - mouse3DDown.Y)) / 100.0));
                    break;
                case Drag3D.Snap:
                    // Off the model, the point stays on its horizontal plane (in a 2D view, on the plane of the view).
                    Vector3 snapNormal = new Vector3(0, 1, 0);
                    if (!mode_3d)
                    {
                        Vector3 sr, su;
                        ViewAxes(out sr, out su, out snapNormal);
                    }
                    Vector3? surface = SurfaceHit(mouse) ?? Ray3D.Plane(o, d, dragAnchor, snapNormal);
                    if (surface != null && (!mode_3d || (surface.Value - o).Length < far3D)) MoveDragged(surface.Value - dragAnchor);
                    break;
            }
        }

        // Rotation matrices for the trackball, in the order of the KMP's rotations (degrees, X, then Y, then Z, as
        // ViewPlaneHandler.modelMatrix): R = Rz Ry Rx.
        private static double[,] EulerToMatrix(Vector3 rotation)
        {
            double rx = rotation.X * Math.PI / 180, ry = rotation.Y * Math.PI / 180, rz = rotation.Z * Math.PI / 180;
            double cx = Math.Cos(rx), sx = Math.Sin(rx), cy = Math.Cos(ry), sy = Math.Sin(ry), cz = Math.Cos(rz), sz = Math.Sin(rz);
            return new double[,] {
                { cy * cz, sx * sy * cz - cx * sz, cx * sy * cz + sx * sz },
                { cy * sz, sx * sy * sz + cx * cz, cx * sy * sz - sx * cz },
                { -sy, sx * cy, cx * cy } };
        }

        private static Vector3 MatrixToEuler(double[,] m)
        {
            double y = Math.Asin(Math.Max(-1, Math.Min(1, -m[2, 0]))), x, z;
            if (Math.Abs(Math.Cos(y)) > 1e-6)
            {
                x = Math.Atan2(m[2, 1], m[2, 2]);
                z = Math.Atan2(m[1, 0], m[0, 0]);
            }
            else
            {
                x = Math.Atan2(-m[1, 2], m[1, 1]);
                z = 0;
            }
            return new Vector3((float)(x * 180 / Math.PI), (float)(y * 180 / Math.PI), (float)(z * 180 / Math.PI));
        }

        private static double[,] AxisRotation(Vector3 a, double angle)
        {
            double c = Math.Cos(angle), s = Math.Sin(angle), t = 1 - c;
            return new double[,] {
                { t * a.X * a.X + c, t * a.X * a.Y - s * a.Z, t * a.X * a.Z + s * a.Y },
                { t * a.X * a.Y + s * a.Z, t * a.Y * a.Y + c, t * a.Y * a.Z - s * a.X },
                { t * a.X * a.Z - s * a.Y, t * a.Y * a.Z + s * a.X, t * a.Z * a.Z + c } };
        }

        private static double[,] Multiply(double[,] a, double[,] b)
        {
            double[,] m = new double[3, 3];
            for (int i = 0; i < 3; i++)
                for (int j = 0; j < 3; j++)
                    for (int k = 0; k < 3; k++) m[i, j] += a[i, k] * b[k, j];
            return m;
        }

        // The shown solid collision or model along the axis line from pos, when it is within 20 pixels; else pos.
        private Vector3 SnapAlongAxis(Vector3 pos, Vector3 axis)
        {
            float best = 20 * GetPixelSize(pos);
            Vector3 result = pos;
            foreach (Vector3 dir in new Vector3[] { axis, axis * -1 })
            {
                float? t = Collision != null ? Collision.RayCast(pos, dir, true) : null;
                float? m = OBJModel != null ? OBJModel.RayCast(pos, dir) : null;
                if (m != null && (t == null || m.Value < t.Value)) t = m;
                if (t != null && t.Value < best)
                {
                    best = t.Value;
                    result = pos + dir * t.Value;
                }
            }
            return result;
        }

        private void MouseMove3D(MouseEventArgs e)
        {
            int dx = e.X - mouse3DLast.X, dy = e.Y - mouse3DLast.Y;
            mouse3DLast = e.Location;
            if (Math.Abs(e.X - mouse3DDown.X) > 2 || Math.Abs(e.Y - mouse3DDown.Y) > 2) mouse3DMoved = true;
            if (drag3D == Drag3D.None || !mouse3DMoved) return;
            if (drag3D == Drag3D.Look)
            {
                cam3DYaw -= dx * 0.25f;
                cam3DPitch = Math.Max(-89, Math.Min(89, cam3DPitch - dy * 0.25f));
                // The timer's ticks only come when no other message waits, so while the mouse moves the keys move the
                // camera from here.
                MoveCamera3D();
            }
            else DragTo(e.Location);
            Render();
        }

        private void MouseUp3D(MouseEventArgs e)
        {
            Drag3D ended = drag3D;
            drag3D = Drag3D.None;
            gizmoActive = TransformGizmo.None;
            if (ended == Drag3D.Look && !mouse3DMoved && e.Button == MouseButtons.Left)
            {
                SelectedDots.Clear();
                dataGridView1.ClearSelection();
            }
            if (ended == Drag3D.Look || ended == Drag3D.None)
            {
                Render();
                return;
            }
            adding_checkpoint = false;
            if (dragKeepsTree) dataGridView1.Refresh();
            else ExpandTreeView();
            start_move = false;
            Render();
        }

        private void simpleOpenGlControl1_KeyDown(object sender, KeyEventArgs e)
        {
            if (!mode_3d)
            {
                // The gizmo modes work in the 2D views too.
                if (e.KeyCode >= Keys.D1 && e.KeyCode <= Keys.D3)
                {
                    SetGizmoMode((TransformGizmo.Modes)(e.KeyCode - Keys.D1));
                    e.Handled = true;
                }
                return;
            }
            switch (e.KeyCode)
            {
                case Keys.W: case Keys.A: case Keys.S: case Keys.D: case Keys.Q: case Keys.E:
                    if (keys3D.Count == 0)
                    {
                        cam3DClock.Restart();
                        cam3DTimer.Start();
                    }
                    keys3D.Add(e.KeyCode);
                    e.Handled = true;
                    break;
                case Keys.Escape:
                    if (kartCameraButton.Checked) kartCameraButton.Checked = false;
                    e.Handled = true;
                    break;
                case Keys.D1: case Keys.D2: case Keys.D3:
                    SetGizmoMode((TransformGizmo.Modes)(e.KeyCode - Keys.D1));
                    e.Handled = true;
                    break;
                case Keys.F:
                    Vector3? pos = GizmoPosition();
                    if (pos != null) cam3DPos = pos.Value - Camera3DForward() * 1500;
                    Render();
                    e.Handled = true;
                    break;
            }
        }

        private void cam3DTimer_Tick(object sender, EventArgs e)
        {
            if (!mode_3d || keys3D.Count == 0)
            {
                cam3DTimer.Stop();
                return;
            }
            MoveCamera3D();
            Render();
        }

        // Moves the camera by the held WASDQE keys for the time since it last moved.
        private void MoveCamera3D()
        {
            if (keys3D.Count == 0) return;
            float dt = Math.Min(0.1f, (float)cam3DClock.Elapsed.TotalSeconds);
            cam3DClock.Restart();
            Vector3 f = Camera3DForward(), r = Camera3DRight(), move = new Vector3(0);
            if (keys3D.Contains(Keys.W)) move += f;
            if (keys3D.Contains(Keys.S)) move -= f;
            if (keys3D.Contains(Keys.D)) move += r;
            if (keys3D.Contains(Keys.A)) move -= r;
            if (keys3D.Contains(Keys.E)) move += new Vector3(0, 1, 0);
            if (keys3D.Contains(Keys.Q)) move -= new Vector3(0, 1, 0);
            float speed = cam3DSpeed * ((ModifierKeys & Keys.Shift) != 0 ? 4 : 1);
            cam3DPos += move * (speed * dt);
        }

        // The wheel moves the camera forward and back; + and - change the speed of WASD.
        private void Wheel3D(bool forward)
        {
            cam3DPos += Camera3DForward() * (cam3DSpeed * 0.25f * (forward ? 1 : -1));
            Render();
        }

        private void ChangeSpeed3D(bool faster)
        {
            cam3DSpeed = Math.Max(100, Math.Min(50000, cam3DSpeed * (faster ? 1.5f : 1 / 1.5f)));
            toolStripStatusLabel1.Text = String.Format("3D camera speed: {0:0} units/s", cam3DSpeed);
        }
    }
}
