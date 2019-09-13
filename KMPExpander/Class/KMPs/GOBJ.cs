using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LibCTR.Collections;
using LibEndianBinaryIO;
using LibEndianBinaryIO.Serialization;
using Extensions;

namespace KMPSections
{
    public class GOBJ
    {
            public GOBJ() { Signature = "JBOG"; }
            public GOBJ(EndianBinaryReaderEx er)
            {
                Signature = er.ReadString(Encoding.ASCII, 4);
                if (Signature != "JBOG") throw new SignatureNotCorrectException(Signature, "JBOG", er.BaseStream.Position - 4);
                NrEntries = er.ReadUInt32();
                for (int i = 0; i < NrEntries; i++) Entries.Add(new GOBJEntry(er));
            }

            public void Write(EndianBinaryWriter er)
            {
                er.Write(Signature, Encoding.ASCII, false);
                NrEntries = (uint)Entries.Count;
                er.Write(NrEntries);
                for (int i = 0; i < NrEntries; i++) Entries[i].Write(er);
            }
            public String Signature;
            public UInt32 NrEntries;
            public List<GOBJEntry> Entries = new List<GOBJEntry>();
            public class GOBJEntry
            {
                public GOBJEntry()
                {
                    ObjectID = 0x05;//coin
                    Unknown1 = 0;
                    Position = new Vector3(0, 0, 0);
                    Rotation = new Vector3(0, 0, 0);
                    Scale = new Vector3(1, 1, 1);
                    RouteID = -1;
                    Settings1 = 0;
                    Settings2 = 0;
                    Settings3 = 0;
                    Settings4 = 0;
                    Settings5 = 0;
                    Settings6 = 0;
                    Settings7 = 0;
                    Settings8 = 0;
                    Visibility = 7;
                    Unknown2 = -1;
                    Unknown3 = 0;
                }
                public GOBJEntry(EndianBinaryReaderEx er)
                {
                    er.ReadObject(this);
                    Rotation = new Vector3(RadianDegree.ToDegrees(Rotation.X), RadianDegree.ToDegrees(Rotation.Y), RadianDegree.ToDegrees(Rotation.Z));
                }

                public void Write(EndianBinaryWriter er)
                {
                    er.Write(ObjectID);
                    er.Write(Unknown1);
                    er.Write(Position.X);
                    er.Write(Position.Y);
                    er.Write(Position.Z);
                    er.Write(RadianDegree.ToRadians(Rotation.X));
                    er.Write(RadianDegree.ToRadians(Rotation.Y));
                    er.Write(RadianDegree.ToRadians(Rotation.Z));
                    er.Write(Scale.X);
                    er.Write(Scale.Y);
                    er.Write(Scale.Z);
                    er.Write(RouteID);
                    er.Write(Settings1);
                    er.Write(Settings2);
                    er.Write(Settings3);
                    er.Write(Settings4);
                    er.Write(Settings5);
                    er.Write(Settings6);
                    er.Write(Settings7);
                    er.Write(Settings8);
                    er.Write(Visibility);
                    er.Write(Unknown2);
                    er.Write(Unknown3);
                }
                public UInt16 ObjectID { get; set; }
                public UInt16 Unknown1 { get; set; }
                public Vector3 Position { get; set; }
                public Vector3 Rotation { get; set; }
                public Vector3 Scale { get; set; }
                public Int16 RouteID { get; set; }
                public UInt16 Settings1 { get; set; }
                public UInt16 Settings2 { get; set; }
                public UInt16 Settings3 { get; set; }
                public UInt16 Settings4 { get; set; }
                public UInt16 Settings5 { get; set; }
                public UInt16 Settings6{ get; set; }
                public UInt16 Settings7 { get; set; }
                public UInt16 Settings8 { get; set; }
                public UInt16 Visibility { get; set; }
                public Int16 Unknown2 { get; set; }
                public UInt16 Unknown3 { get; set; }

                public override string ToString()
                {
                    return "? (" + String.Format("{0:X4}", ObjectID) + ")";
                }
            }
    }
}
