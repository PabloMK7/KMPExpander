using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using System.Xml.Serialization;
using Extensions;
using LibCTR.Collections;
using Tao.OpenGl;

namespace KMPExpander.Class.SimpleKMPs
{
    // Eight karts that are not part of the KMP (they are not saved). While the section is checked in the tree, the
    // enabled karts are drawn, and the preview shows what the game would cull for the camera kart: the clip ID of the
    // checkpoint it is in and the view of the race camera behind it. The other karts are drawn faded when the game would
    // not draw them. Karts can be neither added nor removed; the Enabled column turns each one on and off.
    public class KartSimulator : ISectionBase
    {
        public const int KartCount = 8;
        public List<KartEntry> Entries = new List<KartEntry>();
        public bool Visible = false;
        [Category("Culling"), DisplayName("Clip ID culling"), DefaultValue(true), Description("Hide the parts of the course that the clip ID of the camera kart's checkpoint hides, as the game does.")]
        public bool ClipIDCulling { get; set; } = true;
        [Category("Culling"), DisplayName("Camera view culling"), DefaultValue(false), Description("Hide what is outside the camera kart's view, as the game does, and show that view as an orange cone.")]
        public bool CameraCulling { get; set; } = false;

        public KartSimulator()
        {
            for (int i = 0; i < KartCount; i++)
                Entries.Add(new KartEntry() { Index = i, Enabled = i == 0, Rank = i + 1 });
        }

        // The kart the culling is simulated for: the selected enabled kart, else the first enabled kart, else null.
        [Browsable(false)]
        public KartEntry CameraKart
        {
            get
            {
                List<object> selected = (Application.OpenForms[0] as Form1).SelectedDots;
                foreach (var k in Entries) if (k.Enabled && selected.Contains(k)) return k;
                foreach (var k in Entries) if (k.Enabled) return k;
                return null;
            }
        }

        // The race camera's defaults were measured in the game (Mario Circuit, driving): 55 to 68 units behind the
        // kart, 20 to 40 above it, looking 7 to 22 degrees down; its view is 5:3, with a half height tangent of 0.488
        // (0.543 at top speed), the near corners 1.5 units and the far ones 50000 units away. The kart went about 8 units
        // per frame.
        public enum Modes { Driving, Airborne, Gliding, Cannon, Respawning }

        public class KartEntry
        {
            internal int Index;
            internal RoutePoint Previous, Target, JustReached;
            // Physics (with a KCL): what the kart does, its speed, its vertical speed and the floor it is on.
            internal Modes Mode = Modes.Driving;
            internal float CurSpeed, VelY, GroundVelY;
            // The floor's slope along the way the kart goes (rise per unit), its normal, and the kart's up: the normal
            // smoothed as VehicleMove::calcGndNrm does (rate +0xD84, frames in the air +0xD48).
            internal float GroundSlope, NormalRate = 1;
            internal int AirFrames;
            internal Vector3 GroundNormal = new Vector3(0, 1, 0), BodyUp = new Vector3(0, 1, 0);
            internal ushort FloorValue;
            internal int BoostFrames, RespawnFrames, RespawnCount;
            internal bool GliderArmed, Underwater;
            internal string RespawnReason = "";
            internal GliderRoutes.GliderGroup GlideGroup;
            internal int GlideIndex;
            // The checkpoint the latest search found (its clip ID is used), and the game's checkpoint state of the kart.
            internal CheckPoints.CheckpointGroup.CheckpointEntry Checkpoint;
            internal CheckpointSearch.State CheckpointState = new CheckpointSearch.State();
            internal bool OutOfCheckpoints, RespawnDrop;
            internal RespawnPoints.RespawnEntry RespawnAt;
            internal Vector3 LastGround;
            // The CPU's route state: the path and direction of a battle kart, the points reached before Previous (newest
            // first), and the counters of AIStuck and the re-routes.
            internal int BattleDir = 1, BattlePath = -1;
            internal List<RoutePoint> History = new List<RoutePoint>();
            internal bool Advanced;
            internal int FramesSinceAdvance, NoAdvanceFrames, SlowFrames, WallFrames, BackPhase = -1, BackFrames, FallCount;
            internal float BackRate, AirRate;
            internal Vector3? LastRelocation;
            // The VALLEY variants that do not call Lakitu (bits; VehicleMove +0xE44), the variant of the glider pad that
            // armed the glider, the drift, and the items.
            internal int ValleyIgnore = 0x20;
            internal bool Drifting, DriftDrawn, AllowMiniTurbo = true, BoostStrong;
            internal int DriftOverLv1Frames, DriftSide, MushroomCount, MushroomTimer;
            internal float DriftCharge, TargetLane;
            internal float BodyAngle;                    // degrees between the body and the direction of travel
            internal RoutePoint LaneFor;                 // the target TargetLane was worked out for
            internal bool GoForShortcut, UsedItem;

            [Description("Show this kart and let it drive.")]
            public bool Enabled { get; set; }
            [Description("Whether the game would draw this kart, seen from the camera kart.")]
            public string Shown
            {
                get
                {
                    CullingPreview c = (Application.OpenForms[0] as Form1).Culling;
                    if (!Enabled || c == null || !c.Simulating) return "";
                    if (c.CameraKart == this) return "always";
                    return c.PointHidden(Pos.X, Pos.Z, 20) ? "hidden" : "drawn";
                }
            }
            [XmlIgnore, Browsable(false)]
            public Vector3 Pos { get; set; } = new Vector3(0, 0, 0);
            [Description("Position of the kart.")]
            public Single PositionX
            {
                get { return Pos.X; }
                set { Pos = new Vector3(value, Pos.Y, Pos.Z); }
            }
            [Description("Height of the kart.")]
            public Single PositionY
            {
                get { return Pos.Y; }
                set { Pos = new Vector3(Pos.X, value, Pos.Z); }
            }
            [Description("Position of the kart.")]
            public Single PositionZ
            {
                get { return Pos.Z; }
                set { Pos = new Vector3(Pos.X, Pos.Y, value); }
            }
            [Description("The direction the kart faces, in degrees, like the rotations in the KMP. 0 faces +Z and 90 faces +X.")]
            public Single Direction { get; set; } = 0;
            [Description("Which lane the kart drives in, as a share of the road width at each point, from -0.85 to 0.85. Restart sets it from the kart's place on the start grid, as the game does.")]
            public Single Lane { get; set; } = 0;
            [Description("Top speed in units per frame. Changing Level sets the usual value for that engine class. In the game the CPUs reached about 6.1 to 6.4 at 50cc, 6.8 to 7.2 at 100cc and 7.7 to 8.0 at 150cc.")]
            public Single Speed { get; set; } = 7.9f;
            [Description("How fast the kart can turn, in degrees per frame.")]
            public Single MaxTurn { get; set; } = 4;
            [Description("What the kart is doing, such as driving, in the air, gliding, in a cannon, under water or respawning, and why it respawns.")]
            public string Status
            {
                get
                {
                    if (Target == null && Mode != Modes.Respawning) return "";
                    string text = Mode == Modes.Respawning ? "respawning (" + RespawnReason + ")" : Mode.ToString().ToLowerInvariant();
                    if (Underwater && Mode != Modes.Respawning) text += ", under water";
                    if (Drifting) text += DriftCharge >= MiniTurboLv2 ? ", drifting (mini-turbo 2)" : DriftCharge >= MiniTurboLv1 ? ", drifting (mini-turbo 1)" : ", drifting";
                    if (BoostFrames > 0) text += ", boost";
                    if (MushroomCount > 0) text += ItemVal == 4 ? ", Mushrooms" : ", " + MushroomCount + (ItemVal == 3 ? " Golden" : "") + " Mushroom" + (MushroomCount > 1 ? "s" : "");
                    if (BackPhase >= 0 && Mode != Modes.Respawning) text += ", backing up";
                    return text;
                }
            }
            [Description("How many times the kart has respawned since it started driving.")]
            public int TotalRespawns { get { return RespawnCount; } }
            [Description("Tick it to make Lakitu pick this kart up on the next frame, as after a fall.")]
            public bool Respawn { get; set; }
            [Description("Tick it to make this kart look for its route again on the next frame. It continues from the nearest route point within 500 units, as the game does for a kart that is lost.")]
            public bool Reroute { get; set; }

            private int rank = 1;
            [Description("The kart's place in the race, from 1 to 8. It changes how likely the kart is to drift and to keep its mini-turbo, as for the game's CPUs.")]
            public int Rank
            {
                get { return rank; }
                set { rank = Math.Max(1, Math.Min(8, value)); }
            }
            private static string Option(string[] names, int value) { return value + " - " + names[value]; }
            private static int ParseOption(string value, int count) { return Math.Max(0, Math.Min(count - 1, int.Parse(value.Split(' ')[0]))); }
            private static readonly string[] LevelNames = { "50cc (Easy)", "100cc (Normal)", "150cc and online (Hard)" };
            internal int LevelVal = 2;
            [Description("The CPU level, which goes with the engine class. It changes how often CPUs drift, whether they take mushroom shortcuts and how they take corners.")]
            public string Level
            {
                get { return Option(LevelNames, LevelVal); }
                set
                {
                    LevelVal = ParseOption(value, LevelNames.Length);
                    Speed = new float[] { 6.3f, 7.15f, 7.9f }[LevelVal];
                }
            }
            internal static readonly string[] ItemNames = { "None", "Mushroom", "Triple Mushroom", "Golden Mushroom", "Mushroom, given again after each use" };
            internal int ItemVal = 0;
            [Description("The item the kart holds when it starts driving. A CPU holding a Mushroom or a Triple Mushroom takes mushroom shortcuts, always at 150cc, half of the time at 100cc and never at 50cc. It uses its Mushrooms at points whose mushroom setting is 0. A Golden Mushroom never takes shortcuts.")]
            public string Item
            {
                get { return Option(ItemNames, ItemVal); }
                set { ItemVal = ParseOption(value, ItemNames.Length); }
            }
            internal static readonly string[] TitleNames = { "None", "Boost Jumper", "Safe Driver", "Drift Wizard", "Aviator", "Dolphin", "Model Driver", "Rookie", "Star Racer" };
            private static readonly int[] TitleBits = { 0, 0x4, 0x10, 0x20, 0x40, 0x80, 0x800, 0x100, 0x1000 };
            internal int TitleVal = 0;
            [Description("The Mii title of a Mii CPU in a Grand Prix. [Boost Jumper]s and [Aviator]s prefer paths marked -3, [Dolphin]s prefer paths marked -4, and [Aviator]s and [Dolphin]s skip shortcuts. [Drift Wizard]s always drift and keep the mini-turbo. [Safe Driver]s settle into their lane faster. [Rookie]s and [Star Racer]s drive in the middle and never drift. [Model Driver]s have no special conditions simulated. Set to [None] to disable.")]
            public string MiiTitle
            {
                get { return Option(TitleNames, TitleVal); }
                set { TitleVal = ParseOption(value, TitleNames.Length); }
            }
            internal int TitleMask { get { return TitleBits[TitleVal]; } }
            internal static readonly string[] GroundNames = { "Physics", "Follow the ground", "Follow the route" };
            internal int GroundVal = 0;
            [Description("How the kart moves up and down. 0 uses physics, which needs a loaded collision file (KCL). The kart then falls and jumps, stands on floors, is stopped by walls, is slowed by off-road, gets boosts from boost pads, glides from glider pads and respawns after a fall. Without a KCL it works like 1. 1 puts the kart on the floor of the KCL, or else on the loaded OBJ model, without physics. 2 keeps the kart at the height of the enemy points. With 0 and 1 the start grid also puts the karts on the ground.")]
            public string SimulationMode
            {
                get { return Option(GroundNames, GroundVal); }
                set { GroundVal = ParseOption(value, GroundNames.Length); }
            }
            internal bool Physics { get { return GroundVal == 0; } }
            internal bool SnapToModel { get { return GroundVal != 2; } }

            [Description("For the camera kart while the karts drive, the clip ID of the checkpoint it is in, or none.")]
            public string ClipID
            {
                get
                {
                    CullingPreview c = (Application.OpenForms[0] as Form1).Culling;
                    if (c == null || !c.Simulating || c.CameraKart != this) return "";
                    return c.KartClipID < 0 ? "none" : c.KartClipID.ToString();
                }
            }

