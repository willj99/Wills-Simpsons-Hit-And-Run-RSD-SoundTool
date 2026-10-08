namespace RSDSwissKnife
{
    partial class Form1
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Form1));
            button_opnpath = new Button();
            text_opnpath = new TextBox();
            list_file = new ListView();
            button2 = new Button();
            button3 = new Button();
            button_wipeallfiles = new Button();
            list_attributes = new ListBox();
            button_playpause = new Button();
            label_CurrenrTimeVsTotal = new Label();
            checkbox_loop = new CheckBox();
            SuspendLayout();
            // 
            // button_opnpath
            // 
            button_opnpath.Location = new Point(12, 12);
            button_opnpath.Name = "button_opnpath";
            button_opnpath.Size = new Size(93, 23);
            button_opnpath.TabIndex = 0;
            button_opnpath.Text = "Open Folder";
            button_opnpath.UseVisualStyleBackColor = true;
            // 
            // text_opnpath
            // 
            text_opnpath.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            text_opnpath.BackColor = SystemColors.InactiveCaption;
            text_opnpath.Enabled = false;
            text_opnpath.Location = new Point(111, 12);
            text_opnpath.Name = "text_opnpath";
            text_opnpath.Size = new Size(608, 23);
            text_opnpath.TabIndex = 1;
            // 
            // list_file
            // 
            list_file.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            list_file.Location = new Point(12, 41);
            list_file.Name = "list_file";
            list_file.Size = new Size(353, 261);
            list_file.TabIndex = 2;
            list_file.UseCompatibleStateImageBehavior = false;
            list_file.SelectedIndexChanged += list_file_SelectedIndexChanged_1;
            // 
            // button2
            // 
            button2.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            button2.Location = new Point(12, 308);
            button2.Name = "button2";
            button2.Size = new Size(118, 23);
            button2.TabIndex = 3;
            button2.Text = ".RSD -> .WAV";
            button2.UseVisualStyleBackColor = true;
            button2.Click += button2_Click;
            // 
            // button3
            // 
            button3.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            button3.Location = new Point(12, 333);
            button3.Name = "button3";
            button3.Size = new Size(118, 23);
            button3.TabIndex = 4;
            button3.Text = ".WAV -> .OGG";
            button3.UseVisualStyleBackColor = true;
            button3.Click += button3_Click;
            // 
            // button_wipeallfiles
            // 
            button_wipeallfiles.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            button_wipeallfiles.BackColor = Color.IndianRed;
            button_wipeallfiles.FlatAppearance.BorderColor = Color.Red;
            button_wipeallfiles.FlatAppearance.BorderSize = 5;
            button_wipeallfiles.FlatAppearance.MouseDownBackColor = Color.Red;
            button_wipeallfiles.FlatStyle = FlatStyle.Popup;
            button_wipeallfiles.ForeColor = SystemColors.ButtonFace;
            button_wipeallfiles.Location = new Point(136, 308);
            button_wipeallfiles.Name = "button_wipeallfiles";
            button_wipeallfiles.Size = new Size(229, 48);
            button_wipeallfiles.TabIndex = 5;
            button_wipeallfiles.Text = "Delete all .RSD and .WAV from this folder";
            button_wipeallfiles.UseVisualStyleBackColor = false;
            button_wipeallfiles.Click += button_wipeallfiles_Click;
            // 
            // list_attributes
            // 
            list_attributes.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            list_attributes.FormattingEnabled = true;
            list_attributes.Location = new Point(371, 41);
            list_attributes.Name = "list_attributes";
            list_attributes.Size = new Size(348, 184);
            list_attributes.TabIndex = 6;
            // 
            // button_playpause
            // 
            button_playpause.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            button_playpause.Location = new Point(371, 234);
            button_playpause.Name = "button_playpause";
            button_playpause.Size = new Size(83, 23);
            button_playpause.TabIndex = 8;
            button_playpause.Text = "Play";
            button_playpause.UseVisualStyleBackColor = true;
            button_playpause.Click += button_playpause_Click;
            // 
            // label_CurrenrTimeVsTotal
            // 
            label_CurrenrTimeVsTotal.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            label_CurrenrTimeVsTotal.AutoSize = true;
            label_CurrenrTimeVsTotal.Location = new Point(649, 237);
            label_CurrenrTimeVsTotal.Name = "label_CurrenrTimeVsTotal";
            label_CurrenrTimeVsTotal.Size = new Size(72, 15);
            label_CurrenrTimeVsTotal.TabIndex = 9;
            label_CurrenrTimeVsTotal.Text = "00:00 / 00:00";
            label_CurrenrTimeVsTotal.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // checkbox_loop
            // 
            checkbox_loop.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            checkbox_loop.AutoSize = true;
            checkbox_loop.Location = new Point(460, 237);
            checkbox_loop.Name = "checkbox_loop";
            checkbox_loop.Size = new Size(53, 19);
            checkbox_loop.TabIndex = 10;
            checkbox_loop.Text = "Loop";
            checkbox_loop.UseVisualStyleBackColor = true;
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(731, 368);
            Controls.Add(checkbox_loop);
            Controls.Add(label_CurrenrTimeVsTotal);
            Controls.Add(button_playpause);
            Controls.Add(list_attributes);
            Controls.Add(button_wipeallfiles);
            Controls.Add(button3);
            Controls.Add(button2);
            Controls.Add(list_file);
            Controls.Add(text_opnpath);
            Controls.Add(button_opnpath);
            Icon = (Icon)resources.GetObject("$this.Icon");
            Name = "Form1";
            Text = "Will's RSD Tool - GUI";
            Load += Form1_Load;
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Button button_opnpath;
        private TextBox text_opnpath;
        private ListView list_file;
        private Button button2;
        private Button button3;
        private Button button_wipeallfiles;
        private ListBox list_attributes;
        private Button button_playpause;
        private Label label_CurrenrTimeVsTotal;
        private CheckBox checkbox_loop;
    }
}
