using KMPExpander.Class.SimpleKMPs;
using LibCTR.Collections;
using System;
using System.Collections.Generic;
using System.Linq;

namespace KMPExpander.Class
{
    // What a demo camera needs from a kart: its race camera (Kart::Camera). The look-at point of the race camera is its
    // target (+0x13C), its direction +0x164 and its up vector +0xE0.
    public struct KartView
    {
        public Vector3 Position, Target, Direction, Up;
        public bool Respawning;

        // The race camera's target is 19.7 units above the kart (measured in game: 18.8 to 19.9 while driving on Mario
        // Circuit, within 6 units of the kart sideways); its direction and up vector are those of the simulated camera.
        // The follow cameras take that direction and up vector, not the kart's own orientation; their route offsets are
        // in course axes (both checked against a replay in game).
        public const float TargetHeight = 19.7f;

        public static KartView Of(KartSimulator.KartEntry k)
        {
            KartView v = new KartView();
            v.Position = k.Pos;
            v.Direction = k.GetCameraDirection().Normalize();
            v.Target = k.Pos + new Vector3(0, TargetHeight, 0);
            v.Up = k.CameraUpVector;
            v.Respawning = k.Mode == KartSimulator.Modes.Respawning;
            return v;
        }
    }

    // A route as a camera rides it (Demo::CameraPath, a Field::PathBase with flag 8): straight lines when the route's
    // first setting is 0, else a curve through the points (KDRailSpline, drawn here as a Catmull-Rom curve, which may
    // not be the game's exact curve). A route whose second setting is 0 is closed: the first point is added again at the
    // end and the camera restarts after it. Any other value makes the camera run the route back and forth. The speed is
    // the first setting of the points / 100 units per frame, blended along each segment (CameraPath::updateSpeed).
    public class CameraRail
    {
        private readonly List<Vector3> points = new List<Vector3>();
        private readonly List<float> speeds = new List<float>();
        private readonly float[] lengths;
        private readonly bool smooth, closed;
        private readonly int count;                       // the route's own points, without the repeated first one
        private int segment, direction = 1;
        private float distance;                           // along the current segment

        public CameraRail(Routes.RouteGroup route)
        {
            smooth = route.Smooth;
            closed = route.Loop;
            count = route.Entries.Count;
            foreach (var p in route.Entries)
            {
                points.Add(p.Pos);
                speeds.Add(p.Speed);
            }
            if (closed && count > 1)
            {
                points.Add(points[0]);
                speeds.Add(speeds[0]);
            }
            lengths = new float[Math.Max(0, points.Count - 1)];
            for (int i = 0; i < lengths.Length; i++)
            {
                float length = 0;
                Vector3 last = points[i];
                for (int s = 1; s <= 16; s++)
                {
                    Vector3 p = Evaluate(i, s / 16f);
                    length += (p - last).Length;
                    last = p;
                }
                lengths[i] = length;
            }
        }

        public void Reset()
        {
            segment = 0;
            distance = 0;
            direction = 1;
        }

        private float Rate { get { return lengths.Length == 0 || lengths[segment] <= 0 ? 0 : Math.Min(1, distance / lengths[segment]); } }

        public Vector3 Position
        {
            get { return lengths.Length == 0 ? (points.Count > 0 ? points[0] : new Vector3(0)) : Evaluate(segment, Rate); }
        }

        // One frame (KDRailUpdater::update).
        public void Step()
        {
            if (lengths.Length == 0) return;
            float t = Rate;
            float speed = ((1 - t) * speeds[segment] + t * speeds[segment + 1]) * 0.01f;
            distance += speed * direction;
            for (int guard = 0; guard < 1000; guard++)
            {
                if (distance > lengths[segment])
                {
                    if (segment + 1 < lengths.Length)
                    {
                        distance -= lengths[segment];
                        segment++;
                    }
                    else if (closed)
                    {
                        distance -= lengths[segment];
                        segment = 0;
                    }
                    else
                    {
                        distance = 2 * lengths[segment] - distance;
                        direction = -1;
                    }
                }
                else if (distance < 0)
                {
                    if (segment > 0)
                    {
                        segment--;
                        distance += lengths[segment];
                    }
                    else
                    {
                        distance = -distance;
                        direction = 1;
                    }
                }
                else break;
            }
            distance = Math.Max(0, Math.Min(lengths[segment], distance));
        }

