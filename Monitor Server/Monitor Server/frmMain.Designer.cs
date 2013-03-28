namespace Monitor_Server
{
    partial class frmServer
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
            this.txtIpAddress = new System.Windows.Forms.TextBox();
            this.btnListen = new System.Windows.Forms.Button();
            this.lblIpAddress = new System.Windows.Forms.Label();
            this.txtLog = new System.Windows.Forms.TextBox();
            this.pctImage = new System.Windows.Forms.PictureBox();
            this.lstUsers = new System.Windows.Forms.ListView();
            ((System.ComponentModel.ISupportInitialize)(this.pctImage)).BeginInit();
            this.SuspendLayout();
            // 
            // txtIpAddress
            // 
            this.txtIpAddress.Location = new System.Drawing.Point(91, 12);
            this.txtIpAddress.Name = "txtIpAddress";
            this.txtIpAddress.Size = new System.Drawing.Size(273, 23);
            this.txtIpAddress.TabIndex = 5;
            // 
            // btnListen
            // 
            this.btnListen.Location = new System.Drawing.Point(370, 9);
            this.btnListen.Name = "btnListen";
            this.btnListen.Size = new System.Drawing.Size(118, 28);
            this.btnListen.TabIndex = 4;
            this.btnListen.Text = "Start Listening";
            this.btnListen.UseVisualStyleBackColor = true;
            this.btnListen.Click += new System.EventHandler(this.btnListen_Click);
            // 
            // lblIpAddress
            // 
            this.lblIpAddress.AutoSize = true;
            this.lblIpAddress.Location = new System.Drawing.Point(8, 15);
            this.lblIpAddress.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lblIpAddress.Name = "lblIpAddress";
            this.lblIpAddress.Size = new System.Drawing.Size(76, 17);
            this.lblIpAddress.TabIndex = 3;
            this.lblIpAddress.Text = "IP Address";
            // 
            // txtLog
            // 
            this.txtLog.Location = new System.Drawing.Point(191, 52);
            this.txtLog.Multiline = true;
            this.txtLog.Name = "txtLog";
            this.txtLog.Size = new System.Drawing.Size(391, 69);
            this.txtLog.TabIndex = 7;
            // 
            // pctImage
            // 
            this.pctImage.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.pctImage.Location = new System.Drawing.Point(191, 127);
            this.pctImage.Name = "pctImage";
            this.pctImage.Size = new System.Drawing.Size(391, 405);
            this.pctImage.TabIndex = 6;
            this.pctImage.TabStop = false;
            // 
            // lstUsers
            // 
            this.lstUsers.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)));
            this.lstUsers.Location = new System.Drawing.Point(11, 52);
            this.lstUsers.Name = "lstUsers";
            this.lstUsers.Size = new System.Drawing.Size(174, 480);
            this.lstUsers.TabIndex = 8;
            this.lstUsers.UseCompatibleStateImageBehavior = false;
            // 
            // frmServer
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(594, 544);
            this.Controls.Add(this.lstUsers);
            this.Controls.Add(this.pctImage);
            this.Controls.Add(this.txtLog);
            this.Controls.Add(this.txtIpAddress);
            this.Controls.Add(this.btnListen);
            this.Controls.Add(this.lblIpAddress);
            this.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.Margin = new System.Windows.Forms.Padding(4);
            this.MinimumSize = new System.Drawing.Size(610, 525);
            this.Name = "frmServer";
            this.Text = "Monitor Server";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.frmServer_FormClosing);
            this.Shown += new System.EventHandler(this.frmServer_Shown);
            ((System.ComponentModel.ISupportInitialize)(this.pctImage)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TextBox txtIpAddress;
        private System.Windows.Forms.Button btnListen;
        private System.Windows.Forms.Label lblIpAddress;
        private System.Windows.Forms.TextBox txtLog;
        private System.Windows.Forms.PictureBox pctImage;
        private System.Windows.Forms.ListView lstUsers;


    }
}

