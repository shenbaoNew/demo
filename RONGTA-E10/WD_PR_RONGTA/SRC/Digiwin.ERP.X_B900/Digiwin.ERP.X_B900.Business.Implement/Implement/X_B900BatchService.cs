using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Digiwin.Common;
using Digiwin.Common.Services;
using System.Data;
using Digiwin.Common.Query2;
using Digiwin.Common.Torridity.Metadata;
using Digiwin.Common.Torridity;

namespace Digiwin.ERP.X_B900.Business.Implement {
    [SingleGetCreator]
    [ServiceClass(typeof(IBatchService))]
    [ServiceClass(typeof(IBatchPreviewService))]
    public class X_B900BatchService : FreeBatchService<FreeBatchEventsArgs> {
        protected override FreeBatchEventsArgs CreateFreeBatchEventsArgs(BatchContext context) {
            return new FreeBatchEventsArgs(context);
        }

        public override DataTable GetPreviewData(BatchContext context) {
            return null;
        }

        protected override void DoProcess(FreeBatchEventsArgs args) {
            IQueryService service = this.GetService<IQueryService>();
            IDataEntityType type = CreateTmpTable(service);
            BulkCopyToTempTable(service, type, args.Context.Parameters["X_B900_D"].Value as DependencyObjectCollection);
            base.RefreshProcess(35, null, args.Context);

            Update(service, type);

            base.RefreshProcess(100, null, args.Context);
        }

        public override bool CanPreview() {
            return false;
        }

        private void Update(IQueryService service, IDataEntityType type) {
            QueryNode node = OOQL.Select(OOQL.CreateProperty("A.SALES_ORDER_DOC_D_ID"),
                    OOQL.CreateProperty("A.X_PMC_DATE")
                )
                .From(type.Name, "A");

            QueryNode update = OOQL.Update("SALES_ORDER_DOC.SALES_ORDER_DOC_D")
                .Set(new SetItem[]{
                new SetItem(OOQL.CreateProperty("X_PMC_DATE"),OOQL.CreateProperty("SubNode.X_PMC_DATE"))
            })
            .From(node, "SubNode")
            .Where(OOQL.CreateProperty("SALES_ORDER_DOC.SALES_ORDER_DOC_D.SALES_ORDER_DOC_D_ID") == OOQL.CreateProperty("SubNode.SALES_ORDER_DOC_D_ID"));

            int count = service.ExecuteNoQueryWithManageProperties(update);
        }

        private IDataEntityType CreateTmpTable(IQueryService service) {
            string typeName = "Tmp_XB900_" + DateTime.Now.ToString("HHmmssfff");// 临时表表名的处理
            DependencyObjectType defaultType = new DependencyObjectType(typeName, new Attribute[] { });

            SimplePropertyAttribute primaryKeyAttr = new SimplePropertyAttribute(GeneralDBType.Guid);
            SimplePropertyAttribute stringAttr = new SimplePropertyAttribute(GeneralDBType.String);
            SimplePropertyAttribute qtyAttr = new SimplePropertyAttribute(GeneralDBType.Decimal, 16);
            qtyAttr.Scale = 6;
            SimplePropertyAttribute intAttr = new SimplePropertyAttribute(GeneralDBType.Int32);
            SimplePropertyAttribute dateAttr = new SimplePropertyAttribute(GeneralDBType.DateTime);
            SimplePropertyAttribute textAttr = new SimplePropertyAttribute(GeneralDBType.Text);

            //主键
            defaultType.RegisterSimpleProperty("SALES_ORDER_DOC_D_ID", typeof(Guid), null, false, new Attribute[] { primaryKeyAttr });
            //日期
            defaultType.RegisterSimpleProperty("X_PMC_DATE", typeof(DateTime), OrmDataOption.EmptyDateTime, false, new Attribute[] { dateAttr });

            service.CreateTempTable(defaultType);
            return defaultType;
        }

        private void BulkCopyToTempTable(IQueryService service,IDataEntityType type,DependencyObjectCollection coll) {
            if (coll != null) {
                List<BulkCopyColumnMapping> list = new List<BulkCopyColumnMapping>();
                list.Add(new BulkCopyColumnMapping("X_SALES_ORDER_DOC_D_ID", "SALES_ORDER_DOC_D_ID"));
                list.Add(new BulkCopyColumnMapping("X_PMC_DATE", "X_PMC_DATE"));

                service.BulkCopy(coll, type.Name, list.ToArray());
            }
        }
    }
}
