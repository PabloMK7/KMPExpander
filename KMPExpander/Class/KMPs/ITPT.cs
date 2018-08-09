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
    public class ITPT
    {
        
        public ITPT()
        {
            Signature = "TPTI";
        }
        public ITPT(EndianBinaryReaderEx er)
        {
            Signature = er.ReadString(Encoding.ASCII, 4);
            if (Signature != "TPTI") throw new SignatureNotCorrectException(Signature, "TPTI", er.BaseStream.Position - 4);
            NrEntries = er.ReadUInt32();
            for (int i = 0; i < NrEntries; i++) Entries.Add(new ITPTEntry(er));
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
        public List<ITPTEntry> Entries = new List<ITPTEntry>();
        public class ITPTEntry
        {
            public ITPTEntry() 
            {
                Scale = 1f;
            }
            public ITPTEntry(EndianBinaryReaderEx er)
			{
				er.ReadObject(this);
			}

            public void Write(EndianBinaryWriter er)
            {
                er.Write(Position.X);
                er.Write(Position.Y);
                er.Write(Position.Z);
                er.Write(Scale);
                er.Write(FlyModeVal);
                er.Write(PlayerScanRadiusVal);
            }
            public Vector3 Position { get; set; }
			public Single Scale { get; set; }
			public UInt16 FlyModeVal { get; set; }
            public UInt16 PlayerScanRadiusVal { get; set;}
        }
    }
}