        private Vector3 Point(int i)
        {
            if (closed && count > 1) return points[((i % count) + count) % count];
            return points[Math.Max(0, Math.Min(points.Count - 1, i))];
        }

        public Vector3 Evaluate(int i, float t)
        {
            if (!smooth) return points[i] + (points[i + 1] - points[i]) * t;
            Vector3 p0 = Point(i - 1), p1 = Point(i), p2 = Point(i + 1), p3 = Point(i + 2);
            float t2 = t * t, t3 = t2 * t;
            return (p1 * 2 + (p2 - p0) * t + (p0 * 2 - p1 * 5 + p2 * 4 - p3) * t2 + (p1 * 3 - p0 - p2 * 3 + p3) * t3) * 0.5f;
        }

        // The rail as a line strip, for drawing a route.
        public static List<Vector3> Polyline(Routes.RouteGroup route)
        {
            CameraRail rail = new CameraRail(route);
            List<Vector3> line = new List<Vector3>();
            if (rail.points.Count == 0) return line;
            line.Add(rail.points[0]);
            for (int i = 0; i < rail.lengths.Length; i++)
            {
                int steps = rail.smooth ? 16 : 1;
                for (int s = 1; s <= steps; s++) line.Add(rail.Evaluate(i, s / (float)steps));
            }
            return line;
        }
    }

    // One demo camera (Demo::DemoCameraBase and the class createCamera_ builds for its type), with its own state.
    public class DemoCamera
    {
        public readonly Camera.CameraEntry Entry;
        public readonly int Index;                        // in CAME
        public readonly bool LastOfIntro;                 // a type 5 camera last in the intro chain: DemoCameraPathMoveAt2
        public Vector3 Position, LookAt, Up = new Vector3(0, 1, 0);
        public float Fov = 45;
        public readonly CameraRail Rail;

        private float fovStep;
        private bool fovGrows;
        private Vector3 lookStep;
        private int lookFrames;
        private int easeFrames;

        public DemoCamera(SimpleKMP kmp, int index, bool lastOfIntro)
        {
            Index = index;
            Entry = kmp.Camera.Entries[index];
            LastOfIntro = lastOfIntro;
            Routes.RouteGroup route = Entry.RouteID >= 0 ? kmp.Routes.Entries.ElementAtOrDefault(Entry.RouteID) : null;
            if (route != null && route.Entries.Count > 0) Rail = new CameraRail(route);
        }

        public static bool IsFollow(int type) { return type == 0 || type == 3 || type == 6 || type == 7 || type == 8; }
        private bool MovesLookAt { get { return Entry.TypeID == 4 || Entry.TypeID == 5; } }

        // The kart the camera follows: kart 1 for type 7, kart 2 for type 8 (the second and third of the award
        // ceremony), else the watched kart.
        private KartView Kart(Func<int, KartView> karts)
        {
            return karts(Entry.TypeID == 7 ? 1 : Entry.TypeID == 8 ? 2 : -1);
        }

        // DemoCameraBase::init: back to the start of the route, the field of view and the look-at movement.
        public void Init(Func<int, KartView> karts)
        {
            if (Rail != null)
            {
                Rail.Reset();
                Position = Rail.Position;
            }
            else Position = Entry.Pos;
            Fov = Entry.FOVBegin;
            fovStep = Entry.FOVSpeed * 0.01f;
            fovGrows = Entry.FOVBegin < Entry.FOVEnd;
            LookAt = karts(-1).Target;
            InitInner(karts);
            CalcPre(karts);
            CalcInner(karts);
        }

        // DemoCameraBase::calc: one frame.
        public void Calc(Func<int, KartView> karts)
        {
            CalcPre(karts);
            if (Rail != null)
            {
                Rail.Step();
                Position = Rail.Position;
            }
            Fov = fovGrows ? Math.Min(Entry.FOVEnd, Fov + fovStep) : Math.Max(Entry.FOVEnd, Fov - fovStep);
            CalcInner(karts);
        }

