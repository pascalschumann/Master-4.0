using Xunit;
using Zpp.Mrp2.impl.Scheduling.impl;
using Zpp.Test.Configuration;
using Zpp.Util.Graph;
using Zpp.Util.Graph.impl;
using Zpp.ZppSimulator;

namespace Zpp.Test.Integration_Tests
{
    public class TestOrderOperationGraph : AbstractTest
    {
        private void InitThisTest(string testConfiguration)
        {
            InitTestScenario(testConfiguration);

            IZppSimulator zppSimulator = new ZppSimulator.impl.ZppSimulator();
            zppSimulator.StartTestCycle();
        }
        
        [Theory]
        [InlineData(TestConfigurationFileNames.DESK_COP_5_LOTSIZE_2)]
        [InlineData(TestConfigurationFileNames.DESK_COP_2_LOTSIZE_2)]
        [InlineData(TestConfigurationFileNames.TRUCK_COP_5_LOTSIZE_2)]
        public void TestBuildUp(string testConfigurationFileName)
        {
            InitThisTest(testConfigurationFileName);
            
            OrderOperationGraph orderOperationGraph = new OrderOperationGraph();
            // ProductionOrderToOperationGraph productionOrderToOperationGraph = new ProductionOrderToOperationGraph();
            Assert.True(orderOperationGraph != null);
        }
    }
}