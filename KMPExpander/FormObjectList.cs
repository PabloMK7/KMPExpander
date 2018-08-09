using KMPExpander.Class.SimpleKMPs;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using KMPExpander.Class;

namespace KMPExpander
{
    public partial class FormObjectList : Form
    {
        private Objects.ObjectEntry entry;
        private UInt16 entryID;
        private bool changeComboBox = false;
        private ObjList objList;

        public FormObjectList(Objects.ObjectEntry entry,int Index)
        {
            InitializeComponent();
            objList = (Application.OpenForms[0] as Form1).objlist;
            Text = "Object #" + Index.ToString();
            this.entry = entry;
            entryID = entry.ObjectID;
            comboBox1.DataSource = new BindingSource(objList.displayName, null);
            comboBox1.DisplayMember = "Value";
            comboBox1.ValueMember = "Key";
            textBox1.Text = entryID.ToString("X4");
            try
            {
                button1.Enabled = true;
                comboBox1.SelectedValue = entryID;
                if (!objList.displayName.ContainsKey(entryID))
                {
                    comboBox1.Text = "? [Unknown]";
                }
            }
            catch
            {
                button1.Enabled = false;
                comboBox1.SelectedValue = 0;
                comboBox1.Text = "? [Unknown]";
            }
            changeComboBox = true;
        }
        
        private void FormObjectList_Load(object sender, EventArgs e)
        {

        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            changeComboBox = false;
            try
            {
                entryID = ushort.Parse((sender as TextBox).Text, NumberStyles.HexNumber);
                button1.Enabled = true;
                comboBox1.SelectedValue = entryID;
                if (!objList.displayName.ContainsKey(entryID))
                {
                    comboBox1.Text = "? [Unknown]";
                } 
            }
            catch 
            {
                button1.Enabled = false;
                comboBox1.SelectedValue = 0;
                comboBox1.Text = "? [Unknown]";
            }
            changeComboBox = true;
        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (!changeComboBox) return;

            entryID = (ushort)comboBox1.SelectedValue;
            textBox1.Text = entryID.ToString("X4");
        }

        private void FormObjectList_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter) this.Close();
        }

        private void button1_Click(object sender, EventArgs e) //OK
        {
            entry.ObjectID = entryID;
            this.Close();
        }

        private void button2_Click(object sender, EventArgs e) //Cancel
        {
            this.Close();
        }
    }
}