        private void InitInner(Func<int, KartView> karts)
        {
            if (MovesLookAt && LastOfIntro)
            {
                easeFrames = (int)Entry.DurationRaw;
                LookAt = Entry.VPBegin;
            }
            else if (MovesLookAt)
            {
                // DemoCameraPathMoveAt::initInner: with a speed of 0 the frame count is infinite.
                float speed = Entry.ViewpointSpeed * 0.01f, length = (Entry.VPEnd - Entry.VPBegin).Length;
                lookStep = length > 0 ? (Entry.VPEnd - Entry.VPBegin) * (speed / length) : new Vector3(0);
                lookFrames = speed <= 0 ? int.MaxValue : (int)(length / speed) + 1;
                LookAt = Entry.VPBegin;
            }
            else if (IsFollow(Entry.TypeID) && Entry.Next != -2) Up = Kart(karts).Up;
        }

        private void CalcPre(Func<int, KartView> karts)
        {
            if (!MovesLookAt) LookAt = Kart(karts).Target;
            Up = new Vector3(0, 1, 0);
        }

        private void CalcInner(Func<int, KartView> karts)
        {
            if (MovesLookAt && LastOfIntro)
            {
                // DemoCameraPathMoveAt2: from begin into end, slowing down, in Duration frames.
                if (easeFrames > 0) easeFrames--;
                float f = Entry.DurationRaw > 0 ? easeFrames / Entry.DurationRaw : 0;
                LookAt = Entry.VPEnd + (Entry.VPBegin - Entry.VPEnd) * (f * f);
            }
            else if (MovesLookAt)
            {
                if (lookFrames > 0)
                {
                    LookAt += lookStep;
                    if (lookFrames != int.MaxValue) lookFrames--;
                }
                else if (lookFrames == 0)
                {
                    LookAt = Entry.VPEnd;
                    lookFrames = -1;
                }
            }
            else if (IsFollow(Entry.TypeID))
            {
                KartView kart = Kart(karts);
                if (Entry.Next != -2) Up = kart.Up.Length > 0 ? kart.Up.Normalize() : new Vector3(0, 1, 0);
                if (Entry.TypeID == 0 || Entry.TypeID == 3)
                {
                    // DemoCameraFollow::calcFollow: the reverse of the race camera's direction, turned by viewpoint
                    // begin X around the up vector and -Y around its side axis, at viewpoint begin Z from the look-at
                    // (matches a replay in game, Toad Circuit camera 4, (-160, -16, 44), to 0.4 units).
                    Vector3 back = Rotate(kart.Direction * -1, Up, Entry.ViewpointBeginX);
                    back = Rotate(back, Side(back * -1, Up), -Entry.ViewpointBeginY);
                    Position = LookAt + back * Entry.ViewpointBeginZ;
                }
                else
                {
                    // DemoCameraFollowPath::calcFollow: the route's points are offsets from the kart's target. Without
                    // a route the offset is added again every frame, as in the game.
                    Position += LookAt;
                }
                // calcFollowPosture_: the view turned by viewpoint end, and rolled by its Z.
                Vector3 look = LookAt - Position;
                look = Rotate(look, Up, -Entry.ViewpointEndX);
                // Negative Y turns the view down (matches the replay cameras of Toad Circuit in game exactly).
                look = Rotate(look, Side(look, Up), Entry.ViewpointEndY);
                LookAt = Position + look;
                if (look.Length > 0) Up = Rotate(Up, look.Normalize(), -Entry.ViewpointEndZ);
            }
        }

        // The camera's right side, for a camera looking along forward.
        private static Vector3 Side(Vector3 forward, Vector3 up)
        {
            Vector3 s = forward.Cross(up);
            return s.Length > 1e-6f ? s.Normalize() : new Vector3(1, 0, 0);
        }

        // v turned by degrees around axis, counterclockwise seen from the axis' tip.
        public static Vector3 Rotate(Vector3 v, Vector3 axis, float degrees)
        {
            if (degrees == 0 || axis.Length < 1e-6f) return v;
            Vector3 a = axis.Normalize();
            double r = degrees * Math.PI / 180;
            float c = (float)Math.Cos(r), s = (float)Math.Sin(r);
            return v * c + a.Cross(v) * s + a * (a.Dot(v) * (1 - c));
        }
    }

    // What the screen shows: a demo camera, or the race camera of the watched kart.
    public class DemoView
    {
        public Vector3 Position, LookAt, Up = new Vector3(0, 1, 0);
        public float Fov = 45;
        public DemoCamera Camera;                         // null: the watched kart's race camera

        public void Set(DemoCamera c)
        {
            Position = c.Position;
            LookAt = c.LookAt;
            Up = c.Up;
            Fov = c.Fov;
            Camera = c;
        }

