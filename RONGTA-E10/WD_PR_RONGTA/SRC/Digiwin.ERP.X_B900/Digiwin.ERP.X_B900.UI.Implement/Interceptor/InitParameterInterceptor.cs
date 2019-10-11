using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Digiwin.Common;
using Digiwin.Common.UI;
using Digiwin.Common.Torridity;
using Digiwin.Common.Services;
using System.Windows.Forms;
using Digiwin.Common.Advanced;
using Digiwin.ERP.X_B900.UI.Implement.Properties;
using System.Reflection;

namespace Digiwin.ERP.X_B900.UI.Implement {
    [EventInterceptorClass]
    public class InitParameterInterceptor : ServiceComponent {
        [EventInterceptor(typeof(IDocumentBatchServiceEvents), "InitializedParameter")]
        private void OnBatchProcessInitial(object sender, DocumentBatchServiceEventArgs e) {
            DependencyObject entity = e.DataSource as DependencyObject;
            if (entity != null) {
                DependencyObjectCollection detail = entity["X_B900_D"] as DependencyObjectCollection;
                DependencyObjectCollection selectColl = FilterCollection(detail);
                e.Parameters.Add(new DataParameter("X_B900_D", selectColl));
            } else {
                e.Parameters.Add(new DataParameter("X_B900_D", null));
            }
        }

        [EventInterceptor(typeof(IDocumentBatchServiceEvents), "Confirm")]
        private void OnBatchProcessConfirm(object sender, DocumentBatchConfirmEventArgs e) {
            DependencyObject entity = e.DataSource as DependencyObject;
            if (entity != null) {
                DependencyObjectCollection detail = entity["X_B900_D"] as DependencyObjectCollection;
                DependencyObjectCollection selectColl = FilterCollection(detail);
                if (selectColl.Count <= 0) {
                    DigiwinMessageBox.ShowInfo(Resources.NoRecord);
                    e.Cancel = true;
                }
            }
        }

        [EventInterceptor(typeof(ICurrentConditionProjectWindow), "Load")]
        public void CurrentConditionProjectWindowLoad(object sender, EventArgs e) {
            //添加完成后事件
            ConditionProjectWindow win = (ConditionProjectWindow)sender;
            EventInfo ev = win.GetType().GetEvent("Processed");
            EventHandler<BatchProcessedEventArgs> handler = new EventHandler<BatchProcessedEventArgs>(OnProcessed);
            ev.AddEventHandler(win, handler);
        }

        [EventInterceptor(typeof(IEditorView), "Load")]
        private void Load(object sender, EventArgs e) {
            Control ctr = null;
            IFindControlService findSer = this.GetService<IFindControlService>();
            //查询
            if (findSer.TryGet("Xbutton1", out ctr)) {
                DigiwinButton button = ctr as DigiwinButton;
                button.Click += new EventHandler(button_Click);
            }

            //全选
            if (findSer.TryGet("XlinkLabel1", out ctr)) {
                DigiwinLinkLabel linkSelect = ctr as DigiwinLinkLabel;
                linkSelect.Click += new EventHandler(linkSelect_Click);
            }

            //取消全选
            if (findSer.TryGet("XlinkLabel11", out ctr)) {
                DigiwinLinkLabel linkUnSelect = ctr as DigiwinLinkLabel;
                linkUnSelect.Click += new EventHandler(linkUnSelect_Click);
            }

            //最大化
            (sender as EditorView).Control.FindForm().WindowState = FormWindowState.Maximized;
        }

        void button_Click(object sender, EventArgs e) {
            using (ChangeDateForm form = new ChangeDateForm(this.ResourceServiceProvider)) {
                if (form.ShowDialog() == DialogResult.OK) {
                    SetDetailData(form.Result);
                }
            }
        }

        void SetDetailData(DependencyObjectCollection result) {
            ICurrentDocumentWindow win = this.GetServiceForThisTypeKey<ICurrentDocumentWindow>();
            if (win != null) {
                DependencyObject root = win.EditController.EditorView.DataSource as DependencyObject;
                if (root != null) {
                    DependencyObjectCollection detail = root["X_B900_D"] as DependencyObjectCollection;
                    detail.Clear();
                    try {
                        EnableFormular(false);
                        foreach (DependencyObject item in result) {
                            DependencyObject line = detail.AddNew();
                            line["X_SELECT"] = true;
                            line["X_SALES_ORDER_DOC_ID"] = item["SALES_ORDER_DOC_ID"];
                            line["X_ITEM_ID"] = item["ITEM_ID"];
                            line["X_ITEM_CODE"] = item["ITEM_CODE"];
                            line["X_ITEM_NAME"] = item["ITEM_NAME"];
                            line["X_ITEM_SPEC"] = item["ITEM_SPECIFICATION"];
                            line["X_PMC_DATE"] = item["X_PMC_DATE"];
                            line["X_QTY"] = item["QTY"];
                            line["X_DOC_NO"] = item["DOC_NO"];
                            line["X_SALES_ORDER_DOC_D_ID"] = item["SALES_ORDER_DOC_D_ID"];
                        }
                    } finally {
                        EnableFormular(true);
                    }
                }
            }
        }

        void EnableFormular(bool enable) {
            IFormulaContainer service = this.GetServiceForThisTypeKey<IFormulaContainer>();
            foreach (var item in service.Formulas) {
                if (item.Id == "XFLD_001") {
                    item.Enabled = enable;
                    break;
                }
            }
        }

        void linkSelect_Click(object sender, EventArgs e) {
            SetSelectStatus(true);
        }

        void linkUnSelect_Click(object sender, EventArgs e) {
            SetSelectStatus(false);
        }

        void SetSelectStatus(bool status) {
            ICurrentDocumentWindow win = this.GetServiceForThisTypeKey<ICurrentDocumentWindow>();
            if (win != null) {
                DependencyObject root = win.EditController.EditorView.DataSource as DependencyObject;
                if (root != null) {
                    DependencyObjectCollection detail = root["X_B900_D"] as DependencyObjectCollection;
                    foreach (DependencyObject item in detail) {
                        item["X_SELECT"] = status;
                    }
                }
            }
        }

        private DependencyObjectCollection FilterCollection(DependencyObjectCollection coll) {
            DependencyObjectCollection newColl = new DependencyObjectCollection(coll.ItemDependencyObjectType);
            foreach (var item in coll) {
                if (Convert.ToBoolean(item["X_SELECT"].ToString())) {
                    newColl.Add(item);
                }
            }

            return newColl;
        }

        private void OnProcessed(object sender, BatchProcessedEventArgs e) {
            PropertyInfo p = sender.GetType().GetProperty("DataSource");
            object obj = p.GetValue(sender, null);
            DependencyObject entity = obj as DependencyObject;
            DependencyObjectCollection detail = entity["X_B900_D"] as DependencyObjectCollection;

            List<DependencyObject> toDelete = new List<DependencyObject>();
            foreach (var item in detail) {
                if (Convert.ToBoolean(item["X_SELECT"].ToString())) {
                    toDelete.Add(item);
                }
            }

            foreach (var item in toDelete) {
                detail.Remove(item);
            }
        }
    }
}