            [Description("For the camera kart while the karts drive, how many of the 64 culling cells the game would hide now.")]
            public string HiddenCells
            {
                get
                {
                    CullingPreview c = (Application.OpenForms[0] as Form1).Culling;
                    if (c == null || !c.Simulating || c.CameraKart != this || c.HiddenCells == null) return "";
                    int hidden = 0;
                    foreach (bool b in c.HiddenCells) if (b) hidden++;
                    return hidden + " of 64" + (c.ViewEdges == null ? " (no view culling)" : "");
                }
            }

            [Description("How far behind the kart its race camera is, in units.")]
            public Single CameraBack { get; set; } = 60;
            [Description("How high above the kart its race camera is, in units.")]
            public Single CameraUp { get; set; } = 31;
            [Description("How far the camera looks down, in degrees. Negative values look down.")]
            public Single CameraPitch { get; set; } = -14.5f;
            [Description("How wide the camera's view is. It is the tangent of half the vertical field of view.")]
            public Single ViewHalfHeight { get; set; } = 0.4877f;
            [Description("The closest distance the camera draws, in units.")]
            public Single Near { get; set; } = 1.5f;
            [Description("The farthest distance the camera draws, in units.")]
            public Single Far { get; set; } = 50000;

            [XmlIgnore, Browsable(false)]
            public float ForwardX { get { return (float)Math.Sin(Direction * Math.PI / 180); } }
            [XmlIgnore, Browsable(false)]
            public float ForwardZ { get { return (float)Math.Cos(Direction * Math.PI / 180); } }
            // Where the body points: the direction of travel turned by BodyAngle.
            internal float BodyX { get { return (float)Math.Sin((Direction + BodyAngle) * Math.PI / 180); } }
            internal float BodyZ { get { return (float)Math.Cos((Direction + BodyAngle) * Math.PI / 180); } }

            // While the karts drive, the race camera follows its kart with a lag, as the game's does, so that turns and
            // bumps do not shake it: each frame its direction turns a share of the way toward the kart's and its height
            // moves a share of the way toward the kart's plus CameraUp (the shares are guesses). It stays CameraBack
            // behind the kart. Stopped, it is fixed to the kart.
            internal const float CameraTurnRate = 0.25f, CameraHeightRate = 0.15f;
            internal bool CameraSmoothed;
            internal float CameraFwdX, CameraFwdZ, CameraY;

            internal void UpdateCamera()
            {
                if (!CameraSmoothed)
                {
                    CameraFwdX = ForwardX;
                    CameraFwdZ = ForwardZ;
                    CameraY = Pos.Y + CameraUp;
                    CameraSmoothed = true;
                    return;
                }
                float x = CameraFwdX + (ForwardX - CameraFwdX) * CameraTurnRate, z = CameraFwdZ + (ForwardZ - CameraFwdZ) * CameraTurnRate;
                float l = (float)Math.Sqrt(x * x + z * z);
                if (l > 1e-4f) { CameraFwdX = x / l; CameraFwdZ = z / l; }
                CameraY += (Pos.Y + CameraUp - CameraY) * CameraHeightRate;
            }

            private bool UseSmoothCamera { get { return CameraSmoothed && Target != null; } }

            public Vector3 GetCameraPosition()
            {
                float fx = UseSmoothCamera ? CameraFwdX : ForwardX, fz = UseSmoothCamera ? CameraFwdZ : ForwardZ;
                return new Vector3(Pos.X - fx * CameraBack, UseSmoothCamera ? CameraY : Pos.Y + CameraUp, Pos.Z - fz * CameraBack);
            }

            public Vector3 GetCameraDirection()
            {
                float fx = UseSmoothCamera ? CameraFwdX : ForwardX, fz = UseSmoothCamera ? CameraFwdZ : ForwardZ;
                float c = (float)Math.Cos(CameraPitch * Math.PI / 180), s = (float)Math.Sin(CameraPitch * Math.PI / 180);
                return new Vector3(fx * c, s, fz * c);
            }

            // The eight corners of the view volume, numbered as the game numbers them: bit 0 bottom, bit 1 near, bit 2
            // the other side.
            public Vector3[] GetCorners()
            {
                Vector3 p = GetCameraPosition(), d = GetCameraDirection();
                float rl = (float)Math.Sqrt(d.Z * d.Z + d.X * d.X);
                Vector3 right = new Vector3(d.Z / rl, 0, -d.X / rl);
                Vector3 up = new Vector3(d.Y * right.Z - d.Z * right.Y, d.Z * right.X - d.X * right.Z, d.X * right.Y - d.Y * right.X);
                Vector3[] corners = new Vector3[8];
                for (int i = 0; i < 8; i++)
                {
                    float dist = (i & 2) != 0 ? Near : Far;
                    float h = dist * ViewHalfHeight * ((i & 1) != 0 ? -1 : 1);
                    float w = dist * ViewHalfHeight * (5f / 3f) * ((i & 4) != 0 ? 1 : -1);
                    corners[i] = new Vector3(p.X + d.X * dist + up.X * h + right.X * w,
                                             p.Y + d.Y * dist + up.Y * h + right.Y * w,
                                             p.Z + d.Z * dist + up.Z * h + right.Z * w);
                }
                return corners;
            }

            public void RenderPicking()
            {
                ViewPlaneHandler vph = (Application.OpenForms[0] as Form1).vph;
                VisualSettings Settings = (Application.OpenForms[0] as Form1).Settings;
                Color pickingColor = SectionPicking.GetColor(Sections.KartSimulator, 0, Index);
                Gl.glColor4f(pickingColor.R / 255f, pickingColor.G / 255f, pickingColor.B / 255f, 1f);
                vph.drawPoint(Pos, Settings.PointSize + 6f);
            }

            // The kart as an arrow (orange for the camera kart, blue for the others, faded when the game would not
            // draw it), and for the camera kart its camera and the view region as a cone.
            public void Render()
            {
                Form1 form = Application.OpenForms[0] as Form1;
                ViewPlaneHandler vph = form.vph;
                VisualSettings Settings = form.Settings;
                CullingPreview c = form.Culling;
                bool selected = form.SelectedDots.Contains(this);
                bool isCamera = c != null && c.Simulating && c.CameraKart == this;
                bool hidden = c != null && c.Simulating && !isCamera && c.PointHidden(Pos.X, Pos.Z, 20);
                float r = isCamera ? 1f : 0.2f, g = isCamera ? 0.5f : 0.7f, b = isCamera ? 0f : 1f, a = hidden ? 0.3f : 1f;
                if (Mode == Modes.Respawning && RespawnFrames >= RespawnPlaceFrame) a = 0.25f;
                if (selected) { r = Settings.HighlightPointColor.R / 255f; g = Settings.HighlightPointColor.G / 255f; b = Settings.HighlightPointColor.B / 255f; }
                if (vph.mode != ViewPlaneHandler.PLANE_MODES.XZ)
                {
                    Gl.glColor4f(r, g, b, a);
                    vph.drawPoint(Pos, Settings.PointSize + 2f);
                    return;
                }
                if (isCamera && c.ViewPolygon != null && c.ViewPolygon.Count >= 3)
                {
                    Gl.glColor4f(1f, 0.5f, 0f, 0.12f);
                    Gl.glBegin(Gl.GL_POLYGON);
                    foreach (var p in c.ViewPolygon) vph.drawXZ(p[0], p[1], Pos.Y);
                    Gl.glEnd();
                    Gl.glColor4f(1f, 0.5f, 0f, 0.7f);
                    Gl.glLineWidth(1);
                    Gl.glBegin(Gl.GL_LINE_LOOP);
                    foreach (var p in c.ViewPolygon) vph.drawXZ(p[0], p[1], Pos.Y);
                    Gl.glEnd();
                }
                // In 3D the arrow is never shorter than a kart (about 30 units).
                float size = Settings.PointSize * 3 * form.GetPixelSize(Pos);
                if (vph.Is3D) size = Math.Max(size, 18);
                // The body: turned into the turn while drifting (measured in game: 41 to 44 degrees between the body and
                // the direction of travel), turning in and back gradually.
                // In 3D the kart stands on its up (the smoothed floor normal): its forward is the body's direction laid
                // on that plane. Points are given as (forward, side, height) from the kart.
                Vector3 up = vph.Is3D ? BodyUp : new Vector3(0, 1, 0);
                Vector3 fwd = new Vector3(BodyX, 0, BodyZ);
                fwd = Normalize(Add(fwd, up, -fwd.Dot(up)));
                if (Length(fwd) == 0) fwd = new Vector3(BodyX, 0, BodyZ);
                Vector3 side = up.Cross(fwd);
                Func<float, float, float, Vector3> at = (f, sd, h) => Add(Add(Add(Pos, fwd, f), side, sd), up, h);
                Vector3[] body = { at(size, 0, 0), at(-size * 0.6f, size * 0.5f, 0), at(-size * 0.6f, -size * 0.5f, 0) };
                // In 3D a lit wedge KartHeight high; the sparks and the outline go on its top.
                float top = vph.Is3D ? KartHeight + 0.5f : 0;
                Gl.glColor4f(r, g, b, a);
                if (vph.Is3D) DrawWedge(vph, body, up, KartHeight);
                else
                {
                    Gl.glBegin(Gl.GL_TRIANGLES);
                    foreach (var p in body) vph.draw2DVertice(p);
                    Gl.glEnd();
                }
                if (Drifting && DriftCharge >= MiniTurboLv1)
                {
                    // The drift sparks, as in the game: blue once mini-turbo level 1 is charged, orange at level 2.
                    float back = size * 0.6f, len = size * 0.9f, half = size * 0.35f;
                    if (DriftCharge >= MiniTurboLv2) Gl.glColor4f(1f, 0.55f, 0f, a);
                    else Gl.glColor4f(0.2f, 0.5f, 1f, a);
                    Gl.glBegin(Gl.GL_TRIANGLES);
                    vph.draw2DVertice(at(-back, 0, top));
                    vph.draw2DVertice(at(-back - len, half, top));
                    vph.draw2DVertice(at(-back - len, -half, top));
                    Gl.glEnd();
                    Gl.glColor4f(r, g, b, a);
                }
                // The physics state as an outline: white in the air, cyan gliding, magenta in a cannon, blue under water.
                Color outline = Color.Empty;
                if (Underwater) outline = Color.FromArgb(0, 60, 255);
                if (Mode == Modes.Airborne) outline = Color.White;
                else if (Mode == Modes.Gliding) outline = Color.Cyan;
                else if (Mode == Modes.Cannon) outline = Color.Magenta;
                if (Target != null && outline != Color.Empty)
                {
                    Gl.glColor4f(outline.R / 255f, outline.G / 255f, outline.B / 255f, a);
                    Gl.glLineWidth(2);
                    Gl.glBegin(Gl.GL_LINE_LOOP);
                    foreach (var p in body) vph.draw2DVertice(Add(p, up, top));
                    Gl.glEnd();
                    Gl.glLineWidth(1);
                    Gl.glColor4f(r, g, b, a);
                }
                if (selected && Target != null && form.getKayEmPee().EnemyRoutes.GetVisibility())
                {
                    // The route point the kart heads for, and its aim point (moved to the kart's lane), shown with the
                    // enemy points.
                    Vector3 aim = form.getKayEmPee().KartSimulator.AimPoint(this);
                    Gl.glColor4f(1f, 1f, 0f, 1f);
                    Gl.glLineWidth(1);
                    Gl.glBegin(Gl.GL_LINES);
                    vph.draw2DVertice(Pos);
                    vph.draw2DVertice(aim);
                    Gl.glEnd();
                    vph.drawPoint(Target.Pos, Settings.PointSize + 2f);
                    Gl.glColor4f(r, g, b, a);
                }
                if (isCamera)
                {
                    Vector3 cam = GetCameraPosition();
                    vph.drawPoint(cam, vph.Is3D ? Settings.PointSize / 4 : Settings.PointSize / 2 + 1);
                }
            }
        }

        // The height of the kart drawn in 3D: about its collision sphere's radius.
        internal const float KartHeight = 12;

