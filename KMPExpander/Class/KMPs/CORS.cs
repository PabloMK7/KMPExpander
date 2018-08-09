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
    public class CORS
    {
        public CORS() { Signature = "SROC"; }
        public CORS(EndianBinaryReaderEx er)
        {
            Signature = er.ReadString(Encoding.ASCII, 4);
            if (Signature != "SROC") throw new SignatureNotCorrectException(Signature, "SROC", er.BaseStream.Position - 4);
            NrEntries = er.ReadUInt32();
            //for (int i = 0; i < NrEntries; i++) Entries.Add(new AREAEntry(er));
        }

        public void Write(EndianBinaryWriter er)
        {
            er.Write(Signature, Encoding.ASCII, false);
            //NrEntries = (uint)Entries.Count;
            er.Write(NrEntries);
            //for (int i = 0; i < NrEntries; i++) Entries[i].Write(er);
        }
        public String Signature;
        public UInt32 NrEntries;
        //public List<AREAEntry> Entries = new List<AREAEntry>();
    }
}