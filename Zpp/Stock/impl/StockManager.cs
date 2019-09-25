using System.Collections.Generic;
using System.Linq;
using Master40.DB.Data.WrappersForPrimitives;
using Master40.DB.DataModel;
using Master40.DB.Enums;
using Zpp.Common.DemandDomain;
using Zpp.Common.DemandDomain.Wrappers;
using Zpp.Common.DemandDomain.WrappersForCollections;
using Zpp.Common.ProviderDomain;
using Zpp.Common.ProviderDomain.Wrappers;
using Zpp.Common.ProviderDomain.WrappersForCollections;
using Zpp.Configuration;
using Zpp.DbCache;
using Zpp.Mrp.NodeManagement;
using Zpp.Utils;
using Zpp.WrappersForCollections;
using Zpp.WrappersForPrimitives;

namespace Zpp.Mrp.StockManagement
{
    public class StockManager : IStockManager
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        private readonly Dictionary<Id, Stock> _stocks = new Dictionary<Id, Stock>();
        private HashSet<Provider> _alreadyConsideredProviders = new HashSet<Provider>();

        private readonly IDbMasterDataCache _dbMasterDataCache =
            ZppConfiguration.CacheManager.GetMasterDataCache();

        private readonly ICacheManager _cacheManager = ZppConfiguration.CacheManager;

        private readonly IOpenDemandManager _openDemandManager =
            ZppConfiguration.CacheManager.GetOpenDemandManager();

        // for duplicating it
        public StockManager(IStockManager stockManager)
        {
            foreach (var stock in stockManager.GetStocks())
            {
                _stocks.Add(stock.GetArticleId(),
                    new Stock(stock.GetQuantity(), stock.GetArticleId(), stock.GetMinStockLevel()));
            }
        }

        public StockManager()
        {
            foreach (var stock in _dbMasterDataCache.M_StockGetAll())
            {
                Id articleId = new Id(stock.ArticleForeignKey);
                Stock myStock = new Stock(new Quantity(stock.Current), articleId,
                    new Quantity(stock.Min));
                _stocks.Add(articleId, myStock);
            }
        }

        public EntityCollector AdaptStock(Provider provider)
        {
            // a provider can influence the stock only once
            if (_alreadyConsideredProviders.Contains(provider))
            {
                return null;
            }

            _alreadyConsideredProviders.Add(provider);

            // SE:W decrements stock
            if (provider.GetType() == typeof(StockExchangeProvider))
            {
                Stock stock = _stocks[provider.GetArticleId()];
                stock.DecrementBy(provider.GetQuantity());
                Quantity currentQuantity = stock.GetQuantity();
                if (currentQuantity.IsSmallerThan(stock.GetMinStockLevel()))
                {
                    return CreateDependingDemands(provider.GetArticle(), provider,
                        provider.GetQuantity(), _openDemandManager, provider);
                }
            }

            return null;
        }

        /**
         * overrides (copy) stocks (including quantity) from given stockManager to this
         */
        public void AdaptStock(IStockManager stockManager)
        {
            foreach (var stock in stockManager.GetStocks())
            {
                _stocks[stock.GetArticleId()] = stock;
                _alreadyConsideredProviders = stockManager.GetAlreadyConsideredProviders();
            }
        }

        public List<Stock> GetStocks()
        {
            return _stocks.Values.ToList();
        }

        public static void CalculateCurrent(M_Stock stock, Quantity startQuantity)
        {
            Quantity currentQuantity = new Quantity(startQuantity);
            // TODO
        }

        public Stock GetStockById(Id id)
        {
            return _stocks[id];
        }

        public ResponseWithProviders Satisfy(Demand demand, Quantity demandedQuantity)
        {
            Stock stock = _stocks[demand.GetArticleId()];

            Providers providers = new Providers();
            List<T_DemandToProvider> demandToProviders = new List<T_DemandToProvider>();

            Provider stockProvider = CreateStockExchangeProvider(demand.GetArticle(),
                demand.GetDueTime(), demandedQuantity);
            providers.Add(stockProvider);

            T_DemandToProvider demandToProvider = new T_DemandToProvider()
            {
                DemandId = demand.GetId().GetValue(),
                ProviderId = stockProvider.GetId().GetValue(),
                Quantity = demandedQuantity.GetValue()
            };
            demandToProviders.Add(demandToProvider);


            return new ResponseWithProviders(providers, demandToProviders, demandedQuantity);
        }

