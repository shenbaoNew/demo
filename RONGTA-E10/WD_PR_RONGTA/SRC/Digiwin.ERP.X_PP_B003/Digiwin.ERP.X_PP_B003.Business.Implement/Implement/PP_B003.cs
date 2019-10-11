//---------------------------------------------------------------- 
//Copyright (C) 2009-2010 Digiwin Software Co.,Ltd
//http://www.digiwin.com.cn
// All rights reserved.
//<author>lewi</author>
//<createDate>2011-6-1</createDate>
//<description>产生MRP计划排程批次服务</description>
//---------------------------------------------------------------- 
//20130321 modi by xuqiang for B001-20130320060
//20131223 modi by fuwei for S001-131113001
//20140208 add by xuyf for T001-140128002
//20140311 modi by zhoujuna for T001-140305003
//20150407 modi by liyba for B31-150401007 APS_SERVER_COMMAND_QUEUE表中增加记录MDS版次
//20150610 modi by liuxp FOR T001-150525001
//20150724 add by shenbao for TB31-150724002
//20150831 modi by wangrm for T001-150807002
//20151229 modi by wangrm for T001-151228001
//20160303 modi by guojian for B31-160302009
//20160901 modi by wangrm for T001-160523001
//20161017 modi by guojian for B31-161014009
//20161212 modi by wangrm for T001-161209001
//20170113 modi by xuyang for B31-170112014  应优先满足需求日期在前的需求，因此MDS插入时按需求日期升序排列
//20170213 modi by xuyang for B001-170208026 EMPLOYEE_ID改为EMPLOYEE_CODE
//20170613 modi by xuyang for S001-170607003 新增“所有未生成计划的单据”
//20171030 modi by xuyang for P001-170926001 新增来源选项备货计划
//20171205 modi by xuyang for T001-171116002 筛除不纳入计划的品号
//20171208 modi by xuyang for B001-171208011 补齐条件
//20171218 modi by xuyang for P001-170930001 自动跑计划
//20180426 modi by xuyang for B001-180426011 CODE6应当赋值语言别
//20180529 modi by xuyang for B001-180529016 生成MDS时，需过滤掉备货数量<=0的数据
//20180711 modi by xuyang for P001-170930002 MDS新增字段PLAN_STATUS
//20181026 modi by xuyang for B001-181026004 指令队列表的CREATE_TIME写成数据库时间，方便与APSAgent回写的时间统一出处
//20190227 modi by xuyang for B31-190227012 插入MDS时管理字段要有值
//20190426 modi by xuyang for B001-190425009 生成计划时备货计划对应的MDS.OFFSET_STATUS也要赋1
//20190801 modi by xuyang for B001-190730021 虚设件要展BOM
//^_^ 20191009 modi by xuyang for 容大个案
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using Digiwin.Common;
using Digiwin.Common.Core;
using Digiwin.Common.Query2;
using Digiwin.Common.Services;
using Digiwin.Common.Torridity;
using Digiwin.ERP.Common.Utils;
using Digiwin.ERP.Common.Business;
using Digiwin.ERP.CommonManufacture.Business;
using Digiwin.ERP.X_PP_B003.Business.Implement.Properties;

namespace Digiwin.ERP.X_PP_B003.Business.Implement {
    [SingleGetCreator]
    [ServiceClass(typeof(IBatchService))]
    [ServiceClass(typeof(IBatchPreviewService))]
    public class PP_B003 : FreeBatchService<FreeBatchEventsArgs> {
        private readonly IList<string> _status = new List<string>();
        private string _excuteResult = string.Empty;

        protected override void Execute(DependencyObject task) {
            base.Execute(task);
            var t = new Task(task);
            if (!string.IsNullOrEmpty(_excuteResult) && t.ExecuteResult == "Done") {
                t.ExecuteResult = _excuteResult;
                GetService<ISaveService>("Task").Save(task);
            }
        }

        //20170213 add by xuyang for B001-170208026 ==begin==
        private string QueryEmployeeCode(object employeeId) {
            QueryNode queryNode = OOQL.Select(OOQL.CreateProperty("EMPLOYEE.EMPLOYEE_CODE", "EMPLOYEE_CODE"))
                .From("EMPLOYEE", "EMPLOYEE")
                .Where(OOQL.AuthFilter("EMPLOYEE", "EMPLOYEE") 
                & OOQL.CreateProperty("EMPLOYEE.EMPLOYEE_ID") == OOQL.CreateConstants(employeeId));
            DependencyObjectCollection queryEmployee = GetService<IQueryService>().ExecuteDependencyObject(queryNode);
            if (queryEmployee.Count > 0)
                return queryEmployee[0]["EMPLOYEE_CODE"].ToStringExtension();
            else
                return string.Empty;
        }

        /// <summary>
        /// 给计划批号默认值，后端背景排程用
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        private string GenerateVersionTimes(DataParameterCollection args) {
            string result = args["VERSION_TIMES"].Value.ToStringExtension();
            //if (Maths.IsEmpty(result)) {//20180206 mark by xuyang for B001-180205008 不必要的判断
                //if (args["UNGEN_DOC"].Value.ToBoolean()) {  //按单规划且勾选了所有未生成计划的单据  //20180206 mark by xuyang for B001-180205008
            if (args["PLAN_ACCORDING"].Value.ToStringExtension() != "7") {  //20180206 add xuyang for B001-180205008
                IDateTimeService dtService = this.GetService<IDateTimeService>();
                result = dtService.Now.ToString("yyyyMMddhhmmss", CultureInfo.InvariantCulture);
            } else if (args["PLAN_ACCORDING"].Value.ToStringExtension() == "7") { //物料模式（主需求计划）
                IQueryDataService qryDataSrv = this.GetServiceForThisTypeKey<IQueryDataService>() as IQueryDataService;
                result = qryDataSrv.QueryMdsVersionTimes(args["PLANT_ID"].Value, args["PLAN_STRATEGY_ID"].Value);
            }
            //}
            return result;
        }
        //20170213 add by xuyang for B001-170208026 ==end==

