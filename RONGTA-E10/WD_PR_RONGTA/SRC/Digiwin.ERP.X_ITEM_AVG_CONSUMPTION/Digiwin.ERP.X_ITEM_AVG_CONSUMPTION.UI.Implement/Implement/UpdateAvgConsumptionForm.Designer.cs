namespace Digiwin.ERP.X_ITEM_AVG_CONSUMPTION.UI.Implement {
    partial class UpdateAvgConsumptionForm {
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(UpdateAvgConsumptionForm));
            this.splitContainer1 = new System.Windows.Forms.SplitContainer();
            this.teAvgConsumption = new Digiwin.Common.UI.DigiwinTextEdit();
            this.lbAvgConsumption = new Digiwin.Common.UI.DigiwinLabel();
            this.btnCancel = new Digiwin.Common.UI.DigiwinButton();
            this.btnOk = new Digiwin.Common.UI.DigiwinButton();
            this.splitContainer1.Panel1.SuspendLayout();
            this.splitContainer1.Panel2.SuspendLayout();
            this.splitContainer1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.teAvgConsumption.Properties)).BeginInit();
            this.SuspendLayout();
            // 
            // splitContainer1
            // 
            this.splitContainer1.AccessibleDescription = null;
            this.splitContainer1.AccessibleName = null;
            resources.ApplyResources(this.splitContainer1, "splitContainer1");
            this.splitContainer1.BackgroundImage = null;
            this.splitContainer1.Font = null;
            this.splitContainer1.Name = "splitContainer1";
            // 
            // splitContainer1.Panel1
            // 
            this.splitContainer1.Panel1.AccessibleDescription = null;
            this.splitContainer1.Panel1.AccessibleName = null;
            resources.ApplyResources(this.splitContainer1.Panel1, "splitContainer1.Panel1");
            this.splitContainer1.Panel1.BackgroundImage = null;
            this.splitContainer1.Panel1.Controls.Add(this.teAvgConsumption);
            this.splitContainer1.Panel1.Controls.Add(this.lbAvgConsumption);
            this.splitContainer1.Panel1.Font = null;
            // 
            // splitContainer1.Panel2
            // 
            this.splitContainer1.Panel2.AccessibleDescription = null;
            this.splitContainer1.Panel2.AccessibleName = null;
            resources.ApplyResources(this.splitContainer1.Panel2, "splitContainer1.Panel2");
            this.splitContainer1.Panel2.BackgroundImage = null;
            this.splitContainer1.Panel2.Controls.Add(this.btnCancel);
            this.splitContainer1.Panel2.Controls.Add(this.btnOk);
            this.splitContainer1.Panel2.Font = null;
            // 
            // teAvgConsumption
            // 
            resources.ApplyResources(this.teAvgConsumption, "teAvgConsumption");
            this.teAvgConsumption.BackgroundImage = null;
            this.teAvgConsumption.Name = "teAvgConsumption";
            this.teAvgConsumption.Properties.AccessibleDescription = null;
            this.teAvgConsumption.Properties.AccessibleName = null;
            this.teAvgConsumption.Properties.Appearance.GradientMode = ((System.Drawing.Drawing2D.LinearGradientMode)(resources.GetObject("teAvgConsumption.Properties.Appearance.GradientMode")));
            this.teAvgConsumption.Properties.Appearance.Image = null;
            this.teAvgConsumption.Properties.Appearance.Options.UseTextOptions = true;
            this.teAvgConsumption.Properties.Appearance.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Far;
            this.teAvgConsumption.Properties.AutoHeight = ((bool)(resources.GetObject("teAvgConsumption.Properties.AutoHeight")));
            this.teAvgConsumption.Properties.Mask.AutoComplete = ((DevExpress.XtraEditors.Mask.AutoCompleteType)(resources.GetObject("teAvgConsumption.Properties.Mask.AutoComplete")));
            this.teAvgConsumption.Properties.Mask.BeepOnError = ((bool)(resources.GetObject("teAvgConsumption.Properties.Mask.BeepOnError")));
            this.teAvgConsumption.Properties.Mask.EditMask = resources.GetString("teAvgConsumption.Properties.Mask.EditMask");
            this.teAvgConsumption.Properties.Mask.IgnoreMaskBlank = ((bool)(resources.GetObject("teAvgConsumption.Properties.Mask.IgnoreMaskBlank")));
            this.teAvgConsumption.Properties.Mask.MaskType = ((DevExpress.XtraEditors.Mask.MaskType)(resources.GetObject("teAvgConsumption.Properties.Mask.MaskType")));
            this.teAvgConsumption.Properties.Mask.PlaceHolder = ((char)(resources.GetObject("teAvgConsumption.Properties.Mask.PlaceHolder")));
            this.teAvgConsumption.Properties.Mask.SaveLiteral = ((bool)(resources.GetObject("teAvgConsumption.Properties.Mask.SaveLiteral")));
            this.teAvgConsumption.Properties.Mask.ShowPlaceHolders = ((bool)(resources.GetObject("teAvgConsumption.Properties.Mask.ShowPlaceHolders")));
            this.teAvgConsumption.Properties.Mask.UseMaskAsDisplayFormat = ((bool)(resources.GetObject("teAvgConsumption.Properties.Mask.UseMaskAsDisplayFormat")));
            this.teAvgConsumption.Properties.NullValuePrompt = resources.GetString("teAvgConsumption.Properties.NullValuePrompt");
            this.teAvgConsumption.Properties.NullValuePromptShowForEmptyValue = ((bool)(resources.GetObject("teAvgConsumption.Properties.NullValuePromptShowForEmptyValue")));
            this.teAvgConsumption.Properties.ValidateOnEnterKey = true;
            this.teAvgConsumption.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.TeAvgConsumption_KeyPress);
            // 
            // lbAvgConsumption
            // 
            this.lbAvgConsumption.AccessibleDescription = null;
            this.lbAvgConsumption.AccessibleName = null;
            resources.ApplyResources(this.lbAvgConsumption, "lbAvgConsumption");
            this.lbAvgConsumption.Font = null;
            this.lbAvgConsumption.Name = "lbAvgConsumption";
            // 
            // btnCancel
            // 
            this.btnCancel.AccessibleDescription = null;
            this.btnCancel.AccessibleName = null;
            resources.ApplyResources(this.btnCancel, "btnCancel");
            this.btnCancel.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(234)))), ((int)(((byte)(234)))), ((int)(((byte)(234)))));
            this.btnCancel.BackgroundImage = null;
            this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.btnCancel.Font = null;
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.UseVisualStyleBackColor = true;
            // 
            // btnOk
            // 
            this.btnOk.AccessibleDescription = null;
            this.btnOk.AccessibleName = null;
            resources.ApplyResources(this.btnOk, "btnOk");
            this.btnOk.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(234)))), ((int)(((byte)(234)))), ((int)(((byte)(234)))));
            this.btnOk.BackgroundImage = null;
            this.btnOk.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.btnOk.Font = null;
            this.btnOk.Name = "btnOk";
            this.btnOk.UseVisualStyleBackColor = true;
            // 
            // UpdateAvgConsumptionForm
            // 
            this.AccessibleDescription = null;
            this.AccessibleName = null;
            this.Appearance.BackColor = ((System.Drawing.Color)(resources.GetObject("UpdateAvgConsumptionForm.Appearance.BackColor")));
            this.Appearance.GradientMode = ((System.Drawing.Drawing2D.LinearGradientMode)(resources.GetObject("UpdateAvgConsumptionForm.Appearance.GradientMode")));
            this.Appearance.Image = null;
            this.Appearance.Options.UseBackColor = true;
            resources.ApplyResources(this, "$this");
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.splitContainer1);
            this.LookAndFeel.SkinName = "DigiwinCommonViewSkin";
            this.LookAndFeel.UseDefaultLookAndFeel = false;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "UpdateAvgConsumptionForm";
            this.splitContainer1.Panel1.ResumeLayout(false);
            this.splitContainer1.Panel1.PerformLayout();
            this.splitContainer1.Panel2.ResumeLayout(false);
            this.splitContainer1.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.teAvgConsumption.Properties)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.SplitContainer splitContainer1;
        private Digiwin.Common.UI.DigiwinLabel lbAvgConsumption;
        private Digiwin.Common.UI.DigiwinButton btnCancel;
        private Digiwin.Common.UI.DigiwinButton btnOk;
        private Digiwin.Common.UI.DigiwinTextEdit teAvgConsumption;
    }
}