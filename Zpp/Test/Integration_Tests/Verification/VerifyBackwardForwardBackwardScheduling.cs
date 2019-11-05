using Xunit;
using Zpp.Test.Configuration;
using Zpp.ZppSimulator;

namespace Zpp.Test.Integration_Tests.Verification
{
    public class VerifyBackwardForwardBackwardScheduling: AbstractVerification
    {
    
        [Theory]
        [InlineData(TestConfigurationFileNames.TRUCK_COP_5_LOTSIZE_2)]
        [InlineData(TestConfigurationFileNames.TRUCK_COP_100_LOTSIZE_2)]
        [InlineData(TestConfigurationFileNames.TRUCK_INTERVAL_20160_COP_100_LOTSIZE_2)]
        public void TestBackwardForwardBackwardScheduling(string testConfigurationFileName)
        {
            InitThisTest(testConfigurationFileName);
        }
    }
}