        // A wedge over the triangle bottom, height along up, lit like the points' spheres.
        private static void DrawWedge(ViewPlaneHandler vph, Vector3[] bottom, Vector3 up, float height)
        {
            if (!vph.Picking) Gl.glEnable(Gl.GL_LIGHTING);
            Vector3[] top = new Vector3[3];
            for (int i = 0; i < 3; i++) top[i] = Add(bottom[i], up, height);
            Vector3 centre = Add(Add(bottom[0], bottom[1]), bottom[2]) * (1f / 3);
            // Normals in the (X, Z, Y) layout of the 3D view.
            Action<Vector3> normal = n => Gl.glNormal3f(n.X, n.Z, n.Y);
            Gl.glBegin(Gl.GL_TRIANGLES);
            normal(up);
            for (int i = 0; i < 3; i++) vph.draw3DVertice(top[i]);
            normal(up * -1);
            for (int i = 2; i >= 0; i--) vph.draw3DVertice(bottom[i]);
            Gl.glEnd();
            Gl.glBegin(Gl.GL_QUADS);
            for (int i = 0; i < 3; i++)
            {
                int j = (i + 1) % 3;
                Vector3 n = Add(bottom[j], bottom[i], -1).Cross(up);
                if (n.Dot(Add(Add(bottom[i], bottom[j]) * 0.5f, centre, -1)) < 0) n = n * -1;
                normal(n);
                vph.draw3DVertice(bottom[i]);
                vph.draw3DVertice(bottom[j]);
                vph.draw3DVertice(top[j]);
                vph.draw3DVertice(top[i]);
            }
            Gl.glEnd();
            Gl.glDisable(Gl.GL_LIGHTING);
        }

        // --- Driving, with the rules the game's CPUs use (the research on enemy AI behaviour and enemy points). In a
        // race the route is the enemy points plus the points the game inserts on a Bezier curve between them (one
        // every 150 units at most), and paths are one-way; a kart moves on to its next point when it is within 280 units
        // of its aim point (140 after a point with flag 0x04) or has crossed the plane through it that bisects the turn
        // there; the aim point is the route point moved sideways by the kart's lane; at a fork the kart takes its Mii
        // title's path, else a mushroom shortcut (a path whose first point has mushroom setting 1) if it holds a Mushroom
        // and goes for one, else a random normal branch that does not lead back to where it came from. In a battle (no
        // checkpoints) the enemy points are used as they are, on their centre, and paths are two-way: a kart moves on
        // within 100 units (140 after flag 0x04), keeps the direction it entered a path in, and at the end of a path
        // takes a random branch on another path. Drifts and Mushrooms follow the CPU rules below; rubber-banding, other
        // items, other karts and the objects that close branches are not simulated.

        internal class RoutePoint
        {
            public Vector3 Pos;
            public float Scale;
            public byte Flags;
            public ushort Mushroom;
            public byte Drift;
            public short MaxSearchY;
            public short PathFind;                   // -3 or -4 when a point of its path has it (Mii title paths)
            public float Corner = 1;                 // AIPathPoint m_corner: the turn's cosine, signed; 1 at junctions
            public bool Junction;
            public bool Inserted;
            public int Path, Index;                  // the enemy path, and the place in its list of route points
            public List<RoutePoint> Next = new List<RoutePoint>();
        }

        private List<List<RoutePoint>> route = null;
        private bool battle = false;
        private CheckpointSearch checkpoints = null;          // in a race with checkpoints
        private static Random random = new Random();

        private static float Length(Vector3 v)
        {
            return (float)Math.Sqrt(v.X * v.X + v.Y * v.Y + v.Z * v.Z);
        }

        private static Vector3 Normalize(Vector3 v)
        {
            float l = Length(v);
            return l > 0 ? new Vector3(v.X / l, v.Y / l, v.Z / l) : new Vector3(0, 0, 0);
        }

        private static Vector3 Add(Vector3 a, Vector3 b, float k = 1)
        {
            return new Vector3(a.X + b.X * k, a.Y + b.Y * k, a.Z + b.Z * k);
        }

        private static List<int> Links(List<EnemyRoutes.EnemyGroup> groups, Int16[] links)
        {
            List<int> list = new List<int>();
            if (links != null)
                foreach (var n in links)
                    if (n >= 0 && n < groups.Count && groups[n].Entries.Count > 0 && !list.Contains(n)) list.Add(n);
            return list;
        }

        // The paths linked to one end of a path, as MapdataEnemyPoint::setup reads them: the valid entries packed at the
        // start of the list (the game reads as many slots as there are valid entries), each with its slot number, which
        // picks the bit of the link end flags.
        internal static List<int[]> LinkSlots(List<EnemyRoutes.EnemyGroup> groups, Int16[] links)
        {
            return EnemyRoutes.EnemyGroup.LinkSlots(links).FindAll(l => l[1] < groups.Count && groups[l[1]].Entries.Count > 0);
        }

        // The route the game drives on: per path, its points (with the inserted ones in a race), linked as
        // MapdataEnemyPoint::setup links them. Race: the last point of a path leads to the first point of each next
        // path, or to its last point when bit 16 + slot of the link end flags is set. Battle: every point leads to both
        // neighbours; the first point of a path with more than one point also leads to each previous path's last point
        // (its first point when bit slot is set), and the last point to each next path's first point (its last point
        // when bit 16 + slot is set).
        private static List<List<RoutePoint>> BuildRoute(SimpleKMP kmp, bool race)
        {
            var groups = kmp.EnemyRoutes.Entries;
            List<List<RoutePoint>> result = new List<List<RoutePoint>>();
            for (int g = 0; g < groups.Count; g++)
            {
                var entries = groups[g].Entries;
                List<int> nexts = Links(groups, groups[g].Next), prevs = Links(groups, groups[g].Previous);
                List<RoutePoint> points = new List<RoutePoint>();
                for (int i = 0; i < entries.Count; i++)
                {
                    var e = entries[i];
                    points.Add(new RoutePoint() { Pos = e.Pos, Scale = e.Scale, Flags = e.Flags, Mushroom = e.MushSettingsVal, Drift = e.DriftSettingsVal, MaxSearchY = e.MaxSearchYOffsetVal });
                    if (!race || i + 1 >= entries.Count) continue;
                    // PathSmoother: handles of 75 units along (next - previous) at points with one previous and one next
                    // point, and floor(|point - previous| / 150) points inserted after point i when point i + 1 has a
                    // single next point. Links between paths are never smoothed.
                    List<Vector3> before = new List<Vector3>();
                    if (i > 0) before.Add(entries[i - 1].Pos);
                    else foreach (int pg in prevs) before.Add(groups[pg].Entries[groups[pg].Entries.Count - 1].Pos);
                    if (before.Count != 1) continue;
                    if (i + 2 >= entries.Count && nexts.Count != 1) continue;
                    Vector3 p0 = e.Pos, p3 = entries[i + 1].Pos;
                    int count = (int)Math.Floor(Length(Add(p0, before[0], -1)) / 150);
                    if (count <= 0) continue;
                    Vector3 after = i + 2 < entries.Count ? entries[i + 2].Pos : groups[nexts[0]].Entries[0].Pos;
                    Vector3 p1 = Add(p0, Normalize(Add(p3, before[0], -1)), 75);
                    Vector3 p2 = Add(p3, Normalize(Add(after, p0, -1)), -75);
                    for (int k = 1; k <= count; k++)
                    {
                        float t = k / (float)(count + 1), u = 1 - t;
                        float b0 = u * u * u, b1 = 3 * t * u * u, b2 = 3 * t * t * u, b3 = t * t * t;
                        var src = t < 0.5f ? e : entries[i + 1];
                        Vector3 pos = new Vector3(b0 * p0.X + b1 * p1.X + b2 * p2.X + b3 * p3.X,
                                                  b0 * p0.Y + b1 * p1.Y + b2 * p2.Y + b3 * p3.Y,
                                                  b0 * p0.Z + b1 * p1.Z + b2 * p2.Z + b3 * p3.Z);
                        points.Add(new RoutePoint() { Pos = pos, Scale = src.Scale, Flags = src.Flags, Mushroom = src.MushSettingsVal, Drift = src.DriftSettingsVal, MaxSearchY = src.MaxSearchYOffsetVal, Inserted = true });
                    }
                }
                short pathFind = 0;
                foreach (var e in entries) if (e.PathFindOptsVal == -3 || e.PathFindOptsVal == -4) pathFind = e.PathFindOptsVal;
                for (int i = 0; i < points.Count; i++) { points[i].Path = g; points[i].Index = i; points[i].PathFind = pathFind; }
                for (int i = 0; i + 1 < points.Count; i++) points[i].Next.Add(points[i + 1]);
                if (!race)
                    for (int i = 1; i < points.Count; i++) points[i].Next.Add(points[i - 1]);
                result.Add(points);
            }
            for (int g = 0; g < groups.Count; g++)
            {
                List<RoutePoint> points = result[g];
                if (points.Count == 0) continue;
                uint ends = groups[g].LinkEndFlags;
                RoutePoint first = points[0], last = points[points.Count - 1];
                if (!race && points.Count > 1)
                    foreach (int[] link in LinkSlots(groups, groups[g].Previous))
                    {
                        List<RoutePoint> other = result[link[1]];
                        first.Next.Add((ends & (1u << link[0])) != 0 ? other[0] : other[other.Count - 1]);
                    }
                foreach (int[] link in LinkSlots(groups, groups[g].Next))
                {
                    List<RoutePoint> other = result[link[1]];
                    last.Next.Add((ends & (1u << (16 + link[0]))) != 0 ? other[other.Count - 1] : other[0]);
                }
            }
            if (race) SetCorners(result);
            return result;
        }

        // AIPathPoint m_corner (the research on enemy AI behaviour, Finding 4): at a point with one point before and one
        // after it, the cosine of the turn between the horizontal directions in and out, negative when the route turns
        // away from the side axis cross(in, up); 1 at junctions.
        private static void SetCorners(List<List<RoutePoint>> paths)
        {
            Dictionary<RoutePoint, List<RoutePoint>> incoming = new Dictionary<RoutePoint, List<RoutePoint>>();
            foreach (var path in paths)
                foreach (var p in path)
                    foreach (var n in p.Next)
                    {
                        if (!incoming.ContainsKey(n)) incoming[n] = new List<RoutePoint>();
                        incoming[n].Add(p);
                    }
            foreach (var path in paths)
                foreach (var p in path)
                {
                    List<RoutePoint> before;
                    p.Junction = p.Next.Count != 1 || !incoming.TryGetValue(p, out before) || before.Count != 1;
                    if (p.Junction) continue;
                    Vector3 a = incoming[p][0].Pos, c = p.Next[0].Pos;
                    float ix = p.Pos.X - a.X, iz = p.Pos.Z - a.Z, ox = c.X - p.Pos.X, oz = c.Z - p.Pos.Z;
                    float l = (float)Math.Sqrt((ix * ix + iz * iz) * (ox * ox + oz * oz));
                    if (l == 0) continue;
                    float cos = (ix * ox + iz * oz) / l;
                    p.Corner = -iz * ox + ix * oz < 0 ? -cos : cos;
                }
        }

        // The point after p, which the kart has just reached (cameFrom is the point it reached before that).
        // Race: at a fork a random normal branch that is not cameFrom (mushroom shortcuts are only taken with a Mushroom),
        // branch 0 when there is none; at the end of a path without next paths the game reads a link that does not
        // exist, and in game that sent the CPUs to point 0 (the research on enemy AI behaviour, Finding 13).
        // Battle (AIPathPointBattle::selectNextPointHandle_): along the path in the direction the kart entered it, and at
        // the end of the path a random branch on another path (m_do_not_select_backward), or the only link there is.
        private RoutePoint ChooseNext(KartEntry k, RoutePoint p, RoutePoint cameFrom)
        {
            RoutePoint next;
            if (battle)
            {
                List<RoutePoint> path = route[p.Path];
                int ni = p.Index + k.BattleDir;
                if (p.Path == k.BattlePath && ni >= 0 && ni < path.Count) return path[ni];
                List<RoutePoint> options = new List<RoutePoint>();
                foreach (var n in p.Next) if (n.Path != p.Path) options.Add(n);
                if (options.Count == 0 || p.Next.Count == 1) options = p.Next;
                if (options.Count == 0) return null;
                next = options[random.Next(options.Count)];
                EnterBattlePath(k, next, p);
                return next;
            }
            if (p.Next.Count == 0) return route.Count > 0 && route[0].Count > 0 ? route[0][0] : null;
            if (p.Next.Count == 1) return p.Next[0];
            // selectNextPointHandleIndexWithShortcut_ (Finding 8): never back where the kart came from; a Mii title
            // path (the first one listed); a mushroom shortcut if the CPU goes for one; else a random normal branch.
            List<RoutePoint> branches = p.Next.FindAll(n => n != cameFrom);
            int title = k.TitleMask;
            foreach (var n in branches)
                if ((n.PathFind == -3 && (title & 0x44) != 0) || (n.PathFind == -4 && (title & 0x80) != 0)) return n;
            List<RoutePoint> shortcuts = branches.FindAll(n => n.Mushroom == 1), normal = branches.FindAll(n => n.Mushroom != 1);
            if (k.GoForShortcut && k.MushroomCount > 0 && shortcuts.Count > 0)
            {
                // A single Mushroom is fired on the frame the shortcut is chosen, if the point just reached allows it.
                if (p.Mushroom == 0 && SingleMushroom(k) && !k.Drifting) UseMushroom(k);
                return shortcuts[random.Next(shortcuts.Count)];
            }
            if (normal.Count == 0) return p.Next[0];
            return normal[random.Next(normal.Count)];
        }

