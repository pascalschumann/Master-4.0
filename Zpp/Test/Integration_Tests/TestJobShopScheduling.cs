using System;
using System.Collections.Generic;
using System.Linq;
using Master40.DB.DataModel;
using Xunit;
using Zpp.DataLayer;
using Zpp.DataLayer.impl;
using Zpp.DataLayer.impl.ProviderDomain.Wrappers;
using Zpp.Test.Configuration;
using Zpp.ZppSimulator;

namespace Zpp.Test.Integration_Tests
{
    public class TestJobShopScheduling : AbstractTest
    {
        public TestJobShopScheduling() : base(false)
        {
        }

        private void InitThisTest(string testConfiguration)
        {
            InitTestScenario(testConfiguration);
        }

        [Theory]
        [InlineData(TestConfigurationFileNames.TRUCK_COP_100_LOTSIZE_2)]
        public void TestEveryMachineHasOnlyOneOperationAtAnyTime(string testConfigurationFileName)
        {
            InitThisTest(testConfigurationFileName);

            IZppSimulator zppSimulator = new ZppSimulator.impl.ZppSimulator();
            zppSimulator.StartPerformanceStudy(false);

           ZppConfiguration.CacheManager.ReloadTransactionData();
            IDbMasterDataCache dbMasterDataCache =
                ZppConfiguration.CacheManager.GetMasterDataCache();
            IAggregator aggregator = new Aggregator(ZppConfiguration.CacheManager.GetDbTransactionDataArchive());

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
    }
}