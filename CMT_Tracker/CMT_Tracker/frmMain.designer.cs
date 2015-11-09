namespace CMT_Tracker
{
    partial class frmTrackMain
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
            this.overlay = new System.Windows.Forms.PictureBox();
            this.btnStart = new System.Windows.Forms.Button();
            this.strip = new System.Windows.Forms.StatusStrip();
            this.status = new System.Windows.Forms.ToolStripStatusLabel();
            ((System.ComponentModel.ISupportInitialize)(this.overlay)).BeginInit();
            this.strip.SuspendLayout();
            this.SuspendLayout();
            // 
            // overlay
            // 
            this.overlay.Location = new System.Drawing.Point(13, 13);
            this.overlay.Name = "overlay";
            this.overlay.Size = new System.Drawing.Size(640, 480);
            this.overlay.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;
            this.overlay.TabIndex = 0;
            this.overlay.TabStop = false;
            // 
            // btnStart
            // 
            this.btnStart.Location = new System.Drawing.Point(660, 13);
            this.btnStart.Name = "btnStart";
            this.btnStart.Size = new System.Drawing.Size(130, 49);
            this.btnStart.TabIndex = 1;
            this.btnStart.Text = "Start Tracking";
            this.btnStart.UseVisualStyleBackColor = true;
            this.btnStart.Click += new System.EventHandler(this.btnStart_Click);
            // 
            // strip
            // 
            this.strip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.status});
            this.strip.Location = new System.Drawing.Point(0, 507);
            this.strip.Name = "strip";
            this.strip.Size = new System.Drawing.Size(802, 22);
            this.strip.TabIndex = 2;
            this.strip.Text = "statusStrip1";
            // 
            // status
            // 
            this.status.Name = "status";
            this.status.Size = new System.Drawing.Size(0, 17);
            // 
            // frmTrackMain
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(802, 529);
            this.Controls.Add(this.strip);
            this.Controls.Add(this.btnStart);
            this.Controls.Add(this.overlay);
            this.Name = "frmTrackMain";
            this.Text = "CMT Tracking Host Test";
            ((System.ComponentModel.ISupportInitialize)(this.overlay)).EndInit();
            this.strip.ResumeLayout(false);
            this.strip.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.PictureBox overlay;
        private System.Windows.Forms.Button btnStart;
        private System.Windows.Forms.StatusStrip strip;
        private System.Windows.Forms.ToolStripStatusLabel status;
    }
}

