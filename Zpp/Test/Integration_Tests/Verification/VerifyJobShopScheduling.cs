using System.Collections.Generic;
using System.Linq;
using Master40.DB.DataModel;
using Xunit;
using Zpp.DataLayer;
using Zpp.DataLayer.impl;
using Zpp.DataLayer.impl.DemandDomain.Wrappers;
using Zpp.DataLayer.impl.DemandDomain.WrappersForCollections;
using Zpp.DataLayer.impl.ProviderDomain.Wrappers;
using Zpp.DataLayer.impl.WrappersForCollections;
using Zpp.Test.Configuration;
using Zpp.ZppSimulator;

namespace Zpp.Test.Integration_Tests.Verification
{
    public class VerifyJobShopScheduling : AbstractVerification
    {
        [Theory]
        [InlineData(TestConfigurationFileNames.TRUCK_COP_5_LOTSIZE_2)]
        [InlineData(TestConfigurationFileNames.TRUCK_COP_100_LOTSIZE_2)]
        [InlineData(TestConfigurationFileNames.TRUCK_INTERVAL_20160_COP_100_LOTSIZE_2)]
        public void TestJobShopScheduling(string testConfigurationFileName)
        {
            InitThisTest(testConfigurationFileName);

            IDbTransactionData dbTransactionData =
                ZppConfiguration.CacheManager.GetDbTransactionData();
            IAggregator aggregator = ZppConfiguration.CacheManager.GetAggregator();
            IDbTransactionData dbTransactionDataArchive =
                ZppConfiguration.CacheManager.GetDbTransactionDataArchive();
            IAggregator aggregatorArchive = new Aggregator(dbTransactionDataArchive);

            VerifyEveryOperationIsPlanned(dbTransactionData);
            VerifyEveryOperationIsPlanned(dbTransactionDataArchive);
            
            VerifyEveryMachineHasOnlyOneOperationAtAnyTime(aggregator);
            VerifyEveryMachineHasOnlyOneOperationAtAnyTime(aggregatorArchive);
            
        }

        private void VerifyEveryOperationIsPlanned(IDbTransactionData dbTransactionData)
        {
            foreach (var operation in dbTransactionData.ProductionOrderOperationGetAll())
            {
                Assert.True(operation.GetStartTime() >= 0);
                Assert.True(operation.GetValue().ResourceId != null);
            }
        }

        private void VerifyEveryMachineHasOnlyOneOperationAtAnyTime(IAggregator aggregator)
        {
            IDbMasterDataCache dbMasterDataCache =
                ZppConfiguration.CacheManager.GetMasterDataCache();

            foreach (var resource in dbMasterDataCache.ResourceGetAll())
            {
                List<ProductionOrderOperation> operations =
                    aggregator.GetAllOperationsOnResource(resource.GetValue());
                T_ProductionOrderOperation lastOperation = null;
                foreach (var operation in operations.OrderBy(x => x.GetValue().Start))
                {
                    T_ProductionOrderOperation tOperation = operation.GetValue();
                    if (lastOperation == null)
                    {
                        lastOperation = operation.GetValue();
                    }
                    else
                    {
                        Assert.True(lastOperation.End <= tOperation.Start,
                            $"Operations are overlapping: '{lastOperation}' and {tOperation}'.");
                    }
                }
            }
        }

        /**
         * Can only operate on one executed mrp2, simulation can not be used,
         * since confirmations would be applied and therefore no connection between ProductionOrderBoms
         * and its child StockExchangeProviders would exist anymore
         */
        [Theory]
        [InlineData(TestConfigurationFileNames.TRUCK_COP_5_LOTSIZE_2)]
        [InlineData(TestConfigurationFileNames.TRUCK_INTERVAL_20160_COP_100_LOTSIZE_2)]
        public void TestEveryOperationHasNeededMaterialAtStart(string testConfigurationFileName
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
                        Assert.True(operation.GetStartTime() >=
                                    stockExchangeProvider.GetEndTimeBackward().GetValue());
                    }
                }
            }
        }
    }
}