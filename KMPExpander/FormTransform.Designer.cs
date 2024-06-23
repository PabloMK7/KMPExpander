namespace KMPExpander
{
    partial class FormTransform
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
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.label5 = new System.Windows.Forms.Label();
            this.button_ok = new System.Windows.Forms.Button();
            this.button_cancel = new System.Windows.Forms.Button();
            this.translation_X = new System.Windows.Forms.NumericUpDown();
            this.translation_Y = new System.Windows.Forms.NumericUpDown();
            this.translation_Z = new System.Windows.Forms.NumericUpDown();
            this.scale_X = new System.Windows.Forms.NumericUpDown();
            this.scale_Y = new System.Windows.Forms.NumericUpDown();
            this.scale_Z = new System.Windows.Forms.NumericUpDown();
            ((System.ComponentModel.ISupportInitialize)(this.translation_X)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.translation_Y)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.translation_Z)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.scale_X)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.scale_Y)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.scale_Z)).BeginInit();
            this.SuspendLayout();
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(14, 35);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(59, 13);
            this.label1.TabIndex = 0;
            this.label1.Text = "Translation";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(39, 61);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(34, 13);
            this.label2.TabIndex = 1;
            this.label2.Text = "Scale";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(122, 16);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(14, 13);
            this.label3.TabIndex = 2;
            this.label3.Text = "X";
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(228, 12);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(14, 13);
            this.label4.TabIndex = 9;
            this.label4.Text = "Y";
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(334, 12);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(14, 13);
            this.label5.TabIndex = 10;
            this.label5.Text = "Z";
            // 
            // button_ok
            // 
            this.button_ok.Location = new System.Drawing.Point(319, 89);
            this.button_ok.Name = "button_ok";
            this.button_ok.Size = new System.Drawing.Size(75, 23);
            this.button_ok.TabIndex = 11;
            this.button_ok.Text = "OK";
            this.button_ok.UseVisualStyleBackColor = true;
            this.button_ok.Click += new System.EventHandler(this.button_ok_Click);
            // 
            // button_cancel
            // 
            this.button_cancel.Location = new System.Drawing.Point(238, 89);
            this.button_cancel.Name = "button_cancel";
            this.button_cancel.Size = new System.Drawing.Size(75, 23);
            this.button_cancel.TabIndex = 12;
            this.button_cancel.Text = "Cancel";
            this.button_cancel.UseVisualStyleBackColor = true;
            this.button_cancel.Click += new System.EventHandler(this.button_cancel_Click);
            // 
            // translation_X
            // 
            this.translation_X.DecimalPlaces = 4;
            this.translation_X.Location = new System.Drawing.Point(79, 32);
            this.translation_X.Maximum = new decimal(new int[] {
            1000000,
            0,
            0,
            0});
            this.translation_X.Minimum = new decimal(new int[] {
            1000000,
            0,
            0,
            -2147483648});
            this.translation_X.Name = "translation_X";
            this.translation_X.Size = new System.Drawing.Size(101, 20);
            this.translation_X.TabIndex = 13;
            // 
            // translation_Y
            // 
            this.translation_Y.DecimalPlaces = 4;
            this.translation_Y.Location = new System.Drawing.Point(186, 32);
            this.translation_Y.Maximum = new decimal(new int[] {
            1000000,
            0,
            0,
            0});
            this.translation_Y.Minimum = new decimal(new int[] {
            1000000,
            0,
            0,
            -2147483648});
            this.translation_Y.Name = "translation_Y";
            this.translation_Y.Size = new System.Drawing.Size(101, 20);
            this.translation_Y.TabIndex = 14;
            // 
            // translation_Z
            // 
            this.translation_Z.DecimalPlaces = 4;
            this.translation_Z.Location = new System.Drawing.Point(293, 32);
            this.translation_Z.Maximum = new decimal(new int[] {
            1000000,
            0,
            0,
            0});
            this.translation_Z.Minimum = new decimal(new int[] {
            1000000,
            0,
            0,
            -2147483648});
            this.translation_Z.Name = "translation_Z";
            this.translation_Z.Size = new System.Drawing.Size(101, 20);
            this.translation_Z.TabIndex = 15;
            // 
            // scale_X
            // 
            this.scale_X.DecimalPlaces = 4;
            this.scale_X.Location = new System.Drawing.Point(79, 58);
            this.scale_X.Maximum = new decimal(new int[] {
            1000000,
            0,
            0,
            0});
            this.scale_X.Minimum = new decimal(new int[] {
            1000000,
            0,
            0,
            -2147483648});
            this.scale_X.Name = "scale_X";
            this.scale_X.Size = new System.Drawing.Size(101, 20);
            this.scale_X.TabIndex = 16;
            this.scale_X.Value = new decimal(new int[] {
            1,
            0,
            0,
            0});
            // 
            // scale_Y
            // 
            this.scale_Y.DecimalPlaces = 4;
            this.scale_Y.Location = new System.Drawing.Point(186, 58);
            this.scale_Y.Maximum = new decimal(new int[] {
            1000000,
            0,
            0,
            0});
            this.scale_Y.Minimum = new decimal(new int[] {
            1000000,
            0,
            0,
            -2147483648});
            this.scale_Y.Name = "scale_Y";
            this.scale_Y.Size = new System.Drawing.Size(101, 20);
            this.scale_Y.TabIndex = 17;
            this.scale_Y.Value = new decimal(new int[] {
            1,
            0,
            0,
            0});
            // 
            // scale_Z
            // 
            this.scale_Z.DecimalPlaces = 4;
            this.scale_Z.Location = new System.Drawing.Point(293, 58);
            this.scale_Z.Maximum = new decimal(new int[] {
            1000000,
            0,
            0,
            0});
            this.scale_Z.Minimum = new decimal(new int[] {
            1000000,
            0,
            0,
            -2147483648});
            this.scale_Z.Name = "scale_Z";
            this.scale_Z.Size = new System.Drawing.Size(101, 20);
            this.scale_Z.TabIndex = 18;
            this.scale_Z.Value = new decimal(new int[] {
            1,
            0,
            0,
            0});
            // 
            // FormTransform
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(401, 122);
            this.Controls.Add(this.scale_Z);
            this.Controls.Add(this.scale_Y);
            this.Controls.Add(this.scale_X);
            this.Controls.Add(this.translation_Z);
            this.Controls.Add(this.translation_Y);
            this.Controls.Add(this.translation_X);
            this.Controls.Add(this.button_cancel);
            this.Controls.Add(this.button_ok);
            this.Controls.Add(this.label5);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label1);
            this.Name = "FormTransform";
            this.Text = "Transform KMP";
            ((System.ComponentModel.ISupportInitialize)(this.translation_X)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.translation_Y)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.translation_Z)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.scale_X)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.scale_Y)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.scale_Z)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.Button button_ok;
        private System.Windows.Forms.Button button_cancel;
        private System.Windows.Forms.NumericUpDown translation_X;
        private System.Windows.Forms.NumericUpDown translation_Y;
        private System.Windows.Forms.NumericUpDown translation_Z;
        private System.Windows.Forms.NumericUpDown scale_X;
        private System.Windows.Forms.NumericUpDown scale_Y;
        private System.Windows.Forms.NumericUpDown scale_Z;
    }
}