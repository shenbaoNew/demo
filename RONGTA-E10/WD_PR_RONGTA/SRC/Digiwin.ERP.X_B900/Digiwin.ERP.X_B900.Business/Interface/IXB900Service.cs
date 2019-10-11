using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Digiwin.Common;
using Digiwin.Common.Torridity;

namespace Digiwin.ERP.X_B900.Business {
    [TypeKeyOnly]
    public interface IXB900Service {
        /// <summary>
        /// 批量修改PMC接单日
        /// </summary>
        void UpdateDate(DateTime date);

        /// <summary>
        /// 获取未接单订单
        /// </summary>
        /// <param name="date">接单日期</param>
        /// <returns></returns>
        DependencyObjectCollection QuerySalesOrderDocDetail(DateTime date);
    }
}
