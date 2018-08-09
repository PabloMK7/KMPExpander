using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KMPExpander.Class
{
    public enum Sections
    {
        None=0, StartPositions = 1,EnemyRoutes = 2,ItemRoutes = 3,GliderRoutes=4,CheckPoints=5,Objects=6,RespawnPoints=7,Routes=8,Area=9,Camera=10,GlobalMap=11,LocalMap
    }

    public enum PointID
    {
        None = 0, Left = 1, Right = 2
    }

    public static class ModelPicking
    {
        public static Color GetColor(int face_id)
        {
            return Color.FromArgb(255,Color.FromArgb(face_id));
        }

        public static int FromRgb(int red, int green, int blue)
        {
            return FromColor(Color.FromArgb(0,red,green,blue));
        }

        public static int FromColor(Color color)
        {
            return color.ToArgb();
        }

    }

    public static class SectionPicking
    {
        public static Color GetColor(Sections section,int group_id,int entry_id,PointID point=PointID.None)
        {
            int Red = (((int)section)<<2) | ((int)point&3);
            int Green = group_id;
            int Blue = entry_id;

            return Color.FromArgb(255, Red, Green, Blue);
        }
        
        public static PickingInfo FromColor(Color color)
        {
            return FromRgb(color.R, color.G, color.B);
        }
        
        public static PickingInfo FromRgb(int red,int green,int blue)
        {
            PickingInfo info = new PickingInfo();

            info.Section = (Sections)((red & 0xFC) >> 2);
            info.PointID = (PointID)(red & 3);
            info.GroupID = green;
            info.EntryID = blue;

            return info;
        }   
        public struct PickingInfo
        {
            public Sections Section;
            public PointID PointID;
            public int GroupID;
            public int EntryID;
        }
    }
}
