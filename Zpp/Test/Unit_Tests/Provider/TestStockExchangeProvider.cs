using System;
using System.Linq;
using Master40.DB.DataModel;
using Xunit;
using Zpp.Configuration;
using Zpp.DbCache;
using Zpp.Mrp.StockManagement;
using Zpp.WrappersForPrimitives;


namespace Zpp.Test.Unit_Tests.Provider
{
    public class TestStockExchangeProvider : AbstractTest
    {
        private Random random = new Random();

        public TestStockExchangeProvider()
        {
        }

        /**
         * Verifies, that created StockExchangeProvider has correct 
         * - Quantity (must equal quantity of demand),
         * - DueTime (must equal dueTime of demand),
         * - Article (must equal article of demand)
         * - exact one dependingDemands with quantity == (current-demanded) * (-1) if stock.min == 0 else quantity == stock.min(max would
         * lead to overfilled stock because most time it gets round up due to packsize)
         * - TODO: sync this with the test impl, since this description is always a step behind
         */
        [Fact]
        public void TestCreateStockExchangeProvider()
        {
            IDbMasterDataCache dbMasterDataCache =
                ZppConfiguration.CacheManager.GetMasterDataCache();
            IDbTransactionData dbTransactionData =
                ZppConfiguration.CacheManager.ReloadTransactionData();

            // CustomerOrderPart
            Common.DemandDomain.Demand randomCustomerOrderPart =
                EntityFactory.CreateCustomerOrderPartRandomArticleToBuy(new Random().Next(3, 99),
                    new DueTime(50));
            Common.DemandDomain.Demand[] demands = new[]
            {
                randomCustomerOrderPart,
                EntityFactory.CreateCustomerOrderPartWithGivenArticle(new Random().Next(1001, 1999),
                    dbMasterDataCache.M_ArticleGetAll().First(x => x.ToPurchase), new DueTime(100)),
            };
            foreach (var demand in demands)
            {
                M_Stock stock = dbMasterDataCache.M_StockGetByArticleId(demand.GetArticleId());

                IStockManager stockManager = new StockManager();
                Common.ProviderDomain.Provider providerStockExchange =
                    stockManager.CreateStockExchangeProvider(demand.GetArticle(),
                        demand.GetDueTime(), demand.GetQuantity());
                Assert.True(providerStockExchange.GetQuantity().Equals(demand.GetQuantity()),
                    "Quantity is not correct.");
                Assert.True(providerStockExchange.GetArticle().Equals(demand.GetArticle()),
                    "Article is not correct.");
                Assert.True(
                    providerStockExchange.GetDueTime()
                        .Equals(demand.GetDueTime()), "DueTime is not correct.");
            }
        }

        [Fact]
        public void TestNoDependingDemandsIfStockHasEnough()
        {
        IDbMasterDataCache dbMasterDataCache =
            ZppConfiguration.CacheManager.GetMasterDataCache();
            IDbTransactionData dbTransactionData =
                ZppConfiguration.CacheManager.ReloadTransactionData();

            // CustomerOrderPart
            Common.DemandDomain.Demand demand =
                EntityFactory.CreateCustomerOrderPartRandomArticleToBuy(new Random().Next(1, 9),
                    new DueTime(50));

            M_Stock stock = dbMasterDataCache.M_StockGetByArticleId(demand.GetArticleId());
            // increase stock
            stock.Current = 10;
            IStockManager stockManager = new StockManager();
            Common.ProviderDomain.Provider providerStockExchange =
                stockManager.CreateStockExchangeProvider(demand.GetArticle(),
                    demand.GetDueTime(), demand.GetQuantity());


            Assert.True(providerStockExchange.AnyDependingDemands() == false,
                "Provider should have no depending demands.");
        }
    }
}