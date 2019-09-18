using System.Linq;
using Xunit;
using Zpp.Common.ProviderDomain.Wrappers;
using Zpp.Common.ProviderDomain.WrappersForCollections;
using Zpp.DbCache;
using Zpp.Mrp;
using Zpp.Mrp.MachineManagement;
using Zpp.OrderGraph;
using Zpp.Test.Configuration;

namespace Zpp.Test.Integration_Tests
{
    public class TestProductionOrderToOperationGraph : AbstractTest
    {
        public TestProductionOrderToOperationGraph() : base(initDefaultTestConfig: false)
        {
        }
        
        private void InitThisTest(string testConfiguration)
        {
            InitTestScenario(testConfiguration);

            MrpRun.Start(ProductionDomainContext);
        }

        [Theory]
        [InlineData(TestConfigurationFileNames.DESK_COP_5_SEQUENTIALLY_LOTSIZE_2)]
        [InlineData(TestConfigurationFileNames.TRUCK_COP_5_LOTSIZE_2)]
        public void TestGraphIsComplete(string testConfigurationFileName)
        {
            InitThisTest(testConfigurationFileName);
            IDbMasterDataCache dbMasterDataCache = new DbMasterDataCache(ProductionDomainContext);
            IDbTransactionData dbTransactionData =
                new DbTransactionData(ProductionDomainContext, dbMasterDataCache);

            ProductionOrderToOperationGraph productionOrderToOperationGraph =
                new ProductionOrderToOperationGraph(dbMasterDataCache, dbTransactionData);

            ProductionOrderOperations productionOrderOperations =
                productionOrderToOperationGraph.GetAllOperations();
            ProductionOrders productionOrders =
                productionOrderToOperationGraph.GetAllProductionOrders();
            foreach (var productionOrderOperation in dbTransactionData
                .ProductionOrderOperationGetAll())
            {
                Assert.True(productionOrderOperations.Contains(productionOrderOperation),
                    $"{productionOrderOperation} is missing.");
            }

            foreach (var productionOrder in dbTransactionData.ProductionOrderGetAll())
            {
                Assert.True(productionOrders.Contains(productionOrder),
                    $"{productionOrder} is missing.");
            }
        }
    }
}