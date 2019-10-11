//----------------------------------------------------------------
//<Author>xuyang</Author>
//<CreateDate>2017-12-19</CreateDate>
//<IssueNo>P001-170930001</IssueNo>
//<Description>查询数据接口</Description>
//----------------------------------------------------------------
//20190121 modi by xuyang for T001-181106001 检测新增字段是否存在，不存在则新增
//20190912 modi by zhaijxz for T001-190903001 新增字段同步检查
using System.ComponentModel;
using Digiwin.Common;
using Digiwin.Common.Torridity;

namespace Digiwin.ERP.X_PP_B003.Business {
    /// <summary>
    /// 生成备货计划查询服务接口  （UI端用）
    /// </summary>
    [TypeKeyOnly]
    [Description("数据查询服务接口")]
    public interface IQueryDataService {
        /// <summary>
        /// 查询计划批号
        /// </summary>
        /// <param name="plantId">工厂ID</param>
        /// <param name="planStrategyId">规划策略ID</param>
        /// <returns></returns>
        string QueryMdsVersionTimes(object plantId, object planStrategyId);

        //20190121 add by xuyang for T001-181106001 ==begin==
        /// <summary>
        /// 判断数据库表BATCH_PLAN_STRATEGY是否存在栏位 ERROR_TYPE 或ERROR_MSG；
        /// 判断数据库表SUGGESTION_PLAN是否存在栏位 SCHEDULED 或 SCHEDULED_QTY；
        /// 判断数据库表SUGGESTION_PLAN_RESOURCE是否存在栏位 RELEASED_QTY 或 RESCHED_QTY 或 RUDUCED_QTY；
        /// 判断数据库表MO_DAILY_PRODUCTION_QTY是否存在栏位 RESCHED_QTY 或 RUDUCED_QTY；
        /// 返回TRUE：存在 ，FALSE：不存在。目前仅支持判断SQL Server
        /// </summary>
        /// <returns></returns>
        bool CheckExistColumn(string tableName);//20190912 modi by zhaijxz for T001-190903001 添加参数tableName

        /// <summary>
        /// 数据库表中BATCH_PLAN_STRATEGY新增栏位ERROR_TYPE 和ERROR_MSG;
        /// 数据库表中SUGGESTION_PLAN新增栏位SCHEDULED 和SCHEDULED_QTY；
        /// 数据库表中SUGGESTION_PLAN_RESOURCE新增栏位RELEASED_QTY 和RESCHED_QTY和RUDUCED_QTY；
        /// 数据库表中MO_DAILY_PRODUCTION_QTY新增栏位RESCHED_QTY 和RUDUCED_QTY；
        /// (OOQL无法实现，只能执行sql脚本)
        /// </summary>
        void AddColumn(string tableName);//20190912 modi by zhaijxz for T001-190903001 添加参数tableName
        //20190121 add by xuyang for T001-181106001 ==end==
    }
}
