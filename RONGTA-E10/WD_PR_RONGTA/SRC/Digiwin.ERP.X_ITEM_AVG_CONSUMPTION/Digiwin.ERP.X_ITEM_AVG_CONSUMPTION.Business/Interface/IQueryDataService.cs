//----------------------------------------------------------------
//<Author>xuyang</author>
//<CreateDate>19-10-9</createDate>
//<IssueNo></IssueNo>
//<Description>容大个案</description>
//----------------------------------------------------------------
//^_^ 20191009 add by xuyang for 容大个案

using Digiwin.Common;
using Digiwin.Common.Torridity;

namespace Digiwin.ERP.X_ITEM_AVG_CONSUMPTION.Business{   
    /// <summary>
    ///IQueryDataService接口
    /// </summary>
    [TypeKeyOnly]
    public interface IQueryDataService {
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        DependencyObjectCollection QueryItemInfo();

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        DependencyObjectCollection QueryPlantInfo();

        /// <summary>
        /// 获取领料单信息
        /// </summary>
        /// <returns></returns>
        int UpdateAvgConsumption(int month);
    }
}

