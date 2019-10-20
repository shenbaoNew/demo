using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using DevExpress.XtraEditors;
using System.Xml.Serialization;

namespace WindowsFormsApplication7.Controls {
    public partial class Pager : XtraUserControl {
        private int _pageSize;
        /// <summary>
        /// 设置或获取每页显示的记录数目
        /// </summary>
        [Description("设置或获取每页显示的记录数目"), DefaultValue(50), Category("分页")]
        public int PageSize {
            set {
                this._pageSize = value;
            }
            get {
                return this._pageSize;
            }
        }
        private int _recordCount;
        /// <summary>
        /// 设置或获取记录总数
        /// </summary>
        [Description("设置或获取记录总数"), Category("分页")]
        public int RecordCount {
            set {
                this._recordCount = value;
            }
            get {
                return this._recordCount;
            }
        }

        private int _pageCount;
        /// <summary>
        /// 记录总页数
        /// </summary>
        [Description("记录总页数"), DefaultValue(0), Category("分页")]
        public int PageCount {
            get {
                return this._pageCount;
            }
        }
        private int _currrentPage;
        /// <summary>
        /// 当前页, 开始为1
        /// </summary>
        [Description("当前页, 开始为1"), DefaultValue(1), Category("分页")]
        [Browsable(false)]
        public int CurrentPage {
            set {
                this._currrentPage = value;
            }
            get {
                return this._currrentPage;
            }
        }

        /// <summary>
        /// 页改变事件
        /// </summary>
        public event EventHandler PageChanged;
        /// <summary>
        /// 导出当期页
        /// </summary>
        public event EventHandler ExportCurrentPage;
        /// <summary>
        /// 导出所有页
        /// </summary>
        public event EventHandler ExportAllPage;

        public Pager() {
            InitializeComponent();
            InitPageInfo();
        }

        /// <summary> 
        /// 初始化分页信息
        /// <param name="pageSize">每页记录数</param>
        /// <param name="recordCount">总记录数</param>
        /// </summary>
        public void InitPageInfo(int recordCount, int pageSize) {
            this._recordCount = recordCount;
            this._pageSize = pageSize;
            this.InitPageInfo();
        }

        /// <summary>
        /// 初始化分页信息
        /// </summary>
        public void InitPageInfo() {
            if (this._pageSize < 1)
                this._pageSize = 50; //如果每页记录数不正确，即更改为10
            if (this._recordCount < 0)
                this._recordCount = 0; //如果记录总数不正确，即更改为0

            //取得总页数
            if (this._recordCount % this._pageSize == 0) {
                this._pageCount = this._recordCount / this._pageSize;
            } else {
                this._pageCount = this._recordCount / this._pageSize + 1;
            }

            //设置当前页
            if (this._currrentPage > this._pageCount) {
                this._currrentPage = this._pageCount;
            }
            if (this._currrentPage < 1) {
                this._currrentPage = 1;
            }

            //设置按钮的可用性
            bool enable = (this._currrentPage > 1);
            this.sbtnPre.Enabled = enable;

            enable = (this.CurrentPage < this.PageCount);
            this.sbtnNext.Enabled = enable;

            this.txtPage.Text = this._currrentPage.ToString();
            this.lblPageInfo.Text = string.Format("共 {0} 条记录，每页 {1} 条，共 {2} 页", this.RecordCount, this.PageSize, this.PageCount);
        }

        /// <summary>
        /// 页面变化处理
        /// </summary>
        /// <param name="e"></param>
        protected virtual void OnPageChanged(EventArgs e) {
            if (PageChanged != null) {
                PageChanged(this, e);
            }
        }

        /// <summary>
        /// 刷新页面数据
        /// </summary>
        /// <param name="page">页码</param>
        public void RefreshData(int page) {
            this.CurrentPage = page;
            EventArgs e = new EventArgs();
            OnPageChanged(e);
        }

        private void sbtnFirst_Click(object sender, EventArgs e) {
            this.RefreshData(1);
        }

        private void sbtnPre_Click(object sender, EventArgs e) {
            if (this.CurrentPage > 1) {
                this.RefreshData(this.CurrentPage - 1);
            } else {
                this.RefreshData(1);
            }
        }

        private void sbtnNext_Click(object sender, EventArgs e) {
            if (this.CurrentPage < this.PageCount) {
                this.RefreshData(this.CurrentPage + 1);
            } else if (this.PageCount < 1) {
                this.RefreshData(1);
            } else {
                this.RefreshData(this.PageCount);
            }
        }

        private void sbtnLast_Click(object sender, EventArgs e) {
            if (this.PageCount > 0) {
                this.RefreshData(this.PageCount);
            } else {
                this.RefreshData(1);
            }
        }

        private void sbtnExportCurPage_Click(object sender, EventArgs e) {
            OnExportCurPage();
        }

        private void sbtnExportAllPage_Click(object sender, EventArgs e) {
            OnExportAllPage();
        }

        protected virtual void OnExportCurPage() {
            if (ExportCurrentPage != null) {
                EventArgs e = new EventArgs();
                ExportCurrentPage(this, e);
            }
        }

        protected virtual void OnExportAllPage() {
            if (ExportAllPage != null) {
                EventArgs e = new EventArgs();
                ExportAllPage(this, e);
            }
        }

        private void txtPage_KeyDown(object sender, KeyEventArgs e) {
            if (e.KeyCode == Keys.Enter) {
                int num;
                try {
                    num = Convert.ToInt32(this.txtPage.Text);
                } catch (Exception ex) {
                    num = 1;
                }

                if (num > this._pageCount)
                    num = this._pageCount;
                if (num < 1)
                    num = 1;

                this.RefreshData(num);
            }
        }
    }

    [Serializable]
    public class PagerInfo {
        public event EventHandler PageInfoChanged;

        private int _currenetPage; //当前页码
        private int _pageSize;//每页显示的记录
        private int _recordCount;//记录总数

        #region 属性变量

        /// <summary>
        /// 获取或设置当前页码
        /// </summary>
        [XmlElement(ElementName = "CurrenetPage")]
        public int CurrentPage {
            get { return _currenetPage; }
            set {
                _currenetPage = value;
                OnPageInfoChanged();
            }
        }

        /// <summary>
        /// 获取或设置每页显示的记录
        /// </summary>
        [XmlElement(ElementName = "PageSize")]
        public int PageSize {
            get { return _pageSize; }
            set {
                _pageSize = value;
                OnPageInfoChanged();
            }
        }

        /// <summary>
        /// 获取或设置记录总数
        /// </summary>
        [XmlElement(ElementName = "RecordCount")]
        public int RecordCount {
            get { return _recordCount; }
            set {
                _recordCount = value;
                OnPageInfoChanged();
            }
        }

        #endregion

        protected virtual void OnPageInfoChanged() {
            if (PageInfoChanged != null) {
                PageInfoChanged(this, EventArgs.Empty);
            }
        }
    }
}
