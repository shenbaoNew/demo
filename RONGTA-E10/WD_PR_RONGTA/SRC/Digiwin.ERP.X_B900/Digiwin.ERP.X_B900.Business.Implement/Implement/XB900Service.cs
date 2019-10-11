using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Digiwin.Common;
using Digiwin.Common.Query2;
using Digiwin.Common.Services;
using Digiwin.Common.Torridity;

namespace Digiwin.ERP.X_B900.Business.Implement {
    [ServiceClass(typeof(IXB900Service))]
    [SingleGetCreator]
    sealed class XB900Service : ServiceComponent, IXB900Service {
        #region IXB900Service 成员

        /// <summary>
        /// 批量修改PMC接单日
        /// </summary>
        public void UpdateDate(DateTime date) {
            Update(date);
        }

        /// <summary>
        /// 获取未接单订单
        /// </summary>
        /// <param name="date">接单日期</param>
        /// <returns></returns>
        public DependencyObjectCollection QuerySalesOrderDocDetail(DateTime date) {
            return QueryDetail(date);
        }

        private void Update(DateTime date) {
            QueryNode node = OOQL.Select(OOQL.CreateProperty("B.SALES_ORDER_DOC_D_ID"))
                .From("SALES_ORDER_DOC", "A")
                .InnerJoin("SALES_ORDER_DOC.SALES_ORDER_DOC_D", "B")
                .On(OOQL.CreateProperty("A.SALES_ORDER_DOC_ID") == OOQL.CreateProperty("B.SALES_ORDER_DOC_ID"))
                .InnerJoin("SALES_ORDER_DOC.SALES_ORDER_DOC_D.SALES_ORDER_DOC_SD", "C")
                .On(OOQL.CreateProperty("B.SALES_ORDER_DOC_D_ID") == OOQL.CreateProperty("C.SALES_ORDER_DOC_D_ID"))
                .Where(OOQL.AuthFilter("SALES_ORDER_DOC", "A")
                    & OOQL.CreateProperty("A.ApproveStatus") == OOQL.CreateConstants("Y")
                    & OOQL.CreateProperty("B.X_PMC_DATE") <= OOQL.CreateConstants("1970-10-1")
                    & OOQL.CreateProperty("C.CLOSE") == OOQL.CreateConstants("0"));

            QueryNode update = OOQL.Update("SALES_ORDER_DOC.SALES_ORDER_DOC_D", new SetItem[]{
                new SetItem(OOQL.CreateProperty("X_PMC_DATE"),OOQL.CreateConstants(date))
            }).Where(OOQL.CreateProperty("SALES_ORDER_DOC.SALES_ORDER_DOC_D.SALES_ORDER_DOC_D_ID").In(node));

            int count = this.GetService<IQueryService>().ExecuteNoQueryWithManageProperties(update);
        }

        private DependencyObjectCollection QueryDetail(DateTime date) {
            QueryNode node = OOQL.Select(true,OOQL.CreateArithmetic(
                            OOQL.CreateProperty("A.DOC_NO"),
                            OOQL.CreateArithmetic(
                                    OOQL.CreateConstants("-"),
                                    Formulas.Cast(
                                            OOQL.CreateProperty("B.SequenceNumber"), GeneralDBType.String), ArithmeticOperators.Plus), ArithmeticOperators.Plus, "DOC_NO"),
                    OOQL.CreateProperty("D.ITEM_CODE"),
                    OOQL.CreateProperty("D.ITEM_NAME"),
                    OOQL.CreateProperty("D.ITEM_SPECIFICATION"),
                    OOQL.CreateProperty("B.BUSINESS_QTY", "QTY"),
                    OOQL.CreateProperty("B.SALES_ORDER_DOC_D_ID"),
                    OOQL.CreateProperty("A.SALES_ORDER_DOC_ID"),
                    OOQL.CreateProperty("D.ITEM_ID", "ITEM_ID"),
                    OOQL.CreateProperty("A.DOC_NO", "REAL_DOC_NO"),
                    OOQL.CreateProperty("B.SequenceNumber"),
                    OOQL.CreateConstants(date, "X_PMC_DATE")
                    )
                .From("SALES_ORDER_DOC", "A")
                .InnerJoin("SALES_ORDER_DOC.SALES_ORDER_DOC_D", "B")
                .On(OOQL.CreateProperty("A.SALES_ORDER_DOC_ID") == OOQL.CreateProperty("B.SALES_ORDER_DOC_ID"))
                .InnerJoin("SALES_ORDER_DOC.SALES_ORDER_DOC_D.SALES_ORDER_DOC_SD", "C")
                .On(OOQL.CreateProperty("B.SALES_ORDER_DOC_D_ID") == OOQL.CreateProperty("C.SALES_ORDER_DOC_D_ID"))
                .InnerJoin("ITEM", "D")
                .On(OOQL.CreateProperty("B.ITEM_ID") == OOQL.CreateProperty("D.ITEM_ID"))
                .Where(OOQL.AuthFilter("SALES_ORDER_DOC", "A")
                    & OOQL.CreateProperty("A.ApproveStatus") == OOQL.CreateConstants("Y")
                    & OOQL.CreateProperty("B.X_PMC_DATE") <= OOQL.CreateConstants("1970-10-1")
                    & OOQL.CreateProperty("C.CLOSE") == OOQL.CreateConstants("0"))
                .OrderBy(OOQL.CreateOrderByItem("A.DOC_NO"), OOQL.CreateOrderByItem("B.SequenceNumber"));

            DependencyObjectCollection coll = this.GetService<IQueryService>().ExecuteDependencyObject(node);

            return coll;
        }
        #endregion
    }
}
