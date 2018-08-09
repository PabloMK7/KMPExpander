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
    public class AREA
    {
        public AREA() { Signature = "AERA"; }
        public AREA(EndianBinaryReaderEx er)
        {
            Signature = er.ReadString(Encoding.ASCII, 4);
            if (Signature != "AERA") throw new SignatureNotCorrectException(Signature, "AERA", er.BaseStream.Position - 4);
            NrEntries = er.ReadUInt32();
            for (int i = 0; i < NrEntries; i++) Entries.Add(new AREAEntry(er));
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
        public List<AREAEntry> Entries = new List<AREAEntry>();
        
        public class AREAEntry
        {
            public AREAEntry()
            {

            }
            public AREAEntry(EndianBinaryReaderEx er)
            {
                er.ReadObject(this);
                Rotation = new Vector3(RadianDegree.ToDegrees(Rotation.X), RadianDegree.ToDegrees(Rotation.Y), RadianDegree.ToDegrees(Rotation.Z));
            }

            public void Write(EndianBinaryWriter er)
            {
                er.Write(Mode);
                er.Write(Type);
                er.Write(CAMEIndex);
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
                er.Write(Settings1);
                er.Write(Settings2);
                er.Write(RouteID);
                er.Write(EnemyID);
                er.Write(Unknown5);
            }
            public Byte Mode { get; set; }
            public Byte Type { get; set; }
            public SByte CAMEIndex { get; set; }
            public Byte Unknown1 { get; set; }
            public Vector3 Position { get; set; }
            public Vector3 Rotation { get; set; }
            public Vector3 Scale { get; set; }
            public UInt16 Settings1 { get; set; }
            public UInt16 Settings2 { get; set; }
            public sbyte RouteID { get; set; }
            public sbyte EnemyID { get; set; }
            public UInt16 Unknown5 { get; set; }
        }
    }
}
