using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.IO;
using System.Xml.Serialization;
using KMPClass;
using KMPExpander.Class.SimpleKMPs;
using System.Windows.Forms;
using System.Drawing;
using Tao.OpenGl;
using LibCTR.Collections;
using Extensions;

namespace KMPExpander.Class
{
    [XmlRoot("KMP")]
    public class SimpleKMP
    {
        public StartPositions StartPositions;
        public EnemyRoutes EnemyRoutes;
        public ItemRoutes ItemRoutes;
        public GliderRoutes GliderRoutes;
        public CheckPoints CheckPoints;
        public Objects Objects;
        public Routes Routes;
        public Area Area;
        public Camera Camera;
        public RespawnPoints RespawnPoints;
        public StageInformation StageInformation;
        public List<int> currentCullingRoutes;

        public SimpleKMP()
        {
            StartPositions = new StartPositions();
            EnemyRoutes = new EnemyRoutes();
            ItemRoutes = new ItemRoutes();
            CheckPoints = new CheckPoints();
            Objects = new Objects();
            Routes = new Routes();
            Area = new Area();
            Camera = new Camera();
            RespawnPoints = new RespawnPoints();
            GliderRoutes = new GliderRoutes();
            StageInformation = new StageInformation();
            currentCullingRoutes = new List<int>();
        }

        public SimpleKMP(byte[] data)
        {
            KMP kmp_data;
            kmp_data = new KMP(data);

            StartPositions = new StartPositions(kmp_data.KartPoint);
            EnemyRoutes = new EnemyRoutes(kmp_data.EnemyPoint, kmp_data.EnemyPointPath);
            ItemRoutes = new ItemRoutes(kmp_data.ItemPoint, kmp_data.ItemPointPath);
            CheckPoints = new CheckPoints(kmp_data.CheckPoint, kmp_data.CheckPointPath);
            Objects = new Objects(kmp_data.GlobalObject);
            Routes = new Routes(kmp_data.PointInfo);
            Area = new Area(kmp_data.Area);
            Camera = new Camera(kmp_data.Camera);
            RespawnPoints = new RespawnPoints(kmp_data.JugemPoint);
            GliderRoutes = new GliderRoutes(kmp_data.GliderPoint, kmp_data.GliderPointPath);
            StageInformation = new StageInformation(kmp_data.StageInfo);
            currentCullingRoutes = new List<int>();
        }

        public byte[] Write()
        {
            byte[] m;
            KMP kmp_data;
            kmp_data = new KMP();

            kmp_data.KartPoint = StartPositions.ToKTPT();
            kmp_data.EnemyPoint = EnemyRoutes.ToENPT();
            kmp_data.EnemyPointPath = EnemyRoutes.ToENPH();
            kmp_data.ItemPoint = ItemRoutes.ToITPT();
            kmp_data.ItemPointPath = ItemRoutes.ToITPH();
            kmp_data.CheckPoint = CheckPoints.ToCKPT();
            kmp_data.CheckPointPath = CheckPoints.ToCKPH();
            kmp_data.GlobalObject = Objects.ToGOBJ();
            kmp_data.PointInfo = Routes.ToPOTI();
            kmp_data.Area = Area.ToAREA();
            kmp_data.Camera = Camera.ToCAME();
            kmp_data.JugemPoint = RespawnPoints.ToJGPT();
            kmp_data.CourseSect = new KMPSections.CORS();
            kmp_data.CannonPoint = new KMPSections.CNPT();
            kmp_data.MissionPoint = new KMPSections.MSPT();
            kmp_data.StageInfo = StageInformation.ToSTGI();
            kmp_data.GliderPoint = GliderRoutes.ToGLPT();
            kmp_data.GliderPointPath = GliderRoutes.ToGLPH();

            m = kmp_data.Write();
            return m;
        }

        public byte[] WriteXML()
        {
            MemoryStream m = new MemoryStream();
            XmlSerializer serializer = new XmlSerializer(typeof(SimpleKMP));
            using (TextWriter writer = new StreamWriter(m))
            {
                serializer.Serialize(writer, this);
                writer.Close();
            }
            return m.ToArray();
        }

        public static SimpleKMP FromXML(byte[] xmlfile)
        {
            MemoryStream m = new MemoryStream(xmlfile);
            SimpleKMP new_kmp;
            XmlSerializer deserializer = new XmlSerializer(typeof(SimpleKMP));
            TextReader reader = new StreamReader(m);
            new_kmp = (SimpleKMP)deserializer.Deserialize(reader);
            reader.Close();
            return new_kmp;
        }

