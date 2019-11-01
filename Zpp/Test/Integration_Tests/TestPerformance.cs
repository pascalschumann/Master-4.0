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
        public void TestPerformanceStudy(string testConfigurationFileName)
        {
            Stopwatch stopwatch = new Stopwatch();
            int maxPossibleCops = int.MaxValue / 100;
            ZppConfiguration.CacheManager.ReadInTestConfiguration(testConfigurationFileName);
            int copsCount = ZppConfiguration.CacheManager.GetTestConfiguration()
                .CustomerOrderPartQuantity;
            int elapsedMinutes = 0;
            int maxTime = 5;

            // find out, how much we can process
            while (copsCount < maxPossibleCops && elapsedMinutes < 5)
            {
                InitThisTest(testConfigurationFileName);

                copsCount *= 10;
                ZppConfiguration.CacheManager.GetTestConfiguration().CustomerOrderPartQuantity =
                    copsCount;
                IZppSimulator zppSimulator = new ZppSimulator.impl.ZppSimulator();
                stopwatch.Start();
                zppSimulator.StartPerformanceStudy(false);
                stopwatch.Stop();

                elapsedMinutes = stopwatch.Elapsed.Minutes;

                stopwatch.Reset();

                Assert.True(copsCount < maxPossibleCops && elapsedMinutes < maxTime,
                    $"Without Db persistence: copsCount ({copsCount}) cannot be greater (int.max) OR " + 
                    $"simulation needs  with {elapsedMinutes}min longer than {maxTime} min.");
            }
        }

        [Theory(Skip = "DbPersist() is not terminating.")]
        // [InlineData(TestConfigurationFileNames.DESK_COP_500_LOTSIZE_2)]
        [InlineData(TestConfigurationFileNames.TRUCK_COP_100_LOTSIZE_2)]
        public void TestPerformanceStudyWithDbPersist(string testConfigurationFileName)
        {
            Stopwatch stopwatch = new Stopwatch();
            int maxPossibleCops = int.MaxValue / 100;
            ZppConfiguration.CacheManager.ReadInTestConfiguration(testConfigurationFileName);
            int copsCount = ZppConfiguration.CacheManager.GetTestConfiguration()
                .CustomerOrderPartQuantity;
            int elapsedMinutes = 0;
            int maxTime = 5;

            // find out, how much we can process
            while (copsCount < maxPossibleCops && elapsedMinutes < 5)
            {
                InitThisTest(testConfigurationFileName);

                copsCount *= 10;
                ZppConfiguration.CacheManager.GetTestConfiguration().CustomerOrderPartQuantity =
                    copsCount;
                IZppSimulator zppSimulator = new ZppSimulator.impl.ZppSimulator();
                stopwatch.Start();
                zppSimulator.StartPerformanceStudy(true);
                stopwatch.Stop();

                elapsedMinutes = stopwatch.Elapsed.Minutes;

                stopwatch.Reset();

                Assert.True(copsCount < maxPossibleCops && elapsedMinutes < maxTime,
                    $"With Db persistence: copsCount ({copsCount}) cannot be greater (int.max) OR " + 
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