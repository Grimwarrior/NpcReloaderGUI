namespace NpcReloaderGUI
{
    partial class MainForm
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
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
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            label1 = new Label();
            cmbGameSelection = new ComboBox();
            label2 = new Label();
            txtChrId = new TextBox();
            btnReload = new Button();
            groupBox1 = new GroupBox();
            chkAutoReload = new CheckBox();
            label3 = new Label();
            txtScriptPath = new TextBox();
            btnBrowseScript = new Button();
            txtLog = new TextBox();
            groupBox1.SuspendLayout();
            SuspendLayout();
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(4, 0);
            label1.Name = "label1";
            label1.Size = new Size(58, 25);
            label1.TabIndex = 0;
            label1.Text = "Game";
            label1.Click += label1_Click;
            // 
            // cmbGameSelection
            // 
            cmbGameSelection.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbGameSelection.FormattingEnabled = true;
            cmbGameSelection.Location = new Point(4, 28);
            cmbGameSelection.Name = "cmbGameSelection";
            cmbGameSelection.Size = new Size(153, 33);
            cmbGameSelection.TabIndex = 1;
            cmbGameSelection.SelectedIndexChanged += comboBox1_SelectedIndexChanged;
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new Point(4, 99);
            label2.Name = "label2";
            label2.Size = new Size(213, 25);
            label2.TabIndex = 2;
            label2.Text = "Character ID (e.g., c0000):";
            // 
            // txtChrId
            // 
            txtChrId.Location = new Point(4, 127);
            txtChrId.Name = "txtChrId";
            txtChrId.Size = new Size(329, 31);
            txtChrId.TabIndex = 3;
            // 
            // btnReload
            // 
            btnReload.Location = new Point(4, 459);
            btnReload.Name = "btnReload";
            btnReload.Size = new Size(149, 34);
            btnReload.TabIndex = 4;
            btnReload.Text = "Reload NPC";
            btnReload.UseVisualStyleBackColor = true;
            btnReload.Click += btnReload_Click_1;
            // 
            // groupBox1
            // 
            groupBox1.Controls.Add(btnBrowseScript);
            groupBox1.Controls.Add(txtScriptPath);
            groupBox1.Controls.Add(label3);
            groupBox1.Controls.Add(chkAutoReload);
            groupBox1.Location = new Point(4, 214);
            groupBox1.Name = "groupBox1";
            groupBox1.Size = new Size(364, 202);
            groupBox1.TabIndex = 5;
            groupBox1.TabStop = false;
            groupBox1.Text = "Auto Reload";
            // 
            // chkAutoReload
            // 
            chkAutoReload.AutoSize = true;
            chkAutoReload.Location = new Point(6, 104);
            chkAutoReload.Name = "chkAutoReload";
            chkAutoReload.Size = new Size(336, 29);
            chkAutoReload.TabIndex = 0;
            chkAutoReload.Text = "Enable Auto-Reload on Script Change";
            chkAutoReload.UseVisualStyleBackColor = true;
            chkAutoReload.CheckedChanged += chkAutoReload_CheckedChanged_1;
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Location = new Point(3, 27);
            label3.Name = "label3";
            label3.Size = new Size(88, 25);
            label3.TabIndex = 1;
            label3.Text = "Script File";
            // 
            // txtScriptPath
            // 
            txtScriptPath.Location = new Point(3, 64);
            txtScriptPath.Name = "txtScriptPath";
            txtScriptPath.ReadOnly = true;
            txtScriptPath.Size = new Size(150, 31);
            txtScriptPath.TabIndex = 2;
            // 
            // btnBrowseScript
            // 
            btnBrowseScript.Location = new Point(167, 64);
            btnBrowseScript.Name = "btnBrowseScript";
            btnBrowseScript.Size = new Size(112, 34);
            btnBrowseScript.TabIndex = 3;
            btnBrowseScript.Text = "Browse...";
            btnBrowseScript.UseVisualStyleBackColor = true;
            btnBrowseScript.Click += btnBrowseScript_Click_1;
            // 
            // txtLog
            // 
            txtLog.Dock = DockStyle.Right;
            txtLog.Location = new Point(379, 0);
            txtLog.Multiline = true;
            txtLog.Name = "txtLog";
            txtLog.ReadOnly = true;
            txtLog.ScrollBars = ScrollBars.Vertical;
            txtLog.Size = new Size(302, 567);
            txtLog.TabIndex = 6;
            txtLog.TextChanged += txtLog_TextChanged;
            // 
            // MainForm
            // 
            AutoScaleDimensions = new SizeF(10F, 25F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(681, 567);
            Controls.Add(txtLog);
            Controls.Add(groupBox1);
            Controls.Add(btnReload);
            Controls.Add(txtChrId);
            Controls.Add(label2);
            Controls.Add(cmbGameSelection);
            Controls.Add(label1);
            Name = "MainForm";
            Text = "ChrReloader";
            Load += MainForm_Load;
            groupBox1.ResumeLayout(false);
            groupBox1.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Label label1;
        private ComboBox cmbGameSelection;
        private Label label2;
        private TextBox txtChrId;
        private Button btnReload;
        private GroupBox groupBox1;
        private Button btnBrowseScript;
        private TextBox txtScriptPath;
        private Label label3;
        private CheckBox chkAutoReload;
        private TextBox txtLog;
    }
}