        // The direction a battle kart drives a path in: forward when it enters at the first point, backward at the last
        // one; from inside a path (a re-route), toward the end away from where it comes from.
        private void EnterBattlePath(KartEntry k, RoutePoint p, RoutePoint from)
        {
            if (!battle) return;
            int count = route[p.Path].Count;
            if (from != null && from.Path == p.Path) k.BattleDir = p.Index >= from.Index ? 1 : -1;
            else if (p.Index == 0) k.BattleDir = 1;
            else if (p.Index == count - 1) k.BattleDir = -1;
            else k.BattleDir = 1;
            k.BattlePath = p.Path;
        }

        // Where a kart aims: the target moved by Lane x half-width along the side axis, cross(out, up) with out the
        // direction the route leaves the target in. Battle karts aim at the centre.
        internal Vector3 AimPoint(KartEntry k)
        {
            RoutePoint t = k.Target;
            if (battle || (k.TitleMask & 0x1100) != 0) return t.Pos;
            float lane = k.LaneFor == t ? k.TargetLane : k.Lane;
            Vector3 dir = t.Next.Count > 0 ? Add(t.Next[0].Pos, t.Pos, -1) : Add(t.Pos, k.Previous.Pos, -1);
            float l = (float)Math.Sqrt(dir.X * dir.X + dir.Z * dir.Z);
            if (l == 0) return t.Pos;
            if (lane == 0) return t.Pos;
            float offset = Math.Max(-0.85f, Math.Min(0.85f, lane)) * t.Scale * 50;
            return new Vector3(t.Pos.X - dir.Z / l * offset, t.Pos.Y, t.Pos.Z + dir.X / l * offset);
        }

        private float Height(KartEntry k, OBJWrapper obj, float x, float z, float y)
        {
            KCL kcl = (Application.OpenForms[0] as Form1).Collision;
            if (k.SnapToModel && kcl != null)
            {
                float? h = kcl.GetGroundHeight(x, z, y);
                if (h != null) return h.Value;
            }
            else if (k.SnapToModel && obj != null)
            {
                float? h = obj.GetGroundHeight(x, z, y);
                if (h != null) return h.Value;
            }
            return y;
        }

        // The start of a race, as FieldDirector::calcStartTag places it: the first start grid object (GOBJ 0x12D), or
        // start point (KTPT) 0 when there is none; kart i sits 20 + 40 x i units behind it and
        // 20.6 x (2 x (i mod 4) - 3) - 10.3 (+ 10.3 from the fifth kart on) to its side, mirrored when byte 1 of the stage
        // info (pole position) is not 0. Its lane is 0.75 x (0.375 x c - 0.125) + 0.2 with c = (i mod 4) - 2, negated when
        // that byte is 0, plus a random amount within 0.15 (AIControlRace::initAfterManager). In a battle kart i takes
        // start grid object i, or start point i.
        public bool PlaceOnGrid(SimpleKMP kmp, OBJWrapper obj)
        {
            battle = kmp.CheckPoints.Entries.Count == 0;
            List<Objects.ObjectEntry> grids = new List<Objects.ObjectEntry>();
            foreach (var o in kmp.Objects.Entries) if (o.ObjectID == 0x12D) grids.Add(o);
            bool poleByteZero = kmp.StageInformation.Entries.Count > 0 && kmp.StageInformation.Entries[0].PolePositionRaw == 0;
            bool placed = false;
            for (int i = 0; i < KartCount; i++)
            {
                KartEntry k = Entries[i];
                Vector3 origin;
                float yaw;
                int slot = battle ? i : 0;
                if (slot < grids.Count) { origin = grids[slot].Pos; yaw = grids[slot].RotationY; }
                else if (slot < kmp.StartPositions.Entries.Count) { origin = kmp.StartPositions.Entries[slot].Pos; yaw = kmp.StartPositions.Entries[slot].RotationY; }
                else
                {
                    k.Pos = new Vector3(0, 0, 0);
                    k.Direction = 0;
                    continue;
                }
                float side = 0, back = 0;
                if (!battle)
                {
                    side = 20.6f * (2 * (i % 4) - 3) + (i < 4 ? -10.3f : 10.3f);
                    if (!poleByteZero) side = -side;
                    back = 20 + 40 * i;
                    float c = (i % 4) - 2;
                    float lane = 0.75f * (0.375f * c - 0.125f) + 0.2f;
                    if (poleByteZero) lane = -lane;
                    k.Lane = lane + (float)(random.NextDouble() * 0.3 - 0.15);
                }
                else k.Lane = 0;
                float cs = (float)Math.Cos(yaw * Math.PI / 180), sn = (float)Math.Sin(yaw * Math.PI / 180);
                float x = origin.X + side * cs - back * sn, z = origin.Z - side * sn - back * cs;
                k.Pos = new Vector3(x, Height(k, obj, x, z, origin.Y), z);
                k.Direction = yaw;
                placed = true;
            }
            return placed;
        }

        // Every enabled kart heads for the nearest route point in front of it (the nearest one if none is in front).
        // Returns false if the KMP has no enemy points.
        public bool StartDriving(SimpleKMP kmp, bool standing)
        {
            battle = kmp.CheckPoints.Entries.Count == 0;
            route = BuildRoute(kmp, !battle);
            checkpoints = !battle && kmp.CheckPoints.Entries.Count > 0 ? new CheckpointSearch(kmp) : null;
            bool any = false;
            foreach (var k in Entries)
            {
                k.Target = k.Previous = k.JustReached = null;
                k.Mode = Modes.Driving;
                k.CurSpeed = standing ? 0 : k.Speed;
                k.VelY = k.GroundVelY = k.GroundSlope = 0;
                k.NormalRate = 1;
                k.AirFrames = 0;
                k.GroundNormal = k.BodyUp = new Vector3(0, 1, 0);
                k.RespawnDrop = false;
                k.BoostFrames = k.RespawnFrames = k.RespawnCount = 0;
                k.BoostStrong = false;
                k.GliderArmed = k.Underwater = false;
                k.GlideGroup = null;
                k.Checkpoint = null;
                k.OutOfCheckpoints = false;
                k.CameraSmoothed = false;
                if (checkpoints != null)
                {
                    checkpoints.Init(k.CheckpointState, k.Pos.X, k.Pos.Z);
                    if (k.CheckpointState.Previous >= 0) k.Checkpoint = checkpoints.Points[k.CheckpointState.Previous].Entry;
                }
                k.LastGround = k.Pos;
                k.History.Clear();
                k.Respawn = k.Reroute = false;
                k.AirRate = 0;
                k.LastRelocation = null;
                k.FallCount = 0;
                k.ValleyIgnore = 0x20;
                k.Drifting = k.DriftDrawn = false;
                k.BodyAngle = 0;
                k.AllowMiniTurbo = true;
                k.MushroomCount = new int[] { 0, 1, 3, 1, 1 }[k.ItemVal];
                if (k.MushroomCount > 0) ReceiveMushroom(k);
                ResetStuck(k);
                if (!k.Enabled) continue;
                float best = float.MaxValue, bestAhead = float.MaxValue;
                RoutePoint nearest = null, nearestAhead = null, beforeNearest = null, beforeAhead = null;
                foreach (var path in route)
                {
                    for (int i = 0; i < path.Count; i++)
                    {
                        float dx = path[i].Pos.X - k.Pos.X, dz = path[i].Pos.Z - k.Pos.Z;
                        float d = dx * dx + dz * dz;
                        RoutePoint before = i > 0 ? path[i - 1] : null;
                        if (d < best) { best = d; nearest = path[i]; beforeNearest = before; }
                        if (dx * k.ForwardX + dz * k.ForwardZ > 0 && d < bestAhead) { bestAhead = d; nearestAhead = path[i]; beforeAhead = before; }
                    }
                }
                RoutePoint start = nearestAhead ?? nearest;
                if (start == null) continue;
                k.Target = start;
                k.Previous = (nearestAhead != null ? beforeAhead : beforeNearest) ?? Pseudo(k.Pos, start.Scale);
                // A battle kart drives its path toward the neighbour of the start point that is more in front of it.
                k.BattlePath = start.Path;
                k.BattleDir = 1;
                if (battle)
                {
                    var path = route[start.Path];
                    RoutePoint fwd = start.Index + 1 < path.Count ? path[start.Index + 1] : null, back = start.Index > 0 ? path[start.Index - 1] : null;
                    Func<RoutePoint, float> ahead = q => (q.Pos.X - start.Pos.X) * k.ForwardX + (q.Pos.Z - start.Pos.Z) * k.ForwardZ;
                    if (back != null && (fwd == null || ahead(back) > ahead(fwd))) k.BattleDir = -1;
                }
                any = true;
            }
            return any;
        }

        // One frame of driving for every enabled kart. Returns false when no kart is driving any more.
        public bool Step(SimpleKMP kmp, OBJWrapper obj)
        {
            KCL kcl = (Application.OpenForms[0] as Form1).Collision;
            bool any = false;
            foreach (var k in Entries)
            {
                if (!k.Enabled || k.Target == null) continue;
                bool physics = kcl != null && k.Physics;
                k.Advanced = k.UsedItem = false;
                if (k.Respawn && k.Mode != Modes.Respawning) StartRespawn(k, kmp, "forced");
                if (k.Reroute && k.Mode != Modes.Respawning) Reroute(k, true);
                k.Respawn = k.Reroute = false;
                if (k.Mode == Modes.Respawning) StepRespawn(k, kmp, physics ? kcl : null);
                else if (physics) StepPhysics(k, kmp, kcl);
                else StepOnRoute(k, obj);
                if (k.Mode != Modes.Respawning && k.Target != null)
                {
                    StepDrift(k);
                    StepItems(k);
                }
                if (k.Target != null) any = true;
                k.UpdateCamera();
            }
            return any;
        }

        private static void Turn(KartEntry k, Vector3 toward, float maxTurn)
        {
            float desired = (float)(Math.Atan2(toward.X - k.Pos.X, toward.Z - k.Pos.Z) * 180 / Math.PI);
            float diff = desired - k.Direction;
            while (diff > 180) diff -= 360;
            while (diff < -180) diff += 360;
            diff = Math.Max(-maxTurn, Math.Min(maxTurn, diff));
            k.Direction += diff;
            if (k.Direction > 180) k.Direction -= 360;
            if (k.Direction < -180) k.Direction += 360;
        }

        // Without physics the kart moves at its speed and takes the height of the route, or of the ground under it.
        private void StepOnRoute(KartEntry k, OBJWrapper obj)
        {
            Vector3 aim = AimPoint(k);
            Turn(k, aim, k.MaxTurn);
            float speed = k.Speed;
            if (k.BoostFrames > 0) speed = BoostTop(k);
            float x = k.Pos.X + k.ForwardX * speed, z = k.Pos.Z + k.ForwardZ * speed;

            Vector3 seg = Add(k.Target.Pos, k.Previous.Pos, -1);
            float len2 = seg.X * seg.X + seg.Z * seg.Z;
            float along = len2 > 0 ? ((x - k.Previous.Pos.X) * seg.X + (z - k.Previous.Pos.Z) * seg.Z) / len2 : 1;
            along = Math.Max(0, Math.Min(1, along));
            float y = k.Previous.Pos.Y + (k.Target.Pos.Y - k.Previous.Pos.Y) * along;
            k.Pos = new Vector3(x, Height(k, obj, x, z, y), z);
            AdvanceTarget(k, aim);
        }