        protected override void DoProcess(FreeBatchEventsArgs args) {
            _status.Add("INITIAL");
            _status.Add("SNAPSHOT_FAIL");
            _status.Add("PLAN_FAIL");
            _status.Add("COMMIT_SUCCESS");
            _status.Add("COMMIT_FAIL");
            _status.Add("FORCE_CLOSE");
            _status.Add("LICENSE_FAIL");
            _status.Add("SERVER_COUNT_FAIL");
            _status.Add("CONNECT_FAIL");
            _status.Add("FORCE_STOP");
            //20171218 add by xuyang for P001-170930001 ==begin==
            object apsServerCommandQueueId = null;  
            object batchPlanStrategyId = null;
            if (Maths.IsEmpty(args.Context.Parameters["VERSION_TIMES"].Value) 
                && args.Context.Parameters["IS_BG_SCHEDULE"].Value.ToBoolean()) {
                args.Context.Parameters["VERSION_TIMES"].Value = GenerateVersionTimes(args.Context.Parameters);
            }
            //20171218 add by xuyang for P001-170930001 ==end==
            using (var transService = GetService<ITransactionService>()) {

                RefreshProcess(20, Resources.LABEL_000001, args.Context);

                QueryNode query = OOQL.Select("B.Owner_Org.ROid", "A.PLANT_CODE", "B.PLAN_STRATEGY_CODE", "B.MRP_LEVEL", "B.MRP_STATUS")
                    .From("PLANT", "A")
                    .LeftJoin("PLAN_STRATEGY", "B")
                    .On(OOQL.CreateProperty("B.Owner_Org.ROid") == OOQL.CreateProperty("A.PLANT_ID"))
                    .Where(OOQL.AuthFilter("PLANT", "A") & OOQL.CreateProperty("B.PLAN_STRATEGY_ID") == OOQL.CreateConstants(args.Context.Parameters["PLAN_STRATEGY_ID"].Value)
                           & OOQL.CreateProperty("B.ApproveStatus") == OOQL.CreateConstants("Y"));
                DependencyObjectCollection queryPlanStrategy = GetService<IQueryService>().ExecuteDependencyObject(query);

        
                QueryNode step2 = OOQL.Update("PLAN_STRATEGY", new[] { new SetItem("MRP_STATUS", "BATCH_START") })
                    .Where(OOQL.CreateProperty("PLAN_STRATEGY_ID") == OOQL.CreateConstants(args.Context.Parameters["PLAN_STRATEGY_ID"].Value));
               
                var insertList = new Dictionary<string, QueryProperty>();
                //insertList.Add("APS_SERVER_COMMAND_QUEUE_ID", Formulas.NewId());//20130321 modi by xuqiang for GuidDefaultValue -> NewId //20171218 mark by xuyang for P001-170930001
                //20171218 add by xuyang for P001-170930001 ==begin==
                apsServerCommandQueueId = this.GetServiceForThisTypeKey<IPrimaryKeyService>().CreateId();
                insertList.Add("APS_SERVER_COMMAND_QUEUE_ID", OOQL.CreateConstants(apsServerCommandQueueId));
                //20171218 add by xuyang for P001-170930001 ==end==
                insertList.Add("COMMAND_ID", OOQL.CreateConstants(args.Context.Parameters["STRATEGY_MODE"].Value.ToString() == "4" ? "sim_lrp" : "sim_mcp"));//20150610 modi by liuxp FOR T001-150525001
                insertList.Add("CODE1", OOQL.CreateConstants(queryPlanStrategy[0]["PLANT_CODE"]));
                insertList.Add("CODE2", OOQL.CreateConstants(queryPlanStrategy[0]["PLAN_STRATEGY_CODE"]));
                insertList.Add("CODE3", OOQL.CreateConstants("MRP"));
                //20170213 modi by xuyang for B001-170208026 ==begin==
                //insertList.Add("CODE4", OOQL.CreateConstants(args.Context.Parameters["EMPLOYEE_ID"].Value));
                insertList.Add("CODE4", OOQL.CreateConstants(QueryEmployeeCode(args.Context.Parameters["EMPLOYEE_ID"].Value)));
                //20170213 modi by xuyang for B001-170208026 ==end==
                insertList.Add("CODE5", OOQL.CreateConstants(string.Empty));
                //insertList.Add("CODE6", OOQL.CreateConstants(string.Empty);  //20180426 mark by xuyang for B001-180426011
                insertList.Add("CODE6", OOQL.CreateConstants(this.GetService<ILogOnService>().CurrentUser.LanguageName)); //20180426 add by xuyang for B001-180426011
                insertList.Add("CODE7", OOQL.CreateConstants(args.Context.Parameters["LOCK"].Value));//20150831 modi by wangrm for T001-150807002 
                insertList.Add("CODE8", OOQL.CreateConstants(args.Context.Parameters["FROZEN"].Value));//20150831 modi by wangrm for T001-150807002 
                insertList.Add("CODE9", OOQL.CreateConstants(args.Context.Parameters["VERSION_TIMES"].Value));  //20150407 modi by liyba for B31-150401007
                insertList.Add("CODE10", OOQL.CreateConstants(string.Empty));
                //insertList.Add("CREATE_TIME", OOQL.CreateConstants(DateTime.Now.ToStandardDateString())); //20181026 mark by xuyang for B001-181026004
                insertList.Add("CREATE_TIME", Formulas.GetDate());//20181026 add by xuyang for B001-181026004
                insertList.Add("AUTO_RELEASE_PLAN", OOQL.CreateConstants(args.Context.Parameters["AUTO_RELEASE_PLAN"].Value.ToBoolean(), GeneralDBType.Boolean));      //20171218 add by xuyang for P001-170930001
                QueryNode step3 = OOQL.Insert("APS_SERVER_COMMAND_QUEUE", insertList.Keys.ToArray()).Values(insertList.Values.ToArray());

                RefreshProcess(60, Resources.LABEL_000002, args.Context);

                if (queryPlanStrategy.Count == 0) {
                    _excuteResult = Resources.LABEL_000003;
                } else if (queryPlanStrategy[0]["Owner_Org.ROid"].ToString() != args.Context.Parameters["PLANT_ID"].Value.ToString()) {
                    _excuteResult = Resources.LABEL_000004;
                } else if (queryPlanStrategy[0]["MRP_LEVEL"].ToString() == "False") {
                    _excuteResult = Resources.LABEL_000005;
                } else if (!_status.Contains(queryPlanStrategy[0]["MRP_STATUS"].ToString())) {
                    _excuteResult = Resources.LABEL_000006;
                } else {
                    RefreshProcess(80, Resources.LABEL_000007, args.Context);
                    GetService<IQueryService>().ExecuteNoQueryWithManageProperties(step2);
                    GetService<IQueryService>().ExecuteNoQueryWithManageProperties(step3);
                }

                object planStrategyId = GetPLAN_STRATEGY_ID(args);
                DependencyObjectCollection planStrategyList = GetPLAN_STRATEGYList(args.Context.Parameters["PLAN_STRATEGY_ID"].Value);
                if (planStrategyId != null) {
                    var updateList = new List<SetItem>();
                    updateList.Add(new SetItem(OOQL.CreateProperty("PLAN_STRATEGY_CODE"), OOQL.CreateConstants(planStrategyList[0]["PLAN_STRATEGY_CODE"])));
                    updateList.Add(new SetItem(OOQL.CreateProperty("PLAN_STRATEGY_NAME"), OOQL.CreateConstants(planStrategyList[0]["PLAN_STRATEGY_NAME"])));
                    updateList.Add(new SetItem(OOQL.CreateProperty("STRATEGY_MODE"), OOQL.CreateConstants(planStrategyList[0]["STRATEGY_MODE"])));
                    updateList.Add(new SetItem(OOQL.CreateProperty("MDS_LEVEL"), OOQL.CreateConstants(planStrategyList[0]["MDS_LEVEL"])));
                    updateList.Add(new SetItem(OOQL.CreateProperty("MPS_LEVEL"), OOQL.CreateConstants(planStrategyList[0]["MPS_LEVEL"])));
                    updateList.Add(new SetItem(OOQL.CreateProperty("MRP_LEVEL"), OOQL.CreateConstants(planStrategyList[0]["MRP_LEVEL"])));
                    updateList.Add(new SetItem(OOQL.CreateProperty("MDS_OFFSET_RULE"), OOQL.CreateConstants(planStrategyList[0]["MDS_OFFSET_RULE"])));
                    updateList.Add(new SetItem(OOQL.CreateProperty("MDS_DEMAND_TIME_FENCE"), OOQL.CreateConstants(planStrategyList[0]["MDS_DEMAND_TIME_FENCE"])));
                    updateList.Add(new SetItem(OOQL.CreateProperty("MDS_DEMAND_PRIORITY_ORDER"), OOQL.CreateConstants(planStrategyList[0]["MDS_DEMAND_PRIORITY_ORDER"])));
                    updateList.Add(new SetItem(OOQL.CreateProperty("MDS_DEMAND_TYPE"), OOQL.CreateConstants(planStrategyList[0]["MDS_DEMAND_TYPE"])));
                    updateList.Add(new SetItem(OOQL.CreateProperty("MPS_DEMAND_SOURCE"), OOQL.CreateConstants(planStrategyList[0]["MPS_DEMAND_SOURCE"])));
                    updateList.Add(new SetItem(OOQL.CreateProperty("MPS_FORECAST_ALLOCATION"), OOQL.CreateConstants(planStrategyList[0]["MPS_FORECAST_ALLOCATION"])));
                    updateList.Add(new SetItem(OOQL.CreateProperty("MPS_MO_ALLOCATION"), OOQL.CreateConstants(planStrategyList[0]["MPS_MO_ALLOCATION"])));
                    updateList.Add(new SetItem(OOQL.CreateProperty("MPS_SCHEDULING_STRATEGY"), OOQL.CreateConstants(planStrategyList[0]["MPS_SCHEDULING_STRATEGY"])));
                    updateList.Add(new SetItem(OOQL.CreateProperty("MPS_LEAD_TIME"), OOQL.CreateConstants(planStrategyList[0]["MPS_LEAD_TIME"])));
                    updateList.Add(new SetItem(OOQL.CreateProperty("MPS_TRANSFER_LOT_FLAG"), OOQL.CreateConstants(planStrategyList[0]["MPS_TRANSFER_LOT_FLAG"])));
                    updateList.Add(new SetItem(OOQL.CreateProperty("MPS_REPLACE_FLAG"), OOQL.CreateConstants(planStrategyList[0]["MPS_REPLACE_FLAG"])));
                    updateList.Add(new SetItem(OOQL.CreateProperty("MPS_ALTERNATIVE_FLAG"), OOQL.CreateConstants(planStrategyList[0]["MPS_ALTERNATIVE_FLAG"])));
                    updateList.Add(new SetItem(OOQL.CreateProperty("MPS_REQUIREMENT_CALCULATION"), OOQL.CreateConstants(planStrategyList[0]["MPS_REQUIREMENT_CALCULATION"])));
                    updateList.Add(new SetItem(OOQL.CreateProperty("MRP_TIME_BUCKET"), OOQL.CreateConstants(args.Context.Parameters["MRP_TIME_BUCKET"].Value)));
                    updateList.Add(new SetItem(OOQL.CreateProperty("MRP_STATUS"), OOQL.CreateConstants("BATCH_START")));//20150610 modi by liuxp FOR T001-150525001 原：args.Context.Parameters["MRP_STATUS"].Value)));
                    updateList.Add(new SetItem(OOQL.CreateProperty("MPS_STATUS"), OOQL.CreateConstants(args.Context.Parameters["MPS_STATUS"].Value)));

                    updateList.Add(new SetItem(OOQL.CreateProperty("MRP_TIME_BUCKET_ID"), OOQL.CreateConstants(args.Context.Parameters["MRP_TIME_BUCKET_ID"].Value)));
                    updateList.Add(new SetItem(OOQL.CreateProperty("MRP_DEMAND_SOURCE"), OOQL.CreateConstants(args.Context.Parameters["MRP_DEMAND_SOURCE"].Value)));
                    updateList.Add(new SetItem(OOQL.CreateProperty("MRP_FORECAST_ALLOCATION"), OOQL.CreateConstants(args.Context.Parameters["MRP_FORECAST_ALLOCATION"].Value)));
                    updateList.Add(new SetItem(OOQL.CreateProperty("MRP_PROCUREMENT_PLAN"), OOQL.CreateConstants(args.Context.Parameters["MRP_PROCUREMENT_PLAN"].Value)));
                    updateList.Add(new SetItem(OOQL.CreateProperty("MRP_TRANSFER_LOT_FLAG"), OOQL.CreateConstants(args.Context.Parameters["MRP_TRANSFER_LOT_FLAG"].Value)));
                    updateList.Add(new SetItem(OOQL.CreateProperty("MRP_ATTRITION_RATE_FLAG"), OOQL.CreateConstants(args.Context.Parameters["MRP_ATTRITION_RATE_FLAG"].Value)));
                    updateList.Add(new SetItem(OOQL.CreateProperty("MRP_REPLACE_FLAG"), OOQL.CreateConstants(args.Context.Parameters["MRP_REPLACE_FLAG"].Value)));
                    updateList.Add(new SetItem(OOQL.CreateProperty("MRP_ALTERNATIVE_FLAG"), OOQL.CreateConstants(args.Context.Parameters["MRP_ALTERNATIVE_FLAG"].Value)));

                    updateList.Add(new SetItem(OOQL.CreateProperty("MRP_PRODUCTION_PLAN_FLAG"), OOQL.CreateConstants(args.Context.Parameters["MRP_PRODUCTION_PLAN_FLAG"].Value)));
                    updateList.Add(new SetItem(OOQL.CreateProperty("MRP_REQUIREMENT_CALCULATION"), OOQL.CreateConstants(args.Context.Parameters["MRP_REQUIREMENT_CALCULATION"].Value)));


                    //updateList.Add(new SetItem(OOQL.CreateProperty("REMARK"), OOQL.CreateConstants(args.Context.Parameters["REMARK"])));
                    updateList.Add(new SetItem(OOQL.CreateProperty("Owner_Org.ROid"), OOQL.CreateConstants(args.Context.Parameters["PLANT_ID"].Value)));
                    updateList.Add(new SetItem(OOQL.CreateProperty("Owner_Emp"), OOQL.CreateConstants(args.Context.Parameters["EMPLOYEE_ID"].Value)));

                    //20131223 add by fuwei --------Begin
                    updateList.Add(new SetItem(OOQL.CreateProperty("CONSIDERED_LOCK_STOCK"), OOQL.CreateConstants(planStrategyList[0]["CONSIDERED_LOCK_STOCK"])));
                    updateList.Add(new SetItem(OOQL.CreateProperty("MDS_SALES_ORDER_FLAG"), OOQL.CreateConstants(planStrategyList[0]["MDS_SALES_ORDER_FLAG"])));
                    updateList.Add(new SetItem(OOQL.CreateProperty("MDS_FORECAST_FLAG"), OOQL.CreateConstants(planStrategyList[0]["MDS_FORECAST_FLAG"])));
                    updateList.Add(new SetItem(OOQL.CreateProperty("MDS_TRANSFER_FLAG"), OOQL.CreateConstants(planStrategyList[0]["MDS_TRANSFER_FLAG"])));
                    updateList.Add(new SetItem(OOQL.CreateProperty("MDS_INNER_ORDER_FLAG"), OOQL.CreateConstants(planStrategyList[0]["MDS_INNER_ORDER_FLAG"])));
                    updateList.Add(new SetItem(OOQL.CreateProperty("MDS_STOCKING_PLAN_FLAG"), OOQL.CreateConstants(planStrategyList[0]["MDS_STOCKING_PLAN_FLAG"])));  //20171030 add by xuyang for P001-170926001
                    updateList.Add(new SetItem(OOQL.CreateProperty("MPS_SAFT_STOCK_FLAG"), OOQL.CreateConstants(planStrategyList[0]["MPS_SAFT_STOCK_FLAG"])));
                    updateList.Add(new SetItem(OOQL.CreateProperty("MPS_SAFT_STOCK_PRIORITY"), OOQL.CreateConstants(planStrategyList[0]["MPS_SAFT_STOCK_PRIORITY"])));//20160901 add by wangrm for T001-160523001//20161111 modi by wangrm MPS_SAFT_STOCK_FLAG->MPS_SAFT_STOCK_PRIORITY
                    updateList.Add(new SetItem(OOQL.CreateProperty("MRP_SAFT_STOCK_FLAG"), OOQL.CreateConstants(args.Context.Parameters["MRP_SAFT_STOCK_FLAG"].Value)));
                    updateList.Add(new SetItem(OOQL.CreateProperty("MRP_SAFT_STOCK_PRIORITY"), OOQL.CreateConstants(args.Context.Parameters["MRP_SAFT_STOCK_PRIORITY"].Value)));//20160901 add by wangrm for T001-160523001//20161111 modi by wangrm MRP_SAFT_STOCK_FLAG->MRP_SAFT_STOCK_PRIORITY
                    //20131223 add by fuwei --------End
                    updateList.Add(new SetItem(OOQL.CreateProperty("MRP_KEEP_ISSUED_PLANS"), OOQL.CreateConstants(args.Context.Parameters["MRP_KEEP_ISSUED_PLANS"].Value)));//20140208 add by xuyf for T001-140128002
                    updateList.Add(new SetItem(OOQL.CreateProperty("MRP_ISSUED_ZERO_PLANS"), OOQL.CreateConstants(args.Context.Parameters["MRP_ISSUED_ZERO_PLANS"].Value)));//20140311 add by zhoujuna for T001-140305003
                    //20150610 add by liuxp FOR T001-150525001 ----------------start-------------
                    updateList.Add(new SetItem(OOQL.CreateProperty("MRP_SCHEDULING_STRATEGY"), OOQL.CreateConstants(args.Context.Parameters["MRP_SCHEDULING_STRATEGY"].Value)));
                    updateList.Add(new SetItem(OOQL.CreateProperty("PLAN_TYPE"), OOQL.CreateConstants(args.Context.Parameters["PLAN_TYPE"].Value)));
                    updateList.Add(new SetItem(OOQL.CreateProperty("MRP_LOCK_RANGE"), OOQL.CreateConstants(args.Context.Parameters["MRP_LOCK_RANGE"].Value)));
                    updateList.Add(new SetItem(OOQL.CreateProperty("MPS_LOCK_RANGE"), OOQL.CreateConstants(planStrategyList[0]["MPS_LOCK_RANGE"])));
                    updateList.Add(new SetItem(OOQL.CreateProperty("MDS_VERSION"), OOQL.CreateConstants(args.Context.Parameters["VERSION_TIMES"].Value)));
                    updateList.Add(new SetItem(OOQL.CreateProperty("LOCK"), OOQL.CreateConstants(args.Context.Parameters["LOCK"].Value))); //20150831 added by wangrm for T001-150807002
                    updateList.Add(new SetItem(OOQL.CreateProperty("FROZEN"), OOQL.CreateConstants(args.Context.Parameters["FROZEN"].Value))); //20150831 added by wangrm for T001-150807002
                    //20151229 add by wangrm for T001-151228001 start
                    updateList.Add(new SetItem(OOQL.CreateProperty("MRP_DEMAND_PRIORITY_ORDER"), OOQL.CreateConstants(planStrategyList[0]["MRP_DEMAND_PRIORITY_ORDER"])));
                    updateList.Add(new SetItem(OOQL.CreateProperty("MRP_MANUFACTURING_PART"), OOQL.CreateConstants(planStrategyList[0]["MRP_MANUFACTURING_PART"])));
                    updateList.Add(new SetItem(OOQL.CreateProperty("MRP_PROCESSING_PART"), OOQL.CreateConstants(planStrategyList[0]["MRP_PROCESSING_PART"])));
                    updateList.Add(new SetItem(OOQL.CreateProperty("MRP_PURCHASING_PART"), OOQL.CreateConstants(planStrategyList[0]["MRP_PURCHASING_PART"])));
                    updateList.Add(new SetItem(OOQL.CreateProperty("MRP_INNER_PURCHASING_PART"), OOQL.CreateConstants(planStrategyList[0]["MRP_INNER_PURCHASING_PART"])));
                    updateList.Add(new SetItem(OOQL.CreateProperty("MRP_TRANSFER_PART"), OOQL.CreateConstants(planStrategyList[0]["MRP_TRANSFER_PART"])));
                    updateList.Add(new SetItem(OOQL.CreateProperty("MRP_ADDED_DIFFERENCE"), OOQL.CreateConstants(planStrategyList[0]["MRP_ADDED_DIFFERENCE"])));
                    updateList.Add(new SetItem(OOQL.CreateProperty("MRP_CRITICAL_ITEM_TYPE"), OOQL.CreateConstants(args.Context.Parameters["MRP_CRITICAL_ITEM_TYPE"].Value)));
                    //20151229 add by wangrm for T001-151228001 end
                    updateList.Add(new SetItem(OOQL.CreateProperty("INTERNAL_ITEM_PLAN"), OOQL.CreateConstants(planStrategyList[0]["INTERNAL_ITEM_PLAN"])));//20161212 add by wangrm for T001-161209001
                    object fromValue = string.Empty;//20150814 modi by panzb args.Context.Parameters["VERSION_TIMES"].Value;  -> string.Empty
                    if (args.Context.Parameters["PLAN_ACCORDING"].Value.ToStringExtension().Equals("6"))//20150820 modi by wangrm for B001-150820001 args.Context.Parameters["PLAN_ACCORDING"].ToStringExtension().Equals("4")   ->args.Context.Parameters["PLAN_ACCORDING"].Value.ToStringExtension().Equals("6")
                    {
                        fromValue = args.Context.Parameters["MDS_VERSION"].Value; //20150820 modi by wangrm for B001-150820001
                        updateList.Add(new SetItem(OOQL.CreateProperty("FROM_MDS_VERSION"), OOQL.CreateConstants(fromValue)));
                    }
                    object rootValue = args.Context.Parameters["VERSION_TIMES"].Value;
                    if (args.Context.Parameters["PLAN_ACCORDING"].Value.ToStringExtension().Equals("6"))//20150820 modi by wangrm for B001-150820001 args.Context.Parameters["PLAN_ACCORDING"].ToStringExtension().Equals("4")   ->args.Context.Parameters["PLAN_ACCORDING"].Value.ToStringExtension().Equals("6")
                    {
                        QueryNode node = OOQL.Select(OOQL.CreateProperty("ROOT_MDS_VERSION")).From("BATCH_PLAN_STRATEGY","BATCH_PLAN_STRATEGY").Where(OOQL.AuthFilter("BATCH_PLAN_STRATEGY","BATCH_PLAN_STRATEGY") & OOQL.CreateProperty("MDS_VERSION") == OOQL.CreateConstants(args.Context.Parameters["VERSION_TIMES"].Value));//20150820 modi by wangrm for B001-150820001
                        object result = GetService<IQueryService>().ExecuteScalar(node);
                        if (result != null)
                            rootValue = result;
                    }
                    updateList.Add(new SetItem(OOQL.CreateProperty("ROOT_MDS_VERSION"), OOQL.CreateConstants(rootValue)));
                    updateList.Add(new SetItem(OOQL.CreateProperty("DELETE_FLAG"), OOQL.CreateConstants(0)));
                    //20150610 add by liuxp FOR T001-150525001 -----------------end-------------
                    updateList.Add(new SetItem(OOQL.CreateProperty("AUTO_RELEASE_PLAN"), OOQL.CreateConstants(args.Context.Parameters["AUTO_RELEASE_PLAN"].Value.ToBoolean(), GeneralDBType.Boolean)));      //20171218 add by xuyang for P001-170930001
                    updateList.Add(new SetItem(OOQL.CreateProperty("KEEP_EXIST_DEMAND_BALANCE"), OOQL.CreateConstants(planStrategyList[0]["SATISFY_LOWER_LEVEL"],GeneralDBType.Boolean)));      //20180711 add by xuyang for P001-170930002
                    updateList.Add(new SetItem(OOQL.CreateProperty("SATISFY_LOWER_LEVEL"), OOQL.CreateConstants(planStrategyList[0]["SATISFY_LOWER_LEVEL"],GeneralDBType.Boolean)));      //20180711 add by xuyang for P001-170930002
                    GetService<IQueryService>().ExecuteNoQueryWithManageProperties(OOQL.Update("BATCH_PLAN_STRATEGY", updateList.ToArray()).Where(OOQL.CreateProperty("BATCH_PLAN_STRATEGY_ID") == OOQL.CreateConstants(planStrategyId)));//20150610 modi by liuxp FOR T001-150525001 原：PLAN_STRATEGY_ID
                    batchPlanStrategyId = planStrategyId;  //20171218 add by xuyang for P001-170930001
                } else {
                    batchPlanStrategyId = this.GetServiceForThisTypeKey<IPrimaryKeyService>().CreateId();  //20171218 add by xuyang for P001-170930001
                    var insert = new Dictionary<string, QueryProperty>();
                    insert.Add("PLAN_STRATEGY_ID", OOQL.CreateConstants(args.Context.Parameters["PLAN_STRATEGY_ID"].Value));
                    //insert.Add("BATCH_PLAN_STRATEGY_ID", Formulas.NewId());//20150610 add by liuxp FOR T001-150525001  //20171218 mark by xuyang for P001-170930001
                    insert.Add("BATCH_PLAN_STRATEGY_ID", OOQL.CreateConstants(batchPlanStrategyId));//20171218 add by xuyang for P001-170930001
                    insert.Add("PLAN_STRATEGY_CODE", OOQL.CreateConstants(planStrategyList[0]["PLAN_STRATEGY_CODE"]));
                    insert.Add("PLAN_STRATEGY_NAME", OOQL.CreateConstants(planStrategyList[0]["PLAN_STRATEGY_NAME"]));
                    insert.Add("STRATEGY_MODE", OOQL.CreateConstants(planStrategyList[0]["STRATEGY_MODE"]));
                    insert.Add("MDS_LEVEL", OOQL.CreateConstants(planStrategyList[0]["MDS_LEVEL"]));
                    insert.Add("MPS_LEVEL", OOQL.CreateConstants(planStrategyList[0]["MPS_LEVEL"]));
                    insert.Add("MRP_LEVEL", OOQL.CreateConstants(planStrategyList[0]["MRP_LEVEL"]));
                    insert.Add("MDS_OFFSET_RULE", OOQL.CreateConstants(planStrategyList[0]["MDS_OFFSET_RULE"]));
                    insert.Add("MDS_DEMAND_TIME_FENCE", OOQL.CreateConstants(planStrategyList[0]["MDS_DEMAND_TIME_FENCE"]));
                    insert.Add("MDS_DEMAND_PRIORITY_ORDER", OOQL.CreateConstants(planStrategyList[0]["MDS_DEMAND_PRIORITY_ORDER"]));
                    insert.Add("MDS_DEMAND_TYPE", OOQL.CreateConstants(planStrategyList[0]["MDS_DEMAND_TYPE"]));
                    insert.Add("MPS_DEMAND_SOURCE", OOQL.CreateConstants(planStrategyList[0]["MPS_DEMAND_SOURCE"]));
                    insert.Add("MPS_FORECAST_ALLOCATION", OOQL.CreateConstants(planStrategyList[0]["MPS_FORECAST_ALLOCATION"]));
                    insert.Add("MPS_MO_ALLOCATION", OOQL.CreateConstants(planStrategyList[0]["MPS_MO_ALLOCATION"]));
                    insert.Add("MPS_SCHEDULING_STRATEGY", OOQL.CreateConstants(planStrategyList[0]["MPS_SCHEDULING_STRATEGY"]));
                    insert.Add("MPS_LEAD_TIME", OOQL.CreateConstants(planStrategyList[0]["MPS_LEAD_TIME"]));
                    insert.Add("MPS_TRANSFER_LOT_FLAG", OOQL.CreateConstants(planStrategyList[0]["MPS_TRANSFER_LOT_FLAG"]));
                    insert.Add("MPS_REPLACE_FLAG", OOQL.CreateConstants(planStrategyList[0]["MPS_REPLACE_FLAG"]));
                    insert.Add("MPS_ALTERNATIVE_FLAG", OOQL.CreateConstants(planStrategyList[0]["MPS_ALTERNATIVE_FLAG"]));
                    insert.Add("MPS_REQUIREMENT_CALCULATION", OOQL.CreateConstants(planStrategyList[0]["MPS_REQUIREMENT_CALCULATION"]));
                    insert.Add("MRP_TIME_BUCKET", OOQL.CreateConstants(args.Context.Parameters["MRP_TIME_BUCKET"].Value));
                    insert.Add("MPS_STATUS", OOQL.CreateConstants(args.Context.Parameters["MPS_STATUS"].Value));
                    insert.Add("MRP_STATUS", OOQL.CreateConstants("BATCH_START"));//20150610 modi by liuxp FOR T001-150525001 //args.Context.Parameters["MRP_STATUS"].Value));
                    insert.Add("MRP_TIME_BUCKET_ID", OOQL.CreateConstants(args.Context.Parameters["MRP_TIME_BUCKET_ID"].Value));
                    insert.Add("MRP_DEMAND_SOURCE", OOQL.CreateConstants(args.Context.Parameters["MRP_DEMAND_SOURCE"].Value));
                    insert.Add("MRP_FORECAST_ALLOCATION", OOQL.CreateConstants(args.Context.Parameters["MRP_FORECAST_ALLOCATION"].Value));
                    insert.Add("MRP_PROCUREMENT_PLAN", OOQL.CreateConstants(args.Context.Parameters["MRP_PROCUREMENT_PLAN"].Value));
                    insert.Add("MRP_TRANSFER_LOT_FLAG", OOQL.CreateConstants(args.Context.Parameters["MRP_TRANSFER_LOT_FLAG"].Value));
                    insert.Add("MRP_ATTRITION_RATE_FLAG", OOQL.CreateConstants(args.Context.Parameters["MRP_ATTRITION_RATE_FLAG"].Value));
                    insert.Add("MRP_REPLACE_FLAG", OOQL.CreateConstants(args.Context.Parameters["MRP_REPLACE_FLAG"].Value));
                    insert.Add("MRP_ALTERNATIVE_FLAG", OOQL.CreateConstants(args.Context.Parameters["MRP_ALTERNATIVE_FLAG"].Value));
                    insert.Add("MRP_PRODUCTION_PLAN_FLAG", OOQL.CreateConstants(args.Context.Parameters["MRP_PRODUCTION_PLAN_FLAG"].Value));
                    insert.Add("MRP_REQUIREMENT_CALCULATION", OOQL.CreateConstants(args.Context.Parameters["MRP_REQUIREMENT_CALCULATION"].Value));
                    //insert.Add("REMARK", OOQL.CreateConstants(args.Context.Parameters["REMARK"]));
                    insert.Add("Owner_Org_ROid", OOQL.CreateConstants(args.Context.Parameters["PLANT_ID"].Value));
                    insert.Add("Owner_Org_RTK", OOQL.CreateConstants("PLANT"));
                    insert.Add("Owner_Emp", OOQL.CreateConstants(args.Context.Parameters["EMPLOYEE_ID"].Value));

                    //20131223 add by fuwei --------Begin
                    insert.Add("CONSIDERED_LOCK_STOCK", OOQL.CreateConstants(planStrategyList[0]["CONSIDERED_LOCK_STOCK"]));
                    insert.Add("MDS_SALES_ORDER_FLAG", OOQL.CreateConstants(planStrategyList[0]["MDS_SALES_ORDER_FLAG"]));
                    insert.Add("MDS_FORECAST_FLAG", OOQL.CreateConstants(planStrategyList[0]["MDS_FORECAST_FLAG"]));
                    insert.Add("MDS_TRANSFER_FLAG", OOQL.CreateConstants(planStrategyList[0]["MDS_TRANSFER_FLAG"]));
                    insert.Add("MDS_INNER_ORDER_FLAG", OOQL.CreateConstants(planStrategyList[0]["MDS_INNER_ORDER_FLAG"]));
                    insert.Add("MDS_STOCKING_PLAN_FLAG", OOQL.CreateConstants(planStrategyList[0]["MDS_STOCKING_PLAN_FLAG"])); //20171030 add by xuyang for P001-170926001
                    insert.Add("MPS_SAFT_STOCK_FLAG", OOQL.CreateConstants(planStrategyList[0]["MPS_SAFT_STOCK_FLAG"]));
                    insert.Add("MPS_SAFT_STOCK_PRIORITY", OOQL.CreateConstants(planStrategyList[0]["MPS_SAFT_STOCK_PRIORITY"]));//20160901 add by wangrm for T001-160523001
                    insert.Add("MRP_SAFT_STOCK_FLAG", OOQL.CreateConstants(args.Context.Parameters["MRP_SAFT_STOCK_FLAG"].Value));
                    insert.Add("MRP_SAFT_STOCK_PRIORITY", OOQL.CreateConstants(args.Context.Parameters["MRP_SAFT_STOCK_PRIORITY"].Value));//20160901 add by wangrm for T001-160523001
                    //20131223 add by fuwei --------End
                    insert.Add("MRP_KEEP_ISSUED_PLANS", OOQL.CreateConstants(args.Context.Parameters["MRP_KEEP_ISSUED_PLANS"].Value));//20140208 add by xuyf for T001-140128002
                    insert.Add("MRP_ISSUED_ZERO_PLANS", OOQL.CreateConstants(args.Context.Parameters["MRP_ISSUED_ZERO_PLANS"].Value));//20140311 add by zhoujuna for T001-140305003
                    //20150610 add by liuxp FOR T001-150525001 ----------------start-------------
                    insert.Add("MRP_SCHEDULING_STRATEGY", OOQL.CreateConstants(args.Context.Parameters["MRP_SCHEDULING_STRATEGY"].Value));
                    insert.Add("PLAN_TYPE", OOQL.CreateConstants(args.Context.Parameters["PLAN_TYPE"].Value));
                    insert.Add("MRP_LOCK_RANGE", OOQL.CreateConstants(args.Context.Parameters["MRP_LOCK_RANGE"].Value));
                    insert.Add("MPS_LOCK_RANGE", OOQL.CreateConstants(planStrategyList[0]["MPS_LOCK_RANGE"]));
                    insert.Add("MDS_VERSION", OOQL.CreateConstants(args.Context.Parameters["VERSION_TIMES"].Value));
                    insert.Add("LOCK", OOQL.CreateConstants(args.Context.Parameters["LOCK"].Value)); //20150831 added by wangrm for T001-150807002
                    insert.Add("FROZEN", OOQL.CreateConstants(args.Context.Parameters["FROZEN"].Value)); //20150831 added by wangrm for T001-150807002
                    //20151229 add by wangrm for T001-151228001 start
                    insert.Add("MRP_DEMAND_PRIORITY_ORDER", OOQL.CreateConstants(planStrategyList[0]["MRP_DEMAND_PRIORITY_ORDER"]));
                    insert.Add("MRP_MANUFACTURING_PART", OOQL.CreateConstants(planStrategyList[0]["MRP_MANUFACTURING_PART"]));
                    insert.Add("MRP_PROCESSING_PART", OOQL.CreateConstants(planStrategyList[0]["MRP_PROCESSING_PART"]));
                    insert.Add("MRP_PURCHASING_PART", OOQL.CreateConstants(planStrategyList[0]["MRP_PURCHASING_PART"]));
                    insert.Add("MRP_INNER_PURCHASING_PART", OOQL.CreateConstants(planStrategyList[0]["MRP_INNER_PURCHASING_PART"]));
                    insert.Add("MRP_TRANSFER_PART", OOQL.CreateConstants(planStrategyList[0]["MRP_TRANSFER_PART"]));
                    insert.Add("MRP_ADDED_DIFFERENCE", OOQL.CreateConstants(planStrategyList[0]["MRP_ADDED_DIFFERENCE"]));
                    insert.Add("MRP_CRITICAL_ITEM_TYPE", OOQL.CreateConstants(args.Context.Parameters["MRP_CRITICAL_ITEM_TYPE"].Value));
                    //20151229 add by wangrm for T001-151228001 end
                    insert.Add("INTERNAL_ITEM_PLAN", OOQL.CreateConstants(planStrategyList[0]["INTERNAL_ITEM_PLAN"]));
                    object fromValue = string.Empty;//20150814 modi by panzb args.Context.Parameters["VERSION_TIMES"].Value; -> string.Empty
                    if (args.Context.Parameters["PLAN_ACCORDING"].Value.ToStringExtension().Equals("6"))//20150820 modi by wangrm for B001-150820001 args.Context.Parameters["PLAN_ACCORDING"].ToStringExtension().Equals("4")   ->args.Context.Parameters["PLAN_ACCORDING"].Value.ToStringExtension().Equals("6")
                    {
                        fromValue = args.Context.Parameters["MDS_VERSION"].Value;
                        insert.Add("FROM_MDS_VERSION", OOQL.CreateConstants(fromValue));
                    }
                    object rootValue = args.Context.Parameters["VERSION_TIMES"].Value;
                    if (args.Context.Parameters["PLAN_ACCORDING"].Value.ToStringExtension().Equals("6")) //20150820 modi by wangrm for B001-150820001 args.Context.Parameters["PLAN_ACCORDING"].ToStringExtension().Equals("4")   ->args.Context.Parameters["PLAN_ACCORDING"].Value.ToStringExtension().Equals("6")
                    {
                        QueryNode node = OOQL.Select(OOQL.CreateProperty("ROOT_MDS_VERSION")).From("BATCH_PLAN_STRATEGY","BATCH_PLAN_STRATEGY").Where(OOQL.AuthFilter("BATCH_PLAN_STRATEGY","BATCH_PLAN_STRATEGY") & OOQL.CreateProperty("MDS_VERSION") == OOQL.CreateConstants(args.Context.Parameters["VERSION_TIMES"].Value));//20150820 modi by wangrm for B001-150820001
                        object result = GetService<IQueryService>().ExecuteScalar(node);
                        if (result != null)
                            rootValue = result;
                    }
                    insert.Add("ROOT_MDS_VERSION", OOQL.CreateConstants(rootValue));
                    insert.Add("DELETE_FLAG", OOQL.CreateConstants(0));
                    //20150610 add by liuxp FOR T001-150525001 -----------------end-------------
                    insert.Add("AUTO_RELEASE_PLAN", OOQL.CreateConstants(args.Context.Parameters["AUTO_RELEASE_PLAN"].Value.ToBoolean(),GeneralDBType.Boolean));      //20171218 add by xuyang for P001-170930001
                    insert.Add("KEEP_EXIST_DEMAND_BALANCE", OOQL.CreateConstants(planStrategyList[0]["KEEP_EXIST_DEMAND_BALANCE"], GeneralDBType.Boolean));      //20180711 add by xuyang for P001-170930002
                    insert.Add("SATISFY_LOWER_LEVEL", OOQL.CreateConstants(planStrategyList[0]["SATISFY_LOWER_LEVEL"], GeneralDBType.Boolean));      //20180711 add by xuyang for P001-170930002
                    GetService<IQueryService>().ExecuteNoQueryWithManageProperties(OOQL.Insert("BATCH_PLAN_STRATEGY", insert.Keys.ToArray()).Values(insert.Values.ToArray()));
                }
                //20150610 add by liuxp FOR T001-150525001 ----------------start-------------
                RefreshProcess(70, _excuteResult == string.Empty ? Resources.LABEL_000008 : _excuteResult, args.Context);
                InsertMDSDData(args);
                //20150610 add by liuxp FOR T001-150525001 -----------------end-------------
                RefreshProcess(100, _excuteResult == string.Empty ? Resources.LABEL_000008 : _excuteResult, args.Context);
                
                transService.Complete();
            }
            //20171218 add by xuyang for P001-170930001 ==begin==
            if (args.Context.Parameters["IS_BG_SCHEDULE"].Value.ToBoolean()) {
                if (!Maths.IsEmpty(_excuteResult)) {
                    throw new Digiwin.Common.BusinessRuleException(_excuteResult);
                } else {
                    int result = 1;
                    while (result != 2 && result != -1) {
                        result = QueryPlanCompleted(apsServerCommandQueueId, batchPlanStrategyId);
                        if (result == -1) {
                            throw new Digiwin.Common.BusinessRuleException(_excuteResult);
                        }
                        Thread.Sleep(5000);
                    }
                }
            }
            //20171218 add by xuyang for P001-170930001 ==end==
        }

