using System.Collections.Generic;
using System.Linq;
using Master40.DB.Data.WrappersForPrimitives;
using Master40.DB.Interfaces;
using Xunit;
using Zpp.DataLayer;
using Zpp.DataLayer.impl.DemandDomain.WrappersForCollections;
using Zpp.DataLayer.impl.ProviderDomain.WrappersForCollections;
using Zpp.DataLayer.impl.WrappersForCollections;
using Zpp.ZppSimulator;

namespace Zpp.Test.Integration_Tests
{
    public class TestDemandToProvider : AbstractTest
    {
        public TestDemandToProvider()
        {
        }

        [Fact]
        public void TestAllDemandsAreInDemandToProviderTable()
        {
            IZppSimulator zppSimulator = new ZppSimulator.impl.ZppSimulator();
            zppSimulator.StartTestCycle();

            IDbTransactionData dbTransactionData =
                ZppConfiguration.CacheManager.ReloadTransactionData();

            IDemands allDbDemands = dbTransactionData.DemandsGetAll();
            IDemandToProviderTable demandToProviderTable =
                dbTransactionData.DemandToProviderGetAll();

            foreach (var demand in allDbDemands)
            {
                bool isInDemandToProviderTable = demandToProviderTable.Contains(demand);
                Assert.True(isInDemandToProviderTable,
                    $"Demand {demand} is NOT in demandToProviderTable.");
            }
        }

        /**
         * Tests, if the demands are theoretically satisfied by looking for providers in ProviderTable
         * --> success does not mean, that the demands from demandToProvider table are satisfied by providers from demandToProviderTable
         */
        [Fact]
        public void TestAllDemandsAreSatisfiedWithinProviderTable()
        {
            IZppSimulator zppSimulator = new ZppSimulator.impl.ZppSimulator();
            zppSimulator.StartTestCycle();

            IDbTransactionData dbTransactionData =
                ZppConfiguration.CacheManager.ReloadTransactionData();

            IDemands demands = dbTransactionData.DemandsGetAll();
            IProviders providers = dbTransactionData.ProvidersGetAll();
            IDemands unsatisfiedDemands = providers.CalculateUnsatisfiedDemands(demands);
            foreach (var unsatisfiedDemand in unsatisfiedDemands)
            {
                Assert.True(false,
                    $"The demand {unsatisfiedDemand} should be satisfied, but it is NOT.");
            }
        }

        [Fact]
        public void TestAllDemandsAreSatisfiedByProvidersOfDemandToProviderTable()
        {
            IZppSimulator zppSimulator = new ZppSimulator.impl.ZppSimulator();
            zppSimulator.StartTestCycle();

            IDbTransactionData dbTransactionData =
                ZppConfiguration.CacheManager.ReloadTransactionData();

            IDemands allDbDemands = dbTransactionData.DemandsGetAll();
            foreach (var demand in allDbDemands)
            {
                Quantity satisfiedQuantity = Quantity.Null();
                dbTransactionData.DemandToProviderGetAll().Select(x =>
                {
                    satisfiedQuantity.IncrementBy(x.Quantity);
                    return x;
                }).Where(x => x.GetDemandId().Equals(demand.GetId()));
                Assert.True(satisfiedQuantity.Equals(demand.GetQuantity()),
                    $"Demand {demand} is not satisfied.");
            }
        }

        [Fact]
        public void TestEveryQuantityOnArrowIsPositive()
        {
            IZppSimulator zppSimulator = new ZppSimulator.impl.ZppSimulator();
            zppSimulator.StartTestCycle();

            IDbTransactionData dbTransactionData =
                ZppConfiguration.CacheManager.ReloadTransactionData();

            List<ILinkDemandAndProvider>
                demandAndProviderLinks = new List<ILinkDemandAndProvider>();
            demandAndProviderLinks.AddRange(dbTransactionData.DemandToProviderGetAll());
            demandAndProviderLinks.AddRange(dbTransactionData.ProviderToDemandGetAll());

            foreach (var demandAndProviderLink in demandAndProviderLinks)
            {
                Assert.False(
                    demandAndProviderLink.GetQuantity().IsNegative() ||
                    demandAndProviderLink.GetQuantity().IsNull(),
                    $"A quantity on arrow ({demandAndProviderLink}) cannot be negative or null.");
            }
        }
    }
}