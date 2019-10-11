//---------------------------------------------------------------- 
// <author>panzb</author>
// <createDate>20150429</createDate>
// <description>PP_B003组织批次参数切片</description>
//----------------------------------------------------------------
//20150831 modi by wangrm for T001-150807002
//20151229 modi by wangrm for T001-151228001
//20170613 modi by xuyang for S001-170607003 新增“所有未生成计划的单据”
//20171218 modi by xuyang for P001-170930001 自动跑计划
//20180129 modi by xuyang for P001-170930001 自动跑计划，所有未生成计划单据和自动发放到计划底稿必选
//20181220 modi by xuyang for T001-181210001 新增MRP状态检查
//20190121 modi by xuyang for T001-181106001 检测新增字段是否存在，不存在则新增
//20190912 modi by zhaijxz for T001-190903001 新增字段同步检查
using System;
using System.Collections;
using System.ComponentModel;
using System.Globalization;
using System.Windows.Forms;
using Digiwin.Common;
using Digiwin.Common.Services;
using Digiwin.Common.Torridity;
using Digiwin.Common.UI;
using Digiwin.ERP.Common.Utils;
using Digiwin.ERP.X_PP_B003.Business;

namespace Digiwin.ERP.X_PP_B003.UI.Implement {
    [EventInterceptorClass]
    [Description("PP_B003组织批次参数切片")]
    internal sealed class W001PPB003ParameterInterceptor : ServiceComponent {
        [EventInterceptor(typeof(IDocumentBatchServiceEvents), "InitializedParameter")]
        private void TissueParameterForGridDateSource(object sender, DocumentBatchServiceEventArgs e) {
            DependencyObject myEntity = e.DataSource;
            // Grid中数据

            if (e.Parameters.Contains("PP_B003_SALES_ORDER_DOCS")) {
                e.Parameters["PP_B003_SALES_ORDER_DOCS"].Value = myEntity["PP_B003_SALES_ORDER_DOCS"] as DependencyObjectCollection;
            } else {
                e.Parameters.Add(new DataParameter("PP_B003_SALES_ORDER_DOCS", myEntity["PP_B003_SALES_ORDER_DOCS"] as DependencyObjectCollection));
            }

            if (e.Parameters.Contains("PP_B003_TRANSFER_REQUISITION_DS")) {
                e.Parameters["PP_B003_TRANSFER_REQUISITION_DS"].Value = myEntity["PP_B003_TRANSFER_REQUISITION_DS"] as DependencyObjectCollection;
            } else {
                e.Parameters.Add(new DataParameter("PP_B003_TRANSFER_REQUISITION_DS", myEntity["PP_B003_TRANSFER_REQUISITION_DS"] as DependencyObjectCollection));
            }

            if (e.Parameters.Contains("PP_B003_INNER_ORDER_DOCS")) {
                e.Parameters["PP_B003_INNER_ORDER_DOCS"].Value = myEntity["PP_B003_INNER_ORDER_DOCS"] as DependencyObjectCollection; ;
            } else {
                e.Parameters.Add(new DataParameter("PP_B003_INNER_ORDER_DOCS", myEntity["PP_B003_INNER_ORDER_DOCS"] as DependencyObjectCollection));
            }

            if (e.Parameters.Contains("PP_B003_FORECAST_DS")) {
                e.Parameters["PP_B003_FORECAST_DS"].Value = myEntity["PP_B003_FORECAST_DS"] as DependencyObjectCollection;
            } else {
                e.Parameters.Add(new DataParameter("PP_B003_FORECAST_DS", myEntity["PP_B003_FORECAST_DS"] as DependencyObjectCollection));
            }

            if (e.Parameters.Contains("PP_B003_MOS")) {
                e.Parameters["PP_B003_MOS"].Value = myEntity["PP_B003_MOS"] as DependencyObjectCollection;
            } else {
                e.Parameters.Add(new DataParameter("PP_B003_MOS", myEntity["PP_B003_MOS"] as DependencyObjectCollection));
            }

            //20150814 modi by panzb ----------------start----------------
            //DependencyObjectCollection myGridDateSource = myEntity["PP_B003_SUGGESTION_PLANS"] as DependencyObjectCollection;
            // 添加参数及值
            if (e.Parameters.Contains("MDS_VERSION")) {
                e.Parameters["MDS_VERSION"].Value = myEntity["MDS_VERSION"];
            } else {
                e.Parameters.Add(new DataParameter("MDS_VERSION", myEntity["MDS_VERSION"]));
            }
            //20150814 modi by panzb -----------------end----------------
            
            //20150814 add by panzb ----------------start----------------
            // 添加参数及值
            if (e.Parameters.Contains("Ex_PLAN_STRATEGY_ID")) {
                e.Parameters["Ex_PLAN_STRATEGY_ID"].Value = myEntity["Ex_PLAN_STRATEGY_ID"];
            } else {
                e.Parameters.Add(new DataParameter("Ex_PLAN_STRATEGY_ID", myEntity["Ex_PLAN_STRATEGY_ID"]));
            }
            //20150814 add by panzb -----------------end----------------
            //20150831 add by wangrm for T001-150807002 start
            if (e.Parameters.Contains("LOCK")){
                e.Parameters["LOCK"].Value = myEntity["LOCK"];
            }
            else{
                e.Parameters.Add(new DataParameter("LOCK", myEntity["LOCK"]));
            }
            if (e.Parameters.Contains("FROZEN")){
                e.Parameters["FROZEN"].Value = myEntity["FROZEN"];
            }
            else{
                e.Parameters.Add(new DataParameter("FROZEN", myEntity["FROZEN"]));
            }
            //20150831 add by wangrm for T001-150807002 end
            //20151229 add by wangrm for T001-151228001 start
            if (e.Parameters.Contains("MRP_CRITICAL_ITEM_TYPE")){
                e.Parameters["MRP_CRITICAL_ITEM_TYPE"].Value = myEntity["MRP_CRITICAL_ITEM_TYPE"];
            }
            else {
                e.Parameters.Add(new DataParameter("MRP_CRITICAL_ITEM_TYPE", myEntity["MRP_CRITICAL_ITEM_TYPE"]));
            }
            //20151229 add by wangrm for T001-151228001 end
            //20170613 modi by xuyang for S001-170607003 ==begin==
            if (e.Parameters.Contains("UNGEN_DOC")) {
                e.Parameters["UNGEN_DOC"].Value = myEntity["UNGEN_DOC"];
            } else {
                e.Parameters.Add(new DataParameter("UNGEN_DOC", myEntity["UNGEN_DOC"]));
            }
            //20170613 modi by xuyang for S001-170607003 ==end==
            //20171030 add by xuyang for P001-170926001 ==begin==
            if (e.Parameters.Contains("PP_B003_STOCKING_PLAN")) {
                e.Parameters["PP_B003_STOCKING_PLAN"].Value = myEntity["PP_B003_STOCKING_PLAN"] as DependencyObjectCollection;
            } else {
                e.Parameters.Add(new DataParameter("PP_B003_STOCKING_PLAN", myEntity["PP_B003_STOCKING_PLAN"] as DependencyObjectCollection));
            }
            //20171030 add by xuyang for P001-170926001 ==end==
            //20171218 add by xuyang for P001-170930001 ==begin==
            myEntity["VERSION_TIMES"] = GenerateVersionTimes(myEntity);
            if (e.Parameters.Contains("VERSION_TIMES")) {
                e.Parameters["VERSION_TIMES"].Value = myEntity["VERSION_TIMES"];
            } else {
                e.Parameters.Add(new DataParameter("VERSION_TIMES", myEntity["VERSION_TIMES"]));
            }
            if (e.Parameters.Contains("AUTO_RELEASE_PLAN")) {
                e.Parameters["AUTO_RELEASE_PLAN"].Value = myEntity["AUTO_RELEASE_PLAN"];
            } else {
                e.Parameters.Add(new DataParameter("AUTO_RELEASE_PLAN", myEntity["AUTO_RELEASE_PLAN"]));
            }
            //20171218 add by xuyang for P001-170930001 ==end==
        }

