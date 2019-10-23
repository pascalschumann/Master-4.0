using System.Collections.Generic;
using System.Linq;
using Master40.DB.Data.WrappersForPrimitives;
using Master40.DB.DataModel;
using Master40.DB.Enums;
using Zpp.DataLayer;
using Zpp.DataLayer.impl.DemandDomain;
using Zpp.DataLayer.impl.DemandDomain.Wrappers;
using Zpp.DataLayer.impl.ProviderDomain;
using Zpp.DataLayer.impl.ProviderDomain.Wrappers;
using Zpp.DataLayer.impl.ProviderDomain.WrappersForCollections;
using Zpp.DataLayer.impl.WrapperForEntities;
using Zpp.Util;

namespace Zpp.Mrp2.impl.Mrp1.impl.Stock.impl
{
    public class StockManager
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
            foreach (var stock in GetStocks())
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

        public void AdaptStock(Providers providers)
        {
            foreach (var provider in providers)
            {
                // a provider can influence the stock only once
                if (_alreadyConsideredProviders.Contains(provider))
                {
                    continue;
                }

                _alreadyConsideredProviders.Add(provider);

                // SE:W decrements stock
                if (provider.GetType() == typeof(StockExchangeProvider))
                {
                    Stock stock = _stocks[provider.GetArticleId()];
                    stock.DecrementBy(provider.GetQuantity());
                    
                }
            }
        }

        private List<Stock> GetStocks()
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

        /**
         * COP or PrOB --> satisfy by SE:W
         */
        public EntityCollector Satisfy(Demand demand, Quantity demandedQuantity)
        {
            Stock stock = _stocks[demand.GetArticleId()];
            EntityCollector entityCollector = new EntityCollector();

            Provider stockProvider = CreateStockExchangeProvider(demand.GetArticle(),
                demand.GetStartTime(), demandedQuantity);
            entityCollector.Add(stockProvider);

            T_DemandToProvider demandToProvider =
                new T_DemandToProvider(demand.GetId(), stockProvider.GetId(), demandedQuantity);
            entityCollector.Add(demandToProvider);


            return entityCollector;
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

        public EntityCollector CreateDependingDemands(Provider provider)
        {
            if (provider.GetQuantity().IsNull())
            {
                return null;
            }

            if (provider.GetType() != typeof(StockExchangeProvider))
            {
                throw new MrpRunException("This can only be called for StockExchangeProviders");
            }
            
            // check stock if dependingDemands are needed
            Stock stock = _stocks[provider.GetArticleId()];
            Quantity currentQuantity = stock.GetQuantity();
            if (currentQuantity.IsGreaterThanOrEqualTo(stock.GetMinStockLevel()))
            {
                // no dependingDemands are needed
                return null;
            }

            // try to provide by existing demand
            
            // collects stockExchangeDemands, providerToDemands
            EntityCollector entityCollector =
                _openDemandManager.SatisfyProviderByOpenDemand(provider, provider.GetQuantity());
            if (entityCollector == null)
            {
                entityCollector = new EntityCollector();
            }

            Quantity remainingQuantity = entityCollector.GetRemainingQuantity(provider);

            if (remainingQuantity.IsGreaterThan(Quantity.Null()))
            {
                LotSize.Impl.LotSize lotSizes =
                    new LotSize.Impl.LotSize(remainingQuantity, provider.GetArticleId());
                Quantity lotSizeSum = Quantity.Null();
                foreach (var lotSize in lotSizes.GetLotSizes())
                {
                    lotSizeSum.IncrementBy(lotSize);


                    Demand stockExchangeDemand =
                        StockExchangeDemand.CreateStockExchangeStockDemand(provider.GetArticle(),
                            provider.GetStartTime(), lotSize);
                    entityCollector.Add(stockExchangeDemand);

                    // quantityToReserve can be calculated as following
                    // given demandedQuantity - (sumLotSize - lotSize) - (lotSize - providedByOpen.remaining)
                    Quantity quantityOfNewCreatedDemandToReserve = provider.GetQuantity()
                        .Minus(lotSizeSum.Minus(lotSize)).Minus(provider.GetQuantity()
                            .Minus(entityCollector.GetRemainingQuantity(provider)));
                    T_ProviderToDemand providerToDemand = new T_ProviderToDemand(provider.GetId(),
                        stockExchangeDemand.GetId(), quantityOfNewCreatedDemandToReserve);
                    entityCollector.Add(providerToDemand);

                    if (lotSizeSum.IsGreaterThan(provider.GetQuantity()))
                        // remember created demand as openDemand
                    {
                        _openDemandManager.AddDemand(stockExchangeDemand,
                            quantityOfNewCreatedDemandToReserve);
                    }
                }
            }

            return entityCollector;
        }
        
        
    }
}