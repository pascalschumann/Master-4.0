using System;
using System.Collections.Generic;
using Master40.DB.Data.Helper;
using Master40.DB.Data.WrappersForPrimitives;
using Master40.DB.DataModel;
using Master40.SimulationCore.Helper;
using Master40.XUnitTest.DBContext;
using Xunit;
using Zpp.DemandDomain;
using Zpp.ProviderDomain;

namespace Zpp.Test
{
    public class TestStockExchangeProvider : AbstractTest
    {
        private const int ORDER_QUANTITY = 6;
        private const int DEFAULT_LOT_SIZE = 2;
        private Random random = new Random();

        public TestStockExchangeProvider()
        {
            OrderGenerator.GenerateOrdersSyncron(ProductionDomainContext,
                ContextTest.TestConfiguration(), 1, true, ORDER_QUANTITY);
            LotSize.LotSize.SetDefaultLotSize(new Quantity(DEFAULT_LOT_SIZE));
        }

        /**
         * Verifies, that created StockExchangeProvider has correct 
         * - Quantity (must equal quantity of demand),
         * - DueTime (must equal dueTime of demand),
         * - Article (must equal article of demand)
         */
        [Fact]
        public void TestCreateStockExchangeProvider()
        {
            IDbMasterDataCache dbMasterDataCache = new DbMasterDataCache(ProductionDomainContext);
            IDbTransactionData dbTransactionData =
                new DbTransactionData(ProductionDomainContext, dbMasterDataCache);

            // CustomerOrderPart
            Demand[] demands = new[]
            {
                EntityFactory.CreateCustomerOrderPart(dbMasterDataCache, new Random().Next(1, 100)),
                EntityFactory.CreateCustomerOrderPart(dbMasterDataCache, new Random().Next(1000, 2000))
            };
            foreach (var demand in demands)
            {


                M_Stock stock = dbMasterDataCache.M_StockGetByArticleId(demand.GetArticleId());
                decimal oldCurrentStock = stock.Current;

                Provider providerStockExchange = StockExchangeProvider.CreateStockExchangeProvider(
                    demand.GetArticle(), demand.GetDueTime(), demand.GetQuantity(),
                    dbMasterDataCache, dbTransactionData);
                Assert.True(providerStockExchange.GetQuantity().Equals(demand.GetQuantity()),
                    "Quantity is not correct.");
                Assert.True(providerStockExchange.GetArticle().Equals(demand.GetArticle()),
                    "Article is not correct.");
                Assert.True(providerStockExchange.GetDueTime().Equals(demand.GetDueTime()),
                    "DueTime is not correct.");
                // depending demands
                decimal newCurrentStock = stock.Current;
                Assert.True(newCurrentStock == oldCurrentStock - demand.GetQuantity().GetValue(),
                    "stock.Current ist not correct.");
                if (newCurrentStock < 0)
                {
                    Assert.True(providerStockExchange.AnyDependingDemands(),
                        $"Provider {providerStockExchange} for demand {demand} " +
                        $"has no depending Demands.");
                }

                // ProductionOrderBom TODO

            }
        }
    }
}