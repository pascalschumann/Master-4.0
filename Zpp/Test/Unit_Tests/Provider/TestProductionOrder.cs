using Xunit;
using Zpp.DataLayer;
using Zpp.Test.Integration_Tests;

namespace Zpp.Test.Unit_Tests.Provider
{
    public class TestProductionOrder : AbstractTest
    {


        public TestProductionOrder()
        {
            
        }
        
        /**
         * Verifies, that 
         * - 
         */
        [Fact(Skip = "Not implemented yet.")]
        public void TestCreateProductionOrder()
        {
            
            IDbTransactionData dbTransactionData =
                ZppConfiguration.CacheManager.ReloadTransactionData();
            
            // TODO
            Assert.True(false);
        }
        
        /**
         * Verifies, that 
         * - 
         */
        [Fact(Skip = "Not implemented yet.")]
        public void TestCreateProductionOrderBoms()
        {
            
            IDbTransactionData dbTransactionData =
                ZppConfiguration.CacheManager.ReloadTransactionData();
            
            // TODO
            Assert.True(false);
        }  
    }
}