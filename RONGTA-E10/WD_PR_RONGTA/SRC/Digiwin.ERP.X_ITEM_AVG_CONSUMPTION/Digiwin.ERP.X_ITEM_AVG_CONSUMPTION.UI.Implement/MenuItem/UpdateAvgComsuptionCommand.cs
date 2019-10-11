//----------------------------------------------------------------
//<Author>xuyang</author>
//<CreateDate>19-10-9</createDate>
//<IssueNo></IssueNo>
//<Description>容大个案</description>
//----------------------------------------------------------------
//^_^ 20191009 add by xuyang for 容大个案

using System.Windows.Forms;
using Digiwin.Common;
using Digiwin.Common.Advanced;
using Digiwin.Common.UI;
using Digiwin.ERP.X_ITEM_AVG_CONSUMPTION.Business;
namespace Digiwin.ERP.X_ITEM_AVG_CONSUMPTION.UI.Implement {
    /// <summary>
    /// 菜单命令
    /// </summary>
    public class UpdateAvgComsuptionCommand : CommandBase {
        /// <summary>
        /// 构造器
        /// </summary>
        public UpdateAvgComsuptionCommand()
            : base("UpdateAvgComsuptionCommand") {
        }
        /// <summary>
        ///  初始化服务容器，设置菜单命令的表决器
        /// </summary>
        protected override void InitializeServiceComponent() {
            base.InitializeServiceComponent();
            //添加监视器
            ICommandsService cmdService = this.GetServiceForThisTypeKey<ICommandsService>();
            this.EnabledDeciders.Add(cmdService.GetCommandEnabledDecider<UpdateAvgComsuptionCommandEnableDecider>());
          
        }
        /// <summary>
        /// 菜单命令Action名称
        /// </summary>
        public override string ActionName {
            get {
                return "UpdateAvgComsuptionCommand";
            }
        }

        /// <summary>
        /// 菜单命令执行
        /// </summary>
        public override void Execute() {
            using (UpdateAvgConsumptionForm uacForm = new UpdateAvgConsumptionForm()) {
                if (uacForm.ShowDialog() == DialogResult.OK) {
                    int calculateMonth = uacForm.CalculateMonth;
                    IQueryDataService qryDataSrv = this.GetServiceForThisTypeKey<IQueryDataService>();
                    qryDataSrv.UpdateAvgConsumption(calculateMonth);
                    ICurrentBrowseWindow browseWindow = this.GetServiceForThisTypeKey<ICurrentBrowseWindow>();
                    if (browseWindow != null && browseWindow.BrowseView != null) {                        
                        browseWindow.BrowseView.SynRefreshData();
                    }
                    IInfoEncodeContainer info = this.GetService<IInfoEncodeContainer>();
                    DigiwinMessageBox.ShowInfo(info.GetMessage("A110763"));
                }
            }
        }

        private void Process() {
        }

        /// <summary>
        /// 菜单的表决器
        /// </summary>
        private sealed class UpdateAvgComsuptionCommandEnableDecider : CommandEnabledDecider {
            /// <summary>
            /// 构造器
            /// </summary>
            public UpdateAvgComsuptionCommandEnableDecider()
                : base(0, true) {

            }
            /// <summary>
            /// 设置菜单有效的前提条件
            /// </summary>
            /// <param name="provider"></param>
            /// <param name="callContext"></param>
            /// <param name="context"></param>
            /// <returns></returns>
            protected override bool QueryEnabled(IResourceServiceProvider provider, ServiceCallContext callContext, IDataObject context) {
                bool result = base.QueryEnabled(provider, callContext, context);
                IGridWindowStateService windowStateSrv = (IGridWindowStateService)provider.GetService(typeof(IGridWindowStateService), callContext.TypeKey);
                if (windowStateSrv != null) {
                    result = result && windowStateSrv.EditState == EditState.Open;
                }
                return result;
            }
        }
    }
}