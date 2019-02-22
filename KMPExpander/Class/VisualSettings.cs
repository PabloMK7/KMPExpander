using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using System.Globalization;
using System.ComponentModel;
using System.Xml.Serialization;

namespace KMPExpander.Class
{
    public class VisualSettings
    {
        //General settings
        [Category("Generic Settings")]
        public float PointSize
        {
            get { return point_size; }
            set { point_size = value; }
        }
        private float point_size = 7f;
        private bool object_icons = false;
        [Category("Generic Settings"),Description("Draw Object's icons instead of Circles.")]
        public bool UseObjectIcons
        {
            get { return object_icons; }
            set { object_icons = value; }
        }
        private bool link_points = true;
        [Category("Generic Settings")]
        public bool LinkPoints
        {
            get { return link_points; }
            set { link_points = value; }
        }
        private bool link_paths = true;
        [Category("Generic Settings")]
        public bool LinkPaths
        {
            get { return link_paths; }
            set { link_paths = value; }
        }
        private float line_width = 2f;
        [Category("Generic Settings")]
        public float LineWidth
        {
            get { return line_width; }
            set { line_width = value; }
        }

        private Color bg_color = Color.White;
        [Category("Generic Settings"), XmlIgnore]
        public Color BackgroundColor
        {
            get { return bg_color; }
            set { bg_color = value; }
        }
        [Browsable(false)]
        public string BGColor
        {
            get { return ColorTranslator.ToHtml(bg_color); }
            set { bg_color = ColorTranslator.FromHtml(value); }
        }
        private Color pointborder_color = Color.Black;
        [Category("Generic Settings"), XmlIgnore]
        public Color PointborderColor
        {
            get { return pointborder_color; }
            set { pointborder_color = value; }
        }
        [Browsable(false)]
        public string BorderColor
        {
            get { return ColorTranslator.ToHtml(pointborder_color); }
            set { pointborder_color = ColorTranslator.FromHtml(value); }
        }
        //public Color PointColor = Color.Green;
        private Color highlightpoint_color = Color.Blue;
        [Category("Generic Settings"), XmlIgnore]
        public Color HighlightPointColor
        {
            get { return highlightpoint_color; }
            set { highlightpoint_color = value; }
        }
        [Browsable(false)]
        public string HighlightColor
        {
            get { return ColorTranslator.ToHtml(highlightpoint_color); }
            set { highlightpoint_color = ColorTranslator.FromHtml(value); }
        }
        private Color highlightpointborder_color = Color.LightBlue;
        [Category("Generic Settings"), XmlIgnore]
        public Color HighlightPointborderColor
        {
            get { return highlightpointborder_color; }
            set { highlightpointborder_color = value; }
        }
        [Browsable(false)]
        public string HighlightBordColor
        {
            get { return ColorTranslator.ToHtml(highlightpointborder_color); }
            set { highlightpointborder_color = ColorTranslator.FromHtml(value); }
        }
        //Section-specific Colors settings
        private Color area_color = Color.Brown;
        [Category("Section Specific Colors"), XmlIgnore]
        public Color AreaColor
        {
            get { return area_color; }
            set { area_color = value; }
        }
        [Browsable(false)]
        public string Area
        {
            get { return ColorTranslator.ToHtml(area_color); }
            set { area_color = ColorTranslator.FromHtml(value); }
        }

        private Color came_color = Color.RosyBrown;
        [Category("Section Specific Colors"), XmlIgnore]
        public Color CameColor
        {
            get { return came_color; }
            set { came_color = value; }
        }
        [Browsable(false)]
        public string Camera
        {
            get { return ColorTranslator.ToHtml(came_color); }
            set { came_color = ColorTranslator.FromHtml(value); }
        }

        private Color ckptleft_color = Color.Lime;
        [Category("Section Specific Colors"), XmlIgnore]
        public Color CkptLeftColor
        {
            get { return ckptleft_color; }
            set { ckptleft_color = value; }
        }
        [Browsable(false)]
        public string CheckpointLeft
        {
            get { return ColorTranslator.ToHtml(ckptleft_color); }
            set { ckptleft_color = ColorTranslator.FromHtml(value); }
        }

