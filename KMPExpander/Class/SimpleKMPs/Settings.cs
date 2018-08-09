using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using LibCTR.Collections;
using KMPSections;
using System.Windows.Forms;

namespace KMPExpander.Class.SimpleKMPs
{
    public class Settings
    {
        [XmlAttribute]
        public Byte NrLaps { get; set; }
        [XmlAttribute]
        public Byte Unknown1 { get; set; }
        [XmlAttribute]
        public Byte Unknown2 { get; set; }
        [XmlAttribute]
        public Byte Unknown3 { get; set; }
        [XmlAttribute]
        public UInt32 Unknown4 { get; set; }
        [XmlAttribute]
        public UInt16 Unknown5 { get; set; }
        [XmlAttribute]
        public UInt16 Unknown6 { get; set; }
        public Settings() { NrLaps = 3; }

        public Settings(STGI data)
        {
            STGI.STGIEntry entry = data.Entries[0];
            NrLaps = entry.NrLaps;
            Unknown1 = entry.Unknown1;
            Unknown2 = entry.Unknown2;
            Unknown3 = entry.Unknown3;
            Unknown4 = entry.Unknown4;
            Unknown5 = entry.Unknown5;
            Unknown6 = entry.Unknown6;
        }
        public STGI ToSTGI()
        {
            STGI data = new STGI();
            STGI.STGIEntry entry = new STGI.STGIEntry();
            entry.NrLaps = NrLaps;
            entry.Unknown1 = Unknown1;
            entry.Unknown2 = Unknown2;
            entry.Unknown3 = Unknown3;
            entry.Unknown4 = Unknown4;
            entry.Unknown5 = Unknown5;
            entry.Unknown6 = Unknown6;
            data.Entries.Add(entry);
            data.NrEntries = 0x01;
            return data;
        }

        public TreeNode GetTreeNode()
        {
            TreeNode node = new TreeNode("Stage Settings");
            return node;
        }
    }
}
