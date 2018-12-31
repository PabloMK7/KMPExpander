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
    public class STGI
    {
        public STGI() {
            Signature = "IGTS";
        }
        public STGI(EndianBinaryReaderEx er)
        {
            Signature = er.ReadString(Encoding.ASCII, 4);
            if (Signature != "IGTS") throw new SignatureNotCorrectException(Signature, "IGTS", er.BaseStream.Position - 4);
            NrEntries = er.ReadUInt32();
            for (int i = 0; i < NrEntries; i++) Entries.Add(new STGIEntry(er));
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
        public List<STGIEntry> Entries = new List<STGIEntry>();
        public class STGIEntry
        {
            public STGIEntry() { }
            public STGIEntry(EndianBinaryReaderEx er)
            {
                er.ReadObject(this);
            }

            public void Write(EndianBinaryWriter er)
            {
                er.Write(NrLaps);
                er.Write(Unknown1);
                er.Write(Unknown2);
                er.Write(Unknown3);
                er.Write(Unknown4);
                er.Write(Unknown5);
            }
            public Byte NrLaps { get; set; }
            public Byte Unknown1 { get; set; }
            public Byte Unknown2 { get; set; }
            public Byte Unknown3 { get; set; }
            public Int32 Unknown4 { get; set; }
            public Int32 Unknown5 { get; set; }
        }
    }
}