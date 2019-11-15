using System.Linq;
using Master40.DB.Data.WrappersForPrimitives;
using Master40.DB.Enums;
using Xunit;
using Zpp.DataLayer;
using Zpp.DataLayer.impl;
using Zpp.DataLayer.impl.DemandDomain.Wrappers;
using Zpp.DataLayer.impl.DemandDomain.WrappersForCollections;
using Zpp.DataLayer.impl.OpenDemand;
using Zpp.DataLayer.impl.ProviderDomain;
using Zpp.DataLayer.impl.ProviderDomain.Wrappers;
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
                Id productionOrderId = operation.GetProductionOrderId();
                if (productionOrderIds.Contains(productionOrderId) == false)
                {
                    productionOrderIds.Add(productionOrderId);
                }

                operationIds.Add(operation.GetId());
            }

            foreach (var productionOrder in dbTransactionData.ProductionOrderGetAll())
            {
                Assert.True(productionOrderIds.Contains(productionOrder.GetId()));
            }

            //Es darf keine beendeten CustomerOrderParts, ProductionOrder geben.
            foreach (var provider in dbTransactionData.ProductionOrderGetAll())
            {
                ProductionOrder productionOrder = (ProductionOrder) provider;
                Assert.False(productionOrder.DetermineProductionOrderState()
                    .Equals(State.Finished));
            }

            foreach (var customerOrderPart in dbTransactionData.CustomerOrderPartGetAll())
            {
                if (customerOrderPart.IsFinished() == true)
                {
                    Provider stockExchangeProvider = ZppConfiguration.CacheManager.GetAggregator()
                        .GetAllChildProvidersOf(customerOrderPart).GetAny();
                    Demands demands = ZppConfiguration.CacheManager.GetAggregator()
                        .GetAllChildDemandsOf(stockExchangeProvider);
                }
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
                bool isOpen = OpenDemandManager.IsOpen((StockExchangeDemand) stockExchangeDemand);
                Assert.False(stockExchangeDemand.IsFinished() && isOpen == false);
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