        // Moves the kart on to its next route point when it has reached the current one.
        private void AdvanceTarget(KartEntry k, Vector3 aim)
        {
            Vector3 d = Add(aim, k.Pos, -1);
            bool precise = k.JustReached != null && (k.JustReached.Flags & 4) != 0;
            float radius = precise ? 140 : (battle ? 100 : 280);
            bool reached = d.X * d.X + d.Y * d.Y + d.Z * d.Z < radius * radius;
            if (!reached && !battle)
            {
                // The plane through the aim point whose normal is the sum of the directions into and out of the point.
                RoutePoint after = k.Target.Next.Count > 0 ? k.Target.Next[0] : null;
                Vector3 n = after != null ? Normalize(Add(after.Pos, k.Target.Pos, -1)) : new Vector3(0, 0, 0);
                Vector3 n2 = Add(n, Normalize(Add(k.Target.Pos, k.Previous.Pos, -1)));
                if (Length(n2) > 0) n = n2;
                Vector3 fromAim = Add(k.Pos, aim, -1);
                reached = Length(n) > 0 && n.X * fromAim.X + n.Y * fromAim.Y + n.Z * fromAim.Z >= 0;
            }
            if (reached) Advance(k);
        }

        private static RoutePoint Pseudo(Vector3 pos, float scale)
        {
            return new RoutePoint() { Pos = pos, Scale = scale, Index = -1, Path = -1 };
        }

        // Moves the kart on: the target becomes the point just reached, and the next point is chosen.
        private void Advance(KartEntry k)
        {
            RoutePoint cameFrom = k.Previous;
            if (cameFrom != null && cameFrom.Index >= 0)
            {
                k.History.Insert(0, cameFrom);
                if (k.History.Count > 3) k.History.RemoveAt(3);
            }
            k.JustReached = k.Target;
            k.Previous = k.Target;
            k.Target = ChooseNext(k, k.Target, cameFrom);
            k.Advanced = true;
            k.FramesSinceAdvance = 0;
            SetTargetLane(k);
            // A lane beyond 50% drifts back toward the centre on advances (AIPathPoint::calcPointOffsetDistance_).
            float noise = (float)(random.NextDouble() * 0.02 - 0.01) * ((k.TitleMask & 0x10) != 0 ? 3 : 1);
            if (k.Lane > 0.5f && noise > 0) k.Lane -= noise;
            if (k.Lane < -0.5f && noise < 0) k.Lane -= noise;
        }

        // AIPathPoint::calcNextTargetTrans, when the kart moves on to a target: a kart that is drifting then, heading to a
        // point with flag 0x01 or 0x04 or to a turn of more than about 11 degrees (|corner| <= 0.98), uses its lane plus
        // m_corner_line_shift (+0.15 / -0.4 / -0.7 for the levels, negated when the target's corner is 0 or more, which
        // puts the negative shifts on the inside of the turn) until the next point, and its lane creeps by rand(0, 0.01)
        // the same way; the aim is clamped to +-0.85.
        private void SetTargetLane(KartEntry k)
        {
            RoutePoint t = k.Target;
            k.LaneFor = t;
            k.TargetLane = k.Lane;
            if (t == null || battle || !k.Drifting || ((t.Flags & 0x05) == 0 && Math.Abs(t.Corner) > 0.98f)) return;
            float sign = t.Corner >= 0 ? -1 : 1;
            k.TargetLane = k.Lane + new float[] { 0.15f, -0.4f, -0.7f }[k.LevelVal] * sign;
            k.Lane += (float)random.NextDouble() * 0.01f * sign;
        }

        // --- Physics, with a KCL loaded. The speeds, acceleration, boosts, falling and the respawn's timing below come
        // from the game; the kart's sphere, the steps, turning in the air, water, gliding and cannons are guesses that make
        // the karts behave plausibly. What starts a respawn (fall collision, solid out-of-bounds floor, leaving the
        // checkpoints, Y below 0) and where the kart comes back follow the game (the research on ghost checkpoints,
        // Finding 4, the research on enemy AI behaviour, Finding 7, and measurements in game).
        private const float KartRadius = 12;         // the kart as a sphere, standing on the floor
        private const float StepUp = 20;             // highest step the kart drives up
        internal static float AirTurn = 0.3f;        // share of the turning rate in the air
        // Falling, measured in game (Toad Circuit, a kart dropped from 2500 units up): each frame the vertical speed moves
        // 1.5 % of the way to -10, so 0.15 per frame at first and 10 at most (v(t) = 10 (1 - e^(-t / 66.8)) matched every
        // frame). CPUs in the air on Toad Circuit gave 0.152 per frame, per frame, the same.
        private const float FallTerminal = 10, FallRate = 0.015f;
        // Let go by Lakitu, a kart falls from 2.81 per frame toward 4.6 (measured in game, same course).
        private const float RespawnDropSpeed = 2.81f, RespawnDropTerminal = 4.6f;
        private const float WaterGravity = 0.35f;    // share of the gravity under water
        private const float WaterMaxFallSpeed = 6;
        private const float WaterSpeed = 0.9f;       // share of the top speed under water
        private const float GlideSink = 2;           // units per frame down while gliding without a route
        private const float GlideRise = 1.5f;        // units per frame up toward a higher glider point
        private const float CannonSpeed = 2.5f;      // share of the top speed in a cannon
        // Boosts (VehicleMove::execDashOne and updateDashParam, values of the parameter block at 0x665508 read in game): a
        // Mushroom lasts 90 frames and a boost pad 60, and both raise the top speed to 1.2 x max(top speed, 7.6); a
        // mini-turbo raises it to 1.07 x the top speed, for 29 frames at level 1 and 88 at level 2 (kart stats: 26 to 32
        // and 83 to 94 on the CPUs' karts in game). A new boost keeps the stronger kind and the longer time.
        private const int MushroomBoost = 90, PadBoost = 60, MiniTurboBoost1 = 29, MiniTurboBoost2 = 88;
        // Measured in game on CPUs at 150cc: from a stop the speed grows by 0.0077 x (top - speed) per frame; above the
        // top speed (after a boost) it falls back by about 0.02 x the difference per frame. While a boost lasts, the speed
        // instead grows by 3 per frame up to the boost's top speed (VehicleMove::calcMoveControlCommon with the step of
        // updateDashParam, parameter block +0x29C = 3.0 read in game; +0x2A0 = 0.03 is only for dash type 0, not
        // simulated), so a boost reaches its top speed at once.
        private const float Acceleration = 0.0077f, Deceleration = 0.02f, BoostAcceleration = 3;

        private static void StartBoost(KartEntry k, int frames, bool strong)
        {
            k.BoostStrong |= strong;
            k.BoostFrames = Math.Max(k.BoostFrames, frames);
        }

        // The top speed while boosting, and one frame less of the boost.
        private static float BoostTop(KartEntry k)
        {
            float top = k.BoostStrong ? Math.Max(k.Speed, 7.6f) * 1.2f : k.Speed * 1.07f;
            if (--k.BoostFrames <= 0) k.BoostStrong = false;
            return top;
        }
        // A respawn, measured in game (Toad Circuit, a kart on solid out-of-bounds floor and one out of the checkpoints):
        // the kart goes on moving for 90 frames, is then put 70 units over its respawn spot and held there, and is let go
        // 61 frames later.
        private const int RespawnPlaceFrame = 90, RespawnReleaseFrame = 151;

        // One frame of falling toward terminal (a negative vertical speed), at rate of the difference.
        private static float Fall(float velY, float terminal, float rate)
        {
            return velY + (-terminal - velY) * rate;
        }

        private static float TerrainSpeed(ushort value)
        {
            switch (KCL.Types[value & 0x1F].ID)
            {
                case 5: return 0.65f;               // off-road
                case 6: return 0.4f;                // heavy off-road
                default: return 1;
            }
        }

        // Under water: below a WaterDive object (GOBJ 0x141). Most courses have one with scale 1, which is taken as a
        // water surface over the whole course at its height; a scaled one is taken as a box of its X and Z scale centred on
        // its position, its surface at the position's height and its depth the Y scale.
        private static bool InWater(SimpleKMP kmp, float x, float y, float z)
        {
            foreach (var o in kmp.Objects.Entries)
            {
                if (o.ObjectID != 0x141 || y > o.Pos.Y) continue;
                bool plane = Math.Abs(o.ScaleX - 1) < 0.01f && Math.Abs(o.ScaleZ - 1) < 0.01f;
                if (plane || (Math.Abs(x - o.Pos.X) <= o.ScaleX / 2 && Math.Abs(z - o.Pos.Z) <= o.ScaleZ / 2 && y >= o.Pos.Y - o.ScaleY)) return true;
            }
            return false;
        }

