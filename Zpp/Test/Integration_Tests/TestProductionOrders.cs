using Master40.DB.Data.WrappersForPrimitives;
using Master40.DB.DataModel;
using Xunit;
using Zpp.DbCache;
using Zpp.MrpRun.ProductionManagement.ProductionTypes;

namespace Zpp.Test.Integration_Tests
{
    public class TestProductionOrders : AbstractTest
    {
        [Fact]
        public void TestProductionOrderBomIsACopyOfArticleBom()
        {
            if (Zpp.Configuration.Configuration.ProductionType.Equals(ProductionType.WorkshopProductionClassic))
            {
                Assert.True(true);
            }
            else
            {
                MrpRun.MrpRun.RunMrp(ProductionDomainContext);
                
                IDbTransactionData dbTransactionData =
                    new DbTransactionData(ProductionDomainContext, DbMasterDataCache);

                foreach (var productionOrderBom in dbTransactionData.ProductionOrderBomGetAll())
                {
                    T_ProductionOrderBom tProductionOrderBom =
                        (T_ProductionOrderBom) productionOrderBom.ToIDemand();
                    M_ArticleBom articleBom =
                        DbMasterDataCache.M_ArticleBomGetByArticleChildId(
                            new Id(tProductionOrderBom.ArticleChildId));
                    Assert.True(tProductionOrderBom.Quantity.Equals(articleBom.Quantity),
                        "Quantity of ProductionOrderBom does not equal the quantity from its articleBom.");
                }
            }

        }

        [Fact]
        public void TestProductionOrderOperationIsACopyOfM_Operation()
        {
            if (Zpp.Configuration.Configuration.ProductionType.Equals(ProductionType.WorkshopProductionClassic))
            {
                Assert.True(true);
            }
            else
            {

                MrpRun.MrpRun.RunMrp(ProductionDomainContext);
                IDbMasterDataCache DbMasterDataCache =
                    new DbMasterDataCache(ProductionDomainContext);
                IDbTransactionData dbTransactionData =
                    new DbTransactionData(ProductionDomainContext, DbMasterDataCache);

                foreach (var productionOrderOperation in dbTransactionData
                    .ProductionOrderOperationGetAll())
                {
                    T_ProductionOrderOperation tProductionOrderOperation =
                        productionOrderOperation.GetValue();
                    T_ProductionOrderBom aProductionOrderBom =
                        (T_ProductionOrderBom) dbTransactionData.GetAggregator()
                            .GetAnyProductionOrderBomByProductionOrderOperation(
                                productionOrderOperation).ToIDemand();
                    M_ArticleBom articleBom =
                        DbMasterDataCache.M_ArticleBomGetByArticleChildId(
                            new Id(aProductionOrderBom.ArticleChildId));
                    M_Operation mOperation =
                        DbMasterDataCache.M_OperationGetById(
                            new Id(articleBom.OperationId.GetValueOrDefault()));

                    string errorMessage =
                        "Property of ProductionOrderBom does not equal the one from its articleBom.";
                    Assert.True(
                        tProductionOrderOperation.HierarchyNumber.Equals(mOperation
                            .HierarchyNumber), errorMessage);
                    Assert.True(tProductionOrderOperation.Duration.Equals(mOperation.Duration),
                        errorMessage);
                    Assert.True(
                        tProductionOrderOperation.MachineToolId.Equals(mOperation.MachineToolId),
                        errorMessage);
                    Assert.True(
                        tProductionOrderOperation.MachineGroupId.Equals(mOperation.MachineGroupId),
                        errorMessage);
                    Assert.True(
                        tProductionOrderOperation.MachineGroupId.Equals(mOperation.MachineGroupId),
                        errorMessage);
                }
            }
        }
    }
}