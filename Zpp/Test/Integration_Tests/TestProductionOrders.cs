using Master40.DB.Data.Context;
using Master40.DB.Data.WrappersForPrimitives;
using Master40.DB.DataModel;
using Xunit;
using Zpp.DemandDomain;

namespace Zpp.Test
{
    public class TestProductionOrders : AbstractTest
    {
        [Fact]
        public void TestProductionOrderBomIsACopyOfArticleBom()
        {
            MrpRun.RunMrp(ProductionDomainContext);
            IDbMasterDataCache dbMasterDataCache = new DbMasterDataCache(ProductionDomainContext);
            IDbTransactionData dbTransactionData =
                new DbTransactionData(ProductionDomainContext, dbMasterDataCache);

            foreach (var productionOrderBom in dbTransactionData.ProductionOrderBomGetAll())
            {
                T_ProductionOrderBom tProductionOrderBom =
                    (T_ProductionOrderBom) productionOrderBom.ToIDemand();
                M_ArticleBom articleBom =
                    dbMasterDataCache.M_ArticleBomGetByArticleChildId(
                        new Id(tProductionOrderBom.ArticleChildId));
                Assert.True(tProductionOrderBom.Quantity.Equals(articleBom.Quantity),
                    "Quantity of ProductionOrderBom does not equal the quantity from its articleBom.");
            }
        }

        [Fact]
        public void TestProductionOrderOperationIsACopyOfM_Operation()
        {
            MrpRun.RunMrp(ProductionDomainContext);
            IDbMasterDataCache dbMasterDataCache = new DbMasterDataCache(ProductionDomainContext);
            IDbTransactionData dbTransactionData =
                new DbTransactionData(ProductionDomainContext, dbMasterDataCache);

            foreach (var productionOrderOperation in dbTransactionData
                .ProductionOrderOperationGetAll())
            {
                T_ProductionOrderOperation tProductionOrderOperation =
                    productionOrderOperation.GetValue();
                T_ProductionOrderBom aProductionOrderBom = (T_ProductionOrderBom) dbTransactionData
                    .GetAggregator()
                    .GetAnyProductionOrderBomByProductionOrderOperation(productionOrderOperation)
                    .ToIDemand();
                M_ArticleBom articleBom =
                    dbMasterDataCache.M_ArticleBomGetByArticleChildId(
                        new Id(aProductionOrderBom.ArticleChildId));
                M_Operation mOperation =
                    dbMasterDataCache.M_OperationGetById(new Id(articleBom.OperationId.GetValueOrDefault()));

                string errorMessage =
                    "Property of ProductionOrderBom does not equal the one from its articleBom.";
                Assert.True(tProductionOrderOperation.HierarchyNumber.Equals(mOperation.HierarchyNumber),
                    errorMessage);
                Assert.True(tProductionOrderOperation.Duration.Equals(mOperation.Duration),
                    errorMessage);
                Assert.True(tProductionOrderOperation.MachineToolId.Equals(mOperation.MachineToolId),
                    errorMessage);
                Assert.True(tProductionOrderOperation.MachineGroupId.Equals(mOperation.MachineGroupId),
                    errorMessage);
                Assert.True(tProductionOrderOperation.MachineGroupId.Equals(mOperation.MachineGroupId),
                    errorMessage);
            }
        }
    }
}