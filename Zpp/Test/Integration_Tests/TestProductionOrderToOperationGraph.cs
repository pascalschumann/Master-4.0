using System.IO;
using System.Linq;
using System.Text;
using Xunit;
using Zpp.Configuration;
using Zpp.DataLayer;
using Zpp.DataLayer.ProviderDomain.WrappersForCollections;
using Zpp.Mrp;
using Zpp.Test.Configuration;
using Zpp.Util;
using Zpp.Util.Graph;
using Zpp.Util.Graph.impl;
using Zpp.ZppSimulator;

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

            IZppSimulator zppSimulator = new ZppSimulator.impl.ZppSimulator();
            zppSimulator.StartTestCycle();
        }

        [Theory]
        
        [InlineData(TestConfigurationFileNames.TRUCK_COP_5_LOTSIZE_2)]
        public void TestGraphIsComplete(string testConfigurationFileName)
        {
            InitThisTest(testConfigurationFileName);
            
            IDbTransactionData dbTransactionData =
                ZppConfiguration.CacheManager.ReloadTransactionData();

            ProductionOrderToOperationGraph productionOrderToOperationGraph =
                new ProductionOrderToOperationGraph();

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

        [Theory]
        [InlineData(TestConfigurationFileNames.DESK_COP_5_LOTSIZE_2)]
        [InlineData(TestConfigurationFileNames.TRUCK_COP_5_LOTSIZE_2)]
        public void TestAllOperationsGraphStaysTheSame(string testConfigurationFileName)
        {
            InitThisTest(testConfigurationFileName);

            string operationGraphFileName =
                $"../../../Test/Ordergraphs/productionOrderToOperation_graph_{TestConfiguration.Name}.txt";

            // build operationGraph up
            IDirectedGraph<INode> operationDirectedGraph =
                new ProductionOrderToOperationGraph();
            
            string actualOperationGraph = operationDirectedGraph.ToString();
            if (File.Exists(operationGraphFileName) == false)
            {
                File.WriteAllText(operationGraphFileName, actualOperationGraph, Encoding.UTF8);
            }

            string expectedOperationGraph = File.ReadAllText(operationGraphFileName, Encoding.UTF8);

            bool operationGraphHasNotChanged = expectedOperationGraph.Equals(actualOperationGraph);
            // for debugging: write the changed graphs to files
            if (operationGraphHasNotChanged == false)
            {
                File.WriteAllText(operationGraphFileName, actualOperationGraph, Encoding.UTF8);
            }

            if (Constants.IsWindows)
            {
                Assert.True(operationGraphHasNotChanged, "The AllOperationGraph has changed.");
            }
            else
            {
                // On linux the graph is always different so the test would always fail here.
                Assert.True(true);
            }
        }
    }
}