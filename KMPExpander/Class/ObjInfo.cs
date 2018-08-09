using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml.Serialization;

namespace KMPExpander.Class
{

    // NOTA: El código generado puede requerir, como mínimo, .NET Framework 4.5 o .NET Core/Standard 2.0.
    /// <remarks/>
    [System.SerializableAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
    [System.Xml.Serialization.XmlRootAttribute(Namespace = "", IsNullable = false)]
    public partial class ObjectInfo
    {

        private ObjectInfoEntry[] entryField;

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute("Entry")]
        public ObjectInfoEntry[] Entry
        {
            get
            {
                return this.entryField;
            }
            set
            {
                this.entryField = value;
            }
        }
    }

    /// <remarks/>
    [System.SerializableAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
    public partial class ObjectInfoEntry
    {

        private string idField;

        private string internalField;

        private string valueField;

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string id
        {
            get
            {
                return this.idField;
            }
            set
            {
                this.idField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string @internal
        {
            get
            {
                return this.internalField;
            }
            set
            {
                this.internalField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlTextAttribute()]
        public string Value
        {
            get
            {
                return this.valueField;
            }
            set
            {
                this.valueField = value;
            }
        }
    }

    public class ObjList
    {
        public ObjectInfo Objinf;
        public Dictionary<UInt16, string> displayName;

        public ObjList()
        {
            displayName = new Dictionary<ushort, string>();
            try
            {
                byte[] xmlfile = File.ReadAllBytes(Application.StartupPath + @"\ObjFlow.xml");
                MemoryStream m = new MemoryStream(xmlfile);
                XmlSerializer deserializer = new XmlSerializer(typeof(ObjectInfo));
                TextReader reader = new StreamReader(m);
                Objinf = (ObjectInfo)deserializer.Deserialize(reader);
                reader.Close();
                PopulateDictionary();
            } catch
            {
                Objinf = new ObjectInfo();
            }
        }
        public void PopulateDictionary()
        {
            for (int i = 0; i < Objinf.Entry.Length; i++)
            {
                UInt16 id = Convert.ToUInt16(Objinf.Entry[i].id, 16);
                if (!displayName.ContainsKey(id)) {
                    displayName.Add(id, Objinf.Entry[i].Value + " [" + Objinf.Entry[i].@internal + "]");
                }
            }
        }
        public string GetDisplayName(UInt16 id)
        {
            return displayName[id];
        }
    }
}