        public void PopulateTreeView(TreeView tv)
        {
            
            tv.Nodes.Add(StartPositions.GetTreeNode());
            int i = 0;
            tv.Nodes[i].Tag = StartPositions;
            tv.Nodes[i].ImageIndex = i+1; tv.Nodes[i].SelectedImageIndex = i + 1; i++;
            tv.Nodes.Add(EnemyRoutes.GetTreeNode());
            tv.Nodes[i].Tag = EnemyRoutes;
            tv.Nodes[i].ImageIndex = i + 1; tv.Nodes[i].SelectedImageIndex = i + 1; i++;
            tv.Nodes.Add(ItemRoutes.GetTreeNode());
            tv.Nodes[i].Tag = ItemRoutes;
            tv.Nodes[i].ImageIndex = i + 1; tv.Nodes[i].SelectedImageIndex = i + 1; i++;
            tv.Nodes.Add(GliderRoutes.GetTreeNode());
            tv.Nodes[i].Tag = GliderRoutes;
            tv.Nodes[i].ImageIndex = i + 1; tv.Nodes[i].SelectedImageIndex = i + 1; i++;
            tv.Nodes.Add(CheckPoints.GetTreeNode());
            tv.Nodes[i].Tag = CheckPoints;
            tv.Nodes[i].ImageIndex = i + 1; tv.Nodes[i].SelectedImageIndex = i + 1; i++;
            tv.Nodes.Add(RespawnPoints.GetTreeNode());
            tv.Nodes[i].Tag = RespawnPoints;
            tv.Nodes[i].ImageIndex = i + 1; tv.Nodes[i].SelectedImageIndex = i + 1; i++;
            tv.Nodes.Add(Objects.GetTreeNode());
            tv.Nodes[i].Tag = Objects;
            tv.Nodes[i].ImageIndex = i + 1; tv.Nodes[i].SelectedImageIndex = i + 1; i++;
            tv.Nodes.Add(Routes.GetTreeNode());
            tv.Nodes[i].Tag = Routes;
            tv.Nodes[i].ImageIndex = i + 1; tv.Nodes[i].SelectedImageIndex = i + 1; i++;
            tv.Nodes.Add(Area.GetTreeNode());
            tv.Nodes[i].Tag = Area;
            tv.Nodes[i].ImageIndex = i + 1; tv.Nodes[i].SelectedImageIndex = i + 1; i++;
            tv.Nodes.Add(Camera.GetTreeNode());
            tv.Nodes[i].Tag = Camera;
            tv.Nodes[i].ImageIndex = i + 1; tv.Nodes[i].SelectedImageIndex = i + 1; i++;
            tv.Nodes.Add(StageInformation.GetTreeNode());
            tv.Nodes[i].Tag = StageInformation;
            tv.Nodes[i].ImageIndex = i + 1; tv.Nodes[i].SelectedImageIndex = i + 1; i++;

            foreach (TreeNode node in tv.Nodes)
            {
                if (node.Tag is ISectionBase)
                {
                    node.Checked = ((ISectionBase)node.Tag).GetVisibility();
                    foreach (TreeNode subnode in node.Nodes)
                    subnode.Checked = ((ISectionBase)subnode.Tag).GetVisibility();
                }
            }
        }

        public void Render(bool picking = false)
        {
            Gl.glBindTexture(Gl.GL_TEXTURE_2D, 0);
            if (!picking)
            {
                Gl.glDepthFunc(Gl.GL_ALWAYS);
                Gl.glLoadIdentity();
            }
            StartPositions.Render(picking);
            EnemyRoutes.Render(picking);
            ItemRoutes.Render(picking);
            CheckPoints.Render(picking);
            GliderRoutes.Render(picking);
            Routes.Render(picking);
            Objects.Render(picking);
            RespawnPoints.Render(picking);
            Area.Render(picking);
            Camera.Render(picking);
        }

        public ISectionBase GetGroup(SectionPicking.PickingInfo pick_info)
        {
            switch (pick_info.Section)
            {
                case Sections.StartPositions:
                    return StartPositions;
                case Sections.EnemyRoutes:
                    return EnemyRoutes.Entries[pick_info.GroupID];
                case Sections.ItemRoutes:
                    return ItemRoutes.Entries[pick_info.GroupID];
                case Sections.GliderRoutes:
                    return GliderRoutes.Entries[pick_info.GroupID];
                case Sections.Routes:
                    return Routes.Entries[pick_info.GroupID];
                case Sections.CheckPoints:
                    return CheckPoints.Entries[pick_info.GroupID];
                case Sections.Objects:
                    return Objects;
                case Sections.RespawnPoints:
                    return RespawnPoints;
                case Sections.Area:
                    return Area;
                case Sections.Camera:
                    return Camera;
                case Sections.StageInformation:
                    return StageInformation;
                default:
                    return null;
            }
        }

