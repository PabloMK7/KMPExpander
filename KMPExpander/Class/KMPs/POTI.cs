using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LibCTR.Collections;
using LibEndianBinaryIO;
using LibEndianBinaryIO.Serialization;

namespace KMPSections
{
    public class POTI
    {
        public POTI()
        {
            Signature = "ITOP";
        }
        public POTI(EndianBinaryReader er)
        {
            Signature = er.ReadString(Encoding.ASCII, 4);
            if (Signature != "ITOP") throw new SignatureNotCorrectException(Signature, "ITOP", er.BaseStream.Position - 4);
            NrRoutes = er.ReadUInt16();
            NrPoints = er.ReadUInt16();
            for (int i = 0; i < NrRoutes; i++) Routes.Add(new POTIRoute(er));
        }

        public void Write(EndianBinaryWriter er)
        {
            er.Write(Signature, Encoding.ASCII, false);
            NrRoutes = (UInt16)Routes.Count;
            er.Write(NrRoutes);
            NrPoints = 0;
            for (int i = 0; i < NrRoutes; i++)
                NrPoints += (UInt16)Routes[i].Points.Count;
            er.Write(NrPoints);
            for (int i = 0; i < NrRoutes; i++) Routes[i].Write(er);
        }
        public String Signature;
        public UInt16 NrRoutes;
        public UInt16 NrPoints;

        public List<POTIRoute> Routes = new List<POTIRoute>();
        public class POTIRoute
        {
            public POTIRoute() { NrPoints = 0; Setting1 = 0; Setting2 = 0; }
            public POTIRoute(EndianBinaryReader er)
            {
                NrPoints = er.ReadUInt16();
                Setting1 = er.ReadByte();
                Setting2 = er.ReadByte();
                for (int i = 0; i < NrPoints; i++) Points.Add(new POTIPoint(er));
            }

            public void Write(EndianBinaryWriter er)
            {
                NrPoints = (UInt16)Points.Count;
                er.Write(NrPoints);
                er.Write(Setting1);
                er.Write(Setting2);
                for (int i = 0; i < NrPoints; i++) Points[i].Write(er);
            }
            public UInt16 NrPoints;
            public Byte Setting1;
            public Byte Setting2;

            public List<POTIPoint> Points = new List<POTIPoint>();
            public class POTIPoint
            {
                public POTIPoint() { }
                public POTIPoint(EndianBinaryReader er)
                {
                    Position.X = er.ReadSingle();
                    Position.Y = er.ReadSingle();
                    Position.Z = er.ReadSingle();
                    RouteSpeed = er.ReadUInt16();
                    Setting2 = er.ReadUInt16();
                }

                public void Write(EndianBinaryWriter er)
                {
                    er.Write(Position.X);
                    er.Write(Position.Y);
                    er.Write(Position.Z);
                    er.Write(RouteSpeed);
                    er.Write(Setting2);
                }
                public Vector3 Position;
                public UInt16 RouteSpeed;
                public UInt16 Setting2;
            }
        }
    }
}