        public ResponseWithProviders SatisfyByAvailableStockQuantity(Demand demand,
            Quantity demandedQuantity)
        {
            Stock stock = _stocks[demand.GetArticleId()];
            if (stock.GetQuantity().IsGreaterThan(Quantity.Null()))
            {
                Quantity reservedQuantity;
                if (demandedQuantity.IsGreaterThan(stock.GetQuantity()))
                {
                    reservedQuantity = stock.GetQuantity();
                }
                else
                {
                    reservedQuantity = demandedQuantity;
                }

                Provider stockProvider = CreateStockExchangeProvider(demand.GetArticle(),
                    demand.GetDueTime(), reservedQuantity);

                T_DemandToProvider demandToProvider = new T_DemandToProvider()
                {
                    DemandId = demand.GetId().GetValue(),
                    ProviderId = stockProvider.GetId().GetValue(),
                    Quantity = stockProvider.GetQuantity().GetValue()
                };
                return new ResponseWithProviders(stockProvider, demandToProvider, demandedQuantity);
            }
            else
            {
                return new ResponseWithProviders((Provider) null, null, demandedQuantity);
            }
        }

        /**
         * returns a provider, which can be a stockExchangeProvider, if article can be fulfilled by stock, else
         * a productionOrder/purchaseOrderPart
         */
        public Provider CreateStockExchangeProvider(M_Article article, DueTime dueTime,
            Quantity demandedQuantity)
        {
            M_Stock stock = _dbMasterDataCache.M_StockGetByArticleId(article.GetId());
            T_StockExchange stockExchange = new T_StockExchange();
            stockExchange.StockExchangeType = StockExchangeType.Provider;
            stockExchange.Quantity = demandedQuantity.GetValue();
            stockExchange.State = State.Created;

            stockExchange.Stock = stock;
            stockExchange.StockId = stock.Id;
            stockExchange.RequiredOnTime = dueTime.GetValue();
            stockExchange.ExchangeType = ExchangeType.Withdrawal;
            StockExchangeProvider stockExchangeProvider = new StockExchangeProvider(stockExchange);

            return stockExchangeProvider;
        }

        public HashSet<Provider> GetAlreadyConsideredProviders()
        {
            return _alreadyConsideredProviders;
        }

        private EntityCollector CreateDependingDemands(M_Article article, Provider parentProvider,
            Quantity demandedQuantity, IOpenDemandManager openDemandManager, Provider provider)
        {
            if (demandedQuantity.IsNull())
            {
                return null;
            }

            Demands stockExchangeDemands = new Demands();
            ProviderToDemandTable providerToDemandTable = new ProviderToDemandTable();

            // try to provider by existing demand
            ResponseWithDemands responseWithDemands =
                openDemandManager.SatisfyProviderByOpenDemand(provider, demandedQuantity);

            if (responseWithDemands.GetDemands().Count() > 1)
            {
                throw new MrpRunException("Only one demand should be reservable.");
            }

            Quantity remainingQuantity = new Quantity(demandedQuantity);
            if (responseWithDemands.CalculateReservedQuantity().IsGreaterThan(Quantity.Null()))
            {
                stockExchangeDemands.AddAll(responseWithDemands.GetDemands());
                providerToDemandTable.AddAll(responseWithDemands.GetProviderToDemands());
                remainingQuantity = responseWithDemands.GetRemainingQuantity();
            }

            if (responseWithDemands.IsSatisfied() == false)
            {
                LotSize.LotSize lotSizes = new LotSize.LotSize(remainingQuantity, article.GetId());
                Quantity lotSizeSum = Quantity.Null();
                foreach (var lotSize in lotSizes.GetLotSizes())
                {
                    lotSizeSum.IncrementBy(lotSize);


                    Demand stockExchangeDemand =
                        StockExchangeDemand.CreateStockExchangeStockDemand(article,
                            provider.GetDueTime(), lotSize);
                    stockExchangeDemands.Add(stockExchangeDemand);

                    // quantityToReserve can be calculated as following
                    // given demandedQuantity - (sumLotSize - lotSize) - (lotSize - providedByOpen.remaining)
                    Quantity quantityOfNewCreatedDemandToReserve = demandedQuantity
                        .Minus(lotSizeSum.Minus(lotSize)).Minus(
                            demandedQuantity.Minus(responseWithDemands.GetRemainingQuantity()));

                    providerToDemandTable.Add(provider, stockExchangeDemand.GetId(),
                        quantityOfNewCreatedDemandToReserve);

                    if (lotSizeSum.IsGreaterThan(demandedQuantity))
                        // remember created demand as openDemand
                    {
                        openDemandManager.AddDemand(stockExchangeDemand,
                            quantityOfNewCreatedDemandToReserve);
                    }


                    /*if (stockExchangeDemands.GetQuantity().IsSmallerThan(lotSize))
                    {
                        throw new MrpRunException($"Created demand should have not a smaller " +
                                                  $"quantity ({stockExchangeDemand.GetQuantity()}) " +
                                                  $"than the needed quantity ({lotSize}).");
                    }*/
                }
            }

            EntityCollector entityCollector = new EntityCollector();
            entityCollector._demands.AddAll(stockExchangeDemands);
            entityCollector._providerToDemandTable.AddAll(providerToDemandTable);
            return entityCollector;
        }
    }
}