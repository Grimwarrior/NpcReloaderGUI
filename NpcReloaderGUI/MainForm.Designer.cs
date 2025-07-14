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
            btnReload = new Button();
            groupBox1 = new GroupBox();
            btnBrowseScript = new Button();
            txtScriptPath = new TextBox();
            label3 = new Label();
            chkAutoReload = new CheckBox();
            txtLog = new TextBox();
            cmbChrSelection = new ComboBox();
            labelCharacterName = new Label();
            groupBox2 = new GroupBox();
            btnAddChar = new Button();
            txtNewCharName = new TextBox();
            label4 = new Label();
            txtNewCharId = new TextBox();
            label2 = new Label();
            groupBox1.SuspendLayout();
            groupBox2.SuspendLayout();
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
            cmbGameSelection.Location = new Point(4, 24);
            cmbGameSelection.Name = "cmbGameSelection";
            cmbGameSelection.Size = new Size(153, 33);
            cmbGameSelection.TabIndex = 1;
            cmbGameSelection.SelectedIndexChanged += comboBox1_SelectedIndexChanged;
            // 
            // btnReload
            // 
            btnReload.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            btnReload.Location = new Point(9, 539);
            btnReload.Name = "btnReload";
            btnReload.Size = new Size(130, 43);
            btnReload.TabIndex = 4;
            btnReload.Text = "Reload NPC";
            btnReload.UseVisualStyleBackColor = true;
            btnReload.Click += btnReload_Click_1;
            // 
            // groupBox1
            // 
            groupBox1.Anchor = AnchorStyles.Left | AnchorStyles.Right;
            groupBox1.Controls.Add(btnBrowseScript);
            groupBox1.Controls.Add(txtScriptPath);
            groupBox1.Controls.Add(label3);
            groupBox1.Controls.Add(chkAutoReload);
            groupBox1.Location = new Point(9, 157);
            groupBox1.Name = "groupBox1";
            groupBox1.Size = new Size(369, 143);
            groupBox1.TabIndex = 5;
            groupBox1.TabStop = false;
            groupBox1.Text = "Auto Reload";
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
            // txtScriptPath
            // 
            txtScriptPath.Location = new Point(3, 64);
            txtScriptPath.Name = "txtScriptPath";
            txtScriptPath.ReadOnly = true;
            txtScriptPath.Size = new Size(150, 31);
            txtScriptPath.TabIndex = 2;
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
            // txtLog
            // 
            txtLog.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            txtLog.Location = new Point(465, 0);
            txtLog.Multiline = true;
            txtLog.Name = "txtLog";
            txtLog.ReadOnly = true;
            txtLog.ScrollBars = ScrollBars.Vertical;
            txtLog.Size = new Size(268, 567);
            txtLog.TabIndex = 6;
            txtLog.TextChanged += txtLog_TextChanged;
            // 
            // cmbChrSelection
            // 
            cmbChrSelection.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            cmbChrSelection.FormattingEnabled = true;
            cmbChrSelection.Location = new Point(7, 109);
            cmbChrSelection.Name = "cmbChrSelection";
            cmbChrSelection.Size = new Size(243, 33);
            cmbChrSelection.TabIndex = 7;
            // 
            // labelCharacterName
            // 
            labelCharacterName.AutoSize = true;
            labelCharacterName.Location = new Point(7, 81);
            labelCharacterName.Name = "labelCharacterName";
            labelCharacterName.Size = new Size(147, 25);
            labelCharacterName.TabIndex = 2;
            labelCharacterName.Text = "Character Name :";
            labelCharacterName.Click += labelCharacterName_Click;
            // 
            // groupBox2
            // 
            groupBox2.Controls.Add(btnAddChar);
            groupBox2.Controls.Add(txtNewCharName);
            groupBox2.Controls.Add(label4);
            groupBox2.Controls.Add(txtNewCharId);
            groupBox2.Controls.Add(label2);
            groupBox2.Location = new Point(9, 328);
            groupBox2.Name = "groupBox2";
            groupBox2.Size = new Size(300, 179);
            groupBox2.TabIndex = 8;
            groupBox2.TabStop = false;
            groupBox2.Text = "Add New Character";
            // 
            // btnAddChar
            // 
            btnAddChar.Location = new Point(6, 139);
            btnAddChar.Name = "btnAddChar";
            btnAddChar.Size = new Size(112, 34);
            btnAddChar.TabIndex = 4;
            btnAddChar.Text = "Add Entry";
            btnAddChar.UseVisualStyleBackColor = true;
            btnAddChar.Click += btnAddChar_Click;
            // 
            // txtNewCharName
            // 
            txtNewCharName.Location = new Point(76, 74);
            txtNewCharName.Name = "txtNewCharName";
            txtNewCharName.Size = new Size(150, 31);
            txtNewCharName.TabIndex = 3;
            // 
            // label4
            // 
            label4.AutoSize = true;
            label4.Location = new Point(11, 71);
            label4.Name = "label4";
            label4.Size = new Size(59, 25);
            label4.TabIndex = 2;
            label4.Text = "Name";
            // 
            // txtNewCharId
            // 
            txtNewCharId.Location = new Point(76, 37);
            txtNewCharId.Name = "txtNewCharId";
            txtNewCharId.Size = new Size(150, 31);
            txtNewCharId.TabIndex = 1;
            txtNewCharId.TextChanged += txtNewCharId_TextChanged;
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new Point(11, 34);
            label2.Name = "label2";
            label2.Size = new Size(30, 25);
            label2.TabIndex = 0;
            label2.Text = "ID";
            // 
            // MainForm
            // 
            AutoScaleDimensions = new SizeF(10F, 25F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(742, 595);
            Controls.Add(groupBox2);
            Controls.Add(cmbChrSelection);
            Controls.Add(labelCharacterName);
            Controls.Add(txtLog);
            Controls.Add(groupBox1);
            Controls.Add(btnReload);
            Controls.Add(cmbGameSelection);
            Controls.Add(label1);
            Name = "MainForm";
            Load += MainForm_Load;
            groupBox1.ResumeLayout(false);
            groupBox1.PerformLayout();
            groupBox2.ResumeLayout(false);
            groupBox2.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Label label1;
        private ComboBox cmbGameSelection;
        private Label labelCharacterName;
        private Button btnReload;
        private GroupBox groupBox1;
        private Button btnBrowseScript;
        private TextBox txtScriptPath;
        private Label label3;
        private CheckBox chkAutoReload;
        private TextBox txtLog;
        private ComboBox cmbChrSelection;
        private GroupBox groupBox2;
        private TextBox txtNewCharName;
        private Label label4;
        private TextBox txtNewCharId;
        private Label label2;
        private Button btnAddChar;
    }
}
