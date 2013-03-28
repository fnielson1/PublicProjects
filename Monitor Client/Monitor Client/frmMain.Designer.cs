namespace Monitor_Client
{
    partial class frmMain
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
            this.bgdWorker = new System.ComponentModel.BackgroundWorker();
            this.btnApply = new System.Windows.Forms.Button();
            this.btnCancel = new System.Windows.Forms.Button();
            this.txtLog = new System.Windows.Forms.TextBox();
            this.mnuMain = new System.Windows.Forms.MenuStrip();
            this.mnuMainFile = new System.Windows.Forms.ToolStripMenuItem();
            this.mnuMainFileHide = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
            this.mnuMainFileExit = new System.Windows.Forms.ToolStripMenuItem();
            this.lblShow = new System.Windows.Forms.Label();
            this.mnuMain.SuspendLayout();
            this.SuspendLayout();
            // 
            // txtIpAddress
            // 
            this.txtIpAddress.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.txtIpAddress.Location = new System.Drawing.Point(176, 28);
            this.txtIpAddress.Margin = new System.Windows.Forms.Padding(4);
            this.txtIpAddress.Name = "txtIpAddress";
            this.txtIpAddress.Size = new System.Drawing.Size(256, 23);
            this.txtIpAddress.TabIndex = 3;
            // 
            // bgdWorker
            // 
            this.bgdWorker.WorkerSupportsCancellation = true;
            this.bgdWorker.DoWork += new System.ComponentModel.DoWorkEventHandler(this.bgdWorker_DoWork);
            // 
            // btnApply
            // 
            this.btnApply.Location = new System.Drawing.Point(13, 27);
            this.btnApply.Name = "btnApply";
            this.btnApply.Size = new System.Drawing.Size(75, 24);
            this.btnApply.TabIndex = 2;
            this.btnApply.Text = "Apply";
            this.btnApply.UseVisualStyleBackColor = true;
            this.btnApply.Click += new System.EventHandler(this.btnApply_Click);
            // 
            // btnCancel
            // 
            this.btnCancel.Location = new System.Drawing.Point(94, 27);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(75, 23);
            this.btnCancel.TabIndex = 3;
            this.btnCancel.Text = "Cancel";
            this.btnCancel.UseVisualStyleBackColor = true;
            this.btnCancel.Click += new System.EventHandler(this.btnCancel_Click);
            // 
            // txtLog
            // 
            this.txtLog.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.txtLog.Location = new System.Drawing.Point(13, 58);
            this.txtLog.Multiline = true;
            this.txtLog.Name = "txtLog";
            this.txtLog.ReadOnly = true;
            this.txtLog.Size = new System.Drawing.Size(419, 154);
            this.txtLog.TabIndex = 7;
            this.txtLog.TabStop = false;
            // 
            // mnuMain
            // 
            this.mnuMain.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.mnuMainFile});
            this.mnuMain.Location = new System.Drawing.Point(0, 0);
            this.mnuMain.Name = "mnuMain";
            this.mnuMain.Size = new System.Drawing.Size(446, 24);
            this.mnuMain.TabIndex = 8;
            this.mnuMain.Text = "menuStrip1";
            // 
            // mnuMainFile
            // 
            this.mnuMainFile.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.mnuMainFileHide,
            this.toolStripSeparator1,
            this.mnuMainFileExit});
            this.mnuMainFile.Name = "mnuMainFile";
            this.mnuMainFile.Size = new System.Drawing.Size(37, 20);
            this.mnuMainFile.Text = "File";
            // 
            // mnuMainFileHide
            // 
            this.mnuMainFileHide.Name = "mnuMainFileHide";
            this.mnuMainFileHide.Size = new System.Drawing.Size(99, 22);
            this.mnuMainFileHide.Text = "Hide";
            this.mnuMainFileHide.Click += new System.EventHandler(this.mnuFileHide_Click);
            // 
            // toolStripSeparator1
            // 
            this.toolStripSeparator1.Name = "toolStripSeparator1";
            this.toolStripSeparator1.Size = new System.Drawing.Size(96, 6);
            // 
            // mnuMainFileExit
            // 
            this.mnuMainFileExit.CheckOnClick = true;
            this.mnuMainFileExit.Name = "mnuMainFileExit";
            this.mnuMainFileExit.Size = new System.Drawing.Size(99, 22);
            this.mnuMainFileExit.Tag = "btnExit";
            this.mnuMainFileExit.Text = "Exit";
            this.mnuMainFileExit.Click += new System.EventHandler(this.mnuMainFileExit_Click);
            // 
            // lblShow
            // 
            this.lblShow.AutoSize = true;
            this.lblShow.Location = new System.Drawing.Point(12, 215);
            this.lblShow.Name = "lblShow";
            this.lblShow.Size = new System.Drawing.Size(44, 17);
            this.lblShow.TabIndex = 9;
            this.lblShow.Text = "Press";
            // 
            // frmMain
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(446, 240);
            this.Controls.Add(this.lblShow);
            this.Controls.Add(this.txtLog);
            this.Controls.Add(this.btnCancel);
            this.Controls.Add(this.btnApply);
            this.Controls.Add(this.txtIpAddress);
            this.Controls.Add(this.mnuMain);
            this.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.KeyPreview = true;
            this.MainMenuStrip = this.mnuMain;
            this.Margin = new System.Windows.Forms.Padding(4);
            this.MaximumSize = new System.Drawing.Size(462, 278);
            this.MinimumSize = new System.Drawing.Size(462, 278);
            this.Name = "frmMain";
            this.Text = "Monitor Client";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.frmMain_FormClosing);
            this.Shown += new System.EventHandler(this.frmMain_Shown);
            this.SizeChanged += new System.EventHandler(this.frmMain_SizeChanged);
            this.mnuMain.ResumeLayout(false);
            this.mnuMain.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TextBox txtIpAddress;
        private System.ComponentModel.BackgroundWorker bgdWorker;
        private System.Windows.Forms.Button btnApply;
        private System.Windows.Forms.Button btnCancel;
        private System.Windows.Forms.TextBox txtLog;
        private System.Windows.Forms.MenuStrip mnuMain;
        private System.Windows.Forms.ToolStripMenuItem mnuMainFile;
        private System.Windows.Forms.ToolStripMenuItem mnuMainFileHide;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator1;
        private System.Windows.Forms.ToolStripMenuItem mnuMainFileExit;
        private System.Windows.Forms.Label lblShow;
    }
}

