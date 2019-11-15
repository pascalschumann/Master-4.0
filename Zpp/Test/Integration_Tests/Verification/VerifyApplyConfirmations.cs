using System.Linq;
using Xunit;
using Zpp.DataLayer;
using Zpp.DataLayer.impl;
using Zpp.DataLayer.impl.DemandDomain.Wrappers;
using Zpp.DataLayer.impl.DemandDomain.WrappersForCollections;
using Zpp.DataLayer.impl.OpenDemand;
using Zpp.DataLayer.impl.ProviderDomain.WrappersForCollections;
using Zpp.DataLayer.impl.WrappersForCollections;
using Zpp.Test.Configuration;

namespace Zpp.Test.Integration_Tests.Verification
{
    public class VerifyApplyConfirmations : AbstractVerification
    {

        [Theory]
        [InlineData(TestConfigurationFileNames.TRUCK_COP_5_LOTSIZE_2)]
        [InlineData(TestConfigurationFileNames.TRUCK_COP_100_LOTSIZE_2)]
        [InlineData(TestConfigurationFileNames.TRUCK_INTERVAL_20160_COP_100_LOTSIZE_2)]
        public void TestApplyConfirmations(string testConfigurationFileName)
        {
            InitThisTest(testConfigurationFileName);

            IDbTransactionData dbTransactionData =
                ZppConfiguration.CacheManager.GetDbTransactionData();

            // Für jede noch vorhandene ProductionOrder muss noch Operationen geben.
            Ids productionOrderIds = new Ids();
            Ids operationIds = new Ids();
            foreach (var operation in dbTransactionData.ProductionOrderOperationGetAll())
            {
                productionOrderIds.Add(operation.GetProductionOrderId());
                operationIds.Add(operation.GetId());
            }

            foreach (var productionOrder in dbTransactionData.ProductionOrderGetAll())
            {
                Assert.True(productionOrderIds.Contains(productionOrder.GetId()));
            }

            //Es darf keine beendeten CustomerOrderParts, PurchaseOrderParts, Operations geben.
            foreach (var operation in dbTransactionData.ProductionOrderOperationGetAll())
            {
                Assert.False(operation.IsFinished());
            }
            foreach (var purchaseOrderPart in dbTransactionData.PurchaseOrderPartGetAll())
            {
                Assert.False(purchaseOrderPart.IsFinished());
            }
            foreach (var customerOrderPart in dbTransactionData.CustomerOrderPartGetAll())
            {
                Assert.False(customerOrderPart.IsFinished());
            }
            
            // Für jede noch vorhandene ProductionOrderBom muss es die dazugehörige Operation noch da sein.
            foreach (var demand in dbTransactionData.ProductionOrderBomGetAll())
            {
                ProductionOrderBom productionOrderBom = (ProductionOrderBom) demand;
                operationIds.Contains(productionOrderBom.GetProductionOrderOperationId());
            }
            
            // Es darf keine beendeten und geschlossenen StockExchangeDemands geben.
            foreach (var stockExchangeDemand in dbTransactionData.StockExchangeDemandsGetAll())
            {
                Assert.False(stockExchangeDemand.IsFinished() && OpenDemandManager.IsOpen((StockExchangeDemand)stockExchangeDemand));
            }

            IAggregator aggregator = ZppConfiguration.CacheManager.GetAggregator();
            // Ein nicht beendeter CustomerOrderParts darf kein Kind haben.
            foreach (var customerOrderParts in dbTransactionData.CustomerOrderPartGetAll())
            {
                Providers childs = aggregator.GetAllChildProvidersOf(customerOrderParts);
                Assert.False(childs.Any());
            }
            // Jeder noch vorhandener StockExchangeProvider muss ein Kind haben.
            foreach (var stockExchangeProvider in dbTransactionData.StockExchangeProvidersGetAll())
            {
                Demands childs = aggregator.GetAllChildDemandsOf(stockExchangeProvider);
                Assert.True(childs.Any());
            }
        }
    }
}