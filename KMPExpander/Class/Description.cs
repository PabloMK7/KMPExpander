using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml.Serialization;


// NOTA: El código generado puede requerir, como mínimo, .NET Framework 4.5 o .NET Core/Standard 2.0.
/// <remarks/>
[System.SerializableAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
[System.Xml.Serialization.XmlRootAttribute(Namespace = "", IsNullable = false)]
public partial class KMPExpanderDescriptions
{

    private BigSection[] bigSectionField;

    /// <remarks/>
    [System.Xml.Serialization.XmlElementAttribute("BigSection")]
    public BigSection[] bigSection
    {
        get
        {
            return this.bigSectionField;
        }
        set
        {
            this.bigSectionField = value;
        }
    }
}

/// <remarks/>
[System.SerializableAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
public partial class BigSection
{

    private SubSection[] subSectionField;

    private string nameField;

    /// <remarks/>
    [System.Xml.Serialization.XmlElementAttribute("SubSection")]
    public SubSection[] subSection
    {
        get
        {
            return this.subSectionField;
        }
        set
        {
            this.subSectionField = value;
        }
    }

    /// <remarks/>
    [System.Xml.Serialization.XmlAttributeAttribute()]
    public string Name
    {
        get
        {
            return this.nameField;
        }
        set
        {
            this.nameField = value;
        }
    }
}

/// <remarks/>
[System.SerializableAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
public partial class SubSection
{

    private Item[] itemField;

    private string nameField;

    /// <remarks/>
    [System.Xml.Serialization.XmlElementAttribute("Item")]
    public Item[] item
    {
        get
        {
            return this.itemField;
        }
        set
        {
            this.itemField = value;
        }
    }

    /// <remarks/>
    [System.Xml.Serialization.XmlAttributeAttribute()]
    public string Name
    {
        get
        {
            return this.nameField;
        }
        set
        {
            this.nameField = value;
        }
    }
}

/// <remarks/>
[System.SerializableAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
public partial class Item
{

    private string nameField;

    private string valueField;

    /// <remarks/>
    [System.Xml.Serialization.XmlAttributeAttribute()]
    public string Name
    {
        get
        {
            return this.nameField;
        }
        set
        {
            this.nameField = value;
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

public class Description
{
    public KMPExpanderDescriptions d;
    public Description()
    {
        byte[] xmlfile = File.ReadAllBytes(Application.StartupPath + @"\descriptions.xml");
        MemoryStream m = new MemoryStream(xmlfile);
        XmlSerializer deserializer = new XmlSerializer(typeof(KMPExpanderDescriptions));
        TextReader reader = new StreamReader(m);
        d = (KMPExpanderDescriptions)deserializer.Deserialize(reader);
        reader.Close();
    }
}


