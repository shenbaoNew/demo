using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Digiwin.Common.UI;
using Digiwin.ERP.X_B900.Business;
using Digiwin.Common.Advanced;
using Digiwin.ERP.X_B900.UI.Implement.Properties;
using Digiwin.Common.Torridity;

namespace Digiwin.ERP.X_B900.UI.Implement {
    public partial class ChangeDateForm : DigiwinForm {
        private IResourceServiceProvider _provider;
        public DependencyObjectCollection Result { get; set; }
        public ChangeDateForm(IResourceServiceProvider provider) {
            InitializeComponent();

            this._provider = provider;
        }

        private void btnOk_Click(object sender, EventArgs e) {
            SetResult();
            this.DialogResult = DialogResult.OK;
        }

        private void btnCancel_Click(object sender, EventArgs e) {
            this.DialogResult = DialogResult.Cancel;
        }

        private void UpdateDate() {
            IXB900Service service = _provider.GetService(typeof(IXB900Service), "X_B900") as IXB900Service;
            service.UpdateDate(this.ddtpDate.Value.Date);
            DigiwinMessageBox.ShowInfo(Resources.Info);
        }

        private void SetResult() {
            IXB900Service service = _provider.GetService(typeof(IXB900Service), "X_B900") as IXB900Service;
            Result = service.QuerySalesOrderDocDetail(this.ddtpDate.Value.Date);
        }

        private void ChangeDateForm_Load(object sender, EventArgs e) {
            this.ddtpDate.Value = DateTime.Now.Date;
        }
    }
}
