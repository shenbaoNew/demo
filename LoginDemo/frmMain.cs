using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Threading;
using DevExpress.XtraPrinting;
using DevExpress.XtraEditors;
using System.Data.SqlClient;

namespace WindowsFormsApplication7 {
    public partial class frmMain : DevExpress.XtraBars.Ribbon.RibbonForm {
        public frmMain() {
            InitializeComponent();

        }

        private void frmMain_Load(object sender, EventArgs e) {
            this.pagerGrid.PageChanged += new EventHandler(pagerGrid_PageChanged);

            Splasher.Status = "数据初始化完毕....";

            Thread.Sleep(1000);
            Splasher.Status = "用户验证通过,正在进入系统....";

            Splasher.Close();

            this.pagerGrid.GridPageInfo.RecordCount = 2592;
            BindData();
        }

        void pagerGrid_PageChanged(object sender, EventArgs e) {
            BindData();
        }

        private void InitData() {
            DataTable data = new DataTable();
            data.Columns.Add(new DataColumn("Code", typeof(string)));
            data.Columns.Add(new DataColumn("Name", typeof(string)));

            for (int i = 0; i < 100; i++) {
                DataRow dr = data.NewRow();
                dr["Code"] = "001";
                dr["Name"] = "xxx";
                data.Rows.Add(dr);
            }

        }

        private void button1_Click(object sender, EventArgs e) {
            PrintableComponentLink link = new PrintableComponentLink(new PrintingSystem());
            link.Component = this.pagerGrid.Grid;
            link.Landscape = true;
            link.PaperKind = System.Drawing.Printing.PaperKind.A3;
            link.CreateMarginalHeaderArea += new CreateAreaEventHandler(Link_CreateMarginalHeaderArea);
            link.CreateDocument();
            link.ShowPreview();
        }

        private void Link_CreateMarginalHeaderArea(object sender, CreateAreaEventArgs e) {
            string title = "xxxxxx"+   "备件信息报表";
            PageInfoBrick brick = e.Graph.DrawPageInfo(PageInfo.None, title, Color.DarkBlue,
               new RectangleF(0, 0, 100, 21), BorderSide.None);

            brick.LineAlignment = BrickAlignment.Center;
            brick.Alignment = BrickAlignment.Center;
            brick.AutoWidth = true;
            brick.Font = new System.Drawing.Font("宋体", 11f, FontStyle.Bold);
        }

        private void BindData() {
            DataSet ds = new DataSet();
            using (SqlConnection conn = new SqlConnection("Data Source=127.0.0.1;Initial Catalog=E10_6.0_KF;User ID=sa;Password=110abcABC;Application Name=DcmsCRM;Connect Timeout=30;Pooling=True;Max Pool Size=4000")) {
                conn.Open();
                string strOrder = string.Format(" order by {0} {1}", "DOC_NO", "ASC");
                int minRow = pagerGrid.GridPageInfo.PageSize * (pagerGrid.GridPageInfo.CurrentPage - 1) + 1;
                int maxRow = pagerGrid.GridPageInfo.PageSize * pagerGrid.GridPageInfo.CurrentPage;

                string sql = string.Format(@"With Paging AS
                ( SELECT ROW_NUMBER() OVER ({0}) as RowNumber, {1} FROM {2} Where {3})
                SELECT * FROM Paging WHERE RowNumber Between {4} and {5}", strOrder, "DOC_NO,DOC_DATE,MO_ID", "MO", "1=1",
                minRow, maxRow);

                SqlDataAdapter sda = new SqlDataAdapter(sql, conn);
                sda.Fill(ds);
            }

            this.pagerGrid.DataSource = ds.Tables[0];
        }
    }
}
