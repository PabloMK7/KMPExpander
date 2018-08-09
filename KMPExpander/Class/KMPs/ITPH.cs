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
    public class ITPH
    {
        public ITPH()
        {
            Signature = "HPTI";
        }
        public ITPH(EndianBinaryReaderEx er)
        {
            Signature = er.ReadString(Encoding.ASCII, 4);
            if (Signature != "HPTI") throw new SignatureNotCorrectException(Signature, "HPTI", er.BaseStream.Position - 4);
            NrEntries = er.ReadUInt32();
            for (int i = 0; i < NrEntries; i++) Entries.Add(new ITPHEntry(er));
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
        public List<ITPHEntry> Entries = new List<ITPHEntry>();
        public class ITPHEntry
        {
            public ITPHEntry()
            {
                Previous = new short[] { -1, -1, -1, -1, -1, -1 };
                Next = new short[] { -1, -1, -1, -1, -1, -1 };
            }
            public ITPHEntry(EndianBinaryReaderEx er)
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
            }
            public UInt16 Start { get; set; }
            public UInt16 Length { get; set; }
            [BinaryFixedSize(6)]
            public Int16[] Previous { get; set; }
            [BinaryFixedSize(6)]
            public Int16[] Next { get; set; }
        }
    }
}
