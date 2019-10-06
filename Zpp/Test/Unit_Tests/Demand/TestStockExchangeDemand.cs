using Xunit;
using Zpp.Configuration;
using Zpp.DataLayer;

namespace Zpp.Test.Unit_Tests.Demand
{
    public class TestStockExchangeDemand : AbstractTest
    {


        public TestStockExchangeDemand()
        {
            
        }
        
        /**
         * Verifies, that 
         * - 
         */
        [Fact(Skip = "Not implemented yet.")]
        public void TestCreateStockExchangeStockDemand()
        {
            
            IDbTransactionData dbTransactionData =
                ZppConfiguration.CacheManager.ReloadTransactionData();
            
            // TODO
            Assert.True(false);
        } 
    }
}