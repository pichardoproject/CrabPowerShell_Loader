namespace WinFormsApp2
{
    partial class MainForm
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
                components.Dispose();
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        private void InitializeComponent()
        {
            this.btnSetup = new System.Windows.Forms.Button();
            this.btnRestrict = new System.Windows.Forms.Button();
            this.btnUndo = new System.Windows.Forms.Button();
            this.btnViewLog = new System.Windows.Forms.Button();
            this.btnClearLog = new System.Windows.Forms.Button();
            this.txtLog = new TransparentRichTextBox();
            this.panelButtons = new System.Windows.Forms.FlowLayoutPanel();
            this.panelButtons.SuspendLayout();
            this.SuspendLayout();

            // panelButtons (FlowLayoutPanel for wrapping)
            this.panelButtons.Dock = System.Windows.Forms.DockStyle.Top;
            this.panelButtons.AutoSize = true;
            this.panelButtons.WrapContents = true; // 👈 enables wrapping
            this.panelButtons.AutoScroll = true;
            this.panelButtons.Padding = new System.Windows.Forms.Padding(10, 10, 10, 5);
            this.panelButtons.BackColor = System.Drawing.Color.FromArgb(0, 76, 153);
            this.panelButtons.FlowDirection = System.Windows.Forms.FlowDirection.LeftToRight;

            // Shared button style
            System.Drawing.Font btnFont = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            int btnHeight = 32;



            // btnSetup
            this.btnSetup.Size = new System.Drawing.Size(180, btnHeight);
            this.btnSetup.Text = "Setup Help Desk Admins";
            this.btnSetup.Name = "btnSetup";
            this.btnSetup.Font = btnFont;
            this.btnSetup.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnSetup.ForeColor = System.Drawing.Color.Black;
            this.btnSetup.BackColor = System.Drawing.Color.White;
            this.btnSetup.FlatAppearance.BorderSize = 0;
            this.btnSetup.Margin = new System.Windows.Forms.Padding(8, 5, 8, 5);
            this.btnSetup.Click += new System.EventHandler(this.btnSetup_Click);

            // btnRestrict
            this.btnRestrict.Size = new System.Drawing.Size(180, btnHeight);
            this.btnRestrict.Text = "Apply User Restrictions";
            this.btnRestrict.Name = "btnRestrict";
            this.btnRestrict.Font = btnFont;
            this.btnRestrict.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnRestrict.ForeColor = System.Drawing.Color.Black;
            this.btnRestrict.BackColor = System.Drawing.Color.White;
            this.btnRestrict.FlatAppearance.BorderSize = 0;
            this.btnRestrict.Margin = new System.Windows.Forms.Padding(8, 5, 8, 5);
            this.btnRestrict.Click += new System.EventHandler(this.btnRestrict_Click);

            // btnUndo
            this.btnUndo.Size = new System.Drawing.Size(150, btnHeight);
            this.btnUndo.Text = "Undo Setup";
            this.btnUndo.Name = "btnUndo";
            this.btnUndo.Font = btnFont;
            this.btnUndo.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnUndo.ForeColor = System.Drawing.Color.Black;
            this.btnUndo.BackColor = System.Drawing.Color.White;
            this.btnUndo.FlatAppearance.BorderSize = 0;
            this.btnUndo.Margin = new System.Windows.Forms.Padding(8, 5, 8, 5);
            this.btnUndo.Click += new System.EventHandler(this.btnUndo_Click);

            // btnViewLog
            this.btnViewLog.Size = new System.Drawing.Size(150, btnHeight);
            this.btnViewLog.Text = "View Log Folder";
            this.btnViewLog.Name = "btnViewLog";
            this.btnViewLog.Font = btnFont;
            this.btnViewLog.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnViewLog.ForeColor = System.Drawing.Color.Black;
            this.btnViewLog.BackColor = System.Drawing.Color.White;
            this.btnViewLog.FlatAppearance.BorderSize = 0;
            this.btnViewLog.Margin = new System.Windows.Forms.Padding(8, 5, 8, 5);
            this.btnViewLog.Click += new System.EventHandler(this.btnViewLog_Click);

            // btnClearLog
            this.btnClearLog.Size = new System.Drawing.Size(120, btnHeight);
            this.btnClearLog.Text = "Clear Log";
            this.btnClearLog.Name = "btnClearLog";
            this.btnClearLog.Font = btnFont;
            this.btnClearLog.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnClearLog.ForeColor = System.Drawing.Color.Black;
            this.btnClearLog.BackColor = System.Drawing.Color.White;
            this.btnClearLog.FlatAppearance.BorderSize = 0;
            this.btnClearLog.Margin = new System.Windows.Forms.Padding(8, 5, 8, 5);
            this.btnClearLog.Click += new System.EventHandler(this.btnClearLog_Click);

            // Add buttons
            this.panelButtons.Controls.Add(this.btnSetup);
            this.panelButtons.Controls.Add(this.btnRestrict);
            this.panelButtons.Controls.Add(this.btnUndo);
            this.panelButtons.Controls.Add(this.btnViewLog);
            this.panelButtons.Controls.Add(this.btnClearLog);

            // txtLog
            this.txtLog.Dock = System.Windows.Forms.DockStyle.Fill;
            this.txtLog.ReadOnly = true;
            this.txtLog.BackColor = System.Drawing.Color.White;
            this.txtLog.Font = new System.Drawing.Font("Consolas", 10F);
            this.txtLog.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.txtLog.Text = "";

            // MainForm
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            this.MinimumSize = new System.Drawing.Size(700, 400);
            this.ClientSize = new System.Drawing.Size(900, 550);
            this.Controls.Add(this.txtLog);
            this.Controls.Add(this.panelButtons);
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Help Desk Shell";
            this.BackColor = System.Drawing.Color.White;
            this.panelButtons.ResumeLayout(false);
            this.ResumeLayout(false);
        }

        #endregion

        private System.Windows.Forms.Button btnSetup;
        private System.Windows.Forms.Button btnRestrict;
        private System.Windows.Forms.Button btnUndo;
        private System.Windows.Forms.Button btnViewLog;
        private System.Windows.Forms.Button btnClearLog;
        private TransparentRichTextBox txtLog;
        private System.Windows.Forms.FlowLayoutPanel panelButtons;
    }
}
