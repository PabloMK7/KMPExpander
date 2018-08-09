namespace KMPExpander
{
    partial class ErrorCheck
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
            this.MsgList = new System.Windows.Forms.ListBox();
            this.errorButton = new System.Windows.Forms.Button();
            this.warningButton = new System.Windows.Forms.Button();
            this.notesButton = new System.Windows.Forms.Button();
            this.refreshButton = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // MsgList
            // 
            this.MsgList.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.MsgList.FormattingEnabled = true;
            this.MsgList.HorizontalScrollbar = true;
            this.MsgList.Location = new System.Drawing.Point(12, 38);
            this.MsgList.Name = "MsgList";
            this.MsgList.Size = new System.Drawing.Size(493, 264);
            this.MsgList.TabIndex = 0;
            // 
            // errorButton
            // 
            this.errorButton.Image = global::KMPExpander.Properties.Resources.error;
            this.errorButton.Location = new System.Drawing.Point(12, 12);
            this.errorButton.Name = "errorButton";
            this.errorButton.Size = new System.Drawing.Size(150, 23);
            this.errorButton.TabIndex = 1;
            this.errorButton.Text = "Errors: 0";
            this.errorButton.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.errorButton.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText;
            this.errorButton.UseVisualStyleBackColor = true;
            this.errorButton.Click += new System.EventHandler(this.errorButton_Click);
            // 
            // warningButton
            // 
            this.warningButton.Image = global::KMPExpander.Properties.Resources.warning;
            this.warningButton.Location = new System.Drawing.Point(168, 12);
            this.warningButton.Name = "warningButton";
            this.warningButton.Size = new System.Drawing.Size(150, 23);
            this.warningButton.TabIndex = 2;
            this.warningButton.Text = "Warnings: 0";
            this.warningButton.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.warningButton.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText;
            this.warningButton.UseVisualStyleBackColor = true;
            this.warningButton.Click += new System.EventHandler(this.warningButton_Click);
            // 
            // notesButton
            // 
            this.notesButton.Image = global::KMPExpander.Properties.Resources.info;
            this.notesButton.Location = new System.Drawing.Point(324, 12);
            this.notesButton.Name = "notesButton";
            this.notesButton.Size = new System.Drawing.Size(150, 23);
            this.notesButton.TabIndex = 3;
            this.notesButton.Text = "Notes: 0";
            this.notesButton.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.notesButton.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText;
            this.notesButton.UseVisualStyleBackColor = true;
            this.notesButton.Click += new System.EventHandler(this.notesButton_Click);
            // 
            // refreshButton
            // 
            this.refreshButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.refreshButton.Location = new System.Drawing.Point(430, 308);
            this.refreshButton.Name = "refreshButton";
            this.refreshButton.Size = new System.Drawing.Size(75, 23);
            this.refreshButton.TabIndex = 4;
            this.refreshButton.Text = "Refresh";
            this.refreshButton.UseVisualStyleBackColor = true;
            this.refreshButton.Click += new System.EventHandler(this.refreshButton_Click);
            // 
            // ErrorCheck
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(517, 336);
            this.Controls.Add(this.refreshButton);
            this.Controls.Add(this.notesButton);
            this.Controls.Add(this.warningButton);
            this.Controls.Add(this.errorButton);
            this.Controls.Add(this.MsgList);
            this.Name = "ErrorCheck";
            this.Text = "KMP Error Checking ";
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.ListBox MsgList;
        private System.Windows.Forms.Button errorButton;
        private System.Windows.Forms.Button warningButton;
        private System.Windows.Forms.Button notesButton;
        private System.Windows.Forms.Button refreshButton;
    }
}