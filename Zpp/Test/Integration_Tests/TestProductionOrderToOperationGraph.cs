using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Xunit;
using Zpp.DataLayer;
using Zpp.DataLayer.impl.ProviderDomain.Wrappers;
using Zpp.DataLayer.impl.ProviderDomain.WrappersForCollections;
using Zpp.Mrp2.impl.Scheduling.impl;
using Zpp.Test.Configuration;
using Zpp.Util;
using Zpp.Util.Graph;
using Zpp.Util.Graph.impl;
using Zpp.Util.StackSet;
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

            IDirectedGraph<INode> operationGraph =
                new OperationGraph(new OrderOperationGraph());

            IEnumerable<ProductionOrderOperation> productionOrderOperations =
                operationGraph.GetNodes().Select(x=>(ProductionOrderOperation)x.GetNode().GetEntity());
            foreach (var productionOrderOperation in dbTransactionData
                .ProductionOrderOperationGetAll())
            {
                Assert.True(productionOrderOperations.Contains(productionOrderOperation),
                    $"{productionOrderOperation} is missing.");
            }
        }
    }
}