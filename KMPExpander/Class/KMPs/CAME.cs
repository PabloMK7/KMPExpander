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
    public class CAME
    {
            public CAME() { Signature = "EMAC"; Unknown = 0xFFFF; }
            public CAME(EndianBinaryReaderEx er)
            {
                Signature = er.ReadString(Encoding.ASCII, 4);
                if (Signature != "EMAC") throw new SignatureNotCorrectException(Signature, "EMAC", er.BaseStream.Position - 4);
                NrEntries = er.ReadUInt16();
                Unknown = er.ReadUInt16();
                for (int i = 0; i < NrEntries; i++) Entries.Add(new CAMEEntry(er));
            }
            public void Write(EndianBinaryWriter er)
            {
                er.Write(Signature, Encoding.ASCII, false);
                NrEntries = (UInt16)Entries.Count;
                er.Write(NrEntries);
                er.Write(Unknown);
                for (int i = 0; i < NrEntries; i++) Entries[i].Write(er);
            }
            public String Signature;
            public UInt16 NrEntries;
            public UInt16 Unknown;
            public List<CAMEEntry> Entries = new List<CAMEEntry>();

            public class CAMEEntry
            {
                public CAMEEntry()
                {
                    
                }
                public CAMEEntry(EndianBinaryReaderEx er)
                {
                    er.ReadObject(this);
                    Rotation = new Vector3(RadianDegree.ToDegrees(Rotation.X), RadianDegree.ToDegrees(Rotation.Y), RadianDegree.ToDegrees(Rotation.Z));
                }

                public void Write(EndianBinaryWriter er)
                {
                    er.Write(Type);
                    er.Write(Next);
                    er.Write(VideoNext);
                    er.Write(RouteID);
                    er.Write(PointSpeed);
                    er.Write(FOVSpeed);
                    er.Write(ViewpointSpeed);
                    er.Write(StartFlag);
                    er.Write(VideoFlag);
                    er.Write(Position.X);
                    er.Write(Position.Y);
                    er.Write(Position.Z);
                    er.Write(RadianDegree.ToRadians(Rotation.X));
                    er.Write(RadianDegree.ToRadians(Rotation.Y));
                    er.Write(RadianDegree.ToRadians(Rotation.Z));
                    er.Write(FOVBegin);
                    er.Write(FOVEnd);
                    er.Write(Viewpoint1.X);
                    er.Write(Viewpoint1.Y);
                    er.Write(Viewpoint1.Z);
                    er.Write(Viewpoint2.X);
                    er.Write(Viewpoint2.Y);
                    er.Write(Viewpoint2.Z);
                    er.Write(Duration);
                }
                public Byte Type { get; set; }
                public sbyte Next { get; set; }
                public Byte VideoNext { get; set; }
                public sbyte RouteID { get; set; }
                public UInt16 PointSpeed { get; set; }
                public UInt16 FOVSpeed { get; set; }
                public UInt16 ViewpointSpeed { get; set; }
                public Byte StartFlag { get; set; }
                public Byte VideoFlag { get; set; }
                public Vector3 Position { get; set; }
                public Vector3 Rotation { get; set; }
                public Single FOVBegin { get; set; }
                public Single FOVEnd { get; set; }
                public Vector3 Viewpoint1 { get; set; }
                public Vector3 Viewpoint2 { get; set; }
                public Single Duration { get; set; }
            }
    }
}
