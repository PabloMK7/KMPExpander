namespace KMPExpander
{
    partial class TestFormOBJ
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.menuStrip1 = new System.Windows.Forms.MenuStrip();
            this.loadOBKToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.simpleOpenGlControlTest = new Tao.Platform.Windows.SimpleOpenGlControl();
            this.openFileDialog1 = new System.Windows.Forms.OpenFileDialog();
            this.menuStrip1.SuspendLayout();
            this.SuspendLayout();
            // 
            // menuStrip1
            // 
            this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.loadOBKToolStripMenuItem});
            this.menuStrip1.Location = new System.Drawing.Point(0, 0);
            this.menuStrip1.Name = "menuStrip1";
            this.menuStrip1.Size = new System.Drawing.Size(899, 24);
            this.menuStrip1.TabIndex = 0;
            this.menuStrip1.Text = "menuStrip1";
            // 
            // loadOBKToolStripMenuItem
            // 
            this.loadOBKToolStripMenuItem.Name = "loadOBKToolStripMenuItem";
            this.loadOBKToolStripMenuItem.Size = new System.Drawing.Size(68, 20);
            this.loadOBKToolStripMenuItem.Text = "Load OBJ";
            this.loadOBKToolStripMenuItem.Click += new System.EventHandler(this.loadOBKToolStripMenuItem_Click);
            // 
            // simpleOpenGlControlTest
            // 
            this.simpleOpenGlControlTest.AccumBits = ((byte)(0));
            this.simpleOpenGlControlTest.AutoCheckErrors = false;
            this.simpleOpenGlControlTest.AutoFinish = false;
            this.simpleOpenGlControlTest.AutoMakeCurrent = true;
            this.simpleOpenGlControlTest.AutoSwapBuffers = true;
            this.simpleOpenGlControlTest.BackColor = System.Drawing.Color.Black;
            this.simpleOpenGlControlTest.ColorBits = ((byte)(32));
            this.simpleOpenGlControlTest.DepthBits = ((byte)(16));
            this.simpleOpenGlControlTest.Dock = System.Windows.Forms.DockStyle.Fill;
            this.simpleOpenGlControlTest.Location = new System.Drawing.Point(0, 24);
            this.simpleOpenGlControlTest.Name = "simpleOpenGlControlTest";
            this.simpleOpenGlControlTest.Size = new System.Drawing.Size(899, 409);
            this.simpleOpenGlControlTest.StencilBits = ((byte)(0));
            this.simpleOpenGlControlTest.TabIndex = 1;
            this.simpleOpenGlControlTest.Load += new System.EventHandler(this.simpleOpenGlControlTest_Load);
            // 
            // openFileDialog1
            // 
            this.openFileDialog1.FileName = "openFileDialog1";
            // 
            // TestFormOBJ
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(899, 433);
            this.Controls.Add(this.simpleOpenGlControlTest);
            this.Controls.Add(this.menuStrip1);
            this.MainMenuStrip = this.menuStrip1;
            this.Name = "TestFormOBJ";
            this.Text = "TestFormOBJ";
            this.SizeChanged += new System.EventHandler(this.TestFormOBJ_SizeChanged);
            this.menuStrip1.ResumeLayout(false);
            this.menuStrip1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.MenuStrip menuStrip1;
        private System.Windows.Forms.ToolStripMenuItem loadOBKToolStripMenuItem;
        private Tao.Platform.Windows.SimpleOpenGlControl simpleOpenGlControlTest;
        private System.Windows.Forms.OpenFileDialog openFileDialog1;
    }
}