        public ISectionBase GetSection(SectionPicking.PickingInfo pick_info)
        {
            switch (pick_info.Section)
            {
                case Sections.StartPositions:
                    return StartPositions;
                case Sections.EnemyRoutes:
                    return EnemyRoutes;
                case Sections.ItemRoutes:
                    return ItemRoutes;
                case Sections.GliderRoutes:
                    return GliderRoutes;
                case Sections.Routes:
                    return Routes;
                case Sections.CheckPoints:
                    return CheckPoints;
                case Sections.Objects:
                    return Objects;
                case Sections.RespawnPoints:
                    return RespawnPoints;
                case Sections.Area:
                    return Area;
                case Sections.Camera:
                    return Camera;
                case Sections.StageInformation:
                    return StageInformation;
                default:
                    return null;
            }
        }

        public object GetPoint(SectionPicking.PickingInfo pick_info)
        {
            switch (pick_info.Section)
            {
                case Sections.StartPositions:
                    return StartPositions.Entries[pick_info.EntryID];
                case Sections.EnemyRoutes:
                    return EnemyRoutes.Entries[pick_info.GroupID].Entries[pick_info.EntryID];
                case Sections.ItemRoutes:
                    return ItemRoutes.Entries[pick_info.GroupID].Entries[pick_info.EntryID];
                case Sections.GliderRoutes:
                    return GliderRoutes.Entries[pick_info.GroupID].Entries[pick_info.EntryID];
                case Sections.Routes:
                    return Routes.Entries[pick_info.GroupID].Entries[pick_info.EntryID];
                case Sections.CheckPoints:
                    return CheckPoints.Entries[pick_info.GroupID].Entries[pick_info.EntryID];
                case Sections.Objects:
                    return Objects.Entries[pick_info.EntryID];
                case Sections.RespawnPoints:
                    return RespawnPoints.Entries[pick_info.EntryID];
                case Sections.Area:
                    return Area.Entries[pick_info.EntryID];
                case Sections.Camera:
                    return Camera.Entries[pick_info.EntryID];
                case Sections.StageInformation:
                    return StageInformation.Entries[pick_info.EntryID];
                default:
                    return null;
            }
        }
       
        public void MovePointY(SectionPicking.PickingInfo pick_info,float height)
        {
            switch (pick_info.Section)
            {
                case Sections.StartPositions:
                    StartPositions.Entries[pick_info.EntryID].PositionY = height;
                    break;
                case Sections.EnemyRoutes:
                    EnemyRoutes.Entries[pick_info.GroupID].Entries[pick_info.EntryID].PositionY = height;
                    break;
                case Sections.ItemRoutes:
                    ItemRoutes.Entries[pick_info.GroupID].Entries[pick_info.EntryID].PositionY = height;
                    break;
                case Sections.GliderRoutes:
                    GliderRoutes.Entries[pick_info.GroupID].Entries[pick_info.EntryID].PositionY = height;
                    break;
                case Sections.Routes:
                    Routes.Entries[pick_info.GroupID].Entries[pick_info.EntryID].PositionY = height;
                    break;
                case Sections.Objects:
                    Objects.Entries[pick_info.EntryID].PositionY = height;
                    break;
                case Sections.RespawnPoints:
                    RespawnPoints.Entries[pick_info.EntryID].PositionY = height;
                    break;
                case Sections.Area:
                    Area.Entries[pick_info.EntryID].PositionY = height;
                    break;
                case Sections.Camera:
                    Camera.Entries[pick_info.EntryID].PositionY = height;
                    break;
            }
        }

        public void MoveAnyPoint(object point, Vector3 position,Boolean left=false)
        {
            ViewPlaneHandler vph = (Application.OpenForms[0] as Form1).vph;
            Vector3 res = new Vector3();
            Type type = point.GetType();
            if (type == typeof(StartPositions.StartEntry))
            {
                vph.storeToVector(ref res, position);
                ((StartPositions.StartEntry)point).Pos = res;
            }
            else if (type == typeof(EnemyRoutes.EnemyGroup.EnemyEntry))
            {
                vph.storeToVector(ref res, position);
                ((EnemyRoutes.EnemyGroup.EnemyEntry)point).Pos = res;
            }
            else if (type == typeof(ItemRoutes.ItemGroup.ItemEntry))
            {
                vph.storeToVector(ref res, position);
                ((ItemRoutes.ItemGroup.ItemEntry)point).Pos = res;
            }
            else if (type == typeof(GliderRoutes.GliderGroup.GliderEntry))
            {
                vph.storeToVector(ref res, position);
                ((GliderRoutes.GliderGroup.GliderEntry)point).Pos = res;
            }
            else if (type == typeof(CheckPoints.CheckpointGroup.CheckpointEntry))
            {
                if (left)
                {
                    ((CheckPoints.CheckpointGroup.CheckpointEntry)point).LeftPointX = position.X;
                    ((CheckPoints.CheckpointGroup.CheckpointEntry)point).LeftPointZ = position.Z;
                    return;
                }
                ((CheckPoints.CheckpointGroup.CheckpointEntry)point).RightPointX = position.X;
                ((CheckPoints.CheckpointGroup.CheckpointEntry)point).RightPointZ = position.Z;
            }
            else if (type == typeof(Routes.RouteGroup.RouteEntry))
            {
                vph.storeToVector(ref res, position);
                ((Routes.RouteGroup.RouteEntry)point).Pos = res;
            }
            else if (type == typeof(Objects.ObjectEntry))
            {
                vph.storeToVector(ref res, position);
                ((Objects.ObjectEntry)point).Pos = res;
            }
            else if (type == typeof(RespawnPoints.RespawnEntry))
            {
                vph.storeToVector(ref res, position);
                ((RespawnPoints.RespawnEntry)point).Pos = res;
            }
            else if (type == typeof(Area.AreaEntry))
            {
                ((Area.AreaEntry)point).PositionX = position.X;
                ((Area.AreaEntry)point).PositionY = position.Y;
                ((Area.AreaEntry)point).PositionZ = position.Z;
            }
            else if (type == typeof(Camera.CameraEntry))
            {
                vph.storeToVector(ref res, position);
                ((Camera.CameraEntry)point).Pos = res;
            }
        }

