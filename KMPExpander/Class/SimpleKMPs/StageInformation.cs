using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using LibCTR.Collections;
using KMPSections;
using System.Windows.Forms;
using System.Drawing;
using Tao.OpenGl;
using System.ComponentModel;

namespace KMPExpander.Class.SimpleKMPs
{
    public class StageInformation : SectionBase<StageInformation.StageInformationEntry>
    {
        //[XmlElement("Entry")]
        //public List<StartEntry> Entries = new List<StartEntry>();
        public StageInformation() { }

        public StageInformation(STGI data)
        {
            foreach (STGI.STGIEntry entry in data.Entries)
                Entries.Add(new StageInformationEntry(entry));
        }

        public class StageInformationEntry
        {
            [XmlAttribute, DisplayName("LapCount (Unused)")]
            public Byte LapCount { get; set; }
            [XmlAttribute, Browsable(false)]
            public Byte PolePositionRaw { get; set; }
            [XmlAttribute]
            public string PolePosition
            {
                get
                {
                    switch (PolePositionRaw)
                    {
                        case 0:
                            return "0 - Right";
                        case 1:
                            return "1 - Left";
                        default:
                            return PolePositionRaw + " - Unknown";
                    }
                }
                set
                {
                    Byte val = Byte.Parse(value);
                    PolePositionRaw = val;
                }
            }
            [XmlAttribute]
            public Byte Unknown1 { get; set; }
            [XmlAttribute]
            public Byte Unknown2 { get; set; }
            [XmlAttribute, Browsable(false)]
            public Int32 FlareColorRaw { get; set; }
            [XmlAttribute]
            public Color FlareColor
            {
                get
                {
                    return Color.FromArgb((FlareColorRaw >> 16) & 0xFF, (FlareColorRaw >> 8) & 0xFF, (FlareColorRaw) & 0xFF);
                }
                set
                {
                    FlareColorRaw = (value.R << 16) | (value.G << 8) | (value.B);
                }
            }
            [XmlAttribute]
            public Byte FlareAlpha
            {
                get
                {
                    return (Byte)FlareAlphaRaw;
                }
                set
                {
                    FlareAlphaRaw = value;
                }
            }
            [XmlAttribute, Browsable(false)]
            public Int32 FlareAlphaRaw { get; set; }

            public StageInformationEntry(STGI.STGIEntry entry)
            {
                LapCount = entry.NrLaps;
                PolePositionRaw = entry.Unknown1;
                Unknown1 = entry.Unknown2;
                Unknown2 = entry.Unknown3;
                FlareColorRaw = entry.Unknown4;
                FlareAlphaRaw = entry.Unknown5;
            }

            public StageInformationEntry() { }

            public STGI.STGIEntry ToSTGIEntry()
            {
                STGI.STGIEntry entry = new STGI.STGIEntry();
                entry.NrLaps = LapCount;
                entry.Unknown1 = PolePositionRaw;
                entry.Unknown2 = Unknown1;
                entry.Unknown3 = Unknown2;
                entry.Unknown4 = FlareColorRaw;
                entry.Unknown5 = FlareAlphaRaw;
                return entry;
            }
        }

        public STGI ToSTGI()
        {
            STGI data = new STGI();
            data.NrEntries = (uint)Entries.Count;
            foreach (var entry in Entries)
            {
                data.Entries.Add(entry.ToSTGIEntry());
            }
            return data;
        }

        public TreeNode GetTreeNode()
        {
            TreeNode node = new TreeNode("Stage Information");
            node.Tag = this;
            return node;
        }

        public void Render(bool picking)
        {
            return;
        }
    }
}