        private Color keyckptleft_color = Color.Cyan;
        [Category("Section Specific Colors"), XmlIgnore]
        public Color CkptKeyLeftColor
        {
            get { return keyckptleft_color; }
            set { keyckptleft_color = value; }
        }
        [Browsable(false)]
        public string KeyCheckpointLeft
        {
            get { return ColorTranslator.ToHtml(keyckptleft_color); }
            set { keyckptleft_color = ColorTranslator.FromHtml(value); }
        }
        private Color sectionckptleft_color = Color.LightGray;
        [Category("Section Specific Colors"), XmlIgnore]
        public Color CkptSectionLeftColor
        {
            get { return sectionckptleft_color; }
            set { sectionckptleft_color = value; }
        }
        [Browsable(false)]
        public string SectionCheckpointLeft
        {
            get { return ColorTranslator.ToHtml(sectionckptleft_color); }
            set { sectionckptleft_color = ColorTranslator.FromHtml(value); }
        }

        private Color ckptright_color = Color.Red;
        [Category("Section Specific Colors"), XmlIgnore]
        public Color CkptRightColor
        {
            get { return ckptright_color; }
            set { ckptright_color = value; }
        }
        [Browsable(false)]
        public string CheckpointRight
        {
            get { return ColorTranslator.ToHtml(ckptright_color); }
            set { ckptright_color = ColorTranslator.FromHtml(value); }
        }

        private Color keyckptright_color = Color.Fuchsia;
        [Category("Section Specific Colors"), XmlIgnore]
        public Color CkptKeyRightColor
        {
            get { return keyckptright_color; }
            set { keyckptright_color = value; }
        }
        [Browsable(false)]
        public string KeyCheckpointRight
        {
            get { return ColorTranslator.ToHtml(keyckptright_color); }
            set { keyckptright_color = ColorTranslator.FromHtml(value); }
        }
        private Color sectionckptright_color = Color.DimGray;
        [Category("Section Specific Colors"), XmlIgnore]
        public Color CkptSectionRightColor
        {
            get { return sectionckptright_color; }
            set { sectionckptright_color = value; }
        }
        [Browsable(false)]
        public string SectionCheckpointRight
        {
            get { return ColorTranslator.ToHtml(sectionckptright_color); }
            set { sectionckptright_color = ColorTranslator.FromHtml(value); }
        }

        private Color enemy_color = Color.Black;
        [Category("Section Specific Colors"), XmlIgnore]
        public Color EnemyColor
        {
            get { return enemy_color; }
            set { enemy_color = value; }
        }
        [Browsable(false)]
        public string Enemy
        {
            get { return ColorTranslator.ToHtml(enemy_color); }
            set { enemy_color = ColorTranslator.FromHtml(value); }
        }

        private Color enemylink_color = Color.FromArgb(0xFF, 0x10, 0x10, 0x10);
        [Category("Section Specific Colors"), XmlIgnore]
        public Color EnemyLinkColor
        {
            get { return enemylink_color; }
            set { enemylink_color = value; }
        }
        [Browsable(false)]
        public string EnemyLink
        {
            get { return ColorTranslator.ToHtml(enemylink_color); }
            set { enemylink_color = ColorTranslator.FromHtml(value); }
        }

        private Color glider_color = Color.LightBlue;
        [Category("Section Specific Colors"), XmlIgnore]
        public Color GliderColor
        {
            get { return glider_color; }
            set { glider_color = value; }
        }
        [Browsable(false)]
        public string Glider
        {
            get { return ColorTranslator.ToHtml(glider_color); }
            set { glider_color = ColorTranslator.FromHtml(value); }
        }

        private Color gliderlink_color = Color.LightBlue;
        [Category("Section Specific Colors"), XmlIgnore]
        public Color GliderLinkColor
        {
            get { return gliderlink_color; }
            set { gliderlink_color = value; }
        }
        [Browsable(false)]
        public string GliderLink
        {
            get { return ColorTranslator.ToHtml(gliderlink_color); }
            set { gliderlink_color = ColorTranslator.FromHtml(value); }
        }

