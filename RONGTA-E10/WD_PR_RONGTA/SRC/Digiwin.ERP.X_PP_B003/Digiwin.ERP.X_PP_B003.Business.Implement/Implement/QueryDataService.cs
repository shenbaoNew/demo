//----------------------------------------------------------------
//<Author>xuyang</Author>
//<CreateDate>2017-12-19</CreateDate>
//<IssueNo>P001-170930001</IssueNo>
//<Description>查询数据服务</Description>
//----------------------------------------------------------------
//20190121 modi by xuyang for T001-181106001 检测新增字段是否存在，不存在则新增
//20190912 modi by zhaijxz for T001-190903001 新增字段同步检查
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using Digiwin.Common;
using Digiwin.Common.Core;
using Digiwin.Common.Query2;
using Digiwin.Common.Torridity;
using Digiwin.Common.Torridity.DataSource;
using Digiwin.ERP.Common.Utils;

namespace Digiwin.ERP.X_PP_B003.Business.Implement {
    /// <summary>
    /// 生成备货计划查询服务实现  （UI端用）
    /// </summary>
    [SingleGetCreator]
    [ServiceClass(typeof(IQueryDataService))]
    [Description("数据查询服务实现")]
    internal sealed class QueryDataService : ServiceComponent, IQueryDataService {
        /// <summary>
        /// 查询计划批号
        /// </summary>
        /// <param name="plantId">工厂ID</param>
        /// <param name="planStrategyId">规划策略ID</param>
        /// <returns></returns>
        public string QueryMdsVersionTimes(object plantId, object planStrategyId){
            string result = string.Empty;
            QueryNode qryNode = OOQL.Select(1, OOQL.CreateProperty("M.VERSION_TIMES"))
                .From("MDS", "M")
                .Where(OOQL.CreateProperty("M.Owner_Org.ROid") == OOQL.CreateConstants(plantId)  //计划批号要求全局唯一因此不可插旗
                    & OOQL.CreateProperty("M.PLAN_STRATEGY_ID") == OOQL.CreateConstants(planStrategyId)
                    & OOQL.CreateProperty("M.ApproveStatus") == OOQL.CreateConstants("Y"));
            DependencyObjectCollection dpColl = this.GetService<IQueryService>().ExecuteDependencyObject(qryNode);
            if (dpColl.Count > 0) {
                result = dpColl[0]["VERSION_TIMES"].ToStringExtension();
            }
            return result;
        }

        //20190121 add by xuyang for T001-181106001 ==begin==
        /// <summary>
        /// 判断数据库表BATCH_PLAN_STRATEGY是否存在栏位 ERROR_TYPE 或ERROR_MSG；
        /// 判断数据库表SUGGESTION_PLAN是否存在栏位 SCHEDULED 或 SCHEDULED_QTY；
        /// 判断数据库表SUGGESTION_PLAN_RESOURCE是否存在栏位 RELEASED_QTY 或 RESCHED_QTY 或 RUDUCED_QTY；
        /// 判断数据库表MO_DAILY_PRODUCTION_QTY是否存在栏位 RESCHED_QTY 或 RUDUCED_QTY 或 ORIG_PROD_QTY；
        /// 返回TRUE：存在 ，FALSE：不存在。目前仅支持判断SQL Server
        /// </summary>
        /// <returns></returns>
        public bool CheckExistColumn(string tableName) {//20190912 modi by zhaijxz for T001-190903001 添加参数tableName
            bool flag = false;
            string strSqlExistField = string.Empty;//20190912 add by zhaijxz for T001-190903001 添加参数tableName
            DataBaseType dataBaseType = GetService<IQueryService>().DBType;
            if (dataBaseType == DataBaseType.MSSQL) {
                if (tableName == "BATCH_PLAN_STRATEGY") {//20190912 add by zhaijxz for T001-190903001
                    //数据库表BATCH_PLAN_STRATEGY 不存在 栏位 ERROR_TYPE 或ERROR_MSG
                    strSqlExistField = @"SELECT
                                                            s2.name
                                                        FROM
                                                            sysobjects AS s
                                                        INNER JOIN syscolumns AS s2
                                                            ON s.id = s2.id
                                                        WHERE
                                                            s.name = 'BATCH_PLAN_STRATEGY' AND
                                                            s2.name IN ( 'ERROR_TYPE'
                                                    , 'ERROR_MSG' 
                                                    )";
                }
                    //20190912 add by zhaijxz for T001-190903001 ==========begin==========
                else if (tableName == "SUGGESTION_PLAN") {
                    //数据库表SUGGESTION_PLAN 不存在 栏位 SCHEDULED 或SCHEDULED_QTY
                    strSqlExistField = @"SELECT
                                                            s2.name
                                                        FROM
                                                            sysobjects AS s
                                                        INNER JOIN syscolumns AS s2
                                                            ON s.id = s2.id
                                                        WHERE
                                                            s.name = 'SUGGESTION_PLAN' AND
                                                            s2.name IN ( 'SCHEDULED'
                                                    , 'SCHEDULED_QTY' 
                                                    )";
                } else if (tableName == "SUGGESTION_PLAN_RESOURCE") {
                    //数据库表SUGGESTION_PLAN_RESOURCE 不存在 栏位 RELEASED_QTY 或 RESCHED_QTY 或 RUDUCED_QTY
                    strSqlExistField = @"SELECT
                                                            s2.name
                                                        FROM
                                                            sysobjects AS s
                                                        INNER JOIN syscolumns AS s2
                                                            ON s.id = s2.id
                                                        WHERE
                                                            s.name = 'SUGGESTION_PLAN_RESOURCE' AND
                                                            s2.name IN ( 'RELEASED_QTY'
                                                    , 'RESCHED_QTY' 
                                                    ,'RUDUCED_QTY'
                                                    )";
                } else if (tableName == "MO_DAILY_PRODUCTION_QTY") {
                    //数据库表MO_DAILY_PRODUCTION_QTY 不存在 栏位 RESCHED_QTY 或 RUDUCED_QTY 或 ORIG_PROD_QTY
                    strSqlExistField = @"SELECT
                                                            s2.name
                                                        FROM
                                                            sysobjects AS s
                                                        INNER JOIN syscolumns AS s2
                                                            ON s.id = s2.id
                                                        WHERE
                                                            s.name = 'MO_DAILY_PRODUCTION_QTY' AND
                                                            s2.name IN ( 'RESCHED_QTY'
                                                    , 'RUDUCED_QTY' 
                                                    , 'ORIG_PROD_QTY'
                                                    )";
                }
                //20190912 add by zhaijxz for T001-190903001 ==========begin==========
                using (IConnectionService connSrv = GetService<IConnectionService>()) {
                    IDbCommand dbCommand = connSrv.CreateDbCommand(DatabaseServerOption.Default);
                    SqlCommand sqlCommand = dbCommand as SqlCommand;
                    sqlCommand.CommandType = CommandType.Text;
                    sqlCommand.CommandText = strSqlExistField;

                    object result = sqlCommand.ExecuteScalar();
                    if (result != null) {
                        flag = true;
                    }
                }
            }
            return flag;
        }

