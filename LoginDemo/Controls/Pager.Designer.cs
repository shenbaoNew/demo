namespace LoginDemo.Controls {
    partial class Pager {
        /// <summary> 
        /// 必需的设计器变量。
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary> 
        /// 清理所有正在使用的资源。
        /// </summary>
        /// <param name="disposing">如果应释放托管资源，为 true；否则为 false。</param>
        protected override void Dispose(bool disposing) {
            if (disposing && (components != null)) {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region 组件设计器生成的代码

        /// <summary> 
        /// 设计器支持所需的方法 - 不要
        /// 使用代码编辑器修改此方法的内容。
        /// </summary>
        private void InitializeComponent() {
            this.lblPageInfo = new DevExpress.XtraEditors.LabelControl();
            this.sbtnFirst = new DevExpress.XtraEditors.SimpleButton();
            this.sbtnPre = new DevExpress.XtraEditors.SimpleButton();
            this.sbtnNext = new DevExpress.XtraEditors.SimpleButton();
            this.sbtnLast = new DevExpress.XtraEditors.SimpleButton();
            this.txtPage = new DevExpress.XtraEditors.TextEdit();
            this.sbtnExportCurPage = new DevExpress.XtraEditors.SimpleButton();
            this.sbtnExportAllPage = new DevExpress.XtraEditors.SimpleButton();
            ((System.ComponentModel.ISupportInitialize)(this.txtPage.Properties)).BeginInit();
            this.SuspendLayout();
            // 
            // lblPageInfo
            // 
            this.lblPageInfo.Location = new System.Drawing.Point(12, 6);
            this.lblPageInfo.Name = "lblPageInfo";
            this.lblPageInfo.Size = new System.Drawing.Size(213, 14);
            this.lblPageInfo.TabIndex = 1;
            this.lblPageInfo.Text = "共 {0} 条记录，每页 {1} 条，共 {2} 页";
            // 
            // sbtnFirst
            // 
            this.sbtnFirst.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.sbtnFirst.Location = new System.Drawing.Point(271, 3);
            this.sbtnFirst.Name = "sbtnFirst";
            this.sbtnFirst.Size = new System.Drawing.Size(31, 23);
            this.sbtnFirst.TabIndex = 2;
            this.sbtnFirst.Text = "|<";
            this.sbtnFirst.Click += new System.EventHandler(this.sbtnFirst_Click);
            // 
            // sbtnPre
            // 
            this.sbtnPre.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.sbtnPre.Location = new System.Drawing.Point(308, 3);
            this.sbtnPre.Name = "sbtnPre";
            this.sbtnPre.Size = new System.Drawing.Size(31, 23);
            this.sbtnPre.TabIndex = 3;
            this.sbtnPre.Text = "<";
            this.sbtnPre.Click += new System.EventHandler(this.sbtnPre_Click);
            // 
            // sbtnNext
            // 
            this.sbtnNext.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.sbtnNext.Location = new System.Drawing.Point(390, 3);
            this.sbtnNext.Name = "sbtnNext";
            this.sbtnNext.Size = new System.Drawing.Size(31, 23);
            this.sbtnNext.TabIndex = 4;
            this.sbtnNext.Text = ">";
            this.sbtnNext.Click += new System.EventHandler(this.sbtnNext_Click);
            // 
            // sbtnLast
            // 
            this.sbtnLast.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.sbtnLast.Location = new System.Drawing.Point(427, 3);
            this.sbtnLast.Name = "sbtnLast";
            this.sbtnLast.Size = new System.Drawing.Size(31, 23);
            this.sbtnLast.TabIndex = 5;
            this.sbtnLast.Text = ">|";
            this.sbtnLast.Click += new System.EventHandler(this.sbtnLast_Click);
            // 
            // txtPage
            // 
            this.txtPage.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.txtPage.Location = new System.Drawing.Point(345, 5);
            this.txtPage.Name = "txtPage";
            this.txtPage.Size = new System.Drawing.Size(39, 21);
            this.txtPage.TabIndex = 6;
            this.txtPage.KeyDown += new System.Windows.Forms.KeyEventHandler(this.txtPage_KeyDown);
            // 
            // sbtnExportCurPage
            // 
            this.sbtnExportCurPage.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.sbtnExportCurPage.Location = new System.Drawing.Point(464, 3);
            this.sbtnExportCurPage.Name = "sbtnExportCurPage";
            this.sbtnExportCurPage.Size = new System.Drawing.Size(69, 23);
            this.sbtnExportCurPage.TabIndex = 7;
            this.sbtnExportCurPage.Text = "导出当前页";
            this.sbtnExportCurPage.Click += new System.EventHandler(this.sbtnExportCurPage_Click);
            // 
            // sbtnExportAllPage
            // 
            this.sbtnExportAllPage.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.sbtnExportAllPage.Location = new System.Drawing.Point(539, 3);
            this.sbtnExportAllPage.Name = "sbtnExportAllPage";
            this.sbtnExportAllPage.Size = new System.Drawing.Size(69, 23);
            this.sbtnExportAllPage.TabIndex = 8;
            this.sbtnExportAllPage.Text = "导出所有页";
            this.sbtnExportAllPage.Click += new System.EventHandler(this.sbtnExportAllPage_Click);
            // 
            // Pager
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 14F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.sbtnExportAllPage);
            this.Controls.Add(this.sbtnExportCurPage);
            this.Controls.Add(this.txtPage);
            this.Controls.Add(this.sbtnLast);
            this.Controls.Add(this.sbtnNext);
            this.Controls.Add(this.sbtnPre);
            this.Controls.Add(this.sbtnFirst);
            this.Controls.Add(this.lblPageInfo);
            this.Name = "Pager";
            this.Size = new System.Drawing.Size(614, 29);
            ((System.ComponentModel.ISupportInitialize)(this.txtPage.Properties)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private DevExpress.XtraEditors.LabelControl lblPageInfo;
        private DevExpress.XtraEditors.SimpleButton sbtnFirst;
        private DevExpress.XtraEditors.SimpleButton sbtnPre;
        private DevExpress.XtraEditors.SimpleButton sbtnNext;
        private DevExpress.XtraEditors.SimpleButton sbtnLast;
        private DevExpress.XtraEditors.TextEdit txtPage;
        private DevExpress.XtraEditors.SimpleButton sbtnExportCurPage;
        private DevExpress.XtraEditors.SimpleButton sbtnExportAllPage;
    }
}
