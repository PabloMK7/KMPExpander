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
    public class CKPT
    {
        public CKPT() { Signature = "TPKC"; }
        public CKPT(EndianBinaryReaderEx er)
        {
            Signature = er.ReadString(Encoding.ASCII, 4);
            if (Signature != "TPKC") throw new SignatureNotCorrectException(Signature, "TPKC", er.BaseStream.Position - 4);
            NrEntries = er.ReadUInt32();
            for (int i = 0; i < NrEntries; i++) Entries.Add(new CKPTEntry(er));
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
        public List<CKPTEntry> Entries = new List<CKPTEntry>();

        public class CKPTEntry
        {
            public CKPTEntry()
            {
                Key = -1;
                Section = -1;
            }
            public CKPTEntry(EndianBinaryReaderEx er)
            {
                er.ReadObject(this);
            }

            public void Write(EndianBinaryWriter er)
            {
                er.Write(Point1.X);
                er.Write(Point1.Z);
                er.Write(Point2.X);
                er.Write(Point2.Z);
                er.Write(RespawnId);
                er.Write(Key);
                er.Write(Previous);
                er.Write(Next);
                er.Write(ClipID);
                er.Write(Section);
                er.Write(Unknown3);
                er.Write(Unknown4);
            }
            public Vector2 Point1 { get; set; }
            public Vector2 Point2 { get; set; }
            public Byte RespawnId { get; set; }
            public SByte Key { get; set; }
            public Byte Previous { get; set; }
            public Byte Next { get; set; }
            public SByte ClipID { get; set; }
            public SByte Section { get; set; }
            public Byte Unknown3 { get; set; }
            public Byte Unknown4 { get; set; }
        }
    }
}
