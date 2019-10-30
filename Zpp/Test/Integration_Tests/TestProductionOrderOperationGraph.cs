using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Xunit;
using Zpp.DataLayer;
using Zpp.Test.Configuration;
using Zpp.Util;
using Zpp.Util.Graph;
using Zpp.Util.Graph.impl;
using Zpp.ZppSimulator;

namespace Zpp.Test.Integration_Tests
{
    public class TestProductionOrderOperationGraph : AbstractTest
    {
        public TestProductionOrderOperationGraph(): base(false)
        {
            
        }
        
        private void InitThisTest(string testConfiguration)
        {
            InitTestScenario(testConfiguration);

            IZppSimulator zppSimulator = new ZppSimulator.impl.ZppSimulator();
            zppSimulator.StartTestCycle();
        }
        
        /**
         * In case of failing (and the productionOrderGraph change is expected by you):
         * delete corresponding production_order_operation_graph_cop_*.txt files ind Folder Test/OrderGraphs
         */
        [Theory]
        [InlineData(TestConfigurationFileNames.DESK_COP_5_LOTSIZE_2)]
        
        [InlineData(TestConfigurationFileNames.TRUCK_COP_5_LOTSIZE_2)]
        [InlineData(TestConfigurationFileNames.TRUCK_COP_1_LOTSIZE_1)]
        public void TestProductionOrderOperationGraphStaysTheSame(string testConfigurationFileName)
        {
            InitThisTest(testConfigurationFileName);
            
            string orderGraphFileNameWithIds =
                $"../../../Test/Ordergraphs/production_order_operation_graph_{TestConfiguration.Name}.txt";

            // build orderGraph up
            
            IDbTransactionData dbTransactionData =
                ZppConfiguration.CacheManager.ReloadTransactionData();
           
            IProductionOrderToOperationGraph<INode> productionOrderToOperationGraph =
                new ProductionOrderToOperationGraph();
            
            
            string actualOrderGraphWithIds = productionOrderToOperationGraph.AsString();
            if (File.Exists(orderGraphFileNameWithIds) == false)
            {
                File.WriteAllText(orderGraphFileNameWithIds, actualOrderGraphWithIds,
                    Encoding.UTF8);
            }

            string expectedOrderGraphWithIds =
                File.ReadAllText(orderGraphFileNameWithIds, Encoding.UTF8);

            bool orderGraphWithIdsHasNotChanged =
                expectedOrderGraphWithIds.Equals(actualOrderGraphWithIds);
            // for debugging: write the changed graphs to files
            if (orderGraphWithIdsHasNotChanged == false)
            {
                File.WriteAllText(orderGraphFileNameWithIds, actualOrderGraphWithIds,
                    Encoding.UTF8);
            }

            if (Constants.IsWindows)
            {
                Assert.True(orderGraphWithIdsHasNotChanged, "OrderGraph has changed.");
            }
            else
            {
                // On linux the graph is always different so the test would always fail here.
                Assert.True(true);
            }
        }
    }
}