        //20170613 add by xuyang for S001-170607003 ==begin==
        //[DataEntityChangedInterceptor(Path = "", DependencyItems = "ActiveObject.UNGEN_DOC", IsRunAtInitialized = true)]//ActiveObject.VERSION_TIMES;   //20171218 mark by xuyang for P001-170930001
        [DataEntityChangedInterceptor(Path = "", DependencyItems = "ActiveObject.UNGEN_DOC;ActiveObject.PLAN_STRATEGY_ID", IsRunAtInitialized = true)]   //20171218 add by xuyang for P001-170930001
        [Description("在字段[主需求版次]输入时，获取默认值，默认'日期＋序號'")]
        private void EntityPropertysChanged(IDataEntityBase[] activeObjs, DataChangedCallbackResponseContext context) {
            DependencyObject entity = activeObjs[0] as DependencyObject;
            //20171218 mark by xuyang for P001-170930001 ==begin==
            //Boolean ungen_Doc = entity["UNGEN_DOC"].ToBoolean();
            //if (ungen_Doc && Maths.IsEmpty(entity["VERSION_TIMES"])) {
            //    IDateTimeService dtService = this.GetService<IDateTimeService>();
            //    entity["VERSION_TIMES"] = dtService.Now.ToString("yyyyMMddHHmmss");
            //}
            //20171218 mark by xuyang for P001-170930001 ==end==
            entity["VERSION_TIMES"] = GenerateVersionTimes(entity);       //20171218 add by xuyang for P001-170930001 
        }
        //20170613 add by xuyang for S001-170607003 ==end==
        #region 20171218 add by xuyang for P001-170930001 自动跑计划
        [EventInterceptor(typeof(IEditorView), "Load")]
        private void OnEditorViewLoad(object sender, EventArgs e) {
            bool isBgSchedule = false;  //20181220 add by xuyang for T001-181210001
            ICurrentDocumentWindow doc = this.GetService<ICurrentDocumentWindow>(this.TypeKey) as ICurrentDocumentWindow;
            if (doc != null && doc.EditController != null && doc.EditController.Document != null && doc.EditController.ContextData != null) {
                DependencyObject entity = doc.EditController.Document.DataSource as DependencyObject;
                object obj = doc.EditController.ContextData.GetData(FlowTaskConstants.DOCUMENT_INTERACTIVE);
                if (obj != null && !Maths.IsEmpty(entity)) {
                    entity["IS_BG_SCHEDULE"] = true;
                    entity["UNGEN_DOC"] = true;  //20180129 add by xuyang for P001-170930001
                    entity["AUTO_RELEASE_PLAN"] = true;  //20180129 add by xuyang for P001-170930001
                    isBgSchedule = true;//20181220 add by xuyang for T001-181210001
                }
            }
            //20181220 add by xuyang for T001-181210001 ==begin==
            Control control = null;
            IFindControlService findControlSrv = GetService<IFindControlService>() as IFindControlService;
            findControlSrv.TryGet("btnMrpCheck", out control);
            if (control != null) {
                Button btn = control as Button;
                btn.Enabled = !isBgSchedule;
                btn.Click += new EventHandler(BtnMrpCheckClick);
                Control parent = FindParent(sender as EditorView);
                Control crtExcute = FindTargetCon(parent, "btnProcess");
                if (crtExcute != null) {
                    Button btnProcess = crtExcute as Button;
                    btn.Size = btnProcess.Size;
                    btn.AutoSize = true;
                    btn.Location = btnProcess.Location;
                    btn.Left = btnProcess.PointToClient(btnProcess.Location).X - 2 * btn.Width +23;
                    btn.Anchor = AnchorStyles.Right | AnchorStyles.Top;
                    btn.Visible = true;
                    btnProcess.Parent.Controls.Add(btn);
                    btn.BringToFront();
                }
            }
            //20181220 add by xuyang for T001-181210001 ==end==
            AddBatchPlanStrategyColumn(); //20190121 add by xuyang for T001-181106001 
            //20190912 add by zhaijxz for T001-190903001 ==========begin==========
            AddSuggestionPlanColumn();
            AddSuggestionPlanResourceColumn();
            AddMoDailyProductionQtyColumn();
            //20190912 add by zhaijxz for T001-190903001 ===========end==========
        }

