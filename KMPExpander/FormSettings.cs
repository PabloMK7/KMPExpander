using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace KMPExpander
{
    public partial class FormSettings : Form
    {
        Form1 form1;

        public FormSettings(Form1 form1)
        {
            InitializeComponent();
            this.form1 = form1;
            propertyGrid1.SelectedObject = form1.Settings;
        }

        private void propertyGrid1_PropertyValueChanged(object s, PropertyValueChangedEventArgs e)
        {
            form1.Render();
            form1.Settings.ToXML();
        }
    }
}