        //20171218 add by xuyang for P001-170930001 ==begin==
        private int QueryPlanCompleted(object apsServerCommandQueueId,object batchPlanStrategyId) {
            int result = 1;  //0:APSAgent批次开始，1:APSAgent批次执行中，2:APSAgent批次执行成功，-1:APSAgent批次执行失败
            QueryNode qryNode = OOQL.Select(1, OOQL.CreateProperty("A.STATUS"))
                .From("APS_SERVER_COMMAND_QUEUE", "A").With(TableHintType.NoLock)
                .Where(OOQL.CreateProperty("A.APS_SERVER_COMMAND_QUEUE_ID") == OOQL.CreateConstants(apsServerCommandQueueId));
            DependencyObjectCollection dpColl = GetService<IQueryService>().ExecuteDependencyObject(qryNode);
            if (dpColl.Count > 0) {
                result = dpColl[0]["STATUS"].ToInt32();
            }
            if (result == -1) {
                qryNode = OOQL.Select(1,OOQL.CreateProperty("A.MRP_STATUS"))
                    .From("BATCH_PLAN_STRATEGY", "A").With(TableHintType.NoLock)
                    .Where(OOQL.CreateProperty("A.BATCH_PLAN_STRATEGY_ID") == OOQL.CreateConstants(batchPlanStrategyId));
                dpColl = GetService<IQueryService>().ExecuteDependencyObject(qryNode);
                if (dpColl.Count > 0) {
                    string mrpStatus = dpColl[0]["MRP_STATUS"].ToStringExtension();
                    _excuteResult = GetMrpStatusDisplayText(mrpStatus);
                }
            }
            return result;
        }
        
        //获取MRP_STATUS对应的内容
        private string GetMrpStatusDisplayText(string mrpStatus) {
            string displayText = string.Empty;
            IPickListDataService pickListDataService = this.GetService<IPickListDataService>() as IPickListDataService;
            ReadOnlyCollection<PickListItem> pickListItems = pickListDataService.GetPickListSortedData("APSStatus");
            foreach (PickListItem pickListItem in pickListItems) {
                if (pickListItem.Id.ToString() == mrpStatus) {
                    displayText = pickListItem.DisplayName;
                    break;
                }
            }
            return displayText;
        }
        //20171218 add by xuyang for P001-170930001 ==end==

