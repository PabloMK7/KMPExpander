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
    public class ENPH
    {
        public ENPH()
        {
            Signature = "HPNE";
        }
        public ENPH(EndianBinaryReaderEx er)
        {
            Signature = er.ReadString(Encoding.ASCII, 4);
            if (Signature != "HPNE") throw new SignatureNotCorrectException(Signature, "HPNE", er.BaseStream.Position - 4);
            NrEntries = er.ReadUInt32();
            for (int i = 0; i < NrEntries; i++) Entries.Add(new ENPHEntry(er));
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
        public List<ENPHEntry> Entries = new List<ENPHEntry>();
        public class ENPHEntry
        {
            public ENPHEntry()
            {
                Previous = new short[] { -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 };
                Next = new short[] { -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 };
            }
            public ENPHEntry(EndianBinaryReaderEx er)
            {
                er.ReadObject(this);
            }

            public void Write(EndianBinaryWriter er)
            {
                er.Write(Start);
                er.Write(Length);
                for (int i = 0; i < 16; i++)
                {
                    er.Write(Previous[i]);
                }
                for (int i = 0; i < 16; i++)
                {
                    er.Write(Next[i]);
                }
                er.Write(Unknown);
            }

            public UInt16 Start { get; set; }
            public UInt16 Length { get; set; }
            [BinaryFixedSize(16)]
            public Int16[] Previous { get; set; }
            [BinaryFixedSize(16)]
            public Int16[] Next { get; set; }
            public UInt32 Unknown { get; set; }
        }
    }
}
