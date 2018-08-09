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
    public class GLPT
    {
        public GLPT()
        {
            Signature = "TPLG";
        }
        public GLPT(EndianBinaryReaderEx er)
        {
            Signature = er.ReadString(Encoding.ASCII, 4);
            if (Signature != "TPLG") throw new SignatureNotCorrectException(Signature, "TPLG", er.BaseStream.Position - 4);
            NrEntries = er.ReadUInt32();
            for (int i = 0; i < NrEntries; i++) Entries.Add(new GLPTEntry(er));
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
        public List<GLPTEntry> Entries = new List<GLPTEntry>();
        public class GLPTEntry
        {
            public GLPTEntry() { }
            public GLPTEntry(EndianBinaryReaderEx er)
			{
				er.ReadObject(this);
			}

            public void Write(EndianBinaryWriter er)
            {
                er.Write(Position.X);
                er.Write(Position.Y);
                er.Write(Position.Z);
                er.Write(Scale);
                er.Write(Unknown1);
                er.Write(Unknown2);
            }
            public Vector3 Position { get; set; }
			public Single Scale { get; set; }
			public UInt32 Unknown1 { get; set; }
            public UInt32 Unknown2 { get; set; }
        }
    }
}
