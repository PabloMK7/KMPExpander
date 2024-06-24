using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using LibCTR.Collections;
using LibEndianBinaryIO;
using LibEndianBinaryIO.Serialization;
using Tao.OpenGl;

namespace KMPSections
{
    public class ENPT
    {
        public ENPT()
        {
            Signature = "TPNE";
        }
        public ENPT(EndianBinaryReaderEx er)
        {
            Signature = er.ReadString(Encoding.ASCII, 4);
            if (Signature != "TPNE") throw new SignatureNotCorrectException(Signature, "TPNE", er.BaseStream.Position - 4);
            NrEntries = er.ReadUInt32();
            for (int i = 0; i < NrEntries; i++) Entries.Add(new ENPTEntry(er));
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
        public List<ENPTEntry> Entries = new List<ENPTEntry>();
        public class ENPTEntry
        {
            public ENPTEntry() 
            {
                Scale = 1f;
            }
            public ENPTEntry(EndianBinaryReaderEx er)
			{
				er.ReadObject(this);
			}
            public void Write(EndianBinaryWriter er)
            {
                er.Write(Position.X);
                er.Write(Position.Y);
                er.Write(Position.Z);
                er.Write(Scale);
                er.Write(MushSettingsVal);
                er.Write(DriftSettingsVal);
                er.Write(Flags);
                er.Write(PathFindOptsVal);
                er.Write(MaxSearchYOffsetVal);
            }
            public Vector3 Position { get; set; }
			public Single Scale { get; set; }
            public UInt16 MushSettingsVal { get; set; }
            public byte DriftSettingsVal { get; set; }
            public byte Flags { get; set; }
            public Int16 PathFindOptsVal { get; set; }
            public Int16 MaxSearchYOffsetVal { get; set; }
        }
    }
}
