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
    public class CKPH
    {
        public CKPH() { Signature = "HPKC"; }
		public CKPH(EndianBinaryReaderEx er)
		{
			Signature = er.ReadString(Encoding.ASCII, 4);
			if (Signature != "HPKC") throw new SignatureNotCorrectException(Signature, "HPKC", er.BaseStream.Position - 4);
			NrEntries = er.ReadUInt32();
			for (int i = 0; i < NrEntries; i++) Entries.Add(new CKPHEntry(er));
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
        public List<CKPHEntry> Entries = new List<CKPHEntry>();

		public class CKPHEntry
		{
			public CKPHEntry()
			{
				Previous = new sbyte[] { -1, -1, -1, -1, -1, -1 };
				Next = new sbyte[] { -1, -1, -1, -1, -1, -1 };
			}
			public CKPHEntry(EndianBinaryReaderEx er)
			{
				er.ReadObject(this);
			}

            public void Write(EndianBinaryWriter er)
            {
                er.Write(Start);
                er.Write(Length);
                for (int i = 0; i < 6; i++)
                {
                    er.Write(Previous[i]);
                }
                for (int i = 0; i < 6; i++)
                {
                    er.Write(Next[i]);
                }
                er.Write(Unknown);
            }
			public Byte Start { get; set; }
			public Byte Length { get; set; }
			[BinaryFixedSize(6)]
			public SByte[] Previous { get; set; }
			[BinaryFixedSize(6)]
			public SByte[] Next { get; set; }
			public UInt16 Unknown { get; set; }
		}
	}
}