        //20181220 add by xuyang for T001-181210001 ==begin==
        /// <summary>
        /// 查找当前顶级控件
        /// </summary>
        /// <param name="conNow"></param>
        /// <returns></returns>
        private Control FindParent(Control conNow) {
            if (conNow.Parent != null) {
                return FindParent(conNow.Parent);
            } else {
                return conNow;
            }
        }
        /// <summary>
        /// 查找目标控件
        /// </summary>
        /// <param name="conPar"></param>
        /// <returns></returns>
        private Control FindTargetCon(Control conPar, string conName) {
            Control rtCon = null;
            if (conPar != null && conPar.Controls.Count > 0) {
                System.Windows.Forms.Control.ControlCollection conColl = conPar.Controls;
                foreach (var con in conColl) {
                    if (con is Button) {
                        Button btnCon = con as Button;
                        if (btnCon.Name == conName) {
                            rtCon = btnCon;
                            break;
                        }
                    }
                }
                if (rtCon == null) {
                    foreach (Control con in conColl) {
                        rtCon = FindTargetCon(con, conName);
                        if (rtCon != null) {
                            break;
                        }
                    }
                }
            }
            return rtCon;
        }

        
        private void BtnMrpCheckClick(object sender, EventArgs e) {
            ICurrentDocumentWindow docWin = this.GetService<ICurrentDocumentWindow>(this.TypeKey) as ICurrentDocumentWindow;
            if (docWin != null) {
                SelectWindowContext context = new SelectWindowContext(null, null, docWin);
                Connection(context, ResourceServiceProvider, new ServiceCallContext(null, TypeKey));
                EditorViewBase sourceEv = docWin.EditController.EditorView as EditorViewBase;
                DocumentInteractiveContext ct = new DocumentInteractiveContext(sourceEv, sourceEv.DataSource, "PP_B003");
                IGuidBatchService browseService = context.GetService<IGuidBatchService>("PP_Q001");
                try {
                    browseService.Create(ct);//调用PP_Q001批次
                } catch (ProgramPermissionException ex) {
                    DigiwinMessageBox.ShowInfo(ex.Message);
                }
            }
        }

