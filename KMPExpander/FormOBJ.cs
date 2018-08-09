using KMPExpander.Class;
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
    public partial class FormOBJ : Form
    {
        public Form1 form1;

        public FormOBJ(Form1 form1)
        {
            InitializeComponent();
            this.form1 = form1;
        }

        private void loadWavefrontToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (openFileDialogOBJ.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    form1.OBJModel = new OBJWrapper(openFileDialogOBJ.FileName);
                    comboBox1.DataSource = form1.OBJModel.Materials;
                    checkBox1.Enabled = true;
                    checkBox2.Enabled = true;
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.ToString());
                }
            }
            form1.Render();
        }
        private void closeWavefrontToolStripMenuItem_Click(object sender, EventArgs e)
        {
            form1.OBJModel = null;
            comboBox1.DataSource = null;
            propertyGrid1.SelectedObject = null;
            pictureBoxTex.Image = null;
            checkBox1.Enabled = false;
            checkBox2.Enabled = false;
            form1.Render();
        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (form1.OBJModel == null) return;
            OBJWrapper.MaterialInfo matInfo = ((OBJWrapper.MaterialInfo)comboBox1.Items[comboBox1.SelectedIndex]);
            propertyGrid1.SelectedObject = matInfo;
            pictureBoxTex.Image = matInfo.Texture;
            form1.Render();
        }

        private void propertyGrid1_PropertyValueChanged(object s, PropertyValueChangedEventArgs e)
        {
            form1.Render();
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            form1.OBJModel.Visible = (sender as CheckBox).Checked;
            form1.Render();
        }

        private void checkBox2_CheckedChanged(object sender, EventArgs e)
        {
            form1.OBJModel.Wireframe = (sender as CheckBox).Checked;
            form1.Render();
        }
    }
}
