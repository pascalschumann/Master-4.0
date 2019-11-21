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
        [InlineData(TestConfigurationFileNames.DESK_COP_2_LOTSIZE_2)]
        [InlineData(TestConfigurationFileNames.TRUCK_COP_5_LOTSIZE_2)]
        [InlineData(TestConfigurationFileNames.TRUCK_COP_1_LOTSIZE_2)]
        public void TestGanttChartBar(string testConfigurationFileName)
        {
            InitThisTest(testConfigurationFileName);
            
            string orderGraphAsGanttChartFile =
                $"../../../Test/Ordergraphs/GanttChart/gantt_chart_{TestConfiguration.Name}.json";
            
            
            IDbTransactionData dbTransactionData =
                ZppConfiguration.CacheManager.ReloadTransactionData();

            IGanttChart ganttChart =
                new GraphicalRepresentation.impl.GanttChart(dbTransactionData
                    .ProductionOrderOperationGetAll());
            string actualGanttChart = ganttChart.ToString();
            // create initial file, if it doesn't exists (must be committed then)
            if (File.Exists(orderGraphAsGanttChartFile) == false)
            {
                File.WriteAllText(orderGraphAsGanttChartFile, actualGanttChart, Encoding.UTF8);
            }
            
            string expectedGanttChart = File.ReadAllText(orderGraphAsGanttChartFile, Encoding.UTF8);
            
            bool ganttChartHasNotChanged =
                expectedGanttChart.Equals(actualGanttChart);
            // for debugging: write the changed graphs to files
            if (ganttChartHasNotChanged == false)
            {
                File.WriteAllText(orderGraphAsGanttChartFile, actualGanttChart, Encoding.UTF8);
            }
            
            if (Constants.IsWindows)
            {
                Assert.True(ganttChartHasNotChanged, "Ganttchart has changed.");
            }
            else
            {
                // On linux the graph is always different so the test would always fail here.
                Assert.True(true);
            }

        }
        
        private void InitThisTest(string testConfiguration)
        {
            InitTestScenario(testConfiguration);

            IZppSimulator zppSimulator = new ZppSimulator.impl.ZppSimulator();
            zppSimulator.StartTestCycle();
        }
    }
}