        /// <summary>
        /// IBrowseWindow加载时触发切片
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        [EventInterceptor(typeof(IDocumentRequestServiceEvents), "RequestParamPerparing")]
        private void PrepareData(object sender, RequestParamPerparingEventArgs e) {
            if (e.TargetTypeKey == "PP_Q001") {
                DependencyObject entity = e.ActiveObject as DependencyObject;
                if (entity != null) {
                    Hashtable PareparedData = new Hashtable();
                    PareparedData.Add("PLANT_ID", entity["PLANT_ID"]);
                    PareparedData.Add("PLANT_CODE", entity["PLANT_CODE"]);
                    PareparedData.Add("PLANT_NAME", entity["PLANT_NAME"]);
                    e.Data.SetData("PareparedData", PareparedData);
                }
            }

        }
        //20181220 add by xuyang for T001-181210001 ==end==

        /// <summary>
        /// 给计划批号默认值
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        private string GenerateVersionTimes(DependencyObject entity){
            string result = entity["VERSION_TIMES"].ToStringExtension();
            if (entity["UNGEN_DOC"].ToBoolean()  //勾选了所有未生成计划的单据
                && !entity["IS_BG_SCHEDULE"].ToBoolean() //不是背景排程调用
                && Maths.IsEmpty(entity["VERSION_TIMES"])) {  //计划批号为空
                IDateTimeService dtService = this.GetService<IDateTimeService>();
                result = dtService.Now.ToString("yyyyMMddhhmmss", CultureInfo.InvariantCulture);
            } else if (entity["PLAN_ACCORDING"].ToStringExtension() == "7"  //物料模式（主需求计划）
                && !entity["IS_BG_SCHEDULE"].ToBoolean()) {  //不是背景排程调用
                IQueryDataService qryDataSrv = this.GetServiceForThisTypeKey<IQueryDataService>() as IQueryDataService;
                result = qryDataSrv.QueryMdsVersionTimes(entity["PLANT_ID"], entity["PLAN_STRATEGY_ID"]);
            }
            return result;
        }
        #endregion

