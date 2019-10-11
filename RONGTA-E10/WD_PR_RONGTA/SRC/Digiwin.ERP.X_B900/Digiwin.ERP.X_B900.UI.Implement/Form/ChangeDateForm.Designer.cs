namespace Digiwin.ERP.X_B900.UI.Implement {
    partial class ChangeDateForm {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing) {
            if (disposing && (components != null)) {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent() {
            this.ddtpDate = new Digiwin.Common.UI.DigiwinDateTimePicker();
            this.digiwinLabel1 = new Digiwin.Common.UI.DigiwinLabel();
            this.btnOk = new Digiwin.Common.UI.DigiwinButton();
            this.btnCancel = new Digiwin.Common.UI.DigiwinButton();
            this.SuspendLayout();
            // 
            // ddtpDate
            // 
            this.ddtpDate.Culture = new System.Globalization.CultureInfo("zh-CN");
            this.ddtpDate.FormatString = null;
            this.ddtpDate.Location = new System.Drawing.Point(117, 37);
            this.ddtpDate.MaxDate = new System.DateTime(9998, 12, 31, 0, 0, 0, 0);
            this.ddtpDate.MinDate = new System.DateTime(1753, 1, 1, 0, 0, 0, 0);
            this.ddtpDate.Name = "ddtpDate";
            this.ddtpDate.ShortcutFormula = null;
            this.ddtpDate.Size = new System.Drawing.Size(178, 22);
            this.ddtpDate.TabIndex = 0;
            this.ddtpDate.WeekDaysFormat = Digiwin.Common.UI.DigiwinCalendarDayFormat.Short;
            this.ddtpDate.WeekendColor = System.Drawing.Color.White;
            // 
            // digiwinLabel1
            // 
            this.digiwinLabel1.AutoSize = true;
            this.digiwinLabel1.Location = new System.Drawing.Point(28, 41);
            this.digiwinLabel1.Name = "digiwinLabel1";
            this.digiwinLabel1.Size = new System.Drawing.Size(83, 12);
            this.digiwinLabel1.TabIndex = 1;
            this.digiwinLabel1.Text = "默认PMC接单日";
            // 
            // btnOk
            // 
            this.btnOk.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(234)))), ((int)(((byte)(234)))), ((int)(((byte)(234)))));
            this.btnOk.Location = new System.Drawing.Point(117, 121);
            this.btnOk.Name = "btnOk";
            this.btnOk.Size = new System.Drawing.Size(75, 23);
            this.btnOk.TabIndex = 2;
            this.btnOk.Text = "确定";
            this.btnOk.UseVisualStyleBackColor = true;
            this.btnOk.Click += new System.EventHandler(this.btnOk_Click);
            // 
            // btnCancel
            // 
            this.btnCancel.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(234)))), ((int)(((byte)(234)))), ((int)(((byte)(234)))));
            this.btnCancel.Location = new System.Drawing.Point(220, 121);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(75, 23);
            this.btnCancel.TabIndex = 2;
            this.btnCancel.Text = "取消";
            this.btnCancel.UseVisualStyleBackColor = true;
            this.btnCancel.Click += new System.EventHandler(this.btnCancel_Click);
            // 
            // ChangeDateForm
            // 
            this.Appearance.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(217)))), ((int)(((byte)(221)))), ((int)(((byte)(224)))));
            this.Appearance.Options.UseBackColor = true;
            this.ClientSize = new System.Drawing.Size(318, 172);
            this.Controls.Add(this.btnCancel);
            this.Controls.Add(this.btnOk);
            this.Controls.Add(this.digiwinLabel1);
            this.Controls.Add(this.ddtpDate);
            this.LookAndFeel.SkinName = "DigiwinCommonViewSkin";
            this.LookAndFeel.UseDefaultLookAndFeel = false;
            this.MaximumSize = new System.Drawing.Size(334, 210);
            this.MinimumSize = new System.Drawing.Size(334, 210);
            this.Name = "ChangeDateForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "批量维护PMC接单日";
            this.Load += new System.EventHandler(this.ChangeDateForm_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private Digiwin.Common.UI.DigiwinDateTimePicker ddtpDate;
        private Digiwin.Common.UI.DigiwinLabel digiwinLabel1;
        private Digiwin.Common.UI.DigiwinButton btnOk;
        private Digiwin.Common.UI.DigiwinButton btnCancel;
    }
}