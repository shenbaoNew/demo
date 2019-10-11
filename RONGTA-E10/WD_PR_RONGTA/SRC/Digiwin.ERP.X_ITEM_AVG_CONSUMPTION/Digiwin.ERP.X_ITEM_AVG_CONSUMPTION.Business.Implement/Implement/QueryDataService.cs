//----------------------------------------------------------------
//<Author>xuyang</author>
//<CreateDate>19-10-9</createDate>
//<IssueNo></IssueNo>
//<Description>容大个案</description>
//----------------------------------------------------------------
//^_^ 20191009 add by xuyang for 容大个案
using System;
using System.ComponentModel;
using Digiwin.Common;
using Digiwin.Common.Query2;
using Digiwin.Common.Services;
using Digiwin.Common.Torridity;
namespace Digiwin.ERP.X_ITEM_AVG_CONSUMPTION.Business.Implement {
	/// <summary>
    ///QueryDataService 服务
    /// </summary>
    [ServiceClass(typeof(IQueryDataService))]
    [Description("Digiwin.ERP.X_ITEM_AVG_CONSUMPTION.Business.IQueryDataService接口服务实现")] 
    internal class QueryDataService : ServiceComponent, IQueryDataService {
        public DependencyObjectCollection QueryItemInfo() {
            QueryNode qryNode = OOQL.Select("ITEM_ID", "ITEM_CODE")
                .From("ITEM", "ITEM")
                .Where(OOQL.CreateProperty("ITEM.ApproveStatus") == OOQL.CreateConstants("Y"));
            return this.GetService<IQueryService>().ExecuteDependencyObject(qryNode);
        }
        public DependencyObjectCollection QueryPlantInfo() {
            QueryNode qryNode = OOQL.Select("PLANT_ID", "PLANT_CODE")
                .From("PLANT", "PLANT")
                .Where(OOQL.CreateProperty("PLANT.ApproveStatus") == OOQL.CreateConstants("Y"));
            return this.GetService<IQueryService>().ExecuteDependencyObject(qryNode);
        }