        public void CopyFrom(DemoView v)
        {
            Position = v.Position;
            LookAt = v.LookAt;
            Up = v.Up;
            Fov = v.Fov;
            Camera = v.Camera;
        }

        // The view a share t of the way from a to b, for drawing between two frames; b alone after a cut.
        public static DemoView Lerp(DemoView a, DemoView b, float t)
        {
            if (a.Camera != b.Camera || t >= 1) return b;
            DemoView v = new DemoView();
            v.Position = a.Position + (b.Position - a.Position) * t;
            v.LookAt = a.LookAt + (b.LookAt - a.LookAt) * t;
            v.Up = a.Up + (b.Up - a.Up) * t;
            v.Fov = a.Fov + (b.Fov - a.Fov) * t;
            v.Camera = b.Camera;
            return v;
        }
    }

    // The replay cameras (Demo::DemoCameraDirector in state NORMAL, as in replays and after the finish). When the course
    // loads, every camera area (type 0) with a camera index of 0 or more gets its own copy of that camera, except when
    // the camera is of type 0 or has a video next other than 0. Every frame the area that holds the watched kart is
    // looked up (calcInAreaIndex_): the area found last time while the kart is still in it, else the one with the highest
    // priority. A new area restarts its camera. After 20 frames outside every area the race camera is shown, and the
    // demo cameras come back only after 60 frames in a row inside areas. While Lakitu carries the kart back, the race
    // camera is shown too.
    public class ReplayCameraDirector
    {
        private readonly List<Area.AreaEntry> areas = new List<Area.AreaEntry>();
        private readonly List<int> areaNumbers = new List<int>();     // in AREA
        private readonly List<DemoCamera> cameras = new List<DemoCamera>();
        private int areaIndex = -1, switchTimer;
        private bool outOfArea;
        private DemoCamera current;
        public readonly bool HasAreas;

        public ReplayCameraDirector(SimpleKMP kmp)
        {
            for (int i = 0; i < kmp.Area.Entries.Count; i++)
            {
                Area.AreaEntry a = kmp.Area.Entries[i];
                if (a.TypeID != 0 || a.CAMEIndex < 0) continue;
                HasAreas = true;
                if (a.CAMEIndex >= kmp.Camera.Entries.Count) continue;
                Camera.CameraEntry c = kmp.Camera.Entries[a.CAMEIndex];
                if (c.TypeID == 0 || c.VideoNext != 0) continue;
                areas.Add(a);
                areaNumbers.Add(i);
                cameras.Add(new DemoCamera(kmp, a.CAMEIndex, false));
            }
        }

        public int CameraAreaCount { get { return areas.Count; } }
        // The current camera's view, and its view the frame before.
        public readonly DemoView View = new DemoView(), PrevView = new DemoView();
        public DemoCamera Current { get { return current; } }

        private int FindArea(Vector3 p)
        {
            if (areaIndex >= 0 && areas[areaIndex].Contains(p)) return areaIndex;
            int best = -1, bestPriority = -1;
            for (int i = 0; i < areas.Count; i++)
                if (i != areaIndex && bestPriority < areas[i].Priority && areas[i].Contains(p))
                {
                    best = i;
                    bestPriority = areas[i].Priority;
                }
            return best;
        }

        // stateNormal: one frame. Returns whether the demo camera is shown (else the race camera).
        public bool Step(Func<int, KartView> karts)
        {
            PrevView.CopyFrom(View);
            bool shows = StepInner(karts);
            if (current != null) View.Set(current);
            return shows;
        }

        private bool StepInner(Func<int, KartView> karts)
        {
            KartView kart = karts(-1);
            int found = FindArea(kart.Position);
            if (!outOfArea)
            {
                if (found >= 0) switchTimer = 0;
                else if (++switchTimer > 19)
                {
                    outOfArea = true;
                    switchTimer = 0;
                }
            }
            else
            {
                if (found < 0)
                {
                    switchTimer = 0;
                    return false;
                }
                if (++switchTimer > 59)
                {
                    outOfArea = false;
                    switchTimer = 0;
                }
            }
            if (found >= 0 && found != areaIndex)
            {
                areaIndex = found;
                current = cameras[found];
                current.Init(karts);
            }
            else if (current != null) current.Calc(karts);
            return ShowsDemo(kart);
        }

        public bool ShowsDemo(KartView kart)
        {
            return current != null && !outOfArea && !kart.Respawning;
        }

