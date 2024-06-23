using LibCTR.Collections;
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
    public partial class FormTransform : Form
    {
        public FormTransform()
        {
            InitializeComponent();

            KMPTranslation = new Vector3(0f, 0f, 0f);
            KMPScale = new Vector3(1f, 1f, 1f);
        }

        private void button_cancel_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void button_ok_Click(object sender, EventArgs e)
        {
            Confirmed = true;
            Close();
        }

        public bool Confirmed = false;

        public Vector3 KMPTranslation
        {
            get
            {
                return new Vector3((float)translation_X.Value, (float)translation_Y.Value, (float)translation_Z.Value);
            }
            set
            {
                translation_X.Value = (decimal)value.X;
                translation_Y.Value = (decimal)value.Y;
                translation_Z.Value = (decimal)value.Z;
            }
        }

        public Vector3 KMPScale
        {
            get
            {
                return new Vector3((float)scale_X.Value, (float)scale_Y.Value, (float)scale_Z.Value);
            }
            set
            {
                scale_X.Value = (decimal)value.X;
                scale_Y.Value = (decimal)value.Y;
                scale_Z.Value = (decimal)value.Z;
            }
        }
    }
}
