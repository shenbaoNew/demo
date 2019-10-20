using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using DevExpress.XtraEditors;
using System.Data.SqlClient;
using DevExpress.XtraGrid;

namespace WindowsFormsApplication7.Controls {
    public partial class PagerGrid : XtraUserControl {
        public event EventHandler DataSourceChanged;
        public event EventHandler PageChanged;

        private object _dateSource = null;
        /// <summary>
        /// 获取或设置数据源
        /// </summary>
        public object DataSource {
            get { return _dateSource; }
            set {
                if (this.gridView.Columns != null) {
                    this.gridView.Columns.Clear();
                }

                _dateSource = value;
                this.gridControl.DataSource = _dateSource;
                this.pageControl.InitPageInfo(GridPageInfo.RecordCount, GridPageInfo.PageSize);
            }
        }

        private PagerInfo _pageInfo;
        /// <summary>
        /// 分页信息
        /// </summary>
        public PagerInfo GridPageInfo {
            get {
                if (_pageInfo == null) {
                    _pageInfo = new PagerInfo();
                    _pageInfo.RecordCount = this.pageControl.RecordCount;
                    _pageInfo.CurrentPage = this.pageControl.CurrentPage;
                    _pageInfo.PageSize = this.pageControl.PageSize;
                } else {
                    _pageInfo.CurrentPage = this.pageControl.CurrentPage;
                }

                return _pageInfo;
            }
        }

        public GridControl Grid {
            get { return this.gridControl; }
        }

        public PagerGrid() {
            InitializeComponent();

            pageControl.PageChanged += new EventHandler(pageControl_PageChanged);
            pageControl.ExportCurrentPage += new EventHandler(pageControl_ExportCurrentPage);
            pageControl.ExportAllPage += new EventHandler(pageControl_ExportAllPage);
        }

        void pageControl_PageChanged(object sender, EventArgs e) {
            OnPageChanged();
        }

        void pageControl_ExportAllPage(object sender, EventArgs e) {
            
        }

        void pageControl_ExportCurrentPage(object sender, EventArgs e) {
            
        }

        protected virtual void OnDataSourceChanged() {
            if (this.DataSourceChanged != null) {
                this.DataSourceChanged(this, EventArgs.Empty);
            }
        }

        protected virtual void OnPageChanged() {
            if (this.PageChanged != null) {
                this.PageChanged(this, EventArgs.Empty);
            }
        }
    }
}
