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
    public class KTPT
    {
        public KTPT()
        {
            Signature = "TPTK";
        }

        public KTPT(EndianBinaryReaderEx er)
        {
            Signature = er.ReadString(Encoding.ASCII, 4);
            if (Signature != "TPTK") throw new SignatureNotCorrectException(Signature, "TPTK", er.BaseStream.Position - 4);
            NrEntries = er.ReadUInt32();
            for (int i = 0; i < NrEntries; i++) Entries.Add(new KTPTEntry(er));
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
        public List<KTPTEntry> Entries = new List<KTPTEntry>();
        public class KTPTEntry
		{
			public KTPTEntry() { }
			public KTPTEntry(EndianBinaryReaderEx er)
			{
				er.ReadObject(this);
				Rotation = new Vector3(RadianDegree.ToDegrees(Rotation.X), RadianDegree.ToDegrees(Rotation.Y), RadianDegree.ToDegrees(Rotation.Z));
			}

            public void Write(EndianBinaryWriter er)
            {
                er.Write(Position.X);
                er.Write(Position.Y);
                er.Write(Position.Z);
                er.Write(RadianDegree.ToRadians(Rotation.X));
                er.Write(RadianDegree.ToRadians(Rotation.Y));
                er.Write(RadianDegree.ToRadians(Rotation.Z));
                er.Write(Index);
            }
			public Vector3 Position { get; set; }
			public Vector3 Rotation { get; set; }
			public UInt32 Index { get; set; }
		}
    }
}
