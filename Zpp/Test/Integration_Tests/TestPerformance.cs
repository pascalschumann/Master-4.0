using System;
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
        
        private readonly ITestOutputHelper output;
        
        public TestPerformance(ITestOutputHelper output) : base(initDefaultTestConfig: false)
        {
            this.output = output;
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
            Assert.True( neededTime < MAX_TIME_FOR_MRP_RUN,
                $"MrpRun for example use case ({TestConfiguration.Name}) " +
                $"takes longer than {MAX_TIME_FOR_MRP_RUN} seconds: {neededTime}");
            
            output.WriteLine("This is output from");
        }

        [Theory]
        [InlineData(TestConfigurationFileNames.DESK_COP_500_LOTSIZE_2)]
        [InlineData(TestConfigurationFileNames.TRUCK_COP_500_LOTSIZE_2)]
        public void TestPerformanceStudy(string testConfigurationFileName)
        {
            InitThisTest(testConfigurationFileName);
            
            IZppSimulator zppSimulator = new ZppSimulator.impl.ZppSimulator();
            zppSimulator.StartPerformanceStudy();
            string performanceLog = DebuggingTools.ReadPerformanceLog();
            output.WriteLine($"{performanceLog}");
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