        public string Describe(KartView kart)
        {
            if (areas.Count == 0)
                return HasAreas ? "Replay camera: no camera area has a usable camera (type 0 and Video Next other than 0 are left out); race camera" : "Replay camera: the course has no camera area, so no replay cameras; race camera";
            string area = areaIndex >= 0 ? String.Format("area {0}, camera {1} ({2})", areaNumbers[areaIndex], cameras[areaIndex].Index, cameras[areaIndex].Entry.Type) : "no area yet";
            if (kart.Respawning) return "Replay camera: race camera while Lakitu carries the kart (" + area + ")";
            if (outOfArea) return String.Format("Replay camera: race camera, the kart left the camera areas ({0}/60 frames back inside)", switchTimer);
            if (current == null) return "Replay camera: the kart is in no camera area yet";
            return "Replay camera: " + area + (switchTimer > 0 ? String.Format(", outside the areas for {0}/20 frames", switchTimer) : "");
        }
    }

    // The course intro (Demo::DemoCameraDirector in state COURSE). The chain starts from the first camera with a duration
    // above 0 and a video next of 0, goes back through the cameras whose next points to it, and then follows the next
    // links. The shots last 232, 204 and 240 frames whatever the KMP says; a fourth one lasts until the end. The intro
    // ends after 700 frames. The last camera of the chain, if of type 5, eases its look-at point into viewpoint end in
    // Duration frames. The karts are hidden during the first two shots, and the near clip is 30 units.
    public class IntroCameraDirector
    {
        public static readonly int[] ShotFrames = { 232, 204, 240 };
        public const int IntroFrames = 700;
        public const float Near = 30;

        public readonly List<DemoCamera> Chain = new List<DemoCamera>();
        public readonly string Error;
        private int shot, timer;
        public int Frame { get; private set; }
        public DemoCamera Current { get; private set; }
        public readonly DemoView View = new DemoView(), PrevView = new DemoView();

        public IntroCameraDirector(SimpleKMP kmp)
        {
            var cams = kmp.Camera.Entries;
            if (!kmp.Area.Entries.Any(a => a.TypeID == 0 && a.CAMEIndex >= 0))
            {
                Error = "The course has no camera area with a camera index of 0 or more, so the game builds no intro.";
                return;
            }
            int head = cams.FindIndex(c => c.DurationRaw > 0 && c.VideoNext == 0);
            if (head < 0)
            {
                Error = "No camera has a duration above 0 and a Video Next of 0, so the course has no intro.";
                return;
            }
            HashSet<int> seen = new HashSet<int> { head };
            while (true)
            {
                int before = cams.FindIndex(c => c.DurationRaw > 0 && c.Next == head);
                if (before < 0) break;
                if (!seen.Add(before))
                {
                    Error = "The intro cameras are linked in a loop (Next), so the game would never finish loading the course.";
                    return;
                }
                head = before;
            }
            List<int> chain = new List<int>();
            seen.Clear();
            for (int i = head; i >= 0; i = cams[i].Next)
            {
                if (!seen.Add(i))
                {
                    Error = "The intro cameras are linked in a loop (Next).";
                    return;
                }
                if (i >= cams.Count) break;                // a next past the end stops the chain
                chain.Add(i);
            }
            for (int i = 0; i < chain.Count; i++) Chain.Add(new DemoCamera(kmp, chain[i], i == chain.Count - 1));
        }

        public bool Finished { get { return Frame >= IntroFrames; } }
        public int Shot { get { return shot; } }
        public bool KartsHidden { get { return shot < 2; } }

        public void Start(Func<int, KartView> karts)
        {
            shot = timer = Frame = 0;
            Current = Chain[0];
            Current.Init(karts);
            Capture();
            PrevView.CopyFrom(View);
        }

        // stateCourse: one frame.
        public void Step(Func<int, KartView> karts)
        {
            Frame++;
            PrevView.CopyFrom(View);
            if (Current == null) return;
            Current.Calc(karts);
            Capture();
            if (++timer < (shot < ShotFrames.Length ? ShotFrames[shot] : int.MaxValue)) return;
            timer = 0;
            if (shot + 1 < Chain.Count)
            {
                shot++;
                Current = Chain[shot];
                Current.Init(karts);
                Capture();
            }
            else Current = null;                          // the view stays where the last camera left it
        }

        private void Capture()
        {
            View.Set(Current);
        }
    }
}