        /// <summary>
        /// 5.将基本选项中选择的来源单据信息写入MDS 20150420 add by liuxp FOR T001-150525001
        /// </summary>
        /// <param name="args"></param>
        private void InsertMDSDData(FreeBatchEventsArgs args) {
            //20150724 add by shenbao for TB31-150724002======begin======
            if (!args.Context.Parameters.Contains("STRATEGY_MODE"))
                return;
            if (args.Context.Parameters["STRATEGY_MODE"].Value.ToStringExtension() != "4")
                return;
            //20150724 add by shenbao for TB31-150724002======end======

            IQueryService queryService = this.GetService<IQueryService>();

            ILogOnService logonSrc = this.GetService<ILogOnService>();  //20150818 add by wangrm for B001-150818001  
            IDateTimeService datetimeSrc = this.GetService<IDateTimeService>(); //20150818 add by wangrm for B001-150818001
            IPrimaryKeyService primaryKeyService = this.GetServiceForThisTypeKey<IPrimaryKeyService>();
            QueryNode childNode = OOQL.Select(OOQL.CreateProperty("MDS_ID")).From("MDS","MDS")
                                                                            .Where(OOQL.AuthFilter("MDS","MDS") & OOQL.CreateProperty("PLAN_STRATEGY_ID") == OOQL.CreateConstants(args.Context.Parameters["PLAN_STRATEGY_ID"].Value) &
                                                                                   OOQL.CreateProperty("Owner_Org.ROid") == OOQL.CreateConstants(args.Context.Parameters["PLANT_ID"].Value) &
                                                                                   OOQL.CreateProperty("VERSION_TIMES") == OOQL.CreateConstants(args.Context.Parameters["VERSION_TIMES"].Value));
            QueryNode deleteDNode = OOQL.Delete("MDS.MDS_D").Where(OOQL.CreateProperty("MDS_D.MDS_ID").In(childNode));
            queryService.ExecuteNoQuery(deleteDNode);
            QueryNode deleteNode = OOQL.Delete("MDS").Where(OOQL.CreateProperty("MDS_ID").In(childNode));
            queryService.ExecuteNoQuery(deleteNode);
            var insert = new Dictionary<string, QueryProperty>();
            object MDS_ID = primaryKeyService.CreateId();
            insert.Add("MDS_ID", OOQL.CreateConstants(MDS_ID));
            insert.Add("PLAN_STRATEGY_ID", OOQL.CreateConstants(args.Context.Parameters["PLAN_STRATEGY_ID"].Value));
            insert.Add("Owner_Org.ROid", OOQL.CreateConstants(args.Context.Parameters["PLANT_ID"].Value));
            insert.Add("Owner_Org.RTK", OOQL.CreateConstants("PLANT"));
            insert.Add("VERSION_TIMES", OOQL.CreateConstants(args.Context.Parameters["VERSION_TIMES"].Value));
            insert.Add("STRATEGY_MODE", OOQL.CreateConstants(args.Context.Parameters["STRATEGY_MODE"].Value));
            //20150818 add by wangrm for B001-150818001 begin
            insert.Add("ApproveStatus", OOQL.CreateConstants("Y"));
            insert.Add("ApproveBy", OOQL.CreateConstants(logonSrc.CurrentUserId));
            insert.Add("ApproveDate", OOQL.CreateConstants(datetimeSrc.Now, GeneralDBType.DateTime));
            //20150818 add by wangrm for B001-150818001 end
            //queryService.ExecuteNoQuery(OOQL.Insert("MDS", insert.Keys.ToArray()).Values(insert.Values.ToArray()));  //20190227 mark by xuyang for B31-190227012
            queryService.ExecuteNoQueryWithManageProperties(OOQL.Insert("MDS", insert.Keys.ToArray()).Values(insert.Values.ToArray()));   //20190227 add by xuyang for B31-190227012
            //insert MDS_D
            QueryNode nodeSelect = null;
            IList<QueryProperty> list = new List<QueryProperty>();
            DependencyObjectCollection sources = null;
            DependencyObjectCollection coll = null;
            int num = 0;
            int orderNum = 0;
            switch (args.Context.Parameters["PLAN_ACCORDING"].Value.ToStringExtension()) {
                #region 销售订单
                case "1":
                    //20170613 modi by xuyang for S001-170607003 ==begin==
                    //20171218 mark by xuyang for P001-170930001 ==begin==
                    //sources = args.Context.Parameters["PP_B003_SALES_ORDER_DOCS"].Value as DependencyObjectCollection;
                    //foreach (var item in sources) {
                    //    list.Add(OOQL.CreateConstants(item["SALES_ORDER_DOC_SD_ID"]));
                    //}
                    //20171218 mark by xuyang for P001-170930001 ==end==
                    //20171208 mark by xuyang for B001-171208011 补齐条件 ==begin==
                    //QueryCondition qrCond = OOQL.CreateConstants("1",GeneralDBType.Int32) == OOQL.CreateConstants("1",GeneralDBType.Int32);
                    //if (!args.Context.Parameters["UNGEN_DOC"].Value.ToBoolean()) {
                    //    sources = args.Context.Parameters["PP_B003_SALES_ORDER_DOCS"].Value as DependencyObjectCollection;
                    //    foreach (var item in sources) {
                    //        list.Add(OOQL.CreateConstants(item["SALES_ORDER_DOC_SD_ID"]));
                    //    }
                    //    if (list.Count >0)
                    //        qrCond = OOQL.CreateProperty("SALES_ORDER_DOC_SD.SALES_ORDER_DOC_SD_ID").In(list.ToArray());
                    //} else {
                    //    //20171030 modi by xuyang for P001-170926001 ==begin==
                    //    //qrCond = OOQL.CreateProperty("SALES_ORDER_DOC_SD.PLAN_STATUS") != OOQL.CreateConstants("3", GeneralDBType.Int32);
                    //    qrCond = OOQL.CreateProperty("SALES_ORDER_DOC_SD.PLAN_STATUS").In(OOQL.CreateConstants(1),OOQL.CreateConstants(2),OOQL.CreateConstants(5));
                    //    //20171030 modi by xuyang for P001-170926001 ==end==
                    //}
                    //20170613 modi by xuyang for S001-170607003 ==end==
                    //20171208 mark by xuyang for B001-171208011 补齐条件 ==end==
                    //20171208 add by xuyang for B001-171208011 补齐条件 ==begin==
                    QueryConditionGroup qrCond = OOQL.CreateProperty("IP.ORDER_POLICY") == OOQL.CreateConstants("P")
                        & OOQL.CreateProperty("IP.ITEM_PROPERTY") != OOQL.CreateConstants("F");
                    if (!args.Context.Parameters["UNGEN_DOC"].Value.ToBoolean()) {
                        sources = args.Context.Parameters["PP_B003_SALES_ORDER_DOCS"].Value as DependencyObjectCollection;
                        foreach (var item in sources) {
                            list.Add(OOQL.CreateConstants(item["SALES_ORDER_DOC_SD_ID"]));
                        }
                        if (list.Count > 0)
                            qrCond = qrCond & OOQL.CreateProperty("SALES_ORDER_DOC_SD.SALES_ORDER_DOC_SD_ID").In(list.ToArray());
                    } else {
                        qrCond = qrCond 
                            & OOQL.CreateProperty("SALES_ORDER_DOC_SD.PLAN_STATUS").In(OOQL.CreateConstants(1), OOQL.CreateConstants(2), OOQL.CreateConstants(5))
                            & OOQL.CreateProperty("SALES_ORDER_DOC.ApproveStatus") == OOQL.CreateConstants("Y")
                            & OOQL.CreateProperty("SALES_ORDER_DOC_SD.DELIVERY_PLANT_ID") == OOQL.CreateConstants(args.Context.Parameters["PLANT_ID"].Value)
                            & OOQL.CreateProperty("SALES_ORDER_DOC_SD.CLOSE") == OOQL.CreateConstants("0")
                            & OOQL.CreateProperty("SALES_ORDER_DOC_D.X_PMC_DATE") != OOQL.CreateConstants(OrmDataOption.EmptyDateTime)  //^_^ 20191009 add by xuyang for 容大个案
                            & OOQL.CreateProperty("SALES_ORDER_DOC_SD.DELIVERY_TYPE") != OOQL.CreateConstants("3")
                            & OOQL.CreateProperty("SALES_ORDER_DOC_SD.INCLUDE_PLAN") == OOQL.CreateConstants(1, GeneralDBType.Boolean);
                    }
                    //20171208 add by xuyang for B001-171208011 补齐条件 ==end==
                    //20161017 modi by guojian for B31-161014009 ====begin====
                    nodeSelect = OOQL.Select(OOQL.CreateProperty("SALES_ORDER_DOC_SD.PLAN_SHIP_DATE"),
                                             OOQL.CreateProperty("SALES_ORDER_DOC_SD.DELIVERED_BUSINESS_QTY"),
                                             OOQL.CreateProperty("SALES_ORDER_DOC.DOC_DATE"),
                                             OOQL.CreateProperty("SALES_ORDER_DOC_SD.BUSINESS_QTY"),
                                             //20170113 modi by xuyang for B31-170112014 ==BEGIN==
                                             //OOQL.CreateProperty("SALES_ORDER_DOC_D.ITEM_ID"),
                                             OOQL.CreateProperty("SALES_ORDER_DOC_D.ITEM_ID","ITEM_ID"),
                                             //20170113 modi by xuyang for B31-170112014 ==END==
                                             OOQL.CreateProperty("SALES_ORDER_DOC_D.BUSINESS_UNIT_ID"),
                                             OOQL.CreateProperty("ITEM.STOCK_UNIT_ID"),
                                             //OOQL.CreateProperty("SALES_ORDER_DOC_D.ITEM_ID"),//20170113 mark by xuyang for B31-170112014
                                             OOQL.CreateProperty("SALES_ORDER_DOC_D.ITEM_FEATURE_ID"),
                                             OOQL.CreateProperty("SALES_ORDER_DOC_SD.SALES_ORDER_DOC_SD_ID"),
                                             OOQL.CreateProperty("SALES_ORDER_DOC_SD.SequenceNumber"),
                                             OOQL.CreateProperty("SALES_ORDER_DOC_D.SequenceNumber", "SequenceNumberD"),
                                             OOQL.CreateProperty("SALES_ORDER_DOC.DOC_NO"),
                                             OOQL.CreateProperty("SALES_ORDER_DOC_D.KIT_DISTRIBUTION"),
                                             OOQL.CreateProperty("DISTRIBUTION_LIST.PLAN_SHIP_DATE", "PSDate"),
                                             OOQL.CreateProperty("DISTRIBUTION_LIST.BUSINESS_QTY", "DL_BQ"),
                                             OOQL.CreateProperty("DISTRIBUTION_LIST.DISTRIBUTED_BUS_QTY", "DL_DBQ"),
                                             OOQL.CreateProperty("DISTRIBUTION_LIST.ITEM_ID", "DL_ITEM_ID"),
                                             OOQL.CreateProperty("DISTRIBUTION_LIST.ITEM_FEATURE_ID", "DL_ITEM_FEATURE_ID"),
                                             OOQL.CreateProperty("A.STOCK_UNIT_ID", "A_UNIT_ID"),
                                             OOQL.CreateProperty("DISTRIBUTION_LIST.DISTRIBUTION_LIST_ID"),
                                             OOQL.CreateProperty("DISTRIBUTION_LIST.SEQUENCE_NUMBER"),
                                             OOQL.CreateProperty("DISTRIBUTION_LIST.BUSINESS_UNIT_ID", "DL_BUSINESS_UNIT_ID"),
                        //20170113 ADD by xuyang for B31-170112014 ==BEGIN==
                                             Formulas.Case(
                                                            OOQL.CreateProperty("KIT_DISTRIBUTION"),
                                                            OOQL.CreateProperty("SALES_ORDER_DOC_SD.PLAN_SHIP_DATE"),
                                                            OOQL.CreateCaseArray(
                                                                    OOQL.CreateCaseItem(
                                                                            OOQL.CreateConstants(1, GeneralDBType.Int32),
                                                                            OOQL.CreateProperty("DISTRIBUTION_LIST.PLAN_SHIP_DATE"))), "DEMAND_DATE"),
                        //20170113 ADD by xuyang for B31-170112014 ==END==
                                             OOQL.CreateProperty("SALES_ORDER_DOC_SD.CLOSE")
                        //20180711 add by xuyang for P001-170930002 ==begin==
                                            , Formulas.Case(null, OOQL.CreateProperty("SALES_ORDER_DOC_SD.PLAN_STATUS"), new CaseItem[]{
                                                new CaseItem(OOQL.CreateProperty("SALES_ORDER_DOC_D.KIT_DISTRIBUTION") == OOQL.CreateConstants(true), OOQL.CreateProperty("DISTRIBUTION_LIST.PLAN_STATUS"))
                                             }, "PLAN_STATUS")
                        //20180711 add by xuyang for P001-170930002 ==end==
                                             )
                                     .From("SALES_ORDER_DOC.SALES_ORDER_DOC_D.SALES_ORDER_DOC_SD", "SALES_ORDER_DOC_SD")
                                     .LeftJoin("SALES_ORDER_DOC.SALES_ORDER_DOC_D", "SALES_ORDER_DOC_D")
                                     .On(OOQL.CreateProperty("SALES_ORDER_DOC_SD.SALES_ORDER_DOC_D_ID") == OOQL.CreateProperty("SALES_ORDER_DOC_D.SALES_ORDER_DOC_D_ID"))
                                     .LeftJoin("SALES_ORDER_DOC")
                                     .On(OOQL.CreateProperty("SALES_ORDER_DOC_D.SALES_ORDER_DOC_ID") == OOQL.CreateProperty("SALES_ORDER_DOC.SALES_ORDER_DOC_ID"))
                                     .LeftJoin("ITEM")
                                     .On(OOQL.CreateProperty("SALES_ORDER_DOC_D.ITEM_ID") == OOQL.CreateProperty("ITEM.ITEM_ID"))
                                     .LeftJoin("DISTRIBUTION_LIST")
                                     .On(OOQL.CreateProperty("DISTRIBUTION_LIST.SOURCE_ID") == OOQL.CreateProperty("SALES_ORDER_DOC_SD.SALES_ORDER_DOC_SD_ID")
                                        & OOQL.CreateProperty("SALES_ORDER_DOC_D.KIT_DISTRIBUTION") == OOQL.CreateConstants(true))
                                     .LeftJoin("ITEM", "A")
                                     .On(OOQL.CreateProperty("DISTRIBUTION_LIST.ITEM_ID") == OOQL.CreateProperty("A.ITEM_ID")
                                        & OOQL.CreateProperty("SALES_ORDER_DOC_D.KIT_DISTRIBUTION") == OOQL.CreateConstants(true))
                                    //20171205 add by xuyang for T001-171116002==begin==
                                    .LeftJoin("ITEM_PLANT", "IP")
                                    .On((OOQL.CreateProperty("IP.ITEM_ID") == OOQL.CreateProperty("SALES_ORDER_DOC_D.ITEM_ID"))
                                        & (OOQL.CreateProperty("SALES_ORDER_DOC_SD.DELIVERY_PLANT_ID") == OOQL.CreateProperty("IP.Owner_Org.ROid")))
                                     //20171205 add by xuyang for T001-171116002==end==
                                     //20170613 modi by xuyang for S001-170607003 ==begin==
                                     //.Where(OOQL.AuthFilter("SALES_ORDER_DOC.SALES_ORDER_DOC_D.SALES_ORDER_DOC_SD", "SALES_ORDER_DOC_SD") & OOQL.CreateProperty("SALES_ORDER_DOC_SD.SALES_ORDER_DOC_SD_ID").In(list.ToArray())
                                     .Where(OOQL.AuthFilter("SALES_ORDER_DOC.SALES_ORDER_DOC_D.SALES_ORDER_DOC_SD", "SALES_ORDER_DOC_SD") & qrCond
                                     //20170613 modi by xuyang for S001-170607003 ==end==
                                        //20171208 mark by xuyang for B001-171208011 ==begin==
                                        //20171205 add by xuyang for T001-171116002==begin==
                                        //& (OOQL.CreateProperty("IP.ORDER_POLICY") == OOQL.CreateConstants("P"))
                                        //& (OOQL.CreateProperty("IP.ITEM_PROPERTY") != OOQL.CreateConstants("F"))
                                        //20171205 add by xuyang for T001-171116002==end==
                                        //20171208 mark by xuyang for B001-171208011 ==end==
                                        & (OOQL.CreateProperty("SALES_ORDER_DOC_D.KIT_DISTRIBUTION") == OOQL.CreateConstants(false)
                                            | OOQL.CreateProperty("DISTRIBUTION_LIST.DISTRIBUTION_LIST_ID").IsNotNull())
                                        & ((OOQL.CreateProperty("SALES_ORDER_DOC_D.KIT_DISTRIBUTION") == OOQL.CreateConstants(false)
                                            & OOQL.CreateProperty("ITEM.E_CODE") == OOQL.CreateConstants("P"))
                                          | (OOQL.CreateProperty("SALES_ORDER_DOC_D.KIT_DISTRIBUTION") == OOQL.CreateConstants(true)
                                            & OOQL.CreateProperty("A.E_CODE") == OOQL.CreateConstants("P"))));
                                     //.OrderBy(new OrderByItem[] { new OrderByItem(OOQL.CreateProperty("SALES_ORDER_DOC_D.ITEM_ID"), SortType.Desc) });  //20170113 MARK by xuyang for B31-170112014
                    //20170113 ADD by xuyang for B31-170112014 ==BEGIN==
                    nodeSelect = OOQL.Select(OOQL.CreateProperty("A.PLAN_SHIP_DATE"),
                                             OOQL.CreateProperty("A.DELIVERED_BUSINESS_QTY"),
                                             OOQL.CreateProperty("A.DOC_DATE"),
                                             OOQL.CreateProperty("A.BUSINESS_QTY"),
                                             OOQL.CreateProperty("A.ITEM_ID", "ITEM_ID"),
                                             OOQL.CreateProperty("A.BUSINESS_UNIT_ID"),
                                             OOQL.CreateProperty("A.STOCK_UNIT_ID"),
                                             OOQL.CreateProperty("A.ITEM_FEATURE_ID"),
                                             OOQL.CreateProperty("A.SALES_ORDER_DOC_SD_ID"),
                                             OOQL.CreateProperty("A.SequenceNumber"),
                                             OOQL.CreateProperty("A.SequenceNumberD"),
                                             OOQL.CreateProperty("A.DOC_NO"),
                                             OOQL.CreateProperty("A.KIT_DISTRIBUTION"),
                                             OOQL.CreateProperty("A.PSDate"),
                                             OOQL.CreateProperty("A.DL_BQ"),
                                             OOQL.CreateProperty("A.DL_DBQ"),
                                             OOQL.CreateProperty("A.DL_ITEM_ID"),
                                             OOQL.CreateProperty("A.DL_ITEM_FEATURE_ID"),
                                             OOQL.CreateProperty("A.A_UNIT_ID"),
                                             OOQL.CreateProperty("A.DISTRIBUTION_LIST_ID"),
                                             OOQL.CreateProperty("A.SEQUENCE_NUMBER"),
                                             OOQL.CreateProperty("A.DL_BUSINESS_UNIT_ID"),
                                             OOQL.CreateProperty("A.DEMAND_DATE"),
                                             OOQL.CreateProperty("A.CLOSE")
                                             , OOQL.CreateProperty("A.PLAN_STATUS")  //20180711 add by xuyang for P001-170930002
                                             )
                                             .From(nodeSelect,"A")
                                             .OrderBy(new OrderByItem[] { new OrderByItem(OOQL.CreateProperty("A.DEMAND_DATE"), SortType.Asc)});
                    //20170113 ADD by xuyang for B31-170112014 ==END==
                    coll = queryService.ExecuteDependencyObject(nodeSelect);
                    foreach (var item in coll) {
                        num++;
                        orderNum += 10;
                        Dictionary<string, QueryProperty> dicInsert = new Dictionary<string, QueryProperty>();
                        dicInsert.Add("MDS_D_ID", OOQL.CreateConstants(primaryKeyService.CreateId()));
                        dicInsert.Add("SEQ", OOQL.CreateConstants(num));
                        dicInsert.Add("PRIORITY_ORDER", OOQL.CreateConstants(orderNum.ToStringExtension().PadLeft(6, '0')));
                        dicInsert.Add("DEMAND_START_DATE", OOQL.CreateConstants(item["KIT_DISTRIBUTION"].ToBoolean() ? item["PSDate"] : item["PLAN_SHIP_DATE"]));
                        dicInsert.Add("DEMAND_DATE", OOQL.CreateConstants(item["KIT_DISTRIBUTION"].ToBoolean() ? item["PSDate"] : item["PLAN_SHIP_DATE"]));
                        dicInsert.Add("DEMAND_QTY", Formulas.Case(OOQL.CreateConstants(item["KIT_DISTRIBUTION"].ToBoolean()), Formulas.Ext("UNIT_CONVERT", new object[]
                                {
                                    OOQL.CreateConstants(item["ITEM_ID"]),
                                    OOQL.CreateConstants(item["BUSINESS_UNIT_ID"]), 
                                    OOQL.CreateConstants(new string[]{"0","2"}.Contains(item["CLOSE"].ToString()) ? item["BUSINESS_QTY"] : item["DELIVERED_BUSINESS_QTY"]),
                                    OOQL.CreateConstants(item["STOCK_UNIT_ID"]),
                                    OOQL.CreateConstants(0)

                                }),
                            new CaseItem[] { new CaseItem(OOQL.CreateConstants(true), Formulas.Ext("UNIT_CONVERT", new object[]
                                {
                                    OOQL.CreateConstants(item["DL_ITEM_ID"]),
                                    OOQL.CreateConstants(item["DL_BUSINESS_UNIT_ID"]), 
                                    OOQL.CreateConstants(new string[]{"0","2"}.Contains(item["CLOSE"].ToString()) ? item["DL_BQ"] : item["DL_DBQ"]),
                                    OOQL.CreateConstants(item["A_UNIT_ID"]),
                                    OOQL.CreateConstants(0)

                                })) }));
                        dicInsert.Add("DOC_DATE", OOQL.CreateConstants(item["DOC_DATE"]));
                        dicInsert.Add("ORIGINAL_DEMAND_QTY", OOQL.CreateConstants(item["KIT_DISTRIBUTION"].ToBoolean() ? item["DL_BQ"] : item["BUSINESS_QTY"]));
                        dicInsert.Add("OFFSET_QTY", OOQL.CreateConstants(0));
                        dicInsert.Add("OFFSET_STATUS", OOQL.CreateConstants("1"));
                        dicInsert.Add("APS_FLAG", OOQL.CreateConstants(true));
                        dicInsert.Add("START_DATE", OOQL.CreateConstants(item["KIT_DISTRIBUTION"].ToBoolean() ? item["PSDate"] : item["PLAN_SHIP_DATE"]));
                        dicInsert.Add("END_DATE", OOQL.CreateConstants(item["KIT_DISTRIBUTION"].ToBoolean() ? item["PSDate"] : item["PLAN_SHIP_DATE"]));
                        dicInsert.Add("DELIVERED_QTY", OOQL.CreateConstants(item["KIT_DISTRIBUTION"].ToBoolean() ? item["DL_DBQ"] : item["DELIVERED_BUSINESS_QTY"]));
                        dicInsert.Add("INVENTORY_QTY", Formulas.Case(OOQL.CreateConstants(item["KIT_DISTRIBUTION"].ToBoolean()), Formulas.Ext("UNIT_CONVERT", new object[]
                                {
                                    OOQL.CreateConstants(item["ITEM_ID"]),
                                    OOQL.CreateConstants(item["BUSINESS_UNIT_ID"]), 
                                    OOQL.CreateConstants(item["BUSINESS_QTY"])-OOQL.CreateConstants(item["DELIVERED_BUSINESS_QTY"]),
                                    OOQL.CreateConstants(item["STOCK_UNIT_ID"]),
                                    OOQL.CreateConstants(0)

                                }),
                            new CaseItem[] { new CaseItem(OOQL.CreateConstants(true), Formulas.Ext("UNIT_CONVERT", new object[]
                                {
                                    OOQL.CreateConstants(item["DL_ITEM_ID"]),
                                    OOQL.CreateConstants(item["DL_BUSINESS_UNIT_ID"]), 
                                    OOQL.CreateConstants(item["DL_BQ"])-OOQL.CreateConstants(item["DL_DBQ"]),
                                    OOQL.CreateConstants(item["A_UNIT_ID"]),
                                    OOQL.CreateConstants(0)

                                })) }));
                        dicInsert.Add("ITEM_ID", OOQL.CreateConstants(item["KIT_DISTRIBUTION"].ToBoolean() ? item["DL_ITEM_ID"] : item["ITEM_ID"]));
                        dicInsert.Add("ITEM_FEATURE_ID", OOQL.CreateConstants(item["KIT_DISTRIBUTION"].ToBoolean() ? item["DL_ITEM_FEATURE_ID"] : item["ITEM_FEATURE_ID"]));
                        dicInsert.Add("UNIT_ID", OOQL.CreateConstants(item["KIT_DISTRIBUTION"].ToBoolean() ? item["A_UNIT_ID"] : item["STOCK_UNIT_ID"]));
                        dicInsert.Add("FAMILY_ITEM_ID", OOQL.CreateConstants(Maths.GuidDefaultValue()));
                        dicInsert.Add("PLAN_DELIVERY_DATE", OOQL.CreateConstants(item["PLAN_SHIP_DATE"]));
                        dicInsert.Add("SOURCE_ID.RTK", OOQL.CreateConstants(item["KIT_DISTRIBUTION"].ToBoolean() ? "DISTRIBUTION_LIST" : "SALES_ORDER_DOC.SALES_ORDER_DOC_D.SALES_ORDER_DOC_SD"));
                        dicInsert.Add("SOURCE_ID.ROid", OOQL.CreateConstants(item["KIT_DISTRIBUTION"].ToBoolean() ? item["DISTRIBUTION_LIST_ID"] : item["SALES_ORDER_DOC_SD_ID"]));
                        dicInsert.Add("MDS_ID", OOQL.CreateConstants(MDS_ID));
                        dicInsert.Add("NETTING_TYPE", OOQL.CreateConstants(args.Context.Parameters["MRP_REQUIREMENT_CALCULATION"].Value));
                        dicInsert.Add("DEMAND", OOQL.CreateConstants(item["KIT_DISTRIBUTION"].ToBoolean() ?
                            item["DOC_NO"].ToStringExtension() + "-" + item["SequenceNumberD"].ToStringExtension() + "-" + item["SequenceNumber"].ToStringExtension() + "-" + item["SEQUENCE_NUMBER"].ToStringExtension() :
                            item["DOC_NO"].ToStringExtension() + "-" + item["SequenceNumberD"].ToStringExtension() + "-" + item["SequenceNumber"].ToStringExtension()));
                        dicInsert.Add("ORI_SOURCE_ID.ROid", OOQL.CreateConstants(item["KIT_DISTRIBUTION"].ToBoolean() ? item["DISTRIBUTION_LIST_ID"] : item["SALES_ORDER_DOC_SD_ID"]));
                        dicInsert.Add("ORI_SOURCE_ID.RTK", OOQL.CreateConstants(item["KIT_DISTRIBUTION"].ToBoolean() ? "DISTRIBUTION_LIST" : "SALES_ORDER_DOC.SALES_ORDER_DOC_D.SALES_ORDER_DOC_SD"));
                        dicInsert.Add("BUSINESS_UNIT_ID", OOQL.CreateConstants(item["KIT_DISTRIBUTION"].ToBoolean() ? item["DL_BUSINESS_UNIT_ID"] : item["BUSINESS_UNIT_ID"]));
                        //20161017 modi by guojian for B31-161014009 =====end=====
                        //20150818 add by wangrm for B001-150818001 begin
                        dicInsert.Add("ApproveStatus", OOQL.CreateConstants("Y"));
                        dicInsert.Add("ApproveBy", OOQL.CreateConstants(logonSrc.CurrentUserId));
                        dicInsert.Add("ApproveDate", OOQL.CreateConstants(datetimeSrc.Now, GeneralDBType.DateTime));
                        //20150818 add by wangrm for B001-150818001  end
                        dicInsert.Add("PLAN_STATUS", OOQL.CreateConstants(item["PLAN_STATUS"].ToStringExtension()));  //20180711 add by xuyang for P001-170930002
                        QueryNode insertNode = OOQL.Insert("MDS.MDS_D", dicInsert.Keys.ToArray()).Values(dicInsert.Values.ToArray());
                        //queryService.ExecuteNoQuery(insertNode); //20190227 mark by xuyang for B31-190227012
                        queryService.ExecuteNoQueryWithManageProperties(insertNode); //20190227 add by xuyang for B31-190227012
                    }
                    break;
                #endregion
                #region 调拨申请单
                case "2":
                    //20170613 modi by xuyang for S001-170607003 ==begin==
                    //sources = args.Context.Parameters["PP_B003_TRANSFER_REQUISITION_DS"].Value as DependencyObjectCollection;
                    //foreach (var item in sources) {
                    //    list.Add(OOQL.CreateConstants(item["TRANSFER_REQUISITION_D_ID"]));
                    //}
                    //20171208 mark by xuyang for B001-171208011 补齐条件 ==begin==
                    //qrCond = OOQL.CreateConstants("1", GeneralDBType.Int32) == OOQL.CreateConstants("1", GeneralDBType.Int32);
                    //if (!args.Context.Parameters["UNGEN_DOC"].Value.ToBoolean()) {
                    //    sources = args.Context.Parameters["PP_B003_TRANSFER_REQUISITION_DS"].Value as DependencyObjectCollection;
                    //    foreach (var item in sources) {
                    //        list.Add(OOQL.CreateConstants(item["TRANSFER_REQUISITION_D_ID"]));
                    //    }
                    //    if (list.Count > 0)
                    //        qrCond = OOQL.CreateProperty("TRANSFER_REQUISITION_D.TRANSFER_REQUISITION_D_ID").In(list.ToArray());
                    //} else {
                    //    //20171030 modi by xuyang for P001-170926001 ==begin==
                    //    //qrCond = OOQL.CreateProperty("TRANSFER_REQUISITION_D.PLAN_STATUS") != OOQL.CreateConstants("3",GeneralDBType.Int32);
                    //    qrCond = OOQL.CreateProperty("TRANSFER_REQUISITION_D.PLAN_STATUS").In(OOQL.CreateConstants(1), OOQL.CreateConstants(5)); //20171205 modi by xuyang for T001-171116002 去掉OOQL.CreateConstants(2) OLD: In(OOQL.CreateConstants(1), OOQL.CreateConstants(2), OOQL.CreateConstants(5))
                    //    //20171030 modi by xuyang for P001-170926001 ==end==
                    //}
                    //20171208 mark by xuyang for B001-171208011 补齐条件 ==end==
                    //20171208 add by xuyang for B001-171208011 补齐条件 ==begin==
                    qrCond = OOQL.CreateProperty("IP.ORDER_POLICY") == OOQL.CreateConstants("P")
                        & OOQL.CreateProperty("IP.ITEM_PROPERTY") != OOQL.CreateConstants("F");
                    if (!args.Context.Parameters["UNGEN_DOC"].Value.ToBoolean()) {
                        sources = args.Context.Parameters["PP_B003_TRANSFER_REQUISITION_DS"].Value as DependencyObjectCollection;
                        foreach (var item in sources) {
                            list.Add(OOQL.CreateConstants(item["TRANSFER_REQUISITION_D_ID"]));
                        }
                        if (list.Count > 0)
                            qrCond = qrCond & OOQL.CreateProperty("TRANSFER_REQUISITION_D.TRANSFER_REQUISITION_D_ID").In(list.ToArray());
                    } else {
                        qrCond = qrCond & OOQL.CreateProperty("TRANSFER_REQUISITION_D.PLAN_STATUS").In(OOQL.CreateConstants("1"), OOQL.CreateConstants("5"))
                            & OOQL.CreateProperty("TRANSFER_REQUISITION.FROM_PLANT_ID") == OOQL.CreateConstants(args.Context.Parameters["PLANT_ID"].Value)
                            & OOQL.CreateProperty("TRANSFER_REQUISITION.Owner_Org.ROid") != OOQL.CreateConstants(args.Context.Parameters["PLANT_ID"].Value)
                            & OOQL.CreateProperty("TRANSFER_REQUISITION.ApproveStatus") == OOQL.CreateConstants("Y")
                            & OOQL.CreateProperty("TRANSFER_REQUISITION_D.CLOSE") == OOQL.CreateConstants("0");
                    }
                    //20171208 add by xuyang for B001-171208011 补齐条件 ==end==
                    //20170613 modi by xuyang for S001-170607003 ==end==
                    nodeSelect = OOQL.Select(OOQL.CreateProperty("TRANSFER_REQUISITION_D.REQUISITION_DATE"),
                                             OOQL.CreateProperty("TRANSFER_REQUISITION_D.TRANS_OUT_BUSINESS_QTY"),
                                             OOQL.CreateProperty("TRANSFER_REQUISITION.DOC_DATE"),
                                             OOQL.CreateProperty("TRANSFER_REQUISITION_D.BUSINESS_QTY"),
                                             OOQL.CreateProperty("TRANSFER_REQUISITION_D.TRANS_OUT_BUSINESS_QTY"),
                                             OOQL.CreateProperty("TRANSFER_REQUISITION_D.ITEM_ID"),
                                             OOQL.CreateProperty("TRANSFER_REQUISITION_D.BUSINESS_UNIT_ID"),
                                             OOQL.CreateProperty("ITEM.STOCK_UNIT_ID"),
                                             OOQL.CreateProperty("TRANSFER_REQUISITION_D.ITEM_FEATURE_ID"),
                                             OOQL.CreateProperty("TRANSFER_REQUISITION_D.TRANSFER_REQUISITION_D_ID"),
                                             OOQL.CreateProperty("TRANSFER_REQUISITION_D.SequenceNumber"),
                                             OOQL.CreateProperty("TRANSFER_REQUISITION.DOC_NO"),
                                             OOQL.CreateProperty("TRANSFER_REQUISITION_D.REFERENCE_SOURCE_ID.ROid", "REFERENCE_SOURCE_ID_ROid"),
                                             OOQL.CreateProperty("TRANSFER_REQUISITION_D.REFERENCE_SOURCE_ID.RTK", "REFERENCE_SOURCE_ID_RTK")
                                             ,OOQL.CreateProperty("TRANSFER_REQUISITION_D.PLAN_STATUS", "PLAN_STATUS") //20180711 add by xuyang for P001-170930002
                                             )
                                     .From("TRANSFER_REQUISITION.TRANSFER_REQUISITION_D", "TRANSFER_REQUISITION_D")
                                     .LeftJoin("TRANSFER_REQUISITION")
                                     .On(OOQL.CreateProperty("TRANSFER_REQUISITION.TRANSFER_REQUISITION_ID") == OOQL.CreateProperty("TRANSFER_REQUISITION_D.TRANSFER_REQUISITION_ID"))
                                     .LeftJoin("ITEM")
                                     .On(OOQL.CreateProperty("TRANSFER_REQUISITION_D.ITEM_ID") == OOQL.CreateProperty("ITEM.ITEM_ID"))
                                    //20171205 add by xuyang for T001-171116002==begin==
                                    .LeftJoin("ITEM_PLANT", "IP")
                                    .On((OOQL.CreateProperty("IP.ITEM_ID") == OOQL.CreateProperty("TRANSFER_REQUISITION_D.ITEM_ID"))
                                    & (OOQL.CreateProperty("TRANSFER_REQUISITION.FROM_PLANT_ID") == OOQL.CreateProperty("IP.Owner_Org.ROid")))
                                     //20171205 add by xuyang for T001-171116002==end==
                                     //20170613 modi by xuyang for S001-170607003 ==begin==
                                     //.Where(OOQL.AuthFilter("TRANSFER_REQUISITION.TRANSFER_REQUISITION_D", "TRANSFER_REQUISITION_D") & OOQL.CreateProperty("TRANSFER_REQUISITION_D.TRANSFER_REQUISITION_D_ID").In(list.ToArray()))
                                     //.Where(OOQL.AuthFilter("TRANSFER_REQUISITION.TRANSFER_REQUISITION_D", "TRANSFER_REQUISITION_D") & qrCond)  //20171205 mark by xuyang for T001-171116002
                                     //20170613 modi by xuyang for S001-170607003 ==end==
                                     //20171205 add by xuyang for T001-171116002 ==begin==
                                     .Where(OOQL.AuthFilter("TRANSFER_REQUISITION.TRANSFER_REQUISITION_D", "TRANSFER_REQUISITION_D") & qrCond)
                                     //& OOQL.CreateProperty("IP.ORDER_POLICY") == OOQL.CreateConstants("P")//20171208 mark by xuyang for B001-171208011 补齐条件 
                                     //& OOQL.CreateProperty("IP.ITEM_PROPERTY") != OOQL.CreateConstants("F"))  //20171208 mark by xuyang for B001-171208011 补齐条件 
                                     //20171205 add by xuyang for T001-171116002 ==end==
                                     //20170113 modi by xuyang for B31-170112014 ==begin==
                                     //.OrderBy(new OrderByItem[] { new OrderByItem(OOQL.CreateProperty("TRANSFER_REQUISITION_D.ITEM_ID"), SortType.Desc) });
                                     .OrderBy(new OrderByItem[] { new OrderByItem(OOQL.CreateProperty("TRANSFER_REQUISITION_D.REQUISITION_DATE"), SortType.Asc) });
                                     //20170113 modi by xuyang for B31-170112014 ==end==
                    coll = queryService.ExecuteDependencyObject(nodeSelect);
                    foreach (var item in coll) {
                        num++;
                        orderNum += 10;
                        Dictionary<string, QueryProperty> dicInsert = new Dictionary<string, QueryProperty>();
                        dicInsert.Add("MDS_D_ID", OOQL.CreateConstants(primaryKeyService.CreateId()));
                        dicInsert.Add("SEQ", OOQL.CreateConstants(num));
                        dicInsert.Add("PRIORITY_ORDER", OOQL.CreateConstants(orderNum.ToStringExtension().PadLeft(6, '0')));
                        dicInsert.Add("DEMAND_START_DATE", OOQL.CreateConstants(item["REQUISITION_DATE"]));
                        dicInsert.Add("DEMAND_DATE", OOQL.CreateConstants(item["REQUISITION_DATE"]));
                        dicInsert.Add("DEMAND_QTY", OOQL.CreateConstants(item["TRANS_OUT_BUSINESS_QTY"]));
                        dicInsert.Add("DOC_DATE", OOQL.CreateConstants(item["DOC_DATE"]));
                        dicInsert.Add("ORIGINAL_DEMAND_QTY", OOQL.CreateConstants(item["BUSINESS_QTY"]));
                        dicInsert.Add("OFFSET_QTY", OOQL.CreateConstants(0));
                        dicInsert.Add("OFFSET_STATUS", OOQL.CreateConstants("1"));
                        dicInsert.Add("APS_FLAG", OOQL.CreateConstants(true));
                        dicInsert.Add("START_DATE", OOQL.CreateConstants(item["REQUISITION_DATE"]));
                        dicInsert.Add("END_DATE", OOQL.CreateConstants(item["REQUISITION_DATE"]));
                        dicInsert.Add("DELIVERED_QTY", OOQL.CreateConstants(item["TRANS_OUT_BUSINESS_QTY"]));
                        dicInsert.Add("INVENTORY_QTY", Formulas.Ext("UNIT_CONVERT", new object[]{ OOQL.CreateConstants(item["ITEM_ID"])
                                                                                                                            , OOQL.CreateConstants(item["BUSINESS_UNIT_ID"])
                                                                                                                            , OOQL.CreateConstants(item["BUSINESS_QTY"])-OOQL.CreateConstants(item["TRANS_OUT_BUSINESS_QTY"])
                                                                                                                            , OOQL.CreateConstants(item["STOCK_UNIT_ID"])
                                                                                                                            ,OOQL.CreateConstants(0)}));
                        dicInsert.Add("ITEM_ID", OOQL.CreateConstants(item["ITEM_ID"]));
                        dicInsert.Add("ITEM_FEATURE_ID", OOQL.CreateConstants(item["ITEM_FEATURE_ID"]));
                        dicInsert.Add("UNIT_ID", OOQL.CreateConstants(item["STOCK_UNIT_ID"]));
                        dicInsert.Add("FAMILY_ITEM_ID", OOQL.CreateConstants(Maths.GuidDefaultValue()));
                        dicInsert.Add("PLAN_DELIVERY_DATE", OOQL.CreateConstants(item["REQUISITION_DATE"]));
                        dicInsert.Add("SOURCE_ID.RTK", OOQL.CreateConstants("TRANSFER_REQUISITION.TRANSFER_REQUISITION_D"));
                        dicInsert.Add("SOURCE_ID.ROid", OOQL.CreateConstants(item["TRANSFER_REQUISITION_D_ID"]));
                        dicInsert.Add("MDS_ID", OOQL.CreateConstants(MDS_ID));
                        dicInsert.Add("NETTING_TYPE", OOQL.CreateConstants(args.Context.Parameters["MRP_REQUIREMENT_CALCULATION"].Value));
                        dicInsert.Add("DEMAND", OOQL.CreateConstants(item["DOC_NO"].ToStringExtension() + "-" + item["SequenceNumber"].ToStringExtension()));
                        dicInsert.Add("ORI_SOURCE_ID.ROid", OOQL.CreateConstants(item["REFERENCE_SOURCE_ID_ROid"]));
                        dicInsert.Add("ORI_SOURCE_ID.RTK", OOQL.CreateConstants(item["REFERENCE_SOURCE_ID_RTK"]));
                        dicInsert.Add("BUSINESS_UNIT_ID", OOQL.CreateConstants(item["BUSINESS_UNIT_ID"]));
                        //20150818 add by wangrm for B001-150818001 begin
                        dicInsert.Add("ApproveStatus", OOQL.CreateConstants("Y"));
                        dicInsert.Add("ApproveBy", OOQL.CreateConstants(logonSrc.CurrentUserId));
                        dicInsert.Add("ApproveDate", OOQL.CreateConstants(datetimeSrc.Now, GeneralDBType.DateTime));
                        //20150818 add by wangrm for B001-150818001  end
                        dicInsert.Add("PLAN_STATUS", OOQL.CreateConstants(item["PLAN_STATUS"].ToStringExtension()));  //20180711 add by xuyang for P001-170930002
                        QueryNode insertNode = OOQL.Insert("MDS.MDS_D", dicInsert.Keys.ToArray()).Values(dicInsert.Values.ToArray());
                        //queryService.ExecuteNoQuery(insertNode);//20190227 mark by xuyang for B31-190227012
                        queryService.ExecuteNoQueryWithManageProperties(insertNode); //20190227 add by xuyang for B31-190227012
                    }
                    break;
                #endregion
                #region 内部订单
                case "3":
                    //20170613 modi by xuyang for S001-170607003 ==begin==
                    //sources = args.Context.Parameters["PP_B003_INNER_ORDER_DOCS"].Value as DependencyObjectCollection;
                    //foreach (var item in sources) {
                    //    list.Add(OOQL.CreateConstants(item["INNER_ORDER_DOC_SD_ID"]));
                    //}
                    //20171208 mark by xuyang for B001-171208011 补齐条件 ==begin==
                    //qrCond = OOQL.CreateConstants("1", GeneralDBType.Int32) == OOQL.CreateConstants("1", GeneralDBType.Int32);
                    //if (!args.Context.Parameters["UNGEN_DOC"].Value.ToBoolean()) {
                    //    sources = args.Context.Parameters["PP_B003_INNER_ORDER_DOCS"].Value as DependencyObjectCollection;
                    //    foreach (var item in sources) {
                    //        list.Add(OOQL.CreateConstants(item["INNER_ORDER_DOC_SD_ID"]));
                    //    }
                    //    if (list.Count > 0)
                    //        qrCond = OOQL.CreateProperty("INNER_ORDER_DOC_SD.INNER_ORDER_DOC_SD_ID").In(list.ToArray());
                    //} else {
                    //    //20171030 modi by xuyang for P001-170926001 ==begin==
                    //    //qrCond = OOQL.CreateProperty("INNER_ORDER_DOC_SD.PLAN_STATUS") != OOQL.CreateConstants("3",GeneralDBType.Int32);
                    //    qrCond = OOQL.CreateProperty("INNER_ORDER_DOC_SD.PLAN_STATUS").In(OOQL.CreateConstants(1), OOQL.CreateConstants(5));   //20171205 modi by xuyang for T001-171116002 去掉OOQL.CreateConstants(2) OLD: In(OOQL.CreateConstants(1), OOQL.CreateConstants(2), OOQL.CreateConstants(5))
                    //    //20171030 modi by xuyang for P001-170926001 ==end==
                    //}
                    //20171208 mark by xuyang for B001-171208011 补齐条件 ==end==
                    //20171208 add by xuyang for B001-171208011 补齐条件 ==begin==
                    qrCond = OOQL.CreateProperty("IP.ORDER_POLICY") == OOQL.CreateConstants("P")
                        & OOQL.CreateProperty("IP.ITEM_PROPERTY") != OOQL.CreateConstants("F");
                    if (!args.Context.Parameters["UNGEN_DOC"].Value.ToBoolean()) {
                        sources = args.Context.Parameters["PP_B003_INNER_ORDER_DOCS"].Value as DependencyObjectCollection;
                        foreach (var item in sources) {
                            list.Add(OOQL.CreateConstants(item["INNER_ORDER_DOC_SD_ID"]));
                        }
                        if (list.Count > 0)
                            qrCond = qrCond & OOQL.CreateProperty("INNER_ORDER_DOC_SD.INNER_ORDER_DOC_SD_ID").In(list.ToArray());
                    } else {
                        qrCond = qrCond & OOQL.CreateProperty("INNER_ORDER_DOC_SD.PLAN_STATUS").In(OOQL.CreateConstants("1"), OOQL.CreateConstants("5"))
                            & OOQL.CreateProperty("INNER_ORDER_DOC_SD.DELIVERY_PLANT_ID") == OOQL.CreateConstants(args.Context.Parameters["PLANT_ID"].Value)
                            & OOQL.CreateProperty("INNER_ORDER_DOC.ApproveStatus") == OOQL.CreateConstants("Y")
                            & OOQL.CreateProperty("INNER_ORDER_DOC_SD.CLOSE") == OOQL.CreateConstants("0")
                            & OOQL.CreateProperty("INNER_ORDER_DOC_D.NOT_INCLUDE_PLAN") == OOQL.CreateConstants(0, GeneralDBType.Boolean)
                            & OOQL.CreateProperty("INNER_ORDER_DOC_SD.DELIVERY_TYPE") != OOQL.CreateConstants("3");
                    }
                    //20171208 add by xuyang for B001-171208011 补齐条件 ==end==
                    //20170613 modi by xuyang for S001-170607003 ==end==
                    nodeSelect = OOQL.Select(OOQL.CreateProperty("INNER_ORDER_DOC_SD.PLAN_SHIP_DATE"),
                                             OOQL.CreateProperty("INNER_ORDER_DOC_SD.DELIVERED_BUSINESS_QTY"),
                                             OOQL.CreateProperty("INNER_ORDER_DOC.DOC_DATE"),
                                             OOQL.CreateProperty("INNER_ORDER_DOC_SD.BUSINESS_QTY"),
                                             OOQL.CreateProperty("INNER_ORDER_DOC_D.ITEM_ID"),
                                             OOQL.CreateProperty("INNER_ORDER_DOC_D.BUSINESS_UNIT_ID"),
                                             OOQL.CreateProperty("ITEM.STOCK_UNIT_ID"),
                                             OOQL.CreateProperty("INNER_ORDER_DOC_D.ITEM_FEATURE_ID"),
                                             OOQL.CreateProperty("INNER_ORDER_DOC_SD.INNER_ORDER_DOC_SD_ID"),
                                             OOQL.CreateProperty("INNER_ORDER_DOC_SD.SequenceNumber"),
                                             OOQL.CreateProperty("INNER_ORDER_DOC_D.SequenceNumber", "SequenceNumberD"),
                                             OOQL.CreateProperty("INNER_ORDER_DOC.DOC_NO"),
                                             OOQL.CreateProperty("INNER_ORDER_DOC_SD.INNER_ORDER_DOC_SD_ID"),
                                             OOQL.CreateProperty("INNER_ORDER_DOC_SD.REFERENCE_SOURCE_ID.ROid", "REFERENCE_SOURCE_ID_ROid"),
                                             OOQL.CreateProperty("INNER_ORDER_DOC_SD.REFERENCE_SOURCE_ID.RTK", "REFERENCE_SOURCE_ID_RTK")
                                             , OOQL.CreateProperty("INNER_ORDER_DOC_SD.PLAN_STATUS", "PLAN_STATUS") //20180711 add by xuyang for P001-170930002
                                             )
                                     .From("INNER_ORDER_DOC.INNER_ORDER_DOC_D.INNER_ORDER_DOC_SD", "INNER_ORDER_DOC_SD")
                                     .LeftJoin("INNER_ORDER_DOC.INNER_ORDER_DOC_D", "INNER_ORDER_DOC_D")
                                     .On(OOQL.CreateProperty("INNER_ORDER_DOC_SD.INNER_ORDER_DOC_D_ID") == OOQL.CreateProperty("INNER_ORDER_DOC_D.INNER_ORDER_DOC_D_ID"))
                                     .LeftJoin("INNER_ORDER_DOC")
                                     .On(OOQL.CreateProperty("INNER_ORDER_DOC_D.INNER_ORDER_DOC_ID") == OOQL.CreateProperty("INNER_ORDER_DOC.INNER_ORDER_DOC_ID"))
                                     .LeftJoin("ITEM")
                                     .On(OOQL.CreateProperty("INNER_ORDER_DOC_D.ITEM_ID") == OOQL.CreateProperty("ITEM.ITEM_ID"))
                                     //20171205 add by xuyang for T001-171116002 ==begin==
                                     .LeftJoin("ITEM_PLANT", "IP")
                                        .On((OOQL.CreateProperty("IP.ITEM_ID") == OOQL.CreateProperty("INNER_ORDER_DOC_D.ITEM_ID"))
                                        & (OOQL.CreateProperty("INNER_ORDER_DOC_SD.DELIVERY_PLANT_ID") == OOQL.CreateProperty("IP.Owner_Org.ROid")))
                                     //20171205 add by xuyang for T001-171116002 ==end==
                                     //20170613 modi by xuyang for S001-170607003 ==begin==
                                     //.Where(OOQL.AuthFilter("INNER_ORDER_DOC.INNER_ORDER_DOC_D.INNER_ORDER_DOC_SD", "INNER_ORDER_DOC_SD") & OOQL.CreateProperty("INNER_ORDER_DOC_SD.INNER_ORDER_DOC_SD_ID").In(list.ToArray()))
                                     //.Where(OOQL.AuthFilter("INNER_ORDER_DOC.INNER_ORDER_DOC_D.INNER_ORDER_DOC_SD", "INNER_ORDER_DOC_SD") & qrCond)  //20171205 mark by xuyang for T001-171116002
                                     //20170613 modi by xuyang for S001-170607003 ==end==
                                     //20171205 add by xuyang for T001-171116002 ==begin==
                                     .Where(OOQL.AuthFilter("INNER_ORDER_DOC.INNER_ORDER_DOC_D.INNER_ORDER_DOC_SD", "INNER_ORDER_DOC_SD") & qrCond)
                                        //& OOQL.CreateProperty("IP.ORDER_POLICY") == OOQL.CreateConstants("P")   //20171208 mark by xuyang for B001-171208011 补齐条件 
                                        ////& OOQL.CreateProperty("IP.ITEM_PROPERTY") != OOQL.CreateConstants("F")   //20171208 mark by xuyang for B001-171208011 补齐条件
                                     //20171205 add by xuyang for T001-171116002 ==end==
                                     //20170113 modi by xuyang for B31-170112014 ==begin==
                                     //.OrderBy(new OrderByItem[] { new OrderByItem(OOQL.CreateProperty("INNER_ORDER_DOC_D.ITEM_ID"), SortType.Desc) });
                                     .OrderBy(new OrderByItem[] { new OrderByItem(OOQL.CreateProperty("INNER_ORDER_DOC_SD.PLAN_SHIP_DATE"), SortType.Asc) });
                                     //20170113 modi by xuyang for B31-170112014 ==end==
                    coll = queryService.ExecuteDependencyObject(nodeSelect);
                    foreach (var item in coll) {
                        num++;
                        orderNum += 10;
                        Dictionary<string, QueryProperty> dicInsert = new Dictionary<string, QueryProperty>();
                        dicInsert.Add("MDS_D_ID", OOQL.CreateConstants(primaryKeyService.CreateId()));
                        dicInsert.Add("SEQ", OOQL.CreateConstants(num));
                        dicInsert.Add("PRIORITY_ORDER", OOQL.CreateConstants(orderNum.ToStringExtension().PadLeft(6, '0')));
                        dicInsert.Add("DEMAND_START_DATE", OOQL.CreateConstants(item["PLAN_SHIP_DATE"]));
                        dicInsert.Add("DEMAND_DATE", OOQL.CreateConstants(item["PLAN_SHIP_DATE"]));
                        dicInsert.Add("DEMAND_QTY", OOQL.CreateConstants(item["DELIVERED_BUSINESS_QTY"]));
                        dicInsert.Add("DOC_DATE", OOQL.CreateConstants(item["DOC_DATE"]));
                        dicInsert.Add("ORIGINAL_DEMAND_QTY", OOQL.CreateConstants(item["BUSINESS_QTY"]));
                        dicInsert.Add("OFFSET_QTY", OOQL.CreateConstants(0));
                        dicInsert.Add("OFFSET_STATUS", OOQL.CreateConstants("1"));
                        dicInsert.Add("APS_FLAG", OOQL.CreateConstants(true));
                        dicInsert.Add("START_DATE", OOQL.CreateConstants(item["PLAN_SHIP_DATE"]));
                        dicInsert.Add("END_DATE", OOQL.CreateConstants(item["PLAN_SHIP_DATE"]));
                        dicInsert.Add("DELIVERED_QTY", OOQL.CreateConstants(item["DELIVERED_BUSINESS_QTY"]));
                        dicInsert.Add("INVENTORY_QTY", Formulas.Ext("UNIT_CONVERT", new object[]{ OOQL.CreateConstants(item["ITEM_ID"])
                                                                                                                            , OOQL.CreateConstants(item["BUSINESS_UNIT_ID"])
                                                                                                                            , OOQL.CreateConstants(item["BUSINESS_QTY"])-OOQL.CreateConstants(item["DELIVERED_BUSINESS_QTY"])
                                                                                                                            , OOQL.CreateConstants(item["STOCK_UNIT_ID"])
                                                                                                                            ,OOQL.CreateConstants(0)}));
                        dicInsert.Add("ITEM_ID", OOQL.CreateConstants(item["ITEM_ID"]));
                        dicInsert.Add("ITEM_FEATURE_ID", OOQL.CreateConstants(item["ITEM_FEATURE_ID"]));
                        dicInsert.Add("UNIT_ID", OOQL.CreateConstants(item["STOCK_UNIT_ID"]));
                        dicInsert.Add("FAMILY_ITEM_ID", OOQL.CreateConstants(Maths.GuidDefaultValue()));
                        dicInsert.Add("PLAN_DELIVERY_DATE", OOQL.CreateConstants(item["PLAN_SHIP_DATE"]));
                        dicInsert.Add("SOURCE_ID.RTK", OOQL.CreateConstants("INNER_ORDER_DOC.INNER_ORDER_DOC_D.INNER_ORDER_DOC_SD"));
                        dicInsert.Add("SOURCE_ID.ROid", OOQL.CreateConstants(item["INNER_ORDER_DOC_SD_ID"]));
                        dicInsert.Add("MDS_ID", OOQL.CreateConstants(MDS_ID));
                        dicInsert.Add("NETTING_TYPE", OOQL.CreateConstants(args.Context.Parameters["MRP_REQUIREMENT_CALCULATION"].Value));
                        dicInsert.Add("DEMAND", OOQL.CreateConstants(item["DOC_NO"].ToStringExtension() + "-" + item["SequenceNumberD"].ToStringExtension() + "-" + item["SequenceNumber"].ToStringExtension()));
                        dicInsert.Add("ORI_SOURCE_ID.ROid", OOQL.CreateConstants(item["REFERENCE_SOURCE_ID_ROid"]));
                        dicInsert.Add("ORI_SOURCE_ID.RTK", OOQL.CreateConstants(item["REFERENCE_SOURCE_ID_RTK"]));
                        dicInsert.Add("BUSINESS_UNIT_ID", OOQL.CreateConstants(item["BUSINESS_UNIT_ID"]));
                        //20150818 add by wangrm for B001-150818001 begin
                        dicInsert.Add("ApproveStatus", OOQL.CreateConstants("Y"));
                        dicInsert.Add("ApproveBy", OOQL.CreateConstants(logonSrc.CurrentUserId));
                        dicInsert.Add("ApproveDate", OOQL.CreateConstants(datetimeSrc.Now, GeneralDBType.DateTime));
                        //20150818 add by wangrm for B001-150818001  end
                        dicInsert.Add("PLAN_STATUS", OOQL.CreateConstants(item["PLAN_STATUS"].ToStringExtension()));//20180711 add by xuyang for P001-170930002
                        QueryNode insertNode = OOQL.Insert("MDS.MDS_D", dicInsert.Keys.ToArray()).Values(dicInsert.Values.ToArray());
                        //queryService.ExecuteNoQuery(insertNode);//20190227 mark by xuyang for B31-190227012
                        queryService.ExecuteNoQueryWithManageProperties(insertNode); //20190227 add by xuyang for B31-190227012
                    }
                    break;
                #endregion
                #region 生产预测
                case "4":
                    sources = args.Context.Parameters["PP_B003_FORECAST_DS"].Value as DependencyObjectCollection;
                    foreach (var item in sources) {
                        list.Add(OOQL.CreateConstants(item["FORECAST_D_ID"]));
                    }
                    nodeSelect = OOQL.Select(OOQL.CreateProperty("FORECAST_D.START_DATE"),
                                             OOQL.CreateProperty("FORECAST_D.END_DATE"),
                                             OOQL.CreateProperty("FORECAST.FORECAST_DATE"),
                                             OOQL.CreateProperty("FORECAST_D.FORECAST_QTY"),
                                             //20190802 add by xuyang for B001-190730021 ==begin==
                                             Formulas.IsNull(OOQL.CreateProperty("PB_D.ITEM_ID"), OOQL.CreateProperty("FORECAST_D.ITEM_ID"), "ITEM_ID"),
                                             Formulas.IsNull(OOQL.CreateProperty("PB_D.ITEM_FEATURE_ID"), OOQL.CreateProperty("FORECAST_D.ITEM_FEATURE_ID"), "ITEM_FEATURE_ID"),
                                             OOQL.CreateProperty("FORECAST.Owner_Org.ROid", "Owner_Org_ROid"),
                                             Formulas.Case(null, OOQL.CreateProperty("PB_D.PLAN_RATE") * Formulas.Ext("UNIT_CONVERT", OOQL.CreateProperty("FORECAST_D.ITEM_ID"), OOQL.CreateProperty("FORECAST_D.UNIT_ID"), OOQL.CreateProperty("FORECAST_D.FORECAST_QTY"), OOQL.CreateProperty("ITEM_PB.STOCK_UNIT_ID"), OOQL.CreateConstants(0)),
                                                 OOQL.CreateCaseArray(
                                                    OOQL.CreateCaseItem(OOQL.CreateProperty("PB_D.PLAN_RATE").IsNull(), 
                                                        Formulas.Ext("UNIT_CONVERT", OOQL.CreateProperty("FORECAST_D.ITEM_ID"), OOQL.CreateProperty("FORECAST_D.UNIT_ID"), OOQL.CreateProperty("FORECAST_D.FORECAST_QTY"), OOQL.CreateProperty("ITEM_FC.STOCK_UNIT_ID"), OOQL.CreateConstants(0)))
                                             ), "QTY"),
                                             OOQL.CreateProperty("IP.ITEM_PROPERTY"),
                                             //20190802 add by xuyang for B001-190730021 ==end==
                                             //OOQL.CreateProperty("FORECAST_D.ITEM_ID"), //20190802 mark by xuyang for B001-190730021
                                             OOQL.CreateProperty("FORECAST_D.UNIT_ID"),
                                             //OOQL.CreateProperty("ITEM.STOCK_UNIT_ID"),
                                             //OOQL.CreateProperty("FORECAST_D.ITEM_FEATURE_ID"),//20190802 mark by xuyang for B001-190730021
                                             OOQL.CreateProperty("FORECAST_D.FORECAST_D_ID"),
                                             OOQL.CreateProperty("FORECAST.FORECAST_DATE"),
                                             OOQL.CreateProperty("TIME_BUCKET.TIME_BUCKET_CODE"),
                                             OOQL.CreateProperty("PB_D.PLAN_RATE"),
                                             OOQL.CreateProperty("ITEM_FC.STOCK_UNIT_ID", "ITEM_FC_STOCK_UNIT_ID"),
                                             OOQL.CreateProperty("ITEM_PB.STOCK_UNIT_ID", "ITEM_PB_STOCK_UNIT_ID"),
                                             OOQL.CreateProperty("PB_D.ITEM_ID", "PB_D_ITEM_ID"),
                                             OOQL.CreateProperty("PB_D.ITEM_FEATURE_ID", "PB_D_ITEM_FEATURE_ID"),
                                             OOQL.CreateProperty("PLANT.PLANT_CODE"),
                                             Formulas.IsNull(OOQL.CreateProperty("PB_D.ITEM_ID"), OOQL.CreateProperty("FORECAST_D.ITEM_ID"), "TEMP_ITEM_ID"),
                                             Formulas.IsNull(OOQL.CreateProperty("PB_D.ITEM_FEATURE_ID"), OOQL.CreateProperty("FORECAST_D.ITEM_FEATURE_ID"), "TEMP_ITEM_FEATURE_ID"),
                                             Formulas.IsNull(OOQL.CreateProperty("ITEM_PB.STOCK_UNIT_ID"), OOQL.CreateProperty("ITEM_FC.STOCK_UNIT_ID"), "TEMP_STOCK_UNIT_ID"),
                                             OOQL.CreateProperty("PB_D.PLAN_BOM_D_ID")
                                             )
                                     .From("FORECAST.FORECAST_D", "FORECAST_D")
                                     .LeftJoin("FORECAST")
                                     .On(OOQL.CreateProperty("FORECAST.FORECAST_ID") == OOQL.CreateProperty("FORECAST_D.FORECAST_ID"))
                                     .LeftJoin("TIME_BUCKET")
                                     .On(OOQL.CreateProperty("FORECAST.TIME_BUCKET_ID") == OOQL.CreateProperty("TIME_BUCKET.TIME_BUCKET_ID"))
                                     .LeftJoin("PLAN_BOM", "PB")
                                     .On(OOQL.CreateProperty("PB.ITEM_ID") == OOQL.CreateProperty("FORECAST_D.ITEM_ID"))
                                     .LeftJoin("PLAN_BOM.PLAN_BOM_D", "PB_D")
                                     .On(OOQL.CreateProperty("PB_D.PLAN_BOM_ID") == OOQL.CreateProperty("PB.PLAN_BOM_ID"))
                                     .LeftJoin("PLANT")
                                     .On(OOQL.CreateProperty("PLANT.PLANT_ID") == OOQL.CreateProperty("FORECAST.Owner_Org.ROid"))
                                     .LeftJoin("ITEM", "ITEM_FC")
                                     .On(OOQL.CreateProperty("FORECAST_D.ITEM_ID") == OOQL.CreateProperty("ITEM_FC.ITEM_ID"))
                                     .LeftJoin("ITEM", "ITEM_PB")
                                     .On(OOQL.CreateProperty("PB.ITEM_ID") == OOQL.CreateProperty("ITEM_PB.ITEM_ID"))
                                     .LeftJoin("ITEM", "ITEM_PB_D")
                                     .On(OOQL.CreateProperty("PB_D.ITEM_ID") == OOQL.CreateProperty("ITEM_PB_D.ITEM_ID"))
                                     //20190801 add by xuyang for B001-190730021 ==begin==
                                     .LeftJoin("ITEM_PLANT","IP")
                                     .On(OOQL.CreateProperty("IP.Owner_Org.ROid") == OOQL.CreateProperty("FORECAST.Owner_Org.ROid")
                                        & OOQL.CreateProperty("IP.ITEM_ID") == Formulas.IsNull(OOQL.CreateProperty("PB.ITEM_ID"), OOQL.CreateProperty("FORECAST_D.ITEM_ID")))
                                    //20190801 add by xuyang for B001-190730021 ==end==
                                     .Where(OOQL.AuthFilter("FORECAST.FORECAST_D", "FORECAST_D") & OOQL.CreateProperty("FORECAST_D.FORECAST_D_ID").In(list.ToArray()))
                                     //20170113 modi by xuyang for B31-170112014 ==begin==
                                     //.OrderBy(new OrderByItem[] { new OrderByItem(OOQL.CreateProperty("FORECAST_D.ITEM_ID"), SortType.Desc) });
                                     .OrderBy(new OrderByItem[] { new OrderByItem(OOQL.CreateProperty("FORECAST_D.END_DATE"), SortType.Asc) });
                                     //20170113 modi by xuyang for B31-170112014 ==end==
                    coll = queryService.ExecuteDependencyObject(nodeSelect);
                    IBomExtractAndAutoInsteadService bomExtractAndAutoInstead = this.GetServiceForThisTypeKey<IBomExtractAndAutoInsteadService>();//20190801 add by xuyang for B001-190730021
                    ISysParameterService sysPara = this.GetService<ISysParameterService>();//20190801 add by xuyang for B001-190730021
                    foreach (var item in coll) {
                        //20190801 add by xuyang for B001-190730021 ==begin==
                        if (item["ITEM_PROPERTY"].ToStringExtension() == "Y") {
                            BomExtractAutoInsteadParameters para = new BomExtractAutoInsteadParameters(item["ITEM_ID"], logonSrc.CurrentUserId);
                            para.CurrentUserId = logonSrc.CurrentUserId;
                            para.ItemId = item["ITEM_ID"];
                            para.PlantId = item["Owner_Org_ROid"];
                            para.ItemFeatureId = item["ITEM_FEATURE_ID"];
                            para.EffectiveDate = datetimeSrc.NowDate;
                            para.ExtractMode = ExtractMode.SingleStage;//’1’单阶
                            para.PlanQty = item["QTY"].ToDecimal();
                            para.IsHasLoss = true;
                            para.IsMOAutoReplace = sysPara.GetValue("MO_AUTO_REPLACE", item["Owner_Org_ROid"]).ToBoolean();
                            DependencyObjectCollection queryBomExtractData = bomExtractAndAutoInstead.BomExtractAndAutoInstead(para);
                            foreach (DependencyObject dep in queryBomExtractData) {
                                num++;
                                orderNum += 10;
                                Dictionary<string, QueryProperty> dicInsert = new Dictionary<string, QueryProperty>();
                                dicInsert.Add("MDS_D_ID", OOQL.CreateConstants(primaryKeyService.CreateId()));
                                dicInsert.Add("SEQ", OOQL.CreateConstants(num));
                                dicInsert.Add("PRIORITY_ORDER", OOQL.CreateConstants(orderNum.ToStringExtension().PadLeft(6, '0')));
                                dicInsert.Add("DEMAND_START_DATE", OOQL.CreateConstants(item["START_DATE"]));
                                dicInsert.Add("DEMAND_DATE", OOQL.CreateConstants(item["END_DATE"]));
                                dicInsert.Add("DEMAND_QTY", OOQL.CreateConstants(0m));
                                dicInsert.Add("DOC_DATE", OOQL.CreateConstants(item["FORECAST_DATE"]));
                                dicInsert.Add("ORIGINAL_DEMAND_QTY", OOQL.CreateConstants(dep["REQUIRED_QTY"]));
                                dicInsert.Add("OFFSET_QTY", OOQL.CreateConstants(0m));
                                dicInsert.Add("OFFSET_STATUS", OOQL.CreateConstants("1"));
                                dicInsert.Add("APS_FLAG", OOQL.CreateConstants(true));
                                dicInsert.Add("START_DATE", OOQL.CreateConstants(item["START_DATE"]));
                                dicInsert.Add("END_DATE", OOQL.CreateConstants(item["END_DATE"]));
                                dicInsert.Add("DELIVERED_QTY", OOQL.CreateConstants(0));
                                dicInsert.Add("INVENTORY_QTY", OOQL.CreateConstants(dep["REQUIRED_QTY"]));
                                dicInsert.Add("ITEM_ID", OOQL.CreateConstants(dep["ITEM_ID"]));
                                dicInsert.Add("ITEM_FEATURE_ID", OOQL.CreateConstants(dep["ITEM_FEATURE_ID"]));
                                dicInsert.Add("UNIT_ID", OOQL.CreateConstants(dep["UNIT_ID"]));
                                if (Maths.IsEmpty(item["PLAN_BOM_D_ID"])) {
                                    dicInsert.Add("FAMILY_ITEM_ID", OOQL.CreateConstants(Maths.GuidDefaultValue()));
                                } else {
                                    dicInsert.Add("FAMILY_ITEM_ID", OOQL.CreateConstants(dep["ITEM_ID"]));
                                }
                                dicInsert.Add("PLAN_DELIVERY_DATE", OOQL.CreateConstants(item["END_DATE"]));
                                dicInsert.Add("SOURCE_ID.RTK", OOQL.CreateConstants("FORECAST.FORECAST_D"));
                                dicInsert.Add("SOURCE_ID.ROid", OOQL.CreateConstants(item["FORECAST_D_ID"]));
                                dicInsert.Add("MDS_ID", OOQL.CreateConstants(MDS_ID));
                                dicInsert.Add("NETTING_TYPE", OOQL.CreateConstants(args.Context.Parameters["MRP_REQUIREMENT_CALCULATION"].Value));
                                dicInsert.Add("DEMAND", OOQL.CreateConstants(item["FORECAST_DATE"].ToStringExtension() + "-" + item["PLANT_CODE"].ToStringExtension()
                                                                    + "-" + item["TIME_BUCKET_CODE"].ToStringExtension() + "-" + item["START_DATE"].ToStringExtension()));
                                dicInsert.Add("ORI_SOURCE_ID.ROid", OOQL.CreateConstants(item["FORECAST_D_ID"]));
                                dicInsert.Add("ORI_SOURCE_ID.RTK", OOQL.CreateConstants("FORECAST.FORECAST_D"));
                                dicInsert.Add("BUSINESS_UNIT_ID", OOQL.CreateConstants(dep["UNIT_ID"]));
                                dicInsert.Add("ApproveStatus", OOQL.CreateConstants("Y"));
                                dicInsert.Add("ApproveBy", OOQL.CreateConstants(logonSrc.CurrentUserId));
                                dicInsert.Add("ApproveDate", OOQL.CreateConstants(datetimeSrc.Now, GeneralDBType.DateTime));
                                dicInsert.Add("PLAN_STATUS", OOQL.CreateConstants(string.Empty));
                                QueryNode insertNode = OOQL.Insert("MDS.MDS_D", dicInsert.Keys.ToArray()).Values(dicInsert.Values.ToArray());
                                queryService.ExecuteNoQueryWithManageProperties(insertNode);
                            }

                        } else {
                        //20190801 add by xuyang for B001-190730021 ==end==
                            num++;
                            orderNum += 10;
                            Dictionary<string, QueryProperty> dicInsert = new Dictionary<string, QueryProperty>();
                            dicInsert.Add("MDS_D_ID", OOQL.CreateConstants(primaryKeyService.CreateId()));
                            dicInsert.Add("SEQ", OOQL.CreateConstants(num));
                            dicInsert.Add("PRIORITY_ORDER", OOQL.CreateConstants(orderNum.ToStringExtension().PadLeft(6, '0')));
                            dicInsert.Add("DEMAND_START_DATE", OOQL.CreateConstants(item["START_DATE"]));
                            dicInsert.Add("DEMAND_DATE", OOQL.CreateConstants(item["END_DATE"]));
                            dicInsert.Add("DEMAND_QTY", OOQL.CreateConstants(0));
                            dicInsert.Add("DOC_DATE", OOQL.CreateConstants(item["FORECAST_DATE"]));
                            dicInsert.Add("ORIGINAL_DEMAND_QTY", OOQL.CreateConstants(item["FORECAST_QTY"]));
                            dicInsert.Add("OFFSET_QTY", OOQL.CreateConstants(0));
                            dicInsert.Add("OFFSET_STATUS", OOQL.CreateConstants("1"));
                            dicInsert.Add("APS_FLAG", OOQL.CreateConstants(true));
                            dicInsert.Add("START_DATE", OOQL.CreateConstants(item["START_DATE"]));
                            dicInsert.Add("END_DATE", OOQL.CreateConstants(item["END_DATE"]));
                            dicInsert.Add("DELIVERED_QTY", OOQL.CreateConstants(0));
                            if (Maths.IsEmpty(item["PLAN_BOM_D_ID"])) {
                                dicInsert.Add("INVENTORY_QTY", Formulas.Ext("UNIT_CONVERT", new object[]{ OOQL.CreateConstants(item["ITEM_ID"])
                                                                                                                            , OOQL.CreateConstants(item["UNIT_ID"])
                                                                                                                            , OOQL.CreateConstants(item["FORECAST_QTY"])
                                                                                                                            , OOQL.CreateConstants(item["ITEM_FC_STOCK_UNIT_ID"])//20160303 modi by guojian for B31-160302009
                                                                                                                            ,OOQL.CreateConstants(0)}));
                            } else {//ITEM_PB_STOCK_UNIT_ID
                                dicInsert.Add("INVENTORY_QTY", OOQL.CreateConstants(item["PLAN_RATE"]) * Formulas.Ext("UNIT_CONVERT", new object[]{ OOQL.CreateConstants(item["ITEM_ID"])
                                                                                                                            , OOQL.CreateConstants(item["UNIT_ID"])
                                                                                                                            , OOQL.CreateConstants(item["FORECAST_QTY"])
                                                                                                                            , OOQL.CreateConstants(item["ITEM_PB_STOCK_UNIT_ID"])//20160303 modi by guojian for B31-160302009
                                                                                                                            ,OOQL.CreateConstants(0)}));
                            }
                            //dicInsert.Add("INVENTORY_QTY", Formulas.Case(null, OOQL.CreateConstants(item["PLAN_RATE"]) * Formulas.Ext("UNIT_CONVERT", new object[]{ OOQL.CreateConstants(item["ITEM_ID"])
                            //                                                                                                        , OOQL.CreateConstants(item["UNIT_ID"])
                            //                                                                                                        , OOQL.CreateConstants(item["FORECAST_QTY"])
                            //                                                                                                        , OOQL.CreateConstants(item["ITEM_FC_STOCK_UNIT_ID"])
                            //                                                                                                        ,OOQL.CreateConstants(0)}), new CaseItem[]{
                            //                                    new CaseItem(OOQL.CreateConstants(item["PLAN_RATE"]).IsNull(),Formulas.Ext("UNIT_CONVERT",  new object[]{ OOQL.CreateConstants(item["ITEM_ID"])
                            //                                                                                                        , OOQL.CreateConstants(item["UNIT_ID"])
                            //                                                                                                        , OOQL.CreateConstants(item["FORECAST_QTY"])
                            //                                                                                                        , OOQL.CreateConstants(item["ITEM_PB_STOCK_UNIT_ID"])
                            //                                                                                                        ,OOQL.CreateConstants(0)}))}));

                            dicInsert.Add("ITEM_ID", OOQL.CreateConstants(item["TEMP_ITEM_ID"]));
                            dicInsert.Add("ITEM_FEATURE_ID", OOQL.CreateConstants(item["TEMP_ITEM_FEATURE_ID"]));
                            dicInsert.Add("UNIT_ID", OOQL.CreateConstants(item["TEMP_STOCK_UNIT_ID"]));
                            if (Maths.IsEmpty(item["PLAN_BOM_D_ID"])) {
                                dicInsert.Add("FAMILY_ITEM_ID", OOQL.CreateConstants(Maths.GuidDefaultValue()));
                            } else {
                                dicInsert.Add("FAMILY_ITEM_ID", OOQL.CreateConstants(item["ITEM_ID"]));
                            }
                            //dicInsert.Add("FAMILY_ITEM_ID", Formulas.Case(null,OOQL.CreateConstants(Maths.GuidDefaultValue()), new CaseItem[] { 
                            //                                                                            new CaseItem(OOQL.CreateConstants(item["PLAN_RATE"]).IsNotNull(), OOQL.CreateConstants(item["ITEM_ID"])) }));
                            dicInsert.Add("PLAN_DELIVERY_DATE", OOQL.CreateConstants(item["END_DATE"]));
                            dicInsert.Add("SOURCE_ID.RTK", OOQL.CreateConstants("FORECAST.FORECAST_D"));
                            dicInsert.Add("SOURCE_ID.ROid", OOQL.CreateConstants(item["FORECAST_D_ID"]));
                            dicInsert.Add("MDS_ID", OOQL.CreateConstants(MDS_ID));
                            dicInsert.Add("NETTING_TYPE", OOQL.CreateConstants(args.Context.Parameters["MRP_REQUIREMENT_CALCULATION"].Value));
                            dicInsert.Add("DEMAND", OOQL.CreateConstants(item["FORECAST_DATE"].ToStringExtension() + "-" + item["PLANT_CODE"].ToStringExtension()
                                                                + "-" + item["TIME_BUCKET_CODE"].ToStringExtension() + "-" + item["START_DATE"].ToStringExtension()));
                            dicInsert.Add("ORI_SOURCE_ID.ROid", OOQL.CreateConstants(item["FORECAST_D_ID"]));
                            dicInsert.Add("ORI_SOURCE_ID.RTK", OOQL.CreateConstants("FORECAST.FORECAST_D"));
                            dicInsert.Add("BUSINESS_UNIT_ID", OOQL.CreateConstants(item["UNIT_ID"]));
                            //20150818 add by wangrm for B001-150818001 begin
                            dicInsert.Add("ApproveStatus", OOQL.CreateConstants("Y"));
                            dicInsert.Add("ApproveBy", OOQL.CreateConstants(logonSrc.CurrentUserId));
                            dicInsert.Add("ApproveDate", OOQL.CreateConstants(datetimeSrc.Now, GeneralDBType.DateTime));
                            //20150818 add by wangrm for B001-150818001  end
                            dicInsert.Add("PLAN_STATUS", OOQL.CreateConstants(string.Empty));//20180711 add by xuyang for P001-170930002
                            QueryNode insertNode = OOQL.Insert("MDS.MDS_D", dicInsert.Keys.ToArray()).Values(dicInsert.Values.ToArray());
                            //queryService.ExecuteNoQuery(insertNode);//20190227 mark by xuyang for B31-190227012
                            queryService.ExecuteNoQueryWithManageProperties(insertNode);//20190227 add by xuyang for B31-190227012
                        }
                    }
                    break;
                #endregion
                #region 工单
                case "5":
                    sources = args.Context.Parameters["PP_B003_MOS"].Value as DependencyObjectCollection;
                    foreach (var item in sources) {
                        list.Add(OOQL.CreateConstants(item["MO_ID"]));
                    }
                //20150814 modi by panzb --------------start------------------
                    nodeSelect = OOQL.Select(OOQL.CreateProperty("PLAN_START_DATE"),
                                            OOQL.CreateProperty("PLAN_COMPLETE_DATE"),
                                             OOQL.CreateProperty("COMPLETED_QTY"),
                                             OOQL.CreateProperty("DOC_DATE"),
                                             OOQL.CreateProperty("PLAN_QTY"),
                                             OOQL.CreateProperty("ITEM_ID"),
                                             OOQL.CreateProperty("MO_ID"),
                                             OOQL.CreateProperty("BUSINESS_UNIT_ID", "UNIT_ID"),
                                             OOQL.CreateProperty("ITEM_FEATURE_ID"),
                                             OOQL.CreateProperty("DOC_NO")
                                             )
                                     .From("MO","MO")
                                     .Where(OOQL.AuthFilter("MO","MO") & OOQL.CreateProperty("MO.MO_ID").In(list.ToArray()))
                                     //20170113 modi by xuyang for B31-170112014 ==begin==
                                     //.OrderBy(new OrderByItem[] { new OrderByItem(OOQL.CreateProperty("ITEM_ID"), SortType.Desc) });
                                     .OrderBy(new OrderByItem[] { new OrderByItem(OOQL.CreateProperty("PLAN_COMPLETE_DATE"), SortType.Asc) });
                                     //20170113 modi by xuyang for B31-170112014 ==end==
                    coll = queryService.ExecuteDependencyObject(nodeSelect);
                    foreach (var item in coll) {
                        num++;
                        orderNum += 10;
                        Dictionary<string, QueryProperty> dicInsert = new Dictionary<string, QueryProperty>();
                        dicInsert.Add("MDS_D_ID", OOQL.CreateConstants(primaryKeyService.CreateId()));
                        dicInsert.Add("SEQ", OOQL.CreateConstants(num));
                        dicInsert.Add("PRIORITY_ORDER", OOQL.CreateConstants(orderNum.ToStringExtension().PadLeft(6, '0')));
                        dicInsert.Add("DEMAND_START_DATE", OOQL.CreateConstants(item["PLAN_START_DATE"]));
                        dicInsert.Add("DEMAND_DATE", OOQL.CreateConstants(item["PLAN_COMPLETE_DATE"]));
                        dicInsert.Add("DEMAND_QTY", OOQL.CreateConstants(item["COMPLETED_QTY"]));
                        dicInsert.Add("DOC_DATE", OOQL.CreateConstants(item["DOC_DATE"]));
                        dicInsert.Add("ORIGINAL_DEMAND_QTY", OOQL.CreateConstants(item["PLAN_QTY"]));
                        dicInsert.Add("OFFSET_QTY", OOQL.CreateConstants(0));
                        dicInsert.Add("OFFSET_STATUS", OOQL.CreateConstants("1"));
                        dicInsert.Add("APS_FLAG", OOQL.CreateConstants(true));
                        dicInsert.Add("START_DATE", OOQL.CreateConstants(item["PLAN_START_DATE"]));
                        dicInsert.Add("END_DATE", OOQL.CreateConstants(item["PLAN_COMPLETE_DATE"]));
                        dicInsert.Add("DELIVERED_QTY", OOQL.CreateConstants(item["COMPLETED_QTY"]));
                        dicInsert.Add("INVENTORY_QTY", OOQL.CreateConstants(item["PLAN_QTY"].ToDecimal() - item["COMPLETED_QTY"].ToDecimal()));
                        dicInsert.Add("ITEM_ID", OOQL.CreateConstants(item["ITEM_ID"]));
                        dicInsert.Add("ITEM_FEATURE_ID", OOQL.CreateConstants(item["ITEM_FEATURE_ID"]));
                        dicInsert.Add("UNIT_ID", OOQL.CreateConstants(item["UNIT_ID"]));
                        dicInsert.Add("FAMILY_ITEM_ID", OOQL.CreateConstants(Maths.GuidDefaultValue()));
                        dicInsert.Add("PLAN_DELIVERY_DATE", OOQL.CreateConstants(item["PLAN_START_DATE"]));
                        dicInsert.Add("SOURCE_ID.RTK", OOQL.CreateConstants("MO"));
                        dicInsert.Add("SOURCE_ID.ROid", OOQL.CreateConstants(item["MO_ID"]));
                        dicInsert.Add("MDS_ID", OOQL.CreateConstants(MDS_ID));
                        dicInsert.Add("NETTING_TYPE", OOQL.CreateConstants(args.Context.Parameters["MRP_REQUIREMENT_CALCULATION"].Value));
                        dicInsert.Add("DEMAND", OOQL.CreateConstants(item["DOC_NO"]));
                        dicInsert.Add("ORI_SOURCE_ID.ROid", OOQL.CreateConstants(item["MO_ID"]));
                        dicInsert.Add("ORI_SOURCE_ID.RTK", OOQL.CreateConstants("MO"));
                        dicInsert.Add("BUSINESS_UNIT_ID", OOQL.CreateConstants(item["UNIT_ID"]));
                        //20150818 add by wangrm for B001-150818001 begin
                        dicInsert.Add("ApproveStatus", OOQL.CreateConstants("Y"));
                        dicInsert.Add("ApproveBy", OOQL.CreateConstants(logonSrc.CurrentUserId));
                        dicInsert.Add("ApproveDate", OOQL.CreateConstants(datetimeSrc.Now, GeneralDBType.DateTime));
                        //20150818 add by wangrm for B001-150818001  end
                        dicInsert.Add("PLAN_STATUS", OOQL.CreateConstants(string.Empty));//20180711 add by xuyang for P001-170930002
                        QueryNode insertNode = OOQL.Insert("MDS.MDS_D", dicInsert.Keys.ToArray()).Values(dicInsert.Values.ToArray());
                        //queryService.ExecuteNoQuery(insertNode);//20190227 mark by xuyang for B31-190227012
                        queryService.ExecuteNoQueryWithManageProperties(insertNode);//20190227 add by xuyang for B31-190227012
                    }
                    //20150814 modi by panzb ---------------end------------------
                    break;
                #endregion
                #region 生产计划
                case "6":
                    //20150814 modi by panzb ---------------start------------------
                    //sources = args.Context.Parameters["PLAN_SOURCE6_ID"].Value as object[];
                    //DependencyObjectCollection coll11 = args.Context.Parameters["PP_B003_SUGGESTION_PLANS"].Value as DependencyObjectCollection;
                    //foreach (var item in coll11) {
                    //    list.Add(OOQL.CreateConstants(item["DOC_NO"]));
                    //}
                    nodeSelect = OOQL.Select(OOQL.CreateProperty("MDS_D.DEMAND_START_DATE"),
                                             OOQL.CreateProperty("MDS_D.DEMAND_DATE"),
                                             OOQL.CreateProperty("MDS_D.DEMAND_QTY"),
                                             OOQL.CreateProperty("MDS_D.DOC_DATE"),
                                             OOQL.CreateProperty("MDS_D.ORIGINAL_DEMAND_QTY"),
                                             OOQL.CreateProperty("MDS_D.OFFSET_QTY"),
                                             OOQL.CreateProperty("MDS_D.APS_FLAG"),
                                             OOQL.CreateProperty("MDS_D.START_DATE"),
                                             OOQL.CreateProperty("MDS_D.END_DATE"),
                                             OOQL.CreateProperty("MDS_D.DELIVERED_QTY"),
                                             OOQL.CreateProperty("MDS_D.INVENTORY_QTY"),
                                             OOQL.CreateProperty("MDS_D.ITEM_FEATURE_ID"),
                                             OOQL.CreateProperty("MDS_D.ITEM_ID"),
                                             OOQL.CreateProperty("MDS_D.UNIT_ID"),
                                             OOQL.CreateProperty("MDS_D.FAMILY_ITEM_ID"),
                                             OOQL.CreateProperty("MDS_D.PLAN_DELIVERY_DATE"),
                                             OOQL.CreateProperty("MDS_D.SOURCE_ID.RTK","SOURCE_ID_RTK"),
                                             OOQL.CreateProperty("MDS_D.SOURCE_ID.ROid", "SOURCE_ID_ROid"),
                                             OOQL.CreateProperty("MDS_D.ORI_SOURCE_ID.RTK", "ORI_SOURCE_ID_RTK"),
                                             OOQL.CreateProperty("MDS_D.ORI_SOURCE_ID.ROid", "ORI_SOURCE_ID_ROid"),
                                             OOQL.CreateProperty("MDS_D.DEMAND"),
                                             #region 20180711 add by xuyang for P001-170930002
                                             Formulas.Case(null, OOQL.CreateConstants(string.Empty), 
                                                 new CaseItem[]{
                                                     new CaseItem(OOQL.CreateProperty("MDS_D.SOURCE_ID.RTK") == OOQL.CreateConstants("SALES_ORDER_DOC.SALES_ORDER_DOC_D.SALES_ORDER_DOC_SD"), 
                                                         OOQL.CreateProperty("SALES_ORDER_DOC_SD.PLAN_STATUS")),
                                                     new CaseItem(OOQL.CreateProperty("MDS_D.SOURCE_ID.RTK") == OOQL.CreateConstants("DISTRIBUTION_LIST"), 
                                                         OOQL.CreateProperty("DISTRIBUTION_LIST.PLAN_STATUS")),
                                                     new CaseItem(OOQL.CreateProperty("MDS_D.SOURCE_ID.RTK") == OOQL.CreateConstants("TRANSFER_REQUISITION.TRANSFER_REQUISITION_D"), 
                                                         OOQL.CreateProperty("TRANSFER_REQUISITION_D.PLAN_STATUS")),
                                                     new CaseItem(OOQL.CreateProperty("MDS_D.SOURCE_ID.RTK") == OOQL.CreateConstants("INNER_ORDER_DOC.INNER_ORDER_DOC_D.INNER_ORDER_DOC_SD"), 
                                                         OOQL.CreateProperty("INNER_ORDER_DOC_SD.PLAN_STATUS")),
                                                     new CaseItem(OOQL.CreateProperty("MDS_D.SOURCE_ID.RTK") == OOQL.CreateConstants("STOCKING_PLAN.STOCKING_PLAN_D"), 
                                                         OOQL.CreateProperty("STOCKING_PLAN.PLAN_STATUS"))
                                                 }, "PLAN_STATUS"
                                             )
                                             #endregion
                                      )
                                     .From("MDS.MDS_D", "MDS_D")
                                     .LeftJoin("MDS")
                                     .On(OOQL.CreateProperty("MDS_D.MDS_ID") == OOQL.CreateProperty("MDS.MDS_ID"))
                                     #region 20180711 add by xuyang for P001-170930002
                                     .LeftJoin("SALES_ORDER_DOC.SALES_ORDER_DOC_D.SALES_ORDER_DOC_SD", "SALES_ORDER_DOC_SD")
                                     .On(OOQL.CreateProperty("MDS_D.SOURCE_ID.ROid") == OOQL.CreateProperty("SALES_ORDER_DOC_SD.SALES_ORDER_DOC_SD_ID"))
                                     .LeftJoin("SALES_ORDER_DOC.SALES_ORDER_DOC_D", "SALES_ORDER_DOC_D")
                                     .On(OOQL.CreateProperty("SALES_ORDER_DOC_D.SALES_ORDER_DOC_D_ID") == OOQL.CreateProperty("SALES_ORDER_DOC_SD.SALES_ORDER_DOC_D_ID"))
                                     .LeftJoin("DISTRIBUTION_LIST", "DISTRIBUTION_LIST")
                                     .On(OOQL.CreateProperty("DISTRIBUTION_LIST.SOURCE_ID") == OOQL.CreateProperty("SALES_ORDER_DOC_SD.SALES_ORDER_DOC_SD_ID")
                                     & OOQL.CreateProperty("SALES_ORDER_DOC_D.KIT_DISTRIBUTION") == OOQL.CreateConstants(true, GeneralDBType.Boolean))
                                     .LeftJoin("INNER_ORDER_DOC.INNER_ORDER_DOC_D.INNER_ORDER_DOC_SD", "INNER_ORDER_DOC_SD")
                                     .On(OOQL.CreateProperty("MDS_D.SOURCE_ID.ROid") == OOQL.CreateProperty("INNER_ORDER_DOC_SD.INNER_ORDER_DOC_SD_ID"))
                                     .LeftJoin("TRANSFER_REQUISITION.TRANSFER_REQUISITION_D", "TRANSFER_REQUISITION_D")
                                     .On(OOQL.CreateProperty("MDS_D.SOURCE_ID.ROid") == OOQL.CreateProperty("TRANSFER_REQUISITION_D.TRANSFER_REQUISITION_D_ID"))
                                     .LeftJoin("STOCKING_PLAN.STOCKING_PLAN_D", "STOCKING_PLAN_D")
                                     .On(OOQL.CreateProperty("MDS_D.SOURCE_ID.ROid") == OOQL.CreateProperty("STOCKING_PLAN_D.STOCKING_PLAN_D_ID"))
                                     .LeftJoin("STOCKING_PLAN", "STOCKING_PLAN")
                                     .On(OOQL.CreateProperty("STOCKING_PLAN.STOCKING_PLAN_ID") == OOQL.CreateProperty("STOCKING_PLAN_D.STOCKING_PLAN_ID"))
                                     #endregion
                                     .Where(OOQL.AuthFilter("MDS.MDS_D", "MDS_D") & OOQL.CreateProperty("MDS.VERSION_TIMES") == OOQL.CreateConstants(args.Context.Parameters["MDS_VERSION"].Value) &
                                            OOQL.CreateProperty("MDS.PLAN_STRATEGY_ID") == OOQL.CreateConstants(args.Context.Parameters["Ex_PLAN_STRATEGY_ID"].Value) &
                                            OOQL.CreateProperty("MDS.Owner_Org.ROid") == OOQL.CreateConstants(args.Context.Parameters["PLANT_ID"].Value))
                                     .OrderBy(new OrderByItem[] { new OrderByItem(OOQL.CreateProperty("DEMAND_DATE"), SortType.Asc) });   //20170113 add by xuyang for B31-170112014 
                    coll = queryService.ExecuteDependencyObject(nodeSelect);
                    foreach (var item in coll) {
                        num++;
                        orderNum += 10;
                        Dictionary<string, QueryProperty> dicInsert = new Dictionary<string, QueryProperty>();
                        dicInsert.Add("MDS_D_ID", OOQL.CreateConstants(primaryKeyService.CreateId()));
                        dicInsert.Add("SEQ", OOQL.CreateConstants(num));
                        dicInsert.Add("PRIORITY_ORDER", OOQL.CreateConstants(orderNum.ToStringExtension().PadLeft(6, '0')));
                        dicInsert.Add("DEMAND_START_DATE", OOQL.CreateConstants(item["DEMAND_START_DATE"]));
                        dicInsert.Add("DEMAND_DATE", OOQL.CreateConstants(item["DEMAND_DATE"]));
                        dicInsert.Add("DEMAND_QTY", OOQL.CreateConstants(item["DEMAND_QTY"]));
                        dicInsert.Add("DOC_DATE", OOQL.CreateConstants(item["DOC_DATE"]));
                        dicInsert.Add("ORIGINAL_DEMAND_QTY", OOQL.CreateConstants(item["ORIGINAL_DEMAND_QTY"]));
                        dicInsert.Add("OFFSET_QTY", OOQL.CreateConstants(item["OFFSET_QTY"]));
                        dicInsert.Add("OFFSET_STATUS", OOQL.CreateConstants(item["APS_FLAG"]));
                        dicInsert.Add("APS_FLAG", OOQL.CreateConstants(item["APS_FLAG"]));
                        dicInsert.Add("START_DATE", OOQL.CreateConstants(item["START_DATE"]));
                        dicInsert.Add("END_DATE", OOQL.CreateConstants(item["END_DATE"]));
                        dicInsert.Add("DELIVERED_QTY", OOQL.CreateConstants(item["DELIVERED_QTY"]));
                        dicInsert.Add("INVENTORY_QTY", OOQL.CreateConstants(item["INVENTORY_QTY"].ToDecimal()));
                        dicInsert.Add("ITEM_ID", OOQL.CreateConstants(item["ITEM_ID"]));
                        dicInsert.Add("ITEM_FEATURE_ID", OOQL.CreateConstants(item["ITEM_FEATURE_ID"]));
                        dicInsert.Add("UNIT_ID", OOQL.CreateConstants(item["UNIT_ID"]));
                        dicInsert.Add("FAMILY_ITEM_ID", OOQL.CreateConstants(item["FAMILY_ITEM_ID"]));
                        dicInsert.Add("PLAN_DELIVERY_DATE", OOQL.CreateConstants(item["PLAN_DELIVERY_DATE"]));
                        dicInsert.Add("SOURCE_ID.RTK", OOQL.CreateConstants(item["SOURCE_ID_RTK"]));
                        dicInsert.Add("SOURCE_ID.ROid", OOQL.CreateConstants(item["SOURCE_ID_ROid"]));
                        dicInsert.Add("MDS_ID", OOQL.CreateConstants(MDS_ID));
                        dicInsert.Add("NETTING_TYPE", OOQL.CreateConstants(args.Context.Parameters["MRP_REQUIREMENT_CALCULATION"].Value));
                        dicInsert.Add("DEMAND", OOQL.CreateConstants(item["DEMAND"]));
                        dicInsert.Add("ORI_SOURCE_ID.ROid", OOQL.CreateConstants(item["ORI_SOURCE_ID_ROid"]));
                        dicInsert.Add("ORI_SOURCE_ID.RTK", OOQL.CreateConstants(item["ORI_SOURCE_ID_RTK"]));
                        dicInsert.Add("BUSINESS_UNIT_ID", OOQL.CreateConstants(item["UNIT_ID"]));
                        //20150818 add by wangrm for B001-150818001 begin
                        dicInsert.Add("ApproveStatus", OOQL.CreateConstants("Y"));
                        dicInsert.Add("ApproveBy", OOQL.CreateConstants(logonSrc.CurrentUserId));
                        dicInsert.Add("ApproveDate", OOQL.CreateConstants(datetimeSrc.Now, GeneralDBType.DateTime));
                        //20150818 add by wangrm for B001-150818001  end
                        dicInsert.Add("PLAN_STATUS", OOQL.CreateConstants(item["PLAN_STATUS"]));//20180711 add by xuyang for P001-170930002
                        QueryNode insertNode = OOQL.Insert("MDS.MDS_D", dicInsert.Keys.ToArray()).Values(dicInsert.Values.ToArray());
                        //queryService.ExecuteNoQuery(insertNode);//20190227 mark by xuyang for B31-190227012
                        queryService.ExecuteNoQueryWithManageProperties(insertNode);//20190227 add by xuyang for B31-190227012
                    }
                    //20150814 modi by panzb ---------------end------------------
                    break;
                #endregion
                #region 备货计划 20171030 add by xuyang for P001-170926001
                case "8":
                    //20171208 modi by xuyang for B001-171208011 补齐条件 ==begin==
                    //qrCond = OOQL.CreateConstants("1", GeneralDBType.Int32) == OOQL.CreateConstants("1", GeneralDBType.Int32);
                    //if (!args.Context.Parameters["UNGEN_DOC"].Value.ToBoolean()) {
                    //    sources = args.Context.Parameters["PP_B003_STOCKING_PLAN"].Value as DependencyObjectCollection;
                    //    foreach (var item in sources) {
                    //        list.Add(OOQL.CreateConstants(item["STOCKING_PLAN_ID"]));
                    //    }
                    //    if (list.Count > 0)
                    //        qrCond = OOQL.CreateProperty("SP_D.STOCKING_PLAN_ID").In(list.ToArray());
                    //} else {
                    //    qrCond = OOQL.CreateProperty("SP.PLAN_STATUS").In(OOQL.CreateConstants(1), OOQL.CreateConstants(5));  //20171205 modi by xuyang for T001-171116002 去掉OOQL.CreateConstants(2) OLD: In(OOQL.CreateConstants(1), OOQL.CreateConstants(2), OOQL.CreateConstants(5))
                    //}
                    //qrCond = OOQL.AuthFilter("STOCKING_PLAN", "SP")  & OOQL.CreateConstants("1", GeneralDBType.Int32) == OOQL.CreateConstants("1", GeneralDBType.Int32);//20180522 mark by xuyang for T001-180509003
                    qrCond = OOQL.AuthFilter("STOCKING_PLAN", "SP") & OOQL.CreateProperty("SP_D.STOCKING_QTY") > OOQL.CreateConstants(0m); //20180522 add by xuyang for T001-180509003
                    if (!args.Context.Parameters["UNGEN_DOC"].Value.ToBoolean()) {
                        sources = args.Context.Parameters["PP_B003_STOCKING_PLAN"].Value as DependencyObjectCollection;
                        foreach (var item in sources) {
                            list.Add(OOQL.CreateConstants(item["STOCKING_PLAN_ID"]));
                        }
                        if (list.Count > 0)
                            qrCond = qrCond & OOQL.CreateProperty("SP_D.STOCKING_PLAN_ID").In(list.ToArray());
                    } else {
                        qrCond = qrCond & OOQL.CreateProperty("SP.PLAN_STATUS").In(OOQL.CreateConstants("1"), OOQL.CreateConstants("5"))  //20171205 modi by xuyang for T001-171116002 去掉OOQL.CreateConstants(2) OLD: In(OOQL.CreateConstants(1), OOQL.CreateConstants(2), OOQL.CreateConstants(5))
                            //&OOQL.CreateProperty("STOCKING_PLAN.ApproveStatus") == OOQL.CreateConstants("Y")//20180522 mark by xuyang for T001-180509003
                            //& OOQL.CreateProperty("STOCKING_PLAN.Owner_Org.ROid") == OOQL.CreateConstants(args.Context.Parameters["PLANT_ID"].Value);//20180522 mark by xuyang for T001-180509003
                        & OOQL.CreateProperty("SP.ApproveStatus") == OOQL.CreateConstants("Y")                                                                   //20180522 add by xuyang for T001-180509003
                        & OOQL.CreateProperty("SP.Owner_Org.ROid") == OOQL.CreateConstants(args.Context.Parameters["PLANT_ID"].Value); //20180522 add by xuyang for T001-180509003
                    }
                    //20171208 modi by xuyang for B001-171208011 补齐条件 ==end==
                    nodeSelect = OOQL.Select(OOQL.CreateProperty("SP_D.FORECAST_DATE", "FORECAST_DATE"),
                                             OOQL.CreateProperty("SP.PLAN_DATE","PLAN_DATE"),
                                             OOQL.CreateProperty("SP_D.STOCKING_QTY","STOCKING_QTY"),
                                             OOQL.CreateProperty("SP_D.ITEM_ID","ITEM_ID"),
                                             OOQL.CreateProperty("SP_D.ITEM_FEATURE_ID","ITEM_FEATURE_ID"),
                                             OOQL.CreateProperty("SP_D.UNIT_ID","UNIT_ID"),
                                             OOQL.CreateProperty("SP_D.STOCKING_PLAN_D_ID","STOCKING_PLAN_D_ID"),
                                             OOQL.CreateProperty("SP.PLAN_STATUS", "PLAN_STATUS"),//20180711 add by xuyang for P001-170930002
                                             OOQL.CreateArithmetic(OOQL.CreateProperty("SP.STOCKING_PLAN_NO") + OOQL.CreateConstants("-"), Formulas.Cast(OOQL.CreateProperty("SP_D.SequenceNumber"),GeneralDBType.String), ArithmeticOperators.Plus,"DEMAND"))
                                     .From("STOCKING_PLAN", "SP")
                                     .LeftJoin("STOCKING_PLAN.STOCKING_PLAN_D","SP_D")
                                     .On(OOQL.CreateProperty("SP.STOCKING_PLAN_ID") == OOQL.CreateProperty("SP_D.STOCKING_PLAN_ID"))
                                     .Where(OOQL.AuthFilter("STOCKING_PLAN", "SP") & qrCond)
                                     .OrderBy(new OrderByItem[] { new OrderByItem(OOQL.CreateProperty("SP_D.FORECAST_DATE"), SortType.Asc) });   //20170113 add by xuyang for B31-170112014 
                    coll = queryService.ExecuteDependencyObject(nodeSelect);
                    foreach (var item in coll) {
                        num++;
                        orderNum += 10;
                        Dictionary<string, QueryProperty> dicInsert = new Dictionary<string, QueryProperty>();   
                        dicInsert.Add("MDS_D_ID", OOQL.CreateConstants(primaryKeyService.CreateId()));
                        dicInsert.Add("SEQ", OOQL.CreateConstants(num));
                        dicInsert.Add("PRIORITY_ORDER", OOQL.CreateConstants(orderNum.ToStringExtension().PadLeft(6, '0')));
                        dicInsert.Add("DEMAND_START_DATE", OOQL.CreateConstants(item["FORECAST_DATE"]));
                        dicInsert.Add("DEMAND_DATE", OOQL.CreateConstants(item["FORECAST_DATE"]));
                        dicInsert.Add("DEMAND_QTY", OOQL.CreateConstants(0m,GeneralDBType.Decimal));
                        dicInsert.Add("DOC_DATE", OOQL.CreateConstants(item["PLAN_DATE"]));
                        dicInsert.Add("ORIGINAL_DEMAND_QTY", OOQL.CreateConstants(item["STOCKING_QTY"]));
                        dicInsert.Add("OFFSET_QTY", OOQL.CreateConstants(0m,GeneralDBType.Decimal));
                        //dicInsert.Add("OFFSET_STATUS", OOQL.CreateConstants(string.Empty));  //20190426 mark by xuyang for B001-190425009
                        dicInsert.Add("OFFSET_STATUS", OOQL.CreateConstants("1"));                    //20190426 add by xuyang for B001-190425009
                        dicInsert.Add("APS_FLAG", OOQL.CreateConstants(1,GeneralDBType.Boolean));
                        dicInsert.Add("START_DATE", OOQL.CreateConstants(item["FORECAST_DATE"]));
                        dicInsert.Add("END_DATE", OOQL.CreateConstants(item["FORECAST_DATE"]));
                        dicInsert.Add("DELIVERED_QTY", OOQL.CreateConstants(0m, GeneralDBType.Decimal));
                        dicInsert.Add("INVENTORY_QTY", OOQL.CreateConstants(item["STOCKING_QTY"].ToDecimal()));
                        dicInsert.Add("ITEM_ID", OOQL.CreateConstants(item["ITEM_ID"]));
                        dicInsert.Add("ITEM_FEATURE_ID", OOQL.CreateConstants(item["ITEM_FEATURE_ID"]));
                        dicInsert.Add("UNIT_ID", OOQL.CreateConstants(item["UNIT_ID"]));
                        dicInsert.Add("FAMILY_ITEM_ID", OOQL.CreateConstants(Maths.GuidDefaultValue()));
                        dicInsert.Add("PLAN_DELIVERY_DATE", OOQL.CreateConstants(item["FORECAST_DATE"]));
                        dicInsert.Add("SOURCE_ID.RTK", OOQL.CreateConstants("STOCKING_PLAN.STOCKING_PLAN_D"));
                        dicInsert.Add("SOURCE_ID.ROid", OOQL.CreateConstants(item["STOCKING_PLAN_D_ID"]));
                        dicInsert.Add("MDS_ID", OOQL.CreateConstants(MDS_ID));
                        dicInsert.Add("NETTING_TYPE", OOQL.CreateConstants(args.Context.Parameters["MRP_REQUIREMENT_CALCULATION"].Value));
                        dicInsert.Add("DEMAND", OOQL.CreateConstants(item["DEMAND"]));
                        dicInsert.Add("ORI_SOURCE_ID.ROid", OOQL.CreateConstants(item["STOCKING_PLAN_D_ID"]));
                        dicInsert.Add("ORI_SOURCE_ID.RTK", OOQL.CreateConstants("STOCKING_PLAN.STOCKING_PLAN_D"));
                        dicInsert.Add("BUSINESS_UNIT_ID", OOQL.CreateConstants(item["UNIT_ID"]));
                        dicInsert.Add("ApproveStatus", OOQL.CreateConstants("Y"));
                        dicInsert.Add("ApproveBy", OOQL.CreateConstants(logonSrc.CurrentUserId));
                        dicInsert.Add("ApproveDate", OOQL.CreateConstants(datetimeSrc.Now, GeneralDBType.DateTime));
                        dicInsert.Add("PLAN_STATUS", OOQL.CreateConstants(item["PLAN_STATUS"].ToStringExtension()));//20180711 add by xuyang for P001-170930002
                        QueryNode insertNode = OOQL.Insert("MDS.MDS_D", dicInsert.Keys.ToArray()).Values(dicInsert.Values.ToArray());
                        //queryService.ExecuteNoQuery(insertNode);//20190227 mark by xuyang for B31-190227012
                        queryService.ExecuteNoQueryWithManageProperties(insertNode);//20190227 add by xuyang for B31-190227012
                    }
                    break;
                #endregion
            }
        }

