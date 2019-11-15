using System.Linq;
using Master40.DB.Data.WrappersForPrimitives;
using Master40.DB.Enums;
using Xunit;
using Zpp.DataLayer;
using Zpp.DataLayer.impl;
using Zpp.DataLayer.impl.DemandDomain;
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
            IAggregator aggregator = ZppConfiguration.CacheManager.GetAggregator();

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

            Ids customerOrderIds = new Ids();
            foreach (var demand in dbTransactionData.CustomerOrderPartGetAll())
            {
                CustomerOrderPart customerOrderPart = (CustomerOrderPart)demand;
                customerOrderIds.Add(customerOrderPart.GetCustomerOrderId());
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
            
            // Für jede CustomerOrder muss es mind. noch ein CustomerOrderPart geben.
            foreach (var customerOrder in dbTransactionData.CustomerOrderGetAll())
            {
                Assert.True(customerOrderIds.Contains(customerOrder.GetId()));
            }
            // Für jede PurchaseOrder muss es mind. noch ein PurchaseOrderPart geben.
            Ids purchaseOrderIds = new Ids();
            foreach (var demand in dbTransactionData.PurchaseOrderPartGetAll())
            {
                PurchaseOrderPart purchaseOrderPart = (PurchaseOrderPart)demand;
                purchaseOrderIds.Add(purchaseOrderPart.GetPurchaseOrderId());
            }

            foreach (var purchaseOrder in dbTransactionData.PurchaseOrderGetAll())
            {
                Assert.True(purchaseOrderIds.Contains(purchaseOrder.GetId()));
            }
            
            // Für jeden DemandToProvider und ProviderToDemand müssen die dazugehörigen
            // Demands und Provider noch existieren.
            foreach (var demandToProvider in dbTransactionData.DemandToProviderGetAll())
            {
                Demand demand = dbTransactionData.DemandsGetById(demandToProvider.GetDemandId());
                Provider provider =
                    dbTransactionData.ProvidersGetById(demandToProvider.GetProviderId());
                Assert.NotNull(demand);
                Assert.NotNull(provider);
            }
            
            foreach (var providerToDemand in dbTransactionData.ProviderToDemandGetAll())
            {
                Demand demand = dbTransactionData.DemandsGetById(providerToDemand.GetDemandId());
                Provider provider =
                    dbTransactionData.ProvidersGetById(providerToDemand.GetProviderId());
                Assert.NotNull(demand);
                Assert.NotNull(provider);
            }
        }
        
        [Theory]
        [InlineData(TestConfigurationFileNames.TRUCK_COP_5_LOTSIZE_2)]
        [InlineData(TestConfigurationFileNames.TRUCK_COP_100_LOTSIZE_2)]
        [InlineData(TestConfigurationFileNames.TRUCK_INTERVAL_20160_COP_100_LOTSIZE_2)]
        public void TestApplyConfirmationsArchiv(string testConfigurationFileName)
        {
            InitThisTest(testConfigurationFileName);

            IDbTransactionData dbTransactionDataArchive =
                ZppConfiguration.CacheManager.GetDbTransactionDataArchive();
            IAggregator aggregator = new Aggregator(dbTransactionDataArchive);
            
            // ZPP should only use the archive for the rest of the test
            ZppConfiguration.CacheManager.UseArchiveForGetters();

            // Für jede noch vorhandene ProductionOrder muss es mind. eine  Operation geben.
            Ids productionOrderIds = new Ids();
            Ids operationIds = new Ids();
            foreach (var operation in dbTransactionDataArchive.ProductionOrderOperationGetAll())
            {
                Id productionOrderId = operation.GetProductionOrderId();
                if (productionOrderIds.Contains(productionOrderId) == false)
                {
                    productionOrderIds.Add(productionOrderId);
                }

                operationIds.Add(operation.GetId());
            }

            foreach (var productionOrder in dbTransactionDataArchive.ProductionOrderGetAll())
            {
                Assert.True(productionOrderIds.Contains(productionOrder.GetId()));
            }

            //Alle CustomerOrderParts, ProductionOrders müssen beendet sein.
            foreach (var provider in dbTransactionDataArchive.ProductionOrderGetAll())
            {
                ProductionOrder productionOrder = (ProductionOrder) provider;
                Assert.True(productionOrder.DetermineProductionOrderState()
                    .Equals(State.Finished));
            }

            Ids customerOrderIds = new Ids();
            foreach (var demand in dbTransactionDataArchive.CustomerOrderPartGetAll())
            {
                CustomerOrderPart customerOrderPart = (CustomerOrderPart)demand;
                customerOrderIds.Add(customerOrderPart.GetCustomerOrderId());
                Assert.True(customerOrderPart.IsFinished());
            }

            // Für jede vorhandene ProductionOrderBom muss es die dazugehörige
            // Operation da sein.
            foreach (var demand in dbTransactionDataArchive.ProductionOrderBomGetAll())
            {
                ProductionOrderBom productionOrderBom = (ProductionOrderBom) demand;
                operationIds.Contains(productionOrderBom.GetProductionOrderOperationId());
            }

            // Es darf nur beendete und geschlossene StockExchangeDemands geben.
            foreach (var stockExchangeDemand in dbTransactionDataArchive.StockExchangeDemandsGetAll())
            {
                bool isOpen = OpenDemandManager.IsOpen((StockExchangeDemand) stockExchangeDemand);
                Assert.True(stockExchangeDemand.IsFinished() && isOpen == false);
            }
            
            // Jeder vorhandene StockExchangeProvider muss ein Kind haben.
            foreach (var stockExchangeProvider in dbTransactionDataArchive.StockExchangeProvidersGetAll())
            {
                Demands childs = aggregator.GetAllChildDemandsOf(stockExchangeProvider);
                Assert.True(childs.Any());
            }
            
            // Für jede CustomerOrder muss es mind. ein CustomerOrderPart geben.
            foreach (var customerOrder in dbTransactionDataArchive.CustomerOrderGetAll())
            {
                Assert.True(customerOrderIds.Contains(customerOrder.GetId()));
            }
            // Für jede PurchaseOrder muss es mind. ein PurchaseOrderPart geben.
            Ids purchaseOrderIds = new Ids();
            foreach (var demand in dbTransactionDataArchive.PurchaseOrderPartGetAll())
            {
                PurchaseOrderPart purchaseOrderPart = (PurchaseOrderPart)demand;
                purchaseOrderIds.Add(purchaseOrderPart.GetPurchaseOrderId());
            }

            foreach (var purchaseOrder in dbTransactionDataArchive.PurchaseOrderGetAll())
            {
                Assert.True(purchaseOrderIds.Contains(purchaseOrder.GetId()));
            }
            
            // Für jeden DemandToProvider und ProviderToDemand müssen die dazugehörigen
            // Demands und Provider existieren.
            foreach (var demandToProvider in dbTransactionDataArchive.DemandToProviderGetAll())
            {
                Demand demand = dbTransactionDataArchive.DemandsGetById(demandToProvider.GetDemandId());
                Provider provider =
                    dbTransactionDataArchive.ProvidersGetById(demandToProvider.GetProviderId());
                Assert.NotNull(demand);
                Assert.NotNull(provider);
            }
            
            foreach (var providerToDemand in dbTransactionDataArchive.ProviderToDemandGetAll())
            {
                Demand demand = dbTransactionDataArchive.DemandsGetById(providerToDemand.GetDemandId());
                Provider provider =
                    dbTransactionDataArchive.ProvidersGetById(providerToDemand.GetProviderId());
                Assert.NotNull(demand);
                Assert.NotNull(provider);
            }
        }
    }
}