        /// <summary>
        /// 获取领料单信息
        /// </summary>
        /// <returns></returns>
        public int UpdateAvgConsumption(int month) {
            DateTime nowDate = DateTime.Now;
            DateTime beginDate = nowDate.AddMonths(-month);
            beginDate = new DateTime(beginDate.Year, beginDate.Month, 1);
            DateTime endDate = new DateTime(nowDate.Year, nowDate.Month, 1, 12, 59, 59);
            endDate = endDate.AddDays(-1);
            if (nowDate.AddDays(1).Month != nowDate.Month) {
                beginDate = beginDate.AddMonths(1);
                endDate = endDate.AddMonths(1);
                endDate = new DateTime(endDate.Year, endDate.Month, 1);
                endDate = endDate.AddDays(-1);
            }
            QueryNode qryNodeForIssueReceipt = OOQL.Select(
                OOQL.CreateProperty("IR.Owner_Org.ROid", "PLANT_ID"),
                OOQL.CreateProperty("IRD.ITEM_ID","ITEM_ID"),
                OOQL.CreateProperty("UNIT.DICIMAL_DIGIT", "DICIMAL_DIGIT"),
                OOQL.CreateProperty("IRD.INVENTORY_QTY", "INV_QTY"))
            .From("ISSUE_RECEIPT", "IR")
            .LeftJoin("ISSUE_RECEIPT.ISSUE_RECEIPT_D", "IRD")
            .On(OOQL.CreateProperty("IR.ISSUE_RECEIPT_ID") == OOQL.CreateProperty("IRD.ISSUE_RECEIPT_ID"))
            .LeftJoin("ITEM", "ITEM")
            .On(OOQL.CreateProperty("ITEM.ITEM_ID") == OOQL.CreateProperty("IRD.ITEM_ID"))
            .LeftJoin("UNIT", "UNIT")
            .On(OOQL.CreateProperty("UNIT.UNIT_ID") == OOQL.CreateProperty("ITEM.STOCK_UNIT_ID"))
            .LeftJoin("DOC","DOC")
            .On(OOQL.CreateProperty("IR.DOC_ID") == OOQL.CreateProperty("DOC.DOC_ID"))
            .Where(OOQL.CreateProperty("IR.ApproveStatus") == OOQL.CreateConstants("Y")
            & OOQL.CreateProperty("IR.DOC_DATE") >= OOQL.CreateConstants(beginDate)
            & OOQL.CreateProperty("IR.DOC_DATE") <= OOQL.CreateConstants(endDate)
             & OOQL.CreateProperty("DOC.CATEGORY") == OOQL.CreateConstants("56"));
            QueryNode qryNodeForIssueReturn = OOQL.Select(
                OOQL.CreateProperty("IR.Owner_Org.ROid", "PLANT_ID"),
                OOQL.CreateProperty("IRD.ITEM_ID", "ITEM_ID"),
                OOQL.CreateProperty("UNIT.DICIMAL_DIGIT", "DICIMAL_DIGIT"),
                OOQL.CreateArithmetic(OOQL.CreateProperty("IRD.INVENTORY_QTY"), OOQL.CreateConstants(-1), ArithmeticOperators.Mulit, "INV_QTY"))
            .From("ISSUE_RECEIPT", "IR")
            .LeftJoin("ISSUE_RECEIPT.ISSUE_RECEIPT_D", "IRD")
            .On(OOQL.CreateProperty("IR.ISSUE_RECEIPT_ID") == OOQL.CreateProperty("IRD.ISSUE_RECEIPT_ID"))
            .LeftJoin("ITEM","ITEM")
            .On(OOQL.CreateProperty("ITEM.ITEM_ID") == OOQL.CreateProperty("IRD.ITEM_ID"))
            .LeftJoin("UNIT", "UNIT")
            .On(OOQL.CreateProperty("UNIT.UNIT_ID") == OOQL.CreateProperty("ITEM.STOCK_UNIT_ID"))
            .LeftJoin("DOC", "DOC")
            .On(OOQL.CreateProperty("IR.DOC_ID") == OOQL.CreateProperty("DOC.DOC_ID"))
            .Where(OOQL.CreateProperty("IR.ApproveStatus") == OOQL.CreateConstants("Y")
            & OOQL.CreateProperty("IR.DOC_DATE") >= OOQL.CreateConstants(beginDate)
            & OOQL.CreateProperty("IR.DOC_DATE") <= OOQL.CreateConstants(endDate)
             & OOQL.CreateProperty("DOC.CATEGORY") == OOQL.CreateConstants("57"));
            UnionNode unNode = new UnionNode(qryNodeForIssueReceipt, qryNodeForIssueReturn, true);
            QueryNode qryNode = OOQL.Select(Formulas.NewId("X_ITEM_AVG_CONSUMPTION_ID"), 
                OOQL.CreateConstants("PLANT", "Owner_Org_RTK"),
                OOQL.CreateProperty("ALL_QUERY.PLANT_ID", "Owner_Org_ROid"),
                OOQL.CreateProperty("ALL_QUERY.ITEM_ID", "ITEM_ID"),
                OOQL.CreateArithmetic(
                Formulas.Ceiling(
                OOQL.CreateArithmetic(Formulas.Sum(Formulas.IsNull(OOQL.CreateProperty("ALL_QUERY.INV_QTY"), OOQL.CreateConstants(0m))),
                    OOQL.CreateConstants(month, GeneralDBType.Int32), ArithmeticOperators.Div)
                    * Formulas.Power(10, Formulas.IsNull(OOQL.CreateProperty("ALL_QUERY.DICIMAL_DIGIT"), OOQL.CreateConstants(0)))),
                    Formulas.Power(10, Formulas.IsNull(OOQL.CreateProperty("ALL_QUERY.DICIMAL_DIGIT"), OOQL.CreateConstants(0))),
                    ArithmeticOperators.Div, "AVG_CONSUMPTION"))               
                .From(unNode, "ALL_QUERY")
                .GroupBy(OOQL.CreateProperty("ALL_QUERY.PLANT_ID"), 
                    OOQL.CreateProperty("ALL_QUERY.ITEM_ID"),
                    OOQL.CreateProperty("ALL_QUERY.DICIMAL_DIGIT"));
            QueryNode updateNode = OOQL.Update("X_ITEM_AVG_CONSUMPTION", new SetItem[]{
                    new SetItem(OOQL.CreateProperty("X_ITEM_AVG_CONSUMPTION.AVG_CONSUMPTION"), OOQL.CreateConstants(0m))
                })
                .Where(OOQL.NotExists(OOQL.Select(1, OOQL.CreateProperty("QRY.ITEM_ID")).From(qryNode,"QRY").Where(
                OOQL.CreateProperty("X_ITEM_AVG_CONSUMPTION.ITEM_ID") == OOQL.CreateProperty("QRY.ITEM_ID")
                & OOQL.CreateProperty("X_ITEM_AVG_CONSUMPTION.Owner_Org.ROid") == OOQL.CreateProperty("QRY.Owner_Org_ROid")))
                );
            this.GetService<IQueryService>().ExecuteNoQueryWithManageProperties(updateNode);
            QueryNode mergeNode = OOQL.Merge("X_ITEM_AVG_CONSUMPTION")
                .Using(qryNode, "ALL_MERGE")
                .On(OOQL.CreateProperty("ALL_MERGE.ITEM_ID") == OOQL.CreateProperty("X_ITEM_AVG_CONSUMPTION.ITEM_ID")
                & OOQL.CreateProperty("ALL_MERGE.Owner_Org_ROid") == OOQL.CreateProperty("X_ITEM_AVG_CONSUMPTION.Owner_Org.ROid"))
                .Update(new SetItem[] { new SetItem(OOQL.CreateProperty("X_ITEM_AVG_CONSUMPTION.AVG_CONSUMPTION"), OOQL.CreateProperty("ALL_MERGE.AVG_CONSUMPTION")) })
                .Insert(new SetItem[] { 
                    new SetItem(OOQL.CreateProperty("X_ITEM_AVG_CONSUMPTION.X_ITEM_AVG_CONSUMPTION_ID"), OOQL.CreateProperty("ALL_MERGE.X_ITEM_AVG_CONSUMPTION_ID")),
                    new SetItem(OOQL.CreateProperty("X_ITEM_AVG_CONSUMPTION.ITEM_ID"), OOQL.CreateProperty("ALL_MERGE.ITEM_ID")),
                    new SetItem(OOQL.CreateProperty("X_ITEM_AVG_CONSUMPTION.Owner_Org.RTK"), OOQL.CreateProperty("ALL_MERGE.Owner_Org_RTK")),
                    new SetItem(OOQL.CreateProperty("X_ITEM_AVG_CONSUMPTION.Owner_Org.ROid"), OOQL.CreateProperty("ALL_MERGE.Owner_Org_ROid")),
                    new SetItem(OOQL.CreateProperty("X_ITEM_AVG_CONSUMPTION.AVG_CONSUMPTION"), OOQL.CreateProperty("ALL_MERGE.AVG_CONSUMPTION")) 
                });
            return this.GetService<IQueryService>().ExecuteNoQueryWithManageProperties(mergeNode);
        }
    }
}