        private object GetPLAN_STRATEGY_ID(FreeBatchEventsArgs args) {
            //20150508 add by panzb --------start--------
            QueryConditionGroup condition = OOQL.CreateProperty("PLAN_STRATEGY_ID") == OOQL.CreateConstants(args.Context.Parameters["PLAN_STRATEGY_ID"].Value) & OOQL.CreateConstants(1) == OOQL.CreateConstants(1);
            if (args.Context.Parameters["STRATEGY_MODE"].Value.ToStringExtension().Equals("4"))
                condition &= OOQL.CreateProperty("MDS_VERSION") == OOQL.CreateConstants(args.Context.Parameters["VERSION_TIMES"].Value);
            //20150508 add by panzb ---------end--------
            WhereNode querynode = OOQL.Select("BATCH_PLAN_STRATEGY_ID").From("BATCH_PLAN_STRATEGY","BATCH_PLAN_STRATEGY")//20150610 modi by liuxp FOR T001-150525001
                //.Where(OOQL.CreateProperty("PLAN_STRATEGY_ID") == OOQL.CreateConstants(args.Context.Parameters["PLAN_STRATEGY_ID"].Value));//20150508 mark by panzb
                .Where(OOQL.AuthFilter("BATCH_PLAN_STRATEGY","BATCH_PLAN_STRATEGY") & condition);//20150508 add by panzb
            return GetService<IQueryService>().ExecuteScalar(querynode);
        }

