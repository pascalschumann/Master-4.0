using Xunit;
using Zpp.Configuration;
using Zpp.DbCache;

namespace Zpp.Test.Unit_Tests.Provider
{
    public class TestPurchaseOrderPart : AbstractTest
    {


        public TestPurchaseOrderPart()
        {
            
        }
        
        /**
         * Verifies, that 
         * - 
         */
        [Fact(Skip = "Not implemented yet.")]
        public void TestPurchaseOrderPartCreatePurchaseOrderPart()
        {
            
            IDbTransactionData dbTransactionData =
                ZppConfiguration.CacheManager.ReloadTransactionData();
            
            // TODO
            Assert.True(false);
        }            

    }
}