        private Color objects_color = Color.Red;
        [Category("Section Specific Colors"), XmlIgnore]
        public Color ObjectsColor
        {
            get { return objects_color; }
            set { objects_color = value; }
        }
        [Browsable(false)]
        public string Objects
        {
            get { return ColorTranslator.ToHtml(objects_color); }
            set { objects_color = ColorTranslator.FromHtml(value); }
        }

        private Color item_color = Color.Green;
        [Category("Section Specific Colors"), XmlIgnore]
        public Color ItemColor
        {
            get { return item_color; }
            set { item_color = value; }
        }
        [Browsable(false)]
        public string Item
        {
            get { return ColorTranslator.ToHtml(item_color); }
            set { item_color = ColorTranslator.FromHtml(value); }
        }

        private Color itemlink_color = Color.Green;
        [Category("Section Specific Colors"), XmlIgnore]
        public Color ItemLinkColor
        {
            get { return itemlink_color; }
            set { itemlink_color = value; }
        }
        [Browsable(false)]
        public string ItemLink
        {
            get { return ColorTranslator.ToHtml(itemlink_color); }
            set { itemlink_color = ColorTranslator.FromHtml(value); }
        }

        private Color jugem_color = Color.Yellow;
        [Category("Section Specific Colors"), XmlIgnore]
        public Color JugemColor
        {
            get { return jugem_color; }
            set { jugem_color = value; }
        }
        [Browsable(false)]
        public string Jugem
        {
            get { return ColorTranslator.ToHtml(jugem_color); }
            set { jugem_color = ColorTranslator.FromHtml(value); }
        }

        private Color ktpt_color = Color.Gray;
        [Category("Section Specific Colors"), XmlIgnore]
        public Color KtptColor
        {
            get { return ktpt_color; }
            set { ktpt_color = value; }
        }
        [Browsable(false)]
        public string KartPoints
        {
            get { return ColorTranslator.ToHtml(ktpt_color); }
            set { ktpt_color = ColorTranslator.FromHtml(value); }
        }

        private Color route_color = Color.Blue;
        [Category("Section Specific Colors"), XmlIgnore]
        public Color RouteColor
        {
            get { return route_color; }
            set { route_color = value; }
        }
        [Browsable(false)]
        public string RoutePoint
        {
            get { return ColorTranslator.ToHtml(route_color); }
            set { route_color = ColorTranslator.FromHtml(value); }
        }

        private Color route_link = Color.Blue;
        [Category("Section Specific Colors"), XmlIgnore]
        public Color RouteLinkColor
        {
            get { return route_link; }
            set { route_link = value; }
        }
        [Browsable(false)]
        public string RouteLink
        {
            get { return ColorTranslator.ToHtml(route_link); }
            set { route_link = ColorTranslator.FromHtml(value); }
        }
        private Color div_color = Color.Black;
        [Category("Section Specific Colors"), XmlIgnore]
        public Color DivColor
        {
            get { return div_color; }
            set { div_color = value; }
        }
        [Browsable(false)]
        public string DivPoint
        {
            get { return ColorTranslator.ToHtml(div_color); }
            set { div_color = ColorTranslator.FromHtml(value); }
        }

        public VisualSettings() { }

        public void ToXML()
        {
            MemoryStream m = new MemoryStream();
            XmlSerializer serializer = new XmlSerializer(typeof(VisualSettings));
            using (TextWriter writer = new StreamWriter(m))
            {
                serializer.Serialize(writer, this);
                writer.Close();
            }
            File.WriteAllBytes(Application.StartupPath + @"\settings.xml", m.ToArray());
        }

        public static VisualSettings FromXML()
        {
            byte[] xmlfile = File.ReadAllBytes(Application.StartupPath + @"\settings.xml");
            MemoryStream m = new MemoryStream(xmlfile);
            VisualSettings new_settings;
            XmlSerializer deserializer = new XmlSerializer(typeof(VisualSettings));
            TextReader reader = new StreamReader(m);
            new_settings = (VisualSettings)deserializer.Deserialize(reader);
            reader.Close();
            return new_settings;
        }
    }
}