        private void StepPhysics(KartEntry k, SimpleKMP kmp, KCL kcl)
        {
            if (k.OutOfCheckpoints) { StartRespawn(k, kmp, "left the checkpoints"); return; }
            float x = k.Pos.X, y = k.Pos.Y, z = k.Pos.Z;
            k.Underwater = InWater(kmp, x, y + KartRadius, z);

            // Steering: along the glider route while gliding or in a cannon, else toward the aim point.
            Vector3 aim = AimPoint(k);
            GliderRoutes.GliderGroup.GliderEntry glidePoint = GlidePoint(k);
            bool flying = k.Mode == Modes.Gliding || k.Mode == Modes.Cannon;
            float turn = k.MaxTurn;
            if (k.Mode == Modes.Airborne) turn *= AirTurn;
            else if (k.Mode == Modes.Driving && KCL.Types[k.FloorValue & 0x1F].Kind == KCL.Kinds.Slippery) turn *= 0.6f;
            // Backing up: wheels straight while reversing (phase 0), then toward the spot on the route behind (phase 1).
            Vector3 steerTo = flying && glidePoint != null ? glidePoint.Pos : aim;
            if (k.BackPhase == 1) steerTo = BackTarget(k, aim);
            if (k.BackPhase != 0) Turn(k, steerTo, turn);

            // Speed: the kart's top speed, changed by the floor, boosts, water and cannons; in the air it keeps its speed.
            float top = k.Speed;
            if (k.Mode == Modes.Driving) top *= TerrainSpeed(k.FloorValue);
            bool boosting = k.BoostFrames > 0;
            if (boosting) top = BoostTop(k);
            if (k.Underwater) top *= WaterSpeed;
            if (k.Mode == Modes.Cannon) top = k.Speed * CannonSpeed;
            if (k.BackPhase == 0) top = -k.Speed * ReverseSpeed;
            float rate = top > k.CurSpeed ? Acceleration : Deceleration;
            if (k.Mode == Modes.Airborne) rate = 0;
            if (boosting && top > k.CurSpeed && k.Mode != Modes.Airborne) k.CurSpeed = Math.Min(top, k.CurSpeed + BoostAcceleration);
            else k.CurSpeed += (top - k.CurSpeed) * rate;
            // On the floor the kart covers its speed along the slope, so it moves less across the map on a steep one.
            float slope = k.Mode == Modes.Driving ? k.GroundSlope : 0;
            float step = k.CurSpeed / (float)Math.Sqrt(1 + slope * slope);
            x += k.ForwardX * step;
            z += k.ForwardZ * step;

            // Height: gravity in the air, a slow descent toward the glider points while gliding, straight at them in a
            // cannon.
            float prevY = y;
            switch (k.Mode)
            {
                case Modes.Cannon:
                    if (glidePoint == null) { k.Mode = Modes.Gliding; break; }
                    float hd = (float)Math.Sqrt((glidePoint.Pos.X - x) * (glidePoint.Pos.X - x) + (glidePoint.Pos.Z - z) * (glidePoint.Pos.Z - z));
                    k.VelY = Math.Max(-k.CurSpeed, Math.Min(k.CurSpeed, (glidePoint.Pos.Y - y) * k.CurSpeed / Math.Max(1, hd)));
                    break;
                case Modes.Gliding:
                    float want = -GlideSink;
                    if (glidePoint != null)
                        want = Math.Max(-2 * GlideSink, Math.Min(k.GlideGroup.PreventRaising ? 0 : GlideRise, (glidePoint.Pos.Y - y) / 20));
                    k.VelY += (want - k.VelY) * 0.1f;
                    break;
                case Modes.Airborne:
                    if (k.RespawnDrop) k.VelY = Fall(k.VelY, RespawnDropTerminal, FallRate);
                    else if (k.Underwater) k.VelY = Fall(k.VelY, WaterMaxFallSpeed, FallRate * WaterGravity);
                    else k.VelY = Fall(k.VelY, FallTerminal, FallRate);
                    break;
            }
            if (k.Mode != Modes.Driving) y += k.VelY;

            // Walls push the kart sideways, and slow it a little.
            float pushX, pushZ;
            bool wall = kcl.PushOutOfWalls(ref x, y + KartRadius, ref z, KartRadius, out pushX, out pushZ);
            if (wall) k.CurSpeed *= 0.97f;

            // The floor: a driving kart follows it down slopes and up steps and slopes of any steepness (in the game karts
            // drive up any angle, even upside down, which no course uses and is not simulated), and leaves it (keeping the
            // slope's vertical speed) where it drops away faster than the kart would fall: off the top of a ramp or a bump,
            // or over a crest sharper than about 8.5 degrees (a guess); a kart in the air lands on it.
            ushort value;
            KCL.Triangle floorTri;
            float? floor = kcl.FloorBelow(x, z, Math.Max(y, prevY) + StepUp + step * 4, out value, out floorTri);
            if (k.Mode == Modes.Driving)
            {
                float ballistic = y + Fall(k.GroundVelY, FallTerminal, FallRate);
                if (floor != null && floor.Value >= ballistic - Math.Max(1, k.CurSpeed * 0.15f))
                {
                    // The vertical speed is that of the floor's slope along the way the kart goes, so that a step it
                    // climbs does not throw it up.
                    k.GroundSlope = -(floorTri.NX * k.ForwardX + floorTri.NZ * k.ForwardZ) / floorTri.NY;
                    k.GroundVelY = k.GroundSlope * step;
                    k.GroundNormal = new Vector3(floorTri.NX, floorTri.NY, floorTri.NZ);
                    y = floor.Value;
                    k.FloorValue = value;
                }
                else
                {
                    k.VelY = Math.Max(k.GroundVelY, -k.CurSpeed);
                    k.Mode = Modes.Airborne;
                    if (k.GliderArmed) StartGlide(k, kmp, false);
                }
            }
            else if (floor != null && y <= floor.Value)
            {
                y = floor.Value;
                k.VelY = k.GroundVelY = 0;
                k.RespawnDrop = false;
                k.Mode = Modes.Driving;
                k.FloorValue = value;
                k.GlideGroup = null;
            }
            k.Pos = new Vector3(x, y, z);
            UpdateBodyUp(k);

            // What the floor and the collision the kart touches do.
            if (k.Mode == Modes.Driving)
            {
                k.LastGround = k.Pos;
                // calcMoveControlGnd: on the ground, without the glider ready, only VALLEY variant 5 is ignored.
                if (!k.GliderArmed) k.ValleyIgnore = 0x20;
                KCL.TypeInfo info = KCL.Types[value & 0x1F];
                // Boost and glider pads count when the kart's sphere touches them, not only under its centre: they are
                // often thin strips.
                ushort pad;
                float probe = y + KartRadius - 2;
                if (kcl.Touches(x, probe, z, KartRadius, t => t.Kind == KCL.Kinds.Boost, out pad)) StartBoost(k, PadBoost, true);
                if (kcl.Touches(x, probe, z, KartRadius, t => t.Kind == KCL.Kinds.GliderPad, out pad))
                {
                    // Variant 1 opens the glider at once (EveryFileExplorer: "Instant Glider Activation").
                    if (!k.GliderArmed) ArmValley(k, (pad >> 5) & 7);
                    k.GliderArmed = true;
                    if (((pad >> 5) & 7) == 1) { k.Mode = Modes.Airborne; k.VelY = 3; StartGlide(k, kmp, false); }
                }
                else k.GliderArmed = false;
                if (info.Kind == KCL.Kinds.SolidOutOfBounds) { StartRespawn(k, kmp, "out-of-bounds floor"); return; }
            }
            // A VALLEY (25) or VALLEY2 (29) triangle calls Lakitu unless its variant is ignored (calcGndCollision).
            ushort touched;
            if (kcl.Touches(x, y + KartRadius, z, KartRadius, t => t.Kind == KCL.Kinds.Fall || t.Kind == KCL.Kinds.SolidOutOfBounds,
                            v => KCL.Types[v & 0x1F].Kind != KCL.Kinds.Fall || (k.ValleyIgnore & (1 << ((v >> 5) & 7))) == 0, out touched))
            {
                StartRespawn(k, kmp, "fall collision " + (touched & 0x1F));
                return;
            }
            if (!flying && kcl.Touches(x, y + KartRadius, z, KartRadius, t => t.Kind == KCL.Kinds.GliderActivator, out touched))
            {
                ArmValley(k, (touched >> 5) & 7);
                if (k.Mode == Modes.Driving) k.Mode = Modes.Airborne;
                StartGlide(k, kmp, true);
            }
            // VehicleMove::calcPosAtt: below Y = 0 the kart calls Lakitu.
            if (y < 0) { StartRespawn(k, kmp, "below Y 0"); return; }
            // In a race, the game's checkpoint search; when it finds no quad, the kart has left the course and respawns
            // on the next frame (Vehicle::calcMove, Out_of_Checkpoint_Area).
            if (checkpoints != null)
            {
                if (checkpoints.Update(k.CheckpointState, x, z)) k.Checkpoint = checkpoints.Points[k.CheckpointState.Previous].Entry;
                else k.OutOfCheckpoints = true;
            }
            AdvanceTarget(k, aim);
            if (k.Mode == Modes.Gliding || k.Mode == Modes.Cannon) AdvanceGlide(k);
            if (k.Target != null) UpdateStuck(k, kmp, AimPoint(k), wall);
        }

        // VehicleMove::calcGndNrm: the kart's up moves toward the floor's normal by a rate that grows by 0.02 per frame on
        // the floor (0x665108 +0x368, read in game; plus a share of +0xDA4, not simulated) up to 1, so that it follows the
        // floor at once after a while on it. In the air the rate eases toward 0.01 (0x665508 +0x220; the easing, +0xD98,
        // is a guess) and the target leans from the last floor's normal to straight up over 120 frames (+0x21C = 1/120).
        private static void UpdateBodyUp(KartEntry k)
        {
            Vector3 up = new Vector3(0, 1, 0), target;
            if (k.Mode == Modes.Driving)
            {
                k.AirFrames = 0;
                k.NormalRate = Math.Min(1, k.NormalRate + 0.02f);
                target = k.GroundNormal;
            }
            else
            {
                k.AirFrames++;
                k.NormalRate += (0.01f - k.NormalRate) * 0.1f;
                target = Add(k.GroundNormal, Add(up, k.GroundNormal, -1), Math.Min(1, k.AirFrames / 120f));
            }
            Vector3 n = Normalize(Add(k.BodyUp, Add(target, k.BodyUp, -1), k.NormalRate));
            if (Length(n) > 0) k.BodyUp = n;
        }

        // startWingReady: a glide from a glider pad or activator of variant 5, 6 or 7 makes that VALLEY variant call
        // Lakitu and the other two of them not.
        private static void ArmValley(KartEntry k, int variant)
        {
            if (variant == 5) k.ValleyIgnore = (k.ValleyIgnore & ~0x20) | 0xC0;
            else if (variant == 6) k.ValleyIgnore = (k.ValleyIgnore & ~0x40) | 0xA0;
            else if (variant == 7) k.ValleyIgnore = (k.ValleyIgnore & ~0x80) | 0x60;
        }

        // --- Drifting, as AIControlBase::setBasicDriveInfo_ and AIDriftDrive decide it for race CPUs (the research on
        // enemy AI behaviour, Drift setting and Finding 6). A drift starts toward a target in a bend (|corner| <= 0.998)
        // whose drift setting is not 1 or 2, within 400 units, once a draw from the CPU's probability table passes (one
        // draw until those conditions fail; Drift Wizards skip it). It ends on reaching a point that says so, and when
        // AIDriftDrive::calcSteerDrift_ gives it up (constants at 0x32F834..0x32FA34, read from the disassembly):
        // - the target is a drift corner bending the other way than the one the drift started for;
        // - the body points less than 0.65 (cosine) toward the aim, or the direction of travel less than 0.4 (0.2 with
        //   flag 0x10 on the target), or body and direction of travel are more than acos 0.645 apart;
        // - without flag 0x10, after 120 frames with mini-turbo level 1 charged or at level 2, once the body points more
        //   than 0.7 toward the aim.
        // The drift charges VehicleMove +0xF08 by 2 to 6 per frame (the CPUs measured in game charged about 2 per frame);
        // mini-turbo level 1 is at 220 and level 2 at 460 (parameter block 0x665508 +0x12C / +0x130). The body turns
        // about 42 degrees into the turn; how fast it turns in and back is a guess (DriftBodyRate).
        internal const float MiniTurboLv1 = 220, MiniTurboLv2 = 460, DriftChargeRate = 2, DriftAngle = 42, DriftBodyRate = 3;
        // EnemyProbabilityTable{Easy,Normal,Hard}.bin (RaceCommon/Enemy), columns Drift and MiniTurbo, rows rank 1 to 8.
        private static readonly int[][] DriftChance = { new[] { 60, 50, 40, 20, 10, 0, 0, 0 }, new[] { 90, 80, 70, 60, 40, 20, 0, 0 }, new[] { 100, 100, 90, 80, 70, 50, 30, 30 } };
        private static readonly int[][] MiniTurboChance = { new[] { 30, 20, 20, 10, 0, 0, 0, 0 }, new[] { 60, 60, 50, 40, 40, 30, 0, 0 }, new[] { 100, 100, 90, 80, 70, 60, 50, 50 } };

        private void StepDrift(KartEntry k)
        {
            if (battle || (k.TitleMask & 0x1100) != 0) k.Drifting = false;
            else UpdateDrift(k);
            // The body turns toward its drift angle, or back in line with the direction of travel.
            float want = k.Drifting ? DriftAngle * k.DriftSide : 0;
            k.BodyAngle += Math.Max(-DriftBodyRate, Math.Min(DriftBodyRate, want - k.BodyAngle));
        }

        private void UpdateDrift(KartEntry k)
        {
            RoutePoint t = k.Target, cur = k.JustReached;
            bool wizard = (k.TitleMask & 0x20) != 0;
            if (k.Drifting)
            {
                k.DriftCharge += DriftChargeRate;
                if (k.DriftCharge >= MiniTurboLv1) k.DriftOverLv1Frames++;
                else k.DriftOverLv1Frames = 0;
                if (k.Advanced && cur != null)
                {
                    // isTimeToEndDrift and m_allow_miniturbo, on the frame a point is reached.
                    bool end = cur.Drift != 0 && (cur.Drift == 1 || cur.Drift == 2 || cur.Corner * t.Corner < 0 || Math.Abs(t.Corner) > 0.998f);
                    k.AllowMiniTurbo = !end || (cur.Drift != 2 && (wizard || random.Next(100) < MiniTurboChance[k.LevelVal][k.Rank - 1]));
                    if (end) { EndDrift(k); return; }
                }
                // calcSteerDrift_: the drift side (AIDriftDrive +0x20) times the target's corner below 0 ends it.
                if (Math.Abs(t.Corner) <= 0.998f && t.Corner * k.DriftSide > 0) { EndDrift(k); return; }
                bool hold = (t.Flags & 0x10) != 0;
                Vector3 aim = AimPoint(k);
                float dx = aim.X - k.Pos.X, dz = aim.Z - k.Pos.Z, dl = (float)Math.Sqrt(dx * dx + dz * dz);
                if (dl <= 0) return;
                dx /= dl; dz /= dl;
                float travel = dx * k.ForwardX + dz * k.ForwardZ, body = dx * k.BodyX + dz * k.BodyZ;
                float apart = (float)Math.Cos(k.BodyAngle * Math.PI / 180);
                bool late = !hold && (k.DriftOverLv1Frames > 120 || k.DriftCharge >= MiniTurboLv2) && body > 0.7f;
                if (body < 0.65f || travel < (hold ? 0.2f : 0.4f) || apart < 0.645f || late) EndDrift(k);
                return;
            }
            if (k.Mode != Modes.Driving) return;
            Vector3 d = Add(AimPoint(k), k.Pos, -1);
            bool can = t.Drift != 1 && t.Drift != 2 && Math.Abs(t.Corner) <= 0.998f && d.X * d.X + d.Y * d.Y + d.Z * d.Z < 160000;
            if (!can) { k.DriftDrawn = false; return; }
            if (!wizard)
            {
                if (k.DriftDrawn) return;
                k.DriftDrawn = true;
                if (random.Next(100) >= DriftChance[k.LevelVal][k.Rank - 1]) return;
            }
            k.Drifting = true;
            k.DriftCharge = 0;
            k.DriftOverLv1Frames = 0;
            k.DriftSide = t.Corner >= 0 ? -1 : 1;
            k.AllowMiniTurbo = true;
        }

