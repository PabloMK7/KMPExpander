using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading.Tasks;
using LibEndianBinaryIO;
using LibEndianBinaryIO.Serialization;
using KMPSections;

namespace KMPClass
{
    public class KMP
    {
        public KMP(byte[] Data)
        {
            EndianBinaryReaderEx er = new EndianBinaryReaderEx(new MemoryStream(Data), Endianness.LittleEndian);
            try
            {
                Header = new KMPHeader(er);
                foreach (var v in Header.SectionOffsets)
                {
                    er.BaseStream.Position = Header.HeaderSize + v;
                    String sig = er.ReadString(Encoding.ASCII, 4);
                    er.BaseStream.Position -= 4;
                    switch (sig)
                    {
                        case "TPTK": KartPoint = new KTPT(er); break;
                        case "TPNE": EnemyPoint = new ENPT(er); break;
                        case "HPNE": EnemyPointPath = new ENPH(er); break;
                        case "TPTI": ItemPoint = new ITPT(er); break;
                        case "HPTI": ItemPointPath = new ITPH(er); break;
                        case "TPKC": CheckPoint = new CKPT(er); break;
                        case "HPKC": CheckPointPath = new CKPH(er); break;
                        case "JBOG": GlobalObject = new GOBJ(er); break;
                        case "ITOP": PointInfo = new POTI(er); break;
                        case "AERA": Area = new AREA(er); break;
                        case "EMAC": Camera = new CAME(er); break;
                        case "TPGJ": JugemPoint = new JGPT(er); break;
                        case "TPNC": CannonPoint = new CNPT(er); break;
                        case "TPSM": MissionPoint = new MSPT(er); break;
                        case "IGTS": StageInfo = new STGI(er); break;
                        case "SROC": CourseSect = new CORS(er); break;
                        case "TPLG": GliderPoint = new GLPT(er); break;
                        case "HPLG": GliderPointPath = new GLPH(er); break;
                        default: continue;
                    }
                }
            }
            finally
            {
                er.Close();
            }
        }

        public KMP()
        {
            Header = new KMPHeader();
        }

        public byte[] Write()
        {
            MemoryStream m = new MemoryStream();
            EndianBinaryWriter er = new EndianBinaryWriter(m, Endianness.LittleEndian);
            int NrSections = 0x12;
            Header.SectionOffsets = new UInt32[NrSections];
            Header.Write(er);

            int SectionIdx = 0;
            if (KartPoint != null)
            {
                WriteHeaderInfo(er, SectionIdx);
                KartPoint.Write(er);
                SectionIdx++;
            }
            if (EnemyPoint != null)
            {
                WriteHeaderInfo(er, SectionIdx);
                EnemyPoint.Write(er);
                SectionIdx++;
            }
            if (EnemyPointPath != null)
            {
                WriteHeaderInfo(er, SectionIdx);
                EnemyPointPath.Write(er);
                SectionIdx++;
            }
            if (ItemPoint != null)
            {
                WriteHeaderInfo(er, SectionIdx);
                ItemPoint.Write(er);
                SectionIdx++;
            }
            if (ItemPointPath != null)
            {
                WriteHeaderInfo(er, SectionIdx);
                ItemPointPath.Write(er);
                SectionIdx++;
            }
            if (CheckPoint != null)
            {
                WriteHeaderInfo(er, SectionIdx);
                CheckPoint.Write(er);
                SectionIdx++;
            }
            if (CheckPointPath != null)
            {
                WriteHeaderInfo(er, SectionIdx);
                CheckPointPath.Write(er);
                SectionIdx++;
            }
            if (GlobalObject != null)
            {
                WriteHeaderInfo(er, SectionIdx);
                GlobalObject.Write(er);
                SectionIdx++;
            }
            if (PointInfo != null)
            {
                WriteHeaderInfo(er, SectionIdx);
                PointInfo.Write(er);
                SectionIdx++;
            }
            if (Area != null)
            {
                WriteHeaderInfo(er, SectionIdx);
                Area.Write(er);
                SectionIdx++;
            }
            if (Camera != null)
            {
                WriteHeaderInfo(er, SectionIdx);
                Camera.Write(er);
                SectionIdx++;
            }
            if (JugemPoint != null)
            {
                WriteHeaderInfo(er, SectionIdx);
                JugemPoint.Write(er);
                SectionIdx++;
            }
            if (CannonPoint != null)
            {
                WriteHeaderInfo(er, SectionIdx);
                CannonPoint.Write(er);
                SectionIdx++;
            }
            if (MissionPoint != null)
            {
                WriteHeaderInfo(er, SectionIdx);
                MissionPoint.Write(er);
                SectionIdx++;
            }
            if (StageInfo != null)
            {
                WriteHeaderInfo(er, SectionIdx);
                StageInfo.Write(er);
                SectionIdx++;
            }
            if (CourseSect != null)
            {
                WriteHeaderInfo(er, SectionIdx);
                CourseSect.Write(er);
                SectionIdx++;
            }
            if (GliderPoint != null)
            {
                WriteHeaderInfo(er, SectionIdx);
                GliderPoint.Write(er);
                SectionIdx++;
            }
            if (GliderPointPath != null)
            {
                WriteHeaderInfo(er, SectionIdx);
                GliderPointPath.Write(er);
                SectionIdx++;
            }
            WriteHeaderFileSize(er);
            byte[] result = m.ToArray();
            er.Close();
            return result;
        }

