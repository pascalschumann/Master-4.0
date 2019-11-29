using System.IO;
using System.Text;
using Master40.DB.Data.WrappersForPrimitives;
using Xunit;
using Zpp.DataLayer;
using Zpp.GraphicalRepresentation;
using Zpp.GraphicalRepresentation.impl;
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

        [Fact]
        public void TestDetermineFreeGroup()
        {
            //  first case overlapping
            Interval interval1 = new Interval(new Id(1), new DueTime(740), new DueTime(836));
            Interval interval2 = new Interval(new Id(2), new DueTime(736), new DueTime(836));
            Assert.True(interval1.IntersectsExclusive(interval2));
        }
    }
}