        //20190121 add by xuyang for T001-181106001  ==begin==
        /// <summary>
        /// 检查数据库是否存在ERROR_TYPE 或ERROR_MSG字段，实体存在，数据库不存在数据库中增加列
        /// </summary>
        private void AddBatchPlanStrategyColumn() {
            //判断数据库表BATCH_PLAN_STRATEGY是否存在栏位 ERROR_TYPE 或ERROR_MSG。返回TRUE：存在 ，FALSE：不存在
            IQueryDataService paramSrv = GetServiceForThisTypeKey<IQueryDataService>();
            bool isExistColumn = paramSrv.CheckExistColumn("BATCH_PLAN_STRATEGY");//20190912 modi by zhaijxz for T001-190903001 添加参数"BATCH_PLAN_STRATEGY"
            //如果数据库中不存在才检查实体并新增
            if (!isExistColumn) {
                //判断实体BATCH_PLAN_STRATEGY中存在栏位ERROR_TYPE 或ERROR_MSG
                ICreateService createService = GetService<ICreateService>("BATCH_PLAN_STRATEGY");
                DependencyObject bcRecord = createService.Create() as DependencyObject;
                bool isExistErrorType = bcRecord.GetDataEntityType().SimpleProperties.Contains("ERROR_TYPE");
                bool isExistErrorMsg = bcRecord.GetDataEntityType().SimpleProperties.Contains("ERROR_MSG");
                //满足数据库中不存在，实体中存在对应实体往数据库表中新增栏位ERROR_TYPE 和ERROR_MSG
                if (isExistErrorType && isExistErrorMsg) {
                    paramSrv.AddColumn("BATCH_PLAN_STRATEGY");//20190912 modi by zhaijxz for T001-190903001 添加参数"BATCH_PLAN_STRATEGY"
                }
            }
        }
        //20190121 add by xuyang for T001-181106001  ==end==

        #region 20190912 add by zhaijxz for T001-190903001
        /// <summary>
        /// 检查数据库是否存在SCHEDULED 或 SCHEDULED_QTY字段，实体存在，数据库不存在数据库中增加列
        /// </summary>
        private void AddSuggestionPlanColumn() {
            //判断数据库表SUGGESTION_PLAN是否存在栏位 SCHEDULED 或 SCHEDULED_QTY。返回TRUE：存在 ，FALSE：不存在
            IQueryDataService paramSrv = GetServiceForThisTypeKey<IQueryDataService>();
            bool isExistColumn = paramSrv.CheckExistColumn("SUGGESTION_PLAN");
            //如果数据库中不存在才检查实体并新增
            if (!isExistColumn) {
                //判断实体SUGGESTION_PLAN中存在栏位SCHEDULED 或 SCHEDULED_QTY
                ICreateService createService = GetService<ICreateService>("SUGGESTION_PLAN");
                DependencyObject bcRecord = createService.Create() as DependencyObject;
                bool isExistScheduled = bcRecord.GetDataEntityType().SimpleProperties.Contains("SCHEDULED");
                bool isExistScheduledQty = bcRecord.GetDataEntityType().SimpleProperties.Contains("SCHEDULED_QTY");
                //满足数据库中不存在，实体中存在对应实体往数据库表中新增栏位SCHEDULED 和SCHEDULED_QTY
                if (isExistScheduled && isExistScheduledQty) {
                    paramSrv.AddColumn("SUGGESTION_PLAN");
                }
            }
        }

