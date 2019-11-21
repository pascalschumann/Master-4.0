using System.IO;
using System.Text;
using Xunit;
using Zpp.DataLayer;
using Zpp.GraphicalRepresentation;
using Zpp.Test.Configuration;
using Zpp.Test.Integration_Tests;
using Zpp.Util;
using Zpp.ZppSimulator;
using Zpp.ZppSimulator.impl;

namespace Zpp.Test.Ordergraphs.GanttChart
{
    public class TestGanttChart : AbstractTest
    {
        public TestGanttChart(): base(false)
        {
            
        }

        [Theory]
        [InlineData(TestConfigurationFileNames.DESK_COP_5_LOTSIZE_2)]
        [InlineData(TestConfigurationFileNames.TRUCK_COP_5_LOTSIZE_2)]
        public void TestGanttChartBar(string testConfigurationFileName)
        {
            InitThisTest(testConfigurationFileName);

            IDbTransactionData dbTransactionData =
                ZppConfiguration.CacheManager.ReloadTransactionData();

            IGanttChart ganttChart =
                new GraphicalRepresentation.impl.GanttChart(dbTransactionData
                    .ProductionOrderOperationGetAll());
            string actualGanttChart = ganttChart.ToString();

            Assert.NotNull(actualGanttChart);
            
        }
        
        private void InitThisTest(string testConfiguration)
        {
            InitTestScenario(testConfiguration);

            IZppSimulator zppSimulator = new ZppSimulator.impl.ZppSimulator();
            zppSimulator.StartTestCycle();
        }
    }
}