        private void WriteHeaderInfo(EndianBinaryWriter er, int Index)
        {
            long curpos = er.BaseStream.Position;
            er.BaseStream.Position = 16 + Index * 4;
            er.Write((UInt32)(curpos - Header.HeaderSize));
            er.BaseStream.Position = curpos;
        }

        private void WriteHeaderFileSize(EndianBinaryWriter er)
        {
            UInt32 FileSize = (UInt32)er.BaseStream.Position;
            er.BaseStream.Position = 4;
            er.Write((UInt32)(FileSize));
        }

        public KMPHeader Header;
        public class KMPHeader
        {
            public KMPHeader()
            {
                Signature = "DMDC";
                FileSize = 0;
                NrSections = 0x12;
                HeaderSize = 0x58;
                Version = 0x0C1C;
                SectionOffsets = null;
            }

            public KMPHeader(EndianBinaryReaderEx er)
            {
                er.ReadObject(this);
				SectionOffsets = er.ReadUInt32s(NrSections);
			}
            public void Write(EndianBinaryWriter er)
            {
                er.Write(Signature, Encoding.ASCII, false);
                FileSize = 0;
                er.Write(FileSize);
                er.Write(NrSections);
                er.Write(HeaderSize);
                er.Write(Version);
                er.Write(SectionOffsets, 0, SectionOffsets.Length);
            }
			[BinaryStringSignature("DMDC")]
			[BinaryFixedSize(4)]
			public String Signature;
			public UInt32 FileSize;
			public UInt16 NrSections;
			public UInt16 HeaderSize;
			public UInt32 Version;
			[BinaryIgnore]
			public UInt32[] SectionOffsets;
        }
        public KTPT KartPoint;
        public ENPT EnemyPoint;
        public ENPH EnemyPointPath;
        public ITPT ItemPoint;
        public ITPH ItemPointPath;
        public CKPT CheckPoint;
        public CKPH CheckPointPath;
        public GOBJ GlobalObject;
        public POTI PointInfo;
        public AREA Area;
        public CAME Camera;
        public JGPT JugemPoint;
        public CNPT CannonPoint;
        public MSPT MissionPoint;
        public STGI StageInfo;
        public CORS CourseSect;
        public GLPT GliderPoint;
        public GLPH GliderPointPath;
    }
}
