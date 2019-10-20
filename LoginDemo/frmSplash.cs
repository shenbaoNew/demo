using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace LoginDemo {
    public partial class frmSplash : Form,ISplashForm {
        public frmSplash() {
            InitializeComponent();
        }

        #region ISplashForm 成员

        public void SetStatusInfo(string NewStatusInfo) {
            lbStatusInfo.Text = NewStatusInfo;
        }

        #endregion
    }
}
