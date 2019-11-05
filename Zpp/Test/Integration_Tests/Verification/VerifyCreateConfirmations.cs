using Xunit;
using Zpp.DataLayer;
using Zpp.Test.Configuration;

namespace Zpp.Test.Integration_Tests.Verification
{
    public class VerifyCreateConfirmations : AbstractVerification
    {
        [Theory]
        [InlineData(TestConfigurationFileNames.TRUCK_COP_5_LOTSIZE_2)]
        [InlineData(TestConfigurationFileNames.TRUCK_COP_100_LOTSIZE_2)]
        [InlineData(TestConfigurationFileNames.TRUCK_INTERVAL_20160_COP_100_LOTSIZE_2)]
        public void TestCreateConfirmations(string testConfigurationFileName)
        {
            InitThisTest(testConfigurationFileName);
            
            IDbTransactionData dbTransactionData =
                ZppConfiguration.CacheManager.GetDbTransactionData();
            IDbTransactionData dbTransactionDataArchive =
                ZppConfiguration.CacheManager.GetDbTransactionDataArchive();
        }

    }
}