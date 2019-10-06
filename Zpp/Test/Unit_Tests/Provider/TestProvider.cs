using Xunit;
using Zpp.Configuration;
using Zpp.DataLayer;

namespace Zpp.Test.Unit_Tests.Provider
{
    public class TestProvider : AbstractTest
    {


        public TestProvider()
        {

        }

        /**
         * Verifies, that 
         * - 
         */
        [Fact(Skip = "Not implemented yet.")]
        public void TestCreateNeededDemands()
        {
            
            IDbTransactionData dbTransactionData =
                ZppConfiguration.CacheManager.ReloadTransactionData();
            
            // TODO
            Assert.True(false);
        }

    }
}