        public void MovePoint(SectionPicking.PickingInfo pick_info,Vector3 position)
        {
            ViewPlaneHandler vph = (Application.OpenForms[0] as Form1).vph;
            Vector3 res;
            switch (pick_info.Section)
            {
                case Sections.StartPositions:
                    res = StartPositions.Entries[pick_info.EntryID].Pos;
                    vph.storePlane(ref res, position);
                    StartPositions.Entries[pick_info.EntryID].Pos = res;
                    break;
                case Sections.EnemyRoutes:
                    res = EnemyRoutes.Entries[pick_info.GroupID].Entries[pick_info.EntryID].Pos;
                    vph.storePlane(ref res, position);
                    EnemyRoutes.Entries[pick_info.GroupID].Entries[pick_info.EntryID].Pos = res;
                    break;
                case Sections.ItemRoutes:
                    res = ItemRoutes.Entries[pick_info.GroupID].Entries[pick_info.EntryID].Pos;
                    vph.storePlane(ref res, position);
                    ItemRoutes.Entries[pick_info.GroupID].Entries[pick_info.EntryID].Pos = res;
                    break;
                case Sections.GliderRoutes:
                    res = GliderRoutes.Entries[pick_info.GroupID].Entries[pick_info.EntryID].Pos;
                    vph.storePlane(ref res, position);
                    GliderRoutes.Entries[pick_info.GroupID].Entries[pick_info.EntryID].Pos = res;
                    break;
                case Sections.Routes:
                    res = Routes.Entries[pick_info.GroupID].Entries[pick_info.EntryID].Pos;
                    vph.storePlane(ref res, position);
                    Routes.Entries[pick_info.GroupID].Entries[pick_info.EntryID].Pos = res;
                    break;
                case Sections.CheckPoints:
                    if (vph.mode != ViewPlaneHandler.PLANE_MODES.XZ) break;
                    if (pick_info.PointID == PointID.Left)
                    {
                        CheckPoints.Entries[pick_info.GroupID].Entries[pick_info.EntryID].LeftPointX = position.X;
                        CheckPoints.Entries[pick_info.GroupID].Entries[pick_info.EntryID].LeftPointZ = position.Z;
                    }
                    else
                    {
                        CheckPoints.Entries[pick_info.GroupID].Entries[pick_info.EntryID].RightPointX = position.X;
                        CheckPoints.Entries[pick_info.GroupID].Entries[pick_info.EntryID].RightPointZ = position.Z;
                    }
                    break;
                case Sections.Objects:
                    res = Objects.Entries[pick_info.EntryID].Pos;
                    vph.storePlane(ref res, position);
                    Objects.Entries[pick_info.EntryID].Pos = res;
                    break;
                case Sections.RespawnPoints:
                    res = RespawnPoints.Entries[pick_info.EntryID].Pos;
                    vph.storePlane(ref res, position);
                    RespawnPoints.Entries[pick_info.EntryID].Pos = res;
                    break;
                case Sections.Area:
                    if (vph.mode != ViewPlaneHandler.PLANE_MODES.XZ) break;
                    Area.Entries[pick_info.EntryID].PositionX = position.X;
                    Area.Entries[pick_info.EntryID].PositionZ = position.Z;
                    break;
                case Sections.Camera:
                    res = Camera.Entries[pick_info.EntryID].Pos;
                    vph.storePlane(ref res, position);
                    Camera.Entries[pick_info.EntryID].Pos = res;
                    break;
            }
        }
    }
}