        /// <summary>
        /// 数据库表中BATCH_PLAN_STRATEGY新增栏位ERROR_TYPE 和ERROR_MSG;
        /// 数据库表中SUGGESTION_PLAN新增栏位SCHEDULED 和SCHEDULED_QTY；
        /// 数据库表中SUGGESTION_PLAN_RESOURCE新增栏位RELEASED_QTY 和RESCHED_QTY和RUDUCED_QTY；
        /// 数据库表中MO_DAILY_PRODUCTION_QTY新增栏位RESCHED_QTY 和RUDUCED_QTY和ORIG_PROD_QTY；
        /// (OOQL无法实现，只能执行sql脚本)
        /// </summary>
        public void AddColumn(string tableName) {//20190912 modi by zhaijxz for T001-190903001 添加参数tableName
            string strSqlExistField = string.Empty;
            DataBaseType dataBaseType = GetService<IQueryService>().DBType;
            if (dataBaseType == DataBaseType.MSSQL) {
                if (tableName == "BATCH_PLAN_STRATEGY") {//20190912 add by zhaijxz for T001-190903001
                    strSqlExistField = @"ALTER TABLE [dbo].[BATCH_PLAN_STRATEGY] ADD 
                    ERROR_TYPE NVARCHAR(20) DEFAULT ' ' NOT NULL,
                    ERROR_MSG NTEXT DEFAULT '' NOT NULL 
                    ;";
                }
                    //20190912 add by zhaijxz for T001-190903001 ==========begin==========
                else if (tableName == "SUGGESTION_PLAN") {
                    strSqlExistField = @"ALTER TABLE [dbo].[SUGGESTION_PLAN] ADD 
                    SCHEDULED bit DEFAULT 0 NOT NULL,
                    SCHEDULED_QTY decimal(16, 6) DEFAULT 0 NOT NULL 
                    ;";
                } else if (tableName == "SUGGESTION_PLAN_RESOURCE") {
                    strSqlExistField = @"ALTER TABLE [dbo].[SUGGESTION_PLAN_RESOURCE] ADD 
                    RELEASED_QTY decimal(16, 6) DEFAULT 0 NOT NULL,
                    RESCHED_QTY decimal(16, 6) DEFAULT 0 NOT NULL,
                    RUDUCED_QTY decimal(16, 6) DEFAULT 0 NOT NULL 
                    ;";
                } else if (tableName == "MO_DAILY_PRODUCTION_QTY") {
                    strSqlExistField = @"ALTER TABLE [dbo].[MO_DAILY_PRODUCTION_QTY] ADD 
                    RESCHED_QTY decimal(16, 6) DEFAULT 0 NOT NULL,
                    RUDUCED_QTY decimal(16, 6) DEFAULT 0 NOT NULL,
                    ORIG_PROD_QTY decimal(16, 6) DEFAULT 0 NOT NULL
                    ;";
                }
                //20190912 add by zhaijxz for T001-190903001 ==========begin==========
                using (IConnectionService connSrv = GetService<IConnectionService>()) {
                    IDbCommand dbCommand = connSrv.CreateDbCommand(DatabaseServerOption.Default);
                    SqlCommand sqlCommand = dbCommand as SqlCommand;
                    sqlCommand.CommandText = strSqlExistField;
                    sqlCommand.ExecuteNonQuery();
                }
            }
        }
        //20190121 add by xuyang for T001-181106001 ==end==
    }
}
