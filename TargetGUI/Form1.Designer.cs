namespace TargetGUI
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
            components = new System.ComponentModel.Container();
            BtnWalk = new Button();
            txtWalk = new TextBox();
            timer1 = new System.Windows.Forms.Timer(components);
            LblProcessId = new Label();
            SuspendLayout();
            // 
            // BtnWalk
            // 
            BtnWalk.Location = new Point(45, 121);
            BtnWalk.Name = "BtnWalk";
            BtnWalk.Size = new Size(538, 50);
            BtnWalk.TabIndex = 0;
            BtnWalk.Text = "Iniciar";
            BtnWalk.UseVisualStyleBackColor = true;
            BtnWalk.Click += BntWalk_Click;
            // 
            // txtWalk
            // 
            txtWalk.Location = new Point(45, 82);
            txtWalk.Name = "txtWalk";
            txtWalk.Size = new Size(538, 23);
            txtWalk.TabIndex = 2;
            txtWalk.Text = "-0.5,0.5;0.5,0.5;0.5,-0.5";
            // 
            // timer1
            // 
            timer1.Enabled = true;
            timer1.Interval = 500;
            timer1.Tick += timer1_Tick;
            // 
            // LblProcessId
            // 
            LblProcessId.AutoSize = true;
            LblProcessId.Location = new Point(40, 16);
            LblProcessId.Name = "LblProcessId";
            LblProcessId.Size = new Size(38, 15);
            LblProcessId.TabIndex = 3;
            LblProcessId.Text = "label1";
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(603, 450);
            Controls.Add(LblProcessId);
            Controls.Add(txtWalk);
            Controls.Add(BtnWalk);
            Name = "Form1";
            Text = "Form1";
            Load += Form1_Load;
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Button BtnWalk;
        private TextBox txtWalk;
        private System.Windows.Forms.Timer timer1;
        private Label LblProcessId;
    }
}