        private static void EndDrift(KartEntry k)
        {
            if (k.AllowMiniTurbo && k.DriftCharge >= MiniTurboLv1)
                StartBoost(k, k.DriftCharge >= MiniTurboLv2 ? MiniTurboBoost2 : MiniTurboBoost1, false);
            k.Drifting = false;
        }

        // --- Mushrooms (AIItemRace::stateKinoko and stateInitKinoko; the research on enemy AI behaviour, Mushroom
        // setting and Finding 6). A CPU waits rand(180) frames (rand(50) in battle; in a race held at 10 or more while
        // its target is a junction point) and then uses a Mushroom at a point with mushroom setting 0; a single Mushroom
        // is also used whenever the kart reaches such a point. Never while drifting. Aviators use them while gliding and
        // Dolphins under water. A Golden Mushroom is taken as one that never runs out; the boost length is a guess.
        private void ReceiveMushroom(KartEntry k)
        {
            k.MushroomTimer = random.Next(battle ? 50 : 180);
            k.GoForShortcut = !battle && k.ItemVal != 3 && (k.TitleMask & 0xC0) == 0 && (k.LevelVal == 2 || (k.LevelVal == 1 && random.Next(2) == 0));
        }

        private static bool SingleMushroom(KartEntry k)
        {
            return k.ItemVal == 1 || k.ItemVal == 4;
        }

        private void UseMushroom(KartEntry k)
        {
            StartBoost(k, MushroomBoost, true);
            k.UsedItem = true;
            if (k.ItemVal == 1 || k.ItemVal == 2) k.MushroomCount--;
            else if (k.ItemVal == 3) k.MushroomTimer = random.Next(battle ? 50 : 180);
            else ReceiveMushroom(k);
        }

        private void StepItems(KartEntry k)
        {
            if (k.MushroomCount <= 0 || k.UsedItem) return;
            if (!battle && k.Target.Junction) { if (k.MushroomTimer > 10) k.MushroomTimer--; }
            else if (k.MushroomTimer > 0) k.MushroomTimer--;
            if (k.Drifting) return;
            int title = k.TitleMask;
            if (!battle && (title & 0xC0) != 0)
            {
                if (k.MushroomTimer <= 0 && (((title & 0x40) != 0 && k.Mode == Modes.Gliding) || ((title & 0x80) != 0 && k.Underwater))) UseMushroom(k);
                return;
            }
            if (k.JustReached == null || k.JustReached.Mushroom != 0) return;
            if (k.MushroomTimer <= 0 || (k.Advanced && SingleMushroom(k))) UseMushroom(k);
        }

        // --- Stuck karts, as the game handles its CPUs (the research on enemy AI behaviour, When a CPU gets stuck and
        // Findings 5, 11 and 12): a re-route at 450 frames without reaching a point, and every 30 frames against a wall; the
        // height re-route of flag 0x02 (and of karts gliding or in the air for 20 frames); a back-up after 600 slow frames
        // (below 10 % of the top speed, counted 3 times against a wall); Lakitu at 901 frames without reaching a point,
        // after 600 frames of backing up, or at the sixth re-route in a row at the same spot. The reversing speed is a
        // guess.
        private const float ReverseSpeed = 0.3f;

        private static void ResetStuck(KartEntry k)
        {
            k.SlowFrames = k.NoAdvanceFrames = k.BackFrames = k.WallFrames = k.FramesSinceAdvance = 0;
            k.BackPhase = -1;
            k.BackRate = 0;
        }

        private void UpdateStuck(KartEntry k, SimpleKMP kmp, Vector3 aim, bool wall)
        {
            k.AirRate = k.Mode != Modes.Driving ? Math.Min(1, k.AirRate + 0.05f) : Math.Max(0, k.AirRate - 0.05f);
            if (!k.Advanced) k.FramesSinceAdvance++;

            // AIPathHandler::update: no advance for more than 20 frames and the target steeply above or below.
            Vector3 d = Add(aim, k.Pos, -1);
            bool heightCheck = k.Mode == Modes.Gliding || k.Mode == Modes.Cannon || k.AirRate >= 1 ||
                               (k.JustReached != null && (k.JustReached.Flags & 0x02) != 0);
            if (k.FramesSinceAdvance > 20 && heightCheck && d.Y * d.Y > 0.3f * (d.X * d.X + d.Z * d.Z))
            {
                Reroute(k, false);
                if (k.Mode == Modes.Respawning || k.Target == null) return;
                Advance(k);
            }

            // AIEngine::update: against a wall for more than 30 frames in a row.
            if (wall && ++k.WallFrames > 30)
            {
                k.WallFrames = 0;
                Reroute(k, false);
                if (k.Mode == Modes.Respawning) return;
            }
            else if (!wall) k.WallFrames = 0;

            if (k.BackPhase < 0)
            {
                // AIStuck::stateCheckStuck.
                if (k.CurSpeed / Math.Max(0.001f, k.Speed) < 0.1f) k.SlowFrames += wall ? 3 : 1;
                else k.SlowFrames = 0;
                if (k.Advanced) k.NoAdvanceFrames = 0;
                else k.NoAdvanceFrames++;
                if (k.NoAdvanceFrames == 450) Reroute(k, false);
                else if (k.NoAdvanceFrames > 900) { StartRespawn(k, kmp, "Lakitu: no progress"); return; }
                if (k.SlowFrames > 600)
                {
                    k.SlowFrames = k.BackFrames = 0;
                    k.BackPhase = 0;
                    k.BackRate = 0;
                }
                return;
            }
            // AIStuck::stateBackPathPoint.
            if (k.Advanced) { k.BackPhase = -1; return; }
            k.BackFrames++;
            float rateMax = battle ? 2 : 4;
            if (k.BackPhase == 0)
            {
                k.BackRate += 0.04f;
                if (k.BackRate >= rateMax) { k.BackRate = rateMax; k.BackPhase = 1; }
            }
            else
            {
                Vector3 spot = Add(BackTarget(k, aim), k.Pos, -1);
                float dist = Length(spot);
                if (k.BackPhase == 1 && dist < 100) { k.BackPhase = 2; k.BackFrames = 0; }
                if (k.BackFrames % 120 == 0) { Reroute(k, false); if (k.Mode == Modes.Respawning || k.BackPhase < 0) return; }
                if (k.BackPhase == 2 && Math.Abs(spot.X * k.ForwardX + spot.Z * k.ForwardZ) > 0.8f * dist) { k.BackPhase = -1; return; }
            }
            if (k.BackFrames > 600) StartRespawn(k, kmp, "Lakitu: stuck backing up");
        }

        // AIPathPoint::getInterpolateTarget: the aim slid back along the route by the back-up rate, from the aim point
        // to the point last reached (0 to 1), then to the three points reached before it (1 to 4).
        private static Vector3 BackTarget(KartEntry k, Vector3 aim)
        {
            List<Vector3> points = new List<Vector3>() { aim };
            if (k.Previous != null) points.Add(k.Previous.Pos);
            foreach (var h in k.History) points.Add(h.Pos);
            float r = Math.Max(0, k.BackRate);
            int i = (int)Math.Floor(r);
            if (i >= points.Count - 1) return points[points.Count - 1];
            float f = r - i;
            return Add(points[i], Add(points[i + 1], points[i], -1), f);
        }

        // onAIFall: the route point nearest to the kart (findNearEnemyPoint); the kart continues from it only when it is on
        // another path than its target (always when forced, as for a kart the game drives for a player). In a race it
        // then targets the first point along the route more than 180 units from the kart.
        private void Reroute(KartEntry k, bool force)
        {
            RoutePoint nearest = NearestRoutePoint(k.Pos);
            if (nearest == null || (!force && k.Target != null && nearest.Path == k.Target.Path)) return;
            RoutePoint t = nearest;
            if (!battle)
            {
                int guard = 0;
                while (t.Next.Count > 0 && guard++ < 100000)
                {
                    float dx = t.Pos.X - k.Pos.X, dy = t.Pos.Y - k.Pos.Y, dz = t.Pos.Z - k.Pos.Z;
                    if (dx * dx + dy * dy + dz * dz > 180 * 180) break;
                    t = t.Next[0];
                }
            }
            k.Previous = Pseudo(k.Pos, t.Scale);
            k.Target = t;
            k.JustReached = null;
            EnterBattlePath(k, t, null);
            // AIStuck::onAIFall: the checks start over; the sixth relocation in a row within 10 units calls Lakitu.
            ResetStuck(k);
            if (k.LastRelocation != null && Length(Add(k.Pos, k.LastRelocation.Value, -1)) < 10)
            {
                if (k.FallCount > 3) StartRespawn(k, (Application.OpenForms[0] as Form1).getKayEmPee(), "Lakitu: re-routed six times");
                else k.FallCount++;
            }
            else k.FallCount = 0;
            k.LastRelocation = k.Pos;
        }

        // --- Gliding. A glide follows the glider route (GLPT) whose first point is nearest to the kart, among the
        // routes no other route leads into; a glider activator on a cannon route (CannonSection) shoots the kart
        // along it instead. Without a glider route the kart sinks slowly toward its aim point.
        private void StartGlide(KartEntry k, SimpleKMP kmp, bool activator)
        {
            k.GliderArmed = false;
            k.GlideGroup = null;
            k.GlideIndex = 0;
            var groups = kmp.GliderRoutes.Entries;
            float best = 3000 * 3000;
            foreach (var g in groups)
            {
                if (g.Entries.Count == 0) continue;
                bool isStart = true;
                if (g.Previous != null) foreach (var p in g.Previous) if (p >= 0 && p < groups.Count) isStart = false;
                if (!isStart) continue;
                float dx = g.Entries[0].Pos.X - k.Pos.X, dz = g.Entries[0].Pos.Z - k.Pos.Z;
                if (dx * dx + dz * dz < best) { best = dx * dx + dz * dz; k.GlideGroup = g; }
            }
            k.Mode = activator && k.GlideGroup != null && k.GlideGroup.CannonSection ? Modes.Cannon : Modes.Gliding;
            if (k.Mode == Modes.Gliding) k.VelY = Math.Max(k.VelY, 1);
        }

        private static GliderRoutes.GliderGroup.GliderEntry GlidePoint(KartEntry k)
        {
            if (k.GlideGroup == null || k.GlideIndex >= k.GlideGroup.Entries.Count) return null;
            return k.GlideGroup.Entries[k.GlideIndex];
        }

        // The next glider point within 250 units, or once past the plane square to the route there; at the end of a
        // route a random next route, or the end of the glide on a ForceToRoute route.
        private void AdvanceGlide(KartEntry k)
        {
            var p = GlidePoint(k);
            if (p == null) return;
            Vector3 prev = k.GlideIndex > 0 ? k.GlideGroup.Entries[k.GlideIndex - 1].Pos : k.Pos;
            float dx = p.Pos.X - k.Pos.X, dz = p.Pos.Z - k.Pos.Z;
            float sx = p.Pos.X - prev.X, sz = p.Pos.Z - prev.Z;
            if (dx * dx + dz * dz > 250 * 250 && dx * sx + dz * sz > 0) return;
            k.GlideIndex++;
            if (k.GlideIndex < k.GlideGroup.Entries.Count) return;
            var groups = (Application.OpenForms[0] as Form1).getKayEmPee().GliderRoutes.Entries;
            List<GliderRoutes.GliderGroup> nexts = new List<GliderRoutes.GliderGroup>();
            if (k.GlideGroup.Next != null)
                foreach (var n in k.GlideGroup.Next) if (n >= 0 && n < groups.Count && groups[n].Entries.Count > 0) nexts.Add(groups[n]);
            bool force = k.GlideGroup.ForceToRoute;
            if (nexts.Count > 0)
            {
                k.GlideGroup = nexts[random.Next(nexts.Count)];
                k.GlideIndex = 0;
                return;
            }
            k.GlideGroup = null;
            if (force || k.Mode == Modes.Cannon) k.Mode = force ? Modes.Airborne : Modes.Gliding;
        }

