using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using Xunit;
using Xunit.Abstractions;
using Zpp.Test.Configuration;
using Zpp.Util;
using Zpp.ZppSimulator;

namespace Zpp.Test.Integration_Tests
{
    public class TestPerformance : AbstractTest
    {
        public TestPerformance() : base(initDefaultTestConfig: false)
        {
        }

        private void InitThisTest(string testConfiguration)
        {
            InitTestScenario(testConfiguration);
        }


        [Theory]
        [InlineData(TestConfigurationFileNames.TRUCK_COP_5_LOTSIZE_2)]
        [InlineData(TestConfigurationFileNames.DESK_COP_2_LOTSIZE_2)]
        public void TestMaxTimeForMrpRunIsNotExceeded(string testConfigurationFileName)
        {
            const int MAX_TIME_FOR_MRP_RUN = 90;

            InitThisTest(testConfigurationFileName);

            DateTime startTime = DateTime.UtcNow;

            IZppSimulator zppSimulator = new ZppSimulator.impl.ZppSimulator();
            zppSimulator.StartTestCycle();

            DateTime endTime = DateTime.UtcNow;
            double neededTime = (endTime - startTime).TotalMilliseconds / 1000;
            Assert.True(neededTime < MAX_TIME_FOR_MRP_RUN,
                $"MrpRun for example use case ({TestConfiguration.Name}) " +
                $"takes longer than {MAX_TIME_FOR_MRP_RUN} seconds: {neededTime}");
        }

        [Theory]
        // [InlineData(TestConfigurationFileNames.DESK_COP_500_LOTSIZE_2)]
        [InlineData(TestConfigurationFileNames.TRUCK_COP_100_LOTSIZE_2)]
        [InlineData(TestConfigurationFileNames.TRUCK_INTERVAL_20160_COP_100_LOTSIZE_2)]
        public void TestPerformanceStudyWithoutDbPersist(string testConfigurationFileName)
        {
            Stopwatch stopwatch = new Stopwatch();
            int maxPossibleCops = int.MaxValue / 100;
            ZppConfiguration.CacheManager.ReadInTestConfiguration(testConfigurationFileName);
            TestConfiguration testConfiguration =
                ZppConfiguration.CacheManager.GetTestConfiguration();
            int customerOrderCount = ZppConfiguration.CacheManager.GetTestConfiguration()
                .CustomerOrderPartQuantity;
            int elapsedMinutes = 0;
            int maxTime = 5;
            int cycles = testConfiguration.SimulationMaximumDuration /
                         testConfiguration.SimulationInterval;

            // n cycles here each cycle create & plan configured CustomerOrderPart 
            while (customerOrderCount <= maxPossibleCops && elapsedMinutes < 5)
            {
                InitThisTest(testConfigurationFileName);

                customerOrderCount *= 10;
                testConfiguration.CustomerOrderPartQuantity =
                    customerOrderCount;
                IZppSimulator zppSimulator = new ZppSimulator.impl.ZppSimulator();
                stopwatch.Start();
                // TODO: set this to true once the dbPersisting is terminating in a practical time (rename method)
                zppSimulator.StartPerformanceStudy(false);
                stopwatch.Stop();

                elapsedMinutes = stopwatch.Elapsed.Minutes;

                stopwatch.Reset();

                Assert.True(customerOrderCount < maxPossibleCops && elapsedMinutes < maxTime,
                    $"{testConfigurationFileName}, without Db persistence: customerOrderCount ({customerOrderCount}) " +
                    $"per interval (0-{testConfiguration.SimulationInterval}) in {cycles} cannot be greater (int.max) OR " + 
                    $"simulation needs  with {elapsedMinutes}min longer than {maxTime} min.");
            }
        }

        /**
         * without confirmations to compare it with performanceStudy (which includes confirmations)
         */
        [Theory]
        [InlineData(TestConfigurationFileNames.DESK_COP_2_LOTSIZE_2)]
        [InlineData(TestConfigurationFileNames.TRUCK_COP_2_LOTSIZE_2)]
        // [InlineData(TestConfigurationFileNames.TRUCK_COP_500_LOTSIZE_2)]
        public void TestMultipleTestCycles(string testConfigurationFileName)
        {
            InitThisTest(testConfigurationFileName);

            IZppSimulator zppSimulator = new ZppSimulator.impl.ZppSimulator();
            zppSimulator.StartMultipleTestCycles();
        }
    }
}