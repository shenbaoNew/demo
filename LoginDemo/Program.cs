using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using System.Threading;

namespace LoginDemo {
    static class Program {
        /// <summary>
        /// 应用程序的主入口点。
        /// </summary>
        [STAThread]
        static void Main() {
            GlobalMutex();

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

        private static Mutex mutex = null;
        private static void GlobalMutex() {
            // 是否第一次创建mutex
            bool newMutexCreated = false;
            string mutexName = "Global\\" + "System";
            try {
                mutex = new Mutex(false, mutexName, out newMutexCreated);
            } catch (Exception ex) {
                Console.Write(ex.Message);
                System.Threading.Thread.Sleep(1000);
                Environment.Exit(1);
            }

            // 第一次创建mutex
            if (newMutexCreated) {
                Console.WriteLine("程序已启动");
                //todo:此处为要执行的任务
            } else {
                MessageBox.Show("另一个窗口已在运行，不能重复运行。");
                System.Threading.Thread.Sleep(1000);
                Environment.Exit(1);//退出程序
            }
        }
    }
}