        // --- Respawning. In a race the kart comes back at the respawn point of its current checkpoint, in a battle (or
        // without one) at the respawn point nearest to it; its route continues from the route point nearest to the
        // respawn point (within 500 units horizontally and the point's Max search Y offset), or else from the first
        // point of path 0. The kart falls on for 40 frames, is put on the respawn point at frame 100 and let go at
        // frame 130 (these times are guesses).
        private void StartRespawn(KartEntry k, SimpleKMP kmp, string reason)
        {
            k.Mode = Modes.Respawning;
            ResetStuck(k);
            k.RespawnFrames = 0;
            k.RespawnReason = reason;
            k.RespawnCount++;
            k.GlideGroup = null;
            k.GliderArmed = false;
            k.BoostFrames = 0;
            k.BoostStrong = false;
            k.Drifting = k.DriftDrawn = false;
            k.BodyAngle = 0;
            k.ValleyIgnore = 0x20;
            k.RespawnAt = null;
            k.OutOfCheckpoints = false;
            var points = kmp.RespawnPoints.Entries;
            // Kart::Unit::startJugemRecover: in a race the respawn point of the checkpoint GetKartJugemRecoverSectorIndex
            // gives, then onOutOfBounds puts the kart in that point's checkpoint.
            CheckpointSearch.Point respawnCheckpoint = checkpoints != null ? checkpoints.RespawnCheckpoint(k.CheckpointState) : null;
            if (respawnCheckpoint != null && respawnCheckpoint.Jugem < points.Count)
            {
                k.RespawnAt = points[respawnCheckpoint.Jugem];
                checkpoints.OnRespawn(k.CheckpointState, respawnCheckpoint.Jugem);
            }
            if (k.RespawnAt == null)
            {
                float best = float.MaxValue;
                foreach (var p in points)
                {
                    float dx = p.Pos.X - k.Pos.X, dy = p.Pos.Y - k.Pos.Y, dz = p.Pos.Z - k.Pos.Z;
                    if (dx * dx + dy * dy + dz * dz < best) { best = dx * dx + dy * dy + dz * dz; k.RespawnAt = p; }
                }
            }
        }

        // kcl is null without physics.
        private void StepRespawn(KartEntry k, SimpleKMP kmp, KCL kcl)
        {
            bool physics = kcl != null;
            k.RespawnFrames++;
            if (k.RespawnFrames < RespawnPlaceFrame)
            {
                // Until Lakitu takes it, the kart goes on, falling, and stops on the floor it lands on.
                float x = k.Pos.X + k.ForwardX * k.CurSpeed, z = k.Pos.Z + k.ForwardZ * k.CurSpeed;
                k.VelY = Fall(k.VelY, FallTerminal, FallRate);
                float y = k.Pos.Y + k.VelY;
                ushort value;
                float? floor = physics ? kcl.FloorBelow(x, z, k.Pos.Y + StepUp, out value) : null;
                if (floor != null && y <= floor.Value)
                {
                    y = floor.Value;
                    k.VelY = 0;
                    k.CurSpeed = 0;
                }
                k.Pos = new Vector3(x, y, z);
                return;
            }
            if (k.RespawnFrames == RespawnPlaceFrame)
            {
                Vector3 at = k.RespawnAt != null ? RespawnSpot(k.RespawnAt, k.Index) : k.LastGround;
                k.Pos = at;
                if (k.RespawnAt != null) k.Direction = k.RespawnAt.RotationY;
                k.CurSpeed = 0;
                k.VelY = 0;
                k.GroundNormal = k.BodyUp = new Vector3(0, 1, 0);
                k.CameraSmoothed = false;                // the camera jumps with the kart
                if (checkpoints != null && k.CheckpointState.Previous >= 0) k.Checkpoint = checkpoints.Points[k.CheckpointState.Previous].Entry;
                RoutePoint start = NearestRoutePoint(at);
                if (start == null && route.Count > 0 && route[0].Count > 0) start = route[0][0];
                if (start != null)
                {
                    k.Target = start;
                    k.Previous = Pseudo(at, start.Scale);
                    k.JustReached = null;
                    EnterBattlePath(k, start, null);
                }
                // AIStuck::onOutOfBoundsInner: the checks start over, and the position is kept for the next relocation.
                ResetStuck(k);
                k.LastRelocation = at;
            }
            if (k.RespawnFrames >= RespawnReleaseFrame)
            {
                k.Mode = physics ? Modes.Airborne : Modes.Driving;
                k.VelY = physics ? -RespawnDropSpeed : 0;
                k.RespawnDrop = physics;
                k.RespawnReason = "";
            }
        }

        // Kart::Unit::startJugemRecover: kart i (its player index) comes back at the respawn point plus
        // MapdataJugemPoint::calcBias, (x, 0, z) x 24 from two tables of the game, turned by the respawn point's rotation;
        // Vehicle::startJugemRecover then lifts it by 70 (Kart parameter block +0x1F8, set by the static initializer).
        // The rotation order X, Y, Z (sead's makeR) is assumed.
        private static readonly float[] BiasX = { 0, 1, -1, 0, 1, -1, -1, 1 }, BiasZ = { 0, 0, 0, 1, 1, 1, -1, -1 };
        public const float RespawnBias = 24, RespawnLift = 70;

        public static Vector3 RespawnSpot(RespawnPoints.RespawnEntry p, int kart)
        {
            double rx = p.RotationX * Math.PI / 180, ry = p.RotationY * Math.PI / 180, rz = p.RotationZ * Math.PI / 180;
            double x = BiasX[kart % 8] * RespawnBias, z = BiasZ[kart % 8] * RespawnBias, y = 0;
            double y1 = y * Math.Cos(rx) - z * Math.Sin(rx), z1 = y * Math.Sin(rx) + z * Math.Cos(rx);
            double x2 = x * Math.Cos(ry) + z1 * Math.Sin(ry), z2 = -x * Math.Sin(ry) + z1 * Math.Cos(ry);
            double x3 = x2 * Math.Cos(rz) - y1 * Math.Sin(rz), y3 = x2 * Math.Sin(rz) + y1 * Math.Cos(rz);
            return new Vector3(p.PositionX + (float)x3, p.PositionY + (float)y3 + RespawnLift, p.PositionZ + (float)z2);
        }

        // findNearEnemyPoint (the research on enemy AI behaviour, Finding 6, max_search_y_offset): the first 128 route
        // points, in list order, within 500 units of pos horizontally; of those whose height difference passes their Max
        // search Y offset (0: no limit, negative: 75 units, positive: that many units), the nearest in 3D.
        private RoutePoint NearestRoutePoint(Vector3 pos)
        {
            RoutePoint best = null;
            float bestDist = float.MaxValue;
            int collected = 0;
            foreach (var path in route)
                foreach (var p in path)
                {
                    float dx = p.Pos.X - pos.X, dy = p.Pos.Y - pos.Y, dz = p.Pos.Z - pos.Z;
                    if (dx * dx + dz * dz >= 500 * 500) continue;
                    if (++collected > 128) return best;
                    float limit = p.MaxSearchY == 0 ? float.MaxValue : (p.MaxSearchY < 0 ? 75 : p.MaxSearchY);
                    if (Math.Abs(dy) > limit) continue;
                    float d = dx * dx + dy * dy + dz * dz;
                    if (d >= bestDist) continue;
                    bestDist = d;
                    best = p;
                }
            return best;
        }

        // The route points the game uses in a race (the enemy points and the inserted ones) of the paths of the selected
        // enemy points, or of every path when all is set. In a battle the enemy points are used as they are.
        public static void RenderRoutePoints(SimpleKMP kmp, List<object> selected, bool all)
        {
            if (kmp.CheckPoints.Entries.Count == 0) return;
            var groups = kmp.EnemyRoutes.Entries;
            List<int> paths = new List<int>();
            for (int g = 0; g < groups.Count; g++)
            {
                if (all) { paths.Add(g); continue; }
                foreach (var e in groups[g].Entries)
                    if (selected.Contains(e)) { paths.Add(g); break; }
            }
            if (paths.Count == 0) return;
            var route = BuildRoute(kmp, true);
            ViewPlaneHandler vph = (Application.OpenForms[0] as Form1).vph;
            VisualSettings Settings = (Application.OpenForms[0] as Form1).Settings;
            foreach (int g in paths)
            {
                foreach (var p in route[g])
                {
                    if (p.Inserted) Gl.glColor4f(1f, 1f, 1f, 0.9f);
                    else Gl.glColor4f(1f, 1f, 0f, 0.9f);
                    vph.drawPoint(p.Pos, Math.Max(2f, Settings.PointSize / 2));
                }
            }
        }

        // The checkpoint areas of the selected karts: in blue the current checkpoint (a respawn uses it), in light blue the
        // checkpoint kept while the kart is in no checkpoint area (CheckpointSearch.State.LastValid), when it is another.
        // Only physics (a loaded KCL) runs the checkpoint search, so without it nothing is drawn.
        private void RenderCheckpointAreas()
        {
            Form1 form = Application.OpenForms[0] as Form1;
            if (checkpoints == null || form.Collision == null) return;
            foreach (var k in Entries)
            {
                if (!k.Enabled || !k.Physics || !form.SelectedDots.Contains(k)) continue;
                int current = k.CheckpointState.Current, lastValid = k.CheckpointState.LastValid;
                if (lastValid >= 0 && lastValid != current) RenderCheckpointArea(form.vph, lastValid, 0.55f, 0.8f, 1f);
                if (current >= 0) RenderCheckpointArea(form.vph, current, 0f, 0.3f, 1f);
            }
        }

        private void RenderCheckpointArea(ViewPlaneHandler vph, int index, float r, float g, float b)
        {
            if (index >= checkpoints.Points.Count) return;
            var c = checkpoints.Points[index];
            float height = c.Entry.GetHeight();
            Gl.glBindTexture(Gl.GL_TEXTURE_2D, 0);
            Gl.glEnable(Gl.GL_BLEND);
            Gl.glBlendFunc(Gl.GL_SRC_ALPHA, Gl.GL_ONE_MINUS_SRC_ALPHA);
            foreach (int j in c.Nexts)
            {
                var m = checkpoints.Points[j];
                Gl.glColor4f(r, g, b, 0.45f);
                Gl.glBegin(Gl.GL_QUADS);
                vph.drawXZ(c.LX, c.LZ, height);
                vph.drawXZ(m.LX, m.LZ, height);
                vph.drawXZ(m.RX, m.RZ, height);
                vph.drawXZ(c.RX, c.RZ, height);
                Gl.glEnd();
            }
        }

        public TreeNode GetTreeNode()
        {
            TreeNode node = new TreeNode("Kart Simulator");
            node.Tag = this;
            return node;
        }

        public void Render(bool picking)
        {
            if (!Visible) return;
            if (!picking) RenderCheckpointAreas();
            foreach (var k in Entries)
            {
                if (!k.Enabled) continue;
                if (picking) k.RenderPicking();
                else k.Render();
            }
        }

        // Its eight entries can be neither added to nor removed.
        public object Add() { return null; }
        public void Add(object entry) { }
        public void Remove(object entry) { }
        public object Insert(int row) { return null; }
        public void Insert(int row, object entry) { }
        public object GetEntries() { return Entries; }
        public void New() { }
        public int IndexOf(object entry) { return Entries.IndexOf(entry as KartEntry); }
        public int GetCount() { return Entries.Count; }
        public bool GetVisibility() { return Visible; }
        public void SetVisibility(bool visible) { Visible = visible; }
    }
}