        /// <summary>
        /// 检查数据库表	SUGGESTION_PLAN_RESOURCE是否存在RELEASED_QTY 或 RESCHED_QTY 或 RUDUCED_QTY 字段，实体存在，数据库不存在数据库中增加列
        /// </summary>
        private void AddSuggestionPlanResourceColumn() {
            //判断数据库表SUGGESTION_PLAN_RESOURCE是否存在栏位 RELEASED_QTY 或 RESCHED_QTY 或 RUDUCED_QTY。返回TRUE：存在 ，FALSE：不存在
            IQueryDataService paramSrv = GetServiceForThisTypeKey<IQueryDataService>();
            bool isExistColumn = paramSrv.CheckExistColumn("SUGGESTION_PLAN_RESOURCE");
            //如果数据库中不存在才检查实体并新增
            if (!isExistColumn) {
                //判断实体SUGGESTION_PLAN_RESOURCE中存在栏位RELEASED_QTY 或 RESCHED_QTY 或 RUDUCED_QTY
                ICreateService createService = GetService<ICreateService>("SUGGESTION_PLAN_RESOURCE");
                DependencyObject bcRecord = createService.Create() as DependencyObject;
                bool isExistReleasedQty = bcRecord.GetDataEntityType().SimpleProperties.Contains("RELEASED_QTY");
                bool isExistReachedQty = bcRecord.GetDataEntityType().SimpleProperties.Contains("RESCHED_QTY");
                bool isExistPuducedQty = bcRecord.GetDataEntityType().SimpleProperties.Contains("RUDUCED_QTY");
                //满足数据库中不存在，实体中存在对应实体往数据库表中新增栏位RELEASED_QTY 或 RESCHED_QTY 或 RUDUCED_QTY
                if (isExistReleasedQty && isExistReachedQty && isExistPuducedQty) {
                    paramSrv.AddColumn("SUGGESTION_PLAN_RESOURCE");
                }
            }
        }

        /// <summary>
        /// 检查数据库是否存在RESCHED_QTY 或 RUDUCED_QTY字段，实体存在，数据库不存在数据库中增加列
        /// </summary>
        private void AddMoDailyProductionQtyColumn() {
            //判断数据库表MO_DAILY_PRODUCTION_QTY是否存在栏位 RESCHED_QTY 或 RUDUCED_QTY。返回TRUE：存在 ，FALSE：不存在
            IQueryDataService paramSrv = GetServiceForThisTypeKey<IQueryDataService>();
            bool isExistColumn = paramSrv.CheckExistColumn("MO_DAILY_PRODUCTION_QTY");
            //如果数据库中不存在才检查实体并新增
            if (!isExistColumn) {
                //判断实体MO_DAILY_PRODUCTION_QTY中存在栏位RESCHED_QTY 或 RUDUCED_QTY
                ICreateService createService = GetService<ICreateService>("MO");
                DependencyObject bcRecord = createService.Create() as DependencyObject;
                DependencyObjectCollection bcRecordColl = bcRecord["MO_DAILY_PRODUCTION_QTY"] as DependencyObjectCollection;
                bool isExistReachedQty = bcRecordColl.ItemDependencyObjectType.Properties.Contains("RESCHED_QTY");
                bool isExistPuducedQty = bcRecordColl.ItemDependencyObjectType.Properties.Contains("RUDUCED_QTY");
                bool isExistOrigProdQty = bcRecordColl.ItemDependencyObjectType.Properties.Contains("ORIG_PROD_QTY");
                //满足数据库中不存在，实体中存在对应实体往数据库表中新增栏位RESCHED_QTY 和RUDUCED_QTY
                if (isExistReachedQty && isExistPuducedQty && isExistOrigProdQty) {
                    paramSrv.AddColumn("MO_DAILY_PRODUCTION_QTY");
                }
            }
        }
        #endregion
    }
}
