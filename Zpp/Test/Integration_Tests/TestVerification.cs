using System.Linq;
using Master40.DB.Data.WrappersForPrimitives;
using Microsoft.EntityFrameworkCore.Internal;
using Xunit;
using Zpp.DataLayer;
using Zpp.DataLayer.impl.DemandDomain;
using Zpp.DataLayer.impl.ProviderDomain;
using Zpp.Test.Configuration;
using Zpp.ZppSimulator;

namespace Zpp.Test.Integration_Tests
{
    public class TestVerification : AbstractTest
    {
        public TestVerification() : base(initDefaultTestConfig: false)
        {
        }

        private void InitThisTest(string testConfiguration)
        {
            InitTestScenario(testConfiguration);

            IZppSimulator zppSimulator = new ZppSimulator.impl.ZppSimulator();
            // TODO: set to true once dbPersist() has an acceptable time and and enable ReloadTransactionData
            zppSimulator.StartPerformanceStudy(false);
            // IDbTransactionData dbTransactionData =
            //    ZppConfiguration.CacheManager.ReloadTransactionData();
        }

        [Theory]
        [InlineData(TestConfigurationFileNames.TRUCK_COP_5_LOTSIZE_2)]
        [InlineData(TestConfigurationFileNames.TRUCK_COP_100_LOTSIZE_2)]
        [InlineData(TestConfigurationFileNames.TRUCK_INTERVAL_20160_COP_100_LOTSIZE_2)]
        public void TestMrp1(string testConfigurationFileName)
        {
            InitThisTest(testConfigurationFileName);

            IDbTransactionData dbTransactionData =
                ZppConfiguration.CacheManager.GetDbTransactionData();
            IDbTransactionData dbTransactionDataArchive =
                ZppConfiguration.CacheManager.GetDbTransactionDataArchive();

            VerifyMrp1(dbTransactionData);
            VerifyMrp1(dbTransactionDataArchive);

        }

        private void VerifyMrp1(IDbTransactionData dbTransactionData)
        {
            Assert.True(dbTransactionData.DemandToProviderGetAll().Any());
            foreach (var demandToProvider in dbTransactionData.DemandToProviderGetAll())
            {
                Demand demand = dbTransactionData.DemandsGetById(demandToProvider.GetDemandId());
                Provider provider =
                    dbTransactionData.ProvidersGetById(demandToProvider.GetProviderId());
                
                // every quantity > 0
                Assert.True(demand.GetQuantity().IsGreaterThan(Quantity.Null()));
                Assert.True(provider.GetQuantity().IsGreaterThan(Quantity.Null()));
                Assert.True(demandToProvider.GetQuantity().IsGreaterThan(Quantity.Null()));
                
                // demand's quantity <= provider's quantity
                Assert.True(demand.GetQuantity().IsSmallerThanOrEqualTo(provider.GetQuantity()));
                // demand's quantity >= demandToProvider's quantity
                Assert.True(demand.GetQuantity()
                    .IsGreaterThanOrEqualTo(demandToProvider.GetQuantity()));
                // provider's quantity >= demandToProvider's quantity
                Assert.True(provider.GetQuantity()
                    .IsGreaterThanOrEqualTo(demandToProvider.GetQuantity()));
            }
            
            Assert.True(dbTransactionData.ProviderToDemandGetAll().Any());
            foreach (var providerToDemand in dbTransactionData.ProviderToDemandGetAll())
            {
                Demand demand = dbTransactionData.DemandsGetById(providerToDemand.GetDemandId());
                Provider provider =
                    dbTransactionData.ProvidersGetById(providerToDemand.GetProviderId());

                // every quantity > 0
                Assert.True(demand.GetQuantity().IsGreaterThan(Quantity.Null()));
                Assert.True(provider.GetQuantity().IsGreaterThan(Quantity.Null()));
                Assert.True(providerToDemand.GetQuantity().IsGreaterThan(Quantity.Null()));
                
                // demand's quantity >= providerToDemand's quantity
                Assert.True(demand.GetQuantity()
                    .IsGreaterThanOrEqualTo(providerToDemand.GetQuantity()));
            }
        }

        [Theory]
        [InlineData(TestConfigurationFileNames.TRUCK_COP_5_LOTSIZE_2)]
        [InlineData(TestConfigurationFileNames.TRUCK_COP_100_LOTSIZE_2)]
        [InlineData(TestConfigurationFileNames.TRUCK_INTERVAL_20160_COP_100_LOTSIZE_2)]
        public void TestBackwardForwardBackwardScheduling(string testConfigurationFileName)
        {
            InitThisTest(testConfigurationFileName);
        }

        [Theory]
        [InlineData(TestConfigurationFileNames.TRUCK_COP_5_LOTSIZE_2)]
        [InlineData(TestConfigurationFileNames.TRUCK_COP_100_LOTSIZE_2)]
        [InlineData(TestConfigurationFileNames.TRUCK_INTERVAL_20160_COP_100_LOTSIZE_2)]
        public void TestJobShopScheduling(string testConfigurationFileName)
        {
            InitThisTest(testConfigurationFileName);
        }

        [Theory]
        [InlineData(TestConfigurationFileNames.TRUCK_COP_5_LOTSIZE_2)]
        [InlineData(TestConfigurationFileNames.TRUCK_COP_100_LOTSIZE_2)]
        [InlineData(TestConfigurationFileNames.TRUCK_INTERVAL_20160_COP_100_LOTSIZE_2)]
        public void TestCreateConfirmations(string testConfigurationFileName)
        {
            InitThisTest(testConfigurationFileName);
        }


        [Theory]
        [InlineData(TestConfigurationFileNames.TRUCK_COP_5_LOTSIZE_2)]
        [InlineData(TestConfigurationFileNames.TRUCK_COP_100_LOTSIZE_2)]
        [InlineData(TestConfigurationFileNames.TRUCK_INTERVAL_20160_COP_100_LOTSIZE_2)]
        public void TestApplyConfirmations(string testConfigurationFileName)
        {
            InitThisTest(testConfigurationFileName);
        }
    }
}