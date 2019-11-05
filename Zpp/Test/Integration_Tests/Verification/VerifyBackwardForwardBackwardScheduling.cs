using Xunit;
using Zpp.DataLayer;
using Zpp.DataLayer.impl.DemandDomain.WrappersForCollections;
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
            
            IDbTransactionData dbTransactionData =
                ZppConfiguration.CacheManager.GetDbTransactionData();
            IDbTransactionData dbTransactionDataArchive =
                ZppConfiguration.CacheManager.GetDbTransactionDataArchive();
        }
        
        /**
         * Can only operate on one executed mrp2, simulation can not be used,
         * since confirmations would be applied and therefore no connection between ProductionOrderBoms
         * and its child StockExchangeProviders would exist anymore
         */
        [Theory]
        [InlineData(TestConfigurationFileNames.TRUCK_COP_5_LOTSIZE_2)]
        [InlineData(TestConfigurationFileNames.TRUCK_INTERVAL_20160_COP_100_LOTSIZE_2)]
        public void TestEveryOperationHasNeededMaterialAtStartBackward(string testConfigurationFileName
        )
        {
            InitTestScenario(testConfigurationFileName);

            IZppSimulator zppSimulator = new ZppSimulator.impl.ZppSimulator();
            // TODO: set to true once dbPersist() has an acceptable time
            zppSimulator.StartTestCycle(false);

            // TODO: replace this by ReloadTransactionData() once shouldPersist is enabled
            IDbTransactionData dbTransactionData =
                ZppConfiguration.CacheManager.GetDbTransactionData();
            IAggregator aggregator = ZppConfiguration.CacheManager.GetAggregator();
            
            foreach (var operation in dbTransactionData.ProductionOrderOperationGetAll())
            {
                Demands productionOrderBoms = aggregator.GetProductionOrderBomsBy(operation);
                foreach (var productionOrderBom in productionOrderBoms)
                {
                    
                    foreach (var stockExchangeProvider in aggregator.GetAllChildProvidersOf(
                        productionOrderBom))
                    {
                        Assert.True(operation.GetStartTimeBackward().IsGreaterThanOrEqualTo(
                                    stockExchangeProvider.GetEndTimeBackward()));
                    }
                }
            }
        }
    }
}