        private DependencyObjectCollection GetPLAN_STRATEGYList(object id) {
            WhereNode query = OOQL.Select(OOQL.CreateProperty("PLAN_STRATEGY_CODE"),
                                          OOQL.CreateProperty("PLAN_STRATEGY_NAME"),
                                          OOQL.CreateProperty("STRATEGY_MODE"),
                                          OOQL.CreateProperty("MDS_LEVEL"),
                                          OOQL.CreateProperty("MPS_LEVEL"),
                                          OOQL.CreateProperty("MRP_LEVEL"),
                                          OOQL.CreateProperty("MDS_OFFSET_RULE"),
                                          OOQL.CreateProperty("MDS_DEMAND_TIME_FENCE"),
                                          OOQL.CreateProperty("MDS_DEMAND_PRIORITY_ORDER"),
                                          OOQL.CreateProperty("MDS_DEMAND_TYPE"),
                                          OOQL.CreateProperty("MPS_DEMAND_SOURCE"),
                                          OOQL.CreateProperty("MPS_FORECAST_ALLOCATION"),
                                          OOQL.CreateProperty("MPS_MO_ALLOCATION"),
                                          OOQL.CreateProperty("MPS_SCHEDULING_STRATEGY"),
                                          OOQL.CreateProperty("MPS_LEAD_TIME"),
                                          OOQL.CreateProperty("MPS_TRANSFER_LOT_FLAG"),
                                          OOQL.CreateProperty("MPS_REPLACE_FLAG"),
                                          OOQL.CreateProperty("MPS_ALTERNATIVE_FLAG"),
                                          OOQL.CreateProperty("MRP_REQUIREMENT_CALCULATION"),
                                          OOQL.CreateProperty("MPS_REQUIREMENT_CALCULATION"),
                                          OOQL.CreateProperty("MPS_ATTRITION_RATE_FLAG"),
                                          //20131223 add by fuwei --------Begin
                                          OOQL.CreateProperty("CONSIDERED_LOCK_STOCK"),
                                          OOQL.CreateProperty("MDS_SALES_ORDER_FLAG"),
                                          OOQL.CreateProperty("MDS_FORECAST_FLAG"),
                                          OOQL.CreateProperty("MDS_TRANSFER_FLAG"),
                                          OOQL.CreateProperty("MDS_INNER_ORDER_FLAG"),
                                          OOQL.CreateProperty("MPS_SAFT_STOCK_FLAG"),
                                          OOQL.CreateProperty("MRP_SAFT_STOCK_FLAG")
                                          //20131223 add by fuwei --------End
                                          , OOQL.CreateProperty("MPS_SAFT_STOCK_PRIORITY")//20160901 add by wangrm for T001-160523001
                //20150610 add by liuxp FOR T001-150525001 -------------------------start-------------------------
                                          , OOQL.CreateProperty("MRP_SCHEDULING_STRATEGY")
                                          , OOQL.CreateProperty("PLAN_TYPE")
                                          , OOQL.CreateProperty("MRP_LOCK_RANGE")
                                          , OOQL.CreateProperty("MPS_LOCK_RANGE")
                //20150610 add by liuxp FOR T001-150525001 --------------------------end-------------------------
                //20151229 add by wangrm for T001-151228001 start
                                         , OOQL.CreateProperty("MRP_DEMAND_PRIORITY_ORDER")
                                         , OOQL.CreateProperty("MRP_MANUFACTURING_PART")
                                         , OOQL.CreateProperty("MRP_PROCESSING_PART")
                                         , OOQL.CreateProperty("MRP_PURCHASING_PART")
                                         , OOQL.CreateProperty("MRP_INNER_PURCHASING_PART")
                                         , OOQL.CreateProperty("MRP_TRANSFER_PART")
                                         , OOQL.CreateProperty("MRP_ADDED_DIFFERENCE")
                //20151229 add by wangrm for T001-151228001 end
                                         , OOQL.CreateProperty("INTERNAL_ITEM_PLAN")//20161212 add by wangrm for T001-161209001
                                         , OOQL.CreateProperty("MDS_STOCKING_PLAN_FLAG") //20171030 add by xuyang for P001-170926001
                                         , OOQL.CreateProperty("KEEP_EXIST_DEMAND_BALANCE") //20180711 add by xuyang for P001-170930002
                                         , OOQL.CreateProperty("SATISFY_LOWER_LEVEL") //20180711 add by xuyang for P001-170930002
                                          )
                .From("PLAN_STRATEGY","PLAN_STRATEGY")
                .Where(OOQL.AuthFilter("PLAN_STRATEGY","PLAN_STRATEGY") & OOQL.CreateProperty("PLAN_STRATEGY_ID") == OOQL.CreateConstants(id));
            return GetService<IQueryService>().ExecuteDependencyObject(query);
        }


        protected override FreeBatchEventsArgs CreateFreeBatchEventsArgs(BatchContext context) {
            return new FreeBatchEventsArgs(context);
        }
    }
}