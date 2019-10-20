using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace WindowsFormsApplication7 {
    static class Program {
        /// <summary>
        /// 应用程序的主入口点。
        /// </summary>
        [STAThread]
        static void Main() {
            //界面汉化
            System.Threading.Thread.CurrentThread.CurrentUICulture = new System.Globalization.CultureInfo("zh-CN");
            DevExpress.UserSkins.BonusSkins.Register();
            DevExpress.Skins.SkinManager.EnableFormSkins();

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            frmLogin login = new frmLogin();
            if (login.ShowDialog() == DialogResult.OK) {
                Splasher.Show(typeof(frmSplash));

                frmMain main = new frmMain();
                main.StartPosition = FormStartPosition.CenterScreen;
                Application.Run(main);
            }
        }
    }
}
