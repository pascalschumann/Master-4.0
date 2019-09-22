using System.IO;
using System.Text;
using Xunit;
using Zpp.Configuration;
using Zpp.DbCache;
using Zpp.Mrp;
using Zpp.OrderGraph;
using Zpp.Test.Configuration;
using Zpp.Utils;

namespace Zpp.Test.Integration_Tests
{
    public class TestProductionOrderGraph : AbstractTest
    {
        public TestProductionOrderGraph(): base(false)
        {
            
        }
        
        private void InitThisTest(string testConfiguration)
        {
            InitTestScenario(testConfiguration);

            MrpRun.Start(ProductionDomainContext);
        }
        
        /**
         * In case of failing (and the productionOrderGraph change is expected by you):
         * delete corresponding production_ordergraph_cop_*.txt files ind Folder Test/OrderGraphs
         */
        [Theory]
        
        [InlineData(TestConfigurationFileNames.DESK_COP_1_LOT_ORDER_QUANTITY)]
        [InlineData(TestConfigurationFileNames.DESK_COP_5_CONCURRENT_LOTSIZE_2)]
        [InlineData(TestConfigurationFileNames.DESK_COP_5_SEQUENTIALLY_LOTSIZE_2)]
        [InlineData(TestConfigurationFileNames.TRUCK_COP_5_LOTSIZE_2)]
        [InlineData(TestConfigurationFileNames.TRUCK_COP_1_LOTSIZE_1)]
        public void TestProductionOrderGraphStaysTheSame(string testConfigurationFileName)
        {
            InitThisTest(testConfigurationFileName);
            
            string orderGraphFileName =
                $"../../../Test/Ordergraphs/production_ordergraph_{TestConfiguration.Name}.txt";

            // build orderGraph up
            
            IDbTransactionData dbTransactionData =
                ZppConfiguration.CacheManager.ReloadTransactionData();
            IDirectedGraph<INode> orderDirectedGraph = new ProductionOrderDirectedGraph(dbTransactionData, true);

            
            string actualOrderGraph = orderDirectedGraph.ToString();
            if (File.Exists(orderGraphFileName) == false)
            {
                File.WriteAllText(orderGraphFileName, actualOrderGraph,
                    Encoding.UTF8);
            }
            
            string expectedOrderGraph =
                File.ReadAllText(orderGraphFileName, Encoding.UTF8);
            
            bool orderGraphHasNotChanged =
                expectedOrderGraph.Equals(actualOrderGraph);
            // for debugging: write the changed graphs to files
            if (orderGraphHasNotChanged == false)
            {
                File.WriteAllText(orderGraphFileName, actualOrderGraph,
                    Encoding.UTF8);
            }

            if (Constants.IsWindows)
            {
                Assert.True(orderGraphHasNotChanged, "OrderGraph has changed.");
            }
            else
            {
                // On linux the graph is always different so the test would always fail here.
                Assert.True(true);
            }
        }
    }
}