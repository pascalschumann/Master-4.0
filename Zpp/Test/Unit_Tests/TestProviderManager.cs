using System.Linq;
using Master40.DB.Data.WrappersForPrimitives;
using Master40.DB.DataModel;
using Xunit;
using Zpp.Common.DemandDomain.Wrappers;
using Zpp.Common.ProviderDomain.Wrappers;
using Zpp.Configuration;
using Zpp.DbCache;
using Zpp.Mrp;
using Zpp.Mrp.NodeManagement;
using Zpp.Mrp.StockManagement;
using Zpp.WrappersForPrimitives;

namespace Zpp.Test.Unit_Tests
{
    public class TestProviderManager : AbstractTest
    {
        public TestProviderManager()
        {
        }

        /**
         * Verifies, that a demand (COP, PrOBom) is fulfilled by an existing provider, if such exists
         * - 
         */
        [Fact(Skip="Must be completely rewritten")]
        // TODO: Must be completely rewritten
        public void TestSatisfyByExistingDemand()
        {
            
            IDbTransactionData dbTransactionData =
                ZppConfiguration.CacheManager.ReloadTransactionData();

            StockManager stockManager =
                new StockManager();
            
            IOpenDemandManager openDemandManager = new OpenDemandManager();

            IProviderManager providerManager = new ProviderManager(dbTransactionData);
            IProvidingManager providingManager = (IProvidingManager) providerManager;
            CustomerOrderPart customerOrderPart1 =
                EntityFactory.CreateCustomerOrderPartRandomArticleToBuy(4, new DueTime(50));
            CustomerOrderPart customerOrderPart2 =
                EntityFactory.CreateCustomerOrderPartRandomArticleToBuy(5, new DueTime(100));
            ProductionOrder productionOrder = EntityFactory.CreateT_ProductionOrder(
                dbTransactionData, customerOrderPart1,
                customerOrderPart1.GetQuantity().Plus(customerOrderPart2.GetQuantity()));
            providerManager.AddProvider(customerOrderPart1.GetId(), productionOrder,
                customerOrderPart1.GetQuantity());

            ResponseWithProviders responseWithProviders = providingManager.Satisfy(customerOrderPart2,
                customerOrderPart2.GetQuantity(), dbTransactionData);
            MrpRun.ProcessProvidingResponse(responseWithProviders, providerManager, stockManager,
                dbTransactionData, customerOrderPart2, openDemandManager);

            bool isSatisfied = responseWithProviders.IsSatisfied() && responseWithProviders.GetProviders().Any() == false &&
                               responseWithProviders.GetDemandToProviders().Count == 1 &&
                               IsValidDemandToProvider(responseWithProviders.GetDemandToProviders()[0],
                                   customerOrderPart2, productionOrder,
                                   customerOrderPart2.GetQuantity());
            Assert.True(isSatisfied, "Demand was not satisfied by existing provider.");
        }

        private bool IsValidDemandToProvider(T_DemandToProvider demandToProvider,
            Common.DemandDomain.Demand expectedDemand, Common.ProviderDomain.Provider expectedProvider, Quantity expectedQuantity)
        {
            return demandToProvider.DemandId.Equals(expectedDemand.GetId().GetValue()) &&
                   demandToProvider.ProviderId.Equals(expectedProvider.GetId().GetValue()) &&
                   demandToProvider.Quantity.Equals(expectedQuantity.GetValue());
        }

        /**
         * Verifies, that 
         * - 
         */
        [Fact(Skip = "Not implemented yet.")]
        public void TestAddProvider()
        {
            
            IDbTransactionData dbTransactionData =
                ZppConfiguration.CacheManager.ReloadTransactionData();

            // TODO
            Assert.True(false);
        }
    }
}